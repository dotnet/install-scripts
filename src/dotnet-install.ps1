#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

<#
.SYNOPSIS
    Installs dotnet cli
.DESCRIPTION
    Installs dotnet cli. If dotnet installation already exists in the given directory
    it will update it only if the requested version differs from the one already installed.

    Note that the intended use of this script is for Continuous Integration (CI) scenarios, where:
    - The SDK needs to be installed without user interaction and without admin rights.
    - The SDK installation doesn't need to persist across multiple CI runs.
    To set up a development environment or to run apps, use installers rather than this script. Visit https://dotnet.microsoft.com/download to get the installer.

.PARAMETER Channel
    Default: LTS
    Download from the Channel specified. Possible values:
    - STS - the most recent Standard Term Support release
    - LTS - the most recent Long Term Support release
    - 2-part version in a format A.B - represents a specific release
          examples: 2.0, 1.0
    - 3-part version in a format A.B.Cxx - represents a specific SDK release
          examples: 5.0.1xx, 5.0.2xx
          Supported since 5.0 release
    Warning: Value "Current" is deprecated for the Channel parameter. Use "STS" instead.
    Note: The version parameter overrides the channel parameter when any version other than 'latest' is used.
.PARAMETER Quality
    Download the latest build of specified quality in the channel. The possible values are: daily, signed, validated, preview, GA.
    Works only in combination with channel. Not applicable for STS and LTS channels and will be ignored if those channels are used. 
    For SDK use channel in A.B.Cxx format: using quality together with channel in A.B format is not supported.
    Supported since 5.0 release.
    Note: The version parameter overrides the channel parameter when any version other than 'latest' is used, and therefore overrides the quality.     
.PARAMETER Version
    Default: latest
    Represents a build version on specific channel. Possible values:
    - latest - the latest build on specific channel
    - 3-part version in a format A.B.C - represents specific version of build
          examples: 2.0.0-preview2-006120, 1.1.0
.PARAMETER Internal
    Download internal builds. Requires providing credentials via -FeedCredential parameter.
.PARAMETER FeedCredential
    Token to access Azure feed. Used as a query string to append to the Azure feed.
    This parameter typically is not specified.
.PARAMETER InstallDir
    Default: %LocalAppData%\Microsoft\dotnet
    Path to where to install dotnet. Note that binaries will be placed directly in a given directory.
.PARAMETER Architecture
    Default: <auto> - this value represents currently running OS architecture
    Architecture of dotnet binaries to be installed.
    Possible values are: <auto>, amd64, x64, x86, arm64, arm
.PARAMETER SharedRuntime
    This parameter is obsolete and may be removed in a future version of this script.
    The recommended alternative is '-Runtime dotnet'.
    Installs just the shared runtime bits, not the entire SDK.
.PARAMETER Runtime
    Installs just a shared runtime, not the entire SDK.
    Possible values:
        - dotnet     - the Microsoft.NETCore.App shared runtime
        - aspnetcore - the Microsoft.AspNetCore.App shared runtime
        - windowsdesktop - the Microsoft.WindowsDesktop.App shared runtime
.PARAMETER DryRun
    If set it will not perform installation but instead display what command line to use to consistently install
    currently requested version of dotnet cli. In example if you specify version 'latest' it will display a link
    with specific version so that this command can be used deterministicly in a build script.
    It also displays binaries location if you prefer to install or download it yourself.
.PARAMETER NoPath
    By default this script will set environment variable PATH for the current process to the binaries folder inside installation folder.
    If set it will display binaries location but not set any environment variable.
.PARAMETER Verbose
    Displays diagnostics information.
.PARAMETER AzureFeed
    Default: https://dotnetcli.azureedge.net/dotnet
    For internal use only.
    Allows using a different storage to download SDK archives from.
    This parameter is only used if $NoCdn is false.
.PARAMETER UncachedFeed
    For internal use only.
    Allows using a different storage to download SDK archives from.
    This parameter is only used if $NoCdn is true.
.PARAMETER ProxyAddress
    If set, the installer will use the proxy when making web requests
.PARAMETER ProxyUseDefaultCredentials
    Default: false
    Use default credentials, when using proxy address.
.PARAMETER ProxyBypassList
    If set with ProxyAddress, will provide the list of comma separated urls that will bypass the proxy
.PARAMETER SkipNonVersionedFiles
    Default: false
    Skips installing non-versioned files if they already exist, such as dotnet.exe.
.PARAMETER NoCdn
    Disable downloading from the Azure CDN, and use the uncached feed directly.
.PARAMETER JSonFile
    Determines the SDK version from a user specified global.json file
    Note: global.json must have a value for 'SDK:Version'
.PARAMETER DownloadTimeout
    Determines timeout duration in seconds for dowloading of the SDK file
    Default: 1200 seconds (20 minutes)
.PARAMETER KeepZip
    If set, downloaded file is kept
.PARAMETER ZipPath
    Use that path to store installer, generated by default
.EXAMPLE
    dotnet-install.ps1 -Version 7.0.401
    Installs the .NET SDK version 7.0.401
.EXAMPLE
    dotnet-install.ps1 -Channel 8.0 -Quality GA
    Installs the latest GA (general availability) version of the .NET 8.0 SDK
