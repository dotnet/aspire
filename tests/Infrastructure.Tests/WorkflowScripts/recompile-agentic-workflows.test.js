'use strict';

const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { test } = require('node:test');
const {
    actionReferences, resolveVersion, synchronizeCompiler, compilerUpdatePending, detect, allowedChange,
    checkChanges, checkCompilerUpdatePending,
} = require('../../../.github/workflows/recompile-agentic-workflows.js');

const repoRoot = path.resolve(__dirname, '../../..');
const oldSha = 'a'.repeat(40);
const newSha = 'b'.repeat(40);
const compilerFiles = ['.github/workflows/copilot-setup-steps.yml', '.github/workflows/validate-agentic-workflows.yml'];
const pin = (sha, version) => `        uses: github/gh-aw-actions/setup-cli@${sha} # ${version}`;
const compiler = (sha = oldSha, version = 'v0.89.17') =>
    `${pin(sha, version)}\n        with:\n          version: ${version}\n`;
// Dependabot moves the pinned SHA and its release comment but leaves the version input behind.
const dependabotBump = (release = 'v0.90.0', version = 'v0.89.17') =>
    `${pin(newSha, release)}\n        with:\n          version: ${version}\n`;

function fixture() {
    const contents = new Map(compilerFiles.map(file => [file, compiler()]));
    contents.set('.github/workflows/example.lock.yml', pin(newSha, 'v0.90.0'));
    const outputs = {};
    const messages = [];
    const pulls = [];
    const files = [{
        filename: '.github/workflows/example.lock.yml',
        status: 'modified',
        patch: `@@ -1 +1 @@\n-${pin(oldSha, 'v0.89.17')}\n+${pin(newSha, 'v0.90.0')}`,
    }];
    const context = {
        eventName: 'pull_request',
        repo: { owner: 'microsoft', repo: 'aspire' },
        payload: {
            action: 'closed',
            repository: { default_branch: 'main' },
            pull_request: {
                number: 42, merged: true, user: { login: 'dependabot[bot]' },
                head: { repo: { full_name: 'microsoft/aspire' } }, base: { ref: 'main' },
                title: 'Bump unrelated grouped actions (not a compiler version)',
            },
        },
    };
    const github = {
        rest: {
            pulls: { list: 'pulls', listFiles: 'files' },
            repos: { getCommit: async () => ({ data: { sha: newSha } }), listTags: 'tags' },
        },
        paginate: async endpoint => endpoint === 'pulls' ? pulls :
            endpoint === 'files' ? files : [{ name: 'v0.90.0', commit: { sha: newSha } }],
    };
    const core = {
        setOutput: (name, value) => { outputs[name] = value; },
        info: value => messages.push(value),
        notice: value => messages.push(value),
    };
    const io = {
        readdirSync: () => [...contents.keys()].map(file => file.slice('.github/workflows/'.length)),
        readFileSync: file => {
            assert.ok(contents.has(file), `Missing fixture: ${file}`);
            return contents.get(file);
        },
        writeFileSync: (file, value) => contents.set(file, value),
    };
    return { github, context, core, io, outputs, contents, pulls, files, messages };
}

test('grouped Dependabot updates resolve the action release and synchronize both compiler pins', async () => {
    const f = fixture();
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {
        version: 'v0.90.0', branch: 'recompile-agentic-workflows-42',
        source: 'Dependabot PR #42', updated: 'true',
    });
    for (const file of compilerFiles) {
        assert.equal(f.contents.get(file), compiler(newSha, 'v0.90.0'));
    }
});

function manualFixture() {
    const f = fixture();
    f.context.eventName = 'workflow_dispatch';
    f.context.ref = 'refs/heads/main';
    delete f.context.payload.pull_request;
    f.github.rest.repos.getCommit = async ({ ref }) => ({
        data: { sha: ref === 'v0.89.17' ? oldSha : newSha },
    });
    return f;
}

