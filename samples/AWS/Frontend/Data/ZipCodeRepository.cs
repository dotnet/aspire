using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Frontend.Data;

public class ZipCodeRepository
{
    readonly IAmazonDynamoDB _ddbClient;
    readonly string _zipCodeTableName;
    public ZipCodeRepository(IAmazonDynamoDB ddbClient)
    {
        _ddbClient = ddbClient;

        _zipCodeTableName = Environment.GetEnvironmentVariable("ZIP_CODE_TABLE") ?? "ZipCodes";
    }

    public async Task<ZipCodeEntry?> LoadZipCode(string code)
    {
        var response = await _ddbClient.GetItemAsync(new GetItemRequest
        {
            TableName = _zipCodeTableName,
            Key = new Dictionary<string, AttributeValue>
                {
                    {"Code", new AttributeValue{S = code} }
                }
        });

        if (!response.Item.Any())
        {
            return null;
        }

        return ConvertItemToDTO(response.Item);
    }

    public async Task<ZipCodeEntry[]> LoadZipCodeForCity(string city)
    {
        var response = await _ddbClient.QueryAsync(new QueryRequest
        {
            TableName = _zipCodeTableName,
            IndexName = "City-index",
            KeyConditionExpression = "#S = :s",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":s", new AttributeValue{S=city} }
            },
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                {"#S", "City"}
            }
        });

        return response.Items.Select(ConvertItemToDTO).ToArray();
    }

    public async Task<ZipCodeEntry[]> LoadZipCodeForState(string state)
    {
        var response = await _ddbClient.QueryAsync(new QueryRequest
        {
            TableName = _zipCodeTableName,
            IndexName = "State-index",
            KeyConditionExpression = "#S = :s",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":s", new AttributeValue{S=state} }
            },
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                {"#S", "State"}
            }
        });

        return response.Items.Select(ConvertItemToDTO).ToArray();
    }

    ZipCodeEntry ConvertItemToDTO(IDictionary<string, AttributeValue> item)
    {
        return new ZipCodeEntry
        {
            Code = item["Code"].S,
            City = item["City"].S,
            State = item["State"].S,
            Latitude = double.Parse(item["Latitude"].N, CultureInfo.InvariantCulture),
            Longitude = double.Parse(item["Longitude"].N, CultureInfo.InvariantCulture),
        };
    }
}
