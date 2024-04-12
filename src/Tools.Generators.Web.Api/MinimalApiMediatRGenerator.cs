using System.Text;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Tools.Generators.Web.Api;

/// <summary>
///     A source generator for converting any <see cref="IWebApiService" /> classes to
///     Minimal API registrations and MediatR handlers
/// </summary>
[Generator]
public class MinimalApiMediatRGenerator : ISourceGenerator
{
    private const string Filename = "MinimalApiMediatRGeneratedHandlers.g.cs";
    private const string RegistrationClassName = "MinimalApiRegistration";
    private const string TestingOnlyDirective = "TESTINGONLY";

    // ReSharper disable once UseCollectionExpression
    private static readonly string[] RequiredUsingNamespaces =
    {
        "System", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Http",
        "Microsoft.Extensions.DependencyInjection", "Infrastructure.Web.Api.Common.Extensions"
    };

    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var assemblyNamespace = context.Compilation.AssemblyName;
        var serviceClasses = GetWebApiServiceOperationsFromAssembly(context)
            .GroupBy(registrations => registrations.Class.TypeName)
            .ToList();

        var classUsingNamespaces = BuildUsingList(serviceClasses);
        var handlerClasses = new StringBuilder();
        var endpointRegistrations = new StringBuilder();
        foreach (var serviceRegistrations in serviceClasses)
        {
            BuildHandlerClasses(serviceRegistrations, handlerClasses);

            BuildEndpointRegistrations(serviceRegistrations, endpointRegistrations);
        }

        var fileSource = BuildFile(assemblyNamespace!, classUsingNamespaces, endpointRegistrations.ToString(),
            handlerClasses.ToString());

        context.AddSource(Filename, SourceText.From(fileSource, Encoding.UTF8));

        return;

