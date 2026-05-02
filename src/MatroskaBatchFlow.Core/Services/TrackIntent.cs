using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using static MatroskaBatchFlow.Core.Models.MediaInfoResult.MediaInfo;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Represents the user's intent for modifying a specific track across all files in the batch.
/// </summary>
/// <param name="trackInfo">The scanned track information from MediaInfo. Must not be null.</param>
public sealed class TrackIntent(TrackInfo trackInfo) : INotifyPropertyChanged
{
    private TrackType _type;
    private int _index;
    private string _name = string.Empty;
    private MatroskaLanguageOption _language = MatroskaLanguageOption.Undetermined;
    private bool _default;
    private bool _forced;
    private bool _enabled = true;
    private bool _shouldModifyName;
    private bool _shouldModifyLanguage;
    private bool _shouldModifyDefaultFlag;
    private bool _shouldModifyForcedFlag;
    private bool _shouldModifyEnabledFlag;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The raw track information as returned by MediaInfo for this track position.
    /// </summary>
    public TrackInfo ScannedTrackInfo { get; init; } = trackInfo ?? throw new ArgumentNullException(nameof(trackInfo));

    /// <summary>
    /// The type of this track (Audio, Video, Text).
    /// </summary>
    public TrackType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
    }

    /// <summary>
    /// Zero-based index of this track within its type.
    /// </summary>
    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                OnPropertyChanged(nameof(Index));
            }
        }
    }

    /// <summary>
    /// The track name to apply when <see cref="ShouldModifyName"/> is enabled.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// The track language to apply when <see cref="ShouldModifyLanguage"/> is enabled.
    /// </summary>
    public MatroskaLanguageOption Language
    {
        get => _language;
        set
        {
            if (!EqualityComparer<MatroskaLanguageOption>.Default.Equals(_language, value))
            {
                _language = value;
                OnPropertyChanged(nameof(Language));
            }
        }
    }

    /// <summary>
    /// The default flag to apply when <see cref="ShouldModifyDefaultFlag"/> is enabled.
    /// </summary>
    public bool Default
    {
        get => _default;
        set
        {
            if (_default != value)
            {
                _default = value;
                OnPropertyChanged(nameof(Default));
            }
        }
    }

    /// <summary>
    /// The forced flag to apply when <see cref="ShouldModifyForcedFlag"/> is enabled.
    /// </summary>
    public bool Forced
    {
        get => _forced;
        set
        {
            if (_forced != value)
            {
                _forced = value;
                OnPropertyChanged(nameof(Forced));
            }
        }
    }

    /// <summary>
    /// The enabled flag to apply when <see cref="ShouldModifyEnabledFlag"/> is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }

    /// <summary>
    /// Whether the track name should be modified during batch processing.
    /// </summary>
    public bool ShouldModifyName
    {
        get => _shouldModifyName;
        set
        {
            if (_shouldModifyName != value)
            {
                _shouldModifyName = value;
                OnPropertyChanged(nameof(ShouldModifyName));
            }
        }
    }

    /// <summary>
    /// Whether the track language should be modified during batch processing.
    /// </summary>
    public bool ShouldModifyLanguage
    {
        get => _shouldModifyLanguage;
        set
        {
            if (_shouldModifyLanguage != value)
            {
                _shouldModifyLanguage = value;
                OnPropertyChanged(nameof(ShouldModifyLanguage));
            }
        }
    }

    /// <summary>
    /// Whether the default flag should be modified during batch processing.
    /// </summary>
    public bool ShouldModifyDefaultFlag
    {
        get => _shouldModifyDefaultFlag;
        set
        {
            if (_shouldModifyDefaultFlag != value)
            {
                _shouldModifyDefaultFlag = value;
                OnPropertyChanged(nameof(ShouldModifyDefaultFlag));
            }
        }
    }

    /// <summary>
    /// Whether the forced flag should be modified during batch processing.
    /// </summary>
    public bool ShouldModifyForcedFlag
    {
        get => _shouldModifyForcedFlag;
        set
        {
            if (_shouldModifyForcedFlag != value)
            {
                _shouldModifyForcedFlag = value;
                OnPropertyChanged(nameof(ShouldModifyForcedFlag));
            }
        }
    }

    /// <summary>
    /// Whether the enabled flag should be modified during batch processing.
    /// </summary>
    public bool ShouldModifyEnabledFlag
    {
        get => _shouldModifyEnabledFlag;
        set
        {
            if (_shouldModifyEnabledFlag != value)
            {
                _shouldModifyEnabledFlag = value;
                OnPropertyChanged(nameof(ShouldModifyEnabledFlag));
            }
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}