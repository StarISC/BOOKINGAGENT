# BookingAgent Delivery Plan (Detailed)

## Assumptions & Constraints
- Platform: Windows Server 2016, SQL Server 2019, .NET 9, Blazor Server.
- Canonical sources: `document/RCL Cruise FIT Spec 5.2.pdf` (pricing/booking flows, esp. pages ~162-205) and lookup tables in `document/RCL Cruises Ltd - API TABLES/` (ships, decks, ports, regions/subregions, cabin categories/config, bed types, titles, gateways).
- Security: secrets via env vars/user-secrets; never commit real credentials. Default DB name `BookingAgentDB`, admin account `admin`/`Admin@2025` stored securely.
- SQL Server host (current target): `Server=SHENLUNG\\SQLSERVER2022;Database=BookingAgentDB;User Id=sa;Password=123456;TrustServerCertificate=True;` (set via environment `ConnectionStrings__BookingAgent`; do not commit the password).
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

## Latest Updates (runtime readiness)
- Verified SQL `BookingAgentDB` exists on `SHENLUNG\\SQLSERVER2022` with seeded lookup data (e.g., CabinCategories 3447, Ports 1186, Ships 53, SubRegions 366, Regions 21).
- Build check: `dotnet build BookingAgent.sln` passes cleanly.
- Runtime config: set env `ConnectionStrings__BookingAgent=Server=SHENLUNG\\SQLSERVER2022;Database=BookingAgentDB;User Id=sa;Password=123456;TrustServerCertificate=True;` before running; keep API creds via env (`RoyalCaribbeanApi__Username=CONNCQN`, `RoyalCaribbeanApi__Password=qFDmKFM7eTaLk3s`, `RoyalCaribbeanApi__UseStub=true` by default to avoid unintended calls).
- Live API notes: SailingList works when `UseStub=false`; Login/BookingPrice still return CSE0572 authorization warning, so pricing stays stub until vendor clears access. Keep bin output with secrets out of commits.
- Runtime attempt 2025-12-17: starting on `http://localhost:5234` failed because port is already bound by an existing `BookingAgent.App.exe` (PID 41700). Reuse the running instance or start a new one on a free port, e.g. `dotnet run --project src/BookingAgent.App/BookingAgent.App.csproj --urls http://localhost:5290`.
- Runtime attempt 2025-12-17 (later): launched on `http://localhost:5290` with `UseStub=false` (live SailingList) and env credentials; pricing still returns stub data until vendor clears pricing/Login authorization.
- Runtime fix 2025-12-17: corrected endpoint builder to append path segments without dropping `/sca/`; reran on `http://localhost:5290` with `UseStub=false` (live mode). Stop-gap: terminated instance (PID 8352) after verification; rerun with env vars for live data.
- Branding note 2025-12-17: received Startravel logo (blue star + gradient text “Startravel – live your dream”); future layout must reserve header space, harmonize colors/typography, and avoid recoloring without approval.
- Branding identity 2025-12-17: company name “Công ty cổ phần dịch vụ Star International”, short name “Du lịch Startravel”, primary color #005edc, secondary #212654, hotline 0919 122 127, office 321 Nam Kỳ Khởi Nghĩa, Phường Xuân Hòa, HCM. Apply to header/footer and contact blocks when theming.
- UI pass 2025-12-17: updated layout/header styling to Startravel branding (primary #005edc, secondary #212654), refreshed header copy, contact info, and navigation colors; no binary assets added yet (logo slot ready for later).
- Auth integration 2025-12-17: added cookie auth, login page, AuthService with PBKDF2 verify, nav login/logout, role-aware menu placeholder, admin user hashed (password `123456` on staging DB). App uses `UseAuthentication/UseAuthorization` with login path `/login`.

## API Method Inventory (from RCL Cruise FIT Spec 5.2)
Base staging URL: `https://stage.services.rccl.com/Reservation_FITWeb/sca/{MethodName}` (Basic Auth; OTA payload per method namespace). Below are the interfaces seen in the spec; purposes are brief summaries (confirm payloads in the PDF):
- Login: authenticate agency/terminal (returns agency auth response).
- LookupAgency: fetch agency details/validation.
- SailingList: list sailings by region/date/duration.
- PackageList / PackageDetail: list or fetch details of cruise packages.
- TransferList / TransferDetail: list or fetch transfer add-ons.
- BusList / BusDetail: list or fetch bus add-ons.
- TourList / TourDetail: list or fetch shore tours.
- CategoryList: list cabin categories.
- CabinList / CabinDetail: list cabins and fetch cabin detail.
- HoldCabin / ReleaseCabin: place or release a cabin hold.
- FareList / FareDetail: list fares and fetch fare detail.
- BookingPrice: price a booking (`OTA_CruisePriceBookingRQ/RS`).
- LinkedBooking (validateLinkedBookings): validate linked bookings.
- PaymentExtension: get payment extension options.
- ConfirmBooking: confirm a booking after pricing/hold.
- OptionList / OptionDetail: list optional services and details.
- GuestServiceList: list guest services.
- FastSell: quick-sell flow using predefined data.
- Payment (makePayment): process a payment on a booking.
- ItineraryDetail: fetch itinerary details.
- BookingDocument: retrieve booking documents.
- RetrieveBooking / BookingList / BookingHistory / ReleaseBooking: booking retrieval, listing, history, and release actions.
- AutoAddChargeDetail: fetch automatic add-on charges.
- DiningList: list dining options.

### Method details (key payload/fields and RS elements)
- Common POS block (most methods): `POS/Source@ISOCurrency`, `@TerminalID`, `RequestorID@Type=5/@ID`, `BookingChannel@Type=7/CompanyName@CompanyShortName`.
- SailingList (`getSailingList` → OTA_CruiseSailAvailRQ): `GuestCounts/GuestCount@Quantity`, `SailingDateRange@Start/@MinDuration/@MaxDuration`, `CruiseLinePrefs/CruiseLinePref@VendorCode`, `RegionPref@RegionCode/@SubRegionCode`, optional ship/port filters. RS: `SailingOption/SelectedSailing` (ShipCode, Region/SubRegion, Departure/ArrivalPort, Start, Duration) + `InclusivePackageOption@CruisePackageCode`.
- Login (`login` → RCL_CruiseLoginRQ): POS only; RS returns agency auth/warning codes (e.g., CSE0572).
- LookupAgency (`lookupAgency` → RCL_CruiseLoginRQ): POS only; RS returns agency details (IDs, name, address, phone).
- BookingPrice (`getBookingPrice` → OTA_CruisePriceBookingRQ/RS): RQ fields include POS, `SailingInfo/SelectedSailing` (Start, Duration, StartLocationCode, EndLocationCode, ShipCode/ListOfSailingDescriptionCode), `SelectedCategory/FareCode`, optional `SelectedCabin@CabinNumber/@DeckNumber/@BerthCount`, `GuestDetails/GuestDetail` (Age/BirthDate/Citizenship), currency, promotions, `ReservationInfo` (ReservationID, TransactionActionCode). RS: `BookingPayment` (BookingPrices, PaymentSchedule, GuestPrices/PriceInfos), `SelectedSailing/Category/Cabin`, promotions, warnings/errors.
- HoldCabin (`holdCabin` → OTA_CruiseCabinHoldRQ): POS, SelectedSailing, SelectedCategory, SelectedCabin (number/berths/deck), guest count; RS returns Hold/Option ID and expiration.
- ReleaseCabin (`releaseCabin` → OTA_CruiseCabinUnholdRQ): POS + hold identifiers; RS confirms release.
- CategoryList (`getCategoryList`): POS + sailing context; RS returns cabin category codes/descriptions.
- CabinList (`getCabinList`): POS + sailing/category; RS returns cabins with deck/berth/status.
- FareList/FareDetail: POS + sailing/category; RS returns fare codes, combinability, qualifiers.
- ConfirmBooking (`confirmBooking`): POS + priced context + guest/payment details; RS returns booking confirmation and payment schedule.
- RetrieveBooking/BookingList/BookingHistory: POS + identifiers; RS returns booking header, guests, pricing, history entries.
- PaymentExtension: POS + booking; RS returns allowed extensions.
- Payment (makePayment): POS + booking and payment data; RS returns payment status/refs.
- OptionList/OptionDetail, GuestServiceList: POS + sailing/booking; RS lists optional/guest services.
- PackageList/Detail, TransferList/Detail, BusList/Detail, TourList/Detail: POS + sailing/port/date; RS lists add-on packages/transfers/bus/tours with codes and pricing.
- ItineraryDetail: POS + sailing/package; RS returns day-by-day itinerary segments.
- AutoAddChargeDetail: POS + booking/sailing; RS returns auto-added charge components.

### Deeper field checklist (per method)
- POS (all): Source@ISOCurrency, @TerminalID, RequestorID@Type/ID, BookingChannel@Type + CompanyName@CompanyShortName. Carry currency consistently (USD in examples).
- SailingList RQ: MaxResponses/MoreIndicator, SequenceNmbr, TimeStamp, TransactionIdentifier, Version; GuestCounts (quantities per guest), SailingDateRange (Start, MinDuration, MaxDuration), CruiseLinePref@VendorCode, RegionPref@RegionCode/@SubRegionCode, optional ship (ListOfSailingDescriptionCode) and port filters. RS: SailingOption→SelectedSailing (Start, Duration, ListOfSailingDescriptionCode, ShipCode, Region/SubRegion, DeparturePort@LocationCode, ArrivalPort@LocationCode), InclusivePackageOption@CruisePackageCode; honor MoreIndicator for paging.
- BookingPrice RQ: POS; SailingInfo/SelectedSailing (Start, Duration, StartLocationCode, EndLocationCode, ShipCode/ListOfSailingDescriptionCode, InclusivePackageOption@CruisePackageCode); SelectedCategory/FareCode; SelectedCabin (CabinNumber, DeckNumber, BerthCount, Status); GuestDetails/GuestDetail (GuestRefNumber, Age, BirthDate, Citizenship, Residency, Loyalty IDs if any); Currency; Promotions/PromotionCode; ReservationInfo (ReservationID@ID/@StatusCode, TransactionActionCode e.g., Start/RetrievePrice/Commit); PaymentInfo (optional). RS: BookingPayment→BookingPrices (PriceTypeCode, Amount, Currency), PaymentSchedule/Payment (PaymentNumber, Amount, DueDate), GuestPrices→GuestPrice→PriceInfo (PriceTypeCode/Amount), SelectedSailing/SelectedCategory/SelectedCabin, Promotions, Warnings/Errors.
- HoldCabin RQ: POS; SelectedSailing (Ship/Start/Duration/Ports); SelectedCategory (FareCode/CategoryCode); SelectedCabin (CabinNumber, DeckNumber, MaxOccupancy/BerthCount); GuestCounts; Hold parameters (OptionExpiry). RS: Hold/Option identifiers, expiry, cabin status.
- ReleaseCabin RQ: POS; Hold identifiers (Option/ReservationID). RS: release confirmation.
- CategoryList RQ: POS; sailing context (Ship/Date/Duration/Region). RS: CategoryCode, Description, Min/MaxOccupancy, Deck info.
- CabinList RQ: POS; sailing + category; occupancy filters. RS: CabinNumber, DeckNumber, Status (available/held/booked), MaxOccupancy/BerthConfig, Attributes (location, obstructed view).
- FareList/FareDetail RQ: POS; sailing + category; qualifiers (residency, promo). RS: FareCode, Description, Combinability, Currency, Effective/Expiry dates, qualifiers.
- ConfirmBooking RQ: POS; priced context (Sailing/Category/Cabin/Fare); GuestDetails (names, birthdates, citizenship, contact), PaymentInfo, Promotions, SpecialServices. RS: Booking confirmation (ReservationID), PaymentSchedule, BookingPrices, GuestPrices.
- RetrieveBooking/BookingList/BookingHistory RQ: POS; ReservationID or search qualifiers. RS: booking header, guests, pricing snapshot, history entries with timestamps/actions.
- PaymentExtension RQ: POS; ReservationID; request extension type/days. RS: extension options/approval.
- Payment (makePayment) RQ: POS; ReservationID; PaymentAmount, Currency, PaymentRef/Method. RS: status, approval/transaction refs.
- OptionList/OptionDetail, GuestServiceList RQ: POS; sailing/booking context. RS: service codes, descriptions, prices, eligibility.
- Package/Transfer/Bus/Tour List/Detail RQ: POS; port/ship/date context; MaxResponses/MoreIndicator for paging. RS: codes, descriptions, prices, availability, duration/segments.
- ItineraryDetail RQ: POS; sailing/package context. RS: day-by-day port calls (PortCode, Arrival/Departure times), notes.
- AutoAddChargeDetail RQ: POS; booking/sailing context. RS: list of auto-added charge codes/amounts.
- DiningList RQ: POS; ship/date/meal context. RS: dining options, timeslots, availability.

### Field-level deep dive (selected critical OTAs)
- GuestDetails (BookingPrice/ConfirmBooking): include `GuestRefNumber`, `GuestName` (Prefix, Given, Surname), `BirthDate`, `Age`, `Gender`, `Nationality/Citizenship`, `ResidentCountryCode`, `Document` (Type, Number, Expiration), `LoyaltyInfo` (ProgramID/Level, MembershipID), `ContactInfo` (Email, Phone), `ProfileRef` if supported. Age/citizenship affect pricing/promo eligibility.
- Promotions: `PromotionCode`, `GroupCode`, `AgencyGroupCode` as applicable; ensure multiple promotion elements are handled; respect combinability rules returned in RS.
- Price components (RS): `BookingPrice@PriceTypeCode` (e.g., FARE, TAX, NCF, GRAT), `@Amount`, `@CurrencyCode`, `@AutoAddIndicator`; `PaymentSchedule/Payment` includes `PaymentNumber`, `Amount`, `DueDate`, `Type` (Deposit/Final).
- GuestPrice@PriceInfos: each `PriceInfo@PriceTypeCode` with Amount/Currency; map to UI by guest.
- SelectedCabin (RS): `CabinNumber`, `DeckNumber`, `MaxOccupancy`, `Status`, `CategoryLocation`, `BedConfiguration`, `ConnectingCabinIndicator`, `ObstructedViewIndicator`.
- Sailing identifiers: `ListOfSailingDescriptionCode` often combines ship and sail ID; keep both `ShipCode` and description code for subsequent calls (hold/price/confirm).
- Pagination indicators: `MaxResponses`, `MoreIndicator`, sometimes `StartIndex`/`NextToken` (check RS for continuation handle). Loop until no `MoreIndicator`.
- Error/Warning handling: `Warnings/Warning` and `Errors/Error` nodes include codes/text; surface user-friendly messages and log codes (redacted of PII).
- Currencies: respond with `@CurrencyCode`; do not assume USD; bind currency to UI and calculations.
- Dates/times: OTA uses ISO (e.g., `yyyy-MM-dd`, `yyyy-MM-ddTHH:mm:ss`); ports may be local time—confirm spec if timezone is implied or absent.
- Payment (makePayment): `PaymentAmount`, `Currency`, `PaymentRef/Authorization`, `PaymentCard` (if card), `PaymentType`; store transaction references for audit.
- RetrieveBooking: expect full snapshot (guests, pricing components, payment schedule, cabin/category/fare, promotions, itinerary) and use it for display/reprice.
- ItineraryDetail: `Region`, `PortCall` items with `PortCode`, `Arrival/Departure` times, `DayNumber`, `Duration`.

### Model field reference (RS) by domain entity
- SailingOption (SailingList RS): ShipCode, VendorCode, RegionCode, SubRegionCode, DeparturePort@LocationCode, ArrivalPort@LocationCode, Start (date), Duration (ISO period), ListOfSailingDescriptionCode, CruisePackageCode (from InclusivePackageOption).
- SailingInfo (BookingPrice RS): SelectedSailing (Start, Duration, Start/EndLocationCode, ShipCode, ListOfSailingDescriptionCode, Region/SubRegion), SelectedCategory (FareCode, CategoryLocation, Status), SelectedCabin (CabinNumber, DeckNumber, MaxOccupancy, Status, BedConfiguration, Connecting/Obstructed indicators), InclusivePackageOption@CruisePackageCode, SailingDescription/Itinerary (if present).
- Pricing totals (BookingPayment RS): BookingPrices[] { PriceTypeCode, Amount, CurrencyCode, AutoAddIndicator }, PaymentSchedule/Payments[] { PaymentNumber, Amount, CurrencyCode, DueDate, Type (Deposit/Final) }, GuestPrices[] { GuestRefNumber, PriceInfos[] { PriceTypeCode, Amount, CurrencyCode, AutoAddIndicator } }.
- Promotions (RS): PromotionCode, Description/Text, Status, Combinability, AutoAddIndicator; at booking-level and/or guest-level.
- Booking identifiers (RS): ReservationID (ID, StatusCode), Option/Hold IDs (for hold/release), Transaction identifiers.
- Cabin/Hold (HoldCabin RS): OptionID, ExpirationDateTime, CabinNumber, DeckNumber, Status, BerthCount/MaxOccupancy.
- Fare (FareList/Detail RS): FareCode, Description, CurrencyCode, Effective/ExpiryDate, CombinabilityRules, Qualifiers (Residency, Loyalty, Promo), Min/MaxGuests, ApplicableCategories.
- Category (CategoryList RS): CategoryCode, Description, Min/MaxOccupancy, DeckRange, LocationAttributes.
- Cabin list (CabinList RS): CabinNumber, DeckNumber, Status (Available/Held/Booked), MaxOccupancy, BerthConfig, Location (Forward/Aft/Midship, Port/Starboard), ObstructedViewIndicator, ConnectingIndicator.
- Add-ons (Package/Transfer/Bus/Tour List/Detail RS): Code, Description, Price (Amount/Currency), AvailabilityStatus, Duration/Segments, Port/Ship/Date scope, Capacity limits.
- Options/GuestServices (OptionList/Detail, GuestServiceList RS): ServiceCode, Description, Price, Eligibility (guest/booking), Timing/Slot if applicable.
- DiningList RS: DiningOptionCode, MealType, Timeslot, Availability, Location.
- Payment RS (makePayment): StatusCode, PaymentRef/AuthorizationCode, Amount, Currency, PaymentDate, Error/Warning if failed.
- Booking retrieval (RetrieveBooking/BookingList/History): ReservationID, SailingInfo, Pricing snapshot (BookingPayment), Promotions, GuestDetails, Cabin/Category/Fare, PaymentSchedule, History entries (ActionCode, TimeStamp, Agent/Channel).

### Reference: PriceTypeCode & Promotion codes (from PDF scan)
- PriceTypeCode values seen (examples): 1, 3, 6, 7, 8, 18, 34, 42, 46, 49, 58, 60, 73, 80, 81, 90, 98, 100, 101, 102, 103, 104, 107, 127, 156, 161, 162, 163, 164, 166. Map to spec table (e.g., FARE/NCF/TAX/GRAT/FEES/INSURANCE) when implementing enum.
- PromotionClass observed: 1, 12, 14, 18. PromotionType observed: 1, 24, 44, 50, 58, 68. Store as codes per spec; render description from `PromotionDescription` when returned.

### TODO: Map code meanings (create enums from spec tables)
| Code type | Code | Meaning (per spec) | Notes / Page ref |
|-----------|------|--------------------|------------------|
| PriceTypeCode | 1,3,6,7,8,18,34,42,46,49,58,60,73,80,81,90,98,100,101,102,103,104,107,127,156,161,162,163,164,166 | TBD from spec table (likely FARE/NCF/TAX/PORT/GRAT/FEES/INSURANCE/ONBOARD CHARGE, etc.) | PriceTypeCode table near pricing section (~page 170-180); confirm exact mapping |
| PromotionClass | 1,12,14,18 | TBD from spec | Extract from promo section (Promotions Enhancement) |
| PromotionType | 1,24,44,50,58,68 | TBD from spec | Extract from promo section |
| PricedComponentType | (values referenced with PriceTypeCode, e.g., “I”) | TBD from spec | Capture when parsing pricing components |
| PricingLevel | e.g., 24 (from sample) | TBD | Identify level meaning (booking/guest/cabin) |

Action: extract exact descriptions from `RCL Cruise FIT Spec 5.2.pdf` pricing tables (around pages ~170-180) and wire enums with descriptions/tooltips in UI.

### Manual extraction plan (code meanings)
- Locate pricing tables in PDF (approx. pages 170-180): PriceTypeCode, PricedComponentType, PricingLevel.
- Locate promotions section: PromotionClass/PromotionType tables and descriptions.
- Transcribe code → meaning verbatim; store in a markdown table in `plan.md`/`plan_vi.md` and create enums in code with descriptions.
- Validate against sample RS payloads to ensure mapping correctness; update UI tooltips with the transcribed meanings.
- Docx copy: `document/RCL Cruise FIT Spec 5.2.docx` is available; `python-docx` installed, tables exported to `document/docx_tables_extract.txt` (59 relevant tables). Need manual review to capture definitions (current extract shows samples, not the code→meaning table—likely rendered as non-text/embedded objects).

### Method call sequence (happy path, derived from spec)
1) Login → optional LookupAgency (to validate agency metadata).
2) Lookup/Filter (optional): PackageList/FareList/CategoryList/CabinList for preselection; SailingList to get available sailings (use MaxResponses/MoreIndicator to page).
3) Select sailing/category/cabin:
   - SailingList → pick SailingOption (Ship/Start/PackageCode).
   - CategoryList → pick category; CabinList → pick cabin (if needed).
