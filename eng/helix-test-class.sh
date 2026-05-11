#!/bin/bash
# Runs tests filtered to a single test class on Helix (Unix).
# Usage: helix-test-class.sh Install_Scripts.Test.AkaMsLinksTests
#
# Sets TestRunnerAdditionalArguments as an environment variable so MSBuild
# picks it up without needing %20 escaping through the shell pipeline.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export TestRunnerAdditionalArguments="-class $1"
"$SCRIPT_DIR/../build.sh" --test /p:UseMicrosoftTestingPlatformRunner=false
