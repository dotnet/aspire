// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_TESTS
namespace Aspire.Components.Common.Tests;
#else
namespace Aspire.Components.Common;
#endif

#if BUILDING_TESTS
public
#endif
sealed class ContainerImageTags
{
    public static (string image, string tag) Redis = ("redis", "7.2.4");
}
