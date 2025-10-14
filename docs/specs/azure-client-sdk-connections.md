# Azure Client SDK Connection Reference (Restructured)

> **Note:** This is a working draft for collaborative editing. This document is intended to help contributors working on Azure client SDK integration features and documentation. Please feel free to improve and expand this reference.

This document organizes Azure client SDK connection information by resource, then language, then client library, with all connection details listed under each combination.

## Azure SQL Server (Azure SQL Database)

**Service Notes:**
- Port 1433; TLS is required (`Encrypt=True`; set `TrustServerCertificate=False`).
- Host pattern: `<server>.database.windows.net`.
- Supports SQL authentication and Azure AD (DefaultAzureCredential in most SDKs).

### .NET

#### Microsoft.Data.SqlClient

**Connection string / properties:**
- `Server` / `DataSource`: `tcp:<server>.database.windows.net,1433`
- `Database` / `InitialCatalog`: Database name
- `User Id` / `UserID`: Username
- `Password`: Password
- `Encrypt`: `True` (required)
- `TrustServerCertificate`: `False` (recommended)

**Variant A — URL / connection string:**
```csharp
await using var c = new SqlConnection("Server=tcp:SRV.database.windows.net,1433;Database=DB;User Id=USER;Password=PWD;Encrypt=True;TrustServerCertificate=False");
await c.OpenAsync();
```

**Variant B — property / options object:**
```csharp
var b = new SqlConnectionStringBuilder
{
	DataSource = "SRV.database.windows.net,1433",
	InitialCatalog = "DB",
	UserID = "USER",
	Password = "PWD",
	Encrypt = true,
	TrustServerCertificate = false
};

await using var c = new SqlConnection(b.ConnectionString);
await c.OpenAsync();
```

### Node

#### mssql

**Connection string / properties:**
- Connection string: `Server=tcp:<server>.database.windows.net,1433;Database=<db>;User Id=<user>;Password=<pwd>;Encrypt=True;TrustServerCertificate=False`
- Options object: `server`, `port` (1433), `database`, `user`, `password`, `options.encrypt` (true), `options.trustServerCertificate` (false)

**Variant A — URL / connection string:**
```javascript
import sql from "mssql";

const pool = await sql.connect(
	"Server=tcp:SRV.database.windows.net,1433;Database=DB;User Id=USER;Password=PWD;Encrypt=True;TrustServerCertificate=False"
);
```

**Variant B — property / options object:**
```javascript
import sql from "mssql";

const pool = await sql.connect({
	server: "SRV.database.windows.net",
	port: 1433,
	database: "DB",
	user: "USER",
	password: "PWD",
	options: {
		encrypt: true,
		trustServerCertificate: false
	}
});
```

### Python

#### pyodbc (ODBC Driver 18+)

**Connection string / properties:**
- `DRIVER`: `{ODBC Driver 18 for SQL Server}`
- `SERVER`: `<server>.database.windows.net,1433`
- `DATABASE`: Database name
- `UID`: Username
- `PWD`: Password
- `Encrypt`: `yes`
- `TrustServerCertificate`: `no`

**Variant A — URL / connection string:**
```python
import pyodbc

conn = pyodbc.connect(
	"DRIVER={ODBC Driver 18 for SQL Server};SERVER=SRV.database.windows.net,1433;DATABASE=DB;UID=USER;PWD=PWD;Encrypt=yes;TrustServerCertificate=no;"
)
```

**Variant B — property / options object:**
```python
import pyodbc

conn = pyodbc.connect(
	driver="{ODBC Driver 18 for SQL Server}",
	server="SRV.database.windows.net,1433",
	database="DB",
	uid="USER",
	pwd="PWD",
	Encrypt="yes",
	TrustServerCertificate="no"
)
```

### Go

#### denisenkom/go-mssqldb

**Connection string / properties:**
- Connection URL: `sqlserver://<user>:<pwd>@<server>.database.windows.net:1433?database=<db>&encrypt=true`
- URL components: `Scheme` (sqlserver), `Host` (<server>.database.windows.net:1433), `User` (username:password)
- Query parameters: `database`, `encrypt` (true)

**Variant A — URL / connection string:**
```go
db, _ := sql.Open(
	"sqlserver",
	"sqlserver://USER:PWD@SRV.database.windows.net:1433?database=DB&encrypt=true",
)
```

