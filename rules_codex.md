# Rules for BookingAgent Project

0) Work Protocol (Highest Priority)
- Always analyze the problem and propose an approach before coding. Wait for explicit user approval to proceed with implementation.

1) Security & Secrets
- Do not commit real credentials or connection strings. Keep placeholders (`<your-...>`) in config and use environment variables/user-secrets for real values.
- Treat API credentials and DB access as sensitive; avoid logging them. Redact secrets in errors.
- No uploading proprietary docs (e.g., `document/RCL Cruise FIT Spec 5.2.pdf`) outside the repo.

2) Platforms & Targets
- Runtime/SDK: target .NET 9.0 for all projects (aligns `BookingAgent.App` and `BookingAgent.Domain`).
- App style: Blazor Server (no WASM switch unless approved).
- Backend: Windows Server 2016, SQL Server 2019; ensure SQL scripts remain compatible with 2019.

3) API Integration (Royal Caribbean)
- Follow `OTA_CruisePriceBookingRQ/RS` (see pages ~162-205 in the provided PDF) as the canonical shape for pricing data.
- Mirror response fields in domain models; do not drop pricing components (BookingPrices, PaymentSchedule, GuestPrices, promotions).
- Keep SOAP client isolated behind a service interface; add retries/timeouts and structured logging (without secrets).

4) Configuration
- Store defaults in `appsettings*.json` with placeholders only. Real values must come from secrets/env vars.
- Connection string key: `ConnectionStrings:BookingAgent`. API config section: `RoyalCaribbeanApi`.

5) Database
- Schema must support auditing of price components (booking-level and guest-level line items). Include created/updated timestamps.
- Use migrations/scripts compatible with SQL Server 2019; avoid features beyond that version.

6) UI/UX
- Preserve the current layout shell and navigation. New pages should follow the same styling tokens in `wwwroot/css/site.css`.
- Display complete pricing breakdowns (booking totals, payment schedule, per-guest components, promotions) with clear labels.

7) Coding Standards
- Keep files ASCII unless existing content requires otherwise.
- Add only concise, high-value comments for complex logic.
- Avoid destructive git commands or resets; do not revert user changes.

8) Testing & Build
- Ensure `dotnet build BookingAgent.sln` stays clean. Add focused tests for parsing/serialization when API integration lands.

9) Documentation
- Update this file when team rules change. Keep references to PDF pages for key API sections.

10) New API reference assets
- Additional reference tables are in `document/RCL Cruises Ltd - API TABLES/` (CSV/XLS for ships, decks, ports, regions, cabin categories, bed types, etc.). Treat these as authoritative lookup sources; keep them versioned in-place and never upload externally.

11) Roles & Access
- System supports multiple roles (e.g., internal agents, supervisors, customer-facing users). Enforce role-based access for searching sailings, viewing prices, and booking cabins; secure admin-only endpoints for configuration/lookups.

12) Booking & Payments (forward-looking)
- Core flows: sailing search, fare/price retrieval, cabin selection, booking creation/modification, with auditing of price components.
- Future requirement: integrate online payment API for customers to pay during booking. Design service boundaries so payment gateway can be plugged in without breaking pricing/booking flows; capture payment intent/transaction references in DB when implemented.
