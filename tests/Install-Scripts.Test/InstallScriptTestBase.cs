// Copyright (c) Microsoft. All rights reserved.

using Install_Scripts.Test.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Abstract base class providing shared test data, helpers, and lifecycle management
    /// for .NET install script integration tests. Keeping SDK and runtime tests in separate
    /// derived classes enables xunit to run them as parallel test collections.
    /// </summary>
    public abstract class InstallScriptTestBase : IDisposable
    {
        /// <summary>
        /// All the channels that will be tested.
        /// </summary>
        protected static readonly IReadOnlyList<(string channel, string versionRegex, Quality quality)> _channels =
            new List<(string, string, Quality)>()
            {
                ("2.1", "2\\.1\\..*", Quality.None),
                ("2.2", "2\\.2\\..*", Quality.None),
                ("3.0", "3\\.0\\..*", Quality.None),
                ("3.1", "3\\.1\\..*", Quality.None),
                ("5.0", "5\\.0\\..*", Quality.None),
                ("6.0", "6\\.0\\..*", Quality.Daily),
                ("6.0", "6\\.0\\..*", Quality.None),
                ("7.0", "7\\.0\\..*", Quality.None),
                ("7.0", "7\\.0\\..*", Quality.Ga),
                ("8.0", "8\\.0\\..*", Quality.None),
                ("8.0", "8\\.0\\..*", Quality.Ga),
                ("STS", "9\\.0\\..*", Quality.None),
                ("9.0", "9\\.0\\..*", Quality.None),
                ("9.0", "9\\.0\\..*", Quality.Ga),
                ("LTS", "10\\.0\\..*", Quality.None),
                ("10.0", "10\\.0\\..*", Quality.None),
                ("10.0", "10\\.0\\..*", Quality.Ga),
                ("11.0", "11\\.0\\..*", Quality.Preview),
            };

        /// <summary>
        /// All the branches in runtime repos to be tested.
        /// </summary>
        protected static readonly IReadOnlyList<(string branch, string versionRegex, Quality quality)> _runtimeBranches =
            new List<(string, string, Quality)>()
            {
                ("release/2.1", "2\\.1\\..*", Quality.None),
                ("release/2.2", "2\\.2\\..*", Quality.None),
                ("release/3.0", "3\\.0\\..*", Quality.None),
                ("release/3.1", "3\\.1\\..*", Quality.None),
                // ("release/5.0", "5\\.0\\..*", Quality.None), Broken scenario
                // Branches are no longer supported starting 6.0, but there are channels that correspond to branches.
                // this storage account does not allow public access
                // ("6.0-preview2", "6\\.0\\..*", Quality.Daily | Quality.Signed),
                ("6.0-preview3", "6\\.0\\..*", Quality.Daily),
                ("6.0-preview4", "6\\.0\\..*", Quality.Daily),
                ("6.0", "6\\.0\\..*", Quality.None),
                ("7.0", "7\\.0\\..*", Quality.None),
                ("8.0", "8\\.0\\..*", Quality.None),
                ("9.0", "9\\.0\\..*", Quality.None),
                ("10.0", "10\\.0\\..*", Quality.None),
                ("11.0", "11\\.0\\..*", Quality.Preview),
            };

        /// <summary>
        /// All the branches in installer repo to be tested.
        /// </summary>
        protected static readonly IReadOnlyList<(string branch, string versionRegex, Quality quality)> _sdkBranches =
            new List<(string, string, Quality)>()
            {
                ("release/2.1.8xx", "2\\.1\\.8.*", Quality.None),
                ("release/2.2.4xx", "2\\.2\\.4.*", Quality.None),
                ("release/3.0.1xx", "3\\.0\\.1.*", Quality.None),
                // version is outdated. For more details check the link: https://github.com/dotnet/arcade/issues/10026
                // ("release/3.1.4xx", "3\\.1\\.4.*", Quality.None),
                ("release/5.0.1xx", "5\\.0\\.1.*", Quality.None),
                ("release/5.0.2xx", "5\\.0\\.2.*", Quality.None),
                // Branches are no longer supported starting 6.0, but there are channels that correspond to branches.
                // this storage account does not allow public access
                // ("6.0.1xx-preview2", "6\\.0\\.1.*", Quality.Daily | Quality.Signed),
                ("6.0.1xx-preview3", "6\\.0\\.1.*", Quality.Daily),
                ("6.0.1xx-preview4", "6\\.0\\.1.*", Quality.Daily),
                ("7.0.1xx", "7\\.0\\..*", Quality.Daily),
                ("8.0.1xx", "8\\.0\\..*", Quality.Daily),
                ("9.0.1xx", "9\\.0\\..*", Quality.Daily),
                ("10.0.1xx", "10\\.0\\..*", Quality.Daily),
                ("11.0.1xx", "11\\.0\\..*", Quality.Preview),
            };

        public static IEnumerable<object?[]> InstallSdkFromChannelTestCases
        {
            get
            {
                // Download SDK using branches as channels.
                foreach (var sdkBranchInfo in _sdkBranches)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(sdkBranchInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            sdkBranchInfo.branch,
                            quality,
                            sdkBranchInfo.versionRegex,
                        };
                    }
                }

                // Download SDK from darc channels.
                foreach (var channelInfo in _channels)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(channelInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            channelInfo.channel,
                            quality,
                            channelInfo.versionRegex,
                        };
                    }
                }
            }
        }

        public static IEnumerable<object?[]> InstallRuntimeFromChannelTestCases
        {
            get
            {
                // Download runtimes using branches as channels.
                foreach (var runtimeBranchInfo in _runtimeBranches)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(runtimeBranchInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            runtimeBranchInfo.branch,
                            quality,
                            runtimeBranchInfo.versionRegex,
                        };
                    }
                }

                // Download runtimes using darc channels.
                foreach (var channelInfo in _channels)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(channelInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            channelInfo.channel,
                            quality,
                            channelInfo.versionRegex,
                        };
                    }
                }
            }
        }

        /// <summary>
        /// The directory that the install script will install the .NET into.
        /// </summary>
        protected readonly string _sdkInstallationDirectory;

        protected readonly ITestOutputHelper outputHelper;

        /// <summary>
        /// Initializes a new test instance, creating a unique temporary installation directory.
        /// </summary>
        protected InstallScriptTestBase(ITestOutputHelper testOutputHelper)
        {
            outputHelper = testOutputHelper;

            _sdkInstallationDirectory = Path.Combine(
                Path.GetTempPath(),
                "InstallScript-Tests",
                Path.GetRandomFileName());

            // In case there are any files from previous runs, clean them up.
            try
            {
                Directory.Delete(_sdkInstallationDirectory, true);
            }
            catch (DirectoryNotFoundException)
            {
                // This is expected. Ignore the exception.
            }

            Directory.CreateDirectory(_sdkInstallationDirectory);
        }

        /// <summary>
        /// Disposes the instance, removing the temporary installation directory.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Directory.Delete(_sdkInstallationDirectory, true);
            }
            catch (DirectoryNotFoundException)
            {
                // Directory to cleanup may not be there if installation fails. Not an issue. Ignore the exception.
            }
            catch (UnauthorizedAccessException e)
            {
                throw new Exception($"Failed to remove {_sdkInstallationDirectory}", e);
            }
        }

        protected static IEnumerable<string> GetInstallScriptArgs(
            string? channel,
            string? runtime,
            string? quality,
            string? installDir,
            string? feedCredentials = null,
            bool verboseLogging = false,
            string? version = null)
        {
            if (!string.IsNullOrWhiteSpace(channel))
            {
                yield return "-Channel";
                yield return channel;
            }

            if (!string.IsNullOrWhiteSpace(installDir))
            {
                yield return "-InstallDir";
                yield return installDir;
            }

            if (!string.IsNullOrWhiteSpace(runtime))
            {
                yield return "-Runtime";
                yield return runtime;
            }

            if (!string.IsNullOrWhiteSpace(quality))
            {
                yield return "-Quality";
                yield return quality;
            }

            if (!string.IsNullOrWhiteSpace(feedCredentials))
            {
                yield return "-FeedCredential";
                yield return feedCredentials;
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                yield return "-Version";
                yield return version;
            }

            if (verboseLogging)
            {
                yield return "-Verbose";
            }
        }

        protected static IEnumerable<string> GetQualityOptionsFromFlags(Quality flags)
        {
            ulong flagsValue = (ulong)flags;

            if (flagsValue == 0)
            {
                yield break;
            }

            foreach (Quality quality in Enum.GetValues(typeof(Quality)))
            {
                ulong qualityValue = (ulong)quality;
                if (qualityValue == 0 || (qualityValue & (qualityValue - 1)) != 0)
                {
                    // No bits are set, or more than one bits are set
                    continue;
                }

                if ((flagsValue & qualityValue) != 0)
                {
                    yield return quality.ToString();
                }
            }
        }
    }
}
