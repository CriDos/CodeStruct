using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TextCopy;

namespace HardDev.CodeStruct
{
    public static class MainApp
    {
        private static string[] s_allowedFileExtensions;
        private static string[] s_ignoredDirectories;

        private const string DefaultExtensions =
            "c, h, cpp, hpp, cs, csproj, cshtml, csx, csharp, vb, java, kotlin, py, php, js, ts, html, css, go, ruby, pl, r, groovy, swift, asm, bat, cmd, ps1";

        private const string DefaultIgnoredDirectories =
            "node_modules, .git, .svn, .run, .idea, bin, obj, .vs, .vscode, .metadata, .recommenders, .settings, .angular, .keep, .venv, .virtualenv, _builds, _notes, Build, Debug, release, tmp, temp";

        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"CodeStruct {Assembly.GetEntryAssembly()?.GetName().Version}");

                string workingDirectory = Directory.GetCurrentDirectory();
                Console.WriteLine("Working directory: " + workingDirectory);

                LoadAllowedExtensions();
                LoadIgnoredDirectories();

                bool cleanupContent = args.Contains("-cl");
                Console.WriteLine("Cleanup content: " + cleanupContent);

                string output = GenerateCodeStructure(workingDirectory, cleanupContent);

                Console.WriteLine("Code structure has been successfully generated!");

                if (args.Contains("-c"))
                {
                    Console.WriteLine("Writing to console...");
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine("Copying to clipboard...");
                    ClipboardService.SetText(output);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        private static void LoadAllowedExtensions()
        {
            string allowedExtensionsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllowedExtensions.txt");

            if (!File.Exists(allowedExtensionsFile))
            {
                File.WriteAllText(allowedExtensionsFile, DefaultExtensions);
            }

            var extensions = File.ReadAllText(allowedExtensionsFile)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLower());

            s_allowedFileExtensions = extensions.ToArray();
        }

        private static void LoadIgnoredDirectories()
        {
            string ignoredDirectoriesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IgnoredDirectories.txt");

            if (!File.Exists(ignoredDirectoriesFile))
            {
                File.WriteAllText(ignoredDirectoriesFile, DefaultIgnoredDirectories);
            }

            var directories = File.ReadAllText(ignoredDirectoriesFile)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(dir => dir.Trim())
                .Where(dir => !string.IsNullOrWhiteSpace(dir));

            s_ignoredDirectories = directories.ToArray();
        }

        private static string GenerateCodeStructure(string directory, bool cleanupContent, string prefix = "")
        {
            var directoriesToProcess = new Queue<(string, string)>();
            directoriesToProcess.Enqueue((directory, prefix));

            var outputBuilder = new StringBuilder();
            while (directoriesToProcess.Count > 0)
            {
                (string currentDirectory, string currentPrefix) = directoriesToProcess.Dequeue();

                foreach (string file in Directory.GetFiles(currentDirectory))
                {
                    var fileInfo = new FileInfo(file);
                    string fileExtension = fileInfo.Extension.TrimStart('.').ToLower();

                    if (s_allowedFileExtensions.Contains(fileExtension))
                    {
                        string relativePath = $"{currentPrefix}{fileInfo.Name}";
                        string fileContent = File.ReadAllText(file);

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

                foreach (string subDirectory in Directory.GetDirectories(currentDirectory))
                {
                    string directoryName = Path.GetFileName(subDirectory);

                    if (!s_ignoredDirectories.Contains(directoryName))
                    {
                        directoriesToProcess.Enqueue((subDirectory, $"{currentPrefix}{directoryName}/"));
                    }
                }
            }

            return outputBuilder.ToString();
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

                if (!inMultiLineComment && !inSingleLineComment)
                {
                    if (c == '\"' && !inChar)
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
                }
                else if (inMultiLineComment)
                {
                    if (c == '*' && nextChar == '/')
                    {
                        inMultiLineComment = false;
                        i++;
                        continue;
                    }
                }
                else if (c is '\n' or '\r')
                {
                    inSingleLineComment = false;
                }

                if (!inMultiLineComment && !inSingleLineComment)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
