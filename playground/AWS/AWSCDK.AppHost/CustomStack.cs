// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace AWSCDK.AppHost;

public class CustomStack : Stack
{

    public IBucket Bucket { get; }

    public CustomStack(Construct scope, string id)
        : base(scope, id)
    {
        Bucket = new Bucket(this, "Bucket");
    }

}
