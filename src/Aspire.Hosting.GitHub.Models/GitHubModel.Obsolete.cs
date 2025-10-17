// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Aspire.Hosting.GitHub;

// This file contains obsolete elements kept for backward compatibility.

public partial class GitHubModel
{
    public static partial class Core42
    {
        /// <inheritdoc cref="Jais30bChat"/>
        [Obsolete("Use Jais30bChat instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel JAIS30bChat = new() { Id = "core42/jais-30b-chat" };
    }

    public static partial class Microsoft
    {
        /// <inheritdoc cref="MaiDSR1"/>
        [Obsolete("Use MaiDSR1 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel MAIDSR1 = new() { Id = "microsoft/mai-ds-r1" };
    }

    public static partial class OpenAI
    {
        /// <inheritdoc cref="OpenAIGpt41"/>
        [Obsolete("Use OpenAIGpt41 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel OpenAIGPT41 = new() { Id = "openai/gpt-4.1" };

        /// <inheritdoc cref="OpenAIGpt41Mini"/>
        [Obsolete("Use OpenAIGpt41Mini instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel OpenAIGPT41Mini = new() { Id = "openai/gpt-4.1-mini" };

        /// <inheritdoc cref="OpenAIGpt41Nano"/>
        [Obsolete("Use OpenAIGpt41Nano instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel OpenAIGPT41Nano = new() { Id = "openai/gpt-4.1-nano" };

        /// <inheritdoc cref="OpenAIGpt4o"/>
        [Obsolete("Use OpenAIGpt4o instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel OpenAIGPT4o = new() { Id = "openai/gpt-4o" };

        /// <inheritdoc cref="OpenAIGpt4oMini"/>
        [Obsolete("Use OpenAIGpt4oMini instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly GitHubModel OpenAIGPT4oMini = new() { Id = "openai/gpt-4o-mini" };
    }
}
