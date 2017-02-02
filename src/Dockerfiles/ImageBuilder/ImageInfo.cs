using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ImageBuilder
{
    public class ImageInfo
    {
        private ImageInfo()
        {
        }

        public string DockerfilePath { get; private set; }
        public string FromImage { get; private set; }
        public static Regex FromRegex { get; } = new Regex("FROM\\s+(?<fromImage>\\S+)");
        public string FullName {get; private set;}
        public bool HasRepoDependency { get; private set; }
        public string Name { get; private set; }
        public string ParentDirectory {get; private set;}

        public static ImageInfo Create(string dockerfilePath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(dockerfilePath));

            ImageInfo info = new ImageInfo();
            info.DockerfilePath = dockerfilePath;
            info.ParentDirectory = Path.GetDirectoryName(dockerfilePath);
            string baseTag = info.GetBaseTag();
            info.FromImage = info.GetFromImage();
            info.HasRepoDependency = info.FromImage.StartsWith($"{Config.DockerRepo}:");
            info.Name = $"{Config.DockerRepo}:{baseTag}";
            info.FullName = info.GetFullName();

            return info;
        }

        /// <summary>
        /// Gets the base part of the tag as determined from the directory the Dockerfile is located within.
        /// </summary>
        private string GetBaseTag()
        {
            return ParentDirectory.Substring(Config.BuildRoot.Length + 1)
                .Replace(Path.DirectorySeparatorChar, '-');
        }

        private string GetFromImage()
        {
            Match fromMatch = FromRegex.Match(File.ReadAllText(DockerfilePath));
            if (!fromMatch.Success)
            {
                throw new ArgumentException($"Unable to find the FROM image in {DockerfilePath}.");
            }

            return fromMatch.Groups["fromImage"].Value;
        }

        private string GetFullName()
        {
            string registry = null;
            if (!string.IsNullOrWhiteSpace(Config.DockerRegistry))
            {
                registry = Config.DockerRegistry + ":";
            }

            return registry + Name + GetCommitTimeStamp();
        }

        /// <summary>
        /// Retrieves the commit timestamp of the last change to the Dockerfile.
        /// Format: -{CondensedSHA}-{YYYYMMDDhhmmss} e.g. -8b883c3-20170201183549
        /// </summary>
        private string GetCommitTimeStamp ()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("git", $"log -1 --format=format:-%h-%ad --date=format:%Y%m%d%H%M%S {DockerfilePath}");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            Process gitLogProcess = Process.Start(startInfo);
            gitLogProcess.WaitForExit();

            if (gitLogProcess.ExitCode != 0)
            {
                throw new ArgumentException($"Unable to retrieve the commit timestamp for {DockerfilePath}.");
            }

            return gitLogProcess.StandardOutput.ReadToEnd();
        }
    }
}