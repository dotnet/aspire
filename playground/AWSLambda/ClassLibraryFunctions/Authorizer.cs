// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace ClassLibraryFunctions;

public class Authorizer
{
    public APIGatewayCustomAuthorizerResponse Authorize(APIGatewayCustomAuthorizerRequest request,
        ILambdaContext context)
    {
        return new APIGatewayCustomAuthorizerResponse
        {
            PrincipalID = "Aspire",
            PolicyDocument = new APIGatewayCustomAuthorizerPolicy
            {
                Statement =
                [
                    new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement { Effect = "Allow" }
                ]
            },
        };
    }
}
