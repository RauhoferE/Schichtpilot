import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vitest/config';
import { playwright } from '@vitest/browser-playwright';
import { sveltekit } from '@sveltejs/kit/vite';

export default defineConfig({
	plugins: [tailwindcss(), sveltekit()],

	// Required: bits-ui (used by shadcn-svelte) ships raw .svelte files.
	// Vite must compile them instead of treating them as pre-built SSR modules.
	server: {
        proxy: {
            '/api': {
                target: 'https://localhost:8081', // 👈 Your ASP.NET Core Local URL
                changeOrigin: true,
                secure: false // Allows self-signed development certificates
            }
        }
    },
	ssr: {
		noExternal: ['bits-ui', '@internationalized/date'],
	},
	test: {
		expect: { requireAssertions: true },
		projects: [
			{
				extends: './vite.config.ts',
				test: {
					name: 'client',
					browser: {
						enabled: true,
						provider: playwright(),
						instances: [{ browser: 'chromium', headless: true }]
					},
					include: ['src/**/*.svelte.{test,spec}.{js,ts}'],
					exclude: ['src/lib/server/**']
				}
			},

			{
				extends: './vite.config.ts',
				test: {
					name: 'server',
					environment: 'node',
					include: ['src/**/*.{test,spec}.{js,ts}'],
					exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
				}
			}
		]
	}
});
