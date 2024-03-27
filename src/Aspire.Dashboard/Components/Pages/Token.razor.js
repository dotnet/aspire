export async function validateToken(token) {
    var response = await fetch('/validate-token?token=sdfsdf', {
        
    });

    return response.text();
}
