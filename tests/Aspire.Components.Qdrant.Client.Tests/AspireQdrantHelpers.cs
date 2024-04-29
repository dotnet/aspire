// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Qdrant.Client;

namespace Aspire.Qdrant.Client.Tests;
public static class AspireQdrantHelpers
{
    public const string TestingEndpoint = "http://localhost:6334";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);
    public static bool CanConnectToServer => s_canConnectToServer.Value;

    public static void SkipIfCanNotConnectToServer()
    {
        if (!CanConnectToServer)
        {
            throw new SkipTestException("Unable to connect to the server.");
        }
    }

    private static bool GetCanConnect()
    {
        try
        {
            var client = new QdrantClient(new Uri(TestingEndpoint));
            client.ListCollectionsAsync().Wait();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
