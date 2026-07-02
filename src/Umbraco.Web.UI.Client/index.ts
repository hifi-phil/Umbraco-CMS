import { startMockServiceWorker } from './mocks/index.js';
import { UmbAppElement } from '@umbraco-cms/backoffice/app';
import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';

/**
 *
 */
async function bootstrap() {
	const appElement = new UmbAppElement();
	appElement.backofficePath = '/';

	if (import.meta.env.VITE_UMBRACO_USE_MSW === 'on') {
		appElement.bypassAuth = true;

		const mockSet = localStorage.getItem('umb:mockSet') || import.meta.env.VITE_MOCK_SET || 'default';
		await startMockServiceWorker({
			mockSet,
			useCustomServiceWorker: true,
		});

		// Register mock set switcher header app
		// TODO: implement for the static build too. We need to be able load the mock sets
		if (import.meta.env.MODE === 'development') {
			const { manifests } = await import('./mocks/backoffice-extensions/manifests.js');
			umbExtensionsRegistry.registerMany(manifests);
		}
	} else {
		appElement.serverUrl = import.meta.env.VITE_UMBRACO_API_URL;
	}

	document.body.append(appElement);

	// Example injector:
	// Supports both a relative path (a folder under ./examples, in-repo) and an absolute path
	// (an extension developed outside this repo). Absolute paths are served through Vite's
	// /@fs/ prefix. In both cases the target folder must expose an index.ts that exports manifests.
	if (import.meta.env.VITE_EXAMPLE_PATH) {
		const examplePath = import.meta.env.VITE_EXAMPLE_PATH;
		const importPath = examplePath.startsWith('/') ? '/@fs' + examplePath : './' + examplePath;
		import(/* @vite-ignore */ importPath + '/index.ts').then((js) => {
			if (js) {
				Object.keys(js).forEach((key) => {
					const value = js[key];

					if (Array.isArray(value)) {
						umbExtensionsRegistry.registerMany(value);
					} else if (typeof value === 'object') {
						umbExtensionsRegistry.register(value);
					}
				});
			}
		});
	}
	//#endregion
}

bootstrap();
