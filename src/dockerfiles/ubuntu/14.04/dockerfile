FROM ubuntu:14.04
MAINTAINER <matell@microsoft.com>


# Install the base toolchain we need to build anything (clang, cmake, make and the like)
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN apt-get update && \
    apt-get install -y wget && \
    echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | tee /etc/apt/sources.list.d/llvm.list && \
    wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | apt-key add - && \
    apt-get update && \
    apt-get install -y cmake \
            make \
            llvm-3.5 \
            clang-3.5 \
            lldb-3.6 \
            lldb-3.6-dev && \
    apt-get clean

# Install tools used by the VSO build automation.  nodejs-legacy is a Debian specific
# package that provides `node' on the path (which azure cli needs).
RUN apt-get install -y git \
            zip \
            tar \
            nodejs \
            nodejs-legacy \
            npm && \
    apt-get clean && \
    npm install -g azure-cli && \
    npm cache clean

# Dependencies for CoreCLR and CoreFX
RUN apt-get install -y gettext \
            libunwind8-dev \
            libunwind8 \
            libicu-dev \
            liblttng-ust-dev \
            libcurl4-openssl-dev \
            libssl-dev \
            uuid-dev && \
    apt-get clean
