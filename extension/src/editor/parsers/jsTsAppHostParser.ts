import * as vscode from 'vscode';
import { AppHostResourceParser, ParsedResource, registerParser } from './AppHostResourceParser';

/**
 * JavaScript / TypeScript AppHost resource parser.
 * Detects AppHost files via Aspire module imports, then extracts .add*("name") calls.
 */
class JsTsAppHostParser implements AppHostResourceParser {
    getSupportedExtensions(): string[] {
        return ['.ts', '.js'];
    }

    isAppHostFile(document: vscode.TextDocument): boolean {
        const text = document.getText();
        // Match @aspire package imports, local aspire module imports (e.g. ./.modules/aspire.js),
        // or the createBuilder() entry point from the Aspire TS SDK.
        return /(?:from\s+['"](?:@aspire|[^'"]*aspire[^'"]*)|require\s*\(\s*['"](?:@aspire|[^'"]*aspire[^'"]*))/.test(text)
            || /\bcreateBuilder\s*\(/.test(text);
    }

    parseResources(document: vscode.TextDocument): ParsedResource[] {
        const text = document.getText();
        const results: ParsedResource[] = [];

        // Match .addXyz("name") or .addXyz('name') patterns
        const pattern = /\.(add\w+)\s*\(\s*(['"])([^'"]+)\2/gi;
        let match: RegExpExecArray | null;

        while ((match = pattern.exec(text)) !== null) {
            const methodName = match[1];
            const resourceName = match[3];

            const matchStart = match.index;
            const startPos = document.positionAt(matchStart);
            const endPos = document.positionAt(matchStart + match[0].length);

            results.push({
                name: resourceName,
                methodName: methodName,
                range: new vscode.Range(startPos, endPos),
                kind: methodName.toLowerCase() === 'addstep' ? 'pipelineStep' : 'resource',
            });
        }

        return results;
    }
}

// Self-register on import
registerParser(new JsTsAppHostParser());
