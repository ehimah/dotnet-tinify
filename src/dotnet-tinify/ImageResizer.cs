using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TinifyAPI;

namespace ImageOptimiser
{
    [Command(
          Name = "dotnet tinify",
          FullName = "dotnet-tinify",
          Description = "Uses the TinyPNG API to squash images",
          ExtendedHelpText = Constants.ExtendedHelpText)]
    [HelpOption]
    public partial class ImageResizer
    {
        [Required(ErrorMessage = "You must specify the path to a directory or file to compress")]
        [Argument(0, Name = "path", Description = "Path to the file or directory to squash")]
        [FileOrDirectoryExists]
        public string Path { get; }

        [Option(CommandOptionType.SingleValue, Description = "Your TinyPNG API key")]
        public string ApiKey { get; }

        [Required(ErrorMessage = "You must specify the path to the target directory where the squashed images will be stored")]
        [Argument(1, Name = "target", Description = "The path to the target directory where the squashed images will be stored")]
        public string TargetPath { get; }

        public async Task<int> OnExecute(CommandLineApplication app, IConsole console)
        {
            if (!await SetApiKeyAsync(console))
            {
                app.ShowHelp();
                return Program.ERROR;
            }

            var squasher = new ImageSquasher(console);
            await squasher.SquashFileAsync(GetFilesToSquash(console, Path, TargetPath));

            console.WriteLine($"Compression complete.");
            console.WriteLine($"{Tinify.CompressionCount} compressions this month");
            
            return Program.OK;
        }

        async Task<bool> SetApiKeyAsync(IConsole console)
        {
            try
            {
                var apiKey = string.IsNullOrEmpty(ApiKey)
                    ? Environment.GetEnvironmentVariable(Constants.ApiKeyEnvironmentVariable)
                    : ApiKey;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    console.Error.WriteLine("Error: No API Key provided");
                    return false;
                }

                Tinify.Key = apiKey;
                await Tinify.Validate();
                console.WriteLine("TinyPng API Key verified");
                return true;
            }
            catch (System.Exception ex)
            {
                console.Error.WriteLine("Validation of TinyPng API key failed.");
                console.Error.WriteLine(ex);
                return false;
            }
        }

        static KeyValuePair<string,string>[] GetFilesToSquash(IConsole console, string path, string targetPath)
        {
            console.WriteLine($"Checking '{path}'...");
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                console.WriteLine($"Path '{path}' is a directory, squashing all files in all directories, one level");
                var filePaths = new List<KeyValuePair<string, string>>();
                
                foreach (var dirPath in Directory.GetDirectories(path))
                {
                    foreach(var filePath in Directory.GetFiles(dirPath)) {
                        var parentDirectory = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(filePath));
                        var fileName = System.IO.Path.GetFileName(filePath);
                        var targetFilePath = System.IO.Path.Combine(targetPath, parentDirectory, fileName);
                        filePaths.Add(new KeyValuePair<string, string>(filePath, targetFilePath));
                    }
                }
                return filePaths.ToArray();
            }
            else
            {
                console.WriteLine($"Path '{path}' is a file");
                return null;
            }
        }

        
    }
}
