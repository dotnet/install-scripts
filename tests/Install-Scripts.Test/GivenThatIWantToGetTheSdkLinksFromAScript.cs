// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VerifyTests;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class GivenThatIWantToGetTheSdkLinksFromAScript : TestBase
    {
        public GivenThatIWantToGetTheSdkLinksFromAScript(VerifySettings settings = null)
            : base(settings) { }

        [Theory]
        [InlineData("InstallationScriptTests.json")]
        [InlineData("InstallationScriptTestsWithMultipleSdkFields.json")]
        [InlineData("InstallationScriptTestsWithVersionFieldInTheMiddle.json")]
        [InlineData("InstallationScriptTestsWithWindowsLineEndings.json")]
        public void WhenJsonFileIsPassedToInstallScripts(string filename)
        {
            var installationScriptTestsJsonFile = Path.Combine(Environment.CurrentDirectory,
                "Assets", filename);

            var args = new List<string> { "-dryrun", "-jsonfile", installationScriptTestsJsonFile };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().NotHaveStdOutContaining("jsonfile");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().HaveStdOutContaining("\"1.0.0-beta.19463.3\"");
            commandResult.Should().HaveStdOutMatching(@"URL\s#0\s-\s(legacy|primary|aka\.ms):\shttps://");
        }

        [Theory]
        [InlineData("-nopath", "")]
        [InlineData("-verbose", "")]
        [InlineData("-nocdn", "")]
        [InlineData("-azurefeed", "https://dotnetcli.azureedge.net/dotnet")]
        [InlineData("-uncachedfeed", "https://dotnetcli.blob.core.windows.net/dotnet")]
        public void WhenVariousParametersArePassedToInstallScripts(string parameter, string value)
        {
            var args = new List<string> { "-dryrun", parameter };
            if (!string.IsNullOrEmpty(value))
            {
                args.Add(value);
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().HaveStdOutMatching(@"URL\s#0\s-\s(legacy|primary|aka\.ms):\shttps://");

            //  Non-dynamic input parameters should always be on the ouput line
            commandResult.Should().HaveStdOutContainingIgnoreCase(parameter);
        }

        [Theory]
        [InlineData("-runtime", "dotnet")]
        [InlineData("-runtime", "aspnetcore")]
        [InlineData("-sharedruntime", "dotnet")]
        public void WhenRuntimeParametersArePassedToInstallScripts(string runtime, string runtimeType)
        {
            var args = new List<string> { "-dryrun", runtime };
            if (!runtime.Equals("-sharedruntime", StringComparison.OrdinalIgnoreCase))
            {
                args.Add(runtimeType);
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().HaveStdOutMatching(@"URL\s#0\s-\s(legacy|primary|aka\.ms):\shttps://");

            //  Runtime should resolve to the correct 'type'
            commandResult.Should().HaveStdOutContainingIgnoreCase("-runtime");
            commandResult.Should().HaveStdOutContainingIgnoreCase(runtimeType);
        }

        [Theory]
        [InlineData("1.0", "dotnet")]
        [InlineData("1.1", "dotnet")]
        [InlineData("2.0", "dotnet")]
        [InlineData("2.2", "dotnet")]
        [InlineData("3.0", "dotnet")]
        [InlineData("3.1", "dotnet")]
        [InlineData("3.1", "dotnet", true)]
        [InlineData("5.0", "dotnet")]
        [InlineData("5.0", "dotnet", true)]
        [InlineData("STS", "dotnet")]
        [InlineData("LTS", "dotnet")]
        [InlineData("master", "dotnet")]
        [InlineData("release/2.1", "dotnet")]
        [InlineData("release/2.2", "dotnet")]
        [InlineData("release/3.0", "dotnet")]
        [InlineData("release/3.1", "dotnet")]
        [InlineData("release/3.1", "dotnet", true)]
        // [InlineData("release/5.0", "dotnet")] - Broken
        [InlineData("STS", "aspnetcore")]
        [InlineData("LTS", "aspnetcore")]
        //[InlineData("1.0", "aspnetcore")] - Broken
        //[InlineData("1.1", "aspnetcore")] - Broken
        //[InlineData("2.0", "aspnetcore")] - Broken
        [InlineData("2.2", "aspnetcore")]
        [InlineData("3.0", "aspnetcore")]
        [InlineData("3.1", "aspnetcore")]
        [InlineData("5.0", "aspnetcore")]
        [InlineData("5.0", "aspnetcore", true)]
        [InlineData("master", "aspnetcore")]
        [InlineData("release/2.1", "aspnetcore")]
        [InlineData("release/2.2", "aspnetcore")]
        //[InlineData("release/3.0", "aspnetcore")] - Broken
        //[InlineData("release/3.1", "aspnetcore")] - Broken
        //[InlineData("release/5.0", "aspnetcore")] - Broken 
        [InlineData("STS", "windowsdesktop")]
        [InlineData("STS", "windowsdesktop", true)]
        [InlineData("LTS", "windowsdesktop")]
        [InlineData("3.0", "windowsdesktop")]
        [InlineData("3.1", "windowsdesktop")]
        [InlineData("5.0", "windowsdesktop")]
        [InlineData("5.0", "windowsdesktop", true)]
        [InlineData("master", "windowsdesktop")]
        [InlineData("master", "windowsdesktop", true)]
        public void WhenChannelResolvesToASpecificRuntimeVersion(string channel, string runtimeType, bool useCustomFeedCredential = false)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && runtimeType == "windowsdesktop")
            {
                //do not run windowsdesktop test on Linux environment
                return;
            }
            var args = new List<string> { "-dryrun", "-channel", channel, "-runtime", runtimeType };

            string feedCredentials = default;
            if (useCustomFeedCredential)
            {
                feedCredentials = Guid.NewGuid().ToString();
                args.Add("-feedCredential");
                args.Add(feedCredentials);
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().HaveStdOutMatching(@"URL\s#0\s-\s(legacy|primary|aka\.ms):\shttps://");

            //  Channel should be translated to a specific Runtime version
            commandResult.Should().HaveStdOutContainingIgnoreCase("-version");

            if (useCustomFeedCredential)
            {
                commandResult.Should().NotHaveStdOutContainingIgnoreCase(feedCredentials);
            }
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("1.1")]
        [InlineData("2.0")]
        [InlineData("2.1")]
        [InlineData("3.0")]
        [InlineData("3.1")]
        [InlineData("5.0")]
        [InlineData("STS")]
        [InlineData("LTS")]
        [InlineData("master")]
        [InlineData("release/1.0.0")]
        [InlineData("release/2.0.0")]
        [InlineData("release/2.0.2")]
        [InlineData("release/2.0.2", true)]
        [InlineData("release/2.1.1xx")]
        [InlineData("release/2.1.2xx")]
        [InlineData("release/2.1.3xx")]
        [InlineData("release/2.1.4xx")]
        [InlineData("release/2.1.401")]
        [InlineData("release/2.1.401", true)]
        [InlineData("release/2.1.5xx")]
        [InlineData("release/2.1.502")]
        [InlineData("release/2.1.6xx")]
        [InlineData("release/2.1.7xx")]
        [InlineData("release/2.1.8xx")]
        [InlineData("release/2.2.1xx")]
        [InlineData("release/2.2.1xx", true)]
        [InlineData("release/2.2.2xx")]
        [InlineData("release/2.2.3xx")]
        [InlineData("release/2.2.4xx")]
        [InlineData("release/3.0.1xx")]
        [InlineData("release/5.0.1xx")]
        [InlineData("release/5.0.1xx", true)]
        [InlineData("release/5.0.1xx-preview1")]
        [InlineData("release/5.0.1xx-preview2")]
        [InlineData("release/5.0.1xx-preview3")]
        // [InlineData("release/5.0.1xx-preview4")] - Broken: required assets don't exist in blob storage
        [InlineData("release/5.0.1xx-preview5")]
        [InlineData("release/5.0.1xx-preview6")]
        [InlineData("release/5.0.1xx-preview7")]
        [InlineData("release/5.0.1xx-preview8")]
        [InlineData("release/5.0.1xx-preview8", true)]
        public void WhenChannelResolvesToASpecificSDKVersion(string channel, bool useFeedCredential = false)
        {
            var args = new List<string> { "-dryrun", "-channel", channel };

            string feedCredentials = default;
            if (useFeedCredential)
            {
                feedCredentials = Guid.NewGuid().ToString();
                args.Add("-feedCredential");
                args.Add(feedCredentials);
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().HaveStdOutMatching(@"URL\s#0\s-\s(legacy|primary|aka\.ms):\shttps://");

            //  Channel should be translated to a specific SDK version
            commandResult.Should().HaveStdOutContainingIgnoreCase("-version");

            if (useFeedCredential)
            {
                commandResult.Should().NotHaveStdOutContainingIgnoreCase(feedCredentials);
            }
        }

        [Theory]
        [InlineData("5.0.1", "WindowsDesktop")]
        [InlineData("3.1.10", "Runtime")]
        public void CanResolveCorrectLocationBasedOnVersion(string version, string location)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //do not run windowsdesktop test on Linux environment
                return;
            }
            string expectedLinkLog = $"Constructed primary named payload URL: {Environment.NewLine}https://dotnetcli.azureedge.net/dotnet/{location}/{version}";
            var args = new string[] { "-version", version, "-runtime", "windowsdesktop", "-verbose", "-dryrun" };
            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass().And.HaveStdOutContaining(expectedLinkLog);
        }

        [Theory]
        [InlineData("release/2.6.1xx")]
        [InlineData("4.8.2")]
        [InlineData("abcdefg")]
        public void WhenInvalidChannelWasUsed(string channel)
        {
            string feedCredentials = Guid.NewGuid().ToString();
            var args = new[] { "-dryrun", "-channel", channel, "-internal", "-feedCredential", feedCredentials };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Fail();
            commandResult.Should().HaveStdErrContaining("Failed to resolve the exact version number.");
            commandResult.Should().NotHaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().NotHaveStdOutContainingIgnoreCase(feedCredentials);
            commandResult.Should().NotHaveStdErrContainingIgnoreCase(feedCredentials);
        }

        [Fact]
        public void WhenInstallDirAliasIsUsed()
        {
            var commandResult = CreateInstallCommand(new[] { "-DryRun", "-i", "installation_path" })
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");
            commandResult.Should().HaveStdOutMatching(@"URL\s#0\s-\s(legacy|primary|aka\.ms):\shttps://");

            // -i shouldn't be considered ambiguous on powershell.
            commandResult.Should().NotHaveStdOutContaining("the parameter name 'i' is ambiguous");
            // bash doesn't give error on ambiguity. The first occurance of the alias wins.

            //  -i should translate to -InstallDir
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                commandResult.Should().HaveStdOutContainingIgnoreCase("-InstallDir \"installation_path\"");
            }
            else
            {
                commandResult.Should().HaveStdOutContainingIgnoreCase("-install-dir \"installation_path\"");
            }
        }

        [Theory]
        [InlineData("1.0.5", "dotnet")]
        [InlineData("2.1.0", "aspnetcore")]
        [InlineData("6.0.100", null)]
        public async Task WhenAnExactVersionIsPassedToBash(string version, string runtime)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //do not run bash test on Windows environment
                return;
            }
            string[] args;

            if (string.IsNullOrWhiteSpace(runtime))
            {
                args = new string[] {
                    "-version", version,
                    "-runtimeid", "osx",
                    "--os", "osx",
                    "-installdir", "dotnet-sdk",
                    "-dryrun" };
            }
            else
            {
                args = new string[] {
                    "-version", version,
                    "-runtimeid", "osx",
                    "-runtime", runtime,
                    "--os", "osx",
                    "-installdir", "dotnet-sdk",
                    "-dryrun" };
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdErr();
            await Verify(commandResult.StdOut).UseParameters(version, runtime);
        }

        [Theory]
        [InlineData("1.0.5", "dotnet")]
        [InlineData("2.1.0", "aspnetcore")]
        [InlineData("6.0.100", null)]
        public async Task WhenAnExactVersionIsPassedToPowershell(string version, string? runtime)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //do not run powershell test on Linux environment
                return;
            }
            string[] args;

            if (string.IsNullOrWhiteSpace(runtime))
            {
                args = new string[] {
                    "-version", version,
                    "-installdir", "dotnet-sdk",
                    "-dryrun" };
            }
            else
            {
                args = new string[] {
                    "-version", version,
                    "-runtime", runtime,
                    "-installdir", "dotnet-sdk",
                    "-dryrun" };
            }

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdErr();
            await Verify(commandResult.StdOut).UseParameters(version, runtime);
        }

        [Fact]
        public void ShowScriptPurposeBlurbBash()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //do not run bash test on Windows environment
                return;
            }

            const string IntroBlurb = @"
    .NET Tools Installer
