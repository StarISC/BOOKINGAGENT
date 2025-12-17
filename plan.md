# BookingAgent Delivery Plan (Detailed)

## Assumptions & Constraints
- Platform: Windows Server 2016, SQL Server 2019, .NET 9, Blazor Server.
- Canonical sources: `document/RCL Cruise FIT Spec 5.2.pdf` (pricing/booking flows, esp. pages ~162-205) and lookup tables in `document/RCL Cruises Ltd - API TABLES/` (ships, decks, ports, regions/subregions, cabin categories/config, bed types, titles, gateways).
- Security: secrets via env vars/user-secrets; never commit real credentials. Default DB name `BookingAgentDB`, admin account `admin`/`Admin@2025` stored securely.
- SQL Server host (current target): `Server=SHENLUNG\\SQLSERVER2022;Database=BookingAgentDB;User Id=sa;Password=<provide-at-runtime>;TrustServerCertificate=True;` (set via environment `ConnectionStrings__BookingAgent`; do not commit the password).
- Roles: customers, agents, supervisors/admins. Role-based UI/actions required.
- Future: payment gateway integration; design extensible boundaries now.
- API auth/endpoint (staging): base URL `https://stage.services.rccl.com/Reservation_FITWeb/sca/<API>`; CompanyShortName `NOVASTAR`; credentials `username=CONNCQN`, `password=qFDmKFM7eTaLk3s` must be supplied via environment/user-secrets (do not commit plaintext).

## Core Features (scope)
- Sailing discovery: search by region/subregion, port, ship, date range, duration, cabin category.
- Pricing: call `OTA_CruisePriceBookingRQ/RS` to return booking-level totals, payment schedule, guest-level breakdown, promotions, cabin details.
- Booking: create/modify booking, store pricing audit snapshots, retrieve booking and reprice (amend) via API.
- Reference data: load and manage lookup tables (ships, decks, ports, regions, cabin categories/config, bed type, titles, gateways).
- Security & audit: role-based access, action logging (no secrets), admin-only maintenance.
- Payments (future): integrate gateway for online payment; store payment attempts/refs; idempotent webhook handling.

## Roles & Permissions (initial)
- Customer: search sailings, view allowed pricing output, initiate booking, view their booking/payment schedule (no admin/lookup actions).
- Agent: all customer actions plus full pricing breakdown, apply promotions, create/modify bookings, view booking audits.
- Supervisor: agent permissions plus override/approval flows (if added), access to operational reports.
- Admin: manage lookups/refresh, configuration toggles, user/role administration, view system diagnostics/health.

## Progress
- Done: Solution scaffolding (Blazor Server + Domain), initial domain pricing models, UI shell, placeholder pricing service, database schema for lookups, bulk import script, lookup domain interfaces, SQL-backed lookup service with caching and DI wiring, config placeholders for SHENLUNG\\SQLSERVER2022, search UI wired to lookup data (regions/ports/ships/cabin categories) in `/search`, SOAP pricing client skeleton (config binding, HttpClient, sample response stub).
- In progress: Build real SOAP request/response handling for OTA_CruisePriceBookingRQ/RS against staging; search page now calls pricing service with selected filters (stub response).
- Next: Complete UI search ? pricing call with live SOAP; booking flow and role policies; payment abstraction prep.

## Team Approach (2 developers)
- Dev A (backend focus): schema, lookup ingestion, SOAP client, repositories, payment abstraction; security/auth.
- Dev B (frontend focus): Blazor UI, UX flows, role-aware views, diagnostics; assists with tests and mocks.
- Pair on critical mappings (SOAP ↔ domain) and deployment scripts.

## Phase 1 — Discovery & Architecture
- Trace end-to-end flows from spec: Sailing search → `OTA_CruisePriceBookingRQ/RS` (pricing) → booking commit/modify → retrieve/amend.
- Identify mandatory SOAP fields (POS, currency, sailing, ship code, fare code, cabin selection, promotions, guest qualifiers) from spec pages ~162-205.
- Decide layering: UI (Blazor) → App services (pricing, booking, lookup) → Infrastructure (SOAP client, SQL repo, cache).
- Output: architecture note, component diagram, checklist of required API fields.

