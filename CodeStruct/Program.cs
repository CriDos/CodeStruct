using System;
using System.IO;
using System.Linq;
using System.Text;
using TextCopy;

namespace HardDev.CodeStruct
{
    public static class Program
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

                var outputBuilder = new StringBuilder();
                LoadAllowedExtensions();
                LoadIgnoredDirectories();
                GenerateDirectoryStructureToClipboard(workingDirectory, outputBuilder);
                ClipboardService.SetText(outputBuilder.ToString());

                Console.WriteLine("File structure has been successfully copied to clipboard!");
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

        private static void GenerateDirectoryStructureToClipboard(string directory, StringBuilder outputBuilder,
            string prefix = "")
        {
            var files = Directory.GetFiles(directory);
            var directories = Directory.GetDirectories(directory);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileExtension = fileInfo.Extension.ToLower();

                if (fileExtension.StartsWith("."))
                {
                    fileExtension = fileExtension[1..];
                }

                if (!_allowedFileExtensions.Contains(fileExtension))
                {
                    continue;
                }

                var relativePath = $"{prefix}{Path.GetFileName(file)}";
                var fileContent = File.ReadAllText(file);

                outputBuilder
                    .AppendLine(relativePath)
                    .AppendLine("```")
                    .AppendLine(fileContent)
                    .AppendLine("```");

                Console.WriteLine("Source find: " + relativePath);
            }

            foreach (var subDirectory in directories)
            {
                var directoryName = Path.GetFileName(subDirectory);

                if (_ignoredDirectories.Contains(directoryName))
                {
                    continue;
                }

                var newPrefix = $"{prefix}{directoryName}/";
                GenerateDirectoryStructureToClipboard(subDirectory, outputBuilder, newPrefix);
            }
        }
    }
}