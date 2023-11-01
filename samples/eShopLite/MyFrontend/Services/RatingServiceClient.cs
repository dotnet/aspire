// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace MyFrontend.Services;

public class RatingServiceClient(HttpClient client)
{
    public Task<string> GetRatingAsync(int productId)
    {
        return client.GetStringAsync($"averagerating/{productId.ToString(CultureInfo.InvariantCulture)}");
    }
}
