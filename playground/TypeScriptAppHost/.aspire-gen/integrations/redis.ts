// Auto-generated from Aspire.Hosting.Redis v13.1.0

import type { DistributedApplicationBuilder, ResourceBuilder } from '../distributed-application.js';
import type { InstructionResult } from '../types.js';

export async function addRedis(builder: DistributedApplicationBuilder, name: string): Promise<ResourceBuilder<'unknown'>> {
  const result = await builder.invoke('AddRedis', [name]);
  if (!result.success) {
    throw new Error(result.error || 'Failed to invoke AddRedis');
  }
  return builder.getResourceBuilder<'unknown'>(result.resourceName!);
}


