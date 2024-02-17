// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Aspire.Dashboard.Otlp.Security;

internal sealed class OtlpInterceptor : Interceptor
{
    // Only need to override the UnaryServerHandler method, as the other method types are not used by OTLP.
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.Features.Get<IOtlpConnectionFeature>() == null)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "OTLP is not enabled on this connection."));
        }

        return base.UnaryServerHandler(request, context, continuation);
    }
}
