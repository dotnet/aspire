import * as vscode from 'vscode';
import { isPythonInstalled } from '../capabilities';
import { pythonExtensionRecommendationTitle } from '../loc/strings';
import { extensionLogOutputChannel } from '../utils/logging';

/**
 * Configuration for extension recommendation rules
 */
interface ExtensionRecommendation {
    /** The resource type keyword to look for (case-insensitive) */
    resourceTypeKeyword: string;
    /** The extension ID to recommend */
    extensionId: string;
    /** The display name of the extension */
    extensionName: string;
    /** Function to check if the extension is already installed */
    isInstalled: () => boolean;
    /** Optional: Custom command to execute (defaults to workbench.extensions.installExtension) */
    command?: {
        command: string;
        args?: any[];
    };
    title: string;
}

/**
 * Provides code lens hints for C# code, particularly for Aspire-related scenarios
 */
export class CSharpCodeLensProvider implements vscode.CodeLensProvider {
    private _onDidChangeCodeLenses = new vscode.EventEmitter<void>();
    public readonly onDidChangeCodeLenses = this._onDidChangeCodeLenses.event;

    /**
     * Extension recommendations based on return type keywords.
     *
     * To add a new recommendation:
     * 1. Add a new entry to this array
     * 2. Set resourceTypeKeyword to match text in the return type (case-insensitive)
     *    - For Python resources: looks for "Python" in IResourceBuilder<PythonAppResource>
     *    - For Node resources: looks for "Node" in IResourceBuilder<NodeAppResource>
     * 3. Specify the VS Code extension ID to recommend
     * 4. Provide a display name for the extension
     * 5. Add an isInstalled check (add to capabilities.ts if needed)
     * 6. (Optional) Customize the command, title, or tooltip
     *
     * Example:
     *   AddUvicornApp() → IResourceBuilder<PythonAppResource> → matches "Python" keyword
     *   AddNpmApp() → IResourceBuilder<NodeAppResource> → matches "Node" keyword
     */
    private readonly recommendations: ExtensionRecommendation[] = [
        {
            resourceTypeKeyword: 'Python',
            extensionId: 'ms-python.python',
            extensionName: 'Python',
            isInstalled: isPythonInstalled,
            title: pythonExtensionRecommendationTitle
        }
    ];

    async provideCodeLenses(
        document: vscode.TextDocument,
        token: vscode.CancellationToken
    ): Promise<vscode.CodeLens[]> {
        const codeLenses: vscode.CodeLens[] = [];

        // Only process C# files
        if (document.languageId !== 'csharp') {
            return codeLenses;
        }

        // Filter out recommendations for extensions that are already installed
        const activeRecommendations = this.recommendations.filter(rec => !rec.isInstalled());

        if (activeRecommendations.length === 0) {
            // All relevant extensions are already installed
            return codeLenses;
        }

        // Look for .AddX method calls and check their return types
        const addMethodPattern = /\.Add\w+\s*\(/g;
        const text = document.getText();
        const lines = text.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];

            let match;
            addMethodPattern.lastIndex = 0; // Reset regex
            while ((match = addMethodPattern.exec(line)) !== null) {
                const position = new vscode.Position(i, match.index);

                try {
                    // First, check if this is being called on IDistributedApplicationBuilder
                    // by checking the type of the object before the method call
                    const objectPosition = new vscode.Position(i, Math.max(0, match.index - 1));
                    const objectType = await this.getReturnType(document, objectPosition, token);

                    // Only proceed if this is called on IDistributedApplicationBuilder or its fluent chain
                    if (!objectType || !this.isDistributedApplicationBuilderType(objectType)) {
                        continue;
                    }

                    // Get the return type information from hover
                    const returnType = await this.getReturnType(document, position, token);

                    if (returnType) {
                        // Check if this type matches any of our active recommendations
                        for (const recommendation of activeRecommendations) {
                            if (this.typeContainsKeyword(returnType, recommendation.resourceTypeKeyword)) {
                                // Place the code lens at the start of the line to avoid showing method hover
                                const range = new vscode.Range(i, 0, i, 0);
                                const codeLens = new vscode.CodeLens(range);

                                // Use custom command or default to installing the extension
                                const command = recommendation.command ?? {
                                    command: 'workbench.extensions.installExtension',
                                    args: [recommendation.extensionId]
                                };

                                codeLens.command = {
                                    title: recommendation.title,
                                    command: command.command,
                                    arguments: command.args
                                };

                                codeLenses.push(codeLens);

                                // Only add one code lens per line
                                break;
                            }
                        }
                    }
                } catch (error) {
                    // Log error but continue to avoid breaking the entire CodeLens provider
                    const errorMessage = error instanceof Error ? error.message : String(error);
                    extensionLogOutputChannel.warn(
                        `Error getting return type for CodeLens at ${document.uri.fsPath}:${i + 1}:${match.index}: ${errorMessage}`
                    );
                    continue;
                }
            }
        }

        return codeLenses;
    }

    /**
     * Gets the return type from hover information
     * Protected to allow testing through subclass
     */
    protected async getReturnType(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken
    ): Promise<string | null> {
        try {
            const hovers = await vscode.commands.executeCommand<vscode.Hover[]>(
                'vscode.executeHoverProvider',
                document.uri,
                position
            );

            if (!hovers || hovers.length === 0) {
                return null;
            }

            // Get the text from all hover contents
            let fullText = '';
            for (const hover of hovers) {
                for (const content of hover.contents) {
                    const text = typeof content === 'string'
                        ? content
                        : content instanceof vscode.MarkdownString
                            ? content.value
                            : '';
                    fullText += text + '\n';
                }
            }

            // Just return the full text - we'll search for keywords in it
            return fullText;
        } catch (error) {
            return null;
        }
    }

    /**
     * Checks if a type contains a keyword (case-insensitive)
     */
    private typeContainsKeyword(typeText: string, keyword: string): boolean {
        const regex = new RegExp(keyword, 'i');
        return regex.test(typeText);
    }

    /**
     * Checks if the type is IDistributedApplicationBuilder or IResourceBuilder
     * (which is returned by .AddX methods and allows fluent chaining)
     */
    private isDistributedApplicationBuilderType(typeText: string): boolean {
        // Check for IDistributedApplicationBuilder or IResourceBuilder
        return /IDistributedApplicationBuilder|IResourceBuilder/i.test(typeText);
    }
}