4) Price: BookingPrice (OTA_CruisePriceBookingRQ) with POS + selected sailing/category/cabin + guests + promotions.
5) Hold (optional): HoldCabin to reserve cabin (receive Option/Hold ID and expiry).
6) Confirm booking: ConfirmBooking with pricing context, guest details, payment info, promotions; receive ReservationID, PaymentSchedule, final prices.
7) Payment: makePayment or PaymentExtension (if allowed) using ReservationID.
8) Post-actions: RetrieveBooking/BookingHistory/BookingDocument for display; OptionList/GuestServiceList/Package/Transfer/Bus/Tour for upsell; ReleaseCabin or ReleaseBooking if abandoning.

Notes:
- Pricing may be repeated (repricing) before ConfirmBooking; use TransactionActionCode=RetrievePrice when re-evaluating.
- MoreIndicator requires looping for list endpoints (SailingList, Package/Transfer/Bus/Tour, FareList, BookingList).
- Non-refundable promotions can be excluded via flags in fare/promo selection (per spec).

## API method status & DTO sketch (staging)
- Login (`login`): tested → HTTP 200 with warning CSE0572 “ACCESS NOT AUTHORIZED FOR THIS AGENCY”; auth not granted. DTO: `LoginResponse { WarningCode, WarningText }`.
- LookupAgency (`lookupAgency`): tested → 200 OK, returns agency info (IDs 378372/275611, address/phone). DTO: `AgencyInfo { AgencyIds[], Name, Address, Phone }`.
- SailingList (`getSailingList`): tested → 200 OK, returns sailing options (region FAR.E, OV/SC ships, packages). DTO: `SailingOption { ShipCode, PackageCode, StartDate, Duration, DeparturePort, ArrivalPort, Region, SubRegion }`.
- BookingPrice (`getBookingPrice`): tested → 500 env:Client “Internal Error”; blocked until vendor enables. DTO: `BookingPriceResponse { BookingPayment (BookingPrices, PaymentSchedule, GuestPrices), SailingInfo, Promotions, Warnings/Errors }`.
- HoldCabin/ReleaseCabin: not yet tested; expected DTOs: `HoldCabinResponse { OptionId, Expiration, Cabin }`, `ReleaseCabinResponse { Success, Message }`.
- CategoryList/CabinList: not yet tested; DTOs: `Category { Code, Description, Min/MaxOccupancy }`, `Cabin { CabinNumber, DeckNumber, Status, MaxOccupancy, LocationFlags }`.
- FareList/FareDetail: not yet tested; DTOs: `Fare { FareCode, Description, Currency, Qualifiers, Combinability }`.
- ConfirmBooking: not tested; DTO: `ConfirmBookingResponse { ReservationId, PaymentSchedule, BookingPrices, GuestPrices }`.
- Payment/PaymentExtension: not tested; DTOs: `PaymentResponse { Status, PaymentRef, Amount }`, `PaymentExtensionResponse { Options[], Approved }`.
- OptionList/GuestServiceList/Package/Transfer/Bus/Tour: not tested; DTO: `AddOn { Code, Description, Price, Availability, Scope }`.
- ItineraryDetail: not tested; DTO: `ItineraryDetail { PortCalls[], Region }`.
- BookingDocument/RetrieveBooking/BookingHistory/BookingList/ReleaseBooking: not tested; DTOs: `BookingSnapshot { ReservationId, SailingInfo, Pricing, Promotions, Guests, History[] }`.

