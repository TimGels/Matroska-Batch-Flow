using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

public interface IMkvPropeditService
{
    Task<MkvPropeditResult> ExecuteAsync(string arguments);
}