#>
[cmdletbinding()]
param(
   [string]$Channel="LTS",
   [string]$Quality,
   [string]$Version="Latest",
   [switch]$Internal,
   [string]$JSonFile,
   [Alias('i')][string]$InstallDir="<auto>",
   [string]$Architecture="<auto>",
   [string]$Runtime,
   [Obsolete("This parameter may be removed in a future version of this script. The recommended alternative is '-Runtime dotnet'.")]
   [switch]$SharedRuntime,
   [switch]$DryRun,
   [switch]$NoPath,
   [string]$AzureFeed,
   [string]$UncachedFeed,
   [string]$FeedCredential,
   [string]$ProxyAddress,
   [switch]$ProxyUseDefaultCredentials,
   [string[]]$ProxyBypassList=@(),
   [switch]$SkipNonVersionedFiles,
   [switch]$NoCdn,
   [int]$DownloadTimeout=1200,
   [switch]$KeepZip,
   [string]$ZipPath=[System.IO.Path]::combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName()),
   [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

function Say($str) {
    try {
        Write-Host "dotnet-install: $str"
    }
    catch {
        # Some platforms cannot utilize Write-Host (Azure Functions, for instance). Fall back to Write-Output
        Write-Output "dotnet-install: $str"
    }
}

function Say-Warning($str) {
    try {
        Write-Warning "dotnet-install: $str"
    }
    catch {
        # Some platforms cannot utilize Write-Warning (Azure Functions, for instance). Fall back to Write-Output
        Write-Output "dotnet-install: Warning: $str"
    }
}

# Writes a line with error style settings.
# Use this function to show a human-readable comment along with an exception.
function Say-Error($str) {
    try {
        # Write-Error is quite oververbose for the purpose of the function, let's write one line with error style settings.
        $Host.UI.WriteErrorLine("dotnet-install: $str")
    }
    catch {
        Write-Output "dotnet-install: Error: $str"
    }
}

function Say-Verbose($str) {
    try {
        Write-Verbose "dotnet-install: $str"
    }
    catch {
        # Some platforms cannot utilize Write-Verbose (Azure Functions, for instance). Fall back to Write-Output
        Write-Output "dotnet-install: $str"
    }
}

function Measure-Action($name, $block) {
    $time = Measure-Command $block
    $totalSeconds = $time.TotalSeconds
    Say-Verbose "⏱ Action '$name' took $totalSeconds seconds"
}

function Get-Remote-File-Size($zipUri) {
    try {
        $response = Invoke-WebRequest -Uri $zipUri -Method Head
        $fileSize = $response.Headers["Content-Length"]
        if ((![string]::IsNullOrEmpty($fileSize))) {
            Say "Remote file $zipUri size is $fileSize bytes."
        
            return $fileSize
        }
    }
    catch {
        Say-Verbose "Content-Length header was not extracted for $zipUri."
    }

    return $null
}

function Say-Invocation($Invocation) {
    $command = $Invocation.MyCommand;
    $args = (($Invocation.BoundParameters.Keys | foreach { "-$_ `"$($Invocation.BoundParameters[$_])`"" }) -join " ")
    Say-Verbose "$command $args"
}

function Invoke-With-Retry([ScriptBlock]$ScriptBlock, [System.Threading.CancellationToken]$cancellationToken = [System.Threading.CancellationToken]::None, [int]$MaxAttempts = 3, [int]$SecondsBetweenAttempts = 1) {
    $Attempts = 0
    $local:startTime = $(get-date)

    while ($true) {
        try {
            return & $ScriptBlock
        }
        catch {
            $Attempts++
            if (($Attempts -lt $MaxAttempts) -and -not $cancellationToken.IsCancellationRequested) {
                Start-Sleep $SecondsBetweenAttempts
            }
            else {
                $local:elapsedTime = $(get-date) - $local:startTime
                if (($local:elapsedTime.TotalSeconds - $DownloadTimeout) -gt 0 -and -not $cancellationToken.IsCancellationRequested) {
                    throw New-Object System.TimeoutException("Failed to reach the server: connection timeout: default timeout is $DownloadTimeout second(s)");
                }
                throw;
            }
        }
    }
}

function Get-Machine-Architecture() {
    Say-Invocation $MyInvocation

    # On PS x86, PROCESSOR_ARCHITECTURE reports x86 even on x64 systems.
    # To get the correct architecture, we need to use PROCESSOR_ARCHITEW6432.
    # PS x64 doesn't define this, so we fall back to PROCESSOR_ARCHITECTURE.
    # Possible values: amd64, x64, x86, arm64, arm
    if( $ENV:PROCESSOR_ARCHITEW6432 -ne $null ) {
        return $ENV:PROCESSOR_ARCHITEW6432
    }

    try {        
        if( ((Get-CimInstance -ClassName CIM_OperatingSystem).OSArchitecture) -like "ARM*") {
            if( [Environment]::Is64BitOperatingSystem )
            {
                return "arm64"
            }  
            return "arm"
        }
    }
    catch {
        # Machine doesn't support Get-CimInstance
    }

    return $ENV:PROCESSOR_ARCHITECTURE
}

function Get-CLIArchitecture-From-Architecture([string]$Architecture) {
    Say-Invocation $MyInvocation

    if ($Architecture -eq "<auto>") {
        $Architecture = Get-Machine-Architecture
    }

    switch ($Architecture.ToLowerInvariant()) {
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        { $_ -eq "arm" } { return "arm" }
        { $_ -eq "arm64" } { return "arm64" }
        default { throw "Architecture '$Architecture' not supported. If you think this is a bug, report it at https://github.com/dotnet/install-scripts/issues" }
    }
}

function ValidateFeedCredential([string] $FeedCredential)
{
    if ($Internal -and [string]::IsNullOrWhitespace($FeedCredential)) {
        $message = "Provide credentials via -FeedCredential parameter."
        if ($DryRun) {
            Say-Warning "$message"
        } else {
            throw "$message"
        }
    }
    
    #FeedCredential should start with "?", for it to be added to the end of the link.
    #adding "?" at the beginning of the FeedCredential if needed.
    if ((![string]::IsNullOrWhitespace($FeedCredential)) -and ($FeedCredential[0] -ne '?')) {
        $FeedCredential = "?" + $FeedCredential
    }

    return $FeedCredential
}
function Get-NormalizedQuality([string]$Quality) {
    Say-Invocation $MyInvocation

    if ([string]::IsNullOrEmpty($Quality)) {
        return ""
    }

    switch ($Quality) {
        { @("daily", "signed", "validated", "preview") -contains $_ } { return $Quality.ToLowerInvariant() }
        #ga quality is available without specifying quality, so normalizing it to empty
        { $_ -eq "ga" } { return "" }
        default { throw "'$Quality' is not a supported value for -Quality option. Supported values are: daily, signed, validated, preview, ga. If you think this is a bug, report it at https://github.com/dotnet/install-scripts/issues." }
    }
}

function Get-NormalizedChannel([string]$Channel) {
    Say-Invocation $MyInvocation

    if ([string]::IsNullOrEmpty($Channel)) {
        return ""
    }

    if ($Channel.Contains("Current")) {
        Say-Warning 'Value "Current" is deprecated for -Channel option. Use "STS" instead.'
    }

    if ($Channel.StartsWith('release/')) {
        Say-Warning 'Using branch name with -Channel option is no longer supported with newer releases. Use -Quality option with a channel in X.Y format instead, such as "-Channel 5.0 -Quality Daily."'
    }

    switch ($Channel) {
        { $_ -eq "lts" } { return "LTS" }
        { $_ -eq "sts" } { return "STS" }
        { $_ -eq "current" } { return "STS" }
        default { return $Channel.ToLowerInvariant() }
    }
}

function Get-NormalizedProduct([string]$Runtime) {
    Say-Invocation $MyInvocation

    switch ($Runtime) {
        { $_ -eq "dotnet" } { return "dotnet-runtime" }
        { $_ -eq "aspnetcore" } { return "aspnetcore-runtime" }
        { $_ -eq "windowsdesktop" } { return "windowsdesktop-runtime" }
        { [string]::IsNullOrEmpty($_) } { return "dotnet-sdk" }
        default { throw "'$Runtime' is not a supported value for -Runtime option, supported values are: dotnet, aspnetcore, windowsdesktop. If you think this is a bug, report it at https://github.com/dotnet/install-scripts/issues." }
    }
}


# The version text returned from the feeds is a 1-line or 2-line string:
# For the SDK and the dotnet runtime (2 lines):
# Line 1: # commit_hash
# Line 2: # 4-part version
# For the aspnetcore runtime (1 line):
# Line 1: # 4-part version
function Get-Version-From-LatestVersion-File-Content([string]$VersionText) {
    Say-Invocation $MyInvocation

    $Data = -split $VersionText

    $VersionInfo = @{
        CommitHash = $(if ($Data.Count -gt 1) { $Data[0] })
        Version = $Data[-1] # last line is always the version number.
    }
    return $VersionInfo
}

function Load-Assembly([string] $Assembly) {
    try {
        Add-Type -Assembly $Assembly | Out-Null
    }
    catch {
        # On Nano Server, Powershell Core Edition is used.  Add-Type is unable to resolve base class assemblies because they are not GAC'd.
        # Loading the base class assemblies is not unnecessary as the types will automatically get resolved.
    }
}

function GetHTTPResponse([Uri] $Uri, [bool]$HeaderOnly, [bool]$DisableRedirect, [bool]$DisableFeedCredential)
{
    $cts = New-Object System.Threading.CancellationTokenSource

    $downloadScript = {

        $HttpClient = $null

        try {
            # HttpClient is used vs Invoke-WebRequest in order to support Nano Server which doesn't support the Invoke-WebRequest cmdlet.
            Load-Assembly -Assembly System.Net.Http

            if(-not $ProxyAddress) {
                try {
                    # Despite no proxy being explicitly specified, we may still be behind a default proxy
                    $DefaultProxy = [System.Net.WebRequest]::DefaultWebProxy;
                    if($DefaultProxy -and (-not $DefaultProxy.IsBypassed($Uri))) {
                        if ($null -ne $DefaultProxy.GetProxy($Uri)) {
                            $ProxyAddress = $DefaultProxy.GetProxy($Uri).OriginalString
                        } else {
                            $ProxyAddress = $null
                        }
                        $ProxyUseDefaultCredentials = $true
                    }
                } catch {
                    # Eat the exception and move forward as the above code is an attempt
                    #    at resolving the DefaultProxy that may not have been a problem.
                    $ProxyAddress = $null
                    Say-Verbose("Exception ignored: $_.Exception.Message - moving forward...")
                }
            }

            $HttpClientHandler = New-Object System.Net.Http.HttpClientHandler
            if($ProxyAddress) {
                $HttpClientHandler.Proxy =  New-Object System.Net.WebProxy -Property @{
                    Address=$ProxyAddress;
                    UseDefaultCredentials=$ProxyUseDefaultCredentials;
                    BypassList = $ProxyBypassList;
                }
            }       
            if ($DisableRedirect)
            {
                $HttpClientHandler.AllowAutoRedirect = $false
            }
            $HttpClient = New-Object System.Net.Http.HttpClient -ArgumentList $HttpClientHandler

            # Default timeout for HttpClient is 100s.  For a 50 MB download this assumes 500 KB/s average, any less will time out
            # Defaulting to 20 minutes allows it to work over much slower connections.
            $HttpClient.Timeout = New-TimeSpan -Seconds $DownloadTimeout

            if ($HeaderOnly){
                $completionOption = [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead
            }
            else {
                $completionOption = [System.Net.Http.HttpCompletionOption]::ResponseContentRead
            }

            if ($DisableFeedCredential) {
                $UriWithCredential = $Uri
            }
            else {
                $UriWithCredential = "${Uri}${FeedCredential}"
            }

            $Task = $HttpClient.GetAsync("$UriWithCredential", $completionOption).ConfigureAwait("false");
            $Response = $Task.GetAwaiter().GetResult();

            if (($null -eq $Response) -or ((-not $HeaderOnly) -and (-not ($Response.IsSuccessStatusCode)))) {
                # The feed credential is potentially sensitive info. Do not log FeedCredential to console output.
                $DownloadException = [System.Exception] "Unable to download $Uri."

                if ($null -ne $Response) {
                    $DownloadException.Data["StatusCode"] = [int] $Response.StatusCode
                    $DownloadException.Data["ErrorMessage"] = "Unable to download $Uri. Returned HTTP status code: " + $DownloadException.Data["StatusCode"]

                    if (404 -eq [int] $Response.StatusCode)
                    {
                        $cts.Cancel()
                    }
                }

                throw $DownloadException
            }

            return $Response
        }
        catch [System.Net.Http.HttpRequestException] {
            $DownloadException = [System.Exception] "Unable to download $Uri."

            # Pick up the exception message and inner exceptions' messages if they exist
            $CurrentException = $PSItem.Exception
            $ErrorMsg = $CurrentException.Message + "`r`n"
            while ($CurrentException.InnerException) {
              $CurrentException = $CurrentException.InnerException
              $ErrorMsg += $CurrentException.Message + "`r`n"
            }

            # Check if there is an issue concerning TLS.
            if ($ErrorMsg -like "*SSL/TLS*") {
                $ErrorMsg += "Ensure that TLS 1.2 or higher is enabled to use this script.`r`n"
            }

            $DownloadException.Data["ErrorMessage"] = $ErrorMsg
            throw $DownloadException
        }
        finally {
             if ($null -ne $HttpClient) {
                $HttpClient.Dispose()
            }
        }
    }

    try {
        return Invoke-With-Retry $downloadScript $cts.Token
    }
    finally
    {
        if ($null -ne $cts)
        {
            $cts.Dispose()
        }
    }
}

function Get-Version-From-LatestVersion-File([string]$AzureFeed, [string]$Channel) {
    Say-Invocation $MyInvocation

    $VersionFileUrl = $null
    if ($Runtime -eq "dotnet") {
        $VersionFileUrl = "$AzureFeed/Runtime/$Channel/latest.version"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $VersionFileUrl = "$AzureFeed/aspnetcore/Runtime/$Channel/latest.version"
    }
    elseif ($Runtime -eq "windowsdesktop") {
        $VersionFileUrl = "$AzureFeed/WindowsDesktop/$Channel/latest.version"
    }
    elseif (-not $Runtime) {
        $VersionFileUrl = "$AzureFeed/Sdk/$Channel/latest.version"
    }
    else {
        throw "Invalid value for `$Runtime"
    }

    Say-Verbose "Constructed latest.version URL: $VersionFileUrl"

    try {
        $Response = GetHTTPResponse -Uri $VersionFileUrl
    }
    catch {
        Say-Verbose "Failed to download latest.version file."
        throw
    }
    $StringContent = $Response.Content.ReadAsStringAsync().Result

    switch ($Response.Content.Headers.ContentType) {
        { ($_ -eq "application/octet-stream") } { $VersionText = $StringContent }
        { ($_ -eq "text/plain") } { $VersionText = $StringContent }
        { ($_ -eq "text/plain; charset=UTF-8") } { $VersionText = $StringContent }
        default { throw "``$Response.Content.Headers.ContentType`` is an unknown .version file content type." }
    }

    $VersionInfo = Get-Version-From-LatestVersion-File-Content $VersionText

    return $VersionInfo
}

function Parse-Jsonfile-For-Version([string]$JSonFile) {
    Say-Invocation $MyInvocation

    If (-Not (Test-Path $JSonFile)) {
        throw "Unable to find '$JSonFile'"
    }
    try {
        $JSonContent = Get-Content($JSonFile) -Raw | ConvertFrom-Json | Select-Object -expand "sdk" -ErrorAction SilentlyContinue
    }
    catch {
        Say-Error "Json file unreadable: '$JSonFile'"
        throw
    }
    if ($JSonContent) {
        try {
            $JSonContent.PSObject.Properties | ForEach-Object {
                $PropertyName = $_.Name
                if ($PropertyName -eq "version") {
                    $Version = $_.Value
                    Say-Verbose "Version = $Version"
                }
            }
        }
        catch {
            Say-Error "Unable to parse the SDK node in '$JSonFile'"
            throw
        }
    }
    else {
        throw "Unable to find the SDK node in '$JSonFile'"
    }
    If ($Version -eq $null) {
        throw "Unable to find the SDK:version node in '$JSonFile'"
    }
    return $Version
}

function Get-Specific-Version-From-Version([string]$AzureFeed, [string]$Channel, [string]$Version, [string]$JSonFile) {
    Say-Invocation $MyInvocation

    if (-not $JSonFile) {
        if ($Version.ToLowerInvariant() -eq "latest") {
            $LatestVersionInfo = Get-Version-From-LatestVersion-File -AzureFeed $AzureFeed -Channel $Channel
            return $LatestVersionInfo.Version
        }
        else {
            return $Version 
        }
    }
    else {
        return Parse-Jsonfile-For-Version $JSonFile
    }
}

function Get-Download-Link([string]$AzureFeed, [string]$SpecificVersion, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation

    # If anything fails in this lookup it will default to $SpecificVersion
    $SpecificProductVersion = Get-Product-Version -AzureFeed $AzureFeed -SpecificVersion $SpecificVersion

    if ($Runtime -eq "dotnet") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/dotnet-runtime-$SpecificProductVersion-win-$CLIArchitecture.zip"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $PayloadURL = "$AzureFeed/aspnetcore/Runtime/$SpecificVersion/aspnetcore-runtime-$SpecificProductVersion-win-$CLIArchitecture.zip"
    }
    elseif ($Runtime -eq "windowsdesktop") {
        # The windows desktop runtime is part of the core runtime layout prior to 5.0
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/windowsdesktop-runtime-$SpecificProductVersion-win-$CLIArchitecture.zip"
        if ($SpecificVersion -match '^(\d+)\.(.*)$')
        {
            $majorVersion = [int]$Matches[1]
            if ($majorVersion -ge 5)
            {
                $PayloadURL = "$AzureFeed/WindowsDesktop/$SpecificVersion/windowsdesktop-runtime-$SpecificProductVersion-win-$CLIArchitecture.zip"
            }
        }
    }
    elseif (-not $Runtime) {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-sdk-$SpecificProductVersion-win-$CLIArchitecture.zip"
    }
    else {
        throw "Invalid value for `$Runtime"
    }

    Say-Verbose "Constructed primary named payload URL: $PayloadURL"

    return $PayloadURL, $SpecificProductVersion
}

function Get-LegacyDownload-Link([string]$AzureFeed, [string]$SpecificVersion, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation

    if (-not $Runtime) {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-dev-win-$CLIArchitecture.$SpecificVersion.zip"
    }
    elseif ($Runtime -eq "dotnet") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/dotnet-win-$CLIArchitecture.$SpecificVersion.zip"
    }
    else {
        return $null
    }

    Say-Verbose "Constructed legacy named payload URL: $PayloadURL"

    return $PayloadURL
}

function Get-Product-Version([string]$AzureFeed, [string]$SpecificVersion, [string]$PackageDownloadLink) {
    Say-Invocation $MyInvocation

    # Try to get the version number, using the productVersion.txt file located next to the installer file.
    $ProductVersionTxtURLs = (Get-Product-Version-Url $AzureFeed $SpecificVersion $PackageDownloadLink -Flattened $true),
                             (Get-Product-Version-Url $AzureFeed $SpecificVersion $PackageDownloadLink -Flattened $false)
    
    Foreach ($ProductVersionTxtURL in $ProductVersionTxtURLs) {
        Say-Verbose "Checking for the existence of $ProductVersionTxtURL"

        try {
            $productVersionResponse = GetHTTPResponse($productVersionTxtUrl)

            if ($productVersionResponse.StatusCode -eq 200) {
                $productVersion = $productVersionResponse.Content.ReadAsStringAsync().Result.Trim()
                if ($productVersion -ne $SpecificVersion)
                {
                    Say "Using alternate version $productVersion found in $ProductVersionTxtURL"
                }
                return $productVersion
            }
            else {
                Say-Verbose "Got StatusCode $($productVersionResponse.StatusCode) when trying to get productVersion.txt at $productVersionTxtUrl."
            }
        } 
        catch {
            Say-Verbose "Could not read productVersion.txt at $productVersionTxtUrl (Exception: '$($_.Exception.Message)'. )"
        }
    }

    # Getting the version number with productVersion.txt has failed. Try parsing the download link for a version number.
    if ([string]::IsNullOrEmpty($PackageDownloadLink))
    {
        Say-Verbose "Using the default value '$SpecificVersion' as the product version."
        return $SpecificVersion
    }

    $productVersion = Get-ProductVersionFromDownloadLink $PackageDownloadLink $SpecificVersion
    return $productVersion
}

function Get-Product-Version-Url([string]$AzureFeed, [string]$SpecificVersion, [string]$PackageDownloadLink, [bool]$Flattened) {
    Say-Invocation $MyInvocation

    $majorVersion=$null
    if ($SpecificVersion -match '^(\d+)\.(.*)') {
        $majorVersion = $Matches[1] -as[int]
    }

    $pvFileName='productVersion.txt'
    if($Flattened) {
        if(-not $Runtime) {
            $pvFileName='sdk-productVersion.txt'
        }
        elseif($Runtime -eq "dotnet") {
            $pvFileName='runtime-productVersion.txt'
        }
        else {
            $pvFileName="$Runtime-productVersion.txt"
        }
    }

    if ([string]::IsNullOrEmpty($PackageDownloadLink)) {
        if ($Runtime -eq "dotnet") {
            $ProductVersionTxtURL = "$AzureFeed/Runtime/$SpecificVersion/$pvFileName"
        }
        elseif ($Runtime -eq "aspnetcore") {
            $ProductVersionTxtURL = "$AzureFeed/aspnetcore/Runtime/$SpecificVersion/$pvFileName"
        }
        elseif ($Runtime -eq "windowsdesktop") {
            # The windows desktop runtime is part of the core runtime layout prior to 5.0
            $ProductVersionTxtURL = "$AzureFeed/Runtime/$SpecificVersion/$pvFileName"
            if ($majorVersion -ne $null -and $majorVersion -ge 5) {
                $ProductVersionTxtURL = "$AzureFeed/WindowsDesktop/$SpecificVersion/$pvFileName"
            }
        }
        elseif (-not $Runtime) {
            $ProductVersionTxtURL = "$AzureFeed/Sdk/$SpecificVersion/$pvFileName"
        }
        else {
            throw "Invalid value '$Runtime' specified for `$Runtime"
        }
    }
    else {
        $ProductVersionTxtURL = $PackageDownloadLink.Substring(0, $PackageDownloadLink.LastIndexOf("/"))  + "/$pvFileName"
    }

    Say-Verbose "Constructed productVersion link: $ProductVersionTxtURL"

    return $ProductVersionTxtURL
}

function Get-ProductVersionFromDownloadLink([string]$PackageDownloadLink, [string]$SpecificVersion)
{
    Say-Invocation $MyInvocation

    #product specific version follows the product name
    #for filename 'dotnet-sdk-3.1.404-win-x64.zip': the product version is 3.1.400
    $filename = $PackageDownloadLink.Substring($PackageDownloadLink.LastIndexOf("/") + 1)
    $filenameParts = $filename.Split('-')
    if ($filenameParts.Length -gt 2)
    {
        $productVersion = $filenameParts[2]
        Say-Verbose "Extracted product version '$productVersion' from download link '$PackageDownloadLink'."
    }
    else {
        Say-Verbose "Using the default value '$SpecificVersion' as the product version."
        $productVersion = $SpecificVersion
    }
    return $productVersion 
}

function Get-User-Share-Path() {
    Say-Invocation $MyInvocation

    $InstallRoot = $env:DOTNET_INSTALL_DIR
    if (!$InstallRoot) {
        $InstallRoot = "$env:LocalAppData\Microsoft\dotnet"
    }
    return $InstallRoot
}

function Resolve-Installation-Path([string]$InstallDir) {
    Say-Invocation $MyInvocation

    if ($InstallDir -eq "<auto>") {
        return Get-User-Share-Path
    }
    return $InstallDir
}

function Is-Dotnet-Package-Installed([string]$InstallRoot, [string]$RelativePathToPackage, [string]$SpecificVersion) {
    Say-Invocation $MyInvocation

    $DotnetPackagePath = Join-Path -Path $InstallRoot -ChildPath $RelativePathToPackage | Join-Path -ChildPath $SpecificVersion
    Say-Verbose "Is-Dotnet-Package-Installed: DotnetPackagePath=$DotnetPackagePath"
    return Test-Path $DotnetPackagePath -PathType Container
}

function Get-Absolute-Path([string]$RelativeOrAbsolutePath) {
    # Too much spam
    # Say-Invocation $MyInvocation

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RelativeOrAbsolutePath)
}

