import * as vscode from 'vscode';

export async function getAndActivateExtension() {
	const extension = vscode.extensions.getExtension('aspire-vscode') || vscode.extensions.all.find(e => e.id.endsWith('aspire-vscode'));
	if (!extension) {
		throw new Error('Extension not found');
	}

	await extension.activate();
	return extension;
}
