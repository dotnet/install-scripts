// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VerifyTests;
using Xunit;
using Microsoft.NET.TestFramework.Assertions;

namespace Microsoft.DotNet.InstallationScript.Tests;

public class GivenThatIWantToTestHelpOutput : TestBase
{
    public GivenThatIWantToTestHelpOutput(VerifySettings settings = null)
            : base(settings) { }

    [Fact]
    public void InvokingHelpTriggersHelpForPowershell()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Testing powershell output is intended for Windows
            return;
        }

        var commandResult = CreateInstallCommand(new List<string> { "-help" })
                        .CaptureStdOut()
                        .CaptureStdErr()
                        .Execute();
        
        commandResult.Should().Pass();
        // Verify is not used since the $PSCommandPath could differ from machine to machine, hence using explicit verification of the examples from the script file
        commandResult.Should().HaveStdOutContaining("dotnet-install.ps1 -Version 7.0.401");
        commandResult.Should().HaveStdOutContaining("Installs the .NET SDK version 7.0.401");
        commandResult.Should().HaveStdOutContaining("dotnet-install.ps1 -Channel 8.0 -Quality GA");
        commandResult.Should().HaveStdOutContaining("Installs the latest GA (general availability) version of the .NET 8.0 SDK");
        
    }

    [Fact]
    public async void InvokingHelpTriggersHelpForBash()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Testing bash output is intended for non-Windows
            return;
        }

        var commandResult = CreateInstallCommand(new List<string> { "--help" })
                        .CaptureStdOut()
                        .CaptureStdErr()
                        .Execute();
        
        commandResult.Should().Pass();
        await Verify(commandResult.StdOut);
    }
}