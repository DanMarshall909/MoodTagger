# MoodTagger

MoodTagger is a tool that uses AI to analyze MP3 files and add mood tags for DJing. It extracts audio features from MP3 files, analyzes them using the Ollama API, and writes the results back to the MP3 files as ID3v2 tags.

## Features

- Extracts audio features from MP3 files using NAudio
- Analyzes mood using Ollama AI models
- Writes mood tags to MP3 files using TagLib#
- Supports batch processing of directories
- Command-line interface for easy integration
- Creates backups before modifying files
- Configurable via JSON configuration file

## Requirements

- .NET 6.0 or later
- Ollama running locally (or accessible via network)
- A compatible Ollama model (e.g., llama3)

## Installation

1. Clone the repository
2. Build the solution:
   ```
   dotnet build
   ```
3. Run the CLI:
   ```
   dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj
   ```

## Usage

### Analyze a single file

```
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj analyze -f "path/to/file.mp3"
```

### Process a directory

```
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj batch -d "path/to/directory" -r
```

### Read tags from a file

```
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj read -f "path/to/file.mp3"
```

### Restore a backup

```
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj restore -f "path/to/file.mp3"
```

### Manage configuration

```
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj config --create
dotnet run --project src/MoodTagger.CLI/MoodTagger.CLI.csproj config --show
```

## Mood Dimensions

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

## Configuration

The configuration file is stored in `%APPDATA%\MoodTagger\config.json` by default. You can specify a different path using the `-c` option.

Example configuration:

```json
{
  "SampleRate": 44100,
  "FrameSize": 1024,
  "HopSize": 512,
  "OllamaBaseUrl": "http://localhost:11434/api",
  "OllamaModel": "llama3",
  "Temperature": 0.1,
  "MaxTokens": 1000,
  "MaxGpuMemoryMB": 6000,
  "BatchSize": 1,
  "DynamicBatching": true,
  "CreateBackups": true,
  "PreserveExistingTags": true,
  "WriteTags": true,
  "Recursive": false,
  "VerboseOutput": false
}
```

## Command-Line Options

### Global Options

- `-c, --config`: Path to the configuration file
- `-v, --verbose`: Verbose output

### Analyze Options

- `-f, --file`: Path to the MP3 file
- `-t, --test`: Test mode (don't write tags)
- `-n, --no-backup`: Don't create backups

### Batch Options

- `-d, --dir`: Path to the directory
- `-r, --recursive`: Process subdirectories
- `-t, --test`: Test mode (don't write tags)
- `-n, --no-backup`: Don't create backups

### Read Options

- `-f, --file`: Path to the MP3 file

### Restore Options

- `-f, --file`: Path to the MP3 file

### Config Options

- `--create`: Create default configuration
- `--show`: Show current configuration

## License

MIT

## Acknowledgements

- [NAudio](https://github.com/naudio/NAudio) for audio processing
- [TagLib#](https://github.com/mono/taglib-sharp) for MP3 tag handling
- [CommandLineParser](https://github.com/commandlineparser/commandline) for CLI parsing
- [Ollama](https://ollama.ai/) for AI model serving
