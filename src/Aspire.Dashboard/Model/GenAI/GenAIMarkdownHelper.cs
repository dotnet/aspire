// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Markdown;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model.GenAI;

public static class GenAIMarkdownHelper
{
    public static MarkdownProcessor CreateProcessor(IStringLocalizer<ControlsStrings> loc)
    {
        // GenAI responses are untrusted, so only allow safe schemes.
        return new MarkdownProcessor(loc, safeUrlSchemes: MarkdownHelpers.SafeUrlSchemes, extensions: []);
    }
}
