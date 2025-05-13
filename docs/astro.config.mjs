// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import catppuccin from "@catppuccin/starlight";
import starlightLlmsTxt from 'starlight-llms-txt'
import starlightKbd from 'starlight-kbd'
import starlightImageZoom from 'starlight-image-zoom'
import { viewTransitions } from "astro-vtbot/starlight-view-transitions";

// https://astro.build/config
export default defineConfig({
	site: 'https://IEvangelist.github.io',
	//base: 'aspire',
	integrations: [
		starlight({
			title: 'Aspire',
			logo: {
				src: './src/assets/dotnet-aspire-logo-32.svg'
			},
			editLink: {
				baseUrl: 'https://github.com/dotnet/aspire/edit/main/',
			},
			favicon: 'favicon.svg',
			head: [
				{ tag: 'link', attrs: { rel: 'icon', type: 'image/png', href: 'favicon-96x96.png', sizes: '96x96' } },
				{ tag: 'link', attrs: { rel: 'icon', type: 'image/svg+xml', href: 'favicon.svg' } },
				{ tag: 'link', attrs: { rel: 'shortcut icon', href: 'favicon.ico' } },
				{ tag: 'link', attrs: { rel: 'apple-touch-icon', sizes: '180x180', href: 'apple-touch-icon.png' } },
				{ tag: 'meta', attrs: { name: 'apple-mobile-web-app-title', content: 'Aspire' } },
			],
			social: [
				{
					icon: 'github',
					label: 'GitHub',
					href: 'https://github.com/dotnet/aspire'
				},
				{
					icon: 'discord',
					label: 'Discord',
					href: 'https://discord.com/invite/h87kDAHQgJ'
				},
				{
					icon: 'x.com',
					label: 'X',
					href: 'https://x.com/dotnet'
				},
				{
					icon: 'blueSky',
					label: 'BlueSky',
					href: 'https://bsky.app/profile/dot.net'
				},
				{
					icon: 'youtube',
					label: 'YouTube',
					href: 'https://www.youtube.com/dotnet'
				},
				{
					icon: 'threads',
					label: 'Threads',
					href: 'https://www.threads.com/@dotnet.developers'
				},
				{
					icon: 'tiktok',
					label: 'TikTok',
					href: 'https://www.tiktok.com/@dotnetdevelopers'
				},
				{
					icon: 'linkedin',
					label: 'LinkedIn',
					href: 'https://www.linkedin.com/groups/40949/'
				}
			],
			customCss: [
				'./src/styles/home.css'
			],
			components: {
				ContentPanel: './src/components/ContentPanel.astro',
				SocialIcons: './src/components/SocialIcons.astro',
				Search: './src/components/Search.astro',
				Footer: './src/components/Footer.astro',
				MarkdownContent: './src/components/MarkdownContent.astro',
			},
			expressiveCode: {
				/* TODO: decide which themes we want
				   https://expressive-code.com/guides/themes/#using-bundled-themes
				*/
				themes: ['github-dark-default', 'github-light']
			},
			sidebar: [
				{
					label: 'Home',
					link: '/'
				},
				{
					label: 'Welcome',
					items: [
						{ label: 'Overview', slug: 'get-started/overview' },
						{ label: 'Prerequisites', slug: 'get-started/prerequisites' },
						{ label: 'Installation', slug: 'get-started/installation' },
						{ label: 'First app', slug: 'get-started/first-app', badge: 'Quickstart' }
					],
				},
				{
					label: 'Build',
					badge: {
						text: 'Concepts',
						variant: 'note'
					},
					autogenerate: { directory: 'build' },
				},
				{
					label: 'Dashboard',
					items: [
						{ label: 'Overview', slug: '' },
						{
							label: 'Features', slug: '', badge: {
								text: 'UX',
								variant: 'caution'
							},
						},
						{
							label: 'Standalone', slug: '', badge: {
								text: 'Container',
								variant: 'danger'
							},
						},
						{ label: 'Configuration', slug: '' },
						{ label: 'Browser telemetry', slug: '' },
					]
				},
				{
					label: 'Integrations',
					collapsed: true,
					autogenerate: { directory: 'integrations' },
				},
				{
					label: 'Custom Integrations',
					collapsed: true,
					items: [
						{ label: 'Create hosting integration', slug: '' },
						{ label: 'Create client integration', slug: '' },
						{ label: 'Secure integrations', slug: '' },
					]
				},
				{
					label: 'Deploy',
					collapsed: true,
					autogenerate: { directory: 'deploy' },
				},
				{
					label: 'Reference',
					badge: {
						text: 'API',
						variant: 'tip'
					},
					collapsed: true,
					autogenerate: { directory: 'reference' },
				}
			],
			plugins: [
				viewTransitions(),
				catppuccin(),
				starlightLlmsTxt(),
				starlightImageZoom({
					showCaptions: true
				}),
				starlightKbd({
					types: [
						{ id: 'mac', label: 'macOS' },
						{ id: 'windows', label: 'Windows', default: true },
						{ id: 'linux', label: 'Linux' },
					],
				}),
			],
		}),
	],
});
