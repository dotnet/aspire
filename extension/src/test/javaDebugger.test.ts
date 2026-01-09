import * as assert from 'assert';
import { javaDebuggerExtension } from '../debugger/languages/java';
import { JavaLaunchConfiguration } from '../dcp/types';

suite('Java Debugger Extension Tests', () => {
    test('getProjectFile returns main_class_path when provided', () => {
        const mainClassPath = '/path/to/MainClass.java';
        const launchConfig: JavaLaunchConfiguration = {
            type: 'java',
            main_class_path: mainClassPath,
            project_path: '/path/to/project'
        };

        const result = javaDebuggerExtension.getProjectFile(launchConfig);

        assert.strictEqual(result, mainClassPath);
    });

    test('getProjectFile returns project_path when main_class_path is not provided', () => {
        const projectPath = '/path/to/project';
        const launchConfig: JavaLaunchConfiguration = {
            type: 'java',
            project_path: projectPath
        };

        const result = javaDebuggerExtension.getProjectFile(launchConfig);

        assert.strictEqual(result, projectPath);
    });

    test('getProjectFile throws error when neither main_class_path nor project_path is provided', () => {
        const launchConfig: JavaLaunchConfiguration = {
            type: 'java'
        };

        assert.throws(
            () => javaDebuggerExtension.getProjectFile(launchConfig)
        );
    });

    test('getProjectFile prefers main_class_path over project_path when both are provided', () => {
        const mainClassPath = '/path/to/MainClass.java';
        const projectPath = '/path/to/project';
        const launchConfig: JavaLaunchConfiguration = {
            type: 'java',
            main_class_path: mainClassPath,
            project_path: projectPath
        };

        const result = javaDebuggerExtension.getProjectFile(launchConfig);

        assert.strictEqual(result, mainClassPath);
    });

    test('getProjectFile throws error when launch configuration is not Java type', () => {
        const launchConfig = {
            type: 'python',
            program_path: '/path/to/program.py'
        };

        assert.throws(
            () => javaDebuggerExtension.getProjectFile(launchConfig as any)
        );
    });
});