test('manual dispatch selects the newest pinned action release without a PR payload', async () => {
    const f = manualFixture();
    f.contents.set('.github/workflows/README.md', pin(newSha, 'v99.0.0'));
    f.github.paginate = async () => { throw new Error('Manual dispatch must not query PRs or latest releases.'); };
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {
        version: 'v0.90.0', branch: 'recompile-agentic-workflows-manual',
        source: 'the default branch action pins (manual dispatch)', updated: 'true',
    });
    for (const file of compilerFiles) {
        assert.equal(f.contents.get(file), compiler(newSha, 'v0.90.0'));
    }
});

test('manual dispatch compares pinned versions numerically and reuses its branch on reruns', async () => {
    const f = manualFixture();
    f.contents.set('.github/workflows/example.lock.yml', pin(newSha, 'v0.100.0'));
    await detect(f, f.io);
    assert.equal(f.outputs.version, 'v0.100.0');
    await detect(f, f.io);
    assert.equal(f.outputs.branch, 'recompile-agentic-workflows-manual');
    assert.equal(f.outputs.updated, 'true');
});

test('manual dispatch rejects forks and non-default branches', async () => {
    for (const mutate of [
        f => { f.context.repo.owner = 'fork'; },
        f => { f.context.ref = 'refs/heads/feature'; },
    ]) {
        const f = manualFixture();
        mutate(f);
        await assert.rejects(detect(f, f.io), /requires the upstream default branch/);
        assert.deepEqual(f.outputs, {});
    }
});

test('manual dispatch rejects absent and mismatched pins before making edits', async () => {
    const f = manualFixture();
    f.io.readdirSync = () => [];
    await assert.rejects(detect(f, f.io), /No pinned gh-aw/);
    f.io.readdirSync = () => ['example.lock.yml'];
    f.contents.set('.github/workflows/example.lock.yml', pin(oldSha, 'v0.90.0'));
    await assert.rejects(detect(f, f.io), /does not match/);
    assert.equal(f.contents.get(compilerFiles[0]), compiler());
});

test('uncommented SHA pins resolve through upstream tags', async () => {
    const f = fixture();
    assert.equal(await resolveVersion({ ref: newSha }, f.github), 'v0.90.0');
    assert.deepEqual(actionReferences(`- uses: github/gh-aw-actions/setup@${newSha}`), [{ ref: newSha, version: undefined }]);
});

test('tag pins resolve without using latest or the PR title', async () => {
    const f = fixture();
    assert.equal(await resolveVersion({ ref: 'v0.90.0' }, f.github), 'v0.90.0');
});

test('release comments must agree with the pinned commit', async () => {
    const f = fixture();
    await assert.rejects(resolveVersion({ ref: oldSha, version: 'v0.90.0' }, f.github), /does not match/);
});

test('unmapped and ambiguous SHA pins fail explicitly', async () => {
    const f = fixture();
    await assert.rejects(resolveVersion({ ref: oldSha }, f.github), /Cannot uniquely map/);
    f.github.paginate = async () => ['v0.90.0', 'v0.90.1'].map(name => ({ name, commit: { sha: newSha } }));
    await assert.rejects(resolveVersion({ ref: newSha }, f.github), /Cannot uniquely map/);
});

test('unrelated action updates do nothing', async () => {
    const f = fixture();
    f.files[0].patch = `-uses: actions/checkout@${oldSha}\n+uses: actions/checkout@${newSha}`;
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {});
    assert.equal(f.contents.get(compilerFiles[0]), compiler());
});

test('unchanged gh-aw pins in surrounding changed lines do not trigger a recompile', async () => {
    const f = fixture();
    f.files[0].patch = `-${pin(newSha, 'v0.90.0')}\n+  ${pin(newSha, 'v0.90.0')}`;
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {});
});

test('superseded merges do not downgrade current workflows', async () => {
    const f = fixture();
    f.contents.set(f.files[0].filename, pin(oldSha, 'v0.91.0'));
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {});
    assert.match(f.messages[0], /superseded/);
});

test('an older merge cannot downgrade a newer compiler even while generated locks retain its SHA', async () => {
    const f = fixture();
    f.contents.set(compilerFiles[0], compiler(newSha, 'v0.100.0'));
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {});
    assert.match(f.messages[0], /newer compiler/);
    assert.equal(f.contents.get(compilerFiles[1]), compiler());
});

