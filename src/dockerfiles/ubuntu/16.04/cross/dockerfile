FROM chcosta/dotnetcore:ubuntu1604_coredeps_v1

# Install the base toolchain we need to build anything (clang, cmake, make and the like).
RUN apt-get update \
    && apt-get install -y wget \
    && echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | tee /etc/apt/sources.list.d/llvm.list \
    && wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | apt-key add - \
    && apt-get update \
    && apt-get install -y \
        binfmt-support \
        binutils-arm-linux-gnueabihf \
        clang-3.6 \
        cmake \
        debootstrap \
        lldb-3.6 \
        lldb-3.6-dev \
        llvm-3.6 \
        make \
        qemu \
        qemu-user-static \
    && rm -rf /var/lib/apt/lists/*
