import * as assert from 'assert';
import * as path from 'path';
import * as fs from 'fs';
import * as os from 'os';
import {
    determineBaseLaunchProfile,
    mergeEnvironmentVariables,
    determineArguments,
    determineWorkingDirectory,
    determineServerReadyAction,
    readLaunchSettings,
    LaunchSettings,
    LaunchProfile
} from '../debugger/launchProfiles';
import { ExecutableLaunchConfiguration, EnvVar, ProjectLaunchConfiguration } from '../dcp/types';

suite('Launch Profile Tests', () => {
    suite('determineBaseLaunchProfile', () => {
        const sampleLaunchSettings: LaunchSettings = {
            profiles: {
                'Development': {
                    environmentVariables: {
                        ASPNETCORE_ENVIRONMENT: 'Development'
                    }
                },
                'Production': {
                    environmentVariables: {
                        ASPNETCORE_ENVIRONMENT: 'Production'
                    }
                }
            }
        };

        test('returns null when disable_launch_profile is true', () => {
            const launchConfig: ProjectLaunchConfiguration = {
                type: 'project',
                project_path: '/test/project.csproj',
                disable_launch_profile: true
            };

            const result = determineBaseLaunchProfile(launchConfig, sampleLaunchSettings);

            assert.strictEqual(result.profile, null);
            assert.strictEqual(result.profileName, null);
        });

        test('returns null when no launch settings available', () => {
            const launchConfig: ProjectLaunchConfiguration = {
                type: 'project',
                project_path: '/test/project.csproj'
            };

            const result = determineBaseLaunchProfile(launchConfig, null);

            assert.strictEqual(result.profile, null);
            assert.strictEqual(result.profileName, null);
        });

        test('returns explicit launch profile when specified and exists', () => {
            const launchConfig: ProjectLaunchConfiguration = {
                type: 'project',
                project_path: '/test/project.csproj',
                launch_profile: 'Development'
            };

            const result = determineBaseLaunchProfile(launchConfig, sampleLaunchSettings);

            assert.strictEqual(result.profileName, 'Development');
            assert.strictEqual(result.profile?.environmentVariables?.ASPNETCORE_ENVIRONMENT, 'Development');
        });

        test('returns null when explicit launch profile specified but does not exist', () => {
            const launchConfig: ProjectLaunchConfiguration = {
                type: 'project',
                project_path: '/test/project.csproj',
                launch_profile: 'NonExistent'
            };

            const result = determineBaseLaunchProfile(launchConfig, sampleLaunchSettings);

            assert.strictEqual(result.profile, null);
            assert.strictEqual(result.profileName, null);
        });
    });

    suite('mergeEnvironmentVariables', () => {
        test('merges environment variables with run session taking precedence', () => {
            const baseProfileEnv = {
                'VAR1': 'base1',
                'VAR2': 'base2',
                'VAR3': 'base3'
            };

            const runSessionEnv: EnvVar[] = [
                { name: 'VAR2', value: 'session2' },
                { name: 'VAR4', value: 'session4' }
            ];

            const result = mergeEnvironmentVariables(baseProfileEnv, runSessionEnv);

            assert.strictEqual(result.length, 4);

            const resultMap = new Map(result);
            assert.strictEqual(resultMap.get('VAR1'), 'base1');
            assert.strictEqual(resultMap.get('VAR2'), 'session2'); // Run session takes precedence
            assert.strictEqual(resultMap.get('VAR3'), 'base3');
            assert.strictEqual(resultMap.get('VAR4'), 'session4');
        });

        test('handles empty base profile environment', () => {
            const runSessionEnv: EnvVar[] = [
                { name: 'VAR1', value: 'session1' }
            ];

            const result = mergeEnvironmentVariables(undefined, runSessionEnv);

            assert.strictEqual(result.length, 1);
            const resultMap = new Map(result);
            assert.strictEqual(resultMap.get('VAR1'), 'session1');
        });

        test('handles empty run session environment', () => {
            const baseProfileEnv = {
                'VAR1': 'base1',
                'VAR2': 'base2'
            };

            const result = mergeEnvironmentVariables(baseProfileEnv, []);

            assert.strictEqual(result.length, 2);

            const resultMap = new Map(result);
            assert.strictEqual(resultMap.get('VAR1'), 'base1');
            assert.strictEqual(resultMap.get('VAR2'), 'base2');
        });
    });

    suite('determineArguments', () => {
        test('uses run session args when provided', () => {
            const baseProfileArgs = '--base-arg value';
            const runSessionArgs = ['--session-arg', 'value'];

            const result = determineArguments(baseProfileArgs, runSessionArgs);

            assert.deepStrictEqual(result, '--session-arg value');
        });

        test('uses empty run session args when explicitly provided', () => {
            const baseProfileArgs = '--base-arg value';
            const runSessionArgs: string[] = [];

            const result = determineArguments(baseProfileArgs, runSessionArgs);

            assert.deepStrictEqual(result, '');
        });

        test('uses base profile args when run session args are null', () => {
            const baseProfileArgs = '--base-arg value --flag';
            const runSessionArgs = null;

            const result = determineArguments(baseProfileArgs, runSessionArgs);

            assert.deepStrictEqual(result, baseProfileArgs);
        });

        test('uses base profile args when run session args are undefined', () => {
            const baseProfileArgs = '--base-arg value --flag';
            const runSessionArgs = undefined;

            const result = determineArguments(baseProfileArgs, runSessionArgs);

            assert.deepStrictEqual(result, baseProfileArgs);
        });

        test('returns undefined when no args available', () => {
            const result = determineArguments(undefined, undefined);

            assert.deepStrictEqual(result, undefined);
        });
    });

    suite('determineWorkingDirectory', () => {
        const projectPath = path.join('C:', 'project', 'MyApp.csproj');

        test('uses absolute working directory from launch profile', () => {
            const baseProfile: LaunchProfile = {
                workingDirectory: path.join('C:', 'custom', 'working', 'dir')
            };

            const result = determineWorkingDirectory(projectPath, baseProfile);

            assert.strictEqual(result, path.join('C:', 'custom', 'working', 'dir'));
        });

        test('resolves relative working directory from launch profile', () => {
            const baseProfile: LaunchProfile = {
                workingDirectory: 'custom'
            };

            const result = determineWorkingDirectory(projectPath, baseProfile);

            assert.strictEqual(result, path.join('C:', 'project', 'custom'));
        });

        test('uses project directory when no working directory specified', () => {
            const baseProfile: LaunchProfile = {
            };

            const result = determineWorkingDirectory(projectPath, baseProfile);

            assert.strictEqual(result, path.join('C:', 'project'));
        });

        test('uses project directory when base profile is null', () => {
            const result = determineWorkingDirectory(projectPath, null);

            assert.strictEqual(result, path.join('C:', 'project'));
        });
    });

    suite('determineServerReadyAction', () => {
        test('returns undefined when launchBrowser is false', () => {
            const result = determineServerReadyAction(false, 'https://localhost:5001');
            assert.strictEqual(result, undefined);
        });

        test('returns undefined when applicationUrl is undefined', () => {
            const result = determineServerReadyAction(true, undefined);
            assert.strictEqual(result, undefined);
        });

        test('returns serverReadyAction when launchBrowser true and applicationUrl provided', () => {
            const applicationUrl = 'https://localhost:5001';
            const result = determineServerReadyAction(true, applicationUrl);

            assert.notStrictEqual(result, undefined);
            assert.strictEqual(result?.action, 'openExternally');
            assert.strictEqual(result?.uriFormat, applicationUrl);
            assert.strictEqual(result?.pattern, '\\bNow listening on:\\s+https?://\\S+');
        });
    });

    suite('readLaunchSettings', () => {
        let tempDir: string;
        let projectPath: string;
        let launchSettingsPath: string;

        setup(() => {
            tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'aspire-test-'));
            const projectDir = path.join(tempDir, 'TestProject');
            const propertiesDir = path.join(projectDir, 'Properties');

            fs.mkdirSync(projectDir, { recursive: true });
            fs.mkdirSync(propertiesDir, { recursive: true });

            projectPath = path.join(projectDir, 'TestProject.csproj');
            launchSettingsPath = path.join(propertiesDir, 'launchSettings.json');

            // Create a dummy project file
            fs.writeFileSync(projectPath, '<Project></Project>');
        });

        teardown(() => {
            if (fs.existsSync(tempDir)) {
                fs.rmSync(tempDir, { recursive: true, force: true });
            }
        });

        test('successfully reads valid launch settings file', async () => {
            const launchSettings = {
                profiles: {
                    'Development': {
                        environmentVariables: {
                            ASPNETCORE_ENVIRONMENT: 'Development'
                        }
                    }
                }
            };

            fs.writeFileSync(launchSettingsPath, JSON.stringify(launchSettings, null, 2));

            const result = await readLaunchSettings(projectPath);

            assert.notStrictEqual(result, null);
            assert.strictEqual(result!.profiles['Development'].environmentVariables!.ASPNETCORE_ENVIRONMENT, 'Development');
        });

        test('returns null when launch settings file does not exist', async () => {
            const result = await readLaunchSettings(projectPath);

            assert.strictEqual(result, null);
        });

        test('returns null when launch settings file has invalid JSON', async () => {
            fs.writeFileSync(launchSettingsPath, '{ invalid json content');

            const result = await readLaunchSettings(projectPath);

            assert.strictEqual(result, null);
        });

        test('handles empty launch settings file', async () => {
            const launchSettings = {
                profiles: {}
            };

            fs.writeFileSync(launchSettingsPath, JSON.stringify(launchSettings));

            const result = await readLaunchSettings(projectPath);

            assert.notStrictEqual(result, null);
            assert.deepStrictEqual(result!.profiles, {});
        });
    });
});
