// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class IndexerOperationsTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TestCallbackInvoker _callbackInvoker;
    private readonly RpcOperations _operations;

    public IndexerOperationsTests()
    {
        _objectRegistry = new ObjectRegistry();
        _callbackInvoker = new TestCallbackInvoker();
        _operations = new RpcOperations(_objectRegistry, _callbackInvoker);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _operations.DisposeAsync();
    }

    #region List Operations

    [Fact]
    public void GetIndexer_ListWithPrimitives_ReturnsCorrectItem()
    {
        var list = new List<int> { 10, 20, 30, 40, 50 };
        var id = _objectRegistry.Register(list);

        for (int i = 0; i < list.Count; i++)
        {
            var index = JsonDocument.Parse($"{i}").RootElement;
            var result = _operations.GetIndexer(id, index);
            Assert.Equal(list[i], result);
        }
    }

    [Fact]
    public void GetIndexer_ListWithStrings_ReturnsCorrectItem()
    {
        var list = new List<string> { "alpha", "beta", "gamma" };
        var id = _objectRegistry.Register(list);

        var index = JsonDocument.Parse("1").RootElement;
        var result = _operations.GetIndexer(id, index);

        Assert.Equal("beta", result);
    }

    [Fact]
    public void GetIndexer_ListWithComplexObjects_MarshallesResult()
    {
        var items = new List<ComplexItem>
        {
            new() { Name = "first", Value = 1 },
            new() { Name = "second", Value = 2 }
        };
        var id = _objectRegistry.Register(items);

        var index = JsonDocument.Parse("0").RootElement;
        var result = _operations.GetIndexer(id, index);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Contains("ComplexItem", dict["$type"]!.ToString());
        Assert.True(dict.ContainsKey("$id"));
    }

    [Fact]
    public void GetIndexer_ListNegativeIndex_ThrowsOutOfRange()
    {
        var list = new List<string> { "only" };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("-1").RootElement;

        Assert.Throws<ArgumentOutOfRangeException>(() => _operations.GetIndexer(id, index));
    }

    [Fact]
    public void GetIndexer_ListIndexBeyondCount_ThrowsOutOfRange()
    {
        var list = new List<string> { "a", "b" };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("10").RootElement;

        Assert.Throws<ArgumentOutOfRangeException>(() => _operations.GetIndexer(id, index));
    }

    [Fact]
    public void GetIndexer_ListWithStringIndex_ThrowsInvalidOperation()
    {
        var list = new List<string> { "item" };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("\"notanumber\"").RootElement;

        Assert.Throws<InvalidOperationException>(() => _operations.GetIndexer(id, index));
    }

    [Fact]
    public void SetIndexer_ListWithPrimitives_UpdatesItem()
    {
        var list = new List<int> { 1, 2, 3 };
        var id = _objectRegistry.Register(list);

        var index = JsonDocument.Parse("1").RootElement;
        var value = JsonDocument.Parse("999").RootElement;

        _operations.SetIndexer(id, index, value);

        Assert.Equal(999, list[1]);
    }

    [Fact]
    public void SetIndexer_ListWithStrings_UpdatesItem()
    {
        var list = new List<string?> { "old1", "old2" };
        var id = _objectRegistry.Register(list);

        var index = JsonDocument.Parse("0").RootElement;
        var value = JsonDocument.Parse("\"new_value\"").RootElement;

        _operations.SetIndexer(id, index, value);

        Assert.Equal("new_value", list[0]);
    }

    [Fact]
    public void SetIndexer_ListWithProxyReference_ResolvesAndSetsObject()
    {
        var list = new List<object?> { null, null };
        var id = _objectRegistry.Register(list);

        var refObj = new ComplexItem { Name = "referenced", Value = 42 };
        var refId = _objectRegistry.Register(refObj);

        var index = JsonDocument.Parse("0").RootElement;
        var value = JsonDocument.Parse($"{{\"$id\": \"{refId}\"}}").RootElement;

        _operations.SetIndexer(id, index, value);

        Assert.Same(refObj, list[0]);
    }

    #endregion

    #region Dictionary Operations

    [Fact]
    public void GetIndexer_DictionaryStringKey_ReturnsValue()
    {
        var dict = new Dictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3
        };
        var id = _objectRegistry.Register(dict);

        var key = JsonDocument.Parse("\"two\"").RootElement;
        var result = _operations.GetIndexer(id, key);

        Assert.Equal(2, result);
    }

    [Fact]
    public void GetIndexer_DictionaryMissingKey_ReturnsNull()
    {
        var dict = new Dictionary<string, int> { ["exists"] = 1 };
        var id = _objectRegistry.Register(dict);

        var key = JsonDocument.Parse("\"missing\"").RootElement;
        var result = _operations.GetIndexer(id, key);

        Assert.Null(result);
    }

    [Fact]
    public void GetIndexer_DictionaryComplexValue_MarshallesResult()
    {
        var dict = new Dictionary<string, ComplexItem>
        {
            ["item"] = new ComplexItem { Name = "test", Value = 100 }
        };
        var id = _objectRegistry.Register(dict);

        var key = JsonDocument.Parse("\"item\"").RootElement;
        var result = _operations.GetIndexer(id, key);

        Assert.IsType<Dictionary<string, object?>>(result);
        var marshalled = (Dictionary<string, object?>)result!;
        Assert.Contains("ComplexItem", marshalled["$type"]!.ToString());
        Assert.True(marshalled.ContainsKey("$id"));
    }

    [Fact]
    public void SetIndexer_DictionaryNewKey_AddsEntry()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);

        var key = JsonDocument.Parse("\"newkey\"").RootElement;
        var value = JsonDocument.Parse("\"newvalue\"").RootElement;

        _operations.SetIndexer(id, key, value);

        Assert.Equal("newvalue", dict["newkey"]);
    }

    [Fact]
    public void SetIndexer_DictionaryExistingKey_UpdatesEntry()
    {
        var dict = new Dictionary<string, object?> { ["key"] = "old" };
        var id = _objectRegistry.Register(dict);

        var key = JsonDocument.Parse("\"key\"").RootElement;
        var value = JsonDocument.Parse("\"new\"").RootElement;

        _operations.SetIndexer(id, key, value);

        Assert.Equal("new", dict["key"]);
    }

    [Fact]
    public void SetIndexer_DictionaryWithNumericKey_HandlesCorrectly()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);

        // Numeric keys get converted to string
        var key = JsonDocument.Parse("123").RootElement;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        _operations.SetIndexer(id, key, value);

        Assert.Equal("value", dict["123"]);
    }

    [Fact]
    public void SetIndexer_DictionaryWithProxyReference_ResolvesAndSetsObject()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);

        var refObj = new ComplexItem { Name = "ref", Value = 999 };
        var refId = _objectRegistry.Register(refObj);

        var key = JsonDocument.Parse("\"mykey\"").RootElement;
        var value = JsonDocument.Parse($"{{\"$id\": \"{refId}\"}}").RootElement;

        _operations.SetIndexer(id, key, value);

        Assert.Same(refObj, dict["mykey"]);
    }

    #endregion

    #region String Key Operations

    [Fact]
    public void SetIndexerByStringKey_Dictionary_SetsValue()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);

        var value = JsonDocument.Parse("\"value\"").RootElement;
        _operations.SetIndexerByStringKey(id, "key", value);

        Assert.Equal("value", dict["key"]);
    }

    [Fact]
    public void SetIndexerByStringKey_ListWithNumericKey_SetsValue()
    {
        var list = new List<object?> { null, null, null };
        var id = _objectRegistry.Register(list);

        var value = JsonDocument.Parse("\"middle\"").RootElement;
        _operations.SetIndexerByStringKey(id, "1", value);

        Assert.Equal("middle", list[1]);
    }

    [Fact]
    public void SetIndexerByStringKey_ResolvesProxyReference()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);

        var refObj = new ComplexItem { Name = "ref", Value = 1 };
        var refId = _objectRegistry.Register(refObj);

        // Pass an object with $id to simulate proxy reference
        var value = JsonDocument.Parse($"{{\"$id\": \"{refId}\"}}").RootElement;
        _operations.SetIndexerByStringKey(id, "key", value);

        Assert.Same(refObj, dict["key"]);
    }

    [Fact]
    public void GetIndexerByStringKey_Dictionary_ReturnsValue()
    {
        var dict = new Dictionary<string, int> { ["mykey"] = 42 };
        var id = _objectRegistry.Register(dict);

        var result = _operations.GetIndexerByStringKey(id, "mykey");

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetIndexerByStringKey_DictionaryMissingKey_ReturnsNull()
    {
        var dict = new Dictionary<string, int> { ["exists"] = 1 };
        var id = _objectRegistry.Register(dict);

        var result = _operations.GetIndexerByStringKey(id, "missing");

        Assert.Null(result);
    }

    [Fact]
    public void GetIndexerByStringKey_NonIndexable_ThrowsInvalidOperation()
    {
        var obj = new ComplexItem { Name = "test" };
        var id = _objectRegistry.Register(obj);

        Assert.Throws<InvalidOperationException>(() =>
            _operations.GetIndexerByStringKey(id, "key"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetIndexer_EmptyList_ThrowsOutOfRange()
    {
        var list = new List<string>();
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("0").RootElement;

        Assert.Throws<ArgumentOutOfRangeException>(() => _operations.GetIndexer(id, index));
    }

    [Fact]
    public void GetIndexer_NonIndexableObject_ThrowsInvalidOperation()
    {
        var obj = new ComplexItem { Name = "not indexable" };
        var id = _objectRegistry.Register(obj);
        var index = JsonDocument.Parse("0").RootElement;

        Assert.Throws<InvalidOperationException>(() => _operations.GetIndexer(id, index));
    }

    [Fact]
    public void SetIndexer_NonIndexableObject_ThrowsInvalidOperation()
    {
        var obj = new ComplexItem { Name = "not indexable" };
        var id = _objectRegistry.Register(obj);
        var index = JsonDocument.Parse("0").RootElement;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        Assert.Throws<InvalidOperationException>(() => _operations.SetIndexer(id, index, value));
    }

    #endregion

    #region Test Classes

    private sealed class ComplexItem
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    #endregion
}
