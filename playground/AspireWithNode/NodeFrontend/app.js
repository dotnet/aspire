import http from 'node:http';
import https from 'node:https';
import fs from 'node:fs';
import fetch from 'node-fetch';
import express from 'express';
import { createTerminus, HealthCheckError } from '@godaddy/terminus';
import { createClient } from 'redis';

// Read configuration from environment variables
const config = {
    environment: process.env.NODE_ENV || 'development',
    httpPort: process.env['PORT'] ?? 8080,
    httpsPort: process.env['HTTPS_PORT'] ?? 8443,
    httpsRedirectPort: process.env['HTTPS_REDIRECT_PORT'] ?? (process.env['HTTPS_PORT'] ?? 8443),
    certFile: process.env['HTTPS_CERT_FILE'] ?? '',
    certKeyFile: process.env['HTTPS_CERT_KEY_FILE'] ?? '',
    cacheAddress: process.env['ConnectionStrings__cache'] ?? '',
    apiServer: process.env['services__weatherapi__https__0'] ?? process.env['services__weatherapi__http__0']
};
console.log(`config: ${JSON.stringify(config)}`);

// Setup HTTPS options
const httpsOptions = fs.existsSync(config.certFile) && fs.existsSync(config.certKeyFile)
    ? {
        cert: fs.readFileSync(config.certFile),
        key: fs.readFileSync(config.certKeyFile),
        enabled: true
    }
    : { enabled: false };

// Setup connection to Redis cache
const passwordPrefix = ',password=';
let cacheConfig = {
    url: `redis://${config.cacheAddress}`
};

const cachePasswordIndex = config.cacheAddress.indexOf(passwordPrefix);
if (cachePasswordIndex > 0) {
    cacheConfig = {
        url: `redis://${config.cacheAddress.substring(0, cachePasswordIndex)}`,
        password: config.cacheAddress.substring(cachePasswordIndex + passwordPrefix.length)
    }
}

const cache = createClient(cacheConfig);
cache.on('error', err => console.error('Redis Client Error', err));
await cache.connect();

// Setup express app
const app = express();

// Middleware to redirect HTTP to HTTPS
function httpsRedirect(req, res, next) {
    if (req.secure || req.headers['x-forwarded-proto'] === 'https') {
        // Request is already HTTPS
        return next();
    }
    // Redirect to HTTPS
    const redirectTo = new URL(`https://${process.env.HOST ?? 'localhost'}:${config.httpsRedirectPort}${req.url}`);
    console.log(`Redirecting to ${redirectTo}`);
    res.redirect(redirectTo);
}
if (httpsOptions.enabled) {
    app.use(httpsRedirect);
}

app.get('/', async (req, res) => {
    const cachedForecasts = await cache.get('forecasts');
    if (cachedForecasts) {
        res.render('index', { forecasts: JSON.parse(cachedForecasts) });
        return;
    }

    const response = await fetch(`${config.apiServer}/weatherforecast`);
    const forecasts = await response.json();
    await cache.set('forecasts', JSON.stringify(forecasts), { 'EX': 5 });
    res.render('index', { forecasts: forecasts });
});

// Configure templating
app.set('views', './views');
app.set('view engine', 'pug');

// Define health check callback
async function healthCheck() {
    const errors = [];
    const apiServerHealthAddress = `${config.apiServer}/health`;
    console.log(`Fetching ${apiServerHealthAddress}`);
    try {
        var response = await fetch(apiServerHealthAddress);
        if (!response.ok) {
            console.log(`Failed fetching ${apiServerHealthAddress}. ${response.status}`);
            throw new HealthCheckError(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${response.status}`);
        }
    } catch (error) {
        console.log(`Failed fetching ${apiServerHealthAddress}. ${error}`);
        throw new HealthCheckError(`Fetching ${apiServerHealthAddress} failed with HTTP status: ${error}`);
    }
}

// Start a server
function startServer(server, port) {
    if (server) {
        const serverType = server instanceof https.Server ? 'HTTPS' : 'HTTP';

        // Create the health check endpoint
        createTerminus(server, {
            signal: 'SIGINT',
            healthChecks: {
                '/health': healthCheck,
                '/alive': () => { }
            },
            onSignal: async () => {
                console.log('server is starting cleanup');
                console.log('closing Redis connection');
                await cache.disconnect();
            },
            onShutdown: () => console.log('cleanup finished, server is shutting down')
        });

        // Start the server
        server.listen(port, () => {
            console.log(`${serverType} listening on ${JSON.stringify(server.address())}`);
        });
    }
}

const httpServer = http.createServer(app);
const httpsServer = httpsOptions.enabled ? https.createServer(httpsOptions, app) : null;

startServer(httpServer, config.httpPort);
startServer(httpsServer, config.httpsPort);
