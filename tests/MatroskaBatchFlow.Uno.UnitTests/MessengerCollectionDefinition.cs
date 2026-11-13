namespace MatroskaBatchFlow.Uno.UnitTests;

/// <summary>
/// Groups test classes that use the global <c>WeakReferenceMessenger.Default</c>, so they do not run in parallel.
/// </summary>
/// <remarks>
/// Some tests reset the global messenger, which would remove registrations made by tests running at the same time.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class MessengerCollectionDefinition
{
    public const string Name = "Messenger";
}
