#! /usr/bin/env python3

import argparse
import json
import requests
import hashlib
import platform
from dataclasses import dataclass
from typing import Optional

class Channel:
    def __init__(self, kind: str) -> None:
        if kind == 'lts':
            self.is_lts = True
        if kind == 'sts':
            self.is_sts = True
        channel_parts = kind.split('.')
        if channel_parts.count == 2:
            self.is_channel_version = True
            self.channel_version = kind
        if channel_parts.count == 3:
            self.is_feature_band = True;
            self.channel_version = f"{channel_parts[0]}.{channel_parts[1]}"
            self.feature_band = int(f"{channel_parts[2][0]}00")
    
    def __getattr__(self, name: str):
        return self.__dict__[f"_{name}"]

    def __setattr__(self, name, value):
        self.__dict__[f"_{name}"] = value

    def __str__(self) -> str:
        if self.is_lts:
            return 'lts'
        if self.is_sts:
            return 'sts'
        if self.is_channel_version:
            return self.channel_version
        return f"{self.channel_version}.{self.feature_band.replace('0', 'x')}"

# TODO: platform.system() doesn't handle musl - need separate detection
def compute_platform_part(platform: str) -> str:
    lower = platform.lower()
    match lower:
        case "osx" | "freebsd" | "rhel.6" | "linux-musl" | "linux":
            return lower
        case "macos":
            return "osx"
        case "win" | "windows":
            return "win"

def compute_arch_part(architecture: str) -> str:
    match architecture:
        case "arm64" | "aarch64":
            return "arm64"
        case "s390x" | "ppc64le" | "loongarch64":
            return architecture
        case _:
            return "x64"

def compute_runtime_identifier(platform: str, architecture: str) -> str:
    platform_part = compute_platform_part(platform)
    arch_part = compute_arch_part(architecture)
    if platform_part is None or arch_part is None:
        raise Exception(f"Unable to compute runtime identifier for {platform}/{architecture}")
    return f"{platform_part}-{arch_part}"


@dataclass
class InstallRequest:
    channel: Channel
    runtime: Optional[str]
    architecture: str
    os: str

    @property
    def rid(self):
        return compute_runtime_identifier(self.os, self.architecture)

def download_releases_index() -> json:
    return requests.get("https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json", ).json()

def download_releases_for_channel(channel_data: object) -> list:
    return requests.get(channel_data["releases.json"]).json()['releases']

def pick_best_channel(channels: list, desired_channel: Channel) -> object:
    if desired_channel.is_lts:
        return next(chan for chan in channels if chan['release-type'] == 'lts')
    elif desired_channel.is_sts:
        return next(chan for chan in channels if chan['release-type'] == 'sts')
    elif desired_channel.is_channel_version or desired_channel.is_feature_band:
        return next(chan for chan in channels if chan['channel-version'] == desired_channel.channel_version)

def find_matching_sdk(sdks: list, feature_band: int):
    return next(sdk for sdk in sdks if int(sdk["version"].split('.')[2]) > feature_band)

def pick_best_release(releases: list, requested_version: Channel) -> json:
    if requested_version.is_lts or requested_version.is_sts or requested_version.is_channel_version:
        # take the latest version for any of these
        return releases[0]
    elif requested_version.is_feature_band:
        # find the first release that has an `sdk` in the feature band
        return next(r for r in releases if find_matching_sdk(r['sdks'], requested_version.feature_band))

def pick_matching_sdk(sdks: list, requested_version: Channel):
    if requested_version.is_lts or requested_version.is_sts or requested_version.is_channel_version:
        # take the latest version for any of these
        return sdks[0]
    elif requested_version.is_feature_band:
        # find the first release that has an `sdk` in the feature band
        return find_matching_sdk(sdks, requested_version.feature_band)

def pick_file_to_download(files: list, runtime_identifier: str):
    return next(file for file in files if file['rid'] == runtime_identifier and (file['name'].endswith('tar.gz') or file['name'].endswith('zip')))

def download_file(url: str, hash: str):
    local_filename = url.split('/')[-1]
    with requests.get(url, stream=True) as r:
        r.raise_for_status()
        with open(local_filename, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192): 
                f.write(chunk)
    
    BUF_SIZE = 65536  # lets read stuff in 64kb chunks!

    sha = hashlib.sha512()

    with open(local_filename, 'rb') as f:
        while True:
            data = f.read(BUF_SIZE)
            if not data:
                break
            sha.update(data)
    sha_digest = sha.hexdigest()

    if sha_digest != hash:
        raise Exception("File hash didn't match")

    return local_filename



def install_sdk(request: InstallRequest):
    index = download_releases_index()
    if index is None:
        raise Exception("Releases index could not be downloaded")
    channels: list = index["releases-index"]
    channel_information = pick_best_channel(channels, request.channel)
    channel_version = channel_information["channel-version"]
    if channel_information is None:
        raise Exception(f"No matching channel could be found for desired channel '{channel_version}'")
    channel_releases = download_releases_for_channel(channel_information)
    if channel_releases is None:
        raise Exception(f"No releases found for channel '{channel_version}'")
    release_information = pick_best_release(channel_releases, request.channel)
    if release_information is None:
        raise Exception(f"No best release found in channel '{channel_version}'")
    release_version = release_information['release-version']
    sdk_information = pick_matching_sdk(release_information['sdks'], request.channel)
    if sdk_information is None:
        raise Exception(f"No matching SDK found in release '{release_version}'")
    file_to_download = pick_file_to_download(sdk_information['files'], request.rid)
    if file_to_download is None:
        raise Exception(f"No matching file for runtime identifier {request.rid} found in release '{release_version}'")
    download_file(file_to_download['url'], file_to_download['hash'])

if __name__ == "__main__":
    parser = argparse.ArgumentParser("dotnet-install.py", description="%(prog)s ia a simple command line interface for obtaining the .NET SDK and Runtime", add_help=True, exit_on_error=True)
    parser.add_argument("--channel", "-c", type=Channel, help="The release grouping to download from. Can have a variety of formats: sts, lts, two-part version number (8.0) or three-part version number in major.minor.patchxx format (8.0.2xx)")
    parser.add_argument("--runtime", help="Installs a shared runtime only, without the SDK", choices=["dotnet", "aspnetcore"])
    parser.add_argument("--architecture", help="Architecture of the .NET binaries to be installed, defaults to the current system.", default=platform.machine())
    parser.add_argument("--os", help="Specifies the operating system to be used when selecting the installer", default=platform.system())
    try:
        args = parser.parse_args()
        request = InstallRequest(args.channel, args.runtime, args.architecture, args.os)
        install_sdk(request)
    except:
        parser.print_help()
        exit(1)
