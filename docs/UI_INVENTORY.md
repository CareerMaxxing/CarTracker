# UI_INVENTORY.md

Baseline UI inventory of LubeLogger as cloned, established during Phase 0 reconnaissance
(2026-08-17). Current screens and workflows, with a migration-status column for Phase 2+ to fill in.
Server-rendered Razor + jQuery, Bootstrap 5.3.2, no SPA framework, no `fetch()` usage anywhere —
all UI interaction is jQuery AJAX hitting MVC actions that return partial-view HTML (a **separate**
true JSON API exists under `/api/*`, but the main UI does not consume it — see `API_MAP.md`).

## Screen inventory by area

| Area | Migration status |
|---|---|
| Garage / Home (`Views/Home/Index.cshtml` + `_GarageDisplay`) — vehicle list/grid, tag filter, search, add-vehicle | Not started |
| Vehicle Detail shell (`Views/Vehicle/Index.cshtml`) — tabbed container for the 13 record-type panes below | Not started |
| Dashboard/Reports (`Views/Vehicle/Report/*`) — cost tables/charts, MPG by month, reminder makeup, vehicle image map, history timeline, custom widgets | Not started |
| Planner (`Views/Vehicle/Plan/*`) — Kanban planned-work board + templates | Not started |
| Service Records, Repairs, Upgrades, Fuel, Taxes, Notes (`Views/Vehicle/{Service,Collision,Upgrade,Gas,Tax,Note}/*`) — CRUD list + modal, consistent pattern | Not started |
| Odometer (`Views/Vehicle/Odometer/*`) — history + bulk edit | Not started |
| Supplies/Parts (`Views/Vehicle/Supply/*`) — inventory + shop-wide catalog | Not started |
| Inspections (`Views/Vehicle/Inspection/*`) — templates + dynamic checklist records | Not started |
| Equipment (`Views/Vehicle/Equipment/*`) | Not started |
| Reminders (`Views/Vehicle/Reminder/*`) | Not started |
| Settings (`Views/Home/_Settings.cshtml`) — per-user prefs, theme upload, tab order | Not started |
| Admin Panel (`Views/Admin/*`) — users, tokens, households | Not started |
| Login/Registration/Password Reset (`Views/Login/*`) | Not started |
| Kiosk mode (`Views/Kiosk/*`) — wall-display fleet status | Not started |
| Server Setup (`Views/Home/Setup.cshtml`) — root/admin server config wizard | Not started (ops-facing, low redesign priority) |
| Migration (`Views/Migration/Index.cshtml`) — LiteDB⇄Postgres tool | Not started (ops-facing, low redesign priority) |
| API Explorer (`Views/API/Index.cshtml`, "swigarette") | Not started (dev-facing, low redesign priority) |

## Full file inventory

### Views/Home — Garage, account, server setup
- `Index.cshtml` — Garage (main landing after login). Tabbed shell: Garage / Supplies (if enabled)
  / Calendar (if enabled) / Settings, AJAX-loaded panes. Holds global modals (Add Vehicle, Bulk
  Import, Account Info, Household, Attachment Preview, Collaborators, API Key management).
- `Setup.cshtml` — server settings/initial setup wizard (`serversettings.js`).
- `_GarageDisplay.cshtml` — vehicle-card grid/list, tag filter, search, "Add Vehicle" tile.
- `_Settings.cshtml` — per-user settings panel (color mode, theme picker/upload, language, units,
  tab order/visibility, decimal/date formats, notification prefs).
- `_Calendar.cshtml` / `_ReminderRecordCalendarModal.cshtml` — reminder calendar + detail popup.
- `_WidgetEditor.cshtml` — custom dashboard widget (user-authored HTML/markdown) editor.
- `_AccountModal.cshtml` / `_RootAccountModal.cshtml` — profile editors.
- `_UserHouseholdModal.cshtml` / `_AdminUserHouseholdModal.cshtml` — household management.
- `_CreateApiKeyModal.cshtml` / `_UserApiKeysModal.cshtml` — API key management.
- `_ExtraFields.cshtml` / `_VehicleExtraFields.cshtml` — custom field admin.
- `_VehicleSelector.cshtml` / `_VehicleSelectorOdometer.cshtml` — pickers for cross-vehicle actions.
- `_NotificationServiceConfig.cshtml`, `_Sponsors.cshtml`, `_LocaleSample.cshtml`,
  `_Translations.cshtml`, `_TranslationEditor.cshtml`.

### Views/Vehicle — per-vehicle detail (bulk of the app)
`Index.cshtml` is the tabbed shell: Dashboard, Planner, Odometer, Service Records, Repairs,
Upgrades, Fuel, Supplies, Taxes, Notes, Inspections, Equipment, Reminders, Search. Shared
cross-cutting partials at the root: `_VehicleModal.cshtml`, `_GenericRecordModal.cshtml` (shared
add/edit modal shell reused by several record types), `_FileUploader.cshtml`/`_FilesToUpload.cshtml`/
`_UploadedFiles.cshtml`/`_AttachmentColumn.cshtml` (attachment UI reused everywhere),
`_BulkDataImporter.cshtml`/`_CSVExportParameters.cshtml` (CSV import/export), `_ExtraField.cshtml`/
`_ExtraFieldMultiple.cshtml`, `_GlobalSearchResult.cshtml`, `_RecurringReminderSelector.cshtml`,
`_Stickers.cshtml` (printable QR/asset stickers), `_UserCollaborators.cshtml`.