function Get-Path-Prefix-With-Version($path) {
    # example path with regex: shared/1.0.0-beta-12345/somepath
    $match = [regex]::match($path, "/\d+\.\d+[^/]+/")
    if ($match.Success) {
        return $entry.FullName.Substring(0, $match.Index + $match.Length)
    }

    return $null
}

function Get-List-Of-Directories-And-Versions-To-Unpack-From-Dotnet-Package([System.IO.Compression.ZipArchive]$Zip, [string]$OutPath) {
    Say-Invocation $MyInvocation

    $ret = @()
    foreach ($entry in $Zip.Entries) {
        $dir = Get-Path-Prefix-With-Version $entry.FullName
        if ($null -ne $dir) {
            $path = Get-Absolute-Path $(Join-Path -Path $OutPath -ChildPath $dir)
            if (-Not (Test-Path $path -PathType Container)) {
                $ret += $dir
            }
        }
    }

    $ret = $ret | Sort-Object | Get-Unique

    $values = ($ret | foreach { "$_" }) -join ";"
    Say-Verbose "Directories to unpack: $values"

    return $ret
}

# Example zip content and extraction algorithm:
# Rule: files if extracted are always being extracted to the same relative path locally
# .\
#       a.exe   # file does not exist locally, extract
#       b.dll   # file exists locally, override only if $OverrideFiles set
#       aaa\    # same rules as for files
#           ...
#       abc\1.0.0\  # directory contains version and exists locally
#           ...     # do not extract content under versioned part
#       abc\asd\    # same rules as for files
#            ...
#       def\ghi\1.0.1\  # directory contains version and does not exist locally
#           ...         # extract content
function Extract-Dotnet-Package([string]$ZipPath, [string]$OutPath) {
    Say-Invocation $MyInvocation

    Load-Assembly -Assembly System.IO.Compression.FileSystem
    Set-Variable -Name Zip
    try {
        $Zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)

        $DirectoriesToUnpack = Get-List-Of-Directories-And-Versions-To-Unpack-From-Dotnet-Package -Zip $Zip -OutPath $OutPath

        foreach ($entry in $Zip.Entries) {
            $PathWithVersion = Get-Path-Prefix-With-Version $entry.FullName
            if (($null -eq $PathWithVersion) -Or ($DirectoriesToUnpack -contains $PathWithVersion)) {
                $DestinationPath = Get-Absolute-Path $(Join-Path -Path $OutPath -ChildPath $entry.FullName)
                $DestinationDir = Split-Path -Parent $DestinationPath
                $OverrideFiles=$OverrideNonVersionedFiles -Or (-Not (Test-Path $DestinationPath))
                if ((-Not $DestinationPath.EndsWith("\")) -And $OverrideFiles) {
                    New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
                    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $DestinationPath, $OverrideNonVersionedFiles)
                }
            }
        }
    }
    catch
    {
        Say-Error "Failed to extract package. Exception: $_"
        throw;
    }
    finally {
        if ($null -ne $Zip) {
            $Zip.Dispose()
        }
    }
}