**Variant B — property / options object:**
```go
u := &url.URL{
	Scheme: "sqlserver",
	Host:   "SRV.database.windows.net:1433",
	User:   url.UserPassword("USER", "PWD"),
}

q := u.Query()
q.Set("database", "DB")
q.Set("encrypt", "true")
u.RawQuery = q.Encode()

db, _ := sql.Open("sqlserver", u.String())
```

### Java

#### mssql-jdbc

**Connection string / properties:**
- JDBC URL: `jdbc:sqlserver://<server>.database.windows.net:1433;databaseName=<db>;encrypt=true;trustServerCertificate=false;`
- Properties object: `user`, `password`, `encrypt` (true), `trustServerCertificate` (false)

**Variant A — URL / connection string:**
```java
Connection c = DriverManager.getConnection(
	"jdbc:sqlserver://SRV.database.windows.net:1433;databaseName=DB;encrypt=true;trustServerCertificate=false;",
	"USER",
	"PWD"
);
```

**Variant B — property / options object:**
```java
Properties p = new Properties();
p.put("user", "USER");
p.put("password", "PWD");
p.put("encrypt", "true");
p.put("trustServerCertificate", "false");

Connection c = DriverManager.getConnection(
	"jdbc:sqlserver://SRV.database.windows.net:1433;databaseName=DB",
	p
);
```

## Azure PostgreSQL (Flexible Server)

**Service Notes:**
- Host: `<server>.postgres.database.azure.com`.
- TLS required (`sslmode=require`); typical username format `user@server`.

### .NET

#### Npgsql

**Connection string / properties:**
- `Host`: `<server>.postgres.database.azure.com`
- `Port`: `5432`
- `Database`: Database name
- `Username`: `<user>@<server>` format
- `Password`: Password
- `SslMode`: `Require`

**Variant A — URL / connection string:**
```csharp
await using var c = new NpgsqlConnection(
	"Host=SRV.postgres.database.azure.com;Port=5432;Database=DB;Username=USER@SRV;Password=PWD;SslMode=Require"
);
await c.OpenAsync();
```

**Variant B — property / options object:**
```csharp
var b = new NpgsqlConnectionStringBuilder
{
	Host = "SRV.postgres.database.azure.com",
	Port = 5432,
	Database = "DB",
	Username = "USER@SRV",
	Password = "PWD",
	SslMode = SslMode.Require
};

await using var c = new NpgsqlConnection(b.ConnectionString);
await c.OpenAsync();
```

### Node

#### pg

**Connection string / properties:**
- Connection URL: `postgres://<user>@<server>:<pwd>@<server>.postgres.database.azure.com:5432/<db>?sslmode=require`
- Options object: `host`, `port` (5432), `user` (<user>@<server>), `password`, `database`, `ssl.rejectUnauthorized` (true)

**Variant A — URL / connection string:**
```javascript
const { Client } = require("pg");

const c = new Client({
	connectionString: "postgres://USER@SRV:PWD@SRV.postgres.database.azure.com:5432/DB?sslmode=require"
});

await c.connect();
```

**Variant B — property / options object:**
```javascript
const { Client } = require("pg");

const c = new Client({
	host: "SRV.postgres.database.azure.com",
	port: 5432,
	user: "USER@SRV",
	password: "PWD",
	database: "DB",
	ssl: { rejectUnauthorized: true }
});

await c.connect();
```

### Python

#### psycopg

**Connection string / properties:**
- Connection string: `dbname=<db> user=<user>@<server> password=<pwd> host=<server>.postgres.database.azure.com port=5432 sslmode=require`
- Parameters: `dbname`, `user` (<user>@<server>), `password`, `host`, `port` (5432), `sslmode` (require)

**Variant A — URL / connection string:**
```python
import psycopg

conn = psycopg.connect(
	"dbname=DB user=USER@SRV password=PWD host=SRV.postgres.database.azure.com port=5432 sslmode=require"
)
```

**Variant B — property / options object:**
```python
import psycopg

conn = psycopg.connect(
	dbname="DB",
	user="USER@SRV",
	password="PWD",
	host="SRV.postgres.database.azure.com",
	port=5432,
	sslmode="require"
)
```

