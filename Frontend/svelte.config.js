import adapter from '@sveltejs/adapter-static';
import { relative, sep } from 'node:path';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		adapter: adapter({
			// These options tell SvelteKit exactly where to dump your compiled files
			pages: '../Backend/Schichtpilot/wwwroot',
			assets: '../Backend/Schichtpilot/wwwroot',
			fallback: 'index.html', // Generates a fallback file for your backend's SPA routing
			precompress: false,
			strict: false
		})
	},
	compilerOptions: {
		// defaults to rune mode for the project, except for `node_modules`. Can be removed in svelte 6.
		runes: ({ filename }) => {
			const relativePath = relative(import.meta.dirname, filename);
			const pathSegments = relativePath.toLowerCase().split(sep);
			const isExternalLibrary = pathSegments.includes('node_modules');

			return isExternalLibrary ? undefined : true;
		}
	}
};

export default config;