## Role-based navigation & functions (draft)
- Customer:
  - Menu:
    - Overview/Dashboard: welcome, status of current bookings, quick links.
    - Sailing Search: filters (region/port/ship/date/duration/category), results list, basic sailing detail.
    - Pricing (limited): view allowed price breakdown for selected sailing/cabin.
    - My Bookings: list + detail (itinerary, payment schedule), start payment (when enabled).
    - Support/Contact: hotline, office info.
  - Actions: search sailings, view permitted prices, start booking, view own bookings, initiate payment (if enabled).
- Agent:
  - Menu:
    - Customer menus (full access to price detail).
    - Booking Create/Amend: capture guest data, select cabin/promo, submit pricing/booking.
    - Holds: create/release cabin holds.
    - Booking List/Retrieve: search by ReservationID/date/agency.
    - Payment/Extension: trigger payment, request extension.
    - Reports (basic): booking list/export.
  - Actions: full pricing details, apply promotions, hold/release cabin, create/modify booking, request payment extension, process payments, view audit data.
- Supervisor:
  - Menu:
    - Agent menus.
    - Approval/Overrides: queue of requests needing approval (discounts, overrides).
    - Ops Reports: performance/booking volume.
    - Booking History: detailed audit trail.
    - Health/Diagnostics (read-only): service/API status.
  - Actions: approve overrides/discounts, monitor bookings, review history/audit, escalate incidents.
