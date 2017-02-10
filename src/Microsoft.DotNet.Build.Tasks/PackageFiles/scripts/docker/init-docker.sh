#!/usr/bin/env bash

# Stop script on NZEC
set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

showHelp() {
    echo "Usage: $scriptName [OPTIONS] IMAGE_NAME[:TAG|@DIGEST]"
    echo
    echo "Initializes Docker by:"
    echo "  - Emitting the version of Docker that is being used"
    echo "  - Cleaning up any containers and images that exist on the machine"
    echo "  - Ensuring the latest copy of the specified image exists on the machine"
    echo
    echo "Options:"
    echo "  -r, --retry-count"
    echo "  -w, --wait-factor"

    exit 1
}

# Executes a command and retry up to 5 times if it fails.
execute() {
    local count=0
    until "$@"; do
      count=$(($count + 1))
      if [ $count -lt $retries ]; then
        local wait=$((waitFactor ** (count - 1))
        echo "Retry $count/$retries exited $exit, retrying in $wait seconds..."
        sleep $wait
      else
        local exit=$?
        echo "Retry $count/$retries exited $exit, no more retries left."
        return $exit
      fi
    done

    return 0
}

scriptName=$0
retries=5
waitFactor=4

while [ $# -ne 0 ]
do
    name=$1
    case $name in
        -h|--help)
            shift
            showHelp
            exit 0
            ;;
        -r|--retry-count)
            shift
            $retries=$1
            ;;
        -w|--wait-factor)
            shift
            $waitFactor="$1"
            ;;
        *)
            say_err "Unknown argument \`$name\`"
            exit 1
            ;;
    esac

    shift
done

# Capture Docker version for diagnostic purposes
docker --version
echo

echo "Cleaning Docker Artifacts"
./cleanup-docker.sh
echo

image=$1
echo "Pulling Docker image $image"
execute docker pull $image
