// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { connectNativeAppHost } from './native-client.mjs';
import { basename, parse, resolve } from 'node:path';
import type { ApplicationExecution, ApplicationWorkspace, Composition, DeclaredResource, DeclaredValue } from './aspire.mjs';
import type { AspireClientRpc } from './transport.mjs';

/** Inert authored data, not a running resource or locally authoritative model. */
export class Resource {
    constructor(readonly name: string) {}
}
export class ContainerResource extends Resource {
    constructor(name: string, readonly image: string) { super(name); }
}
export class ExecutableResource extends Resource {
    constructor(name: string, readonly executablePath: string, readonly workingDirectory: string) { super(name); }
}
export class ParameterResource extends Resource {}

/** Native symbolic syntax: this recorder never reads a credential or resolves an endpoint. */
export class ReferenceExpression {
    constructor(readonly materialize: (context: NativeBuildContext) => Promise<DeclaredValue>) {}
    static literal(value: string): ReferenceExpression {
        return new ReferenceExpression(async context => await context.composition.createLiteralValue(value));
    }
    static create(...parts: (ReferenceExpression | string)[]): ReferenceExpression {
        if (parts.length === 0) throw new Error('A reference needs at least one part.');
        const expressions = parts.map(expression);
        return new ReferenceExpression(async context => {
            let result = await context.value(expressions[0]!);
            for (const part of expressions.slice(1)) result = await result.concatValue(await context.value(part));
            return result;
        });
    }
    encodeUri(): ReferenceExpression {
        return new ReferenceExpression(async context => (await context.value(this)).encodeUriValue());
    }
    toString(): string { return '[native reference expression]'; }
}
const expression = (value: ReferenceExpression | string): ReferenceExpression =>
    typeof value === 'string' ? ReferenceExpression.literal(value) : value;

export class EndpointReference {
    readonly host: ReferenceExpression;
    readonly port: ReferenceExpression;
    readonly hostAndPort: ReferenceExpression;
    constructor(resource: Resource, name: string) {
        const property = (key: string): ReferenceExpression => new ReferenceExpression(async context =>
            await context.composition.createEndpointValue(context.declaration(resource), name, key));
        this.host = property('host'); this.port = property('port'); this.hostAndPort = property('hostAndPort');
    }
}

/** Generated-handle adapter used only while asynchronous transport commands materialize. */
export class NativeBuildContext {
    private readonly declarations = new Map<Resource, DeclaredResource>();
    private readonly parameters = new Map<ParameterResource, DeclaredValue>();
    private readonly connections = new Map<Resource, ReferenceExpression>();
    private readonly values = new Map<ReferenceExpression, Promise<DeclaredValue>>();
    constructor(readonly composition: Composition, private readonly transport?: AspireClientRpc) {}
    get client(): AspireClientRpc {
        if (!this.transport) throw new Error('This provider composition has no AppHost transport.');
        return this.transport;
    }
    bind(resource: Resource, declaration: DeclaredResource): void { this.declarations.set(resource, declaration); }
    bindParameter(resource: ParameterResource, value: DeclaredValue): void { this.parameters.set(resource, value); }
    bindConnection(resource: Resource, value: ReferenceExpression): void { this.connections.set(resource, value); }
    declaration(resource: Resource): DeclaredResource {
        const declaration = this.declarations.get(resource);
        if (!declaration) throw new Error('Resource is not materialized in this transport plan.');
        return declaration;
    }
    parameter(resource: ParameterResource): Promise<DeclaredValue> {
        const value = this.parameters.get(resource);
        if (!value) throw new Error('Parameter is not materialized in this transport plan.');
        return Promise.resolve(value);
    }
    connection(resource: Resource): ReferenceExpression {
        const reference = this.connections.get(resource);
        if (!reference) throw new Error('The resource does not expose a native connection reference.');
        return reference;
    }
    value(reference: ReferenceExpression): Promise<DeclaredValue> {
        let value = this.values.get(reference);
        if (!value) { value = reference.materialize(this); this.values.set(reference, value); }
        return value;
    }
    static reference(value: DeclaredValue): ReferenceExpression { return new ReferenceExpression(async () => value); }
    static connectionReference(resource: Resource): ReferenceExpression {
        return new ReferenceExpression(context => context.value(context.connection(resource)));
    }
}

