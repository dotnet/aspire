// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Aspire.Cli.Rosetta.Models.Types;

namespace Aspire.Cli.Rosetta;

// NOTE: Previously this code used MetadataLoadContext to load assemblies in an isolated context.
// That API is relatively heavyweight and not friendly to NativeAOT trimming. We now rely on
// System.Reflection.Metadata (PEReader) to inspect assemblies cheaply and then load the ones
// we actually need directly. This loses full isolation (version conflicts are still possible)
// but is acceptable for the CLI scenario where we only need surface information to drive code
// generation / model creation.

// Type specifications vs. type definitions vs. type references:
// Assemblies contain Type Definition, for instance System.Int32, or generic types like System.Action`1, System.Action`2.
// Arrays are represented as Type Specifications. So `System.String[]` is a Type Specification based on the Type Definition `System.String`.
// Type References are used to reference types defined in other assemblies. For instance, if assembly A defines type X and assembly B references A and defines type Y : X, then Y's base type is a Type Reference to X in assembly A.
// Assemblies reference each other via Assembly References. An assembly references all the assemblies it directly depends on. Hence if a Type Reference is encountered, the referenced assembly must be one of the Assembly References of the assembly being inspected.

// Type instantiations:
// System.Action`1 is a generic type definition. System.Action`1[System.String] is a generic type instantiation, represented as a Type Specification based on the Type Definition System.Action`1.

internal class AssemblyLoaderContext : IDisposable
{
    private List<IDisposable> _disposables = [];
    private bool _disposed;
    private readonly Dictionary<string, RoAssembly> _loadedAssemblies = [];
    // Cache for constructed (generic / array) types so repeated resolutions are cheap.
    private readonly Dictionary<string, RoType> _constructedTypes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, RoAssembly> LoadedAssemblies => _loadedAssemblies;

    public RoAssembly? LoadAssembly(string assemblyPath, bool loadDependencies = true)
    {
        var path = Path.GetDirectoryName(assemblyPath);
        var name = Path.GetFileNameWithoutExtension(assemblyPath);
        if (path is null || name is null)
        {
            return null;
        }
        return LoadAssembly(name, [path], loadDependencies);
    }