function DownloadFile($Source, [string]$OutPath) {
    if ($Source -notlike "http*") {
        #  Using System.IO.Path.GetFullPath to get the current directory
        #    does not work in this context - $pwd gives the current directory
        if (![System.IO.Path]::IsPathRooted($Source)) {
            $Source = $(Join-Path -Path $pwd -ChildPath $Source)
        }
        $Source = Get-Absolute-Path $Source
        Say "Copying file from $Source to $OutPath"
        Copy-Item $Source $OutPath
        return
    }

    $Stream = $null
    
    try {
        $Response = GetHTTPResponse -Uri $Source
        $Stream = $Response.Content.ReadAsStreamAsync().Result
        $File = [System.IO.File]::Create($OutPath)
        $Stream.CopyTo($File)
        $File.Close()

        ValidateRemoteLocalFileSizes -LocalFileOutPath $OutPath -SourceUri $Source
    }
    finally {
        if ($null -ne $Stream) {
            $Stream.Dispose()
        }
    }
}

function ValidateRemoteLocalFileSizes([string]$LocalFileOutPath, $SourceUri) {
    try {
        $remoteFileSize = Get-Remote-File-Size -zipUri $SourceUri
        $fileSize = [long](Get-Item $LocalFileOutPath).Length
        Say "Downloaded file $SourceUri size is $fileSize bytes."
    
        if ((![string]::IsNullOrEmpty($remoteFileSize)) -and !([string]::IsNullOrEmpty($fileSize)) ) {
            if ($remoteFileSize -ne $fileSize) {
                Say "The remote and local file sizes are not equal. Remote file size is $remoteFileSize bytes and local size is $fileSize bytes. The local package may be corrupted."
            }
            else {
                Say "The remote and local file sizes are equal."
            }   
        }
        else {
            Say "Either downloaded or local package size can not be measured. One of them may be corrupted."
        }
    }
    catch {
        Say "Either downloaded or local package size can not be measured. One of them may be corrupted."
    }
}

