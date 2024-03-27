using Amazon;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

var builder = DistributedApplication.CreateBuilder(args);

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

/*var stack = builder.AddAWSCDK().AddStack(
        "Stack",
        app => new WebAppStack(app, "AspireWebAppStack", new WebAppStackProps()))
    .WithOutput("TableName", stack => stack.Table.TableName, "Table::TableName")
    .WithReference(awsConfig);*/

var stack = builder.AddAWSCDKStack("Stack").WithReference(awsConfig);
var table = stack.AddConstruct("Table", scope => new Table(scope, "Table", new TableProps
{
    PartitionKey = new Attribute { Name = "id", Type = AttributeType.STRING },
    BillingMode = BillingMode.PAY_PER_REQUEST,
    RemovalPolicy = RemovalPolicy.DESTROY
})).WithOutput("TableName", c => c.TableName);

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(table)
    .WithReference(awsConfig);

builder.Build().Run();
