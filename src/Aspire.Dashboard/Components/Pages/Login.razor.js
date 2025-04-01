export async function validateToken(token) {
    try {
        var url = `api/validatetoken?token=${encodeURIComponent(token)}`;
        var response = await fetch(url, { method: 'POST' });
        return response.text();
    } catch (ex) {
        return `Error validating token: ${ex}`;
    }
}
