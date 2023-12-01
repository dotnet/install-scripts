#!/usr/bin/env bash

# System must first have curl installed.
# The following command will download the installation script and run it.
# curl -L https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/install-dotnet-preview.sh -o install-dotnet-preview.sh && bash install-dotnet-preview.sh
# The script will
#   - install any additional dependences needed for the script to continue
#   - download a tar.gz containing the .NET preview installer packages to the current directory
#   - expand the tar.gz into ./dotnet_packages
#   - download the appropriate runtime dependency installer package into ./dotnet_packages
#   - install the contents of ./dotnet_packages using rpm or dpkg as appropriate


if [ -n "$(command -v lsb_release)" ]; then
    DISTRO_NAME=$(lsb_release -s -d)
elif [ -f "/etc/os-release" ]; then
    DISTRO_NAME=$(grep PRETTY_NAME /etc/os-release | sed 's/PRETTY_NAME=//g' | tr -d '="')
elif [ -f "/etc/debian_version" ]; then
    DISTRO_NAME="Debian $(cat /etc/debian_version)"
elif [ -f "/etc/redhat-release" ]; then
    DISTRO_NAME=$(cat /etc/redhat-release)
else
    DISTRO_NAME="$(uname -s) $(uname -r)"
fi

PACKAGE_TYPE=""
DEPS_PACKAGE=""
DOWNLOAD_DIR=$PWD
DOTNET_PACKAGE_DIR="dotnet_packages"
SUPPORTED_DISTRO=1

DEPS_BUILD="23479.6"
PREVIEW_NUMBER="2"

declare -a ADDITIONAL_DEPS

function distro_check()
{
    case $DISTRO_NAME in
        *"Debian"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 16.04"* | *"Mint 18"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 18.04"* | *"Mint 19"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 19.10"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 20.04"* | *"Focal"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 20.10"* | *"Groovy"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 22.04"* | *"Jammy"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 22.10"* | *"Kinetic"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Ubuntu 23.04"* | *"Lunar"*)
          PACKAGE_TYPE="deb"
          ADDITIONAL_DEPS=("tar" "gzip" "curl")
          ;;
        *"Fedora"* | *"Red Hat"*)
          PACKAGE_TYPE="rpm"
          DEPS_PACKAGE="https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/dotnet-runtime-deps-8.0.0-rc.$PREVIEW_NUMBER.$DEPS_BUILD-fedora.34-x64.rpm"
          ADDITIONAL_DEPS=("tar" "gzip" "compat-openssl10" "libicu")
          ;;
        *"openSUSE"*)
          PACKAGE_TYPE="rpm"
          DEPS_PACKAGE="https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/dotnet-runtime-deps-8.0.0-rc.$PREVIEW_NUMBER.$DEPS_BUILD-opensuse.42-x64.rpm"
          ADDITIONAL_DEPS=("tar" "gzip" "libopenssl1_0_0" "libicu")
          ;;
        *"sles"**)
          PACKAGE_TYPE="rpm"
          DEPS_PACKAGE="https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/dotnet-runtime-deps-8.0.0-rc.$PREVIEW_NUMBER.$DEPS_BUILD-sles.12-x64.rpm"
          ADDITIONAL_DEPS=("tar" "gzip" "libopenssl1_0_0" "libicu")
          ;;
        *"Oracle"*)
          PACKAGE_TYPE="rpm"
          DEPS_PACKAGE="https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/dotnet-runtime-deps-8.0.0-rc.$PREVIEW_NUMBER.$DEPS_BUILD-oraclelinux.8-x64.rpm"
          ADDITIONAL_DEPS=("tar" "gzip" "libicu")
          ;;
        *"CentOS"*)
          PACKAGE_TYPE="rpm"
          DEPS_PACKAGE="https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/dotnet-runtime-deps-8.0.0-rc.$PREVIEW_NUMBER.$DEPS_BUILD-centos.7-x64.rpm"
          ADDITIONAL_DEPS=("tar" "gzip" "libicu")
          ;;
        *) SUPPORTED_DISTRO=0 ;;
    esac
}

function download_preview()
{
    case $PACKAGE_TYPE in
        "rpm")
            echo "*** Setting package type to rpm."
            DOTNET_SRC="dotnet-8.0.0-rc.$PREVIEW_NUMBER-rpm.tar.gz"
            ;;
        "deb")
            echo "*** Setting package type to deb."
            DOTNET_SRC="dotnet-8.0.0-rc.$PREVIEW_NUMBER-deb.tar.gz"
            ;;
        *)
    esac

    echo "*** Download source: ${DOTNET_SRC}"
    echo
    echo "*** Downloading https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/$DOTNET_SRC to $DOWNLOAD_DIR ..."

    curl "https://dotnetcli.blob.core.windows.net/dotnet/release/install-preview/8.0.0-rc.$PREVIEW_NUMBER/"$DOTNET_SRC -o $DOWNLOAD_DIR/$DOTNET_SRC
    
    echo
    echo "*** Unpacking ${DOTNET_SRC} ..."
    echo
    tar xvf $DOTNET_SRC -C $DOWNLOAD_DIR
    echo
    
    if [ $PACKAGE_TYPE == "rpm" ]
    then
        echo "*** Downloading $DEPS_PACKAGE"
        curl $DEPS_PACKAGE -o $DOWNLOAD_DIR/$DOTNET_PACKAGE_DIR/dotnet-runtime-deps.rpm
    fi
}


function check_dependencies()
{
    for dependency in "${ADDITIONAL_DEPS[@]}"
    do

        if [ $PACKAGE_TYPE == "rpm" ]
        then
            dep_found=$(rpm -qa | grep ${dependency} | wc -c )
        elif [ $PACKAGE_TYPE == "deb" ]
            then
            dep_found=$(dpkg --get-selections | grep "^${dependency}" | wc -c )
        fi
        
        if [ $dep_found == 0 ]
        then
            echo
            echo "***     Installing ${dependency} ..."
            echo
            case "$1" in
                *"openSUSE"* | *"SLES"* )
                    zypper install -y ${dependency}
                    ;;
                *"Fedora"*)
                    dnf install -y ${dependency}
                    ;;
                *"CentOS"* | *"Oracle"*)
                    yum install -y ${dependency}
                    ;;
                *"Debian"* | *"Ubuntu"* | *"Mint"*)
                    apt install -y ${dependency}
                    ;;
                *)
            esac
            echo
        else
            echo "***     ${dependency} is already installed."
        fi
    done
}

function install()
{
    if [ $PACKAGE_TYPE == "rpm" ]
    then
        rpm -ivh --replacepkgs $DOWNLOAD_DIR/$DOTNET_PACKAGE_DIR/*
    elif [ $PACKAGE_TYPE == "deb" ]
    then
        apt install -y $DOWNLOAD_DIR/$DOTNET_PACKAGE_DIR/*
    fi
}

#-----------------------------------#

distro_check
echo ${SUPPORTED_DISTRO}
if [ ${SUPPORTED_DISTRO} == 1 ]
then
    echo
    echo "*** Checking required system dependencies for detected OS: ${DISTRO_NAME} ..."
    check_dependencies $DISTRO_NAME
    echo
    download_preview
    echo
    install
else
    echo "${DISTRO_NAME} is not supported by the .NET 5 Preview installer."
fi
