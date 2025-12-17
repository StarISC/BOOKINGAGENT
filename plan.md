# BookingAgent Delivery Plan (Detailed)

## Assumptions & Constraints
- Platform: Windows Server 2016, SQL Server 2019, .NET 9, Blazor Server.
- Canonical sources: `document/RCL Cruise FIT Spec 5.2.pdf` (pricing/booking flows, esp. pages ~162-205) and lookup tables in `document/RCL Cruises Ltd - API TABLES/` (ships, decks, ports, regions/subregions, cabin categories/config, bed types, titles, gateways).
- Security: secrets via env vars/user-secrets; never commit real credentials. Default DB name `BookingAgentDB`, admin account `admin`/`Admin@2025` stored securely.
- Roles: customers, agents, supervisors/admins. Role-based UI/actions required.
- Future: payment gateway integration; design extensible boundaries now.
- API auth/endpoint (staging): base URL `https://stage.services.rccl.com/Reservation_FITWeb/sca/<API>`; CompanyShortName `NOVASTAR`; credentials `username=CONNCQN`, `password=qFDmKFM7eTaLk3s` must be supplied via environment/user-secrets (do not commit plaintext).

## Phase 1 — Discovery & Architecture
- Confirm user journeys: sailing search → price retrieval → cabin selection → booking → (future) payment.
- Identify API endpoints to prioritize: `OTA_CruisePriceBookingRQ/RS` (pricing), booking create/modify, booking retrieve.
- Define architecture: Blazor Server UI; application services (pricing, booking, lookup); infrastructure (SOAP client, SQL repositories, caching); authZ layer.
- Deliverable: architecture note with component boundaries; updated domain outline.

## Phase 2 — Data & Lookup Layer
- Design SQL schema with audit columns for: ships, decks, ports, regions/subregions, cabin categories/config, bed type, titles, gateways.
- Build import/refresh scripts from the provided CSV/XLS; keep reproducible seed scripts.
- Expose lookup service with caching for UI filters and request builders; include invalidation strategy.
- Deliverable: schema + seed scripts; lookup service contracts; basic tests.

## Phase 3 — Domain Model Finalization
- Align models to OTA: SailingInfo, BookingPayment, BookingPrices, PaymentSchedule, GuestPrices/PriceInfos, Promotions, SelectedCabin/category.
- Map response fields carefully (restricted indicators, auto-added options, non-refundable types, price type codes).
- Deliverable: finalized domain models + mapping guidelines referencing spec sections.

## Phase 4 — SOAP Client Integration (Pricing First)
- Implement SOAP client wrapper with configurable endpoint, timeouts, retries, correlation IDs; structured logging without secrets.
- Build request builders for `OTA_CruisePriceBookingRQ` (price/reprice) including POS, sailing, category, cabin, promotions, guest qualifiers.
- Parse `OTA_CruisePriceBookingRS` into domain models; handle warnings/errors and auto-add charges.
- Add health/diagnostics endpoint.
- Deliverable: pricing call working against staging/mock; parser tests; fallback mock provider.

## Phase 5 — Sailing Search & Pricing UI
- Search page: filters by region/port/ship/date/duration/category using lookup data.
- Pricing results page: booking totals, payment schedule, guest-level components, promotions, cabin details; clear labels and error states.
- Role-aware presentation (agents/supervisors see full breakdown; customers see permitted fields).
- Deliverable: functional UI bound to pricing service.

## Phase 6 — Booking Workflow
- Implement booking create flow: collect guest data, cabin selection, promotions; call booking APIs; display confirmation/payment schedule.
- Persist booking snapshot and pricing audit (booking + guest line items) to SQL Server with versioning.
- Retrieve/amend flow: reprice existing booking, show deltas.
- Deliverable: end-to-end booking (sans payment), persisted audits, role checks.

## Phase 7 — Security & Authorization
- Implement authentication (Identity/SSO placeholder) and policies for customer/agent/supervisor/admin.
- Protect API calls and admin/lookup refresh endpoints; log actions (no secrets).
- Deliverable: protected endpoints/UI; audit logging of pricing/booking actions.

## Phase 8 — Payment Integration Readiness
- Define payment abstraction (intent/authorize/capture) with idempotency and webhook hooks.
- Extend DB for payment attempts, provider references, statuses, reconciliation markers.
- Add UI placeholders for payment steps (flagged off until gateway chosen).
- Deliverable: payment-ready interfaces, schema changes, toggled UI pathway.

## Phase 9 — Testing & Quality
- Unit tests: request builders, response parsers, lookup service, pricing calculators (if any).
- Integration tests: SOAP client vs. staging or recorded fixtures; repository tests on SQL 2019.
- Performance: measure pricing latency, caching impact; paginate search results.
- Deliverable: test suite + run instructions; baseline perf notes.

## Phase 10 — Deployment & Operations
- Deployment guide for Windows Server 2016 (IIS/Kestrel), config via env vars/user-secrets, HTTPS binding.
- DB migration scripts and backup/restore steps; lookup seeding.
- Monitoring/alerts for API failures, booking errors, and health checks.
- Deliverable: deployment checklist and ops runbook.

## Acceptance Criteria (incremental)
- Pricing: valid inputs return booking-level and guest-level breakdown per spec, rendered in UI.
- Lookups: reference data available in filters; refresh path exists.
- Booking: bookings persisted with pricing audit; retrieve/amend works with repricing.
- Security: role policies enforced; no secrets in logs; secrets not in source.
- Payment-ready: abstraction and schema in place; UI can branch into payment when gateway is selected.
