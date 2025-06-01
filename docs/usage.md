# MoodTagger Usage Guide

This guide explains how to use MoodTagger to analyze MP3 files and add mood tags for DJing.

## Prerequisites

Before using MoodTagger, ensure you have:

1. Installed [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0) or later
2. Installed [Ollama](https://ollama.ai/download)
3. Pulled a compatible model in Ollama (e.g., `ollama pull llama3`)
4. Started the Ollama service (`ollama serve`)

## Basic Usage

### Analyzing a Single File

To analyze a single MP3 file:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj analyze -f "path/to/file.mp3"
```

This will:
1. Extract audio features from the MP3 file
2. Send the features to Ollama for mood analysis
3. Write the analysis results back to the MP3 file as ID3v2 tags
4. Display the analysis results in the console

### Processing a Directory

To process all MP3 files in a directory:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj batch -d "path/to/directory"
```

To include subdirectories:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj batch -d "path/to/directory" -r
```

### Reading Tags

To read mood tags from an MP3 file:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj read -f "path/to/file.mp3"
```

### Restoring Backups

MoodTagger creates backups of MP3 files before modifying them. To restore a backup:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj restore -f "path/to/file.mp3"
```

## Advanced Options

### Test Mode

To analyze files without writing tags (test mode):

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj analyze -f "path/to/file.mp3" -t
```

### Disabling Backups

To disable creating backups:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj analyze -f "path/to/file.mp3" -n
```

### Verbose Output

To enable verbose output:

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj analyze -f "path/to/file.mp3" -v
```

## Configuration

MoodTagger uses a configuration file to control its behavior. The default configuration file is located at `%APPDATA%\MoodTagger\config.json`.

### Creating a Default Configuration

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj config --create
```

### Viewing the Current Configuration

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj config --show
```

### Using a Custom Configuration File

```bash
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj analyze -f "path/to/file.mp3" -c "path/to/config.json"
```

## Understanding Mood Tags

MoodTagger analyzes the following mood dimensions:

1. **Mood Valence** (-5 to +5): Emotional tone. -5 = very dark, 0 = neutral, +5 = euphoric
2. **Energy** (1 to 10): Intensity & drive. 1 = ambient, 10 = rave monster
3. **Groove Tightness** (-5 to +5): -5 = highly swung/broken, 0 = straight, +5 = extremely tight quantized
4. **Funk/Swing** (0 to 10): Groove funkiness. More swing, syncopation = higher score
5. **Tempo**: Exact BPM (e.g., 128, 140, 172)
6. **Dancefloor Use** (1 to 5): 1 = ambient opener, 3 = peak groover, 5 = main drop bomb
7. **Layering Potential** (0 to 10): How well it layers over others; e.g., tool tracks score high
8. **Tension** (-5 to +5): -5 = deeply relaxing, +5 = anxiety-inducing / suspenseful
9. **Rhythmic Complexity** (0 to 10): Polyrhythms, syncopation, odd time = high score
10. **Sound Palette** (-5 to +5): -5 = organic/acoustic, +5 = synthetic/futuristic

These tags are written to the MP3 file as ID3v2 TXXX frames, which can be read by many DJ software applications.

## Troubleshooting

### Ollama Connection Issues

If MoodTagger cannot connect to Ollama, ensure:

1. Ollama is running (`ollama serve`)
2. The Ollama URL in the configuration is correct (default: `http://localhost:11434/api`)
3. The specified model is available in Ollama (check with `ollama list`)

### Audio Processing Issues

If MoodTagger fails to process an MP3 file:

1. Ensure the file is a valid MP3 file
2. Check if the file is corrupted or protected
3. Try using a different MP3 file

### Tag Writing Issues

If MoodTagger fails to write tags:

1. Ensure the file is not read-only
2. Check if the file is in use by another application
3. Try using the `-t` option to test without writing tags

## Building from Source

To build MoodTagger from source:

```bash
git clone https://github.com/yourusername/MoodTagger.git
cd MoodTagger
dotnet build
```

## Creating a Standalone Executable

To create a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be located in `src/MoodTagger.CLI/bin/Release/net6.0/win-x64/publish/`.