**Variant C — password-less with Azure Identity (recommended for production):**
```python
import psycopg
from azure.identity import DefaultAzureCredential

# Get Azure AD access token
credential = DefaultAzureCredential()
token = credential.get_token("https://ossrdbms-aad.database.windows.net/.default")

conn = psycopg.connect(
	dbname="DB",
	user="USER@SRV",  # Use Azure AD user/managed identity name
	password=token.token,
	host="SRV.postgres.database.azure.com",
	port=5432,
	sslmode="require"
)
```

> **Note:** For password-less authentication with Azure AD, install `azure-identity` package and use `DefaultAzureCredential` to obtain an access token. The token is used as the password. See [Microsoft Learn documentation](https://learn.microsoft.com/azure/postgresql/flexible-server/connect-python?tabs=cmd%2Cpasswordless) for more details.

### Go

#### pgx

**Connection string / properties:**
- Connection URL: `postgres://<user>@<server>:<pwd>@<server>.postgres.database.azure.com:5432/<db>?sslmode=require`
- Config object: `Host`, `Port` (5432), `User` (<user>@<server>), `Password`, `Database`, `TLSConfig`

**Variant A — URL / connection string:**
```go
pool, _ := pgxpool.New(
	ctx,
	"postgres://USER@SRV:PWD@SRV.postgres.database.azure.com:5432/DB?sslmode=require",
)
```

**Variant B — property / options object:**
```go
cfg, _ := pgxpool.ParseConfig("")
cfg.ConnConfig.Host = "SRV.postgres.database.azure.com"
cfg.ConnConfig.Port = 5432
cfg.ConnConfig.User = "USER@SRV"
cfg.ConnConfig.Password = "PWD"
cfg.ConnConfig.Database = "DB"
cfg.ConnConfig.TLSConfig = &tls.Config{}

pool, _ := pgxpool.NewWithConfig(ctx, cfg)
```

### Java

#### org.postgresql JDBC

**Connection string / properties:**
- JDBC URL: `jdbc:postgresql://<server>.postgres.database.azure.com:5432/<db>?sslmode=require`
- Properties object: `user` (<user>@<server>), `password`, `sslmode` (require)

**Variant A — URL / connection string:**
```java
Connection c = DriverManager.getConnection(
	"jdbc:postgresql://SRV.postgres.database.azure.com:5432/DB?sslmode=require",
	"USER@SRV",
	"PWD"
);
```

**Variant B — property / options object:**
```java
Properties p = new Properties();
p.put("user", "USER@SRV");
p.put("password", "PWD");
p.put("sslmode", "require");

Connection c = DriverManager.getConnection(
	"jdbc:postgresql://SRV.postgres.database.azure.com:5432/DB",
	p
);
```

## Azure Redis (Cache for Redis)

**Service Notes:**
- Port 6380 (TLS).
- Host: `<name>.redis.cache.windows.net` or `<name>.redisenterprise.cache.azure.net` (for Enterprise tiers).
- `rediss://` scheme enables TLS.

### .NET

#### StackExchange.Redis

**Connection string / properties:**
- Connection string: `<name>.redis.cache.windows.net:6380,password=<pwd>,ssl=True`
- Configuration options: `EndPoints` (host:port), `Password`, `Ssl` (true)

**Variant A — URL / connection string:**
```csharp
var mux = await ConnectionMultiplexer.ConnectAsync(
	"SRV.redis.cache.windows.net:6380,password=PWD,ssl=True"
);
```

**Variant B — property / options object:**
```csharp
var opt = new ConfigurationOptions
{
	Ssl = true,
	Password = "PWD"
};

opt.EndPoints.Add("SRV.redis.cache.windows.net", 6380);

var mux = await ConnectionMultiplexer.ConnectAsync(opt);
```

### Node

#### ioredis / redis

**Connection string / properties:**
- Connection URL: `rediss://:<pwd>@<name>.redis.cache.windows.net:6380/0`
- Options object: `host`, `port` (6380), `password`, `tls` ({}), `db` (0)

**Variant A — URL / connection string:**
```javascript
import Redis from "ioredis";

const r = new Redis("rediss://:PWD@SRV.redis.cache.windows.net:6380/0");
```

**Variant B — property / options object:**
```javascript
import Redis from "ioredis";

const r = new Redis({
	host: "SRV.redis.cache.windows.net",
	port: 6380,
	password: "PWD",
	tls: {},
	db: 0
});
```

### Python

#### redis

**Connection string / properties:**
- Connection URL: `rediss://:<pwd>@<name>.redis.cache.windows.net:6380/0`
- Parameters: `host`, `port` (6380), `password`, `ssl` (True), `db` (0)

**Variant A — URL / connection string:**
```python
from redis import Redis

r = Redis.from_url("rediss://:PWD@SRV.redis.cache.windows.net:6380/0")
```

**Variant B — property / options object:**
```python
from redis import Redis

r = Redis(
	host="SRV.redis.cache.windows.net",
	port=6380,
	password="PWD",
	ssl=True,
	db=0
)
```

### Go

#### go-redis

**Connection string / properties:**
- Options: `Addr` (<name>.redis.cache.windows.net:6380), `Password`, `DB` (0), `TLSConfig`

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```go
rdb := redis.NewClient(&redis.Options{
	Addr:      "SRV.redis.cache.windows.net:6380",
	Password:  "PWD",
	DB:        0,
	TLSConfig: &tls.Config{},
})
```

### Java

#### Lettuce / Jedis

**Connection string / properties:**
- Connection URL: `rediss://:<pwd>@<name>.redis.cache.windows.net:6380/0`
- RedisURI builder: `host`, `port` (6380), `withSsl` (true), `withPassword`, `withDatabase` (0)

**Variant A — URL / connection string:**
```java
var client = RedisClient.create("rediss://:PWD@SRV.redis.cache.windows.net:6380/0");
var conn = client.connect();
```

**Variant B — property / options object:**
```java
RedisURI uri = RedisURI.Builder.redis("SRV.redis.cache.windows.net", 6380)
	.withSsl(true)
	.withPassword("PWD")
	.withDatabase(0)
	.build();

var conn = RedisClient.create(uri).connect();
```

## Azure Cosmos DB (Core SQL API)

**Service Notes:**
- Endpoint: `https://<account>.documents.azure.com:443/`.
- Auth: Account Key (primary/secondary) or Azure AD (RBAC).
- Some SDKs accept a connection-string form; others use endpoint + credential.

### .NET

#### Azure.Cosmos

**Connection string / properties:**
- Connection string: `AccountEndpoint=https://<account>.documents.azure.com:443/;AccountKey=<key>;`
- Constructor parameters: endpoint URL (`https://<account>.documents.azure.com:443/`), account key

**Variant A — URL / connection string:**
```csharp
var client = new CosmosClient("AccountEndpoint=https://ACCT.documents.azure.com:443/;AccountKey=KEY;");
```

**Variant B — property / options object:**
```csharp
var client = new CosmosClient("https://ACCT.documents.azure.com:443/", "KEY");
```

### Node

#### @azure/cosmos

**Connection string / properties:**
- Options object: `endpoint` (`https://<account>.documents.azure.com:443/`), `key`

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```javascript
import { CosmosClient } from "@azure/cosmos";

const c = new CosmosClient({
	endpoint: "https://ACCT.documents.azure.com:443/",
	key: "KEY"
});
```

### Python

#### azure-cosmos

**Connection string / properties:**
- Connection string: `AccountEndpoint=https://<account>.documents.azure.com:443/;AccountKey=<key>;`
- Constructor parameters: `url` (`https://<account>.documents.azure.com:443/`), `credential` (key)

**Variant A — URL / connection string:**
```python
from azure.cosmos import CosmosClient

c = CosmosClient.from_connection_string(
	"AccountEndpoint=https://ACCT.documents.azure.com:443/;AccountKey=KEY;"
)
```

**Variant B — property / options object:**
```python
from azure.cosmos import CosmosClient

c = CosmosClient(
	url="https://ACCT.documents.azure.com:443/",
	credential="KEY"
)
```

### Go

#### azcosmos

**Connection string / properties:**
- Parameters: endpoint URL (`https://<account>.documents.azure.com:443/`), credential (KeyCredential or AAD)

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```go
cred, _ := azcosmos.NewKeyCredential("KEY")

client, _ := azcosmos.NewClientWithKey(
	"https://ACCT.documents.azure.com:443/",
	cred,
	nil,
)
```

### Java

#### com.azure:azure-cosmos

**Connection string / properties:**
- Builder methods: `endpoint()` (`https://<account>.documents.azure.com:443/`), `key()`

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```java
CosmosClient c = new CosmosClientBuilder()
	.endpoint("https://ACCT.documents.azure.com:443/")
	.key("KEY")
	.buildClient();
```

## Azure SignalR Service

**Service Notes:**
- Best supported in .NET server-side with `Microsoft.Azure.SignalR`.
- Connection string format: `Endpoint=https://<name>.service.signalr.net;AccessKey=KEY;Version=1.0;`.
- Client SDKs exist for web/mobile; cross-language server alternatives include Azure Web PubSub.

### .NET

#### Microsoft.Azure.SignalR

**Connection string / properties:**
- Connection string: `Endpoint=https://<name>.service.signalr.net;AccessKey=<key>;Version=1.0;`
- Options object: `ConnectionString`

**Variant A — URL / connection string:**
```csharp
services
	.AddSignalR()
	.AddAzureSignalR("Endpoint=https://NAME.service.signalr.net;AccessKey=KEY;Version=1.0;");
```

**Variant B — property / options object:**
```csharp
var mgr = new ServiceManagerBuilder()
	.WithOptions(o =>
	{
		o.ConnectionString = "Endpoint=https://NAME.service.signalr.net;AccessKey=KEY;Version=1.0;";
	})
	.BuildServiceManager();
```

### Node

#### —

**Connection string / properties:** Not available

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
N/A

### Python

#### —

**Connection string / properties:** Not available

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
N/A

### Go

#### —

**Connection string / properties:** Not available

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
N/A

### Java

#### com.microsoft.azure:azure-signalr (server)

**Connection string / properties:** Connection via Spring/SDK configuration (endpoint and key setup)

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
N/A

## Azure Service Bus

**Service Notes:**
- Namespace: `sb://<namespace>.servicebus.windows.net/`.
- Auth: SAS connection string or Azure AD (FQDN + credential).
- Optional `EntityPath=` when connection string is entity-scoped.

### .NET

#### Azure.Messaging.ServiceBus

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;[EntityPath=<queue|topic>]`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```csharp
var client = new ServiceBusClient("Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;");
```

**Variant B — property / options object:**
```csharp
var cred = new DefaultAzureCredential();

var client = new ServiceBusClient("NS.servicebus.windows.net", cred);
```

### Node

#### @azure/service-bus

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;[EntityPath=<queue|topic>]`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```javascript
import { ServiceBusClient } from "@azure/service-bus";

const sb = new ServiceBusClient(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;"
);
```

**Variant B — property / options object:**
```javascript
import { ServiceBusClient } from "@azure/service-bus";
import { DefaultAzureCredential } from "@azure/identity";

const sb = new ServiceBusClient(
	"NS.servicebus.windows.net",
	new DefaultAzureCredential()
);
```

### Python

#### azure-servicebus

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;[EntityPath=<queue|topic>]`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```python
from azure.servicebus import ServiceBusClient

sb = ServiceBusClient.from_connection_string(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;"
)
```

**Variant B — property / options object:**
```python
from azure.identity import DefaultAzureCredential
from azure.servicebus import ServiceBusClient

sb = ServiceBusClient(
	"NS.servicebus.windows.net",
	credential=DefaultAzureCredential()
)
```

### Go

#### azservicebus

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;[EntityPath=<queue|topic>]`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```go
client, _ := azservicebus.NewClientFromConnectionString(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;",
	nil,
)
```

**Variant B — property / options object:**
```go
cred, _ := azidentity.NewDefaultAzureCredential(nil)

client, _ := azservicebus.NewClient("NS.servicebus.windows.net", cred, nil)
```

### Java

#### com.azure:azure-messaging-servicebus

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;[EntityPath=<queue|topic>]`
- Builder methods: `connectionString()` or `fullyQualifiedNamespace()` + `credential()`

**Variant A — URL / connection string:**
```java
ServiceBusClientBuilder b = new ServiceBusClientBuilder()
	.connectionString("Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;");
```

**Variant B — property / options object:**
```java
ServiceBusClientBuilder b = new ServiceBusClientBuilder()
	.fullyQualifiedNamespace("NS.servicebus.windows.net")
	.credential(new DefaultAzureCredentialBuilder().build());
```

## Azure Storage (Blobs)

**Service Notes:**
- Account endpoint: `https://<account>.blob.core.windows.net`.
- Auth: Connection string, Shared Key, SAS, or Azure AD.

### .NET

#### Azure.Storage.Blobs

**Connection string / properties:**
- Connection string: `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net`
- Constructor parameters: service URI (`https://<account>.blob.core.windows.net`), credential (StorageSharedKeyCredential)

**Variant A — URL / connection string:**
```csharp
var svc = new BlobServiceClient(
	"DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=KEY;EndpointSuffix=core.windows.net"
);
```

**Variant B — property / options object:**
```csharp
var svc = new BlobServiceClient(
	new Uri("https://acct.blob.core.windows.net"),
	new StorageSharedKeyCredential("acct", "KEY")
);
```

### Node

#### @azure/storage-blob

**Connection string / properties:**
- Connection string: `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net`
- Constructor parameters: service URL (`https://<account>.blob.core.windows.net`), credential (StorageSharedKeyCredential or SAS)

**Variant A — URL / connection string:**
```javascript
import { BlobServiceClient } from "@azure/storage-blob";

const svc = BlobServiceClient.fromConnectionString(
	"DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=KEY;EndpointSuffix=core.windows.net"
);
```

**Variant B — property / options object:**
```javascript
import { BlobServiceClient, StorageSharedKeyCredential } from "@azure/storage-blob";

const cred = new StorageSharedKeyCredential("acct", "KEY");
const svc = new BlobServiceClient("https://acct.blob.core.windows.net", cred);
```

### Python

#### azure-storage-blob

**Connection string / properties:**
- Connection string: `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net`
- Constructor parameters: `account_url` (`https://<account>.blob.core.windows.net`), credential (StorageSharedKeyCredential)

**Variant A — URL / connection string:**
```python
from azure.storage.blob import BlobServiceClient

svc = BlobServiceClient.from_connection_string(
	"DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=KEY;EndpointSuffix=core.windows.net"
)
```

**Variant B — property / options object:**
```python
from azure.storage.blob import BlobServiceClient, StorageSharedKeyCredential

cred = StorageSharedKeyCredential("acct", "KEY")
svc = BlobServiceClient(
	account_url="https://acct.blob.core.windows.net",
	credential=cred
)
```

### Go

#### azblob

**Connection string / properties:**
- Connection string: `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net`
- Constructor parameters: service URL (`https://<account>.blob.core.windows.net`), credential (SharedKeyCredential)

**Variant A — URL / connection string:**
```go
svc, _ := azblob.NewClientFromConnectionString(
	"DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=KEY;EndpointSuffix=core.windows.net",
	nil,
)
```

**Variant B — property / options object:**
```go
cred, _ := azblob.NewSharedKeyCredential("acct", "KEY")

svc, _ := azblob.NewClientWithSharedKeyCredential(
	"https://acct.blob.core.windows.net",
	cred,
	nil,
)
```

### Java

#### com.azure:azure-storage-blob

**Connection string / properties:**
- Connection string: `DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net`
- Builder methods: `connectionString()` or `endpoint()` + `credential()`

**Variant A — URL / connection string:**
```java
BlobServiceClient svc = new BlobServiceClientBuilder()
	.connectionString("DefaultEndpointsProtocol=https;AccountName=acct;AccountKey=KEY;EndpointSuffix=core.windows.net")
	.buildClient();
```

**Variant B — property / options object:**
```java
BlobServiceClient svc = new BlobServiceClientBuilder()
	.endpoint("https://acct.blob.core.windows.net")
	.credential(new StorageSharedKeyCredential("acct", "KEY"))
	.buildClient();
```

## Azure Event Hubs

**Service Notes:**
- Namespace: `sb://<namespace>.servicebus.windows.net/`.
- Auth: SAS connection string (requires `EntityPath` when scoped) or Azure AD with FQDN + hub name.

### .NET

#### Azure.Messaging.EventHubs

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;EntityPath=<hub>`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), event hub name, credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```csharp
var prod = new EventHubProducerClient(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;EntityPath=HUB"
);
```

**Variant B — property / options object:**
```csharp
var cred = new DefaultAzureCredential();

var prod = new EventHubProducerClient("NS.servicebus.windows.net", "HUB", cred);
```

### Node

#### @azure/event-hubs

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;EntityPath=<hub>`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), event hub name, credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```javascript
import { EventHubProducerClient } from "@azure/event-hubs";

const p = new EventHubProducerClient(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;EntityPath=HUB"
);
```

