import * as vscode from 'vscode';

export function getAspireTerminal(): vscode.Terminal {
    const terminalName = 'Aspire Terminal';

    const existingTerminal = vscode.window.terminals.find(terminal => terminal.name === terminalName);
    if (existingTerminal) {
        return existingTerminal;
    } else {
        return vscode.window.createTerminal(terminalName);
    }
}

type CommandFlag = {
    singleDash?: boolean;
    name: string;
    value?: string;
};

export function buildCliCommand(executable: string, args: string | undefined, flags: CommandFlag[] | undefined) {
    const commandParts: string[] = [executable];

    if (args) {
        commandParts.push(args);
    }

    if (flags) {
        flags.forEach(flag => {
            const flagPrefix = flag.singleDash ? '-' : '--';
            commandParts.push(`${flagPrefix}${flag.name}`);
            // If the flag has a value, append it to the command
            if (flag.value) {
                commandParts.push(" " + flag.value);
            }
        });
    }

    return commandParts.join(' ');
}