# Database setup

1) Create/update schema  
   - Run `db/schema.sql` on SQL Server 2019. It will create the database `BookingAgentDB` if missing and set up lookup tables with audit columns.

2) Import lookup data (CSV/XLS provided by RCL)  
   - Open `db/import_lookups_template.sql` in SSMS with SQLCMD mode enabled.  
   - Set `:setvar ROOT` to your repo path (e.g., `D:\\STI\\BOOKING_AGENT\\BOOKINGAGENT`).  
   - Execute to bulk insert all lookup CSVs from `document/RCL Cruises Ltd - API TABLES/`.

3) Auth schema (users/roles)  
   - Run `db/auth_schema.sql` on SQL Server 2019 to create `Users`, `Roles`, `UserRoles` (seed roles included, extra profile fields, indexes). Optional `Permissions` tables are commented out.

Notes
- Files contain quoted fields and commas; the script uses `FORMAT='CSV'` and `FIELDQUOTE='"'`. If your SQL Server build rejects `FORMAT='CSV'`, switch to an `OPENROWSET(BULK ... FORMAT='CSV')` pattern or preprocess the CSVs.
- Keep secrets out of scripts; connection/auth should be supplied via environment/user-secrets in the application layer.
