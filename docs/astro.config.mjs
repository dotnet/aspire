// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import catppuccin from "@catppuccin/starlight";
import starlightLlmsTxt from 'starlight-llms-txt'
import starlightKbd from 'starlight-kbd'
import starlightImageZoom from 'starlight-image-zoom'
import starlightLinksValidator from 'starlight-links-validator'
import starlightScrollToTop from 'starlight-scroll-to-top'

// https://astro.build/config
export default defineConfig({
	site: 'https://aspire.dev',
	base: `aspire`,
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
					href: 'https://x.com/aspiredotdev'
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
				'./src/styles/site.css',
				'@fontsource-variable/outfit'
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
				//themes: ['dark-plus', 'light-plus'],
				themes: ['laserwave', 'slack-ochin'],
				styleOverrides: { borderRadius: '0.5rem', codeFontSize: '1rem' },
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
					label: 'Integrations',
					items: [
						{ label: 'Gallery', slug: 'integrations/gallery' }
					],
				},
				{
					label: 'Community',
					items: [
						{
							label: 'Videos',
							slug: 'community/videos'
						},
						{
							label: 'BlueSky',
							slug: 'community/posts',
							badge: {
								text: '#aspire',
								variant: 'note'
							}
						},
						{
							label: 'Contributors',
							slug: 'community/contributors',
						}
					]
				}
			],
			plugins: [
				catppuccin(),
				starlightLinksValidator({
					exclude: [ '#', '/' ],
					errorOnRelativeLinks: false
				}),
				starlightScrollToTop({
					// https://frostybee.github.io/starlight-scroll-to-top/svg-paths/
					svgPath: 'M4 16L12 8L20 16',
					showTooltip: true,
				}),
				starlightLlmsTxt({
					projectName: 'Aspire',
					description: 'Aspire is a polyglot local dev-time orchestration tool chain for building, running, debugging, and deploying distributed applications.',
				}),
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
