# 1-Pager Design: Make script installation faster for Windows environment #

## Problem Summary ##
Install-scripts runs 3 times slower in Windows compare to Linux environment. This behaviour can be tracked during DownloadFile or Extract-Dotnet-Package functions invocation. Measurement results can be observed during [install-scripts tests run](https://dev.azure.com/dnceng/internal/_build?definitionId=1019).
Also, one of the customers [benchmarked](https://github.com/actions/setup-dotnet/issues/260#issue-1105497391) this trend on his local machine.

We want to investigate the ways of improving install-scripts run performance and align the speed ratio between a run on Windows vs Linux machines.

## Open Items ##
None

## High Level Design ##

### Requirements ###
Windows speed should be comparable to Linux:
- Download time shouldn't exceed ~1 minute on an average machine
- [NTH] Regressions should be detected on CI using perf tests

### Non-goals ###
Network and CDN changes to the Azure storages in order to improve download speeds are not part of this story

### Design Details ###
|Requirement|Design|Estimate|
|:---|:---|:---|
|Define the part is degrading the speed the most|Benchmark install-scripts run for Windows environment, prepare a document with measurements results|1 person day|
|Investigate and suggest new ways to improve speed for Windows machines| Look thorough internals of existing methods, define soft spots and search for alternative solutions.|3 person day|
|Implementing the determined improvements|Apply changes to the script|4 person day|
|Collect metrics after applied changes|Need to go to the second step if requirements are not met|1 person day|
|[NTH] Add performance tests|?? No idea how to approach this thing right now||
|Test the latest changes on internal repos to ensure stability|Check if the changes didn't introduce any regression issues|1 person day|
|Deploy the updated scripts to dot.net website|Publish changes|0.5 person day|

### Output ###
Faster install speeds using dotnet-install.ps1 script and reduced CI costs across all install script customers.
