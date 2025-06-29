using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;
internal class TrackOptions
{
    public int? TrackId { get; set; }
    public TrackType? TrackType { get; set; }
    public string? Language { get; set; }
    public string? Name { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsForced { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsCommentary { get; set; }
}
