// Auto-generated type definitions

export interface InstructionResult {
  success: boolean;
  builderName?: string;
  resourceName?: string;
  result?: unknown;
  error?: string;
}

export interface CreateBuilderInstruction {
  name: 'CREATE_BUILDER';
  builderName: string;
  args?: string[];
}

export interface InvokeInstruction {
  name: 'INVOKE';
  builderName: string;
  resourceName?: string;
  methodName: string;
  args: unknown[];
}

export interface RunBuilderInstruction {
  name: 'RUN_BUILDER';
  builderName: string;
}

export type AnyInstruction = CreateBuilderInstruction | InvokeInstruction | RunBuilderInstruction;

export interface ResourceBuilderOptions {
  name: string;
}

export interface EndpointOptions {
  port?: number;
  targetPort?: number;
  scheme?: 'http' | 'https' | 'tcp';
  name?: string;
}

export interface EnvironmentOptions {
  name: string;
  value: string | (() => string);
}

export interface VolumeOptions {
  name?: string;
  target: string;
  isReadOnly?: boolean;
}

export interface BindMountOptions {
  source: string;
  target: string;
  isReadOnly?: boolean;
}
