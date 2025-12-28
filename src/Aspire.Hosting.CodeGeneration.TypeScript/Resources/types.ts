/**
 * Type for callback functions that can be registered and invoked from .NET.
 */
export type CallbackFunction = (args: unknown) => unknown | Promise<unknown>;

/**
 * Represents a marshalled .NET object received over JSON-RPC.
 * Contains the object ID for RPC calls and metadata about the object.
 */
export interface MarshalledObject {
    /** The object ID in the .NET object registry */
    $id: string;
    /** The .NET type name */
    $type: string;
    /** The full .NET type name including namespace */
    $fullType?: string;
    /** Available methods on the object */
    $methods?: Array<{
        name: string;
        parameters: Array<{ name: string; type: string }>;
    }>;
    /** Additional properties with their values (simple types) or type info (complex types) */
    [key: string]: unknown;
}
