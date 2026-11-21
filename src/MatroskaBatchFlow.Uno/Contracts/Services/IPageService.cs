namespace MatroskaBatchFlow.Uno.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);

    /// <summary>
    /// Creates the parameter for a navigation that supplies none, or returns <see langword="null"/>
    /// when the page does not need one.
    /// </summary>
    object? CreateDefaultParameter(string key);
}
