using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageBuilder
{
    public class ImageBuilder
    {
        static void Main(string[] args)
        {
            // Get the information about the Dockerfiles to build
            Dictionary<string, ImageInfo> images = Directory.GetFiles(Config.BuildRoot, "Dockerfile", SearchOption.AllDirectories)
                .Select(dockerfilePath => ImageInfo.Create(dockerfilePath))
                .ToDictionary(info => info.Name);

            // Build the images in the correct dependency order
            StringBuilder buildSummary = new StringBuilder($"{Environment.NewLine}IMAGES BUILT{Environment.NewLine}");
            foreach (ImageInfo imageInfo in images.Values.TopologicalSort((info) => GetDependencies(info, images)))
            {
                Console.WriteLine($"---- Building {imageInfo.FullName} ----");
                buildSummary.AppendLine(imageInfo.FullName);

                string dockerfile = File.ReadAllText(imageInfo.DockerfilePath);
                if (imageInfo.HasRepoDependency)
                {
                    // The FROM image is within the list of images getting built.  Update the FROM to be the timestamped name.
                    dockerfile = ImageInfo.FromRegex.Replace(dockerfile, $"FROM {images[imageInfo.FromImage].FullName}");
                }

                ImageBuilder.Build(dockerfile, imageInfo);
            }

            Console.WriteLine(buildSummary);
        }

        private static void Build(string dockerfile, ImageInfo imageInfo)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("docker", $"build -t {imageInfo.FullName} -");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            Process buildProcess = Process.Start(startInfo);

            using (StreamWriter buildInput = buildProcess.StandardInput)
            {
                buildInput.Write(dockerfile);
            }

            buildProcess.WaitForExit();

            if (buildProcess.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to build {imageInfo.DockerfilePath}");
            }
        }

        private static IEnumerable<ImageInfo> GetDependencies(ImageInfo imageInfo, Dictionary<string, ImageInfo> images)
        {
            ImageInfo fromImage;
            if (images.TryGetValue(imageInfo.FromImage, out fromImage))
            {
                return new ImageInfo[] { fromImage };
            }

            return Enumerable.Empty<ImageInfo>();
        }
    }
}
