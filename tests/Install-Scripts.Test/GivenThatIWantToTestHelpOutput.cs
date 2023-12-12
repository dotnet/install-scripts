// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.InteropServices;
using VerifyTests;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests;

public class GivenThatIWantToTestHelpOutput : TestBase
{
    public GivenThatIWantToTestHelpOutput(VerifySettings settings = null)
            : base(settings) { }

    [Fact]
    public async void InvokingHelpTriggersHelpForPowershell()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Testing powershell output is intended for Windows
            return;
        }

        var commandResult = CreateInstallCommand("--help")
                        .CaptureStdOut()
                        .CaptureStdErr()
                        .Execute();
        await Verify(commandResult.StdOut);
    }

    [Fact]
    public async void InvokingHelpTriggersHelpForBash()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Testing bash output is intended for non-Windows
            return;
        }

        var commandResult = CreateInstallCommand("--help")
                        .CaptureStdOut()
                        .CaptureStdErr()
                        .Execute();
        await Verify(commandResult.StdOut);
    }
}