// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CommandLine;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

//
// QuarantineTools – high-level overview
// -------------------------------------
// This small command-line tool helps developers quarantine or unquarantine failing/flaky xUnit tests
// across the repository's tests folder by adding or removing the [QuarantinedTest] attribute on
// test methods. It edits source files directly using Roslyn (Microsoft.CodeAnalysis) to ensure safe and
// structured modifications.
//
// Primary flow (Program.Main):
// 1. Parse command-line arguments: the mode is either quarantine (-q) or unquarantine (-u).
//    - quarantine: provide one or more fully-qualified test names immediately after -q, and pass the
//      issue URL using -i/--url. Example: `quarantine -q N.C.M -i https://...`
//    - unquarantine: provide one or more fully-qualified test names after -u. Example: `quarantine -u N.C.M`
// 2. Locate the repo root (looks for a .git folder) and then the tests directory under it.
// 3. Enumerate all .cs files under tests, ignoring bin/ and obj/.
// 4. For each file, parse a syntax tree and find method declarations. For each method, compute its
//    containing namespace and type chain and compare against requested targets (namespace + nested type
//    chain + method name) to determine matches.
// 5. Depending on the action:
//      - quarantine: add [QuarantinedTest("<issue-url>")] to matched methods (if not present) and ensure
//        a using Aspire.TestUtilities; exists at the file level.
//      - unquarantine: remove the [QuarantinedTest] attribute from matched methods and remove the using
//        Aspire.TestUtilities; if no method in the file uses it anymore.
// 6. If any file contents change, write them back to disk and print a summary of updated files.
//
// The tool is conservative: if a requested test is already in the desired state, it makes no change.
// It also avoids touching files that don't contain the specified methods.

public class Program
{
    private const string DefaultQuarantinedTestAttributeFullName = "Aspire.TestUtilities.QuarantinedTest";
    private const string DefaultActiveIssueAttributeFullName = "Xunit.ActiveIssueAttribute";

    public static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Quarantine or unquarantine xUnit tests by adding/removing a QuarantinedTest or ActiveIssue attribute.");

        var optQuarantine = new Option<bool>("--quarantine", "-q") { Description = "Quarantine the specified test(s)." };
        var optUnquarantine = new Option<bool>("--unquarantine", "-u") { Description = "Unquarantine the specified test(s)." };
        var optUrl = new Option<string?>("--url", "-i") { Description = "Issue URL required for quarantining (http/https)." };
        var optRoot = new Option<string?>("--root", "-r") { Description = "Tests root to scan (defaults to '<repo>/tests')." };
        var optAttribute = new Option<string?>("--attribute", "-a") { Description = "Fully-qualified attribute type to add/remove. If not specified, defaults based on --mode." };
        var optMode = new Option<string>("--mode", "-m") { Description = "Mode: 'quarantine' for QuarantinedTest or 'activeissue' for ActiveIssue (default: quarantine)." };
        optMode.DefaultValueFactory = _ => "quarantine";

        var argTests = new Argument<string[]>("tests") { Arity = ArgumentArity.ZeroOrMore, Description = "Fully-qualified test method name(s) like Namespace.Type.Method" };

        rootCommand.Options.Add(optQuarantine);
        rootCommand.Options.Add(optUnquarantine);
        rootCommand.Options.Add(optUrl);
        rootCommand.Options.Add(optRoot);
        rootCommand.Options.Add(optAttribute);
        rootCommand.Options.Add(optMode);
        rootCommand.Arguments.Add(argTests);

