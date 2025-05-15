using MatroskaBatchFlow.Core.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MatroskaBatchFlow.Core
{
    /// <summary>
    /// Represents the configuration for batch processing of media files.
    /// </summary>
    public class BatchConfiguration : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _directoryPath = string.Empty;
        private string _title = string.Empty;
        private string _version = string.Empty;
        private IList<TrackConfiguration> _audioTracks = [];
        private IList<TrackConfiguration> _videoTracks = [];
        private IList<TrackConfiguration> _subtitleTracks = [];

        /// <summary>
        /// Gets or sets the directory path for the files.
        /// </summary>
        public string DirectoryPath { get => _directoryPath; set => SetField(ref _directoryPath, value); }

        /// <summary>
        /// Gets or sets the title of the files.
        /// </summary>
        public string Title { get => _title; set => SetField(ref _title, value); }

        /// <summary>
        /// Gets or sets the version of the files.
        /// </summary>
        public string Version { get => _version; set => SetField(ref _version, value); }

        /// <summary>
        /// Gets or sets the list of audio track configurations.
        /// </summary>
        public IList<TrackConfiguration> AudioTracks { get => _audioTracks; set => SetField(ref _audioTracks, value); }

        /// <summary>
        /// Gets or sets the list of video track configurations.
        /// </summary>
        public IList<TrackConfiguration> VideoTracks { get => _videoTracks; set => SetField(ref _videoTracks, value); }

        /// <summary>
        /// Gets or sets the list of subtitle track configurations.
        /// </summary>
        public IList<TrackConfiguration> SubtitleTracks { get => _subtitleTracks; set => SetField(ref _subtitleTracks, value); }

        /// <summary>
        /// Invokes the PropertyChanged event for a given property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Sets the field value and raises the PropertyChanged event if the value changes.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (optional).</param>
        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }
    }

    /// <summary>
    /// Represents the configuration for a specific media track.
    /// </summary>
    public sealed class TrackConfiguration : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private TrackType _trackType;
        private int _position;
        private string _name = string.Empty;
        private string _language = string.Empty;
        private bool _default;
        private bool _forced;
        private bool _remove;

        /// <summary>
        /// Gets or sets the type of the track (e.g., Audio, Video, Subtitle).
        /// </summary>
        public TrackType TrackType { get => _trackType; set => SetField(ref _trackType, value); }

        /// <summary>
        /// Gets or sets the position of the track in the media file.
        /// </summary>
        public int Position { get => _position; set => SetField(ref _position, value); }

        /// <summary>
        /// Gets or sets the name of the track.
        /// </summary>
        public string Name { get => _name; set => SetField(ref _name, value); }

        /// <summary>
        /// Gets or sets the language of the track.
        /// </summary>
        public string Language { get => _language; set => SetField(ref _language, value); }

        /// <summary>
        /// Gets or sets a value indicating whether the track is the default track.
        /// </summary>
        public bool Default { get => _default; set => SetField(ref _default, value); }

        /// <summary>
        /// Gets or sets a value indicating whether the track is forced.
        /// </summary>
        public bool Forced { get => _forced; set => SetField(ref _forced, value); }

        /// <summary>
        /// Gets or sets a value indicating whether the track should be removed.
        /// </summary>
        public bool Remove { get => _remove; set => SetField(ref _remove, value); }

        /// <summary>
        /// Invokes the PropertyChanged event for a given property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Sets the field value and raises the PropertyChanged event if the value changes.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (optional).</param>
        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }
    }
}
