# Nexus Communication System - merged project (Members 1-4)

ASP.NET Core MVC (.NET 10) + EF Core + SQL Server. One project, one DbContext, one Program.cs, one database.
Member 5 (Search / Reports) is included - see "Member 5" below.

## Run
1. Open `NexusCommunicationSystem.sln` in Visual Studio, press F5 (internet needed once for NuGet restore).
2. The database `NexusCommunicationSystemDb` and all tables are created automatically on first run
   (LocalDB first, then SQLEXPRESS / localhost - see `appsettings.json`).
3. Start page `/` lists the five portals.
4. **After updating from an older zip, delete the old database `NexusCommunicationSystemDb` in SSMS first** (new columns for employee logins and retail orders), then press F5.

| Portal | URL | Login |
|---|---|---|
| Customer | /Customer/Login | register a new account |
| Admin | /Admin/Login | admin / Admin@123 |
| Technical | /Technical/Login | technical / Tech@123 |
| Accounts | /Accounts/Login | accounts / Acc@123 |
| Retail Outlet | /Retail/Login | the Admin first adds a Retail Shop and a Retail employee (Admin > Retail Shops / Employees) |

All five use one login cookie with a role (Admin / Customer / Technical / Accounts / Retail), so logging in to a second
portal in the same browser logs you out of the first. Use another browser or a private window to test two at once.

## Migrations
No migration files are shipped (they could not be generated without the .NET SDK). On startup the tables are
created from the models (`EnsureCreated`). When you want real migrations:
1. Drop the database `NexusCommunicationSystemDb` in SSMS.
2. Package Manager Console: `Add-Migration InitialCreate`, then run the app (Program.cs applies migrations automatically
   as soon as a migration exists).

## What was unified (conflicts between members)
- **Namespace / project**: everything is `NexusCommunicationSystem`. Members 3 and 4 had their own names and Program.cs.
- **Customer**: Member 2's class (Member 4 had a copy of it).
- **Plan**: Member 1's `Plans` table replaces Member 2's hard-coded plan list. Two fields were added to it:
  `ConnectionType` (Dial-Up / Broadband / Landline) and `Speed`.
  `PlanType` stays Prepaid / Postpaid. The 12 plans from Member 2 are seeded on first run.
- **Vendor**: Member 1's class (Member 3's had `CompanyName`; it was only a stub for the equipment link).
- **Equipment**: Member 1's class plus Member 3's `Location` and `PurchaseDate`. Member 3's `Category` is now `EquipmentType`,
  status "Assigned" is now "In Use". Assigning equipment takes 1 unit from `Quantity`; the last unit flips the item to "In Use".
- **Connection**: one class. Member 2's names (`Id`, `ConnectionNumber`, `ActivatedAt`) plus Member 3/4's `OrderId`, `PlanId`,
  `DeactivatedAt`, `Remarks`. Status is Pending / Active / Temporarily Inactive / Permanently Inactive.
- **Order**: Member 2's class; now also stores `PlanId`, `Speed`, `InstallAddress`, `City`.
- **Bill**: now has real foreign keys to Customer and Connection (Generate Bill checks that the customer owns the connection).
- Seeded: Plans (above) and the four bulk-discount slabs from the specification (25 / 50 / 75 / 100 %).

## How the modules connect now
1. Customer applies and picks a plan from the Admin's Plans -> an **Order** is created.
2. Technical sees every real order in *Work Orders Queue*, saves a feasibility check. Dial-Up needs landline + internet (only internet if the customer already has our
   active landline), Broadband needs internet, Landline needs the telephone line; server availability is always checked.
   The result (Feasible / Rejected) is worked out from the answers; Rejected also marks the customer's order as Rejected.
3. Technical creates the **Connection** from a Feasible order (customer, plan, address and charges are copied from the order),
   assigns equipment, and activates it (the order becomes Completed). Deactivate makes it Inactive.
4. Accounts generates **Bills** for a connection, applies discounts, records payments.
5. Customer sees Orders, Connections, the first invoice (installation + first month, payable online) and the monthly bills from Accounts.

Behaviour changes versus the standalone modules (please review):
- The customer's order no longer creates a connection and paying the first invoice no longer activates it;
  the Technical team does both (as the specification says).
- Technical *Work Orders* now lists real customer orders; the work-order number typed by hand is gone (dropdowns instead).
- Accounts got a login page (the work-division list had one but the zip did not).
- Admin cannot delete equipment that is assigned to a connection.
- The old `Database/SampleData.sql` of Member 2 was not carried over (it targets the old table layout).
- Member 2's zip contains an old `NexusCommunicationSystem_Member5_NET10.zip`; it was ignored.

