// aspire.ts - Core Aspire types: base classes, ReferenceExpression
import { Handle, AspireClient, MarshalledHandle } from './transport.js';

// Re-export transport types for convenience
export { Handle, AspireClient, CapabilityError, registerCallback, unregisterCallback } from './transport.js';
export type { MarshalledHandle, AtsError, AtsErrorDetails, CallbackFunction } from './transport.js';
export { AtsErrorCodes, isMarshalledHandle, isAtsError, wrapIfHandle } from './transport.js';

// ============================================================================
// Handle Type Aliases (Core)
// ============================================================================

export type BuilderHandle = Handle<'Aspire.Hosting/IDistributedApplicationBuilder'>;
export type ApplicationHandle = Handle<'Aspire.Hosting/DistributedApplication'>;

// ============================================================================
// Reference Expression
// ============================================================================

/**
 * Represents a reference expression that can be passed to capabilities.
 *
 * Reference expressions are serialized in the protocol as:
 * ```json
 * {
 *   "$expr": {
 *     "format": "redis://{0}:{1}",
 *     "valueProviders": [
 *       { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReference:1" },
 *       { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReference:2" }
 *     ]
 *   }
 * }
 * ```
 *
 * @example
 * ```typescript
 * const redis = await builder.addRedis("cache");
 * const endpoint = await redis.getEndpoint("tcp");
 *
 * // Create a reference expression
 * const expr = refExpr`redis://${endpoint}:6379`;
 *
 * // Use it in an environment variable
 * await api.withEnvironment("REDIS_URL", expr);
 * ```
 */
export class ReferenceExpression {
    private readonly _format: string;
    private readonly _valueProviders: unknown[];

    private constructor(format: string, valueProviders: unknown[]) {
        this._format = format;
        this._valueProviders = valueProviders;
    }

    /**
     * Creates a reference expression from a tagged template literal.
     *
     * @param strings - The template literal string parts
     * @param values - The interpolated values (handles to value providers)
     * @returns A ReferenceExpression instance
     */
    static create(strings: TemplateStringsArray, ...values: unknown[]): ReferenceExpression {
        // Build the format string with {0}, {1}, etc. placeholders
        let format = '';
        for (let i = 0; i < strings.length; i++) {
            format += strings[i];
            if (i < values.length) {
                format += `{${i}}`;
            }
        }

        // Extract handles from values
        const valueProviders = values.map(extractHandleForExpr);

        return new ReferenceExpression(format, valueProviders);
    }

    /**
     * Serializes the reference expression for JSON-RPC transport.
     * Uses the $expr format recognized by the server.
     */
    toJSON(): { $expr: { format: string; valueProviders?: unknown[] } } {
        return {
            $expr: {
                format: this._format,
                valueProviders: this._valueProviders.length > 0 ? this._valueProviders : undefined
            }
        };
    }

    /**
     * String representation for debugging.
     */
    toString(): string {
        return `ReferenceExpression(${this._format})`;
    }
}

/**
 * Extracts a value for use in reference expressions.
 * Supports handles (objects) and string literals.
 * @internal
 */
function extractHandleForExpr(value: unknown): unknown {
    if (value === null || value === undefined) {
        throw new Error('Cannot use null or undefined in reference expression');
    }

    // String literals - include directly in the expression
    if (typeof value === 'string') {
        return value;
    }

    // Number literals - convert to string
    if (typeof value === 'number') {
        return String(value);
    }

    // Handle objects - get their JSON representation
    if (value instanceof Handle) {
        return value.toJSON();
    }

    // Objects with $handle property (already in handle format)
    if (typeof value === 'object' && value !== null && '$handle' in value) {
        return value;
    }

    // Objects with toJSON that returns a handle
    if (typeof value === 'object' && value !== null && 'toJSON' in value && typeof value.toJSON === 'function') {
        const json = value.toJSON();
        if (json && typeof json === 'object' && '$handle' in json) {
            return json;
        }
    }

    throw new Error(
        `Cannot use value of type ${typeof value} in reference expression. ` +
        `Expected a Handle, string, or number.`
    );
}

/**
 * Tagged template function for creating reference expressions.
 *
 * Use this to create dynamic expressions that reference endpoints, parameters, and other
 * value providers. The expression is evaluated at runtime by Aspire.
 *
 * @example
 * ```typescript
 * const redis = await builder.addRedis("cache");
 * const endpoint = await redis.getEndpoint("tcp");
 *
 * // Create a reference expression using the tagged template
 * const expr = refExpr`redis://${endpoint}:6379`;
 *
 * // Use it in an environment variable
 * await api.withEnvironment("REDIS_URL", expr);
 * ```
 */
export function refExpr(strings: TemplateStringsArray, ...values: unknown[]): ReferenceExpression {
    return ReferenceExpression.create(strings, ...values);
}

// ============================================================================
// DistributedApplicationBase
// ============================================================================

/**
 * Base class for DistributedApplication.
 * Provides the run() method and handle management.
 */
export class DistributedApplicationBase {
    constructor(protected _handle: ApplicationHandle, protected _client: AspireClient) {}

    get handle(): ApplicationHandle { return this._handle; }

    async run(): Promise<void> {
        await this._client.invokeCapability('Aspire.Hosting/run', { app: this._handle });
    }
}

// ============================================================================
// DistributedApplicationBuilderBase
// ============================================================================

/**
 * Base class for DistributedApplicationBuilder.
 * Provides handle management. The build() method is generated.
 */
export class DistributedApplicationBuilderBase {
    constructor(protected _handle: BuilderHandle, protected _client: AspireClient) {}

    get handle(): BuilderHandle { return this._handle; }
}

// ============================================================================
// ResourceBuilderBase
// ============================================================================

/**
 * Base class for resource builders (e.g., RedisBuilder, ContainerBuilder).
 * Provides handle management and JSON serialization.
 */
export class ResourceBuilderBase<THandle extends Handle = Handle> {
    constructor(protected _handle: THandle, protected _client: AspireClient) {}

    get handle(): THandle { return this._handle; }

    toJSON(): MarshalledHandle { return this._handle.toJSON(); }
}