- Admin:
  - Menu:
    - Admin Console: lookup refresh/import, config flags (UseStub, endpoints, feature toggles), user/role management.
    - Diagnostics/Health: API/DB connectivity, background jobs.
    - Logs (redacted): operational logs without secrets.
    - Reference Data: view lookup tables, trigger reseed.
  - Actions: manage users/roles, toggle UseStub/ops config, refresh/import lookups, view health, manage feature flags (payment, live API), run backup/maintenance scripts.

### Auth/Role storage (gap to address)
- Current DB schema only covers lookups; no tables yet for users/roles/permissions.
- Proposed (future): `Users` (Id, Username, PasswordHash, Email, Phone, IsActive, CreatedAt/UpdatedAt), `Roles` (Id, Name, Description), `UserRoles` (UserId, RoleId). Optional: `Permissions`/`RolePermissions` for fine-grained control.
- Consider Identity/SSO later; interim local auth should use salted hashes and minimal profile; do not store plaintext passwords.
- Script added: `db/auth_schema.sql` creates `Users`, `Roles`, `UserRoles` with richer fields (profile, contact, lockout, timestamps) and seeds roles (Customer, Agent, Supervisor, Admin); permissions tables commented for future.
- Admin seed: script inserts `admin` user with empty hash/salt (0x); must be set to real hash/salt via secure deployment step before production use.
- Admin password updated (temp for staging): user `admin` set with PBKDF2-SHA256 hash/salt for password `123456` on BookingAgentDB (updated via script; change in production if needed).
## Progress
- Done: Solution scaffolding (Blazor Server + Domain), initial domain pricing models, UI shell, placeholder pricing service, database schema for lookups, bulk import script, lookup domain interfaces, SQL-backed lookup service with caching and DI wiring, config placeholders for SHENLUNG\\SQLSERVER2022, search UI wired to lookup data (regions/ports/ships/cabin categories) in `/search`, SOAP pricing client skeleton (config binding, HttpClient, sample response stub).
- In progress: Expand SOAP request builder/response parser toward OTA_CruisePriceBookingRQ/RS (POS/Sailing/DeparturePort/Category, guests, promotions, booking/guest prices, payment schedule); search page calls pricing service with filters (stub response). Using configurable UseStub to avoid accidental live calls.
- Next: Disable UseStub with env/user-secrets credentials to test live SOAP pricing, adjust SOAPAction/path if required; then complete booking flow and role policies; payment abstraction prep.

