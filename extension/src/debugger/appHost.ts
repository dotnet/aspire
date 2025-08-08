import * as vscode from 'vscode';
import { generateRunId } from './common';
import { extensionLogOutputChannel } from '../utils/logging';
import { ICliRpcClient } from '../server/rpcClient';
import { startDotNetProgram } from './languages/dotnet';
import { extensionContext } from '../extension';
import { EnvVar } from '../dcp/types';

