/**
 * Type for callback functions that can be registered and invoked from .NET.
 */
export type CallbackFunction = (args: unknown) => unknown | Promise<unknown>;

// ============================================================================
// ATS (Aspire Type System) Types
// ============================================================================

/**
 * Represents a handle to a .NET object in the ATS system.
 * Handles are typed references that can be passed between capabilities.
 */
export interface MarshalledHandle {
    /** The handle ID (format: "{typeId}:{instanceId}") */
    $handle: string;
    /** The ATS type ID (e.g., "aspire/Builder", "aspire.redis/RedisBuilder") */
    $type: string;
}

/**
 * Error details for ATS errors.
 */
export interface AtsErrorDetails {
    /** The parameter that caused the error */
    parameter?: string;
    /** The expected type or value */
    expected?: string;
    /** The actual type or value */
    actual?: string;
}

/**
 * Structured error from ATS capability invocation.
 */
export interface AtsError {
    /** Machine-readable error code */
    code: string;
    /** Human-readable error message */
    message: string;
    /** The capability that failed (if applicable) */
    capability?: string;
    /** Additional error details */
    details?: AtsErrorDetails;
}

/**
 * ATS error codes returned by the server.
 */
export const AtsErrorCodes = {
    /** Unknown capability ID */
    CapabilityNotFound: 'CAPABILITY_NOT_FOUND',
    /** Handle ID doesn't exist or was disposed */
    HandleNotFound: 'HANDLE_NOT_FOUND',
    /** Handle type doesn't satisfy capability's type constraint */
    TypeMismatch: 'TYPE_MISMATCH',
    /** Missing required argument or wrong type */
    InvalidArgument: 'INVALID_ARGUMENT',
    /** Argument value outside valid range */
    ArgumentOutOfRange: 'ARGUMENT_OUT_OF_RANGE',
    /** Error occurred during callback invocation */
    CallbackError: 'CALLBACK_ERROR',
    /** Unexpected error in capability execution */
    InternalError: 'INTERNAL_ERROR',
} as const;

/**
 * Type guard to check if a value is an ATS error response.
 */
export function isAtsError(value: unknown): value is { $error: AtsError } {
    return (
        value !== null &&
        typeof value === 'object' &&
        '$error' in value &&
        typeof (value as { $error: unknown }).$error === 'object'
    );
}

/**
 * Type guard to check if a value is a marshalled handle.
 */
export function isMarshalledHandle(value: unknown): value is MarshalledHandle {
    return (
        value !== null &&
        typeof value === 'object' &&
        '$handle' in value &&
        '$type' in value
    );
}
