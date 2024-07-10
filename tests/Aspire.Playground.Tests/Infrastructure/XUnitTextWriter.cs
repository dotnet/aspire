// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit.Abstractions;

namespace SamplesIntegrationTests.Infrastructure;

/// <summary>
/// A <see cref="TextWriter"/> that writes to an <see cref="ITestOutputHelper"/>.
/// </summary>
internal sealed class XUnitTextWriter(ITestOutputHelper output) : TextWriter
{
    private readonly StringBuilder _sb = new();

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value)
    {
        if (value == '\r' || value == '\n')
        {
            if (_sb.Length > 0)
            {
                output.WriteLine(_sb.ToString());
                _sb.Clear();
            }
        }
        else
        {
            _sb.Append(value);
        }
    }
}