        rootCommand.SetAction(static (parseResult, token) =>
        {
            var quarantine = parseResult.GetValue<bool>("--quarantine");
            var unquarantine = parseResult.GetValue<bool>("--unquarantine");

            if (quarantine == unquarantine)
            {
                Console.Error.WriteLine("Specify exactly one of -q/--quarantine or -u/--unquarantine.");
                return Task.FromResult(1);
            }

            var tests = parseResult.GetValue<string[]?>("tests") ?? [];

            if (tests.Length == 0)
            {
                Console.Error.WriteLine("Specify at least one fully-qualified test method name.");
                return Task.FromResult(1);
            }

            var issueUrl = parseResult.GetValue<string?>("--url");
            var scanRoot = parseResult.GetValue<string?>("--root");
            var mode = parseResult.GetValue<string>("--mode") ?? "quarantine";
            var attributeFullName = parseResult.GetValue<string?>("--attribute");

            // Validate mode
            if (mode != "quarantine" && mode != "activeissue")
            {
                Console.Error.WriteLine("Mode must be 'quarantine' or 'activeissue'.");
                return Task.FromResult(1);
            }

            // If attribute not explicitly provided, use default based on mode
            if (string.IsNullOrWhiteSpace(attributeFullName))
            {
                attributeFullName = mode == "activeissue"
                    ? DefaultActiveIssueAttributeFullName
                    : DefaultQuarantinedTestAttributeFullName;
            }

            if (quarantine)
            {
                if (string.IsNullOrWhiteSpace(issueUrl))
                {
                    Console.Error.WriteLine("Quarantining requires an issue URL (--url or -i).");
                    return Task.FromResult(1);
                }
                if (!IsHttpUrl(issueUrl!))
                {
                    Console.Error.WriteLine("Quarantining requires a valid http(s) URL, e.g. https://github.com/org/repo/issues/1234.");
                    return Task.FromResult(1);
                }
            }

            return ExecuteAsync(
                    quarantine,
                    unquarantine,
                    tests.ToList(),
                    string.IsNullOrWhiteSpace(issueUrl) ? null : issueUrl,
                    scanRoot,
                    attributeFullName,
                    token);
        });

