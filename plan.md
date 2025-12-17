# BookingAgent Delivery Plan

## Phase 1 — Foundations
- Confirm requirements and constraints (roles, sailing search, pricing, booking, future payments). Lock target framework (.NET 9, Blazor Server) and SQL Server 2019 compatibility.
- Catalog external artifacts: RCL Cruise FIT Spec 5.2 (pricing/booking flows) and lookup tables in `document/RCL Cruises Ltd - API TABLES/` (ships, decks, ports, cabin categories, etc.).
- Finalize domain model aligned with `OTA_CruisePriceBookingRQ/RS` (sailing info, booking payment, guest price lines, promotions).

## Phase 2 — Data & Lookup Layer
- Import lookup tables into SQL Server (ships, regions, ports, deck, cabin category, bed type, titles, gateway). Provide scripts/stored procs for refresh.
- Expose a lookup service for Blazor/UI and pricing logic (cached, read-only).

## Phase 3 — API Integration
- Build SOAP client wrapper for Royal Caribbean pricing/booking endpoints with retries, timeouts, logging (no secret logging). Configuration from env/user-secrets.
- Implement request builders for `OTA_CruisePriceBookingRQ` and parsers for `RS`, mapping into domain models.
- Add basic health checks and diagnostics for the API client.

## Phase 4 — Search & Pricing UI
- Implement sailing search UI (filters: ship, region, departure port, date, duration, cabin category).
- Display pricing breakdowns (booking totals, payment schedule, guest-level components, promotions) using the domain models.
- Role-aware UI: agents/supervisors vs. customer-facing views (hide controls not permitted).

## Phase 5 — Booking Workflow
- Build booking flow: select sailing/category/cabin, collect guest details, submit booking, show confirmation and payment schedule.
- Persist booking snapshots and pricing audit (booking-level + guest-level line items) to SQL Server.

## Phase 6 — Security & Roles
- Implement authentication/authorization with role policies (agent, supervisor, customer). Protect booking and admin endpoints; restrict lookup maintenance to admins.
- Add logging/auditing for pricing calls and booking actions (no secrets).

## Phase 7 — Payments (future integration)
- Design payment abstraction to plug a payment gateway (capture intent, authorize/capture, webhooks). Store transaction references in DB.
- Add UI steps for payment initiation/confirmation; ensure idempotency and reconciliation hooks.

## Phase 8 — Testing & Hardening
- Unit tests for request builders/parsers and lookup services. Integration tests against staging API if available.
- Performance checks for search/pricing flows; handle pagination/caching of lookup data.
- CI build to enforce `dotnet build BookingAgent.sln` and test suite.

## Phase 9 — Deployment
- Produce deployment checklist for Windows Server 2016 hosting (IIS/Kestrel), configuration via env vars/user-secrets.
- DB migration scripts for SQL Server 2019; backup/restore guidance and seeding for lookups.
