// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyTests;
using Xunit;
using Xunit.Sdk;

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
            throw SkipException.ForSkip("Testing powershell output is intended for Windows");
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
            throw SkipException.ForSkip("Testing bash output is intended for non-Windows");
        }

        var commandResult = CreateInstallCommand("--help")
                        .CaptureStdOut()
                        .CaptureStdErr()
                        .Execute();
        await Verify(commandResult.StdOut);
    }
}