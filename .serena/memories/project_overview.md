# Matroska Batch Flow - Project Overview

## Purpose
**Matroska Batch Flow** is a powerful tool for batch processing Matroska (`.mkv`) files. It helps users efficiently manage and modify large collections of MKV files. The tool is designed for archivists, home theater enthusiasts, and anyone working with Matroska files to quickly perform tasks like fixing language tags, updating track names, or preparing files for media servers such as Jellyfin.

## Key Features
- **Batch Editing:** Modify container properties (track names, languages, flags) for multiple files at once
- **Modern GUI:** Built with WinUI 3 and Uno Platform for cross-platform support
- **Drag-and-Drop Support:** Easy file/folder addition
- **Validation:** Ensures only supported Matroska files are processed with compatibility checks
- **MediaInfo Integration:** Extracts detailed file information
- **MKVToolNix Integration:** Uses mkvpropedit for fast, direct container property editing

## Project Status
⚠️ Under active development - features and behavior may change

## Target Platforms
- **Windows 10/11 (Primary):** WinAppSDK/WinUI 3 (net10.0-windows10.0.19041)
- **Secondary Platforms:** Can be run on Windows and Linux via Uno Platform Skia Desktop renderer (net10.0-desktop)
  - Note: Not heavily tested; issues on these platforms are not prioritized

## Main Technologies
- **.NET 10.0** (net10.0, net10.0-windows10.0.19041, net10.0-desktop)
- **Uno Platform SDK 6.5.0** (preview/dev version)
- **WinUI 3** for the user interface
- **MediaInfo** library for media file analysis
- **MKVToolNix's mkvpropedit** for editing Matroska containers
- **Uno Features**: Lottie, MediaElement, Hosting, Toolkit, Logging (Serilog), Mvvm, Configuration, Serialization, Localization, ThemeService, SkiaRenderer
