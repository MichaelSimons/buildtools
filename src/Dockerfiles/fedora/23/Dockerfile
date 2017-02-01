FROM fedora:23
MAINTAINER Matt Ellis <matell@microsoft.com>

# Install the base toolchain we need to build anything (clang, cmake, make and the like)
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN dnf install -y cmake \
        clang \
        lldb-devel \
        make \
        which && \
    dnf clean all

# Install tools used by the VSO build automation.
RUN dnf install -y git \
        zip \
        tar \
        nodejs \
        npm && \
    dnf clean all && \
    npm install -g azure-cli && \
    npm cache clean

# Dependencies of CoreCLR and CoreFX.
RUN dnf install -y libicu-devel \
        libuuid-devel \
        libcurl-devel \
        openssl-devel \
        libunwind-devel \
        lttng-ust-devel && \
    dnf clean all
