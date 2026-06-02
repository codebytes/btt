# BarTabTracker

Mobile-first web app for sharing and splitting a bar tab with OAuth/dev-login auth, Redis storage, and an Aspire AppHost.

## Prerequisites

- .NET 10 SDK
- Node.js 20.19+ or 22.12+
- Aspire CLI 13.4+
- Docker running locally for the Redis container

## Build

```bash
dotnet build BarTabTracker.slnx
cd src/frontend
npm install
npm run build
```

## Run locally

From the repository root:

```bash
aspire run --apphost src/BarTabTracker.AppHost/BarTabTracker.AppHost.csproj
```

For non-interactive/background agent runs, use:

```bash
aspire start --non-interactive --apphost src/BarTabTracker.AppHost/BarTabTracker.AppHost.csproj
```

Open the `webfrontend` URL shown by Aspire. Configure Microsoft Entra ID with `Authentication:Microsoft:TenantId` (defaults to `common`), `Authentication:Microsoft:ClientId`, and `Authentication:Microsoft:ClientSecret`; register `/api/auth/callback` as the redirect path. In Development without Entra credentials, use `POST /api/auth/dev-login` for cookie-based local auth.
