// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MyFrontend.Services;

public class PetStoreClient(HttpClient client)
{
    public async Task<IReadOnlyList<Pet>?> GetPets()
    {
        var pets = await client.GetFromJsonAsync<IReadOnlyList<Pet>>("v2/pet/findByStatus?status=available");
        return pets!.Take(10).ToList().AsReadOnly();
    }
}

public record Pet(long Id, string Name)
{
}
