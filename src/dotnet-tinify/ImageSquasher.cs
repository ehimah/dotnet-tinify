using Humanizer;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TinifyAPI;
using System.Drawing;

namespace ImageOptimiser
{
    public class ImageSquasher
    {        
        public IConsole Console { get; }

        public ImageSquasher(IConsole console) => Console = console;

        public async Task SquashFileAsync(IEnumerable<KeyValuePair<string, string>> filesToSquash)
        {
            if (filesToSquash is null) throw new ArgumentNullException(nameof(filesToSquash));            

            var compressFileTasks = filesToSquash
                .Where(file => Constants.SupportedExtensions.Contains(Path.GetExtension(file.Key)))
                .Select(file => CompressFileAsync(file.Key,file.Value));

            await Task.WhenAll(compressFileTasks);
        }
        
        async Task CompressFileAsync(string file, string target)
        {
            try
            {
                //check if target file already exists as a non-empty file
                var targetInfo = new FileInfo(target);
                if (targetInfo.Length > 0) return;
                
                

                var originalSizeInBytes = new FileInfo(file).Length;
                //return for empty file
                if (originalSizeInBytes == 0) return;

                var image = Image.FromFile(file);
                
                var source = Tinify.FromFile(file);
                var resized = source.Resize(new
                {
                    method = "scale",
                    width = Math.Min(image.Width, 1920)
                });

                if (!File.Exists(target)) {
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Create(target);
                }

                Console.WriteLine($"Compressing {Path.GetFileName(file)}...");
                await resized.ToFile(target);

                var newSizeInBytes = new FileInfo(target).Length;
                var percentChange = (newSizeInBytes - originalSizeInBytes) * 100.0 / originalSizeInBytes;

                Console.WriteLine($"Compression complete. {Path.GetFileName(file)} was {originalSizeInBytes.Bytes()}, now {newSizeInBytes.Bytes()} (-{percentChange:0}%)");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"An error occurred compressing {Path.GetFileName(file)}: ");
                Console.WriteLine(ex);
                //clean up already created file when an error occurs
                if (!File.Exists(target))
                {
                    File.Delete(target);
                    //check if folder is empty and delete
                    if(!Directory.EnumerateFileSystemEntries(target).Any())
                    {
                        Directory.Delete(Path.GetDirectoryName(target));
                    }
                }
            }
        }
    }
}
