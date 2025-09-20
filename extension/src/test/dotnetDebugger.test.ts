import * as assert from 'assert';
import * as sinon from 'sinon';
import * as vscode from 'vscode';
import { projectDebuggerExtension, buildDotNetProject, getDotNetTargetPath } from '../debugger/languages/dotnet';
import { AspireResourceExtendedDebugConfiguration } from '../dcp/types';
import * as io from '../utils/io';
import * as launchProfiles from '../debugger/launchProfiles';

suite('Dotnet Debugger Extension Tests', () => {
    teardown(() => sinon.restore());

    test('createDebugSessionConfigurationCallback works when C# dev kit is not installed', async () => {
        const projectPath = 'C:\\temp\\TestProject.csproj';
        const outputPath = 'C:\\temp\\bin\\Debug\\net7.0\\TestProject.dll';

        const dotnetModule = require('../debugger/languages/dotnet');
        sinon.stub(dotnetModule, 'getDotNetTargetPath').resolves(outputPath);

        sinon.stub(io, 'doesFileExist').resolves(false);

        // Simulate C# dev kit not installed
        sinon.stub(vscode.extensions, 'getExtension').withArgs('ms-dotnettools.csdevkit').returns(undefined);

        const launchConfig: any = {
            type: 'project',
            project_path: projectPath
        };

        const debugConfig: AspireResourceExtendedDebugConfiguration = { runId: '1', debugSessionId: null } as any;

        await (projectDebuggerExtension as any).createDebugSessionConfigurationCallback(launchConfig, [], [], { debug: true, runId: '1', debugSessionId: null }, debugConfig);

        assert.strictEqual(debugConfig.program, outputPath);
    });

    test('createDebugSessionConfigurationCallback calls buildDotNetProject on successful build', async () => {
        const projectPath = 'C:\\temp\\TestProject.csproj';
        const outputPath = 'C:\\temp\\bin\\Debug\\net7.0\\TestProject.dll';

        const dotnetModule = require('../debugger/languages/dotnet');
        sinon.stub(dotnetModule, 'getDotNetTargetPath').resolves(outputPath);
        sinon.stub(io, 'doesFileExist').resolves(false);

        const buildStub = sinon.stub(dotnetModule, 'buildDotNetProject').resolves();

        const launchConfig: any = {
            type: 'project',
            project_path: projectPath
        };

        const debugConfig: AspireResourceExtendedDebugConfiguration = { runId: '1', debugSessionId: null } as any;

        await (projectDebuggerExtension as any).createDebugSessionConfigurationCallback(launchConfig, ['arg1'], [], { debug: true, runId: '1', debugSessionId: null }, debugConfig);

        assert.strictEqual(debugConfig.program, outputPath);
        assert.strictEqual(buildStub.calledOnceWithExactly(projectPath), true);
    });

    test('createDebugSessionConfigurationCallback rejects when build fails', async () => {
        const projectPath = 'C:\\temp\\TestProject.csproj';
        const outputPath = 'C:\\temp\\bin\\Debug\\net7.0\\TestProject.dll';

        const dotnetModule = require('../debugger/languages/dotnet');
        sinon.stub(dotnetModule, 'getDotNetTargetPath').resolves(outputPath);
        sinon.stub(io, 'doesFileExist').resolves(false);

        const buildStub = sinon.stub(dotnetModule, 'buildDotNetProject').rejects(new Error('build failed'));

        const launchConfig: any = {
            type: 'project',
            project_path: projectPath
        };

        const debugConfig: AspireResourceExtendedDebugConfiguration = { runId: '1', debugSessionId: null } as any;

        await (projectDebuggerExtension as any).createDebugSessionConfigurationCallback(launchConfig, [], [], { debug: true, runId: '1', debugSessionId: null }, debugConfig);

        assert.strictEqual(buildStub.calledOnceWithExactly(projectPath), true);
    });

    test('applies launch profile settings to debug configuration', async () => {
        const fs = require('fs');
        const os = require('os');
        const path = require('path');

        const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'aspire-test-'));
        const projectDir = path.join(tempDir, 'TestProject');
        const propertiesDir = path.join(projectDir, 'Properties');
        fs.mkdirSync(propertiesDir, { recursive: true });

        const projectPath = path.join(projectDir, 'TestProject.csproj');
        fs.writeFileSync(projectPath, '<Project></Project>');

        const launchSettings = {
            profiles: {
                'Development': {
                    commandLineArgs: '--arg "value" --flag',
                    environmentVariables: {
                        BASE: 'base'
                    },
                    workingDirectory: 'custom',
                    executablePath: 'exePath',
                    useSSL: true,
                    launchBrowser: true,
                    applicationUrl: 'https://localhost:5001'
                }
            }
        };

        fs.writeFileSync(path.join(propertiesDir, 'launchSettings.json'), JSON.stringify(launchSettings, null, 2));

        const outputPath = path.join(projectDir, 'bin', 'Debug', 'net7.0', 'TestProject.dll');

        const dotnetModule = require('../debugger/languages/dotnet');
        sinon.stub(dotnetModule, 'getDotNetTargetPath').resolves(outputPath);
        sinon.stub(io, 'doesFileExist').resolves(true); // file exists -> no build

        const launchConfig: any = {
            type: 'project',
            project_path: projectPath,
            launch_profile: 'Development'
        };

        // Provide a run session env that overrides BASE and adds RUN
        const runEnv = [
            { name: 'BASE', value: 'overridden' },
            { name: 'RUN', value: 'run' }
        ];

        const debugConfig: any = { runId: '1', debugSessionId: null };

        await (projectDebuggerExtension as any).createDebugSessionConfigurationCallback(launchConfig, undefined as any, runEnv, { debug: true, runId: '1', debugSessionId: null }, debugConfig);

        // program should be set
        assert.strictEqual(debugConfig.program, outputPath);

        // cwd should resolve to projectDir/custom
        assert.strictEqual(debugConfig.cwd, path.join(projectDir, 'custom'));

        // args should be parsed from commandLineArgs
        assert.deepStrictEqual(debugConfig.args, ['--arg', 'value', '--flag']);

        // env should include merged values with run session overriding base
        assert.strictEqual(debugConfig.env.BASE, 'overridden');
        assert.strictEqual(debugConfig.env.RUN, 'run');

        // executablePath and checkForDevCert
        assert.strictEqual(debugConfig.executablePath, 'exePath');
        assert.strictEqual(debugConfig.checkForDevCert, true);

        // serverReadyAction should be present with the applicationUrl
        assert.notStrictEqual(debugConfig.serverReadyAction, undefined);
        assert.strictEqual(debugConfig.serverReadyAction.uriFormat, 'https://localhost:5001');

        // cleanup
        fs.rmSync(tempDir, { recursive: true, force: true });
    });
});
