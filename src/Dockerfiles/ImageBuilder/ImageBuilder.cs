using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ImageBuilder
{
    public class ImageBuilder
    {
        private static bool s_isPushEnabled = false;

        public static int Main(string[] args)
        {
            try
            {
                ParseArgs(args);

                // Get the information about the Dockerfiles to build
                Dictionary<string, ImageInfo> images = Directory.GetFiles(Config.BuildRoot, "Dockerfile", SearchOption.AllDirectories)
                    .Select(dockerfilePath => ImageInfo.Create(dockerfilePath))
                    .ToDictionary(info => info.Name);

                BuildImages(images);
                PushImages(images.Values);

                Console.WriteLine($"{Environment.NewLine}IMAGES BUILT");
                foreach (ImageInfo imageInfo in images.Values)
                {
                    Console.WriteLine(imageInfo.FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }

            return 0;
        }

        private static void BuildImages(Dictionary<string, ImageInfo> images)
        {
            // Build the images in the correct dependency order
            foreach (ImageInfo imageInfo in images.Values.TopologicalSort((info) => GetDependencies(info, images)))
            {
                Console.WriteLine($"---- Building {imageInfo.FullName} ----");

                string dockerfile = File.ReadAllText(imageInfo.DockerfilePath);
                if (imageInfo.HasRepoDependency)
                {
                    // The FROM image is within the list of images getting built.  Update the FROM to be the timestamped name.
                    dockerfile = ImageInfo.FromRegex.Replace(dockerfile, $"FROM {images[imageInfo.FromImage].FullName}");
                }

                ImageBuilder.BuildImage(dockerfile, imageInfo);
            }
        }

        private static void BuildImage(string dockerfile, ImageInfo imageInfo)
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

        private static void ParseArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--Push", StringComparison.OrdinalIgnoreCase))
                {
                    s_isPushEnabled = true;
                }
                else
                {
                    throw new InvalidOperationException($"Unrecognized argument '{arg}'");
                }
            }
        }

        private static void PushImages(IEnumerable<ImageInfo> images)
        {
            if (s_isPushEnabled)
            {
                foreach (ImageInfo imageInfo in images)
                {
                    Console.WriteLine($"---- Pushing {imageInfo.FullName} ----");

                    Process pushProcess = Process.Start("docker", $"push {imageInfo.FullName}");
                    pushProcess.WaitForExit();

                    if (pushProcess.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Failed to push {imageInfo.DockerfilePath}");
                    }
                }
            }
        }
    }
}
