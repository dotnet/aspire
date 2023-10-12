// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public static class ServiceBindingAnnotationExtensions
{
    public static ServiceBindingAnnotation AsHttp2(this ServiceBindingAnnotation binding)
    {
        binding.Transport = "http2";
        binding.UriScheme = "https";
        return binding;
    }

    public static ServiceBindingAnnotation AsExternal(this ServiceBindingAnnotation binding)
    {
        binding.IsExternal = true;
        return binding;
    }

}
