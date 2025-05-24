# Matroska Batch Flow

A cross-platform tool for batch processing and analyzing Matroska (MKV) files. This utility features both a command-line interface and a GUI built with Uno Platform, leveraging [MediaInfo](https://mediaarea.net/en/MediaInfo) to extract file details.

Matroska Batch Flow is inspired by the now-abandoned [jmkvpropedit](https://github.com/BrunoReX/jmkvpropedit) and aims to become a modern, straightforward replacement for users who need an easy-to-use application for Matroska batch processing tasks.

> [!WARNING]
> This project is in an early development stage and is under active development. It is not ready for production or testing use.

## Features

> [!NOTE]
> The application is in very early stages of development. This feature list is a placeholder and does not represent working features.

- **Batch Scanning**: Finds media files recursively in specified folders.
- **Detailed Analysis**: Extracts track and metadata information using MediaInfo.
- **CLI & GUI**: Provides both a command-line app and a graphical interface.
- **Extensible Core**: Core functionality is separated into a reusable library.

## Quick Start

```sh
# Build all projects
dotnet build

# Run the console application
cd MatroskaBatchFlow.Console
dotnet run

# Run the GUI application
cd MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno
dotnet run
```

## Project Structure

- **MatroskaBatchFlow.Core**  
   Core logic for file scanning, filtering, and MediaInfo integration. Contains the shared business logic.
- **MatroskaBatchFlow.Console**  
   Command-line utility. Mostly available for development purposes for interacting with the Core project.
- **MatroskaBatchFlow.Uno**  
   Cross-platform GUI built with Uno Platform. This is the primary way to use Matroska Batch Flow, offering an intuitive interface for batch processing Matroska files.

## Requirements

- [.NET 8.x SDK](https://dotnet.microsoft.com/download)
- [MediaInfo DLLs](https://mediaarea.net/en/MediaInfo) (included)
