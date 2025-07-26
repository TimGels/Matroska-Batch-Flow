using System.Text.Json;
using MatroskaBatchFlow.Core.Builders.MkvPropeditArguments;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _scanResult = string.Empty;

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }

    private readonly IFileScanner _fileScanner;
    private readonly IBatchConfiguration _batchConfiguration;

    public ICommand ScanFiles { get; }

    public ICommand GenerateMkvpropeditArguments { get; }

    public MainViewModel(
        IFileScanner fileScanner,
        IBatchConfiguration batchConfiguration,
        INavigationService navigationService,
        INavigationViewService navigationViewService)
    {
        _fileScanner = fileScanner;
        _batchConfiguration = batchConfiguration;
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        ScanFiles = new AsyncRelayCommand(ScanFilesAsync);
        GenerateMkvpropeditArguments = new RelayCommand(GenerateMkvpropeditArgumentsCommand);
        NavigationService.Navigated += OnNavigated;
    }

    private async Task ScanFilesAsync()
    {
        var scannedFiles = await _fileScanner.ScanWithMediaInfoAsync();

        // Create a dictionary to map each file to its scan result
        var scanResults = scannedFiles.ToDictionary(
            file => file.Path,
            file => file.Result
        );

        // Serialize the dictionary to JSON
        ScanResult = JsonSerializer.Serialize(scanResults, new JsonSerializerOptions
        {
            WriteIndented = true // For pretty-printing
        });
    }

    private void GenerateMkvpropeditArgumentsCommand()
    {
        var results = new List<string>();

        foreach (var file in _batchConfiguration.FileList)
        {
            var builder = new MkvPropeditArgumentsBuilder()
                .SetInputFile(file.Path)
                .WithTitle(_batchConfiguration.Title);

            // Add all tracks to the builder.
            AddTracksToBuilder(builder, _batchConfiguration.AudioTracks, TrackType.Audio);
            AddTracksToBuilder(builder, _batchConfiguration.VideoTracks, TrackType.Video);
            AddTracksToBuilder(builder, _batchConfiguration.SubtitleTracks, TrackType.Text);

            var args = builder.Build();
            results.Add(string.Join(" ", args));
        }

        ScanResult = string.Join(Environment.NewLine + Environment.NewLine, results);
    }

    /// <summary>
    /// Adds a collection of track configurations to the specified mkvpropedit arguments builder.
    /// </summary>
    /// <param name="builder">The builder to which the track configurations will be added.</param>
    /// <param name="tracks">A collection of track configurations to add. Each specifies properties such as position, 
    /// language, name, and flags.</param>
    /// <param name="type">The type of tracks to add. Only tracks of a type that corresponds to a Matroska track element 
    /// will be processed.</param>
    private static void AddTracksToBuilder(IMkvPropeditArgumentsBuilder builder, IEnumerable<TrackConfiguration> tracks, TrackType type)
    {
        if (!type.IsMatroskaTrackElement())
            return;

        foreach (var track in tracks)
        {
            builder.AddTrack(t => t
                .SetTrackId(track.Position)
                .SetTrackType(type)
                .WithLanguage(track.Language.Code)
                .WithName(track.Name)
                .WithIsDefault(track.Default)
                .WithIsForced(track.Forced)
            );
        }
    }

    /// <summary>
    /// Handles navigation events to update the navigation view's selected item and back navigation state.
    /// </summary>
    /// <param name="sender">The source of the navigation event.</param>
    /// <param name="eventArgs">The event data containing information about the navigation event, including the destination page type.</param>
    private void OnNavigated(object sender, NavigationEventArgs eventArgs)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (eventArgs.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(eventArgs.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}
