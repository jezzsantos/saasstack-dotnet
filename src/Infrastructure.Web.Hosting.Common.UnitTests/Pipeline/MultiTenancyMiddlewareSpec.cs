using System.Net;
using System.Reflection;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Pipeline;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Pipeline;

[UsedImplicitly]
public class MultiTenancyMiddlewareSpec
{
    private static HttpContext SetupContext(ICallerContextFactory callerContextFactory,
        ITenancyContext tenancyContext)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
        serviceCollection.AddSingleton(callerContextFactory);
        serviceCollection.AddSingleton(tenancyContext);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Response =
            {
                StatusCode = 200,
                Body = new MemoryStream()
            }
        };

        return context;
    }

    [Trait("Category", "Unit")]
    public class GivenAnyCaller
    {
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<IEndUsersService> _endUsersService;
        private readonly MultiTenancyMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly Mock<ITenancyContext> _tenancyContext;
        private readonly Mock<ITenantDetective> _tenantDetective;

        public GivenAnyCaller()
        {
            var identifierFactory = new Mock<IIdentifierFactory>();
            identifierFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
                .Returns(true);
            _tenancyContext = new Mock<ITenancyContext>();
            _callerContextFactory = new Mock<ICallerContextFactory>();
            var caller = new Mock<ICallerContext>();
            caller.Setup(c => c.IsAuthenticated)
                .Returns(false);
            _callerContextFactory.Setup(ccf => ccf.Create())
                .Returns(caller.Object);
            var organizationsService = new Mock<IOrganizationsService>();
            organizationsService.Setup(os =>
                    os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantSettings());
            _endUsersService = new Mock<IEndUsersService>();
            _tenantDetective = new Mock<ITenantDetective>();
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(false, null));
            _next = new Mock<RequestDelegate>();

            _middleware = new MultiTenancyMiddleware(_next.Object, identifierFactory.Object, _endUsersService.Object,
                organizationsService.Object);
        }

        [Fact]
        public async Task WhenInvokeAndHasNoEndpoint_ThenContinuesPipeline()
        {
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(context));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAndHasEndpointWithNoParameters_ThenContinuesPipeline()
        {
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "aroute"));

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(context));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAndHasEndpointWithWrongNumberOfParameters_ThenContinuesPipeline()
        {
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            var methodDelegate = () => { };
            var metadata = new EndpointMetadataCollection(methodDelegate.GetMethodInfo());
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "aroute"));

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(context));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAndHasEndpointWithWrongRequestType_ThenContinuesPipeline()
        {
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            var methodDelegate = (object _, TestIllegalRequest request) => { };
            var metadata = new EndpointMetadataCollection(methodDelegate.GetMethodInfo());
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "aroute"));

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(context));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAndHasEndpointWithCorrectRequestType_ThenContinuesPipeline()
        {
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            var methodDelegate = (object _, TestTenantedRequest request) => { };
            var metadata = new EndpointMetadataCollection(methodDelegate.GetMethodInfo());
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "aroute"));

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(context));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, typeof(TestTenantedRequest), CancellationToken.None));
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnAnonymousUser
    {
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<IEndUsersService> _endUsersService;
        private readonly Mock<IIdentifierFactory> _identifierFactory;
        private readonly MultiTenancyMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly Mock<IOrganizationsService> _organizationsService;
        private readonly Mock<ITenancyContext> _tenancyContext;
        private readonly Mock<ITenantDetective> _tenantDetective;

        public GivenAnAnonymousUser()
        {
            _identifierFactory = new Mock<IIdentifierFactory>();
            _identifierFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
                .Returns(true);
            _tenancyContext = new Mock<ITenancyContext>();
            _callerContextFactory = new Mock<ICallerContextFactory>();
            var caller = new Mock<ICallerContext>();
            caller.Setup(c => c.IsAuthenticated)
                .Returns(false);
            _callerContextFactory.Setup(ccf => ccf.Create())
                .Returns(caller.Object);
            _organizationsService = new Mock<IOrganizationsService>();
            _organizationsService.Setup(os =>
                    os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantSettings());
            _endUsersService = new Mock<IEndUsersService>();
            _tenantDetective = new Mock<ITenantDetective>();
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(false, null));
            _next = new Mock<RequestDelegate>();

            _middleware = new MultiTenancyMiddleware(_next.Object, _identifierFactory.Object, _endUsersService.Object,
                _organizationsService.Object);
        }

        [Fact]
        public async Task WhenInvokeAndNoUnRequiredTenantId_ThenContinuesPipeline()
        {
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(context));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAndNoRequiredTenantId_ThenRespondsWithAProblem()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, null));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.BadRequest,
                Resources.MultiTenancyMiddleware_MissingTenantId);
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAndTenantedRequestButTenantIdIsInvalid_ThenRespondsWithAProblem()
        {
            _identifierFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
                .Returns(false);
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.BadRequest,
                Resources.MultiTenancyMiddleware_InvalidTenantId);
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenInvokeWithUnRequiredTenantId_ThenSetsTenantAndContinuesPipeline()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(false, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()));
            _tenancyContext.Verify(t => t.Set("atenantid", It.IsAny<TenantSettings>()));
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
            _organizationsService.Verify(os =>
                os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), "atenantid", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeWithRequiredTenantId_ThenSetsTenantAndContinuesPipeline()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()));
            _tenancyContext.Verify(t => t.Set("atenantid", It.IsAny<TenantSettings>()));
            _endUsersService.Verify(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
            _organizationsService.Verify(os =>
                os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), "atenantid", It.IsAny<CancellationToken>()));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnAuthenticatedUser
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<IEndUsersService> _endUsersService;
        private readonly MultiTenancyMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly Mock<IOrganizationsService> _organizationsService;
        private readonly Mock<ITenancyContext> _tenancyContext;
        private readonly Mock<ITenantDetective> _tenantDetective;

        public GivenAnAuthenticatedUser()
        {
            var identifierFactory = new Mock<IIdentifierFactory>();
            identifierFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
                .Returns(true);
            _tenancyContext = new Mock<ITenancyContext>();
            _callerContextFactory = new Mock<ICallerContextFactory>();
            _caller = new Mock<ICallerContext>();
            _caller.Setup(cc => cc.IsAuthenticated)
                .Returns(true);
            _caller.Setup(cc => cc.CallerId)
                .Returns("acallerid");
            _callerContextFactory.Setup(ccf => ccf.Create())
                .Returns(_caller.Object);
            _organizationsService = new Mock<IOrganizationsService>();
            _organizationsService.Setup(os =>
                    os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantSettings());
            _endUsersService = new Mock<IEndUsersService>();
            _endUsersService.Setup(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EndUserWithMemberships
                {
                    Id = "auserid",
                    Memberships = new List<Membership>()
                });
            _tenantDetective = new Mock<ITenantDetective>();
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(false, null));

            _next = new Mock<RequestDelegate>();

            _middleware = new MultiTenancyMiddleware(_next.Object, identifierFactory.Object, _endUsersService.Object,
                _organizationsService.Object);
        }

        [Fact]
        public async Task WhenInvokeAndUnRequiredTenantIdButNotAMember_ThenRespondsWithAProblem()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(false, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.MultiTenancyMiddleware_UserNotAMember.Format("atenantid"));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), "acallerid", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeAndUnRequiredTenantIdAndIsAMember_ThenSetsTenantAndContinuesPipeline()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(false, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            _endUsersService.Setup(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EndUserWithMemberships
                {
                    Id = "auserid",
                    Memberships =
                    [
                        new Membership
                        {
                            Id = "amembershipid",
                            UserId = "auserid",
                            OrganizationId = "atenantid"
                        }
                    ]
                });

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()));
            _tenancyContext.Verify(t => t.Set("atenantid", It.IsAny<TenantSettings>()));
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), "acallerid", It.IsAny<CancellationToken>()));
            _organizationsService.Verify(os =>
                os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), "atenantid", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeAndNoRequiredTenantIdButNoMemberships_ThenRespondsWithAProblem()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, null));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.BadRequest,
                Resources.MultiTenancyMiddleware_MissingTenantId);
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), "acallerid", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeAndNoRequiredTenantIdButNoDefaultOrganization_ThenRespondsWithAProblem()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, null));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            _endUsersService.Setup(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EndUserWithMemberships
                {
                    Id = "auserid",
                    Memberships =
                    [
                        new Membership
                        {
                            Id = "amembershipid",
                            UserId = "auserid",
                            OrganizationId = "atenantid"
                        }
                    ]
                });

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.BadRequest,
                Resources.MultiTenancyMiddleware_MissingTenantId);
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), "acallerid", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeAndNoRequiredTenantIdButHasDefaultOrganization_ThenSetsTenantAndContinuesPipeline()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, null));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            _endUsersService.Setup(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EndUserWithMemberships
                {
                    Id = "auserid",
                    Memberships =
                    [
                        new Membership
                        {
                            Id = "amembershipid",
                            UserId = "auserid",
                            IsDefault = true,
                            OrganizationId = "adefaultorganizationid"
                        }
                    ]
                });

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set("adefaultorganizationid", It.IsAny<TenantSettings>()));
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(_caller.Object, "acallerid", CancellationToken.None));
            _organizationsService.Verify(os =>
                os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), "adefaultorganizationid",
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeAndRequiredTenantIdButNotAMember_ThenRespondsWithAProblem()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.MultiTenancyMiddleware_UserNotAMember.Format("atenantid"));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
            _tenancyContext.Verify(t => t.Set(It.IsAny<string>(), It.IsAny<TenantSettings>()), Times.Never);
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), "acallerid", It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenInvokeAndRequiredTenantIdAndIsAMember_ThenSetsTenantAndContinuesPipeline()
        {
            _tenantDetective.Setup(td =>
                    td.DetectTenantAsync(It.IsAny<HttpContext>(), It.IsAny<Optional<Type>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantDetectionResult(true, "atenantid"));
            var context = SetupContext(_callerContextFactory.Object, _tenancyContext.Object);
            _endUsersService.Setup(os =>
                    os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EndUserWithMemberships
                {
                    Id = "auserid",
                    Memberships =
                    [
                        new Membership
                        {
                            Id = "amembershipid",
                            UserId = "auserid",
                            OrganizationId = "atenantid"
                        }
                    ]
                });

            await _middleware.InvokeAsync(context, _tenancyContext.Object, _callerContextFactory.Object,
                _tenantDetective.Object);

            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()));
            _tenantDetective.Verify(td =>
                td.DetectTenantAsync(context, Optional<Type>.None, CancellationToken.None));
            _tenancyContext.Verify(t => t.Set("atenantid", It.IsAny<TenantSettings>()));
            _organizationsService.Verify(os =>
                os.GetSettingsPrivateAsync(It.IsAny<ICallerContext>(), "atenantid", It.IsAny<CancellationToken>()));
            _endUsersService.Verify(os =>
                os.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), "acallerid",
                    It.IsAny<CancellationToken>()));
        }
    }

    [UsedImplicitly]
    private class TestIllegalRequest;

    [UsedImplicitly]
    private class TestTenantedRequest : IWebRequest<TestResponse>, ITenantedRequest
    {
        public string? OrganizationId { get; set; }
    }

    [UsedImplicitly]
    private class TestResponse : IWebResponse;
}