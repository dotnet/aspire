#:package Microsoft.Extensions.FileSystemGlobbing

using System.Collections.Concurrent;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return args.Length == 0 ? 1 : 0;
}

// Parse arguments
var paths = new List<string>();
var replacements = new List<(string Find, string Replace)>();
var currentMode = (string?)null;
var replacementBuffer = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];

    if (arg == "--files")
    {
        currentMode = "files";
        continue;
    }
    else if (arg == "--replacements")
    {
        currentMode = "replacements";
        continue;
    }

    if (currentMode == "files")
    {
        paths.Add(arg);
    }
    else if (currentMode == "replacements")
    {
        replacementBuffer.Add(arg);
    }
    else
    {
        Console.Error.WriteLine($"Error: Unexpected argument '{arg}'. Use --files or --replacements to specify argument type.");
        return 1;
    }
}

// Validate paths
if (paths.Count == 0)
{
    Console.Error.WriteLine("Error: No file paths provided. Use --files to specify paths.");
    PrintUsage();
    return 1;
}

// Validate and parse replacements
if (replacementBuffer.Count == 0)
{
    Console.Error.WriteLine("Error: No replacements provided. Use --replacements to specify find/replace pairs.");
    PrintUsage();
    return 1;
}

if (replacementBuffer.Count % 2 != 0)
{
    Console.Error.WriteLine($"Error: Replacement arguments must be provided in pairs (find, replace).");
    Console.Error.WriteLine($"       Received {replacementBuffer.Count} arguments after --replacements.");
    return 1;
}

for (var i = 0; i < replacementBuffer.Count; i += 2)
{
    var find = replacementBuffer[i];
    var replace = replacementBuffer[i + 1];

    if (string.IsNullOrEmpty(find))
    {
        Console.Error.WriteLine($"Error: Find text at position {i + 1} in --replacements cannot be empty.");
        return 1;
    }

    replacements.Add((find, replace));
}

Console.WriteLine($"Paths: {paths.Count}");
foreach (var path in paths)
{
    Console.WriteLine($"  '{path}'");
}
Console.WriteLine($"Replacements: {replacements.Count}");
foreach (var (find, replace) in replacements)
{
    Console.WriteLine($"  '{find}' -> '{replace}'");
}
Console.WriteLine();

var filesToProcess = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

var matcher = new Matcher();
var hasGlobPatterns = false;

foreach (var pathValue in paths)
{
    if (File.Exists(pathValue))
    {
        // If it's a direct file path add it as is
        filesToProcess.Add(Path.GetFullPath(pathValue));
    }
    else if (Directory.Exists(pathValue))
    {
        // If it's a directory, include all files within it
        matcher.AddInclude(Path.Combine(pathValue, "**/*"));
        hasGlobPatterns = true;
    }
    else if (pathValue.Contains('*') || pathValue.Contains('?'))
    {
        matcher.AddInclude(pathValue);
        hasGlobPatterns = true;
    }
}

var currentDirectory = Directory.GetCurrentDirectory();

if (hasGlobPatterns)
{
    // Collect files from glob matching
    var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(currentDirectory));
    var matchResult = matcher.Execute(directoryInfo);

    foreach (var file in matchResult.Files)
    {
        filesToProcess.Add(Path.GetFullPath(Path.Combine(currentDirectory, file.Path)));
    }
}

if (filesToProcess.Count == 0)
{
    Console.WriteLine("No files matched the provided paths.");
    return 0;
}

Console.WriteLine($"Found {filesToProcess.Count} file(s) matching the provided paths.");
Console.WriteLine();

var processedCount = 0;
var modifiedCount = 0;
var errorCount = 0;
var errors = new ConcurrentBag<(string File, string Error)>();

Parallel.ForEach(filesToProcess, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, filePath =>
{
    try
    {
        var content = File.ReadAllText(filePath);
        var originalContent = content;

        foreach (var (find, replace) in replacements)
        {
            content = content.Replace(find, replace, StringComparison.Ordinal);
        }

        if (!string.Equals(content, originalContent, StringComparison.Ordinal))
        {
            File.WriteAllText(filePath, content);
            Interlocked.Increment(ref modifiedCount);
            Console.WriteLine($"Modified: {Path.GetRelativePath(currentDirectory, filePath)}");
        }

        Interlocked.Increment(ref processedCount);
    }
    catch (Exception ex)
    {
        Interlocked.Increment(ref errorCount);
        errors.Add((filePath, ex.Message));
    }
});

Console.WriteLine();
Console.WriteLine($"Processed: {processedCount} file(s)");
Console.WriteLine($"Modified:  {modifiedCount} file(s)");

if (errorCount > 0)
{
    Console.WriteLine($"Errors:    {errorCount} file(s)");
    Console.WriteLine();
    Console.Error.WriteLine("Errors encountered:");
    foreach (var (file, error) in errors)
    {
        Console.Error.WriteLine($"  {Path.GetRelativePath(currentDirectory, file)}: {error}");
    }
    return 1;
}

return 0;

static void PrintUsage()
{
    Console.Error.WriteLine("""
        Usage: dotnet replace-text.cs --files <path1> [path2] ... --replacements <find1> <replace1> [<find2> <replace2> ...]

        Arguments:
          --files         One or more file paths, directory paths, or glob patterns
          --replacements  Pairs of find/replace text values

        Examples:
          dotnet replace-text.cs --files "./src/**/*.cs" "./src/**/*.csproj" --replacements "!!VERSION!!" "7.0.5" "!!MAJOR_MINOR!!" "7.0"
          dotnet replace-text.cs --files ./path/to/file.cs --replacements "oldText" "newText"
        """);
}

