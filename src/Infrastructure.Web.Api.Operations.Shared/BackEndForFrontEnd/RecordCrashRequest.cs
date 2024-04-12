using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

[Route("/record/crash", OperationMethod.Post)]
public class RecordCrashRequest : UnTenantedEmptyRequest
{
    public required string Message { get; set; }
}