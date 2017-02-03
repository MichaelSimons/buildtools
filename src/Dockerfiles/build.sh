#!/usr/bin/env bash

repo_root=$(git rev-parse --show-cdup)
script_path=$(pwd -P)
dotnet_pkg="dotnet-dev-ubuntu-x64"
dotnet_tools_version=$(cat $repo_root/.cliversion)
tools_dir=$script_path/tools
dotnet_path=$tools_dir/dotnetcli
dotnet_cmd=$dotnet_path/dotnet
dotnet_download_url="https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/${dotnet_pkg}.$DOTNET_SDK_VERSION.tar.gz"
#https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/${dotnet_tools_version}/${dotnet_pkg}.${dotnet_tools_version}.tar.gz"

echo "Installing dotnet cli..."
# curl has HTTPS CA trust-issues less often than wget, so lets try that first.
which curl > /dev/null 2> /dev/null
if [ $? -ne 0 ]; then
    wget -q -O $dotnet_path/dotnet.tar ${dotnet_download_url}
else
    curl --retry 10 -sSL --create-dirs -o $dotnet_path/dotnet.tar ${dotnet_download_url}
fi

tar -zxf $dotnet_path/dotnet.tar -C $dotnet_path

echo $dotnet_cmd
pushd ImageBuilder
$dotnet_cmd restore
$dotnet_cmd publish -o publish
$dotnet_cmd run publish/ImageBuilder.dll $*
popd