test('closed follow-up PRs are not recreated; open PRs reuse the same branch', async () => {
    const f = fixture();
    f.pulls.push({ state: 'closed' });
    await detect(f, f.io);
    assert.deepEqual(f.outputs, {});
    f.pulls[0].state = 'open';
    await detect(f, f.io);
    assert.equal(f.outputs.branch, 'recompile-agentic-workflows-42');
});

test('conflicting releases and missing diffs fail before editing any compiler pins', async () => {
    const f = fixture();
    f.files.push({ ...f.files[0], patch: `+${pin(newSha, 'v0.90.1')}` });
    await assert.rejects(detect(f, f.io), /conflicting/);
    assert.equal(f.contents.get(compilerFiles[0]), compiler());
    delete f.files[0].patch;
    await assert.rejects(detect(f, f.io), /Missing PR diff/);
});

test('untrusted, unmerged and non-default-branch events are rejected', async () => {
    for (const mutate of [
        f => { f.context.eventName = 'pull_request_target'; },
        f => { f.context.payload.action = 'opened'; },
        f => { delete f.context.payload.pull_request; },
        f => { f.context.payload.pull_request.merged = false; },
        f => { f.context.payload.pull_request.user.login = 'contributor'; },
        f => { f.context.payload.pull_request.head.repo.full_name = 'fork/aspire'; },
        f => { f.context.payload.pull_request.base.ref = 'release/13.4'; },
        f => { f.context.repo.owner = 'fork'; },
    ]) {
        const f = fixture();
        mutate(f);
        await assert.rejects(detect(f, f.io), /requires a merged upstream/);
        assert.deepEqual(f.outputs, {});
    }
});

test('compiler pin synchronization preserves line endings and fails on unexpected shapes', async () => {
    assert.equal(synchronizeCompiler(compiler().replaceAll('\n', '\r\n'), 'v0.90.0', newSha),
        compiler(newSha, 'v0.90.0').replaceAll('\n', '\r\n'));
    assert.throws(() => synchronizeCompiler('missing setup-cli', 'v0.90.0', newSha), /exactly one/);
    const f = fixture();
    f.contents.set(compilerFiles[1], 'missing setup-cli');
    await assert.rejects(detect(f, f.io), /exactly one/);
    assert.equal(f.contents.get(compilerFiles[0]), compiler());
});

test('only compiler pins, generated locks, the action cache and side-repo maintenance are publishable', () => {
    for (const file of [...compilerFiles, '.github/aw/actions-lock.json',
        '.github/workflows/example.lock.yml', '.github/workflows/nested/example.lock.yml',
        '.github/workflows/agentics-maintenance-microsoft-aspire.dev.yml']) {
        assert.equal(allowedChange(file), true, file);
    }
    for (const file of ['.github/agents/agent.md', '.github/workflows/source.md',
        '.github/workflows/recompile-agentic-workflows.yml', 'unexpected.txt']) {
        assert.equal(allowedChange(file), false, file);
    }
});

test('empty compilations do not publish and unexpected tracked or untracked files fail', () => {
    const output = [];
    const io = { appendFileSync: (_, value) => output.push(value) };
    checkChanges(() => '', io);
    assert.deepEqual(output, ['changed=false\n']);
    output.length = 0;
    checkChanges(() => '.github/workflows/new.lock.yml\0', io);
    assert.deepEqual(output, ['changed=true\n']);
    for (const command of ['diff', 'ls-files']) {
        assert.throws(() => checkChanges((_, args) => args[0] === command ? 'unexpected.txt\0' : '', io), /unexpected files/);
    }
});

test('a compiler pin ahead of its version input is a pending Dependabot update', () => {
    assert.equal(compilerUpdatePending(dependabotBump()), true);
    assert.equal(compilerUpdatePending(dependabotBump().replaceAll('\n', '\r\n')), true);
    assert.equal(compilerUpdatePending(`${compiler()}${dependabotBump()}`), true);
    assert.equal(compilerUpdatePending(dependabotBump('v0.100.0', 'v0.99.0')), true);
});

