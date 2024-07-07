using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using HardDev.CodeStruct.Configs;
using HardDev.CoreUtils.Config;
using HardDev.CoreUtils.Logging;
using Serilog;
using TextCopy;

namespace HardDev.CodeStruct;

public static class MainApp
{
    private static readonly ILogger s_logger = AppLogger.Build(new LoggerConfig { EnableFile = false });

    public static async Task Main(string[] args)
    {
        try
        {
            s_logger.Information($"CodeStruct {Assembly.GetEntryAssembly()?.GetName().Version}");

            DisplayUsageAndConfigInfo();

            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(RunAsync);
        }
        catch (Exception e)
        {
            s_logger.Error(e, "An error occurred in the main execution");
        }
    }

    private static void DisplayUsageAndConfigInfo()
    {
        Console.WriteLine("Usage information:");
        Console.WriteLine("  -c, --console     Output to console instead of clipboard");
        Console.WriteLine("  --cl              Clean up file content");
        Console.WriteLine("  -s, --structure   Generate only directory structure");
        Console.WriteLine("  --set-path        Set CodeStruct path in system environment variables");
        Console.WriteLine("\nConfiguration options:");

        var config = AppConfig.GetOrLoad<CodeStructConfig>(out bool loaded);
        if (!loaded)
        {
            config.Save();
        }

        Console.WriteLine($"  Allowed extensions: {string.Join(", ", config.AllowedExtensions)}");
        Console.WriteLine($"  Ignored directories: {string.Join(", ", config.IgnoredDirectories)}");
        Console.WriteLine($"\nConfiguration file: {config.ConfigPath}");
        Console.WriteLine();
    }

    private static async Task RunAsync(Options opts)
    {
        if (opts.SetPath)
        {
            bool pathSet = SetEnvironmentPath();
            if (pathSet)
            {
                Environment.Exit(0);
            }

            return;
        }

        string workingDirectory = Directory.GetCurrentDirectory();
        s_logger.Information("Working directory: {Directory}", workingDirectory);

        var config = AppConfig.GetOrLoad<CodeStructConfig>(out bool loaded);
        if (!loaded)
        {
            config.Save();
        }

        s_logger.Information("Cleanup content: {CleanupContent}", opts.CleanupContent);
        s_logger.Information("Generate structure only: {GenerateStructureOnly}", opts.GenerateStructureOnly);

        string output;
        if (opts.GenerateStructureOnly)
        {
            output = GenerateDirectoryStructure(workingDirectory, config.IgnoredDirectories);
        }
        else
        {
            output = await GenerateCodeStructureAsync(workingDirectory, opts.CleanupContent, config.AllowedExtensions,
                config.IgnoredDirectories);
        }

        s_logger.Information("Structure has been successfully generated!");

        if (opts.OutputToConsole)
        {
            s_logger.Information("Writing to console...");
            Console.WriteLine(output);
        }
        else
        {
            s_logger.Information("Copying to clipboard...");
            await ClipboardService.SetTextAsync(output);
        }
    }

