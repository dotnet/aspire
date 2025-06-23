import { defineCollection, z } from 'astro:content';
import { docsLoader } from '@astrojs/starlight/loaders';
import { docsSchema } from '@astrojs/starlight/schema';

export const collections = {
	docs: defineCollection({
		loader: docsLoader(),
		schema: docsSchema(
			{
				extend: ({ image }) => z.object({
					renderBlocking: z.string().optional(),
					category: z.enum([
						'conceptual',
						'quickstart',
						'tutorial',
						'blog',
						'reference',
						'sample'
					]).optional(),
				})
			}
		)
	}),
};
