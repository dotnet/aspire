export interface Instruction {
  name: string;
}

export interface CreateBuilderInstruction extends Instruction {
  name: 'CREATE_BUILDER';
  builderName: string;
  args: string[];
}

export interface RunBuilderInstruction extends Instruction {
  name: 'RUN_BUILDER';
  builderName: string;
}

export interface PragmaInstruction extends Instruction {
  name: 'pragma';
  type: string;
  value: string;
}

export interface DeclareInstruction extends Instruction {
  name: 'DECLARE';
  type: string;
  varName: string;
}

export interface InvokeInstruction extends Instruction {
  name: 'INVOKE';
  source: string;
  target: string;
  methodAssembly: string;
  methodType: string;
  methodName: string;
  methodArgumentTypes: string[];
  metadataToken: number;
  args: Record<string, any>;
}

export type AnyInstruction = 
  | CreateBuilderInstruction 
  | RunBuilderInstruction 
  | PragmaInstruction 
  | DeclareInstruction 
  | InvokeInstruction;

export interface InstructionResult {
  success: boolean;
  error?: string;
  [key: string]: any;
}