**Variant B — property / options object:**
```javascript
import { EventHubProducerClient } from "@azure/event-hubs";
import { DefaultAzureCredential } from "@azure/identity";

const p = new EventHubProducerClient(
	"NS.servicebus.windows.net",
	"HUB",
	new DefaultAzureCredential()
);
```

### Python

#### azure-eventhub

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;EntityPath=<hub>`
- Constructor parameters: `fully_qualified_namespace` (`<namespace>.servicebus.windows.net`), `eventhub_name`, credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```python
from azure.eventhub import EventHubProducerClient

p = EventHubProducerClient.from_connection_string(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;EntityPath=HUB"
)
```

**Variant B — property / options object:**
```python
from azure.identity import DefaultAzureCredential
from azure.eventhub import EventHubProducerClient

p = EventHubProducerClient(
	fully_qualified_namespace="NS.servicebus.windows.net",
	eventhub_name="HUB",
	credential=DefaultAzureCredential()
)
```

### Go

#### azeventhubs

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;EntityPath=<hub>`
- Constructor parameters: fully qualified namespace (`<namespace>.servicebus.windows.net`), event hub name, credential (DefaultAzureCredential)

**Variant A — URL / connection string:**
```go
prod, _ := azeventhubs.NewProducerClientFromConnectionString(
	"Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;EntityPath=HUB",
	nil,
)
```

