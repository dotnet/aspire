// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ConfigurationSchemaGenerator;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using SourceGenerators;
using static ConfigurationSchemaGenerator.ConfigSchemaGenerator;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration;

/// <summary>
/// Extensions to the ConfigurationBindingGenerator for use in the ConfigurationSchemaGenerator.
/// </summary>
public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) { }

    internal sealed class CompilationData
    {
        public bool LanguageVersionIsSupported { get; } = true;
        public KnownTypeSymbols? TypeSymbols { get; }

        public CompilationData(KnownTypeSymbols? typeSymbols)
        {
            TypeSymbols = typeSymbols;
        }
    }

    internal sealed partial class Parser
    {
        private readonly ConfigSchemaAttributeInfo _configSchemaInfo;

        public Parser(ConfigSchemaAttributeInfo configSchemaInfo, KnownTypeSymbols typeSymbols)
            : this(new CompilationData(typeSymbols))
        {
            _configSchemaInfo = configSchemaInfo;
        }

        public SchemaGenerationSpec? GetSchemaGenerationSpec(CancellationToken cancellationToken)
        {
            var types = new List<TypeSpec>();
            if (_configSchemaInfo.Types is not null)
            {
                foreach (var type in _configSchemaInfo.Types)
                {
                    var typeParseInfo = TypeParseInfo.Create(type, MethodsToGen.None, invocation: null);

                    _typesToParse.Enqueue(typeParseInfo);
                    types.Add(CreateTopTypeSpec(cancellationToken));
                }
            }

            return new SchemaGenerationSpec
            {
                ConfigurationTypes = types,
                ConfigurationPaths = _configSchemaInfo.ConfigurationPaths,
                ExclusionPaths = _configSchemaInfo.ExclusionPaths,
                LogCategories = _configSchemaInfo.LogCategories,
                AllTypes = _createdTypeSpecs.Values.ToImmutableEquatableArray()
            };
        }

        private TypeSpec CreateTopTypeSpec(CancellationToken cancellationToken)
        {
            Debug.Assert(_typesToParse.Count == 1, "there should only be one type to parse to start.");
            TypeSpec result = null;

            while (_typesToParse.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TypeParseInfo typeParseInfo = _typesToParse.Dequeue();
                ITypeSymbol typeSymbol = typeParseInfo.TypeSymbol;

                TypeSpec currentResult;
                if (!_createdTypeSpecs.TryGetValue(typeSymbol, out currentResult))
                {
                    currentResult = CreateTypeSpec(typeParseInfo);
                    _createdTypeSpecs.Add(typeSymbol, currentResult);
                }

                result ??= currentResult;
            }

            Debug.Assert(result is not null);
            return result;
        }
    }
}
