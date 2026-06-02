# RAI Audit Trail

> Append-only evidence log. Entries are redacted — never contains raw secrets or harmful content.

<!-- Rai appends findings below -->


## 2026-06-02T17:02:31+02:00 — rai-review — 🟡 Yellow

Requested by: Chris Ayers  
Scope: BarTabTracker security/privacy RAI guardrail review.  
Evidence is redacted; no raw credentials or personal data included.

### Non-blocking findings

1. 🟡 Medium — Public/unrate-limited reverse geocode proxy
   - WHAT: `/api/geocode/reverse` accepts anonymous requests and has no app-level rate limiting before calling Nominatim.
   - WHY: It can be abused as an open third-party geocoding proxy and can forward arbitrary valid coordinate lookups to an external service.
   - HOW: Require authentication unless truly public, add ASP.NET rate limiting by user/IP, keep cache, and enforce a conservative per-client cooldown.
   - References: `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:209-221`, `src/BarTabTracker.Server/Location/LocationServices.cs:34-35`.

2. 🟡 Medium — Session cookie hardening is implicit rather than enforced
   - WHAT: Auth cookie options set name/paths/expiration, but do not explicitly enforce `HttpOnly`, production `SecurePolicy=Always`, or an intentional `SameSite` value.
   - WHY: Framework defaults are reasonable, but explicit production settings reduce risk of accidental insecure deployment behind proxies or HTTP endpoints.
   - HOW: Set cookie flags explicitly, configure forwarded headers for proxy deployments, and consider anti-CSRF protection for cookie-authenticated state-changing API calls.
   - References: `src/BarTabTracker.Server/Auth/AuthServiceCollectionExtensions.cs:20-25`.

3. 🟡 Low — Precise GPS sharing/minimization needs stronger user notice
   - WHAT: Location capture is one-shot and button-initiated, but high-accuracy coordinates are stored with the tab and returned to tab members; UI copy does not clearly say the precise pin will be saved/shared.
   - WHY: Precise location is sensitive personal data; users should understand persistence and audience before sharing.
   - HOW: Add explicit copy/confirmation before saving, consider rounding coordinates or storing selected venue coordinates instead of raw current GPS, and define retention/deletion behavior.
   - References: `src/frontend/src/pages/NewTabPage.tsx:29-55`, `src/frontend/src/pages/NewTabPage.tsx:90-97`, `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:60-64`, `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:366-378`.

### Passed checks

- 🟢 Credentials/secrets: No hard-coded OAuth client secret/key/token found in appsettings or reviewed source; Google OAuth reads client id/secret from configuration. References: `src/BarTabTracker.Server/appsettings.json:1-9`, `src/BarTabTracker.Server/appsettings.Development.json:1-8`, `src/BarTabTracker.Server/Auth/AuthServiceCollectionExtensions.cs:13-16`.
- 🟢 Dev-login: Backend dev-login route is mapped only when `app.Environment.IsDevelopment()`. Reference: `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:233-259`.
- 🟢 Invite links: Tokens use 128 bits of CSPRNG entropy, joining requires auth, and tab read/write routes require membership. References: `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:170-192`, `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:278-292`, `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:399-404`.
- 🟢 PII logging: Reviewed logging does not include display names, avatar URLs, OAuth subjects, invite tokens, or raw coordinates. References: `src/BarTabTracker.Server/Program.cs:50-52`, `src/BarTabTracker.Server/Storage/Redis/RedisUserRepository.cs:33-35`.
- 🟢 Injection/SSRF: Nominatim target is fixed BaseAddress; lat/lng are numeric and range-validated. References: `src/BarTabTracker.Server/Program.cs:26-30`, `src/BarTabTracker.Server/Api/ApiEndpointExtensions.cs:209-221`, `src/BarTabTracker.Server/Location/LocationServices.cs:34-35`.

Recommended fix agents: Basher for backend auth/rate-limit/cookie items; Linus for frontend GPS consent/disclosure.