Usage: dotnet-install.sh [-c|--channel <CHANNEL>] [-v|--version <VERSION>] [-p|--prefix <DESTINATION>]
       dotnet-install.sh -h|-?|--help
dotnet-install.sh is a simple command line interface for obtaining dotnet cli.
    Note that the intended use of this script is for Continuous Integration (CI) scenarios, where:
    - The SDK needs to be installed without user interaction and without admin rights.
    - The SDK installation doesn't need to persist across multiple CI runs.
    To set up a development environment or to run apps, use installers rather than this script. Visit https://dotnet.microsoft.com/download to get the installer.
";

            string[] args = new string[] {
                "-help" };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdErr();
            commandResult.StdOut.StartsWith(IntroBlurb);
        }

        [Fact]
        public void ShowScriptPurposeBlurbBashPowershellVerbose()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //do not run powershell test on Linux environment
                return;
            }


            const string IntroBlurb = @"
VERBOSE: dotnet-install: Note that the intended use of this script is for Continuous Integration (CI) scenarios, where:
VERBOSE: dotnet-install: - The SDK needs to be installed without user interaction and without admin rights.
VERBOSE: dotnet-install: - The SDK installation doesn't need to persist across multiple CI runs.
VERBOSE: dotnet-install: To set up a development environment or to run apps, use installers rather than this script. 
";

            string[] args = new string[] {
                "-version", "6.0.100",
                "-installdir", "dotnet-sdk",
                "-dryrun",
                "-verbose" };
            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdErr();
            commandResult.StdOut.StartsWith(IntroBlurb);
        }
    }
}