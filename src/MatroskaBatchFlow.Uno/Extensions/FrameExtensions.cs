namespace MatroskaBatchFlow.Uno.Extensions;

public static class FrameExtensions
{
    // TODO: On release of C# 14, use the newly introduced extension method feature to simplify this.
    public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}