export class ExecutableCommand {
    readonly environment = new Map<string, ReferenceExpression>();
    constructor(readonly executablePath: string, readonly workingDirectory: string, readonly arguments_: readonly string[]) {}
    withEnvironment(name: string, value: ReferenceExpression): this { this.environment.set(name, value); return this; }
    launch() {
        return { executablePath: this.executablePath, workingDirectory: this.workingDirectory,
            arguments: [...this.arguments_], environment: [], endpoints: [] };
    }
    bindings() {
        return { environment: [...this.environment.keys()].map(name => ({ name, configurationKey: `health.env.${name}` })), arguments: [] };
    }
}

/** Fluent configuration records commands synchronously, so awaiting a chain performs no composition-time RPC. */
export class ResourceBuilder<T extends Resource> {
    constructor(readonly applicationBuilder: DistributedApplicationBuilder, readonly resource: T) {}
    asReference(this: ResourceBuilder<ParameterResource>): ReferenceExpression {
        return new ReferenceExpression(context => context.parameter(this.resource));
    }
    withEnvironment(name: string, value: ReferenceExpression | string | ResourceBuilder<ParameterResource>): this {
        if (value instanceof ResourceBuilder) this.sameBuilder(value);
        const reference = value instanceof ResourceBuilder ? value.asReference() : expression(value);
        this.applicationBuilder.record(async context => {
            await context.declaration(this.resource).withEnvironment(name, await context.value(reference));
        });
        return this;
    }
    withArgs(...args: (ReferenceExpression | string)[]): this {
        for (const argument of args.map(expression)) this.applicationBuilder.record(async context => {
            await context.declaration(this.resource).withArgument(await context.value(argument));
        });
        return this;
    }
    withEndpoint(name: string, targetPort: number, scheme = 'tcp'): this {
        if (!(this.resource instanceof ContainerResource)) throw new Error('Container endpoints require a container declaration.');
        this.applicationBuilder.record(async context => {
            await context.declaration(this.resource).withEndpoint({
                name, protocol: 'TCP', uriScheme: scheme, targetPort, hostPort: 0, environmentVariable: ''
            });
        });
        return this;
    }
    getEndpoint(name: string): EndpointReference { return new EndpointReference(this.resource, name); }
    withEntrypoint(entrypoint: string): this {
        if (!(this.resource instanceof ContainerResource)) throw new Error('Entrypoints require a container declaration.');
        this.applicationBuilder.record(async context => { await context.declaration(this.resource).withEntrypoint(entrypoint); });
        return this;
    }
    withVolume(name: string, target: string): this {
        this.applicationBuilder.record(async context => { await context.declaration(this.resource).withVolume(name, target, false); });
        return this;
    }
    withConnectionString(reference: ReferenceExpression): this {
        this.applicationBuilder.record(async context => { context.bindConnection(this.resource, reference); });
        return this;
    }
    withReference(producer: ResourceBuilder<Resource>): this {
        this.sameBuilder(producer);
        this.applicationBuilder.record(async context => {
            await context.declaration(this.resource).withReference(`ConnectionStrings__${producer.resource.name}`,
                await context.value(context.connection(producer.resource)));
        });
        return this;
    }
    waitFor(dependency: ResourceBuilder<Resource>): this {
        this.sameBuilder(dependency);
        this.applicationBuilder.record(async context => {
            await context.declaration(this.resource).waitFor(context.declaration(dependency.resource));
        });
        return this;
    }
    withHealthCheck(command: ExecutableCommand): this {
        const captured = new ExecutableCommand(command.executablePath, command.workingDirectory, [...command.arguments_]);
        for (const [name, value] of command.environment) captured.withEnvironment(name, value);
        this.applicationBuilder.record(async context => {
            for (const [name, value] of captured.environment) {
                await context.declaration(this.resource).setSymbolicConfiguration(`health.env.${name}`, await context.value(value));
            }
            await context.declaration(this.resource).withLifecycleExecutable('health', captured.launch(), captured.bindings());
        });
        return this;
    }
    private sameBuilder(resource: ResourceBuilder<Resource>): void {
        if (this.applicationBuilder !== resource.applicationBuilder) throw new Error('Reference belongs to another authoring plan.');
    }
}