**Variant B — property / options object:**
```go
cred, _ := azidentity.NewDefaultAzureCredential(nil)

prod, _ := azeventhubs.NewProducerClient("NS.servicebus.windows.net", "HUB", cred, nil)
```

### Java

#### com.azure:azure-messaging-eventhubs

**Connection string / properties:**
- Connection string: `Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>;EntityPath=<hub>`
- Builder methods: `connectionString()` or `fullyQualifiedNamespace()` + `eventHubName()` + `credential()`

**Variant A — URL / connection string:**
```java
EventHubProducerClient p = new EventHubClientBuilder()
	.connectionString("Endpoint=sb://NS.servicebus.windows.net/;SharedAccessKeyName=NAME;SharedAccessKey=KEY;EntityPath=HUB")
	.buildProducerClient();
```

**Variant B — property / options object:**
```java
EventHubProducerClient p = new EventHubClientBuilder()
	.fullyQualifiedNamespace("NS.servicebus.windows.net")
	.eventHubName("HUB")
	.credential(new DefaultAzureCredentialBuilder().build())
	.buildProducerClient();
```

## Azure Web PubSub

**Service Notes:**
- Endpoint: `https://<name>.webpubsub.azure.com`.
- Auth: connection string or endpoint + AzureKeyCredential; supports AAD in some SDKs.

