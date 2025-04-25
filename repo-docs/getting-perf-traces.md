# Wall-clock time investigations

## Collect the trace
Collect the trace using PerfView (https://github.com/microsoft/perfview/releases). Use Collect menu, then Collect again, to open the collection dialog and make the following changes from defaults:

1. If you do not intend to share the trace with anyone, uncheck the "Zip" and "Merge" option.
1. Increase Circular MB to 8192.
1. Check the "Thread Time" checkbox.
1. Expand Advanced Options panel and make sure you have Kernel Base, Cpu Samples, File I/O, .NET, and Task (TPL) options checked.
1. In "Additional Providers" add `*Microsoft-Aspire-Hosting` (note the asterisk before the .NET Aspire provider name!).

Once you are ready, git "Start Collection" button and run your scenario.

When done with the scenario, hit "Stop Collection". Wait for PerfView to finish merging and analyzing data (the "working" status bar stops flashing).

## Verify that the trace contains data .NET Aspire data

This is an optional step, but if you are wondering if your trace has been captured properly, you can check the following:

1. Open the trace (usually named PerfViewData.etl, if you haven't changed the name) and double click Events view. Verify you have a bunch of events from Microsoft-Aspire-Hosting provider.
