export interface Instruction {
  name: string;
}

export interface CreateObjectInstruction extends Instruction {
  name: 'CREATE_OBJECT';
  typeName: string;
  assemblyName?: string;
  target: string;
  args?: Record<string, any>;
}

export interface PragmaInstruction extends Instruction {
  name: 'pragma';
  type: string;
  value: string;
}

export interface InvokeInstruction extends Instruction {
  name: 'INVOKE';
  source?: string;
  target: string;
  methodAssembly: string;
  methodType: string;
  methodName: string;
  methodArgumentTypes?: string[];
  metadataToken?: number;
  args?: Record<string, any>;
}

export type AnyInstruction =
  | CreateObjectInstruction
  | PragmaInstruction
  | InvokeInstruction;

export interface InstructionResult {
  success: boolean;
  error?: string;
  [key: string]: any;
}

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
