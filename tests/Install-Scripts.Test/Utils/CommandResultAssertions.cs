// Copyright (c) Microsoft. All rights reserved.
// Taken from https://github.com/dotnet/sdk/

using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using CommandResult = Install_Scripts.Test.Utils.DotNetCommand.CommandResult;

namespace Microsoft.NET.TestFramework.Assertions
{
    internal class CommandResultAssertions
    {
        private CommandResult _commandResult;

        internal CommandResultAssertions(CommandResult commandResult)
        {
            _commandResult = commandResult;
        }

        internal AndConstraint<CommandResultAssertions> ExitWith(int expectedExitCode)
        {
            _commandResult.ExitCode.Should().Be(expectedExitCode, AppendDiagnosticsTo($"Expected command to exit with {expectedExitCode} but it did not."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> Pass()
        {
            _commandResult.ExitCode.Should().Be(0, AppendDiagnosticsTo($"Expected command to pass but it did not."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> Fail()
        {
            _commandResult.ExitCode.Should().NotBe(0, AppendDiagnosticsTo($"Expected command to fail but it did not."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOut()
        {
            _commandResult.StdOut.Should().NotBeNullOrEmpty(AppendDiagnosticsTo("Command did not output anything to stdout"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOut(string expectedOutput)
        {
            _commandResult.StdOut.Should().Be(expectedOutput, AppendDiagnosticsTo($"Command did not output with Expected Output. Expected: {expectedOutput}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOutContaining(string pattern)
        {
            _commandResult.StdOut.Should().Contain(pattern, AppendDiagnosticsTo($"The command output did not contain expected result: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOutContaining(Func<string, bool> predicate, string description = "")
        {
            predicate(_commandResult.StdOut!).Should().BeTrue(AppendDiagnosticsTo($"The command output did not contain expected result: {description} {Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdOutContaining(string pattern)
        {
            _commandResult.StdOut.Should().NotContain(pattern, AppendDiagnosticsTo($"The command output contained a result it should not have contained: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdOutContainingIgnoreCase(string pattern)
        {
            _commandResult.StdOut.Should().NotContainEquivalentOf(pattern, AppendDiagnosticsTo($"The command output contained a result it should not have contained (ignoring case): {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOutContainingIgnoreSpaces(string pattern)
        {
            var commandResultNoSpaces = _commandResult.StdOut!.Replace(" ", "");
            commandResultNoSpaces.Should().Contain(pattern, AppendDiagnosticsTo($"The command output did not contain expected result: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOutContainingIgnoreCase(string pattern)
        {
            _commandResult.StdOut.Should().ContainEquivalentOf(pattern, AppendDiagnosticsTo($"The command output did not contain expected result (ignoring case): {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            _commandResult.StdOut.Should().MatchRegex(pattern, AppendDiagnosticsTo($"Matching the command output failed. Pattern: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdOutMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            _commandResult.StdOut.Should().NotMatchRegex(pattern, AppendDiagnosticsTo($"The command output matched a pattern it should not have contained: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdErr()
        {
            _commandResult.StdErr.Should().NotBeNullOrEmpty(AppendDiagnosticsTo("Command did not output anything to stderr."));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdErrContaining(string pattern)
        {
            _commandResult.StdErr.Should().Contain(pattern, AppendDiagnosticsTo($"The command error output did not contain expected result: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdErrContaining(string pattern)
        {
            _commandResult.StdErr.Should().NotContain(pattern, AppendDiagnosticsTo($"The command error output contained a result it should not have contained: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdErrContainingIgnoreCase(string pattern)
        {
            _commandResult.StdErr.Should().NotContainEquivalentOf(pattern, AppendDiagnosticsTo($"The command error output contained a result it should not have contained (ignoring case): {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveStdErrMatching(string pattern, RegexOptions options = RegexOptions.None)
        {
            _commandResult.StdErr.Should().MatchRegex(pattern, AppendDiagnosticsTo($"Matching the command error output failed. Pattern: {pattern}{Environment.NewLine}"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdOut()
        {
            _commandResult.StdOut.Should().BeNullOrEmpty(AppendDiagnosticsTo($"Expected command to not output to stdout but it did:"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> NotHaveStdErr()
        {
            _commandResult.StdErr.Should().BeNullOrEmpty(AppendDiagnosticsTo("Expected command to not output to stderr but it did:"));
            return new AndConstraint<CommandResultAssertions>(this);
        }

        private string AppendDiagnosticsTo(string s)
        {
            return s + $"{Environment.NewLine}" +
                       $"File Name: {_commandResult.StartInfo.FileName}{Environment.NewLine}" +
                       $"Arguments: {_commandResult.StartInfo.Arguments}{Environment.NewLine}" +
                       $"Exit Code: {_commandResult.ExitCode}{Environment.NewLine}" +
                       $"StdOut:{Environment.NewLine}{_commandResult.StdOut}{Environment.NewLine}" +
                       $"StdErr:{Environment.NewLine}{_commandResult.StdErr}{Environment.NewLine}"; ;
        }

        internal AndConstraint<CommandResultAssertions> HaveSkippedProjectCompilation(string skippedProject, string frameworkFullName)
        {
            _commandResult.StdOut!.Should().Contain($"Project {skippedProject} ({frameworkFullName}) was previously compiled. Skipping compilation.");

            return new AndConstraint<CommandResultAssertions>(this);
        }

        internal AndConstraint<CommandResultAssertions> HaveCompiledProject(string compiledProject, string frameworkFullName)
        {
            _commandResult.StdOut!.Should().Contain($"Project {compiledProject} ({frameworkFullName}) will be compiled");

            return new AndConstraint<CommandResultAssertions>(this);
        }
    }
}
