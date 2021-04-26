// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Xunit;
using System.Collections.Generic;
using Microsoft.NET.TestFramework.Assertions;
using FluentAssertions;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class GivenThatIWantToGetTheSdkLinksFromAScript : TestBase
    {
        // This is not how credentials look like, this is just a testing string.
        private const string _feedCredential = "478a920c-2217-49f2-9c31-2fc3b4fef7cb";

        [Theory]
        [InlineData("InstallationScriptTests.json")]
        [InlineData("InstallationScriptTestsWithMultipleSdkFields.json")]
        [InlineData("InstallationScriptTestsWithVersionFieldInTheMiddle.json")]
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

            //  Runtime should resolve to the correct 'type'
            commandResult.Should().HaveStdOutContainingIgnoreCase("-runtime");
            commandResult.Should().HaveStdOutContainingIgnoreCase(runtimeType);
        }


        [Theory]
        [InlineData("3.1", _feedCredential)]
        [InlineData("5.0.2xx", _feedCredential)]
        [InlineData("LTS", "?" + _feedCredential)]
        [InlineData("release/1.0.0", "?" + _feedCredential)]
        [InlineData("master", _feedCredential)]
        public void WhenFeedCredentialParameterIsPassedToInstallScripts(string channel, string feedCredential)
        {
            var args = new List<string> { "-dryrun", "-channel", channel, "-feedcredential", feedCredential, "-verbose" };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");

            // Checking format of the link after applying feed credential
            string linkWithCredentialsExpression = @"Primary named payload URL: [a-zA-Z0-9:/.-]*\?" + feedCredential;
            commandResult.Should().HaveStdOutContaining(output => Regex.IsMatch(output, linkWithCredentialsExpression));
        }

        [Theory]
        [InlineData("1.0", "dotnet")]
        [InlineData("1.1", "dotnet")]
        [InlineData("2.0", "dotnet")]
        [InlineData("2.2", "dotnet")]
        [InlineData("3.0", "dotnet")]
        [InlineData("3.1", "dotnet")]
        [InlineData("5.0", "dotnet")]
        [InlineData("Current", "dotnet")]
        [InlineData("LTS", "dotnet")]
        [InlineData("master", "dotnet")]
        [InlineData("release/2.1", "dotnet")]
        [InlineData("release/2.2", "dotnet")]
        [InlineData("release/3.0", "dotnet")]
        [InlineData("release/3.1", "dotnet")]
        // [InlineData("release/5.0", "dotnet")] - Broken
        [InlineData("Current", "aspnetcore")]
        [InlineData("LTS", "aspnetcore")]
        //[InlineData("1.0", "aspnetcore")] - Broken
        //[InlineData("1.1", "aspnetcore")] - Broken
        //[InlineData("2.0", "aspnetcore")] - Broken
        [InlineData("2.2", "aspnetcore")]
        [InlineData("3.0", "aspnetcore")]
        [InlineData("3.1", "aspnetcore")]
        [InlineData("5.0", "aspnetcore")]
        [InlineData("master", "aspnetcore")]
        [InlineData("2.2", "aspnetcore")]
        [InlineData("3.0", "aspnetcore")]
        [InlineData("3.1", "aspnetcore")]
        [InlineData("5.0", "aspnetcore")]
        [InlineData("release/2.1", "aspnetcore")]
        [InlineData("release/2.2", "aspnetcore")]
        //[InlineData("release/3.0", "aspnetcore")] - Broken
        //[InlineData("release/3.1", "aspnetcore")] - Broken
        //[InlineData("release/5.0", "aspnetcore")] - Broken 
        [InlineData("Current", "windowsdesktop")]
        [InlineData("LTS", "windowsdesktop")]
        [InlineData("3.0", "windowsdesktop")]
        [InlineData("3.1", "windowsdesktop")]
        [InlineData("5.0", "windowsdesktop")]
        [InlineData("master", "windowsdesktop")]
        public void WhenChannelResolvesToASpecificRuntimeVersion(string channel, string runtimeType)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && runtimeType == "windowsdesktop")
            {
                //do not run windowsdesktop test on Linux environment
                return;
            }
            var args = new string[] { "-dryrun", "-channel", channel, "-runtime", runtimeType };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");

            //  Channel should be translated to a specific Runtime version
            commandResult.Should().HaveStdOutContainingIgnoreCase("-version");
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("1.1")]
        [InlineData("2.0")]
        [InlineData("2.1")]
        [InlineData("3.0")]
        [InlineData("3.1")]
        [InlineData("5.0")]
        [InlineData("Current")]
        [InlineData("LTS")]
        [InlineData("master")]
        [InlineData("release/1.0.0")]
        [InlineData("release/2.0.0")]
        [InlineData("release/2.0.2")]
        [InlineData("release/2.1.1xx")]
        [InlineData("release/2.1.2xx")]
        [InlineData("release/2.1.3xx")]
        [InlineData("release/2.1.4xx")]
        [InlineData("release/2.1.401")]
        [InlineData("release/2.1.5xx")]
        [InlineData("release/2.1.502")]
        [InlineData("release/2.1.6xx")]
        [InlineData("release/2.1.7xx")]
        [InlineData("release/2.1.8xx")]
        [InlineData("release/2.2.1xx")]
        [InlineData("release/2.2.2xx")]
        [InlineData("release/2.2.3xx")]
        [InlineData("release/2.2.4xx")]
        [InlineData("release/3.0.1xx")]
        [InlineData("release/5.0.1xx")]
        [InlineData("release/5.0.1xx-preview1")]
        [InlineData("release/5.0.1xx-preview2")]
        [InlineData("release/5.0.1xx-preview3")]
        // [InlineData("release/5.0.1xx-preview4")] - Broken: required assets don't exist in blob storage
        [InlineData("release/5.0.1xx-preview5")]
        [InlineData("release/5.0.1xx-preview6")]
        [InlineData("release/5.0.1xx-preview7")]
        [InlineData("release/5.0.1xx-preview8")]
        public void WhenChannelResolvesToASpecificSDKVersion(string channel)
        {
            var args = new string[] { "-dryrun", "-channel", channel };

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            //  Standard 'dryrun' criterium
            commandResult.Should().Pass();
            commandResult.Should().NotHaveStdOutContaining("dryrun");
            commandResult.Should().HaveStdOutContaining("Repeatable invocation:");

            //  Channel should be translated to a specific SDK version
            commandResult.Should().HaveStdOutContainingIgnoreCase("-version");
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
            var args = new string[] { "-version", version, "-runtime", "windowsdesktop", "-verbose", "-dryrun"};
            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().Pass().And.HaveStdOutContaining(expectedLinkLog);
        } 

    }
}