interface NativeBuilderSettings {
    options: NativeBuilderOptions;
    endpoint?: string;
    volumePrefix: string;
    exitOnCompletion?: string;
    providers: Map<string, { executable: string; arguments: string[] }>;
    configuration: Record<string, string>;
}
/** Stable application source identity; defaults to the launch working directory, not compiled output. */
export interface NativeBuilderOptions {
    applicationRoot?: string;
    applicationName?: string;
    providerCleanupTimeoutMilliseconds?: number;
    providerShutdownTimeoutMilliseconds?: number;
}
function defaultVolumePrefix(applicationRoot: string, applicationName?: string): string {
    if (!applicationRoot.trim()) throw new Error('Application root cannot be empty.');
    const root = resolve(applicationRoot);
    applicationName ??= root === parse(root).root ? 'app' : basename(root);
    if (!applicationName.trim()) throw new Error('Application name cannot be empty.');
    const asciiLower = (value: string) => value.replace(/[A-Z]/g, c => c.toLowerCase());
    const canonicalRoot = process.platform === 'win32' ? asciiLower(root.replaceAll('\\', '/')) : root;
    if (process.platform === 'win32') applicationName = asciiLower(applicationName);
    // Match NativeBuilderSettings: UTF-8(root + NUL + name), FNV-1a/64,
    // lowercase 16-digit hex. NUL separates identity fields without ambiguity.
    // No runtime crypto/hash package is needed for this nonsecurity identity.
    // https://www.ietf.org/archive/id/draft-eastlake-fnv-35.html
    let hash = 14695981039346656037n;
    for (const value of Buffer.from(`${canonicalRoot}\0${applicationName}`, 'utf8')) {
        hash = BigInt.asUintN(64, (hash ^ BigInt(value)) * 1099511628211n);
    }
    const label = asciiLower(applicationName.slice(0, 40)).replace(/[^a-z0-9_.-]/g, '_');
    return `aspire-${label}-${hash.toString(16).padStart(16, '0')}`;
}
function settingsFromArgs(args: readonly string[], options: NativeBuilderOptions = {}): NativeBuilderSettings {
    const settings: NativeBuilderSettings = { options, endpoint: process.env.ASPIRE_NATIVE_RPC_ENDPOINT,
        volumePrefix: '', providers: new Map(), configuration: {} };
    let applicationRoot = options.applicationRoot ?? process.cwd(), applicationName = options.applicationName;
    let volumePrefix: string | undefined;
    // --native-provider=package=executable[|arg...] keeps process arguments out
    // of shell interpolation. Only the framework consumes activation coordinates.
    for (const arg of args) {
        const index = arg.indexOf('=');
        if (index < 0) throw new Error('AppHost configuration requires --name=value.');
        const name = arg.slice(0, index).replace(/^--/, ''), value = arg.slice(index + 1);
        switch (name) {
            case 'native-endpoint': settings.endpoint = value; break;
            case 'native-volume-prefix': volumePrefix = value; break;
            case 'native-application-root': applicationRoot = value; break;
            case 'native-application-name': applicationName = value; break;
            case 'native-exit-on-completion': settings.exitOnCompletion = value; break;
            case 'native-provider': {
                const split = value.indexOf('=');
                if (split < 0) throw new Error('Provider configuration requires package=executable.');
                const [executable, ...arguments_] = value.slice(split + 1).split('|');
                settings.providers.set(value.slice(0, split), { executable: executable!, arguments: arguments_ }); break;
            }
            case 'publish': throw new Error('Native builder publishing is not supported.');
            default: settings.configuration[name] = value; break;
        }
    }
    settings.volumePrefix = volumePrefix ?? defaultVolumePrefix(applicationRoot, applicationName);
    return settings;
}

