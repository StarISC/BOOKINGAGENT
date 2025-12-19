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
- Default database name: `BookingAgentDB`; default admin account `admin` with password `Admin@2025` (store via env vars/user secrets; never commit plaintext secrets).

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

13) Planning Hygiene
- Keep `plan.md` (English) and `plan_vi.md` (Vietnamese) in sync with progress and proposals. Add new mandatory rules here when discovered to prevent regressions.

14) SOAP/API Safety
- Do not log SOAP payloads or credentials. If calling staging endpoints, prefer basic auth via HTTP headers and redact secrets in logs. Fall back to mocks if connectivity/auth is not confirmed.
- Use staging endpoints by default; set reasonable timeouts/retries. If a call fails, surface a safe user-facing message and do not expose SOAP faults verbatim.
- Keep a configurable stub toggle for external calls (e.g., `UseStub` in API options) to avoid accidental calls when credentials are not set; never hardcode secrets in code or configs.

15) Plan Updates
- Always update `plan.md` and `plan_vi.md` with progress, proposals, and work log entries after significant changes. Keep histories aligned in both languages.

16) API Method Findings
- Login: current credentials return warning CSE0572 "ACCESS NOT AUTHORIZED FOR THIS AGENCY" (RequestorID 275611/378372, TerminalID JOHN12); requires vendor clearance/agency enablement before use.
- LookupAgency: works with provided credentials; returns agency IDs (e.g., 378372, 275611) and contact info; can be used to confirm agency data.
- SailingList: works with provided credentials and OTA_CruiseSailAvailRQ; returns sailing options; can be integrated with caution while keeping `UseStub` toggle for live calls.

17) Config Hygiene
- Never rely on `bin/` output appsettings that may contain real secrets; ensure runtime secrets (DB password, API credentials) come from env vars/user-secrets and clean build artifacts before commit.

18) Endpoint Construction
- When calling RCCL SOAP APIs, do not lose the `/sca/` segment: avoid relative URI replacement that drops the last path segment. Always append paths explicitly (BaseUrl ending with `/sca/` or string-concatenate) and keep `UseStub` explicit when switching between live/stub.

19) Branding
- Use the provided Startravel logo and align layout to its branding. Keep a header space for the logo and ensure colors/typography harmonize with the logo (#005edc primary, #212654 secondary). Do not substitute or recolor without approval, and include company identity: “Công ty cổ phần dịch vụ Star International” (short name “Du lịch Startravel”), hotline 0919 122 127, office 321 Nam Kỳ Khởi Nghĩa, Phường Xuân Hòa, HCM.

20) Result Limits & Pagination
- The RCCL FIT APIs cap response sizes (e.g., MaxResponses/MoreIndicator). When pulling lists (SailingList, FareList, Package/Tour/Transfer/BusList, BookingList), always request in bounded batches and honor `MoreIndicator`/continuation tokens to page through all results; do not assume full data arrives in one call.

21) Admin Seed Safety
- Default admin user created by `db/auth_schema.sql` has empty hash/salt; deployment must set real hash/salt via a secure process. Never leave PasswordHash/PasswordSalt as 0x in production.

22) Staging API Warnings
- Treat CSE0572 (“ACCESS NOT AUTHORIZED FOR THIS AGENCY”) as a warning for Login; continue testing downstream calls while noting auth may be limited. If other endpoints return 401 “User is not found,” verify headers/payload, but keep a mock/stub path to avoid blocking UI.