## Work Log (recent)
- Created SQL schema + import scripts for lookup tables; added lookup service with cache and DI.
- Built Blazor shell, search page using lookup data; pricing page shows sample breakdown.
- Added SOAP pricing client skeleton (HttpClient, config binding, Basic Auth), request builder stub, response parser stub; search page triggers pricing service with criteria.
- Expanded SOAP builder (POS/Sailing/DeparturePort/Category, duration, guest list, selected cabin, promotions) and parser (booking prices, payment schedule, guest price infos, promotions, selected cabin); client still falls back to sample when parsing fails.
- Search page now shows pricing breakdown (booking totals, payment schedule, guest pricing) from the service stub while awaiting live SOAP integration.
- Config now includes OperationPath/SoapAction options for staging endpoint flexibility and UseStub toggle to prevent accidental live calls.
- UI displays current pricing mode (stub vs live) on the search page to clarify environment status.
- Attempted staging call via SOAP POST to BookingPrice (Basic auth) returned SOAP Fault env:Client "Internal Error"; next step: verify endpoint/SOAPAction/payload with vendor docs/support.
- Tested staging Login endpoint (Basic auth) per provided payload; HTTP 200 with warning CSE0572 "ACCESS NOT AUTHORIZED FOR THIS AGENCY" — need vendor confirmation on agency/RequestorID/credentials to proceed.

