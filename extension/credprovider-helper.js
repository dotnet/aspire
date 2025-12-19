const { spawn } = require('node:child_process');

const child = spawn('npx', ['@microsoft/artifacts-npm-credprovider'], {
    env: {
        ...process.env,
        npm_config_registry: 'https://pkgs.dev.azure.com/artifacts-public/PublicTools/_packaging/AzureArtifacts/npm/registry/',
    },
    stdio: 'inherit',
    shell: false,
});

child.on('exit', code => process.exit(code));
