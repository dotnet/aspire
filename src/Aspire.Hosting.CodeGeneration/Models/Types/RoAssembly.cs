// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Aspire.Hosting.CodeGeneration.Models.Types;

public sealed class RoAssembly
{
    private readonly Lazy<IDictionary<string, RoType>> _types;
    private readonly Lazy<IReadOnlyList<string>> _referencedAssemblyNames;
    private readonly Lazy<IReadOnlyList<RoCustomAttributeData>> _customAttributes;
    private readonly AssemblyDefinition _assemblyDefinition;

    public RoAssembly(AssemblyDefinition assemblyDefinition, MetadataReader reader, AssemblyLoaderContext assemblyLoaderContext)
    {
        if (!reader.IsAssembly)
        {
            throw new ArgumentException("Not an Assembly reader", nameof(reader));
        }

        Reader = reader;
        AssemblyLoaderContext = assemblyLoaderContext;
        _assemblyDefinition = assemblyDefinition;
        _types = new(LoadTypes);
        _referencedAssemblyNames = new(LoadReferencedAssemblyNames);
        _customAttributes = new(LoadCustomAttributes);
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
    public IEnumerable<RoCustomAttributeData> GetCustomAttributes() => _customAttributes.Value;

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

    private IReadOnlyList<RoCustomAttributeData> LoadCustomAttributes()
    {
        var list = new List<RoCustomAttributeData>();
        var provider = new CustomAttributeTypeProvider(Reader);

        foreach (var attrHandle in _assemblyDefinition.GetCustomAttributes())
        {
            try
            {
                var customAttribute = Reader.GetCustomAttribute(attrHandle);
                RoType? attributeType = null;

                switch (customAttribute.Constructor.Kind)
                {
                    case HandleKind.MethodDefinition:
                        {
                            var ctorMethod = Reader.GetMethodDefinition((MethodDefinitionHandle)customAttribute.Constructor);
                            var declaringTypeHandle = ctorMethod.GetDeclaringType();
                            var typeDef = Reader.GetTypeDefinition(declaringTypeHandle);
                            var name = Reader.GetString(typeDef.Name);
                            var ns = typeDef.Namespace.IsNil ? string.Empty : Reader.GetString(typeDef.Namespace);
                            var fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
                            attributeType = AssemblyLoaderContext.GetType(fullName);
                            break;
                        }
                    case HandleKind.MemberReference:
                        {
                            var memberRef = Reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                            var parent = memberRef.Parent;
                            var fullName = parent.GetTypeName(Reader);

                            if (fullName is not null)
                            {
                                attributeType = AssemblyLoaderContext.GetType(fullName);
                            }
                            break;
                        }
                }

                var val = customAttribute.DecodeValue(provider);

                var fixedArgs = val.FixedArguments.Select(a => a.Value).ToArray();
                var namedArgs = val.NamedArguments.Select(na => new KeyValuePair<string, object>(na.Name!, na.Value!)).ToArray();

                if (attributeType is not null)
                {
                    list.Add(new RoCustomAttributeData
                    {
                        AttributeType = attributeType,
                        FixedArguments = fixedArgs,
                        NamedArguments = namedArgs
                    });
                }
            }
            catch
            {
                // Skip attributes that can't be resolved
            }
        }

        return list;
    }

    public override string ToString()
    {
        return Name;
    }
}