## Phase 2 — Data & Lookup Layer
- Design tables: Ships, Decks, Ports, Regions, SubRegions, CabinCategories, CabinConfig, BedType, Titles, Gateways; add audit columns and source version.
- Build repeatable import scripts from CSV/XLS; include checksum/version to detect updates.
- Implement lookup repository + cached lookup service; add refresh endpoint (admin-only).
- Output: SQL scripts, lookup service, seed job; quick tests for lookup correctness.

## Phase 3 — Domain Model Finalization
- Finalize C# records/classes for: SailingInfo, SelectedCategory/Cabin, Promotions, BookingPayment (BookingPrices, PaymentSchedule), GuestPrices (PriceInfos), error/warning envelopes.
- Map OTA codes/attributes: price type codes, non-refundable types, auto-added indicators, promotion codes, POS/currency handling.
- Output: domain models + mapping guide referencing spec sections; unit tests for mapping fixtures.

## Phase 4 — SOAP Client Integration (Pricing First)
- Build SOAP client wrapper with configurable endpoint, timeouts, retries, correlation IDs; redact secrets in logs.
- Implement request builder for `OTA_CruisePriceBookingRQ` (price/reprice) using lookups; support promo codes and guest qualifiers.
- Implement response parser for `OTA_CruisePriceBookingRS` to domain, including warnings/errors and auto-add charges.
- Add health/diagnostics endpoint and mock provider for offline dev.
- Output: working pricing call to staging; parser/builder tests; mock implementation.

## Phase 5 — Sailing Search & Pricing UI
- Search UI: filters for region/port/ship/date/duration/category; validate inputs; show loading/errors.
- Pricing view: render booking totals, payment schedule, guest-level breakdown, promotions, cabin details; include warnings from RS.
- Role-aware display: agents/supervisors see full detail; customers see allowed subset.
- Output: Blazor pages bound to pricing service; UX with error/empty states.

## Phase 6 — Booking Workflow
- Booking create: form for guest data, cabin selection, promotions; submit to booking API; handle confirmation/payment schedule.
- Persist booking snapshot and pricing audit (booking + guest line items) with versioning and timestamps.
- Retrieve/amend: fetch existing booking, reprice with `TransactionActionCode=RetrievePrice`, display differences.
- Output: end-to-end booking sans payments; DB persistence; amend/retrieve flow.

## Phase 7 — Security & Authorization
- Implement authentication (Identity/SSO placeholder) and policies: customer, agent, supervisor, admin.
- Protect pricing/booking calls and lookup refresh; log actions with correlation IDs (no secrets).
- Output: policy-enforced endpoints/UI; audit log trail.

## Phase 8 — Payment Integration Readiness
- Define payment abstraction (intent/authorize/capture) with idempotency keys and webhook contracts.
- Extend DB: payment_attempts table (provider ref, status, amounts, currency, audit).
- UI placeholders after booking confirmation to start payment; feature-flagged until gateway chosen.
- Output: payment-ready interfaces/schema; toggled UI path.

## Phase 9 — Testing & Quality
- Unit tests: request builders, response parsers, lookup service, pricing calculators (if any).
- Integration tests: SOAP client vs. staging or recorded fixtures; repository tests on SQL 2019.
- Performance checks: pricing latency benchmarks; caching effectiveness; pagination for search.
- Output: test suite + runbook; perf notes and thresholds.

## Phase 10 — Deployment & Operations
- Deployment guide for Windows Server 2016 (IIS/Kestrel), config via env vars/user-secrets, HTTPS binding.
- DB migrations and backup/restore steps; lookup seeding job.
- Monitoring/alerts: API failure rates, booking errors, health checks; log retention plan.
- Output: deployment checklist, ops runbook.

## Acceptance Criteria (incremental)
- Pricing: valid inputs return booking-level and guest-level breakdown per spec and render in UI.
- Lookups: reference data available in filters; admin refresh works.
- Booking: bookings persisted with pricing audit; retrieve/amend supports repricing.
- Security: role policies enforced; secrets not logged/committed.
- Payment-ready: abstraction and schema in place; UI can branch into payment once gateway is selected.





