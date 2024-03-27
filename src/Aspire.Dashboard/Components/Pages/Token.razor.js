export async function validateToken(token) {
    var response = await fetch(`/validate-token?token=${encodeURIComponent(token)}`);
    return response.text();
}
