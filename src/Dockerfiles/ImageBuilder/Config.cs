using System;
using System.IO;

namespace ImageBuilder
{
    public static class Config
    {
        public static string BuildRoot { get; } = Directory.GetCurrentDirectory();
        public static string DockerRegistry { get; } = Environment.GetEnvironmentVariable("docker.registry");
        public const string DockerRepo = "build-prereqs";
    }
}
