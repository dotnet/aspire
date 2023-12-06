// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace ConfigurationSchemaGenerator;

internal sealed class ConfigSchemaEmitter(SourceGenerationSpec spec) : EmitterBase(tabString: "  ")
{
    public string GenerateSchema()
    {
        if (spec == null || spec.ConfigTypes.Count == 0)
        {
            return string.Empty;
        }

        OutOpenBrace();

        GenerateLogs();
        GenerateProperties();

        OutLn("""
            "type": "object"
            """);
        OutCloseBrace();
        return Capture();
    }

    private void GenerateLogs()
    {
        OutLn("""
            "definitions": {
            """);
        Indent();

        OutLn("""
            "logLevel": {
            """);
        Indent();

        OutLn("""
            "properties": {
            """);
        Indent();

        var categories = spec.LogCategories;
        for (int i = 0; i < categories.Length; i++)
        {
            string logCategory = categories[i];
            var property = $"""
                "{logCategory}":
                """;
            OutLn(property + " {");
            Indent();

            OutLn("""
                "$ref": "#/definitions/logLevelThreshold"
                """);
            OutCloseBrace(includeComma: i != categories.Length - 1);
        }

        OutCloseBrace();
        OutCloseBrace();
        OutCloseBrace(includeComma: true);
    }

    private void GenerateProperties()
    {
        OutLn("""
            "properties": {
            """);
        Indent();

        OutCloseBrace(includeComma: true);
    }
}