## UI (Stitch design)
The whole front end follows the Stitch "Nexus Communication System" design (`DESIGN.md` + the 5 screens).
Only views, layouts, CSS and JS were changed - controllers, models, database and routes are untouched.

| Piece | File |
|---|---|
| Design tokens + shared components (top bar, side rail, KPI tiles, tables, status chips, forms, login) | `wwwroot/css/nexus-base.css` |
| Shared shell behaviour (mobile menu, collapse rail, top-bar search) | `wwwroot/js/nexus.js` |
| Split-screen login used by all 4 portals + customer register | `Views/Shared/_AuthLayout.cshtml`, `_PersonaSwitcher.cshtml` |
| Admin / Customer / Accounts layouts | `Views/Shared/_AdminLayout.cshtml`, `_CustomerLayout.cshtml`, `_AccountsLayout.cshtml` |
| Technical layout | `Views/Technical/_TechnicalLayout.cshtml` + `wwwroot/css/technical.css` |
| Portal-specific tweaks | `admin.css`, `customer.css`, `accounts.css` |

Fonts (Inter, JetBrains Mono) and icons (Material Symbols) load from Google Fonts, so the browser needs internet once.
Bootstrap is no longer used by the Accounts portal.

## Member 5 - Search & Reports (merged)
Admin only (`/Search`), linked from the Admin sidebar under "Search & Reports". It is read-only and adds **no new tables**.
- Pages: Global Search, Search Order, Search Connection, Advanced Search, Order Status, Connection Status, and the Customer / Order / Billing / Payment / Feedback reports (quick filter, CSV export, print).
- Member 5's own `Entities.cs`, `DbSeeder.cs` (sample customers such as Ali Khan / Sara Ahmed), Migrations, Program.cs, csproj, setup.bat and `search.css` were **not** carried over - his queries were rewritten for the shared models:
  Customer `Id`/`CreatedAt`, Order `OrderNumber`/`ConnectionType`/`PlanName`/`TotalAmount`, Connection `ConnectionNumber`/`CreatedAt`/`ActivatedAt`,
  Bill `BillDate`/`TotalAmount`/`Status` (Paid / Partial / Unpaid), Payment `AmountPaid`/`PaymentMethod`, Feedback `CreatedAt`/`Subject`/`Message`.
- Order / connection lookups now use the order number / connection number instead of a numeric id.
- Orders: Processing / Completed / Rejected. Connections: Pending / Active / Inactive. (Member 5 assumed Pending / Cancelled / Suspended, which do not exist.)
- Files: `Controllers/SearchController.cs`, `Models/SearchViewModels.cs`, `Views/Search/*`, `wwwroot/js/search.js`.

## Specification fixes (round 2)
**IMPORTANT: delete the old database `NexusCommunicationSystemDb` in SSMS before running** (columns and seed data changed).

- **Connection types** are now exactly the three of the specification: Dial-Up, Broadband, Landline (telephone only).
- **Plans** are the plans of the specification with the prices in $ (Dial-Up hourly / 28K / 56K, Broadband hourly / 64K / 128K,
  Landline local + STD). "STD Yearly" has no price in the specification, so it is seeded as inactive with price 0:
  set the price in Admin > Plans and tick Active. All screens now show $.
- **Order ID** = 11 characters: type letter + 10-digit serial, e.g. `D0000000001` (D = Dial-Up, B = Broadband, T = telephone only).
  The serial runs separately for every letter. Generated in `Models/NexusIds.cs`.
- **Account ID** (stored in `Connection.ConnectionNumber`) = 16 characters: type letter + 3-digit city code + 12-digit serial,
  e.g. `B101000000000001`. Created when the Technical team creates the connection. City codes are in `PlanCatalog.CityCodes`
  (101 = Karachi ... 111 = Gujranwala).
- **Security deposit** per type (Dial-Up $325, Broadband $500, Landline $250) replaces the old installation fee on the customer's first invoice.
- **Inactive connections**: Technical > Connections > "Make Inactive" asks for Temporarily or Permanently inactive (plus a reason).
  Temporarily inactive can be re-activated; permanently inactive is final. Search filters know the new statuses.

Still open from the specification: retail-employee login, employee user credentials, 12.24% service tax,
automatic bill calculation and bulk-discount rules, dial-up/landline feasibility logic, vendor purchase list, customer-side search by id,
due amount and contact number in search.

