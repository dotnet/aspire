const http = require('http');
const port = process.env.PORT ?? 3000;

const server = http.createServer((req, res) => {
    res.statusCode = 200;
    res.setHeader('Content-Type', 'text/plain');
    if (process.env.npm_lifecycle_event === undefined) {
        res.end('Hello from node!');
    } else {
        res.end('Hello from npm!');
    }
});

server.listen(port, () => {
    console.log('Web server running on on %s', port);
});