    public RoAssembly? LoadAssembly(string assemblyName, IEnumerable<string> paths, bool loadDependencies = true)
    {
        // Use PEReader first so that we exercise System.Reflection.Metadata directly (requirement)
        try
        {
            var assemblyPath = paths.Select(p => Path.Combine(p, assemblyName + ".dll")).FirstOrDefault(File.Exists);

            if (assemblyPath == null)
            {
                return null;
            }

            using var fs = File.OpenRead(assemblyPath);
            var peReader = new PEReader(fs, PEStreamOptions.LeaveOpen);
            if (!peReader.HasMetadata)
            {
                // Not a managed assembly; skip.
                throw new BadImageFormatException();
            }
            var reader = peReader.GetMetadataReader();
            var asmDef = reader.GetAssemblyDefinition();
            var asmName = reader.GetString(asmDef.Name);
            if (_loadedAssemblies.TryGetValue(asmName, out var existing))
            {
                peReader.Dispose();
                return existing;
            }

            RegisterForDisposal(peReader);

            var assembly = _loadedAssemblies[asmName] = new RoAssembly(asmDef, reader, this);

            if (loadDependencies)
            {
                // ReferencedAssemblyNames contains all the assemblies that were used during compilation.
                // It ensures we don't need to recursively scan the entire dependency graph.
                foreach (var dependency in assembly.ReferencedAssemblyNames)
                {
                    if (!LoadedAssemblies.ContainsKey(dependency))
                    {
                        LoadAssembly(dependency, paths, loadDependencies);
                    }
                }
            }

            return assembly;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a type by its name from the loaded assemblies, supporting generic and array types. Returns null if the
    /// type cannot be found or the name is invalid.
    /// </summary>
    /// <remarks>Type resolution supports generic and array types using their string representations. Pointer
    /// and reference types are not supported and will result in an exception. If the type name is malformed or the type
    /// cannot be found in any loaded assembly, the method returns null. The method caches constructed generic and array
    /// types for subsequent lookups.</remarks>
    /// <param name="fullName">The fully qualified name of the type to resolve. May include generic type arguments (e.g.,
    /// "Namespace.Type&lt;Arg&gt;") and array notation (e.g., "Namespace.Type[]"). Cannot represent pointer or reference
    /// types.</param>
    /// <param name="genericParameterTypes"></param>
    /// <returns>A <see cref="RoType"/> instance representing the resolved type if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="fullName"/> represents a pointer type (contains '*') or a reference type (contains '&amp;'),
    /// which are not supported.</exception>
    public RoType? GetType(string fullName, IReadOnlyList<RoGenericParameterType>? genericParameterTypes = null)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return null;
        }

        var span = fullName.AsSpan();

        if (span.IndexOf('*') >= 0)
        {
            throw new ArgumentException("Pointer types are not supported", nameof(fullName));
        }

        if (span.IndexOf('&') >= 0)
        {
            span = span.TrimEnd('&');
        }

        try
        {
            return Resolve(span);
        }
        catch
        {
            return null;
        }

        RoType? Resolve(ReadOnlySpan<char> typeSpan)
        {
            var key = new string(typeSpan);

            // Check constructed-type cache first
            // Don't use cache if generic parameters are involved

            if ((genericParameterTypes is null || genericParameterTypes.Count == 0) && _constructedTypes.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Find end of base type (first '<' or '[')
            var genericIdx = typeSpan.IndexOf('<');
            var arrayIdx = typeSpan.IndexOf('[');
            var baseEnd = genericIdx >= 0 && arrayIdx >= 0 ? Math.Min(genericIdx, arrayIdx) :
                          genericIdx >= 0 ? genericIdx :
                          arrayIdx >= 0 ? arrayIdx : typeSpan.Length;

            var baseNameSpan = typeSpan.Slice(0, baseEnd);
            var baseNameStr = new string(baseNameSpan);

            RoType? baseTypeLocal = null;

            // Generic parameter type marker: !!{index} for method parameters, !{index} for type parameters
            if (genericParameterTypes is not null && baseNameSpan.Contains('!'))
            {
                var index = int.Parse(baseNameSpan.TrimStart('!'), CultureInfo.InvariantCulture);
                if (index >= 0 && index < genericParameterTypes.Count)
                {
                    baseTypeLocal = genericParameterTypes[index];
                }
                else
                {
                    return null; // invalid generic parameter index
                }
            }

            if (baseTypeLocal is null)
            {
                foreach (var asm in LoadedAssemblies.Values)
                {
                    baseTypeLocal = asm.GetType(baseNameStr);
                    if (baseTypeLocal is not null)
                    {
                        break;
                    }
                }
            }

            if (baseTypeLocal is null)
            {
                return null;
            }

            var idx = baseEnd;

            // Generic instantiation
            if (idx < typeSpan.Length && typeSpan[idx] == '<')
            {
                var depth = 0;
                var startArgs = idx + 1;
                var pos = startArgs;
                for (; pos < typeSpan.Length; pos++)
                {
                    var ch = typeSpan[pos];
                    if (ch == '<')
                    {
                        depth++;
                    }
                    else if (ch == '>')
                    {
                        if (depth == 0)
                        {
                            break;
                        }
                        depth--;
                    }
                }
                if (pos >= typeSpan.Length)
                {
                    return null; // malformed
                }

                var argsSpan = typeSpan.Slice(startArgs, pos - startArgs);
                var argNameList = SplitGenericArguments(argsSpan);
                var resolvedArgs = new List<RoType>(argNameList.Count);
                foreach (var argNameStr in argNameList)
                {
                    var resolvedArg = Resolve(argNameStr.AsSpan());
                    if (resolvedArg is null)
                    {
                        return null;
                    }
                    resolvedArgs.Add(resolvedArg);
                }
                baseTypeLocal = new RoConstructedGenericType(baseTypeLocal, resolvedArgs);
                idx = pos + 1; // past '>'
            }

            // Arrays (jagged / multi-dimensional)
            while (idx < typeSpan.Length && typeSpan[idx] == '[')
            {
                var close = typeSpan.Slice(idx + 1).IndexOf(']');
                if (close < 0)
                {
                    return null; // malformed
                }
                var rankSlice = typeSpan.Slice(idx + 1, close); // inside brackets
                var rank = 1;
                if (!rankSlice.IsEmpty)
                {
                    var commas = 0;
                    for (var i2 = 0; i2 < rankSlice.Length; i2++)
                    {
                        if (rankSlice[i2] == ',')
                        {
                            commas++;
                        }
                    }
                    rank = commas + 1;
                }
                baseTypeLocal = new RoArrayType(baseTypeLocal, rank);
                idx += close + 2; // move past "]"
            }

            if (genericParameterTypes is null || genericParameterTypes.Count == 0)
            {
                _constructedTypes[key] = baseTypeLocal;
            }

            return baseTypeLocal;
        }
        
    }
    private static List<string> SplitGenericArguments(ReadOnlySpan<char> text)
    {
        var list = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            switch (ch)
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth--;
                    break;
                case ',':
                    if (depth == 0)
                    {
                        Add(text.Slice(start, i - start), list);
                        start = i + 1;
                    }
                    break;
            }
        }
        if (start <= text.Length)
        {
            Add(text.Slice(start), list);
        }
        return list;

