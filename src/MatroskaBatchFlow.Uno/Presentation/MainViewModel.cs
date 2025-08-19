using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Builders.MkvPropeditArguments;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IMkvPropeditService _mkvPropeditService;

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }
    public ICommand GenerateMkvpropeditArgumentsCommand { get; }
    public ICommand ProcessFileCommand { get; }

    public MainViewModel(
        IFileScanner fileScanner,
        IBatchConfiguration batchConfiguration,
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IMkvPropeditService mkvPropeditService)
    {
        _batchConfiguration = batchConfiguration;
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        _mkvPropeditService = mkvPropeditService;
        GenerateMkvpropeditArgumentsCommand = new RelayCommand(GenerateMkvpropeditArgumentsHandler);
        ProcessFileCommand = new AsyncRelayCommand(ProcessFileAsync);
        NavigationService.Navigated += OnNavigated;
    }

    /// <summary>
    /// Generates the <c>mkvpropedit</c> command-line arguments based on the current batch configuration.
    /// </summary>
    private void GenerateMkvpropeditArgumentsHandler()
    {
        // Generate the mkvpropedit arguments for the current batch configuration.
        string[] arguments = GenerateMkvpropeditArguments(_batchConfiguration);
        var argumentsString = string.Join(Environment.NewLine + Environment.NewLine, arguments);

        _batchConfiguration.MkvpropeditArguments = argumentsString;
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

    /// <summary>
    /// Processes the current file by executing the <c>mkvpropedit</c> command with the arguments generated from the batch configuration.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessFileAsync()
    {
        // Generate the mkvpropedit arguments for the current batch.
        string[] arguments = GenerateMkvpropeditArguments(_batchConfiguration);
        var argumentsString = string.Join(Environment.NewLine + Environment.NewLine, arguments);

        var result = await _mkvPropeditService.ExecuteAsync(argumentsString);

        string dialogTitle = result.Status switch
        {
            MkvPropeditStatus.Success => "Success",
            MkvPropeditStatus.Warning => "Warning",
            _ => "Error"
        };

        WeakReferenceMessenger.Default.Send(new DialogMessage(dialogTitle, result.Output));
    }

    /// <summary>
    /// Generates an array of command-line arguments for <c>mkvpropedit</c> based on the provided batch configuration.
    /// Each string in the returned array corresponds to the arguments for a single file in the batch.
    /// </summary>
    /// <param name="batchConfiguration">The batch configuration containing the list of files and associated settings.</param>
    /// <returns>
    /// An array of strings, where each string represents the command-line arguments for <c>mkvpropedit</c> for a specific file.
    /// </returns>
    private static string[] GenerateMkvpropeditArguments(IBatchConfiguration batchConfiguration)
    {
        List<string> results = [];

        foreach (var file in batchConfiguration.FileList)
        {
            string[] arguments = GenerateMkvpropeditArgumentsForFile(file, batchConfiguration);
            results.Add(string.Join(" ", arguments));
        }

        return [.. results];
    }

    /// <summary>
    /// Generates an array of command-line arguments for <c>mkvpropedit</c> for the specified file and batch configuration.
    /// The generated arguments will only include the title if <see cref="IBatchConfiguration.ShouldModifyTitle"/> is <c>true</c>.
    /// Track-related arguments are included only for properties whose corresponding <c>ShouldModify*</c> flags are set to <c>true</c>
    /// in each <see cref="TrackConfiguration"/> (see <see cref="AddTracksToBuilder"/> for details).
    /// </summary>
    /// <param name="scannedFile">The file to be processed, containing information such as the file path.</param>
    /// <param name="batchConfiguration">
    /// The batch configuration specifying the title and track information to include in the arguments.
    /// Only properties with their <c>ShouldModify*</c> flags set to <c>true</c> will be included.
    /// </param>
    /// <returns>
    /// An array of strings representing the arguments to be passed to <c>mkvpropedit</c> for the specified file.
    /// </returns>
    private static string[] GenerateMkvpropeditArgumentsForFile(ScannedFileInfo scannedFile, IBatchConfiguration batchConfiguration)
    {
        var builder = new MkvPropeditArgumentsBuilder();

        if (batchConfiguration.ShouldModifyTitle)
        {
            builder.WithTitle(batchConfiguration.Title);
        }

        if (batchConfiguration.ShouldModifyTrackStatisticsTags)
        {
            if (batchConfiguration.AddTrackStatisticsTags)
            {
                builder.WithAddTrackStatisticsTags();
            }
            if (batchConfiguration.DeleteTrackStatisticsTags)
            {
                builder.WithDeleteTrackStatisticsTags();
            }
        }

        // Add track configurations to the builder for audio, video, and subtitle tracks.
        AddTracksToBuilder(builder, batchConfiguration.AudioTracks, TrackType.Audio);
        AddTracksToBuilder(builder, batchConfiguration.VideoTracks, TrackType.Video);
        AddTracksToBuilder(builder, batchConfiguration.SubtitleTracks, TrackType.Text);

        // If no modifications are specified, we do not need to generate any arguments.
        if (builder.IsEmpty())
        {
            return [];
        }

        builder.SetInputFile(scannedFile.Path);

        return builder.Build();
    }

    /// <summary>
    /// Adds a collection of track configurations to the specified mkvpropedit arguments builder.
    /// For each track, only properties with their corresponding <c>ShouldModify*</c> flag set to <c>true</c>
    /// will be included in the generated arguments
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
            // Only add the track if at least one property should be modified.
            if (!(track.ShouldModifyLanguage ||
                  track.ShouldModifyName ||
                  track.ShouldModifyDefaultFlag ||
                  track.ShouldModifyForcedFlag ||
                  track.ShouldModifyEnabledFlag))
            {
                continue;
            }

            builder.AddTrack(tb =>
            {
                tb.SetTrackId(track.Index + 1) // Convert to 1-based index for mkvpropedit
                .SetTrackType(type);

                if (track.ShouldModifyLanguage)
                    tb.WithLanguage(track.Language.Code);

                if (track.ShouldModifyName)
                    tb.WithName(track.Name);

                if (track.ShouldModifyDefaultFlag)
                    tb.WithIsDefault(track.Default);

                if (track.ShouldModifyForcedFlag)
                    tb.WithIsForced(track.Forced);

                if (track.ShouldModifyEnabledFlag)
                    tb.WithIsEnabled(track.Enabled);

                return tb;
            });
        }
    }
}
