import { describe, it, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { AspireHost } from '../src/aspire-host.ts';
import { spawn } from 'child_process';
import { mkdtempSync, rmSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';

// ---------------------------------------------------------------------------
// Scaffold a ts-starter project using aspire new (handles interactive prompts)
// ---------------------------------------------------------------------------

async function scaffoldTestApp(): Promise<string> {
  const dir = mkdtempSync(join(tmpdir(), 'aspire-sdk-test-'));
  const name = 'sdk-test';
  const output = join(dir, name);

  await new Promise<void>((resolve, reject) => {
    const child = spawn('aspire', [
      'new', 'aspire-ts-starter',
      '--name', name,
      '--output', output,
      '--non-interactive',
    ], {
      cwd: dir,
      stdio: ['ignore', 'pipe', 'pipe'],
    });

    let stderr = '';
    child.stderr.on('data', (data: Buffer) => { stderr += data.toString(); });

    child.on('close', (code) => {
      if (code === 0) resolve();
      else reject(new Error(`aspire new exited with code ${code}: ${stderr}`));
    });

    child.on('error', reject);
    setTimeout(() => reject(new Error('scaffold timed out')), 120_000);
  });

  return output;
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('AspireHost', { timeout: 180_000 }, () => {
  let host: AspireHost;
  let testDir: string;

  before(async () => {
    testDir = await scaffoldTestApp();
    const appHostPath = join(testDir, 'apphost.ts');
    host = await AspireHost.start({ appHost: appHostPath });
  });

  after(async () => {
    if (host) {
      await host.stop();
    }
    if (testDir) {
      rmSync(testDir, { recursive: true, force: true });
    }
  });

  describe('start()', () => {
    it('should return start info with pid and dashboard url', () => {
      assert.ok(host.info.appHostPid > 0, 'appHostPid should be positive');
      assert.ok(host.info.cliPid > 0, 'cliPid should be positive');
      assert.ok(host.info.dashboardUrl, 'dashboardUrl should be set');
      assert.ok(host.info.logFile, 'logFile should be set');
      assert.ok(host.info.appHostPath, 'appHostPath should be set');
    });
  });

  describe('getResources()', () => {
    it('should return an array of resources', async () => {
      const resources = await host.getResources();
      assert.ok(Array.isArray(resources), 'should return an array');
      assert.ok(resources.length > 0, 'should have at least one resource');
    });

    it('should include the app resource', async () => {
      const resources = await host.getResources();
      const app = resources.find(r => r.displayName === 'app' || r.name?.includes('app'));
      assert.ok(app, 'should have an app resource');
    });
  });

  describe('getResource()', () => {
    it('should return a single resource by name', async () => {
      const resource = await host.getResource('app');
      assert.ok(resource, 'should return the resource');
    });

    it('should throw for unknown resources', async () => {
      await assert.rejects(
        () => host.getResource('nonexistent'),
      );
    });
  });

  describe('waitForResource()', () => {
    it('should wait for a resource to be healthy', async () => {
      await host.waitForResource('app', { status: 'healthy', timeout: 60 });
      const resource = await host.getResource('app');
      assert.equal(resource.healthStatus, 'Healthy');
    });
  });

  describe('getEndpoint()', () => {
    it('should return the endpoint for a resource', async () => {
      const endpoint = await host.getEndpoint('app');
      assert.ok(endpoint.url, 'endpoint should have a url');
      assert.ok(endpoint.url.startsWith('http'), 'url should start with http');
    });

    it('should return a named endpoint', async () => {
      const endpoint = await host.getEndpoint('app', 'http');
      assert.ok(endpoint.url.startsWith('http'));
    });

    it('should throw for unknown endpoint name', async () => {
      await assert.rejects(
        () => host.getEndpoint('app', 'nonexistent'),
        /not found/i
      );
    });
  });

  describe('getLogs()', () => {
    it('should return logs for a resource', async () => {
      // Hit the API to generate some logs
      const endpoint = await host.getEndpoint('app');
      await fetch(`${endpoint.url}/api/test`);

      const logs = await host.getLogs('app');
      assert.ok(Array.isArray(logs), 'should return an array');
    });
  });

  describe('executeCommand()', () => {
    it('should restart a resource', async () => {
      await host.executeCommand('app', 'restart');
      await host.waitForResource('app', { status: 'healthy', timeout: 60 });
      const resource = await host.getResource('app');
      assert.equal(resource.state, 'Running');
    });
  });
});
