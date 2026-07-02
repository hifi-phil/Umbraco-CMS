# Commands
[← Umbraco Backoffice](../CLAUDE.md) | [← Monorepo Root](../../CLAUDE.md)

---


### Installation

```bash
# Install dependencies (must use npm, not yarn/pnpm due to workspaces)
npm install

# Note: Requires Node >=22.17.1 and npm >=10.9.2
```

### Build Commands

```bash
# TypeScript compilation only
npm run build

# Build for CMS (production build + copy to .NET project)
npm run build:for:cms

# Build for npm distribution (with type declarations)
npm run build:for:npm

# Build with Vite (alternative build method)
npm run build:vite

# Build workspaces
npm run build:workspaces

# Build Storybook documentation
npm run build-storybook
```

### Development Commands

```bash
# Start dev server (with live reload)
npm run dev

# Start dev server connected to real backend
npm run dev:server

# Start dev server with MSW mocks (default)
npm run dev:mock

# Preview production build
npm run preview
```

### Developing an Extension Against the Dev Server

The dev server can load an extension and register its manifests via the `VITE_EXAMPLE_PATH`
environment variable. The target folder must expose an `index.ts` that exports manifests
(an array, or an object per named export — see the folders under `examples/`).

**In-repo example** (folder under `examples/`) — interactive picker:

```bash
npm run example
```

**External extension** (a folder outside this repo) — pass an absolute path:

```bash
# Absolute path -> served via Vite's /@fs/ prefix; shared packages
# (@umbraco-cms/backoffice, lit, @umbraco-ui/uui) resolve from this repo.
VITE_EXAMPLE_PATH=/abs/path/to/my-extension npm run dev:mock
```

The external folder's `index.ts` is the entry point (not `src/index.ts`), matching the
in-repo `examples/` convention.

**Registering custom API mocks from an external extension**

When running with MSW on (`dev:mock`), an extension can mock its own API endpoints at
runtime via the global `window.MockServiceWorker.addMockHandlers(...)`. These layer on top
of the active mock set:

```ts
// In the external extension's index.ts
if (import.meta.env.VITE_UMBRACO_USE_MSW === 'on') {
	const { addMockHandlers, http, HttpResponse } = window.MockServiceWorker;
	addMockHandlers(http.get('/umbraco/my-api/items', () => HttpResponse.json({ items: [] })));
}
```

### Test Commands

```bash
# Run all unit tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests in development mode
npm run test:dev

# Run tests in watch mode (dev config)
npm run test:dev-watch

# Run E2E tests with Playwright
npm run test:e2e

# Run example tests
npm run test:examples

# Run example tests in watch mode
npm run test:examples:watch

# Run example tests in browser
npm run test:examples:browser
```

### Code Quality Commands

```bash
# Lint TypeScript files
npm run lint

# Lint and show only errors
npm run lint:errors

# Lint and auto-fix issues
npm run lint:fix

# Format code
npm run format

# Format and auto-fix
npm run format:fix

# Type check
npm run compile

# Run all checks (lint, compile, build-storybook, jsonschema)
npm run check
```

### Code Generation Commands

```bash
# Generate TypeScript config
npm run generate:tsconfig

# Generate OpenAPI client from backend API
npm run generate:server-api

# Generate icons
npm run generate:icons

# Generate package manifest
npm run generate:manifest

# Generate JSON schema for umbraco-package.json
npm run generate:jsonschema

# Generate JSON schema to dist
npm run generate:jsonschema:dist

# Generate UI API docs with TypeDoc
npm run generate:ui-api-docs

# Generate const check tests
npm run generate:check-const-test
```

### Analysis Commands

```bash
# Check for circular dependencies
npm run check:circular

# Check module dependencies
npm run check:module-dependencies

# Check path lengths
npm run check:paths

# Analyze web components
npm run wc-analyze

# Analyze web components for VS Code
npm run wc-analyze:vscode
```

### Storybook Commands

```bash
# Start Storybook dev server
npm run storybook

# Build Storybook
npm run storybook:build

# Build and preview Storybook
npm run storybook:preview
```

### Package Management

```bash
# Validate package exports
npm run package:validate

# Prepare for npm publish
npm run prepack
```

### Environment Setup

**Prerequisites**:
- Node.js >=22.17.1
- npm >=10.9.2
- Modern browser (Chrome, Firefox, Safari)

**Initial Setup**:

1. Clone repository
   ```bash
   git clone https://github.com/umbraco/Umbraco-CMS.git
   cd Umbraco-CMS/src/Umbraco.Web.UI.Client
   ```

2. Install dependencies
   ```bash
   npm install
   ```

3. Configure environment (optional)
   ```bash
   cp .env .env.local
   # Edit .env.local with your settings
   ```

4. Start development
   ```bash
   npm run dev
   ```

**Environment Variables** (see `.env` file):

- `VITE_UMBRACO_USE_MSW` - Enable/disable Mock Service Worker (`on`/`off`)
- `VITE_UMBRACO_API_URL` - Backend API URL (e.g., `https://localhost:44339`)
- `VITE_UMBRACO_INSTALL_STATUS` - Install status (`running`, `must-install`, `must-upgrade`)
- `VITE_UMBRACO_EXTENSION_MOCKS` - Enable extension mocks (`on`/`off`)
- `VITE_EXAMPLE_PATH` - Load an extension into the dev server. Relative (a folder under `examples/`) or absolute (an external extension folder served via `/@fs/`). See "Developing an Extension Against the Dev Server" above.