Sub-folders, one per record type, each `_<Type>Records.cshtml` (list) + `_<Type>RecordModal.cshtml`
(add/edit) following a consistent CRUD pattern: `Service/`, `Gas/`, `Collision/` (Repairs tab),
`Upgrade/`, `Tax/`, `Note/`, `Odometer/`, `Inspection/` (templates + dynamic checklist builder),
`Equipment/`, `Reminder/`, `Plan/` (Kanban board, templates, supply ordering), `Supply/` (inventory
+ shop-wide catalog, usage/requisition history).

`Report/` (the Dashboard tab): `_Report.cshtml` orchestrator, `_ReportHeader.cshtml`/
`_ReportParameters.cshtml`, chart.js-driven `_CostTableReport.cshtml`/`_CostDistanceTableReport.cshtml`/
`_CostMakeUpReport.cshtml`/`_GasCostByMonthReport.cshtml`/`_MPGByMonthReport.cshtml`/
`_ReminderMakeUpReport.cshtml`, `_VehicleHistory.cshtml` (full timeline), `_VehicleImageMap.cshtml`/
`_MapSearchResult.cshtml` (clickable vehicle diagram), `_ReportWidgets.cshtml`, `_Collaborators.cshtml`,
`_ImportModeSelector.cshtml`.

### Views/Admin — Admin Panel (`IsAdmin` role)
`Index.cshtml`, `_Users.cshtml`, `_AdminUserHouseholdModal.cshtml`, `_Tokens.cshtml`.

### Views/Login — auth flows (anonymous)
`Index.cshtml` (login, shows OIDC button if configured), `Registration.cshtml`,
`OpenIDRegistration.cshtml`, `ForgotPassword.cshtml`, `ResetPassword.cshtml`,
`RemoteAuthDebug.cshtml` (OIDC troubleshooting page).

### Views/Kiosk
`Index.cshtml`, `_Kiosk.cshtml`, `_KioskPlan.cshtml`, `_KioskPlanRecordItem.cshtml`,
`_KioskReminder.cshtml`, `_KioskVehicleInfo.cshtml`.

### Views/Migration
`Index.cshtml` — LiteDB⇄Postgres migration tool UI.

### Views/API
`Index.cshtml` — self-hosted API explorer/tester ("swigarette.js"), dev-facing, not part of core UX.

### Views/Files
`_AttachmentPreview.cshtml` — modal partial for previewing an attached file/image/PDF.

### Views/Shared
- `_Layout.cshtml` — the single app shell used by (almost) every page: viewport/PWA meta tags,
  Bootstrap 5.3.2 CSS/JS, bootstrap-datepicker, bootstrap-tagsinput, `site.css`, `loader.css`,
  dynamically-generated `theme.css`, SweetAlert2, jQuery, `shared.js`, `loader.js`. Sections: `Nav`
  (each top-level view supplies its own — **no shared nav partial exists**, strong consolidation
  candidate for the redesign), `@RenderBody()`, `Footer`. Inline JS globals: `getGlobalConfig()`,
  date/currency formatting helpers, `setThemeBasedOnDevice()`.
- `401.cshtml`, `Error.cshtml` — error pages.
- `_ValidationScriptsPartial.cshtml` — jQuery unobtrusive validation includes.
- `_UserColumnPreferences.cshtml` — per-user visible-column picker, reused across record tables.

## Styling approach

- **Framework**: Bootstrap 5.3.2 (`wwwroot/lib/bootstrap`) using its built-in
  `data-bs-theme="dark"/"light"` attribute for dark mode. Plus bootstrap-icons, bootstrap-datepicker,
  bootstrap-tagsinput.
- **Custom CSS**: only `wwwroot/css/site.css` (942 lines — nav, tabs, cards, print styles,
  responsive breakpoints) and `wwwroot/css/loader.css`. No design-token/CSS-custom-property system
  beyond what Bootstrap provides.
- **Theme system**: `Controllers/ThemeController.cs` serves `/css/theme.css` **dynamically** —
  reads the user's `UserConfig.UserTheme` (falls back to server default), loads matching CSS from
  disk. Recently added feature: users/admins can **upload arbitrary `.css` files** as named themes
  (`Home/_Settings.cshtml`, `settings.js`), layered on top of Bootstrap + `site.css`. Distinct from
  the separate dark/light/adaptive toggle.
- **Dark mode**: `UserConfig.UseDarkMode`/`UseSystemColorMode` (Light/Dark/Adaptive), adaptive mode
  uses `window.matchMedia('(prefers-color-scheme: dark)')` inline in `_Layout.cshtml`.