        static string BuildFile(string assemblyNamespace, string allUsingNamespaces, string allEndpointRegistrations,
            string allHandlerClasses)
        {
            return $@"// <auto-generated/>
{allUsingNamespaces}
namespace {assemblyNamespace}
{{
    public static class {RegistrationClassName}
    {{
        public static void RegisterRoutes(this global::Microsoft.AspNetCore.Builder.WebApplication app)
        {{
    {allEndpointRegistrations}
        }}
    }}
}}

{allHandlerClasses}";
        }
    }

    private static string BuildUsingList(
        List<IGrouping<WebApiAssemblyVisitor.TypeName, WebApiAssemblyVisitor.ServiceOperationRegistration>>
            serviceClasses)
    {
        var usingList = new StringBuilder();

        var allNamespaces = serviceClasses.SelectMany(serviceClass => serviceClass)
            .SelectMany(registration => registration.Class.UsingNamespaces)
            .Concat(RequiredUsingNamespaces)
            .Distinct()
            .OrderByDescending(s => s)
            .ToList();

        allNamespaces.ForEach(@using => usingList.AppendLine($"using {@using};"));

        return usingList.ToString();
    }

    private static void BuildEndpointRegistrations(
        IGrouping<WebApiAssemblyVisitor.TypeName, WebApiAssemblyVisitor.ServiceOperationRegistration>
            serviceRegistrations, StringBuilder endpointRegistrations)
    {
        var serviceClassName = serviceRegistrations.Key.Name;
        var groupName = $"{serviceClassName.ToLowerInvariant()}Group";
        var basePath = serviceRegistrations.FirstOrDefault()?.Class.BasePath;
        var prefix = basePath.HasValue()
            ? $"\"{basePath}\""
            : "string.Empty";

        var endpointFilters = BuildEndpointFilters(serviceRegistrations);
        endpointRegistrations.AppendLine($@"        var {groupName} = app.MapGroup({prefix})
                .WithGroupName(""{serviceClassName}"")
                .RequireCors(""{WebHostingConstants.DefaultCORSPolicyName}""){endpointFilters};");

        foreach (var registration in serviceRegistrations)
        {
            if (registration.IsTestingOnly)
            {
                endpointRegistrations.AppendLine($"#if {TestingOnlyDirective}");
            }

            var routeEndpointMethodNames = ToMinimalApiRegistrationMethodNames(registration.OperationMethod);
            foreach (var routeEndpointMethod in routeEndpointMethodNames)
            {
                endpointRegistrations.AppendLine(
                    $"            {groupName}.{routeEndpointMethod}(\"{registration.RoutePath}\",");
                if (registration.OperationMethod == OperationMethod.Get
                    || registration.OperationMethod == OperationMethod.Search
                    || registration.OperationMethod == OperationMethod.Delete)
                {
                    endpointRegistrations.AppendLine(
                        $"                async (global::MediatR.IMediator mediator, [global::Microsoft.AspNetCore.Http.AsParameters] global::{registration.RequestDto.FullName} request) =>");
                }
                else
                {
                    if (registration.OperationMethod == OperationMethod.Post
                        && registration.IsMultipartFormData)
                    {
                        endpointRegistrations.AppendLine(
                            $"                async (global::MediatR.IMediator mediator, [global::Microsoft.AspNetCore.Mvc.FromForm] global::{registration.RequestDto.FullName} request) =>");
                    }
                    else
                    {
                        endpointRegistrations.AppendLine(
                            $"                async (global::MediatR.IMediator mediator, global::{registration.RequestDto.FullName} request) =>");
                    }
                }

                endpointRegistrations.Append(
                    "                     await mediator.Send(request, global::System.Threading.CancellationToken.None))");
                if (registration.OperationAccess != AccessType.Anonymous)
                {
                    endpointRegistrations.AppendLine();
                    var policyName = registration.OperationAccess switch
                    {
                        AccessType.Token => AuthenticationConstants.Authorization.TokenPolicyName,
                        AccessType.HMAC => AuthenticationConstants.Authorization.HMACPolicyName,
                        _ => string.Empty
                    };
                    if (policyName.HasValue())
                    {
                        endpointRegistrations.Append(
                            $@"                .RequireAuthorization(""{policyName}"")");
                    }
                }

                if (registration.OperationAuthorization is not null)
                {
                    var policyName = registration.OperationAuthorization.PolicyName;
                    endpointRegistrations.AppendLine();
                    endpointRegistrations.Append(
                        $@"                .RequireCallerAuthorization(""{policyName}"")");
                }

                if (registration.IsMultipartFormData)
                {
                    endpointRegistrations.AppendLine();
                    endpointRegistrations.Append(
                        @"                .DisableAntiforgery()");
                }

                endpointRegistrations.AppendLine(";");
            }

            if (registration.IsTestingOnly)
            {
                endpointRegistrations.AppendLine("#endif");
            }
        }
    }

    private static string BuildEndpointFilters(
        IGrouping<WebApiAssemblyVisitor.TypeName, WebApiAssemblyVisitor.ServiceOperationRegistration>
            serviceRegistrations)
    {
        var filterSet = new List<string>
        {
            "global::Infrastructure.Web.Api.Common.Endpoints.ApiUsageFilter",
            "global::Infrastructure.Web.Api.Common.Endpoints.RequestCorrelationFilter",
            "global::Infrastructure.Web.Api.Common.Endpoints.ContentNegotiationFilter"
        };
        var isMultiTenanted = serviceRegistrations.Any(registration => registration.IsRequestDtoTenanted);
        if (isMultiTenanted)
        {
            filterSet.Insert(0, "global::Infrastructure.Web.Api.Common.Endpoints.MultiTenancyFilter");
        }

        var builder = new StringBuilder();
        var counter = filterSet.Count;
        if (filterSet.HasAny())
        {
            builder.AppendLine();
            filterSet.ForEach(filter =>
            {
                counter--;
                var value = $"                .AddEndpointFilter<{filter}>()";
                if (counter == 0)
                {
                    builder.Append(value);
                }
                else
                {
                    builder.AppendLine(value);
                }
            });
        }

        return builder.ToString();
    }

    private static void BuildHandlerClasses(
        IGrouping<WebApiAssemblyVisitor.TypeName, WebApiAssemblyVisitor.ServiceOperationRegistration>
            serviceRegistrations, StringBuilder handlerClasses)
    {
        var serviceClassNamespace = $"{serviceRegistrations.Key.FullName}MediatRHandlers";
        handlerClasses.AppendLine($"namespace {serviceClassNamespace}");
        handlerClasses.AppendLine("{");

        foreach (var registration in serviceRegistrations)
        {
            var handlerClassName = $"{registration.MethodName}_{registration.RequestDto.Name}_Handler";
            var constructorAndFields = BuildInjectorConstructorAndFields(handlerClassName,
                registration.Class.Constructors.ToList());

            if (registration.IsTestingOnly)
            {
                handlerClasses.AppendLine($"#if {TestingOnlyDirective}");
            }

            handlerClasses.AppendLine(
                $"    public class {handlerClassName} : global::MediatR.IRequestHandler<global::{registration.RequestDto.FullName},"
                + $" global::Microsoft.AspNetCore.Http.IResult>");
            handlerClasses.AppendLine("    {");
            if (constructorAndFields.HasValue())
            {
                handlerClasses.AppendLine(constructorAndFields);
            }

            handlerClasses.AppendLine($"        public async Task<global::Microsoft.AspNetCore.Http.IResult>"
                                      + $" Handle(global::{registration.RequestDto.FullName} request, global::System.Threading.CancellationToken cancellationToken)");
            handlerClasses.AppendLine("        {");
            if (!registration.IsAsync)
            {
                handlerClasses.AppendLine(
                    "            await Task.CompletedTask;");
            }

            var callingParameters = string.Empty;
            var injectorCtor = registration.Class.Constructors.FirstOrDefault(ctor => ctor.IsInjectionCtor);
            if (injectorCtor is not null)
            {
                var parameters = injectorCtor.CtorParameters.ToList();
                foreach (var param in parameters)
                {
                    handlerClasses.AppendLine(
                        $"            var {param.VariableName} = _serviceProvider.GetRequiredService<{param.TypeName.FullName}>();");
                }

                handlerClasses.AppendLine();
                callingParameters = BuildInjectedParameters(registration.Class.Constructors.ToList());
            }

            handlerClasses.AppendLine(
                $"            var api = new global::{registration.Class.TypeName.FullName}({callingParameters});");
            var asyncAwait = registration.IsAsync
                ? "await "
                : string.Empty;
            var hasCancellationToken = registration.HasCancellationToken
                ? ", cancellationToken"
                : string.Empty;
            handlerClasses.AppendLine(
                $"            var result = {asyncAwait}api.{registration.MethodName}(request{hasCancellationToken});");
            handlerClasses.AppendLine(
                $"            return result.HandleApiResult(global::{typeof(OperationMethod).FullName}.{registration.OperationMethod});");
            handlerClasses.AppendLine("        }");
            handlerClasses.AppendLine("    }");
            if (registration.IsTestingOnly)
            {
                handlerClasses.AppendLine("#endif");
            }

            handlerClasses.AppendLine();
        }

        handlerClasses.AppendLine("}");
        handlerClasses.AppendLine();
    }

    private static string BuildInjectorConstructorAndFields(string handlerClassName,
        List<WebApiAssemblyVisitor.Constructor> constructors)
    {
        var handlerClassConstructorAndFields = new StringBuilder();

        var injectorCtor = constructors.FirstOrDefault(ctor => ctor.IsInjectionCtor);
        if (injectorCtor is not null)
        {
            handlerClassConstructorAndFields.AppendLine(
                "        private readonly global::System.IServiceProvider _serviceProvider;");
            handlerClassConstructorAndFields.AppendLine();
            handlerClassConstructorAndFields.AppendLine(
                $"        public {handlerClassName}(global::System.IServiceProvider serviceProvider)");
            handlerClassConstructorAndFields.AppendLine("        {");
            handlerClassConstructorAndFields.AppendLine("            _serviceProvider = serviceProvider;");
            handlerClassConstructorAndFields.AppendLine("        }");
        }

        return handlerClassConstructorAndFields.ToString();
    }

    private static string BuildInjectedParameters(List<WebApiAssemblyVisitor.Constructor> constructors)
    {
        var methodParameters = new StringBuilder();

        var injectorCtor = constructors.FirstOrDefault(ctor => ctor.IsInjectionCtor);
        if (injectorCtor is not null)
        {
            var parameters = injectorCtor.CtorParameters.ToList();

            var paramsRemaining = parameters.Count();
            foreach (var param in parameters)
            {
                methodParameters.Append($"{param.VariableName}");
                if (--paramsRemaining > 0)
                {
                    methodParameters.Append(", ");
                }
            }
        }

        return methodParameters.ToString();
    }

    private static List<WebApiAssemblyVisitor.ServiceOperationRegistration> GetWebApiServiceOperationsFromAssembly(
        GeneratorExecutionContext context)
    {
        var visitor = new WebApiAssemblyVisitor(context.CancellationToken, context.Compilation);
        visitor.Visit(context.Compilation.Assembly);
        return visitor.OperationRegistrations;
    }

    private static string[] ToMinimalApiRegistrationMethodNames(OperationMethod method)
    {
        return method switch
        {
            OperationMethod.Get => new[] { "MapGet" },
            OperationMethod.Search => new[] { "MapGet" },
            OperationMethod.Post => new[] { "MapPost" },
            OperationMethod.PutPatch => new[] { "MapPut", "MapPatch" },
            OperationMethod.Delete => new[] { "MapDelete" },
            _ => new[] { "MapGet" }
        };
    }
}