function SafeRemoveFile($Path) {
    try {
        if (Test-Path $Path) {
            Remove-Item $Path
            Say-Verbose "The temporary file `"$Path`" was removed."
        }
        else {
            Say-Verbose "The temporary file `"$Path`" does not exist, therefore is not removed."
        }
    }
    catch {
        Say-Warning "Failed to remove the temporary file: `"$Path`", remove it manually."
    }
}

function Prepend-Sdk-InstallRoot-To-Path([string]$InstallRoot) {
    $BinPath = Get-Absolute-Path $(Join-Path -Path $InstallRoot -ChildPath "")
    if (-Not $NoPath) {
        $SuffixedBinPath = "$BinPath;"
        if (-Not $env:path.Contains($SuffixedBinPath)) {
            Say "Adding to current process PATH: `"$BinPath`". Note: This change will not be visible if PowerShell was run as a child process."
            $env:path = $SuffixedBinPath + $env:path
        } else {
            Say-Verbose "Current process PATH already contains `"$BinPath`""
        }
    }
    else {
        Say "Binaries of dotnet can be found in $BinPath"
    }
}

function PrintDryRunOutput($Invocation, $DownloadLinks)
{
    Say "Payload URLs:"
    
    for ($linkIndex=0; $linkIndex -lt $DownloadLinks.count; $linkIndex++) {
        Say "URL #$linkIndex - $($DownloadLinks[$linkIndex].type): $($DownloadLinks[$linkIndex].downloadLink)"
    }
    $RepeatableCommand = ".\$ScriptName -Version `"$SpecificVersion`" -InstallDir `"$InstallRoot`" -Architecture `"$CLIArchitecture`""
    if ($Runtime -eq "dotnet") {
       $RepeatableCommand+=" -Runtime `"dotnet`""
    }
    elseif ($Runtime -eq "aspnetcore") {
       $RepeatableCommand+=" -Runtime `"aspnetcore`""
    }

    foreach ($key in $Invocation.BoundParameters.Keys) {
        if (-not (@("Architecture","Channel","DryRun","InstallDir","Runtime","SharedRuntime","Version","Quality","FeedCredential") -contains $key)) {
            $RepeatableCommand+=" -$key `"$($Invocation.BoundParameters[$key])`""
        }
    }
    if ($Invocation.BoundParameters.Keys -contains "FeedCredential") {
        $RepeatableCommand+=" -FeedCredential `"<feedCredential>`""
    }
    Say "Repeatable invocation: $RepeatableCommand"
    if ($SpecificVersion -ne $EffectiveVersion)
    {
        Say "NOTE: Due to finding a version manifest with this runtime, it would actually install with version '$EffectiveVersion'"
    }
}

function Get-AkaMSDownloadLink([string]$Channel, [string]$Quality, [bool]$Internal, [string]$Product, [string]$Architecture) {
    Say-Invocation $MyInvocation 

    #quality is not supported for LTS or STS channel
    if (![string]::IsNullOrEmpty($Quality) -and (@("LTS", "STS") -contains $Channel)) {
        $Quality = ""
        Say-Warning "Specifying quality for STS or LTS channel is not supported, the quality will be ignored."
    }
    Say-Verbose "Retrieving primary payload URL from aka.ms link for channel: '$Channel', quality: '$Quality' product: '$Product', os: 'win', architecture: '$Architecture'." 
   
    #construct aka.ms link
    $akaMsLink = "https://aka.ms/dotnet"
    if ($Internal) {
        $akaMsLink += "/internal"
    }
    $akaMsLink += "/$Channel"
    if (-not [string]::IsNullOrEmpty($Quality)) {
        $akaMsLink +="/$Quality"
    }
    $akaMsLink +="/$Product-win-$Architecture.zip"
    Say-Verbose  "Constructed aka.ms link: '$akaMsLink'."
    $akaMsDownloadLink=$null

    for ($maxRedirections = 9; $maxRedirections -ge 0; $maxRedirections--)
    {
        #get HTTP response
        #do not pass credentials as a part of the $akaMsLink and do not apply credentials in the GetHTTPResponse function
        #otherwise the redirect link would have credentials as well
        #it would result in applying credentials twice to the resulting link and thus breaking it, and in echoing credentials to the output as a part of redirect link
        $Response= GetHTTPResponse -Uri $akaMsLink -HeaderOnly $true -DisableRedirect $true -DisableFeedCredential $true
        Say-Verbose "Received response:`n$Response"

        if ([string]::IsNullOrEmpty($Response)) {
            Say-Verbose "The link '$akaMsLink' is not valid: failed to get redirect location. The resource is not available."
            return $null
        }

        #if HTTP code is 301 (Moved Permanently), the redirect link exists
        if  ($Response.StatusCode -eq 301)
        {
            try {
                $akaMsDownloadLink = $Response.Headers.GetValues("Location")[0]

                if ([string]::IsNullOrEmpty($akaMsDownloadLink)) {
                    Say-Verbose "The link '$akaMsLink' is not valid: server returned 301 (Moved Permanently), but the headers do not contain the redirect location."
                    return $null
                }

                Say-Verbose "The redirect location retrieved: '$akaMsDownloadLink'."
                # This may yet be a link to another redirection. Attempt to retrieve the page again.
                $akaMsLink = $akaMsDownloadLink
                continue
            }
            catch {
                Say-Verbose "The link '$akaMsLink' is not valid: failed to get redirect location."
                return $null
            }
        }
        elseif ((($Response.StatusCode -lt 300) -or ($Response.StatusCode -ge 400)) -and (-not [string]::IsNullOrEmpty($akaMsDownloadLink)))
        {
            # Redirections have ended.
            return $akaMsDownloadLink
        }

        Say-Verbose "The link '$akaMsLink' is not valid: failed to retrieve the redirection location."
        return $null
    }

    Say-Verbose "Aka.ms links have redirected more than the maximum allowed redirections. This may be caused by a cyclic redirection of aka.ms links."
    return $null

}

