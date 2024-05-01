// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ExecutableLambdaHttpApi;

public interface IDatabaseRepository
{
    Task<DatabaseRecord> GetById(string id);
}

public class DatabaseRepository : IDatabaseRepository
{
    public Task<DatabaseRecord> GetById(string id)
    {
        return Task.FromResult(new DatabaseRecord { Id = id, Title = "HelloWorld" });
    }
}

public class DatabaseRecord
{
    public required string Id { get; init; }
    public required string Title { get; init; }
}
