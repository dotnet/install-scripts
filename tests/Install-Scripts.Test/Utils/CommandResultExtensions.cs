// Copyright (c) Microsoft. All rights reserved.
// Taken from https://github.com/dotnet/sdk/

using static Install_Scripts.Test.Utils.InstallDotNetCommand;

namespace Microsoft.NET.TestFramework.Assertions
{
    public static class CommandResultExtensions
    {
        public static CommandResultAssertions Should(this CommandResult commandResult)
        {
            return new CommandResultAssertions(commandResult);
        }
    }
}