/** Ordered declarations/configuration are a transport plan only. Native ATS owns validation, dependencies and scheduling. */
export class DistributedApplicationBuilder {
    private readonly declarations: ((context: NativeBuildContext) => Promise<void>)[] = [];
    private readonly configurationCommands: ((context: NativeBuildContext) => Promise<void>)[] = [];
    readonly resources: Resource[] = [];
    readonly providers = new Set<string>();
    private built = false;
    readonly configuration: Readonly<Record<string, string>>;
    constructor(readonly settings: NativeBuilderSettings = settingsFromArgs([])) { this.configuration = settings.configuration; }
    addContainer<T extends ContainerResource>(resource: T): ResourceBuilder<T> {
        this.add(resource);
        this.declarations.push(async context => context.bind(resource, await context.composition.addContainer(resource.name, resource.image)));
        return new ResourceBuilder(this, resource);
    }
    addExecutable(name: string, executablePath: string, workingDirectory: string): ResourceBuilder<ExecutableResource> {
        const resource = new ExecutableResource(name, executablePath, workingDirectory);
        this.add(resource);
        this.declarations.push(async context => context.bind(resource, await context.composition.addExecutable(name, executablePath, workingDirectory)));
        return new ResourceBuilder(this, resource);
    }
    addParameter(name: string, value?: string, secret = true): ResourceBuilder<ParameterResource> {
        const resource = new ParameterResource(name);
        this.add(resource);
        this.declarations.unshift(async context => context.bindParameter(resource, secret
            ? value === undefined ? await context.composition.addGeneratedSecretParameter(name) : await context.composition.addSecretParameter(name, value)
            : await context.composition.createLiteralValue(value ?? (() => { throw new Error('Nonsecret parameters require a value.'); })())));
        return new ResourceBuilder(this, resource);
    }
    importParameter(name: string, value: DeclaredValue): ResourceBuilder<ParameterResource> {
        const resource = new ParameterResource(name);
        this.add(resource);
        this.declarations.unshift(async context => { context.bindParameter(resource, value); });
        return new ResourceBuilder(this, resource);
    }
    addIntegrationResource<T extends Resource>(resource: T, packageName: string,
        declare: (context: NativeBuildContext) => Promise<{ declaration: DeclaredResource; connection: ReferenceExpression }>): ResourceBuilder<T> {
        this.add(resource); this.providers.add(packageName);
        this.declarations.push(async context => {
            const result = await declare(context);
            context.bind(resource, result.declaration); context.bindConnection(resource, result.connection);
        });
        return new ResourceBuilder(this, resource);
    }
    private add(resource: Resource): void {
        this.ensureMutable();
        if (!resource.name.trim()) throw new Error('Resource names cannot be empty.');
        if (this.resources.some(existing => existing.name.toLowerCase() === resource.name.toLowerCase())) throw new Error('Duplicate resource name.');
        this.resources.push(resource);
    }
    record(command: (context: NativeBuildContext) => Promise<void>): void {
        this.ensureMutable(); this.configurationCommands.push(command);
    }
    private ensureMutable(): void { if (this.built) throw new Error('The authoring plan is sealed.'); }
    getDefaultDataVolumeName(resource: Resource): string { return `${this.settings.volumePrefix}-${resource.name}-data`; }
    build(): NativeApplication { this.ensureMutable(); this.built = true; return new NativeApplication(this); }
    async materialize(composition: Composition, transport?: AspireClientRpc, signal?: AbortSignal): Promise<NativeBuildContext> {
        this.built = true;
        const context = new NativeBuildContext(composition, transport);
        for (const declaration of this.declarations) { signal?.throwIfAborted(); await declaration(context); }
        for (const command of this.configurationCommands) { signal?.throwIfAborted(); await command(context); }
        signal?.throwIfAborted();
        return context;
    }
}
export async function createBuilder(args: readonly string[] = process.argv.slice(2), options: NativeBuilderOptions = {}): Promise<DistributedApplicationBuilder> {
    return new DistributedApplicationBuilder(settingsFromArgs(args, options));
}

