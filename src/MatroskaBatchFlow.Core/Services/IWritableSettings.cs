using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services;

public interface IWritableSettings<T> : IOptions<T> where T : class, new()
{
    Task UpdateAsync(Action<T> applyChanges);

    Task UpdateAsync(Action<T> applyChanges, Action<T> restoreOnFailure);

    Task<T> ReadFromDiskAsync();
}
