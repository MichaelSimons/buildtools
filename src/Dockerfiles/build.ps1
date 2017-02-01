[cmdletbinding()]
param(
    [string]$DockerRepo = "build-prereqs",
    [switch]$UseImageCache
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$dirSeparator = [IO.Path]::DirectorySeparatorChar

if ($UseImageCache) {
    $optionalDockerBuildArgs = ""
}
else {
    $optionalDockerBuildArgs = "--no-cache"
}

Get-ChildItem -Recurse -Filter "Dockerfile" |
    sort DirectoryName |
    foreach {
        $commitTimeStamp = git log -1 --format=format:%h-%ad --date=format:%Y%m%d%H%M%S $_.FullName
        Write-Host $_.FullName $commitTimeStamp
        $tag = "$($DockerRepo):" +
            $_.DirectoryName.
            Replace("$pwd$dirSeparator", '').
            Replace($dirSeparator, '-') +
            $commitTimeStamp

        Write-Host "--- Building $tag from $($_.DirectoryName) ---"
        #docker build $optionalDockerBuildArgs -t $tag $_.DirectoryName
        if (-NOT $?) {
            throw "Failed building $tag"
        }
    }