    private static bool SetEnvironmentPath()
    {
        try
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            string pathVariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;

            if (!pathVariable.Contains(executablePath))
            {
                string newPath = $"{pathVariable};{executablePath}";
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                Console.WriteLine("CodeStruct path has been added to the system PATH variable.");
                return true;
            }

            Console.WriteLine("CodeStruct path is already in the system PATH variable.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set environment path: {ex.Message}");
            return false;
        }
    }

    private static string GenerateDirectoryStructure(string directory, string[] ignoredDirectories)
    {
        var sb = new StringBuilder();
        GenerateDirectoryStructureRecursive(directory, "", ignoredDirectories, sb);
        return sb.ToString();
    }

    private static void GenerateDirectoryStructureRecursive(string directory, string indent, string[] ignoredDirectories, StringBuilder sb)
    {
        var dirInfo = new DirectoryInfo(directory);
        sb.AppendLine($"{indent}{dirInfo.Name}/");

        foreach (var file in dirInfo.GetFiles())
        {
            sb.AppendLine($"{indent}  {file.Name}");
        }

        foreach (var subDir in dirInfo.GetDirectories())
        {
            if (!ignoredDirectories.Contains(subDir.Name))
            {
                GenerateDirectoryStructureRecursive(subDir.FullName, indent + "  ", ignoredDirectories, sb);
            }
        }
    }

    private static async Task<string> GenerateCodeStructureAsync(string directory, bool cleanupContent, string[] allowedExtensions,
        string[] ignoredDirectories)
    {
        var directoriesToProcess = new Queue<(string, string)>();
        directoriesToProcess.Enqueue((directory, ""));

        var outputBuilder = new StringBuilder();
        while (directoriesToProcess.Count > 0)
        {
            (string currentDirectory, string currentPrefix) = directoriesToProcess.Dequeue();

            await ProcessFilesInDirectoryAsync(currentDirectory, currentPrefix, cleanupContent, allowedExtensions, outputBuilder);
            EnqueueSubdirectories(currentDirectory, currentPrefix, ignoredDirectories, directoriesToProcess);
        }

        return outputBuilder.ToString();
    }

    private static async Task ProcessFilesInDirectoryAsync(string directory, string prefix, bool cleanupContent,
        string[] allowedExtensions, StringBuilder outputBuilder)
    {
        foreach (string file in Directory.GetFiles(directory))
        {
            var fileInfo = new FileInfo(file);
            string fileExtension = fileInfo.Extension.TrimStart('.').ToLower();

            if (allowedExtensions.Contains(fileExtension))
            {
                string relativePath = $"{prefix}{fileInfo.Name}";
                string fileContent = await File.ReadAllTextAsync(file);

                if (cleanupContent)
                {
                    fileContent = CleanupFileContent(fileContent);
                }

                outputBuilder
                    .AppendLine(relativePath)
                    .AppendLine("```")
                    .AppendLine(fileContent)
                    .AppendLine("```");
            }
        }
    }

    private static void EnqueueSubdirectories(string directory, string prefix, string[] ignoredDirectories,
        Queue<(string, string)> directoriesToProcess)
    {
        foreach (string subDirectory in Directory.GetDirectories(directory))
        {
            string directoryName = Path.GetFileName(subDirectory);

            if (!ignoredDirectories.Contains(directoryName))
            {
                directoriesToProcess.Enqueue((subDirectory, $"{prefix}{directoryName}/"));
            }
        }
    }

    private static string CleanupFileContent(string fileContent)
    {
        string cleanedContent = RemoveComments(fileContent);

        cleanedContent = cleanedContent.Replace("\t", " ");
        cleanedContent = cleanedContent.Replace("\r", " ").Replace("\n", " ");
        cleanedContent = Regex.Replace(cleanedContent, @"\s+", " ");
        cleanedContent = cleanedContent.Trim();

        return cleanedContent;
    }


    private static string RemoveComments(string fileContent)
    {
        var sb = new StringBuilder();
        bool inString = false;
        bool inChar = false;
        bool inMultiLineComment = false;
        bool inSingleLineComment = false;

        for (int i = 0; i < fileContent.Length; i++)
        {
            char c = fileContent[i];
            char nextChar = i + 1 < fileContent.Length ? fileContent[i + 1] : '\0';

            switch (inMultiLineComment)
            {
                case false when !inSingleLineComment:
                {
                    if (c == '"' && !inChar)
                    {
                        inString = !inString;
                    }
                    else if (c == '\'' && !inString)
                    {
                        inChar = !inChar;
                    }
                    else if (c == '/' && nextChar == '*' && !inString && !inChar)
                    {
                        inMultiLineComment = true;
                        i++;
                        continue;
                    }
                    else if (c == '/' && nextChar == '/' && !inString && !inChar)
                    {
                        inSingleLineComment = true;
                        i++;
                        continue;
                    }

                    break;
                }
                case true:
                {
                    if (c == '*' && nextChar == '/')
                    {
                        inMultiLineComment = false;
                        i++;
                        continue;
                    }

                    break;
                }
                default:
                {
                    if (c is '\n' or '\r')
                    {
                        inSingleLineComment = false;
                    }

                    break;
                }
            }

            if (!inMultiLineComment && !inSingleLineComment)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