function Get-AkaMsLink-And-Version([string] $NormalizedChannel, [string] $NormalizedQuality, [bool] $Internal, [string] $ProductName, [string] $Architecture) {
    $AkaMsDownloadLink = Get-AkaMSDownloadLink -Channel $NormalizedChannel -Quality $NormalizedQuality -Internal $Internal -Product $ProductName -Architecture $Architecture
   
    if ([string]::IsNullOrEmpty($AkaMsDownloadLink)){
        if (-not [string]::IsNullOrEmpty($NormalizedQuality)) {
            # if quality is specified - exit with error - there is no fallback approach
            Say-Error "Failed to locate the latest version in the channel '$NormalizedChannel' with '$NormalizedQuality' quality for '$ProductName', os: 'win', architecture: '$Architecture'."
            Say-Error "Refer to: https://aka.ms/dotnet-os-lifecycle for information on .NET Core support."
            throw "aka.ms link resolution failure"
        }
        Say-Verbose "Falling back to latest.version file approach."
        return ($null, $null, $null)
    }
    else {
        Say-Verbose "Retrieved primary named payload URL from aka.ms link: '$AkaMsDownloadLink'."
        Say-Verbose  "Downloading using legacy url will not be attempted."

        #get version from the path
        $pathParts = $AkaMsDownloadLink.Split('/')
        if ($pathParts.Length -ge 2) { 
            $SpecificVersion = $pathParts[$pathParts.Length - 2]
            Say-Verbose "Version: '$SpecificVersion'."
        }
        else {
            Say-Error "Failed to extract the version from download link '$AkaMsDownloadLink'."
            return ($null, $null, $null)
        }

        #retrieve effective (product) version
        $EffectiveVersion = Get-Product-Version -SpecificVersion $SpecificVersion -PackageDownloadLink $AkaMsDownloadLink
        Say-Verbose "Product version: '$EffectiveVersion'."

        return ($AkaMsDownloadLink, $SpecificVersion, $EffectiveVersion);
    }
}

