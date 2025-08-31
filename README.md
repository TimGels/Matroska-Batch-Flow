![Matroska Batch Flow Banner](Assets/Title-Banner.png)

**Matroska Batch Flow** is a powerful tool for batch processing Matroska (`.mkv`) files, designed to help you efficiently manage and modify large collections. It features a modern graphical user interface built with WinUI 3 and Uno Platform, and integrates [MediaInfo](https://mediaarea.net/en/MediaInfo) for extracting detailed file information, as well as [MKVToolNix's](https://mkvtoolnix.org) mkvpropedit for fast, direct container property editing.

Whether you're an archivist, home theater enthusiast, or anyone who works with Matroska files, Matroska Batch Flow helps you save time and effort. Quickly perform tasks like fixing language tags, updating track names, or preparing files for media servers such as Jellyfin. The tool is suitable for both small batches and large collections, making repetitive editing tasks efficient and ensuring consistency throughout your library.

> [!WARNING]
> This project is under active development. Features and behavior may change, and it may not be suitable for production use.

## Features

- **Batch Editing:** Modify container properties (such as track names, languages, and flags) for multiple files at once.
- **Modern Graphical User Interface:** A modern GUI built with WinUI 3 and Uno Platform for an intuitive user experience.
- **Drag-and-Drop Support:** Easily add files or folders to the batch by dragging them into the application.
- **Validation:** Ensures only supported Matroska files are processed, and checks compatibility (e.g., matching tracks) to prevent accidental modification of files with differing structures or properties.

## Preview

![Screenshot: Example input view in Matroska Batch Flow](Assets/Input.png)

## Project Structure

- **MatroskaBatchFlow.Core**  
   Core logic for file scanning, filtering, MediaInfo integration, and batch processing. Contains shared business logic and services.
- **MatroskaBatchFlow.Console**  
   Command-line utility, currently primarily intended and used for development and interacting with the Core project.
- **MatroskaBatchFlow.Uno**  
   Graphical user interface built with Uno Platform and WinUI 3. This is the primary way to use Matroska Batch Flow for most users.
- **tests/**  
   Unit tests for the projects.

## Development requirements

These requirements are for building the project:

- Windows 10/11 for GUI (support for other platforms is planned, utilizing Uno Platform's Skia Desktop)
- [MKVToolNix's](https://mkvtoolnix.download/) mkvpropedit (already bundled for WinAppSDK target)
- [.NET 9.x SDK](https://dotnet.microsoft.com/download)