/** Framework-owned connection/provider/revision lifetime; no user-facing handle or invitation choreography. */
export class NativeApplication {
    private native?: Awaited<ReturnType<typeof connectNativeAppHost>>;
    private workspace?: ApplicationWorkspace;
    private execution?: ApplicationExecution;
    private context?: NativeBuildContext;
    private readonly providers: { close(): Promise<void> }[] = [];
    private pending: Promise<void> = Promise.resolve();
    private disposal?: Promise<void>;
    private readonly lifetime = new AbortController();
    private closed = false;
    constructor(private readonly initial: DistributedApplicationBuilder) {}
    private serialize<T>(operation: () => Promise<T>): Promise<T> {
        const result = this.pending.then(operation);
        this.pending = result.then(() => {}, () => {});
        return result;
    }
    start(signal?: AbortSignal): Promise<void> {
        return this.serialize(() => this.startCore(signal));
    }
    private async startCore(signal?: AbortSignal): Promise<void> {
        const operation = signal ? AbortSignal.any([signal, this.lifetime.signal]) : this.lifetime.signal;
        operation.throwIfAborted();
        if (this.closed || this.native) throw new Error('Application is already started or disposed.');
        const endpoint = this.initial.settings.endpoint;
        const authenticationToken = process.env.ASPIRE_NATIVE_RPC_TOKEN;
        if (!endpoint || !authenticationToken) throw new Error('Framework native endpoint/authentication configuration is required.');
        const [host, port] = endpoint.split(':');
        if (host !== '127.0.0.1' || !port) throw new Error('Native builder currently supports loopback TCP.');
        try {
            this.native = await connectNativeAppHost({
                endpoint: { host, port: Number(port) }, authenticationToken, authenticationTimeoutMilliseconds: 15000,
                providerCleanupTimeoutMilliseconds: this.initial.settings.options.providerCleanupTimeoutMilliseconds,
                providerShutdownTimeoutMilliseconds: this.initial.settings.options.providerShutdownTimeoutMilliseconds
            });
            for (const packageName of this.initial.providers) {
                const command = this.initial.settings.providers.get(packageName);
                if (!command) throw new Error(`Framework provider configuration is missing for '${packageName}'.`);
                this.providers.push(await this.native.activateProvider(packageName, command.executable, command.arguments));
            }
            this.workspace = await this.native.server.createApplicationWorkspace();
            await this.apply(this.initial, true, operation);
        } catch (error) {
            try { await this.disposeCore(); }
            catch (cleanup) { throw new AggregateError([error, cleanup], 'Native application startup and cleanup failed.'); }
            throw error;
        }
    }
    update(next: DistributedApplicationBuilder, signal?: AbortSignal): Promise<void> {
        return this.serialize(async () => {
            if (this.closed || !this.workspace) throw new Error('Application is not running.');
            if ([...next.providers].some(packageName => !this.initial.providers.has(packageName))) throw new Error('Adding providers after activation is not supported.');
            await this.apply(next, false, signal ? AbortSignal.any([signal, this.lifetime.signal]) : this.lifetime.signal);
        });
    }
    private async apply(builder: DistributedApplicationBuilder, initial: boolean, signal: AbortSignal): Promise<void> {
        signal.throwIfAborted();
        const revision = await this.workspace!.beginRevision();
        let context: NativeBuildContext;
        try { context = await builder.materialize(revision, this.native!.client, signal); }
        catch (error) { await this.workspace!.abortRevision(revision); throw error; }
        try { await this.workspace!.commitRevision(revision); }
        catch (error) {
            // Only the native workspace knows whether validation failed before
            // adoption. Abort never reopens an adopted/retired generation.
            try { await this.workspace!.abortRevision(revision); } catch { /* Preserve the original native failure. */ }
            throw error;
        }
        this.execution = initial ? await revision.startApplication() : await this.workspace!.getApplicationExecution();
        this.context = context;
        await this.wait(this.execution.waitApplicationReady(60000), signal);
    }
    private async wait<T>(result: PromiseLike<T>, signal: AbortSignal): Promise<T> {
        signal.throwIfAborted();
        let cancel: () => void = () => {};
        const cancelled = new Promise<never>((_resolve, reject) => {
            cancel = () => reject(signal.reason);
            signal.addEventListener('abort', cancel, { once: true });
        });
        try { return await Promise.race([result, cancelled]); }
        finally { signal.removeEventListener('abort', cancel); }
    }
    waitForCompletion(resource: ResourceBuilder<Resource>, signal?: AbortSignal): Promise<void> {
        return this.serialize(async () => {
            if (this.closed || !this.context || !this.execution) throw new Error('Application is not running.');
            const operation = signal ? AbortSignal.any([signal, this.lifetime.signal]) : this.lifetime.signal;
            if (!await this.wait(this.execution.waitResourceCompletion(this.context.declaration(resource.resource), 60000), operation)) {
                throw new Error(`Executable '${resource.resource.name}' failed.`);
            }
        });
    }
    async run(signal?: AbortSignal): Promise<void> {
        const controller = new AbortController();
        const cancel = (): void => controller.abort();
        signal?.addEventListener('abort', cancel, { once: true });
        this.lifetime.signal.addEventListener('abort', cancel, { once: true });
        process.once('SIGINT', cancel); process.once('SIGTERM', cancel);
        const operation = signal ? AbortSignal.any([signal, controller.signal, this.lifetime.signal]) :
            AbortSignal.any([controller.signal, this.lifetime.signal]);
        try {
            await this.start(operation);
            const selected = this.initial.settings.exitOnCompletion;
            if (selected) {
                const resource = this.initial.resources.find(resource => resource.name === selected);
                if (!resource || !await this.wait(this.execution!.waitResourceCompletion(this.context!.declaration(resource), 60000), operation)) {
                    throw new Error('Selected executable failed.');
                }
            } else if (!signal?.aborted && !controller.signal.aborted) {
                await new Promise<void>(resolve => controller.signal.addEventListener('abort', () => resolve(), { once: true }));
            }
        } catch (error) {
            if (!operation.aborted) throw error;
        } finally {
            signal?.removeEventListener('abort', cancel);
            this.lifetime.signal.removeEventListener('abort', cancel);
            process.removeListener('SIGINT', cancel); process.removeListener('SIGTERM', cancel);
            await this.dispose();
        }
    }
    dispose(): Promise<void> {
        if (!this.disposal) {
            this.lifetime.abort();
            this.disposal = this.serialize(() => this.disposeCore());
        }
        return this.disposal;
    }
    private async disposeCore(): Promise<void> {
        if (this.closed) return;
        this.closed = true;
        const failures: unknown[] = [];
        try { await this.workspace?.retireApplicationWorkspace(); } catch (error) { failures.push(error); }
        for (const provider of this.providers) {
            try { await provider.close(); } catch (error) { failures.push(error); }
        }
        try { await this.native?.client.disconnect(); } catch (error) { failures.push(error); }
        if (failures.length) throw new AggregateError(failures, 'Native application cleanup failed.');
    }
}
