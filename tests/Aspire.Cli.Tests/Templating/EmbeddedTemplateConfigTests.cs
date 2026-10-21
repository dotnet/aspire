// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Templating;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Templating;

public class EmbeddedTemplateConfigTests
{
    public static TheoryData<string> EmbeddedTemplateConfigResourceNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in typeof(CliTemplateFactory).Assembly.GetManifestResourceNames().Order(StringComparer.Ordinal))
        {
            if (name.EndsWith(".aspire.config.json", StringComparison.Ordinal) ||
                name.EndsWith(".apphost.run.json", StringComparison.Ordinal))
            {
                data.Add(name);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EmbeddedTemplateConfigResourceNames))]
    public void EmbeddedTemplateConfig_UsesLfLineBreaksAndSingleFinalNewline(string resourceName)
    {
        using var stream = typeof(CliTemplateFactory).Assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        // CliTemplateFactory writes embedded files to disk without rewriting line breaks, so these bytes are
        // what `aspire new` scaffolds. .gitattributes pins them to LF so a CRLF checkout (core.autocrlf on
        // Windows) cannot leak into a CLI build. A CRLF here means this working copy was not checked out with
        // the pin in effect (git does not rewrite unchanged files when .gitattributes changes): delete the
        // file under src/Aspire.Cli/Templating/Templates and run `git checkout -- <path>`.
        LineEndingAssert.IsLfWithSingleFinalNewline(reader.ReadToEnd());
    }
}
