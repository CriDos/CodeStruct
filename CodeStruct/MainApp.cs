using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TextCopy;

namespace HardDev.CodeStruct
{
    public static class MainApp
    {
        private static string[] _allowedFileExtensions;
        private static string[] _ignoredDirectories;

        private const string DefaultExtensions =
            "c, h, cpp, hpp, cs, csproj, cshtml, csx, csharp, vb, java, kotlin, py, php, js, ts, html, css, go, ruby, pl, r, groovy, swift, asm, bat, cmd, ps1";

        private const string DefaultIgnoredDirectories =
            "node_modules, .git, .svn, .run, .idea, bin, obj, .vs, .vscode, .metadata, .recommenders, .settings, .angular, .keep, .venv, .virtualenv, _builds, _notes, Build, Debug, release, tmp, temp";

        public static void Main()
        {
            try
            {
                var workingDirectory = Directory.GetCurrentDirectory();
                Console.WriteLine("Working directory: " + workingDirectory);

                LoadAllowedExtensions();
                LoadIgnoredDirectories();

                var output = GenerateCodeStructure(workingDirectory);

                Console.WriteLine("Code structure has been successfully generated!");

                ClipboardService.SetText(output);

                Console.WriteLine(output);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        private static void LoadAllowedExtensions()
        {
            var allowedExtensionsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AllowedExtensions.txt");

            if (!File.Exists(allowedExtensionsFile))
            {
                File.WriteAllText(allowedExtensionsFile, DefaultExtensions);
            }

            var extensions = File.ReadAllText(allowedExtensionsFile)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLower());

            _allowedFileExtensions = extensions.ToArray();
        }

        private static void LoadIgnoredDirectories()
        {
            var ignoredDirectoriesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IgnoredDirectories.txt");

            if (!File.Exists(ignoredDirectoriesFile))
            {
                File.WriteAllText(ignoredDirectoriesFile, DefaultIgnoredDirectories);
            }

            var directories = File.ReadAllText(ignoredDirectoriesFile)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(dir => dir.Trim())
                .Where(dir => !string.IsNullOrWhiteSpace(dir));

            _ignoredDirectories = directories.ToArray();
        }

        private static string GenerateCodeStructure(string directory, string prefix = "")
        {
            var directoriesToProcess = new Queue<(string, string)>();
            directoriesToProcess.Enqueue((directory, prefix));

            var outputBuilder = new StringBuilder();
            while (directoriesToProcess.Count > 0)
            {
                var (currentDirectory, currentPrefix) = directoriesToProcess.Dequeue();

                foreach (var file in Directory.GetFiles(currentDirectory))
                {
                    var fileInfo = new FileInfo(file);
                    var fileExtension = fileInfo.Extension.TrimStart('.').ToLower();

                    if (_allowedFileExtensions.Contains(fileExtension))
                    {
                        var relativePath = $"{currentPrefix}{fileInfo.Name}";
                        var fileContent = File.ReadAllText(file);

                        outputBuilder
                            .AppendLine(relativePath)
                            .AppendLine("```")
                            .AppendLine(fileContent)
                            .AppendLine("```");

                        Console.WriteLine("Source find: " + relativePath);
                    }
                }

                foreach (var subDirectory in Directory.GetDirectories(currentDirectory))
                {
                    var directoryName = Path.GetFileName(subDirectory);

                    if (!_ignoredDirectories.Contains(directoryName))
                    {
                        directoriesToProcess.Enqueue((subDirectory, $"{currentPrefix}{directoryName}/"));
                    }
                }
            }

            return outputBuilder.ToString();
        }
    }
}