## Work Log (recent)
- Created SQL schema + import scripts for lookup tables; added lookup service with cache and DI.
- Built Blazor shell, search page using lookup data; pricing page shows sample breakdown.
- Added SOAP pricing client skeleton (HttpClient, config binding, Basic Auth), request builder stub, response parser stub; search page triggers pricing service with criteria.
- Expanded SOAP builder (POS/Sailing/DeparturePort/Category, duration, guest list, selected cabin, promotions) and parser (booking prices, payment schedule, guest price infos, promotions, selected cabin); client still falls back to sample when parsing fails.
- Search page now shows pricing breakdown (booking totals, payment schedule, guest pricing) from the service stub while awaiting live SOAP integration.
- Config now includes OperationPath/SoapAction options for staging endpoint flexibility (still set via env/user-secrets; no secrets committed).

## Work Log (recent)
- Created SQL schema + import scripts for lookup tables; added lookup service with cache and DI.
- Built Blazor shell, search page using lookup data; pricing page shows sample breakdown.
- Added SOAP pricing client skeleton (HttpClient, config binding, Basic Auth), request builder stub, response parser stub; search page triggers pricing service with criteria.
- Expanded SOAP builder (POS/Sailing/DeparturePort/Category, duration) and parser (booking prices, payment schedule, guest price infos, promotions); client still falls back to sample when parsing fails.

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






