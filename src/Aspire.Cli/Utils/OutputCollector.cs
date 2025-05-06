// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

internal sealed class OutputCollector
{
    private readonly CircularBuffer<(string Stream, string Line)> _lines = new(10000); // 10k lines.

    public void AppendOutput(string line)
    {
        _lines.Add(("stdout", line));
    }

    public void AppendError(string line)
    {
        _lines.Add(("stderr", line));
    }

    public IEnumerable<(string Stream, string Line)> GetLines()
    {
        return _lines;
    }
}