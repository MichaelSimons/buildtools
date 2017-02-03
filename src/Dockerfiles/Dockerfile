FROM microsoft/dotnet:1.0.3-sdk-msbuild-rc3

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        docker.io \
    && rm -rf /var/lib/apt/lists/*

WORKDIR ImageBuilder

COPY ImageBuilder.csproj .
RUN dotnet restore

COPY ./ImageBuilder .
RUN dotnet publish -c Release -o publish

WORKDIR Dockerfiles
COPY . .
