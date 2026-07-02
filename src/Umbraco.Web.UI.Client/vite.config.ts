import { defineConfig, PluginOption } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';
import viteTSConfigPaths from 'vite-tsconfig-paths';
import path from 'path';

// External example support: when VITE_EXAMPLE_PATH is an absolute path it points at an
// extension developed outside this repo (see index.ts and docs/commands.md). Relative paths
// (folders under ./examples) are unaffected and the config below is a no-op for them.
const EXAMPLE_PATH = process.env.VITE_EXAMPLE_PATH;
const externalExamplePath = EXAMPLE_PATH?.startsWith('/') ? path.resolve(EXAMPLE_PATH) : null;

if (externalExamplePath) {
	// eslint-disable-next-line no-console
	console.log(`\n📦 Loading external example from: ${externalExamplePath}\n`);
}

// Packages that must resolve to a single instance shared with the main project. `resolve.dedupe`
// handles the common case, but imports from files outside the project root bypass
// vite-tsconfig-paths (which maps @umbraco-cms/backoffice/* -> ./src/* via tsconfig paths), so the
// external-example-resolver plugin below re-resolves them through the main project. Without this an
// external extension's @umbraco-cms/backoffice/* and lit imports resolve into its own node_modules,
// producing duplicate Lit instances and broken rendering.
const SHARED_PACKAGE_PREFIXES = ['@umbraco-cms/backoffice', 'lit', '@umbraco-ui/uui'];
const isSharedPackage = (source: string) => SHARED_PACKAGE_PREFIXES.some((prefix) => source.startsWith(prefix));

export const plugins: PluginOption[] = [
	viteStaticCopy({
		targets: [
			{
				src: 'public-assets/App_Plugins/*.js',
				dest: 'App_Plugins',
			},
			{
				src: 'public-assets/App_Plugins/custom-bundle-package/*.js',
				dest: 'App_Plugins/custom-bundle-package',
			},
			{
				src: 'src/css/*.css',
				dest: 'umbraco/backoffice/css',
			},
			{
				// UUI theme CSS (dark.css, high-contrast.css, etc.).
				// This config is used by the dev server and PR preview builds only.
				// The production build (build:for:cms) copies these files via
				// src/external/uui/vite.config.ts during the workspace build instead.
				// Theme manifests referencing these files: src/packages/core/themes/manifests.ts.
				src: 'node_modules/@umbraco-ui/uui/dist/themes/*',
				dest: 'umbraco/backoffice/css',
			},
			{
				src: 'node_modules/@umbraco-ui/uui/dist/assets/fonts/*',
				dest: 'umbraco/backoffice/assets/fonts',
			},
			{
				src: 'src/assets/*',
				dest: 'umbraco/backoffice/assets',
			},
			{
				src: 'mocks/msw-handlers/backoffice/assets/*',
				dest: 'umbraco/backoffice/assets',
			},
			{
				src: 'node_modules/msw/lib/iife/**/*',
				dest: 'umbraco/backoffice/msw',
			},
		],
	}),
	viteTSConfigPaths(),
];

// https://vitejs.dev/config/
export default defineConfig({
	build: {
		sourcemap: true,
		rollupOptions: {
			input: {
				main: new URL('index.html', import.meta.url).pathname, // Vite should only load the main index.html file
			},
		},
	},
	// Dedupe shared front-end libraries so an external example never loads its own copy.
	resolve: externalExamplePath ? { dedupe: [...SHARED_PACKAGE_PREFIXES] } : undefined,
	// Allow Vite to serve files from the external example folder (outside the project root).
	server: externalExamplePath ? { fs: { allow: ['.', externalExamplePath] } } : undefined,
	// External example folders may drag in dev/test-only deps that break pre-bundling.
	optimizeDeps: externalExamplePath
		? { exclude: ['puppeteer-core', '@web/dev-server-core', '@web/dev-server-esbuild', '@web/dev-server-rollup'] }
		: undefined,
	plugins: [
		...plugins,
		// Re-resolve shared-package imports originating from the external example through the main
		// project, so they map onto this repo's source instead of the extension's node_modules.
		...(externalExamplePath
			? [
					{
						name: 'external-example-resolver',
						enforce: 'pre' as const,
						resolveId(source: string, importer: string | undefined) {
							if (!importer?.startsWith(externalExamplePath)) return null;
							if (!isSharedPackage(source)) return null;
							return this.resolve(source, path.resolve('./index.ts'), { skipSelf: true });
						},
					},
				]
			: []),
	],
});