### .NET

#### Azure.Messaging.WebPubSub

**Connection string / properties:**
- Connection string: `Endpoint=https://<name>.webpubsub.azure.com;AccessKey=<key>;Version=1.0;`
- Constructor parameters: service endpoint URI (`https://<name>.webpubsub.azure.com`), credential (AzureKeyCredential), hub name

**Variant A — URL / connection string:**
```csharp
var svc = new WebPubSubServiceClient(
	"Endpoint=https://NAME.webpubsub.azure.com;AccessKey=KEY;Version=1.0;",
	"hub"
);
```

**Variant B — property / options object:**
```csharp
var cred = new AzureKeyCredential("KEY");

var svc = new WebPubSubServiceClient(
	new Uri("https://NAME.webpubsub.azure.com"),
	cred,
	"hub"
);
```

### Node

#### @azure/web-pubsub

**Connection string / properties:**
- Connection string: `Endpoint=https://<name>.webpubsub.azure.com;AccessKey=<key>;Version=1.0;`
- Options object: `endpoint` (`https://<name>.webpubsub.azure.com`), `credential` (AzureKeyCredential), plus hub name parameter

**Variant A — URL / connection string:**
```javascript
import { WebPubSubServiceClient } from "@azure/web-pubsub";

const s = new WebPubSubServiceClient(
	"Endpoint=https://NAME.webpubsub.azure.com;AccessKey=KEY;Version=1.0;",
	"hub"
);
```

