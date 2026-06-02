# BarTabTracker Playwright E2E

This Playwright project is intentionally separate from the .NET unit tests.

## Harness choice

The suite starts the real app with Aspire because the backend depends on the Aspire-managed Redis resource and Vite receives the backend URL from Aspire service discovery. `global-setup.ts` runs:

```bash
aspire run --apphost src/BarTabTracker.AppHost/BarTabTracker.AppHost.csproj --non-interactive --nologo
```

It then waits for `server` to become healthy and `webfrontend` to be running, calls `aspire describe --format Json`, and uses the discovered `http://localhost:<dynamic-port>` webfrontend URL. `global-teardown.ts` stops the AppHost with `aspire stop`.

To point at an already-running app instead, set `E2E_BASE_URL`.

## Run

```bash
cd test/e2e
npm install
npm run install:browsers
npm test
```

The tests use `/api/auth/dev-login`, so no Microsoft Entra ID secrets are required.
