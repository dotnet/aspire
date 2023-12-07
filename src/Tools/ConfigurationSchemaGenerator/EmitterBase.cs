// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;

namespace ConfigurationSchemaGenerator;

internal abstract class EmitterBase
{
    private readonly IndentedTextWriter _writer;

    protected EmitterBase(string tabString)
    {
        _writer = new IndentedTextWriter(new StringWriter(), tabString);
    }

    protected void OutOpenBrace()
    {
        OutLn("{");
        Indent();
    }

    protected void OutCloseBrace(bool includeComma = false)
    {
        Unindent();
        if (includeComma)
        {
            OutLn("},");
        }
        else
        {
            OutLn("}");
        }
    }

    protected void OutLn(string line)
    {
        _writer.WriteLine(line);
    }

    protected void Out(string text) => _writer.Write(text);
    protected void Out(char ch) => _writer.Write(ch);
    protected void Indent() => _writer.Indent++;
    protected void Unindent() => _writer.Indent--;
    protected string Capture() => _writer.InnerWriter.ToString();
}
