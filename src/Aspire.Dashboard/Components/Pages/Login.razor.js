export function validateToken(token) {
    // Module import errors in the browser could have been caused by this function using async/await and some browsers not supporting it.
    // Use promises instead.
    var url = `/api/validatetoken?token=${encodeURIComponent(token)}`;
    return fetch(url, { method: 'POST' })
        .then(response => response.text())
        .catch(ex => `Error validating token: ${ex}`);
}