function Get-Feeds-To-Use()
{
    $feeds = @(
    "https://dotnetcli.azureedge.net/dotnet",
    "https://dotnetbuilds.azureedge.net/public"
    )

    if (-not [string]::IsNullOrEmpty($AzureFeed)) {
        $feeds = @($AzureFeed)
    }

    if ($NoCdn) {
        $feeds = @(
        "https://dotnetcli.blob.core.windows.net/dotnet",
        "https://dotnetbuilds.blob.core.windows.net/public"
        )

        if (-not [string]::IsNullOrEmpty($UncachedFeed)) {
            $feeds = @($UncachedFeed)
        }
    }

    return $feeds
}

function Resolve-AssetName-And-RelativePath([string] $Runtime) {
    
    if ($Runtime -eq "dotnet") {
        $assetName = ".NET Core Runtime"
        $dotnetPackageRelativePath = "shared\Microsoft.NETCore.App"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $assetName = "ASP.NET Core Runtime"
        $dotnetPackageRelativePath = "shared\Microsoft.AspNetCore.App"
    }
    elseif ($Runtime -eq "windowsdesktop") {
        $assetName = ".NET Core Windows Desktop Runtime"
        $dotnetPackageRelativePath = "shared\Microsoft.WindowsDesktop.App"
    }
    elseif (-not $Runtime) {
        $assetName = ".NET Core SDK"
        $dotnetPackageRelativePath = "sdk"
    }
    else {
        throw "Invalid value for `$Runtime"
    }

    return ($assetName, $dotnetPackageRelativePath)
}

function Prepare-Install-Directory {
    $diskSpaceWarning = "Failed to check the disk space. Installation will continue, but it may fail if you do not have enough disk space.";

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        Say-Verbose $diskSpaceWarning
        return
    }

    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null

    $installDrive = $((Get-Item $InstallRoot -Force).PSDrive.Name);
    $diskInfo = $null
    try {
        $diskInfo = Get-PSDrive -Name $installDrive
    }
    catch {
        Say-Warning $diskSpaceWarning
    }

    # The check is relevant for PS version >= 7, the result can be irrelevant for older versions. See https://github.com/PowerShell/PowerShell/issues/12442.
    if ( ($null -ne $diskInfo) -and ($diskInfo.Free / 1MB -le 100)) {
        throw "There is not enough disk space on drive ${installDrive}:"
    }
}

if ($Help)
{
    Get-Help $PSCommandPath -Examples
    exit
}

Say-Verbose "Note that the intended use of this script is for Continuous Integration (CI) scenarios, where:"
Say-Verbose "- The SDK needs to be installed without user interaction and without admin rights."
Say-Verbose "- The SDK installation doesn't need to persist across multiple CI runs."
Say-Verbose "To set up a development environment or to run apps, use installers rather than this script. Visit https://dotnet.microsoft.com/download to get the installer.`r`n"

if ($SharedRuntime -and (-not $Runtime)) {
    $Runtime = "dotnet"
}

$OverrideNonVersionedFiles = !$SkipNonVersionedFiles

Measure-Action "Product discovery" {
    $script:CLIArchitecture = Get-CLIArchitecture-From-Architecture $Architecture
    $script:NormalizedQuality = Get-NormalizedQuality $Quality
    Say-Verbose "Normalized quality: '$NormalizedQuality'"
    $script:NormalizedChannel = Get-NormalizedChannel $Channel
    Say-Verbose "Normalized channel: '$NormalizedChannel'"
    $script:NormalizedProduct = Get-NormalizedProduct $Runtime
    Say-Verbose "Normalized product: '$NormalizedProduct'"
    $script:FeedCredential = ValidateFeedCredential $FeedCredential
}

$InstallRoot = Resolve-Installation-Path $InstallDir
Say-Verbose "InstallRoot: $InstallRoot"
$ScriptName = $MyInvocation.MyCommand.Name
($assetName, $dotnetPackageRelativePath) = Resolve-AssetName-And-RelativePath -Runtime $Runtime

$feeds = Get-Feeds-To-Use
$DownloadLinks = @()

if ($Version.ToLowerInvariant() -ne "latest" -and -not [string]::IsNullOrEmpty($Quality)) {
    throw "Quality and Version options are not allowed to be specified simultaneously. See https:// learn.microsoft.com/dotnet/core/tools/dotnet-install-script#options for details."
}

