// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace OrleansContracts;

public interface ICounterGrain : IGrainWithStringKey
{
    ValueTask<int> Increment();
    ValueTask<int> Get();
}