**Variant B — property / options object:**
```javascript
import { WebPubSubServiceClient } from "@azure/web-pubsub";
import { AzureKeyCredential } from "@azure/core-auth";

const s = new WebPubSubServiceClient(
	{
		endpoint: "https://NAME.webpubsub.azure.com",
		credential: new AzureKeyCredential("KEY")
	},
	"hub"
);
```

### Python

#### azure-messaging-webpubsubservice

**Connection string / properties:**
- Connection string: `Endpoint=https://<name>.webpubsub.azure.com;AccessKey=<key>;Version=1.0;`
- Constructor parameters: `endpoint` (`https://<name>.webpubsub.azure.com`), `credential` (AzureKeyCredential), `hub` name

**Variant A — URL / connection string:**
```python
from azure.messaging.webpubsubservice import WebPubSubServiceClient

s = WebPubSubServiceClient.from_connection_string(
	"Endpoint=https://NAME.webpubsub.azure.com;AccessKey=KEY;Version=1.0;",
	hub="hub"
)
```

**Variant B — property / options object:**
```python
from azure.core.credentials import AzureKeyCredential
from azure.messaging.webpubsubservice import WebPubSubServiceClient

s = WebPubSubServiceClient(
	endpoint="https://NAME.webpubsub.azure.com",
	credential=AzureKeyCredential("KEY"),
	hub="hub"
)
```