# aka.ms links can only be used if the user did not request a specific version via the command line or a global.json file.
if ([string]::IsNullOrEmpty($JSonFile) -and ($Version -eq "latest")) {
    ($DownloadLink, $SpecificVersion, $EffectiveVersion) = Get-AkaMsLink-And-Version $NormalizedChannel $NormalizedQuality $Internal $NormalizedProduct $CLIArchitecture
    
    if ($null -ne $DownloadLink) {
        $DownloadLinks += New-Object PSObject -Property @{downloadLink="$DownloadLink";specificVersion="$SpecificVersion";effectiveVersion="$EffectiveVersion";type='aka.ms'}
        Say-Verbose "Generated aka.ms link $DownloadLink with version $EffectiveVersion"
        
        if (-Not $DryRun) {
            Say-Verbose "Checking if the version $EffectiveVersion is already installed"
            if (Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $EffectiveVersion)
            {
                Say "$assetName with version '$EffectiveVersion' is already installed."
                Prepend-Sdk-InstallRoot-To-Path -InstallRoot $InstallRoot
                return
            }
        }
    }
}

# Primary and legacy links cannot be used if a quality was specified.
# If we already have an aka.ms link, no need to search the blob feeds.
if ([string]::IsNullOrEmpty($NormalizedQuality) -and 0 -eq $DownloadLinks.count)
{
    foreach ($feed in $feeds) {
        try {
            $SpecificVersion = Get-Specific-Version-From-Version -AzureFeed $feed -Channel $Channel -Version $Version -JSonFile $JSonFile
            $DownloadLink, $EffectiveVersion = Get-Download-Link -AzureFeed $feed -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture
            $LegacyDownloadLink = Get-LegacyDownload-Link -AzureFeed $feed -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture
            
            $DownloadLinks += New-Object PSObject -Property @{downloadLink="$DownloadLink";specificVersion="$SpecificVersion";effectiveVersion="$EffectiveVersion";type='primary'}
            Say-Verbose "Generated primary link $DownloadLink with version $EffectiveVersion"
    
            if (-not [string]::IsNullOrEmpty($LegacyDownloadLink)) {
                $DownloadLinks += New-Object PSObject -Property @{downloadLink="$LegacyDownloadLink";specificVersion="$SpecificVersion";effectiveVersion="$EffectiveVersion";type='legacy'}
                Say-Verbose "Generated legacy link $LegacyDownloadLink with version $EffectiveVersion"
            }
    
            if (-Not $DryRun) {
                Say-Verbose "Checking if the version $EffectiveVersion is already installed"
                if (Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $EffectiveVersion)
                {
                    Say "$assetName with version '$EffectiveVersion' is already installed."
                    Prepend-Sdk-InstallRoot-To-Path -InstallRoot $InstallRoot
                    return
                }
            }
        }
        catch
        {
            Say-Verbose "Failed to acquire download links from feed $feed. Exception: $_"
        }
    }
}

if ($DownloadLinks.count -eq 0) {
    throw "Failed to resolve the exact version number."
}

if ($DryRun) {
    PrintDryRunOutput $MyInvocation $DownloadLinks
    return
}

Measure-Action "Installation directory preparation" { Prepare-Install-Directory }

Say-Verbose "Zip path: $ZipPath"

$DownloadSucceeded = $false
$DownloadedLink = $null
$ErrorMessages = @()

foreach ($link in $DownloadLinks)
{
    Say-Verbose "Downloading `"$($link.type)`" link $($link.downloadLink)"

    try {
        Measure-Action "Package download" { DownloadFile -Source $link.downloadLink -OutPath $ZipPath }
        Say-Verbose "Download succeeded."
        $DownloadSucceeded = $true
        $DownloadedLink = $link
        break
    }
    catch {
        $StatusCode = $null
        $ErrorMessage = $null

        if ($PSItem.Exception.Data.Contains("StatusCode")) {
            $StatusCode = $PSItem.Exception.Data["StatusCode"]
        }
    
        if ($PSItem.Exception.Data.Contains("ErrorMessage")) {
            $ErrorMessage = $PSItem.Exception.Data["ErrorMessage"]
        } else {
            $ErrorMessage = $PSItem.Exception.Message
        }

        Say-Verbose "Download failed with status code $StatusCode. Error message: $ErrorMessage"
        $ErrorMessages += "Downloading from `"$($link.type)`" link has failed with error:`nUri: $($link.downloadLink)`nStatusCode: $StatusCode`nError: $ErrorMessage"
    }

    # This link failed. Clean up before trying the next one.
    SafeRemoveFile -Path $ZipPath
}

if (-not $DownloadSucceeded) {
    foreach ($ErrorMessage in $ErrorMessages) {
        Say-Error $ErrorMessages
    }

    throw "Could not find `"$assetName`" with version = $($DownloadLinks[0].effectiveVersion)`nRefer to: https://aka.ms/dotnet-os-lifecycle for information on .NET support"
}

Say "Extracting the archive."
Measure-Action "Package extraction" { Extract-Dotnet-Package -ZipPath $ZipPath -OutPath $InstallRoot }

#  Check if the SDK version is installed; if not, fail the installation.
$isAssetInstalled = $false

# if the version contains "RTM" or "servicing"; check if a 'release-type' SDK version is installed.
if ($DownloadedLink.effectiveVersion -Match "rtm" -or $DownloadedLink.effectiveVersion -Match "servicing") {
    $ReleaseVersion = $DownloadedLink.effectiveVersion.Split("-")[0]
    Say-Verbose "Checking installation: version = $ReleaseVersion"
    $isAssetInstalled = Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $ReleaseVersion
}

#  Check if the SDK version is installed.
if (!$isAssetInstalled) {
    Say-Verbose "Checking installation: version = $($DownloadedLink.effectiveVersion)"
    $isAssetInstalled = Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $DownloadedLink.effectiveVersion
}

# Version verification failed. More likely something is wrong either with the downloaded content or with the verification algorithm.
if (!$isAssetInstalled) {
    Say-Error "Failed to verify the version of installed `"$assetName`".`nInstallation source: $($DownloadedLink.downloadLink).`nInstallation location: $InstallRoot.`nReport the bug at https://github.com/dotnet/install-scripts/issues."
    throw "`"$assetName`" with version = $($DownloadedLink.effectiveVersion) failed to install with an unknown error."
}

if (-not $KeepZip) {
    SafeRemoveFile -Path $ZipPath
}

Measure-Action "Setting up shell environment" { Prepend-Sdk-InstallRoot-To-Path -InstallRoot $InstallRoot }

Say "Note that the script does not ensure your Windows version is supported during the installation."
Say "To check the list of supported versions, go to https://learn.microsoft.com/en-us/dotnet/core/install/windows#supported-versions"
Say "Installed version is $($DownloadedLink.effectiveVersion)"
Say "Installation finished"