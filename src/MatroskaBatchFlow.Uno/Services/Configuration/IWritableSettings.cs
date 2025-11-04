namespace MatroskaBatchFlow.Uno.Services.Configuration;

public interface IWritableSettings<T> : IOptions<T> where T : class, new()
{
    Task UpdateAsync(Action<T> applyChanges);
}
