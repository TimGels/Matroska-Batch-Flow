using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services;

public interface IWritableSettings<T> : IOptions<T> where T : class, new()
{
    Task UpdateAsync(Action<T> applyChanges);

    Task DiscardPendingChanges(Action<T> restoreChanges);

    Task<T> ReadFromDiskAsync();
}
