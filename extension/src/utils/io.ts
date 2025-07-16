import { access } from 'fs/promises';

export async function doesFileExist(filePath: string): Promise<boolean> {
    try {
        await access(filePath);
        return true;
    } catch {
        return false;
    }
}