        return rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task<int> ExecuteAsync(bool quarantine, bool unquarantine, List<string> fullMethodNames, string? issueUrl, string? scanRootOverride, string attributeFullName, CancellationToken cancellationToken)
    {
        // Resolve repository root and tests folder
        var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
        var testsRoot = string.IsNullOrWhiteSpace(scanRootOverride)
                            ? Path.Combine(repoRoot, "tests")
                            : (Path.IsPathRooted(scanRootOverride!)
                                ? scanRootOverride!
                                : Path.GetFullPath(Path.Combine(repoRoot, scanRootOverride!)));

        if (!Directory.Exists(testsRoot))
        {
            Console.Error.WriteLine($"Tests folder not found at: {testsRoot}");
            return 2;
        }

        // Pre-parse targets for efficiency and group by method name for fast filtering
        var targets = fullMethodNames.Select(ParseFullMethodName).ToList();
        var targetsByMethod = targets
            .GroupBy(t => t.Method, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(t => t.PathPartsBeforeMethod).ToList(), StringComparer.Ordinal);

        // Build a single regex to prefilter files by method name (avoids N x Contains)
        var methodNamePrefilterRegex = BuildAnyMethodNameRegex(targetsByMethod.Keys);

        // Gather candidate source files under tests, ignoring build outputs and common heavy folders
        var csFiles = EnumerateCsFiles(testsRoot).ToList();

        var foundAnyCount = 0; // incremented if any target method found
        var modifiedFiles = new ConcurrentBag<string>();

        // Prep attribute handling based on configuration
        PrepareAttributeHandling(attributeFullName, out var attributeNameToInsert, out var attributeNamespaceToEnsure, out var isTargetAttribute);
        // Build a regex to quickly detect the attribute textually in files
        var attributePrefilterRegex = BuildAttributeRegex(attributeNamespaceToEnsure, attributeNameToInsert);

        // Parallel parse each file asynchronously, identify target methods, then add/remove attributes
        await Parallel.ForEachAsync(
            csFiles,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1), CancellationToken = cancellationToken },
            async (file, ct) =>
            {
                try
                {
                    // Cheap textual prefilter: if unquarantining, require attribute hint; otherwise require any target method name present
                    // This avoids Roslyn parsing for most files.
                    string text;
                    Encoding encoding;
                    try
                    {
                        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                        using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                        text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                        encoding = reader.CurrentEncoding;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[warn] Failed to read file: {file}. {ex.Message}");
                        return; // skip unreadable files
                    }

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    if (unquarantine)
                    {
                        // If attribute isn't present textually (regex), skip.
                        if (!attributePrefilterRegex.IsMatch(text))
                        {
                            return;
                        }
                    }
                    else if (quarantine) // quarantine
                    {
                        // If none of the target method names appear (regex), skip.
                        if (!methodNamePrefilterRegex.IsMatch(text))
                        {
                            return;
                        }
                    }

                    var newline = DetectNewLine(text);
                    var tree = CSharpSyntaxTree.ParseText(text, cancellationToken: ct);
                    var root = tree.GetCompilationUnitRoot(ct);

                    var methodNodes = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    var updates = new List<(MethodDeclarationSyntax original, MethodDeclarationSyntax updated)>();

                    foreach (var method in methodNodes)
                    {
                        // Fast filter by method name first
                        var name = method.Identifier.ValueText;
                        if (!targetsByMethod.TryGetValue(name, out var candidatePaths))
                        {
                            continue;
                        }

                        // Compute the enclosing namespace and nested type chain for the method
                        var (ns, typeChain) = GetEnclosingNames(method);
                        var actualParts = new List<string>(typeChain.Count + (string.IsNullOrEmpty(ns) ? 0 : ns.Count(c => c == '.') + 1));
                        if (!string.IsNullOrEmpty(ns))
                        {
                            actualParts.AddRange(ns.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        }
                        actualParts.AddRange(typeChain);

                        // Match any of the requested targets by enclosing paths
                        var matchesAny = candidatePaths.Any(cp => SequenceEquals(actualParts, cp));
                        if (!matchesAny)
                        {
                            continue;
                        }

                        Interlocked.Increment(ref foundAnyCount);

                        if (unquarantine)
                        {
                            var updated = RemoveTargetAttribute(method, isTargetAttribute, out var removed);
                            if (removed)
                            {
                                updates.Add((method, updated));
                            }
                        }
                        else if (quarantine) // quarantine
                        {
                            var updated = AddTargetAttribute(method, attributeNameToInsert, issueUrl ?? string.Empty, newline);
                            if (!ReferenceEquals(updated, method))
                            {
                                updates.Add((method, updated));
                            }
                        }
                    }

                    if (updates.Count == 0)
                    {
                        return;
                    }

                    // Replace nodes using a dictionary to avoid O(n^2) lookups
                    var map = updates.ToDictionary(t => t.original, t => t.updated);
                    var newRoot = root.ReplaceNodes(map.Keys, (orig, _) => map[orig]);

                    // Manage using directives for configured attribute namespace when using short name
                    if (quarantine)
                    {
                        if (!string.IsNullOrEmpty(attributeNamespaceToEnsure) && !attributeNameToInsert.Contains('.'))
                        {
                            newRoot = EnsureUsingDirective(newRoot, attributeNamespaceToEnsure!, newline);
                        }
                    }
                    else if (unquarantine)
                    {
                        if (!string.IsNullOrEmpty(attributeNamespaceToEnsure))
                        {
                            var anyLeft = newRoot.DescendantNodes().OfType<AttributeSyntax>().Any(isTargetAttribute);
                            if (!anyLeft)
                            {
                                newRoot = RemoveUsingDirective(newRoot, attributeNamespaceToEnsure!);
                            }
                        }
                    }

                    var newText = newRoot.ToFullString();
                    if (newText != text)
                    {
                        try
                        {
                            using var outStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true);
                            using var writer = new StreamWriter(outStream, encoding);
                            await writer.WriteAsync(newText.AsMemory(), ct).ConfigureAwait(false);
                            await writer.FlushAsync(ct).ConfigureAwait(false);
                            modifiedFiles.Add(file);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[warn] Failed to write file: {file}. {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[warn] Error processing {file}: {ex.Message}");
                }
            }).ConfigureAwait(false);

        if (foundAnyCount == 0)
        {
            Console.Error.WriteLine($"No method found matching any of: {string.Join(", ", fullMethodNames)}");
            return 3;
        }

        if (modifiedFiles.IsEmpty)
        {
            Console.WriteLine(quarantine
                ? "The test is already quarantined or no change was necessary."
                : "The test was already unquarantined or no change was necessary.");
            return 0;
        }

        var modified = modifiedFiles.Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal).ToList();
        Console.WriteLine($"Updated {modified.Count} file(s):");
        foreach (var f in modified)
        {
            Console.WriteLine($" - {Path.GetRelativePath(repoRoot, f)}");
        }

        return 0;
    }

