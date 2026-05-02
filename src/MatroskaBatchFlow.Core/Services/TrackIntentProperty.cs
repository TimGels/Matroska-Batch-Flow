namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Identifies a single user-editable property on a <see cref="TrackIntent"/>.
/// Used by the user-set flag API so callers can mark or reset the dirty state of
/// individual properties without exposing per-property setters.
/// </summary>
public enum TrackIntentProperty
{
    Name,
    Language,
    Default,
    Forced,
    Enabled,
}
