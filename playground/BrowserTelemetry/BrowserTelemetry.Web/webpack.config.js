const path = require('path');

module.exports = {
    entry: './Scripts/index.js',
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'wwwroot/scripts'),
        library: 'BrowserTelemetry'
    },
    mode: 'production'
};
