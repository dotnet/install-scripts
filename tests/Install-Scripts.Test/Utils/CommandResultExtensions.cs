// Copyright (c) Microsoft. All rights reserved.
// Taken from https://github.com/dotnet/sdk/

using static Install_Scripts.Test.Utils.InstallDotNetCommand;

namespace Microsoft.NET.TestFramework.Assertions
{
    internal static class CommandResultExtensions
    {
        internal static CommandResultAssertions Should(this CommandResult commandResult) => new CommandResultAssertions(commandResult);
    }
}
