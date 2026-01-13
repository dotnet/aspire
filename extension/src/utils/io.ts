import { access, stat } from 'fs/promises';

export async function doesFileExist(filePath: string): Promise<boolean> {
    try {
        await access(filePath);
        return true;
    } catch {
        return false;
    }
}

export async function isDirectory(path: string): Promise<boolean> {
    try {
        const statResult = await stat(path);
        return statResult.isDirectory();
    } catch {
        return false;
    }
}
