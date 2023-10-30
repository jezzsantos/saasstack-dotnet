using JetBrains.Annotations;

namespace Common.Recording;

/// <summary>
///     Records permanent audit events in the system
/// </summary>
public interface IAuditReporter
{
    void Audit(ICallContext? context, string againstId, string auditCode,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs);
}