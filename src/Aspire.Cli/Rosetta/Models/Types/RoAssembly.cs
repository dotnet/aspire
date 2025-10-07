// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Aspire.Cli.Rosetta.Models.Types;

internal sealed class RoAssembly
{
    private readonly Lazy<IDictionary<string, RoType>> _types;
    private readonly Lazy<IReadOnlyList<string>> _referencedAssemblyNames;
    public RoAssembly(AssemblyDefinition assemblyDefinition, MetadataReader reader, AssemblyLoaderContext assemblyLoaderContext)
    {
        if (!reader.IsAssembly)
        {
            throw new ArgumentException("Not an Assembly reader", nameof(reader));
        }

        Reader = reader;
        AssemblyLoaderContext = assemblyLoaderContext;
        _types = new(LoadTypes);
        _referencedAssemblyNames = new(LoadReferencedAssemblyNames);
        Name = reader.GetString(assemblyDefinition.Name) ?? throw new InvalidOperationException("Invalid assembly, missing Name.");
    }

    public string Name { get; }
    public IReadOnlyList<string> ReferencedAssemblyNames => _referencedAssemblyNames.Value;

    public MetadataReader Reader { get; }
    public AssemblyLoaderContext AssemblyLoaderContext { get; }

    public RoType? GetType(string name)
    {
        _types.Value.TryGetValue(name, out var result);
        return result;
    }

    public IEnumerable<RoType> GetTypeDefinitions() => _types.Value.Values;
    public IEnumerable<RoCustomAttributeData> GetCustomAttributes() => throw new NotImplementedException();

    private Dictionary<string, RoType> LoadTypes()
    {
        var types = new Dictionary<string, RoType>();

        foreach (var typeDefHandle in Reader.TypeDefinitions)
        {
            var typeDef = Reader.GetTypeDefinition(typeDefHandle);

            // Skip compiler-generated types (those with special names)
            var name = Reader.GetString(typeDef.Name);
            if (string.IsNullOrEmpty(name) || name.StartsWith('<'))
            {
                continue;
            }

            // Ignore non-public types
            var attributes = typeDef.Attributes;
            var visibility = attributes & System.Reflection.TypeAttributes.VisibilityMask;
            var isPublic = visibility == System.Reflection.TypeAttributes.Public ||
                      visibility == System.Reflection.TypeAttributes.NestedPublic;

            if (!isPublic)
            {
                continue;
            }

            // Create RoType instance - all metadata extraction happens in constructor
            var roType = new RoDefinitionType(typeDef, this);
            types[roType.FullName] = roType;
        }

        return types;
    }

    private IReadOnlyList<string> LoadReferencedAssemblyNames()
    {
        var list = new List<string>();
        foreach (var handle in Reader.AssemblyReferences)
        {
            var reference = Reader.GetAssemblyReference(handle);
            var name = Reader.GetString(reference.Name);
            if (!string.IsNullOrEmpty(name))
            {
                list.Add(name);
            }
        }
        return list;
    }

    public override string ToString()
    {
        return Name;
    }
}
