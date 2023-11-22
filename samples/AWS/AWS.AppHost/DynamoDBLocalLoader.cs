using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Aspire.Hosting.AWS.DynamoDBLocal;

namespace AWS.AppHost;

internal sealed class DynamoDBLocalLoader
{
    internal const string ZIP_CODE_TABLENAME = "ZipCodes";
    const int MAX_BATCH_WRITE_SIZE = 25;
    public static async Task Configure(SeedDynamoDBUtilities seedUtilities, CancellationToken token)
    {
        if(await seedUtilities.CheckTableExists(ZIP_CODE_TABLENAME) is false)
        {
            await CreateZipCodeTableAsync(seedUtilities, token);
            await LoadZipCodeTableAsync(seedUtilities, token);
        }
    }

    private static async Task CreateZipCodeTableAsync(SeedDynamoDBUtilities seedUtilities, CancellationToken token)
    {
        await seedUtilities.DynamoDBClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = ZIP_CODE_TABLENAME,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement{AttributeName = "Code", KeyType = KeyType.HASH}
            },
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new GlobalSecondaryIndex
                {
                    IndexName = "City-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement{AttributeName = "City", KeyType = KeyType.HASH}
                    },
                    Projection = new Projection{ProjectionType = ProjectionType.ALL},
                },
                new GlobalSecondaryIndex
                {
                    IndexName = "State-index",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement{AttributeName = "State", KeyType = KeyType.HASH}
                    },
                    Projection = new Projection{ProjectionType = ProjectionType.ALL},
                }
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition{AttributeName = "Code", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition{AttributeName = "City", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition{AttributeName = "State", AttributeType = ScalarAttributeType.S },
            }
        }, token);
    }

    private static async Task LoadZipCodeTableAsync(SeedDynamoDBUtilities seedUtilities, CancellationToken token)
    {
        Console.WriteLine("Starting loading seed data");
        var page = 0;
        try
        {
            var listWrites = new List<WriteRequest>();
            var batchWriteRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    {ZIP_CODE_TABLENAME, listWrites }
                }
            };
            foreach (var line in File.ReadAllLines("seed-data-wa-zipcodes.csv").Skip(1))
            {
                var tokens = line.Split(',').Select(x => x.Replace("\"", "")).ToList();
                if (tokens.Count == 6 && double.TryParse(tokens[1], out var _) && double.TryParse(tokens[2], out var _))
                {
                    listWrites.Add(new WriteRequest
                    {
                        PutRequest = new PutRequest
                        {
                            Item =
                            {
                                {"Code", new AttributeValue{S =  tokens[0] } },
                                {"Latitude", new AttributeValue{N =  tokens[1] } },
                                {"Longitude", new AttributeValue{N =  tokens[2] } },
                                {"City", new AttributeValue{S =  tokens[3] } },
                                {"State", new AttributeValue{S =  tokens[4] } },
                                {"County", new AttributeValue{S =  tokens[5] } },
                            }
                        }
                    });

                    if (listWrites.Count == MAX_BATCH_WRITE_SIZE)
                    {
                        await seedUtilities.DynamoDBClient.BatchWriteItemAsync(batchWriteRequest, token).ConfigureAwait(false);
                        listWrites.Clear();

                        page++;
                        Console.WriteLine($"\tPage loaded: {page}");
                    }
                }
            }

            if (listWrites.Count != 0)
            {
                await seedUtilities.DynamoDBClient.BatchWriteItemAsync(batchWriteRequest, token).ConfigureAwait(false);
            }

            Console.WriteLine("Seed data loaded");
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("Error adding seed data");
            Console.Error.WriteLine(ex.ToString());
        }
    }
}
