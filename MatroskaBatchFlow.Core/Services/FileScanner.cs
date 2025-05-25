using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services.TrackNamingRuleEngine;
using MediaInfoLib;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Services
{
    /// <summary>
    /// Scans directories for files based on specified options and analyzes them using MediaInfo.
    /// </summary>
    public class FileScanner(IOptionsMonitor<ScanOptions> optionsMonitor, IBatchConfiguration batchConfiguration) : IFileScanner
    {
        /// <summary>
        /// The options used for scanning directories and filtering files.
        /// </summary>
        private readonly ScanOptions _options = optionsMonitor.CurrentValue;

        /// <summary>
        /// List of scanned files containing their paths and MediaInfo parsed results.
        /// </summary>
        private readonly List<ScannedFileInfo> _scannedFiles = [];

        /// <summary>
        /// List of file processing rules for applying automatic track names based on their type.
        /// </summary>
        private static readonly List<IFileProcessingRule> _trackNameRules =
        [
            new SubtitleTrackNamingRule(),
            new AudioTrackNamingRule(),
            new VideoTrackNamingRule(),
        ];

        /// <summary>
        /// Scans the directory for files that match the specified filtering options.
        /// </summary>
        /// <param name="files">An array of <see cref="FileInfo"/> objects representing the files to scan.</param>
        /// <returns>A collection of <see cref="ScannedFileInfo"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when no files are provided for scanning.</exception>
        public async Task<IEnumerable<ScannedFileInfo>> ScanAsync(FileInfo[] files)
        {
            if (files == null || files.Length == 0)
                throw new ArgumentException("No files provided for scanning.", nameof(files));

            // Clear previous track configurations to avoid accumulation
            batchConfiguration.Clear();

            var scannedFiles = await AnalyzeFilesWithMediaInfoAsync(files.Select(f => f.FullName));
            AddTracksToBatchConfiguration(scannedFiles);

            var engine = new FileProcessingRuleEngine(_trackNameRules);
            foreach (var scannedFile in scannedFiles)
            {
                engine.ApplyRules(scannedFile, batchConfiguration);
            }

            _scannedFiles.Clear();
            _scannedFiles.AddRange(scannedFiles);
            return scannedFiles;
        }

        /// <summary>
        /// Scans the directory for files and analyzes them using MediaInfo.
        /// </summary>
        /// <returns> A collection of <see cref="ScannedFileInfo"/>.</returns>
        public async Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync()
        {
            EnsureDirectoryExists();
            var files = await Task.Run(() => GetFilteredFiles());
            var scannedFiles = await AnalyzeFilesWithMediaInfoAsync(files);

            _scannedFiles.Clear();
            _scannedFiles.AddRange(scannedFiles);

            return scannedFiles;
        }

        /// <summary>
        /// Gets the list of scanned files.
        /// </summary>
        /// <returns>A collection of <see cref="ScannedFileInfo"/>.</returns>
        public IEnumerable<ScannedFileInfo> GetScannedFiles() => _scannedFiles;

        /// <summary>
        /// Adds tracks to the batch configuration based on the scanned files. 
        /// Only tracks that are editable (configurable within the batch configuration) are added.
        /// </summary>
        /// <param name="scannedFileInfos">A collection of <see cref="ScannedFileInfo"/>.</param>
        private void AddTracksToBatchConfiguration(IEnumerable<ScannedFileInfo> scannedFileInfos)
        {
            var editableTracks = scannedFileInfos
                .SelectMany(sfi => sfi.Result?.Media?.Track ?? Enumerable.Empty<MediaInfoResult.MediaInfo.TrackInfo>())
                .Where(track => track.Type.IsEditable());

            foreach (var track in editableTracks)
            {
                var trackConfiguration = new TrackConfiguration
                {
                    TrackType = track.Type,
                    Position = track.StreamKindID,
                    Language = track.Language,
                };
                batchConfiguration.GetTrackListForType(trackConfiguration.TrackType).Add(trackConfiguration);
            }
            Console.WriteLine(batchConfiguration.VideoTracks.Count);
        }

        /// <summary>
        /// Ensures that the directory specified in the options exists.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_options.DirectoryPath))
                throw new DirectoryNotFoundException($"Directory: '{_options.DirectoryPath}' does not exist.");
        }

        /// <summary>
        /// Retrieves files from the directory based on the filtering options.
        /// </summary>
        private IEnumerable<string> GetFilteredFiles()
        {
            var files = Directory.EnumerateFiles(
                _options.DirectoryPath,
                "*.*",
                _options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            if (_options.ExcludeHidden)
            {
                files = files.Where(f => (File.GetAttributes(f) & FileAttributes.Hidden) == 0);
            }

            return files.Where(f => _options.AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Parses the JSON MediaInfo summary into a <see cref="ScannedFileInfo"/> object.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="filePath"></param>
        /// <returns><see cref="ScannedFileInfo"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
        private static ScannedFileInfo ParseMediaInfoJson(string json, string filePath)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            var mediaInfoResult = JsonSerializer.Deserialize<MediaInfoResult>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize MediaInfo JSON.");

            return new ScannedFileInfo()
            {
                Path = filePath,
                Result = mediaInfoResult
            };
        }

        /// <summary>
        ///  Analyzes the specified files using MediaInfo and returns detailed information.
        /// </summary>
        /// <param name="files"> The collection of file paths to analyze.</param>
        /// <returns> A collection of <see cref="ScannedFileInfo"/>.</returns>
        private static async Task<IEnumerable<ScannedFileInfo>> AnalyzeFilesWithMediaInfoAsync(IEnumerable<string> files)
        {
            return await Task.Run(() =>
            {
                var scannedFiles = new List<ScannedFileInfo>();
                var mediaInfo = new MediaInfo();

                foreach (var file in files)
                {
                    mediaInfo.Open(file);
                    mediaInfo.Option("Complete", "1");
                    mediaInfo.Option("Output", "JSON");
                    string info = mediaInfo.Inform();
                    mediaInfo.Close();
                    // Parse the JSON into a ScannedFileInfo object
                    var scannedFile = ParseMediaInfoJson(info, file);
                    scannedFiles.Add(scannedFile);
                }

                return scannedFiles;
            });
        }
    }
}
