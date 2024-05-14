// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Milvus.Client;

namespace Aspire.Milvus.Client.Tests;
public class AspireMilvusHelpers
{
    public const string TestingEndpoint = "http://localhost:19530";
    public const string TestingAuth = "root:Milvus";

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
            var client = new MilvusClient(new Uri(TestingEndpoint),apiKey:TestingAuth);
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
