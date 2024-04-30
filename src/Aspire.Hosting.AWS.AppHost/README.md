# Lambda Class Libraries
When an Aspire AppHost project has this package installed and a referenced project is a class library project containing the property `<AWSProjectType>Lambda</AWSProjectType>` (case-insensitive), code will be generated based upon project content and the following constraints:

- The class and method are both public.
- The first parameter in the method **equals** or **inherits** from one the official event types published in `Amazon.Lambda.*Events` packages; OR
- the method has a second parameter and the second parameter is `Amazon.Lambda.Core.ILambdaContext`.

## Example Functions
If a single class contains multiple valid methods, each method generates a metadata class.

```csharp
namespace MyProject;

public class Function1
{
    public async Task HandleAsync(SQSEvent input)
    {
    }

    public void Handle(string value, ILambdaContext context)
    {
    }
}

public class Function2
{
    public async Task<string> DoThingsWithEvent(S3Event @event)
    {
    }
}
```

### Generated output

```csharp
namespace LambdaFunctions;

public class MyProject_Function1_HandleAsync : global::Aspire.Hosting.AWS.Lambda.ILambdaFunctionMetadata
{
    public string ProjectPath => """/path-to/src/MyProject/MyProject.csproj""";
    public string Handler => "MyProject::MyProject.Function1::HandleAsync";
}

public class MyProject_Function1_Handle : global::Aspire.Hosting.AWS.Lambda.ILambdaFunctionMetadata
{
    public string ProjectPath => """/path-to/src/MyProject/MyProject.csproj""";
    public string Handler => "MyProject::MyProject.Function1::Handle";
}

public class MyProject_Function2_DoThingsWithEvent : global::Aspire.Hosting.AWS.Lambda.ILambdaFunctionMetadata
{
    public string ProjectPath => """/path-to/src/MyProject/MyProject.csproj""";
    public string Handler => "MyProject::MyProject.Function2::DoThingsWithEvent";
}
```

## Usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddLambdaFunction<LambdaFunctions.MyProject_Function1_HandleAsync>("function1");

builder.Build().Run();
```

### Configuration Option - Method Filters
Below are three methods that would generate three metadata classes even when the intent is clearly different.

Making the last two methods `private` will remove them from generated metadata. It's also possible to configure `AspireAWSLambdaMetadataMethodFilter` in `AppHost.csproj` to restrict generation to only methods of certain names. Regardless of method filters the method must also match parameter constraints.

```csharp
public class OrderFunction
{
    public async Task HandleAsync(SQSEvent input)
    {
        if (logicCheckingIfOrderIsCancelled)
        {
            await CancelOrder(input);
        } else if (logicCheckingIfOrderIsNew)
        {
            await CreateNewOrder(input);
        }
    }

    public async Task<bool> CancelOrder(SQSEvent input)
    {
        // ...
    }

    public async Task<bool> CreateNewOrder(SQSEvent input)
    {
        // ...
    }
}
```

```xml
<!-- AppHost.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <IsAspireHost>true</IsAspireHost>
        <!-- Only methods with name equal to HandleAsync or Handle should be eligible for metadata output -->
        <AspireAWSLambdaMetadataMethodFilter>HandleAsync,Handle</AspireAWSLambdaMetadataMethodFilter>
    </PropertyGroup>
</Project>
```

### Configuration - AspireProjectMetadataTypeName
Configure a value on the project reference to use that instead of the project namespace as prefix in generated metadata classes.

```xml
<!-- AppHost.csproj -->
<ProjectReference Include="..\MyCompany.Functions.ShoppingFunctions\MyCompany.Functions.ShoppingFunctions.csproj" AspireProjectMetadataTypeName="Shopping" />
```

```csharp
// Without MetadataTypeName
builder.AddLambdaFunction<LambdaFunctions.MyCompany_Functions_ShoppingFunctions_OrderFunction_HandleAsync>("function1");

// With MetadataTypeName
builder.AddLambdaFunction<LambdaFunctions.Shopping_OrderFunction_HandleAsync>("function1");
```