## Employee logins and the Retail portal
- Every employee has his own **User ID, password (stored as a hash) and Role** (Admin / Technical / Accounts / Retail).
  The Admin sets them under *Admin > Employees* (leave the password empty when editing to keep the old one).
  An employee can only log in to the portal of his role, and only while he is *Active*.
- The fixed logins of `appsettings.json` (admin, technical, accounts) still work as built-in accounts. Retail has no fixed login.
- A Retail employee belongs to a **Retail Shop**. In *Retail* he registers customers, places orders for them (Order ID `D/B/T...`),
  and tracks the orders of his shop: feasibility status, Account ID, connections, bills and payments (read only).
  Orders placed this way store the employee (`Orders.PlacedByEmployeeId`).
- Technical feasibility checks now store the logged-in employee's name in *Checked By*.
- An employee who placed orders cannot be deleted, and a retail shop with employees cannot be deleted (untick Active instead).

## Billing rules (Accounts)
- *Generate Bill* needs only Customer Id, Connection Id and month. It adds the plan charges, the equipment assigned to the connection
  and the refundable security deposit (Dial-Up $325, Broadband $500, Landline $250); equipment and deposit only on the first bill
  (the deposit is skipped when the customer already paid the order invoice).
- Bulk discount by the customer's number of connections: 10-15 -> 25%, 16-25 -> 50%, 26-50 -> 75%, more than 50 -> 100%,
  taken on charges + security deposit. *Apply Discount* is still there to change it by hand.
- 12.24% service tax on the whole bill (charges + deposit - discount).
- No sample data is seeded: only the plans and discount slabs of the specification.

## Purchase list (Admin)
- Admin > **Purchase List**: order equipment from a vendor, optionally for a waiting customer order. The page also shows how many customer orders wait for a connection, the equipment in stock and the quantity already on order.
- **Mark received** adds the quantity to the equipment stock automatically (same item + vendor = quantity is increased, otherwise a new stock line). Cancel is possible while the purchase is still "Ordered".
- Files: `Models/PurchaseOrder.cs`, `AdminController` (PurchaseList, AddPurchase, ReceivePurchase, CancelPurchase), `Views/Admin/PurchaseList.cshtml`, `AddPurchase.cshtml`.

## Track Status (Customer and Retail)
- Customer portal and Retail portal both have **Track Status**: type an Order ID or Account ID to see the order status, feasibility, account ID, connection status, amount due and the customer's contact number.
- A customer finds only his own orders; a retail employee only the orders of his outlet.
- Files: `Models/StatusLookup.cs`, `Views/Shared/_StatusLookup.cshtml`, `Views/Customer/TrackStatus.cshtml`, `Views/Retail/TrackStatus.cshtml`.

## Last round
- Admin search: Account ID / Order ID lookups show the amount due and the contact number; Advanced Search has Contact number and Connection type filters.
- Plans are Postpaid only (the Prepaid option is gone from the plan forms).
- Bills carry forward the unpaid balance and the payments of the earlier bills of the same connection (Previous Balance, Previous Payments, Total Payable).
- Short comments were added above every action, model and helper.

## Invoice tax and landing page
- The first invoice of an order (plan + refundable deposit) now carries the 12.24% service tax (`Order.ServiceTax`), for both Customer and Retail orders.
- The landing page shows the real active plans from the Admin (Dial-Up, Broadband, Landline), with their real prices. The fake fiber marketing, telemetry numbers, hotline and certifications were removed.

## Payments and last fixes (round 4)
- **Retail records payments** (specification: payments are maintained by the retail outlet and Accounts):
  *Retail > Payments > Record payment* saves a payment against a bill of the outlet (amount must be more than 0 and not more than the balance;
  the bill becomes Partial / Paid). On an order, *Receive payment* marks the first invoice (plan + deposit + tax) as paid.
- **Accounts > Order Invoices** lists every unpaid first invoice with a *Mark paid* button. Accounts > Add Payment now also rejects 0 and over-payments.
- Paying the first invoice matters: Generate Bill skips the security deposit when the order invoice is already Paid.
- Customer and Retail plan pages now show the 12.24% service tax in the total, so the screen matches the invoice.
- Feasibility form: *Server available* now starts on "Yes" (it used to start on "No" and could reject an order by mistake).
- Not changed on purpose: the Customer portal still lets a customer order online (the specification mentions both online ordering and
  "only retail employees place orders"). Remove the customer's *Apply Connection* link if your teacher wants retail-only ordering.
- Open question: the first invoice already includes the plan price of the first month; the first monthly bill charges the plan again.
  If the first invoice is meant to be an advance, deduct it on the first bill.