    /// <summary>
    /// Enumerate .cs files under root while proactively skipping common heavy/irrelevant directories.
    /// Avoids the overhead of scanning into folders like bin, obj, .git, artifacts, node_modules, etc.
    /// </summary>
    private static IEnumerable<string> EnumerateCsFiles(string root)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        // Directories to skip by exact segment match
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".git", ".github", ".vscode", ".vs", "artifacts", "packages", "node_modules", "out", "dist", ".idea"
        };

        var dirs = new Stack<string>();
        dirs.Push(root);
        while (dirs.Count > 0)
        {
            var dir = dirs.Pop();
            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (var sd in subdirs)
            {
                var name = Path.GetFileName(sd);
                if (skip.Contains(name))
                {
                    continue;
                }
                dirs.Push(sd);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var f in files)
            {
                yield return f;
            }
        }
    }

    /// <summary>
    /// Builds a compiled regex that matches any appearance of the configured attribute name in text, including:
    /// - short name (e.g., QuarantinedTest)
    /// - with Attribute suffix (e.g., QuarantinedTestAttribute)
    /// - fully-qualified with namespace if provided (e.g., Aspire.TestUtilities.QuarantinedTest[Attribute])
    /// Word boundaries are enforced to avoid partial matches.
    /// </summary>
    private static Regex BuildAttributeRegex(string? attributeNamespace, string attributeShortName)
    {
        var variants = new List<string>
        {
            Regex.Escape(attributeShortName),
            Regex.Escape(attributeShortName + "Attribute")
        };

        if (!string.IsNullOrEmpty(attributeNamespace))
        {
            variants.Add(Regex.Escape(attributeNamespace + "." + attributeShortName));
            variants.Add(Regex.Escape(attributeNamespace + "." + attributeShortName + "Attribute"));
        }

        // Use alternation with word boundaries
        var pattern = $"\\b(?:{string.Join("|", variants.Distinct(StringComparer.Ordinal))})\\b";
        return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
    }

    /// <summary>
    /// Builds a compiled regex that matches any of the provided method names as whole words.
    /// Intended for fast prefiltering of files before Roslyn parsing.
    /// </summary>
    private static Regex BuildAnyMethodNameRegex(IEnumerable<string> methodNames)
    {
        var alts = methodNames
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(Regex.Escape)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var pattern = alts.Length == 0 ? "(?!)" : $"\\b(?:{string.Join("|", alts)})\\b";
        return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
    }

    /// <summary>
    /// Detects and returns the newline convention used by the file content: CRLF ("\r\n") if present,
    /// otherwise LF ("\n"). This ensures we preserve existing line endings when editing files.
    /// </summary>
    private static string DetectNewLine(string text)
    {
        // Respect existing file line endings. If any CRLF is present, use CRLF; otherwise use LF.
        // This avoids introducing Windows newlines on Unix files.
        if (text.Contains("\r\n"))
        {
            return "\r\n";
        }
        // Default to LF if no newlines or only LF are present
        return "\n";
    }

    /// <summary>
    /// Minimal validation that a string is an absolute http or https URL.
    /// </summary>
    private static bool IsHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// Walks up the directory tree from <paramref name="startDir"/> to locate the repository root
    /// (identified by the presence of a .git folder). Returns null if not found.
    /// </summary>
    private static string? FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Parses a fully-qualified method name like "A.B.Type+Nested.Method" into its enclosing path
    /// parts (namespace and nested type names) and the method name.
    /// </summary>
    private static (List<string> PathPartsBeforeMethod, string Method) ParseFullMethodName(string input)
    {
        var parts = input.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException($"Invalid method name '{input}'. Expected 'Namespace.Type.Method'.");
        }

        var method = parts[^1];
        var beforeMethod = parts.Take(parts.Length - 1)
            .SelectMany(p => p.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();
        return (beforeMethod, method);
    }

    /// <summary>
    /// From a syntax node inside a method, determines the file-scoped or block-scoped namespace name
    /// and the chain of enclosing types (including nested classes/structs/records/interfaces).
    /// </summary>
    private static (string Namespace, List<string> TypeChain) GetEnclosingNames(SyntaxNode node)
    {
        var typeNames = new List<string>();
        var ns = string.Empty;

        for (var current = node.Parent; current != null; current = current.Parent)
        {
            switch (current)
            {
                case ClassDeclarationSyntax cd:
                    typeNames.Insert(0, cd.Identifier.ValueText);
                    break;
                case StructDeclarationSyntax sd:
                    typeNames.Insert(0, sd.Identifier.ValueText);
                    break;
                case RecordDeclarationSyntax rd:
                    typeNames.Insert(0, rd.Identifier.ValueText);
                    break;
                case InterfaceDeclarationSyntax id:
                    typeNames.Insert(0, id.Identifier.ValueText);
                    break;
                case NamespaceDeclarationSyntax nd:
                    ns = nd.Name.ToString();
                    current = null; // break out
                    break;
                case FileScopedNamespaceDeclarationSyntax fsn:
                    ns = fsn.Name.ToString();
                    current = null; // break out
                    break;
            }
            if (current == null)
            {
                break;
            }
        }

        return (ns, typeNames);
    }

    /// <summary>
    /// Ordinal equality comparison for two string lists.
    /// </summary>
    private static bool SequenceEquals(List<string> a, List<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    // Creates a predicate that matches attributes against the configured attribute full name.
    private static void PrepareAttributeHandling(string attributeFullNameInput, out string attributeNameToInsert, out string? namespaceToEnsure, out Func<AttributeSyntax, bool> matcher)
    {
        // Normalize input: allow with or without namespace and with or without "Attribute" suffix
        attributeFullNameInput = attributeFullNameInput.Trim();
        var ns = string.Empty;
        var typeName = attributeFullNameInput;
        var lastDot = attributeFullNameInput.LastIndexOf('.');
        if (lastDot >= 0)
        {
            ns = attributeFullNameInput.Substring(0, lastDot);
            typeName = attributeFullNameInput.Substring(lastDot + 1);
        }

        // Determine preferred short name (without Attribute suffix if present)
        var shortName = typeName;
        var shortNameNoSuffix = shortName.EndsWith("Attribute", StringComparison.Ordinal) ? shortName.Substring(0, shortName.Length - "Attribute".Length) : shortName;

        // We'll insert short name (without namespace), relying on a using directive if ns provided
        attributeNameToInsert = shortNameNoSuffix;
        namespaceToEnsure = string.IsNullOrEmpty(ns) ? null : ns;

        // Build a matcher that accepts qualified and unqualified, with or without Attribute suffix, matching configured ns/type
        matcher = attr =>
        {
            var full = attr.Name.ToString(); // may be qualified or not
                                             // Extract right-most identifier for suffix/no-suffix comparison
            var rightMost = attr.Name switch
            {
                IdentifierNameSyntax ins => ins.Identifier.ValueText,
                QualifiedNameSyntax qns => (qns.Right as IdentifierNameSyntax)?.Identifier.ValueText ?? qns.Right.ToString(),
                AliasQualifiedNameSyntax aqn => (aqn.Name as IdentifierNameSyntax)?.Identifier.ValueText ?? aqn.Name.ToString(),
                _ => full.Split('.').Last(),
            };
            var rightMatches = string.Equals(rightMost, shortNameNoSuffix, StringComparison.Ordinal)
                || string.Equals(rightMost, shortNameNoSuffix + "Attribute", StringComparison.Ordinal);

            if (!rightMatches)
            {
                return false;
            }

            if (string.IsNullOrEmpty(ns))
            {
                // No namespace constraint supplied; accept any ns as long as right-most matches
                return true;
            }

            // If a namespace is supplied, ensure qualification (if any) ends with the same right-most
            // and the left part (if present) matches provided namespace exactly
            if (attr.Name is QualifiedNameSyntax qn)
            {
                var leftNs = qn.Left.ToString();
                return string.Equals(leftNs, ns, StringComparison.Ordinal);
            }
            // Unqualified attribute in code but ns required; allow match because we will also add the using for that ns
            return true;
        };
    }

    /// <summary>
    /// Removes the [QuarantinedTest] attribute from a method, if present. Returns the (potentially)
    /// modified method and flags via <paramref name="removed"/> whether a change occurred.
    /// </summary>
    private static MethodDeclarationSyntax RemoveTargetAttribute(MethodDeclarationSyntax method, Func<AttributeSyntax, bool> isTargetAttribute, out bool removed)
    {
        removed = false;
        if (method.AttributeLists.Count == 0)
        {
            return method;
        }

        var newLists = new List<AttributeListSyntax>();
        foreach (var list in method.AttributeLists)
        {
            var remaining = list.Attributes.Where(a => !isTargetAttribute(a)).ToList();
            if (remaining.Count == list.Attributes.Count)
            {
                newLists.Add(list);
                continue;
            }

            removed = true;
            if (remaining.Count > 0)
            {
                var newList = list.WithAttributes(SyntaxFactory.SeparatedList(remaining));
                newLists.Add(newList);
            }
        }

        return removed ? method.WithAttributeLists(SyntaxFactory.List(newLists)) : method;
    }

    /// <summary>
    /// Adds the configured attribute (optionally with an issue URL) to a method if one does
    /// not already exist. Preserves indentation and ensures a clean newline layout.
    /// </summary>
    private static MethodDeclarationSyntax AddTargetAttribute(MethodDeclarationSyntax method, string attributeNameToInsert, string issueUrl, string newline)
    {
        foreach (var list in method.AttributeLists)
        {
            // If any attribute with the same right-most identifier (with/without suffix) exists, skip adding
            if (list.Attributes.Any(a =>
            {
                var id = a.Name switch
                {
                    IdentifierNameSyntax ins => ins.Identifier.ValueText,
                    QualifiedNameSyntax qns => (qns.Right as IdentifierNameSyntax)?.Identifier.ValueText ?? qns.Right.ToString(),
                    AliasQualifiedNameSyntax aqn => (aqn.Name as IdentifierNameSyntax)?.Identifier.ValueText ?? aqn.Name.ToString(),
                    _ => a.Name.ToString().Split('.').Last()
                };
                return string.Equals(id, attributeNameToInsert, StringComparison.Ordinal)
                    || string.Equals(id, attributeNameToInsert + "Attribute", StringComparison.Ordinal);
            }))
            {
                return method;
            }
        }

        // Use provided attribute name as-is (can be short or qualified)
        var attrName = SyntaxFactory.ParseName(attributeNameToInsert);
        var attrArgs = string.IsNullOrWhiteSpace(issueUrl)
            ? null
            : SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(issueUrl)))));

        var attr = SyntaxFactory.Attribute(attrName, attrArgs);
        var newList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr));

        if (method.AttributeLists.Count > 0)
        {
            // Append after existing attributes.
            // Ensure there is exactly one newline between the previous attribute and the new one.
            // If the previous attribute does not end with a newline (e.g., attributes were on the same line
            // as the method signature), add a leading newline to the new attribute so it starts on the next line.
            var last = method.AttributeLists[method.AttributeLists.Count - 1];
            var indentation = SyntaxFactory.TriviaList(last.GetLeadingTrivia().Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)));
            var lastEndsWithNewline = last.GetTrailingTrivia().Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            var leading = lastEndsWithNewline
                ? indentation
                : indentation.Add(SyntaxFactory.EndOfLine(newline));
            newList = newList
                .WithLeadingTrivia(leading)
                .WithTrailingTrivia(SyntaxFactory.EndOfLine(newline));
        }
        else
        {
            var leading = method.GetLeadingTrivia();
            newList = newList.WithLeadingTrivia(leading)
                             .WithTrailingTrivia(SyntaxFactory.EndOfLine(newline));
        }

        var newLists = method.AttributeLists.Add(newList);
        var updated = method.WithAttributeLists(newLists);
        return updated;
    }

    // Removed legacy Options/ParseArgs in favor of System.CommandLine
    /// <summary>
    /// Ensures a <c>using &lt;namespaceName&gt;;</c> directive is present at the compilation unit level.
    /// Respects existing file trivia and newline style.
    /// </summary>
    private static CompilationUnitSyntax EnsureUsingDirective(CompilationUnitSyntax root, string namespaceName, string newline)
    {
        // If a matching using already exists, do nothing
        if (root.Usings.Any(u => u.Name != null && u.Name.ToString() == namespaceName))
        {
            return root;
        }
        // Create a using directive with a trailing newline, but avoid inserting an extra
        // leading blank line when appending to an existing list of usings.
        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .WithUsingKeyword(
                SyntaxFactory.Token(SyntaxKind.UsingKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space))
            .WithSemicolonToken(
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                    .WithTrailingTrivia(SyntaxFactory.EndOfLine(newline)));

        // Only add a leading newline if there are no existing usings and the file
        // already has content that would otherwise run into the using. In typical
        // cases (either first using at top of file, or appending after other usings),
        // no leading trivia is needed.
        if (root.Usings.Count == 0)
        {
            // If the file has any leading trivia (e.g., license header) that ends
            // without a newline, ensure there's a newline before the first using.
            var leadingTrivia = root.GetLeadingTrivia();
            var endsWithNewline = leadingTrivia.Count > 0 &&
                leadingTrivia.Last().IsKind(SyntaxKind.EndOfLineTrivia);

            if (!endsWithNewline)
            {
                usingDirective = usingDirective.WithLeadingTrivia(SyntaxFactory.EndOfLine(newline));
            }
        }

        return root.WithUsings(root.Usings.Add(usingDirective));
    }

    /// <summary>
    /// Removes all occurrences of <c>using &lt;namespaceName&gt;;</c> from the file, if present. This is
    /// called after unquarantining when no QuarantinedTest attributes remain in the file.
    /// </summary>
    private static CompilationUnitSyntax RemoveUsingDirective(CompilationUnitSyntax root, string namespaceName)
    {
        // Remove matching using directives wherever they appear in the tree
        var nodesToRemove = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name != null && u.Name.ToString() == namespaceName)
            .ToList();
        CompilationUnitSyntax updated;
        if (nodesToRemove.Count > 0)
        {
            updated = (CompilationUnitSyntax)root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
        }
        else
        {
            updated = root;
        }

        // Also ensure the compilation unit usings are filtered (in case any remain)
        if (updated.Usings.Count > 0)
        {
            var filtered = updated.Usings.Where(u => u.Name == null || u.Name.ToString() != namespaceName).ToList();
            updated = updated.WithUsings(SyntaxFactory.List(filtered));
        }

        // Fallback: if textual occurrence remains, strip it textually and reparse
        var text = updated.ToFullString();
        if (text.Contains($"using {namespaceName};"))
        {
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                $@"^\s*using\s+{System.Text.RegularExpressions.Regex.Escape(namespaceName)}\s*;\s*\r?\n",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);
            updated = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
        }

        return updated;
    }
}
