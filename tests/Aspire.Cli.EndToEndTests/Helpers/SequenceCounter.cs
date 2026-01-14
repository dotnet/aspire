// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Automation;

namespace Aspire.Cli.EndToEndTests.Helpers;

public class SequenceCounter
{
    public int Value { get; private set; } = 1;

    public int Increment()
    {
        return ++Value;
    }
}

public static class SequenceCounterExtensions
{
    public static Hex1bTerminalInputSequenceBuilder WaitForSuccessPrompt(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(500);

        return builder.WaitUntil(snapshot =>
            {
                var successPromptSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" OK] $ ");

                var result = successPromptSearcher.Search(snapshot);
                return result.Count > 0;
            }, effectiveTimeout)
            .IncrementSequence(counter);
                
    }

    public static Hex1bTerminalInputSequenceBuilder IncrementSequence(this Hex1bTerminalInputSequenceBuilder builder, SequenceCounter counter)
    {
        return builder.WaitUntil(s =>
        {
           // Hack to pump the counter fluently.
           counter.Increment();
           return true;
        }, TimeSpan.FromSeconds(1));
    }
}