### Go

#### —

**Connection string / properties:** Not available

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
N/A

### Java

#### com.azure:azure-messaging-webpubsub

**Connection string / properties:**
- Builder methods: `endpoint()` (`https://<name>.webpubsub.azure.com`), `credential()` (AzureKeyCredential), `hub()`

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```java
WebPubSubServiceClient s = new WebPubSubServiceClientBuilder()
	.endpoint("https://NAME.webpubsub.azure.com")
	.credential(new AzureKeyCredential("KEY"))
	.hub("hub")
	.buildClient();
```

## Azure Cognitive Search (Azure AI Search)

**Service Notes:**
- Endpoint: `https://<service>.search.windows.net`.
- Auth: Admin/API key (AzureKeyCredential) or AAD; most SDKs prefer endpoint + credential (no connection string).

### .NET

#### Azure.Search.Documents

**Connection string / properties:**
- Constructor parameters: service endpoint URI (`https://<service>.search.windows.net`), index name, credential (AzureKeyCredential)

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```csharp
var c = new SearchClient(new Uri("https://SERVICE.search.windows.net"), "index", new AzureKeyCredential("KEY"));
```

### Node

#### @azure/search-documents

**Connection string / properties:**
- Constructor parameters: service endpoint (`https://<service>.search.windows.net`), index name, credential (AzureKeyCredential)

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```javascript
import { SearchClient, AzureKeyCredential } from "@azure/search-documents";

const c = new SearchClient(
	"https://SERVICE.search.windows.net",
	"index",
	new AzureKeyCredential("KEY")
);
```

### Python

#### azure-search-documents

**Connection string / properties:**
- Constructor parameters: `endpoint` (`https://<service>.search.windows.net`), `index_name`, `credential` (AzureKeyCredential)

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```python
from azure.search.documents import SearchClient
from azure.core.credentials import AzureKeyCredential

c = SearchClient(
	endpoint="https://SERVICE.search.windows.net",
	index_name="index",
	credential=AzureKeyCredential("KEY")
)
```

### Go

#### —

**Connection string / properties:** Not available

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
N/A

### Java

#### com.azure:azure-search-documents

**Connection string / properties:**
- Builder methods: `endpoint()` (`https://<service>.search.windows.net`), `indexName()`, `credential()` (AzureKeyCredential)

**Variant A — URL / connection string:**
N/A

**Variant B — property / options object:**
```java
SearchClient c = new SearchClientBuilder().endpoint("https://SERVICE.search.windows.net").indexName("index").credential(new AzureKeyCredential("KEY")).buildClient();
```