        static void Add(ReadOnlySpan<char> slice, List<string> target)
        {
            var s = 0;
            var e = slice.Length - 1;
            while (s <= e && char.IsWhiteSpace(slice[s]))
            {
                s++;
            }
            while (e >= s && char.IsWhiteSpace(slice[e]))
            {
                e--;
            }
            if (e >= s)
            {
                target.Add(new string(slice.Slice(s, e - s + 1)));
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve the fully qualified name of the specified entity represented by the given handle.
    /// </summary>
    /// <remarks>This method supports type definitions, type references, and type specifications. For type
    /// specifications representing arrays, the full name reflects the element type and array rank. If the entity cannot
    /// be resolved or is not supported, the method returns false and <paramref name="fullName"/> is set to <see
    /// langword="null"/>.</remarks>
    /// <param name="entityHandle">The handle identifying the metadata entity for which to obtain the full name. Must not be nil.</param>
    /// <param name="reader">The metadata reader used to access information about the entity.</param>
    /// <param name="fullName">When this method returns, contains the fully qualified name of the entity if found; otherwise, <see
    /// langword="null"/>. This parameter is passed uninitialized.</param>
    /// <returns>true if the full name was successfully retrieved; otherwise, false.</returns>
    public static bool TryGetFullName(EntityHandle entityHandle, MetadataReader reader, [NotNullWhen(true)] out string? fullName)
    {
        fullName = null;

        if (entityHandle.IsNil)
        {
            return false;
        }

        switch (entityHandle.Kind)
        {
            case HandleKind.TypeDefinition:
            {
                var typeDefHandle = (TypeDefinitionHandle)entityHandle;
                var typeDef = reader.GetTypeDefinition(typeDefHandle);
                var name = reader.GetString(typeDef.Name);
                var namespaceName = typeDef.Namespace.IsNil ? string.Empty : reader.GetString(typeDef.Namespace);
                fullName = string.IsNullOrEmpty(namespaceName) ? name : $"{namespaceName}.{name}";
                return true;
            }

            case HandleKind.TypeReference:
            {
                var typeRefHandle = (TypeReferenceHandle)entityHandle;
                var typeRef = reader.GetTypeReference(typeRefHandle);
                var refName = reader.GetString(typeRef.Name);
                var refNamespace = typeRef.Namespace.IsNil ? string.Empty : reader.GetString(typeRef.Namespace);
                fullName = string.IsNullOrEmpty(refNamespace) ? refName : $"{refNamespace}.{refName}";
                return true;
            }

            case HandleKind.TypeSpecification:
            {
                // Complex shape: array / generic instantiation / pointer / byref / modified types.
                // Use the DisplayTypeProvider to decode the signature into a displayable full name.
                try
                {
                    var tsHandle = (TypeSpecificationHandle)entityHandle;
                    var ts = reader.GetTypeSpecification(tsHandle);
                    var provider = new DisplayTypeProvider(reader);
                    fullName = ts.DecodeSignature(provider, genericContext: null);
                    return fullName is not null;
                }
                catch
                {
                    fullName = null;
                    return false;
                }
            }

            default:
                return false;
        }
    }

    /// <summary>
    /// Adds an object to an internal list of objects to be disposed when the MetadataLoadContext is disposed.
    /// </summary>
    internal void RegisterForDisposal(IDisposable disposable) => _disposables.Add(disposable);

    private void DisposeInternal()
    {
        if (_disposed)
        {
            return;
        }

        // Dispose all PE readers. This releases any file locks on the underlying
        // assembly files.
        var disposables = _disposables;
        if (disposables != null)
        {
            _disposables = null!;

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DisposeInternal();
    }

    ~AssemblyLoaderContext()
    {
        DisposeInternal();
    }
}