test('synchronized, behind and unrecognized compiler pins are not pending', () => {
    assert.equal(compilerUpdatePending(compiler()), false);
    assert.equal(compilerUpdatePending(compiler(newSha, 'v0.90.0')), false);
    assert.equal(compilerUpdatePending(dependabotBump('v0.9.0', 'v0.10.0')), false);
    assert.equal(compilerUpdatePending(dependabotBump('v0.89.0')), false);
    for (const unrecognized of [
        `        uses: github/gh-aw-actions/setup-cli@${newSha}\n        with:\n          version: v0.89.17\n`,
        dependabotBump().replace('setup-cli', 'setup'),
        dependabotBump().replace('github/gh-aw-actions/setup-cli', 'actions/checkout'),
        'no compiler pin',
    ]) {
        assert.equal(compilerUpdatePending(unrecognized), false, unrecognized);
    }
});

test('synchronizing the compiler clears a pending update', () => {
    assert.equal(compilerUpdatePending(synchronizeCompiler(dependabotBump(), 'v0.90.0', newSha)), false);
});

test('the committed compiler files keep the shape that synchronization edits and detection recognizes', () => {
    for (const file of compilerFiles) {
        const content = fs.readFileSync(path.join(repoRoot, file), 'utf8');
        assert.equal(compilerUpdatePending(synchronizeCompiler(content, 'v0.89.17', newSha)), false, file);
        // Moving the version input behind the pinned release reproduces a pin-only bump of the committed shape.
        const pinOnly = content.replace(/(setup-cli@[^\r\n]+\r?\n[ \t]*with:\r?\n[ \t]*version: )v\d+\.\d+\.\d+/, '$1v0.0.1');
        assert.notEqual(pinOnly, content, file);
        assert.equal(compilerUpdatePending(pinOnly), true, file);
    }
});

function checkPending(contents) {
    const output = [];
    const messages = [];
    const io = {
        readFileSync: file => contents.get(file),
        appendFileSync: (_, value) => output.push(value),
    };
    checkCompilerUpdatePending(io, message => messages.push(message));
    return { output, messages };
}

test('a pending pin in either compiler file reports a pending update', () => {
    assert.deepEqual(checkPending(new Map(compilerFiles.map(file => [file, compiler()]))),
        { output: ['pending=false\n'], messages: [] });
    for (const pendingFile of compilerFiles) {
        const { output, messages } = checkPending(new Map(
            compilerFiles.map(file => [file, file === pendingFile ? dependabotBump() : compiler()])));
        assert.deepEqual(output, ['pending=true\n']);
        assert.equal(messages.length, 1);
        assert.match(messages[0], /^::notice::/);
    }
});

test('the compiler-update-pending command writes the step output and rejects unknown commands', () => {
    const script = path.join(repoRoot, '.github', 'workflows', 'recompile-agentic-workflows.js');
    const workspace = fs.mkdtempSync(path.join(os.tmpdir(), 'compiler-update-pending-'));
    try {
        const output = path.join(workspace, 'github-output');
        const run = command => execFileSync(process.execPath, [script, command], {
            cwd: workspace, encoding: 'utf8', stdio: 'pipe', env: { ...process.env, GITHUB_OUTPUT: output },
        });
        fs.mkdirSync(path.join(workspace, '.github', 'workflows'), { recursive: true });
        for (const [contents, expected] of [[compiler(), 'pending=false\n'], [dependabotBump(), 'pending=true\n']]) {
            for (const file of compilerFiles) {
                fs.writeFileSync(path.join(workspace, file), contents);
            }
            fs.rmSync(output, { force: true });
            const log = run('compiler-update-pending');
            assert.equal(fs.readFileSync(output, 'utf8'), expected);
            assert.equal(log.startsWith('::notice::'), expected === 'pending=true\n');
        }
        assert.throws(() => run('unknown'),
            error => /Expected the changes or compiler-update-pending command/.test(error.stderr));
    } finally {
        fs.rmSync(workspace, { recursive: true, force: true });
    }
});