## JS architecture

- One JS file per feature/record-type (`servicerecord.js`, `gasrecord.js`, ..., `vehicle.js` [899
  lines, page orchestration], `garage.js` [894 lines], `settings.js`, `serversettings.js`,
  `login.js`, `kiosk.js`, `swigarette.js`), loaded selectively per view, plain `<script>` tags with
  a version-number cache-buster — no bundler.
- **`wwwroot/js/shared.js`** (2,336 lines) — the common toolkit: date/number formatting, file
  upload handling, CSV export/import, bulk move/delete/duplicate, global search, sticker printing,
  attachment preview, URL/query-param helpers, column preferences.
- **API pattern**: jQuery AJAX exclusively (`$.get`/`$.post`/`$.ajax`) against MVC actions
  returning server-rendered partial HTML injected via `.html(data)`. Zero `fetch()` usage. (The
  real JSON API at `/api/*` exists but the UI doesn't call it.)
- **SweetAlert2** used pervasively for confirms/prompts/toasts instead of native dialogs.
- **Chart.js** (`wwwroot/lib/chart-js`) powers Dashboard/Report charts, loaded only on
  `Vehicle/Index.cshtml`.
- Other libs: jquery, jquery-validation(-unobtrusive), bootstrap-datepicker, bootstrap-tagsinput,
  masonry (Kiosk card grid only), drawdown (markdown→HTML for Notes/Widgets/Kiosk), qrcode
  (stickers), signalr (live updates, gated behind `webSocketEnabled` config).

## Navigation structure

- Canonical tab/record-type set is `Enum/ImportMode.cs` (Dashboard, Planner, Odometer, Service
  Records, Repairs, Upgrades, Fuel, Supplies, Taxes, Notes, Inspections, Equipment, Reminders),
  plus a non-`ImportMode` Search tab appended at the end.
- `appsettings.json`'s `VisibleTabs`/`TabOrder`/`DefaultTab` (`List<ImportMode>`/`ImportMode`) are
  consumed per-user via `UserConfig`. Example server default order: Dashboard, Planner, Odometer,
  Service, Repairs, Upgrades, Fuel, Supplies, Taxes, Notes, Inspections, Equipment, Reminders.
- `Views/Vehicle/Index.cshtml` renders the tab bar **three times**: desktop strip, a duplicated
  "more" overflow dropdown, and a separate full-screen mobile nav list — largely copy-pasted
  markup with a manual responsive technique (4 stacked breakpoints progressively shrinking tab
  labels before collapsing to the dropdown/mobile nav). A strong candidate for a proper responsive
  nav component during Phase 2.
- Each tab pane is an empty shell; content is fetched via AJAX on tab activation, lazy-loaded.
- Home/Garage nav (Garage/Supplies/Calendar/Settings + user dropdown) is a separate, simpler system
  independent of the `ImportMode`/`TabOrder` machinery.
- No client-side router; hash/query param syncing done manually via `URLSearchParams` in `shared.js`.

## Responsive / mobile

- Viewport meta (`width=device-width, initial-scale=1.0`, optionally zoom-disabled per user pref).
- Full PWA meta tag set + `wwwroot/manifest.json` (installable, maskable icons, install-prompt
  screenshots, `display: standalone`).
- `site.css` breakpoints at 768px/1200px min-width, a mobile-specific block at ≤575px governing the
  off-canvas `.lubelogger-mobile-nav` (hamburger-triggered full-panel overlay, separate markup from
  the desktop tab bar — not CSS-only reflow), dedicated `@media print` rules for sticker printing.

## Kiosk mode

`Controllers/KioskController.cs` + `Views/Kiosk/*` — a distinct, simplified, read-mostly display UI
(wall-mounted tablet use case): own minimal shell (no shared app nav), Masonry.js card grid,
drawdown.js markdown, SignalR live refresh. Three explicit modes + a `Cycle` auto-rotate mode
(`Enum/KioskMode.cs`: Vehicle/Plan/Reminder/Cycle). Still behind normal auth (`[Authorize]`), one
of the four path prefixes that accepts API-key auth. Supports vehicle exclusion list via query
param and respects `HideSoldVehicles`/collaborator permissions.

## Recommended grouping for redesign planning (Phase 2+)

1. Auth (Login/Registration/Reset/OpenID)
2. Garage/Home + Settings + Household/Account
3. Vehicle Detail shell + its 13 tabs (each a CRUD workflow; several have sub-features like
   templates/history/usage that deserve their own design pass)
4. Dashboard/Reports (chart-heavy)
5. Admin Panel
6. Kiosk mode
7. Server Setup/Migration/API-explorer (ops/dev-facing, lower redesign priority)
8. Cross-cutting shared primitives — file upload/attachments, CSV import/export, bulk record ops,
   global search, stickers, extra fields, column preferences — currently scattered across
   `Views/Vehicle/_*.cshtml` and `shared.js`; redesign once as reusable components rather than
   per-screen.
