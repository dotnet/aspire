// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Rosetta;
using Aspire.Cli.Rosetta.Models;
using Aspire.Cli.Rosetta.Models.Types;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Cli.Tests.Polyglot;

public class TypeResolutionTests
{
    [Fact]
    public void LoadsTypesByFullName()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var testMethodsType = testAssembly.GetType(typeof(TestMethods).FullName!);
        Assert.NotNull(testMethodsType);
        Assert.Equal(typeof(TestMethods).FullName, testMethodsType.FullName);
    }

    [Fact]
    public void ArrayTypesAreResolved()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var testMethodsType = testAssembly.GetType(typeof(TestMethods).FullName!);
        Assert.NotNull(testMethodsType);

        var methodC = testMethodsType.GetMethod(nameof(TestMethods.MethodC));

        Assert.NotNull(methodC);
        Assert.Equal("MethodC", methodC.Name);
        Assert.Single(methodC.Parameters);
        Assert.Equal("input", methodC.Parameters[0].Name);
        Assert.Equal("System.String", methodC.Parameters[0].ParameterType.FullName);

        var methodCReturnType = methodC.ReturnType;
        Assert.Equal("System.String[]", methodCReturnType.FullName);

        var methodD = testMethodsType.GetMethod(nameof(TestMethods.MethodD));
        Assert.NotNull(methodD);
        Assert.Equal("MethodD", methodD.Name);
        Assert.Single(methodD.Parameters);
        Assert.Equal("inputs", methodD.Parameters[0].Name);
        Assert.Equal("System.String[]", methodD.Parameters[0].ParameterType.FullName);

        var methodDReturnType = methodD.ReturnType;
        Assert.Equal("System.String", methodDReturnType.FullName);
    }

    [Fact]
    public void ReadsOptionalParameters()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var testMethodsType = testAssembly.GetType(typeof(TestMethods).FullName!);
        Assert.NotNull(testMethodsType);

        var methodE = testMethodsType.GetMethod(nameof(TestMethods.MethodE));
        Assert.NotNull(methodE);
        Assert.Equal("MethodE", methodE.Name);
        Assert.Equal(4, methodE.Parameters.Count);

        var paramA = methodE.Parameters[0];
        Assert.Equal("a", paramA.Name);
        Assert.False(paramA.IsOptional);
        Assert.Equal("System.Int32", paramA.ParameterType.FullName);
        Assert.Equal(DBNull.Value, paramA.RawDefaultValue);

        var paramB = methodE.Parameters[1];
        Assert.Equal("b", paramB.Name);
        Assert.False(paramB.IsOptional);
        Assert.Equal("System.Nullable<System.Int32>", paramB.ParameterType.FullName);
        Assert.Equal(DBNull.Value, paramB.RawDefaultValue);

        var paramC = methodE.Parameters[2];
        Assert.Equal("c", paramC.Name);
        Assert.True(paramC.IsOptional);
        Assert.Equal("System.Int32", paramC.ParameterType.FullName);
        Assert.Equal(1, paramC.RawDefaultValue);

        var paramD = methodE.Parameters[3];
        Assert.Equal("d", paramD.Name);
        Assert.True(paramD.IsOptional);
        Assert.Equal("System.Nullable<System.Int32>", paramD.ParameterType.FullName);
        Assert.Equal(default(int?), paramD.RawDefaultValue);
    }

    [Fact]
    public void AssemblyLoaderContextResolvesTypeDefinitions()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var typeA = testAssembly.GetType(typeof(A).FullName!);
        var typeB = testAssembly.GetType(typeof(B).FullName!);
        Assert.NotNull(typeA);
        Assert.NotNull(typeB);
        Assert.Equal(typeof(A).FullName, typeA.FullName);
        Assert.Equal(typeof(B).FullName, typeB.FullName);
        Assert.Equal(typeA, typeB.BaseType);
    }

    [Fact]
    public void AssemblyLoaderContextResolvesGenericTypeDefinitions()
    {
        // Type Definition names come from the assembly blobs. They use the `1, `2 suffixes for generic types.

        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var typeA1 = testAssembly.GetType(typeof(GenericTypeA<>).FullName!);
        var typeA2 = testAssembly.GetType(typeof(GenericTypeA<,>).FullName!);

        var typeB1 = testAssembly.GetType(typeof(GenericTypeB<>).FullName!);
        var typeB2 = testAssembly.GetType(typeof(GenericTypeB<,>).FullName!);

        Assert.NotNull(typeA1);
        Assert.NotNull(typeA2);
        Assert.NotNull(typeB1);
        Assert.NotNull(typeB2);
        Assert.Equal(typeof(GenericTypeA<>).FullName, typeA1.FullName);
        Assert.Equal(typeof(GenericTypeA<,>).FullName, typeA2.FullName);
        Assert.Equal(typeof(GenericTypeB<>).FullName, typeB1.FullName);
        Assert.Equal(typeof(GenericTypeB<,>).FullName, typeB2.FullName);
    }

    [Theory]
    [InlineData("System.Int32")]
    [InlineData("System.Int32[]")]
    [InlineData("System.Int32[,]")]
    [InlineData("System.Int32[,,]")]
    [InlineData("System.Action`1")]
    [InlineData("System.Action`2")]
    [InlineData("System.Action<System.Int32, System.String>")]
    [InlineData("System.Action<System.Action`1>")]
    [InlineData("System.Action<System.Action`2>")]
    [InlineData("System.Action<System.Action<System.Int32>>")]
    [InlineData("System.Action<System.Action<System.Int32, System.String>>")]
    [InlineData("System.Action<System.Action<System.Int32[], System.String[]>>[]")]
    public void CanRoundtripTypenames(string fullName)
    {
        using var loader = CreateAssemblyLoaderContext(out var _);

        var type = loader.GetType(fullName);
        Assert.NotNull(type);
        Assert.Equal(fullName, type.FullName);
    }

    [Fact]
    public void NonPublicTypeDefinitionsAreIgnored()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var publicType = testAssembly.GetType(typeof(PublicType).FullName!);
        var internalType = testAssembly.GetType(typeof(InternalType).FullName!);
        var privateType = testAssembly.GetType("Aspire.Cli.Tests.Polyglot.PrivateType");
        var nestedType = testAssembly.GetType("Aspire.Cli.Tests.Polyglot.PublicSealedType+NestedType");
        Assert.NotNull(nestedType); // Nested types are considered public if the containing type is public
        Assert.NotNull(publicType);
        Assert.Null(internalType);
        Assert.Null(privateType);
    }

    [Fact]
    public void CanMakeGenericType()
    {
        using var loader = CreateAssemblyLoaderContext(out var _);

        var action = loader.GetType(typeof(Action<>).FullName!);
        Assert.NotNull(action);
        Assert.IsType<RoDefinitionType>(action);

        var intType = loader.GetType(typeof(int).FullName!)!;
        var constructed = action.MakeGenericType([intType]);

        Assert.IsType<RoConstructedGenericType>(constructed);
        Assert.Equal("System.Action<System.Int32>", constructed.FullName);
    }

    [Fact]
    public void ExtractsGenericMethods()
    {
        using var loader = CreateAssemblyLoaderContext(out var _);

        var method = loader.GetType(typeof(TestMethods).FullName!)?.GetMethod(nameof(TestMethods.MethodF));
        var clrMethod = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodF))!;

        Assert.NotNull(method);
        Assert.IsType<RoDefinitionMethod>(method);

        Assert.Equal("MethodF", method.Name);
        Assert.True(method.IsGenericMethodDefinition);
        Assert.Equal(2, method.GetGenericArguments().Count);
        Assert.Equal("T", method.GetGenericArguments()[0].FullName);
        Assert.Equal("U", method.GetGenericArguments()[1].FullName);
        Assert.Equal(2, method.Parameters.Count);
        Assert.Equal("a", method.Parameters[0].Name);
        Assert.Equal("b", method.Parameters[1].Name);
        Assert.Equal("T", method.Parameters[0].ParameterType.FullName);
        Assert.Equal("U", method.Parameters[1].ParameterType.FullName);
        Assert.Equal("T", method.ReturnType.FullName);
        Assert.False(method.ReturnType.IsGenericType);
        Assert.True(method.ReturnType.IsGenericParameter);
        Assert.False(method.ReturnType.IsGenericTypeParameter);
        Assert.True(method.ReturnType.IsGenericMethodParameter);

        Assert.Equal(clrMethod.Name, method.Name);
        Assert.Equal(clrMethod.IsGenericMethodDefinition, method.IsGenericMethodDefinition);
        Assert.Equal(clrMethod.GetGenericArguments().Length, method.GetGenericArguments().Count);
        Assert.Equal(clrMethod.GetParameters().Length, method.Parameters.Count);
        Assert.Equal(clrMethod.ReturnType.Name, method.ReturnType.Name);
        Assert.Equal(clrMethod.ReturnType.IsGenericType, method.ReturnType.IsGenericType);
        Assert.Equal(clrMethod.ReturnType.IsGenericParameter, method.ReturnType.IsGenericParameter);
        Assert.Equal(clrMethod.ReturnType.IsGenericTypeParameter, method.ReturnType.IsGenericTypeParameter);
        Assert.Equal(clrMethod.ReturnType.IsGenericMethodParameter, method.ReturnType.IsGenericMethodParameter);

        // The CLR returns null for the FullName of generic method parameters
        // Assert.Equal(clrMethod.ReturnType.FullName, method.ReturnType.FullName);

        var genericMethod = method.MakeGenericMethod([loader.GetType(typeof(string).FullName!)!, loader.GetType(typeof(int).FullName!)!]);
        Assert.IsType<RoConstructedGenericMethod>(genericMethod);
        Assert.Equal("MethodF", genericMethod.Name);
        Assert.Equal("System.String", genericMethod.GetGenericArguments()[0].FullName);
        Assert.Equal("System.Int32", genericMethod.GetGenericArguments()[1].FullName);
        Assert.Equal("System.String", genericMethod.ReturnType.FullName);
    }

    [Fact]
    public void ExtractsConstaintsFromMethods()
    {
        using var loader = CreateAssemblyLoaderContext(out var _);

        var method = loader.GetType(typeof(ContainerResourceBuilderExtensions).FullName!)?.GetMethod(nameof(ContainerResourceBuilderExtensions.WithVolume))!;
        var containerResource = loader.GetType(typeof(ContainerResource).FullName!)!;
        var clrMethod = typeof(ContainerResourceBuilderExtensions).GetMethod(nameof(ContainerResourceBuilderExtensions.WithVolume))!;

        Assert.Single(method.GetGenericArguments());
        Assert.Equal(containerResource, method.GetGenericArguments()[0].GetGenericParameterConstraints()[0]);

        Assert.Single(clrMethod.GetGenericArguments());
        Assert.Equal(typeof(ContainerResource), clrMethod.GetGenericArguments()[0].GetGenericParameterConstraints()[0]);
    }

    [Fact]
    public void MakeGenericMethodConvertsGenericReturnType()
    {
        using var loader = CreateAssemblyLoaderContext(out var _);

        var method = loader.GetType(typeof(ContainerResourceBuilderExtensions).FullName!)?.GetMethod(nameof(ContainerResourceBuilderExtensions.WithVolume))!;
        var containerResource = loader.GetType(typeof(ContainerResource).FullName!)!;
        var clrMethod = typeof(ContainerResourceBuilderExtensions).GetMethod(nameof(ContainerResourceBuilderExtensions.WithVolume))!;

        var constructedMethod = method.MakeGenericMethod(containerResource);
        Assert.Equal("IResourceBuilder<ContainerResource>", constructedMethod.ReturnType.Name);

        var constructedClrMethod = clrMethod.MakeGenericMethod(typeof(ContainerResource));
        Assert.Equal(typeof(IResourceBuilder<ContainerResource>), constructedClrMethod.ReturnType);
    }

    [Fact]
    public void ReadsGenericInterface()
    {
        using var loader = CreateAssemblyLoaderContext(out var _);

        var builder = loader.GetType(typeof(IResourceBuilder<>).FullName!);
        Assert.NotNull(builder);

        var clrBuilder = typeof(IResourceBuilder<>);

        Assert.IsType<RoDefinitionType>(builder);

        Assert.Equal("IResourceBuilder`1", builder.Name);
        Assert.True(builder.IsGenericType);
        Assert.True(builder.IsInterface);
        Assert.Single(builder.GetGenericArguments());
        Assert.Equal("T", builder.GetGenericArguments()[0].Name);
        Assert.Empty(builder.GenericTypeArguments);

        Assert.Equal(clrBuilder.Name, builder.Name);
        Assert.Equal(clrBuilder.IsInterface, builder.IsInterface);
        Assert.Equal(clrBuilder.IsGenericType, builder.IsGenericType);
        Assert.Equal(clrBuilder.GetGenericArguments().Length, builder.GetGenericArguments().Count);
        Assert.Equal(clrBuilder.GetGenericArguments()[0].Name, builder.GetGenericArguments()[0].Name);
        Assert.Equal(clrBuilder.GenericTypeArguments.Length, builder.GenericTypeArguments.Count);
        Assert.Equal(clrBuilder.GetGenericTypeDefinition().Name, builder.GenericTypeDefinition?.Name);

        var concreteBuilder = builder.MakeGenericType(loader.GetType(typeof(ContainerResource).FullName!)!);
        var concreteClrBuilder = clrBuilder.MakeGenericType(typeof(ContainerResource));

        Assert.Equal("IResourceBuilder<ContainerResource>", concreteBuilder.Name);
        Assert.True(concreteBuilder.IsGenericType);
        Assert.True(concreteBuilder.IsInterface);
        Assert.Single(concreteBuilder.GetGenericArguments());
        Assert.Equal("ContainerResource", concreteBuilder.GetGenericArguments()[0].Name);
        Assert.Single(concreteBuilder.GenericTypeArguments);
        Assert.Equal("ContainerResource", concreteBuilder.GenericTypeArguments[0].Name);

        // The CLR omits generic types in Name
        //Assert.Equal(concreteClrBuilder.Name, concreteBuilder.Name);
        Assert.Equal(concreteClrBuilder.IsInterface, concreteBuilder.IsInterface);
        Assert.Equal(concreteClrBuilder.IsGenericType, concreteBuilder.IsGenericType);
        Assert.Equal(concreteClrBuilder.GetGenericArguments().Length, concreteBuilder.GetGenericArguments().Count);
        Assert.Equal(concreteClrBuilder.GetGenericArguments()[0].Name, concreteBuilder.GetGenericArguments()[0].Name);
        Assert.Equal(concreteClrBuilder.GenericTypeArguments.Length, concreteBuilder.GenericTypeArguments.Count);
        Assert.Equal(concreteClrBuilder.GenericTypeArguments[0].Name, concreteBuilder.GenericTypeArguments[0].Name);
        Assert.Equal(concreteClrBuilder.GetGenericTypeDefinition().Name, concreteBuilder.GenericTypeDefinition?.Name);
    }

    [Fact]
    public void IsByRefIsSet()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var typeA = testAssembly.GetType(typeof(PublicRefStructType).FullName!);
        Assert.NotNull(typeA);
        Assert.True(typeA.IsByRef);
    }

    [Fact]
    public void MetadataTokenMatchesClrMethod()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var testMethodsType = testAssembly.GetType(typeof(TestMethods).FullName!);
        Assert.NotNull(testMethodsType);

        // Compare RoDefinitionMethod metadata token with CLR MethodInfo metadata token
        var roMethod = testMethodsType.GetMethod(nameof(TestMethods.MethodA));
        var clrMethod = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodA));

        Assert.NotNull(roMethod);
        Assert.NotNull(clrMethod);
        Assert.Equal(clrMethod.MetadataToken, roMethod.MetadataToken);

        // Test with another method to ensure consistency
        var roMethodB = testMethodsType.GetMethod(nameof(TestMethods.MethodB));
        var clrMethodB = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodB));

        Assert.NotNull(roMethodB);
        Assert.NotNull(clrMethodB);
        Assert.Equal(clrMethodB.MetadataToken, roMethodB.MetadataToken);

        // Test with generic method
        var roMethodF = testMethodsType.GetMethod(nameof(TestMethods.MethodF));
        var clrMethodF = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodF));

        Assert.NotNull(roMethodF);
        Assert.NotNull(clrMethodF);
        Assert.Equal(clrMethodF.MetadataToken, roMethodF.MetadataToken);
    }

    [Fact]
    public void TypeCustomAttributesAreLoaded()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        // Test that TestAttribute is loaded on ClassWithAttribute
        var type = testAssembly.GetType(typeof(ClassWithAttribute).FullName!);
        Assert.NotNull(type);

        var customAttributes = type.GetCustomAttributes().ToList();
        Assert.NotEmpty(customAttributes);

        // Verify that the TestAttribute is present
        var testAttribute = customAttributes.FirstOrDefault(attr => 
            attr.AttributeType.FullName == typeof(TestAttribute).FullName);
        Assert.NotNull(testAttribute);
        //var argument = testAttribute.NamedArguments.Single();
        //Assert.Equal("Name", argument.Key);
        //Assert.Equal("class", argument.Value);

        var method = type.GetMethod(nameof(ClassWithAttribute.MethodWithAttribute));
        Assert.NotNull(method);

        customAttributes = method.GetCustomAttributes().ToList();
        Assert.NotEmpty(customAttributes);

        testAttribute = customAttributes.FirstOrDefault(attr =>
            attr.AttributeType.FullName == typeof(TestAttribute).FullName);
        Assert.NotNull(testAttribute);
        //argument = testAttribute.NamedArguments.Single();
        //Assert.Equal("Name", argument.Key);
        //Assert.Equal("method", argument.Value);

        var parameter = method.Parameters[0];
        Assert.NotNull(parameter);

        customAttributes = parameter.GetCustomAttributes().ToList();
        Assert.NotEmpty(customAttributes);

        testAttribute = customAttributes.FirstOrDefault(attr =>
            attr.AttributeType.FullName == typeof(TestAttribute).FullName);
        Assert.NotNull(testAttribute);
        //argument = testAttribute.NamedArguments.Single();
        //Assert.Equal("Name", argument.Key);
        //Assert.Equal("parameter", argument.Value);
    }

    [Fact]
    public void MethodCustomAttributesAreLoaded()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        // Get the extension attribute type
        var extensionAttributeType = loader.GetType(typeof(System.Runtime.CompilerServices.ExtensionAttribute).FullName!);
        Assert.NotNull(extensionAttributeType);

        // Find extension methods in the test assembly
        var staticMethods = from type in testAssembly.GetTypeDefinitions()
                            from method in type.Methods
                            where method.IsStatic && method.IsPublic
                            select method;

        var extensionMethods = (from method in staticMethods
                               let attrs = method.GetCustomAttributes().ToList()
                               where attrs.Any(attr => attr.AttributeType == extensionAttributeType)
                               select method).ToList();

        // Verify we found at least one extension method with the ExtensionAttribute
        Assert.NotEmpty(extensionMethods);

        // Verify each extension method has the ExtensionAttribute
        foreach (var method in extensionMethods)
        {
            var methodAttributes = method.GetCustomAttributes().ToList();
            var hasExtensionAttribute = methodAttributes.Any(attr => 
                attr.AttributeType == extensionAttributeType);
            Assert.True(hasExtensionAttribute, $"Method {method.Name} should have ExtensionAttribute");
        }
    }

    [Fact]
    public void ExntensionMethodsShouldBeDiscovered()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        _ = loader.LoadAssembly(typeof(string).Assembly.Location) ?? throw new InvalidOperationException("System.Private.CoreLib.dll assembly not found.");
        _ = loader.LoadAssembly(typeof(Uri).Assembly.Location) ?? throw new InvalidOperationException("System.Runtime.dll assembly not found.");

        var wellKnownTypes = new WellKnownTypes(loader);

        var integrationModel = IntegrationModel.Create(wellKnownTypes, testAssembly);

        Assert.DoesNotContain(integrationModel.SharedExtensionMethods, x => x.Name == nameof(ContainerResourceBuilderExtensions.Ignored));
        Assert.Contains(integrationModel.SharedExtensionMethods, x => x.Name == nameof(ContainerResourceBuilderExtensions.WithVolume));
        Assert.Contains(integrationModel.SharedExtensionMethods, x => x.Name == nameof(ContainerResourceBuilderExtensions.WithSomethingSpecial));

        Assert.DoesNotContain(integrationModel.SharedExtensionMethods, x => x.Name == nameof(ContainerResourceBuilderExtensions.AddSomeResource));
        Assert.Contains(integrationModel.IDistributedApplicationBuilderExtensionMethods, x => x.Name == nameof(ContainerResourceBuilderExtensions.AddSomeResource));
    }

    [Fact]
    public void AssignabilityReflectsInheritanceAndInterfaces()
    {
        using var loader = CreateAssemblyLoaderContext(out var testAssembly);

        var containerResourceType = testAssembly.GetType(typeof(ContainerResource).FullName!);
        var resourceType = loader.GetType(typeof(Resource).FullName!);
        var resourceInterfaceType = loader.GetType(typeof(IResource).FullName!);

        Assert.NotNull(containerResourceType);
        Assert.NotNull(resourceType);
        Assert.NotNull(resourceInterfaceType);

        Assert.True(resourceType.IsAssignableFrom(containerResourceType));
        Assert.True(resourceInterfaceType.IsAssignableFrom(containerResourceType));
        Assert.True(resourceInterfaceType.IsAssignableFrom(resourceType));

        Assert.True(containerResourceType.IsAssignableTo(resourceType));
        Assert.True(containerResourceType.IsAssignableTo(resourceInterfaceType));
        Assert.True(resourceType.IsAssignableTo(resourceInterfaceType));

        Assert.False(containerResourceType.IsAssignableFrom(resourceType));
        Assert.False(resourceType.IsAssignableFrom(resourceInterfaceType));
        Assert.False(resourceType.IsAssignableTo(containerResourceType));
        Assert.False(resourceInterfaceType.IsAssignableTo(resourceType));
    }

    private static AssemblyLoaderContext CreateAssemblyLoaderContext(out RoAssembly testAssembly)
    {
        var assemblyLoaderContext = new AssemblyLoaderContext();
        var mscorlib = assemblyLoaderContext.LoadAssembly(typeof(int).Assembly.Location, true);
        var result = assemblyLoaderContext.LoadAssembly(typeof(TestMethods).Assembly.Location, true);
        Assert.NotNull(mscorlib);
        Assert.NotNull(result);
        testAssembly = result;
        return assemblyLoaderContext;
    }
}
