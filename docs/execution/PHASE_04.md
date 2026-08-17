# PHASE_04 — Vehicle Experience

## Scoping decision

The target spec lists six areas under "Vehicle": Overview, Maintenance, History, Parts, Projects,
Documents. Checked what already exists before planning any work — five of the six already have a
home:

- **Overview** → existing Dashboard tab.
- **Maintenance** → existing Service/Repair/Upgrade/Tax/Fuel/Supply tabs.
- **History** → existing full timeline (`GetVehicleHistory`, inside the Dashboard tab).
- **Projects** → existing Planner tab (`PlanRecord`).
- **Parts** → existing Supplies tab (full split into a real Parts domain is Phase 5's job, not this
  phase's).
- **Documents** → the one real gap. Backend aggregation already existed
  (`GetVehicleAttachments`/`GetAttachmentDataForVehicle`, aggregates every attachment across every
  record type for a vehicle) but was **export-only** — a zip-download button, no in-app browsable
  view.

Given the current 13-tab structure was just carefully consolidated and verified in Phase 2, the
user chose to keep it rather than regroup navigation into the six spec categories (a much larger,
riskier UX change) — see chat history for the explicit decision. This phase therefore scoped down
to: build a proper browsable Documents view, reusing the existing aggregation logic.

## Task packet

```
TASK ID: PHASE-04-01
TITLE: Browsable Documents tab
OBJECTIVE: Give vehicles a proper in-app way to browse every attached file, reusing the existing
  cross-record attachment aggregation rather than duplicating it.
INPUTS: Controllers/Vehicle/ReportController.cs (GetVehicleAttachments), Models/Report/
  GenericReportModel.cs, Helper/StaticHelper.cs (GetImportModeIcon, GetImportModeName,
  GetIconByFileExtension - all pre-existing), Views/Vehicle/Index.cshtml (nav), wwwroot/js/
  vehicle.js (tab wiring).
ALLOWED SCOPE: Extract existing aggregation logic into a reusable method; add one new read-only
  GET action; one new partial view; nav/tab wiring for a 14th tab. No changes to how any other
  tab or the existing export feature works.
NON-SCOPE: Document type categorization (Invoice/MOT/V5C/etc. - that's Phase 10, needs a schema
  change to UploadedFiles); editing/deleting documents from this view (editing happens from the
  owning record's own tab); regrouping the other 13 tabs into the six spec categories.
IMPLEMENTATION REQUIREMENTS:
  - Extract GetVehicleAttachments's aggregation loop into GetAttachmentDataForVehicle(vehicleId,
    exportTabs), called by both the existing export action (unchanged behavior) and the new one.
  - New GetDocumentsByVehicleId(vehicleId) action, CollaboratorFilter-protected like the rest of
    the vehicle detail page, calling the helper with every ImportMode.
  - New Views/Vehicle/Documents/_Documents.cshtml: sorted list, type icon + translated label
    (StaticHelper.GetImportModeIcon/GetImportModeName, both pre-existing), date, filename with
    file-type icon and click-to-preview (openAttachmentPreview, pre-existing), using the
    .ct-empty-state primitive from Phase 2 for the no-documents case (its first real use).
  - New "Documents" tab in all three Vehicle/Index.cshtml nav renderings (desktop/dropdown/
    mobile), positioned like Search: always visible, not gated by VisibleTabs (it isn't a distinct
    record type the way the others are).
  - vehicle.js: load-on-tab-activation wiring matching the existing pattern (getVehicleReport as
    the template), plus pane-clearing on tab-away for consistency with the other tabs.
DELIVERABLES: Working Documents tab, export feature behaviorally unchanged.
ACCEPTANCE CRITERIA:
  - A record with an attachment shows up in the Documents tab with correct type/date/filename/icon
    and a working preview click.
  - A vehicle with no attachments shows the empty state, not an empty table.
  - The pre-existing "Export Attachments" zip button still works identically after the refactor.
  - The new tab appears in all three nav renderings without needing any VisibleTabs change.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via API: add a service record with a Files entry, GET
  /Vehicle/GetDocumentsByVehicleId, confirm it renders; GET a vehicle with no attachments, confirm
  empty state; POST /Vehicle/GetVehicleAttachments with the same vehicle, confirm the zip export
  still succeeds; curl the vehicle page HTML, confirm documents-tab appears exactly 3 times and
  documents-tab-pane exactly once.
STOP CONDITION: Acceptance criteria met, verified end-to-end via API against throwaway vehicles,
  user confirmed live in browser, changes committed.
```

## What was done

1. Confirmed the scoping decision with the user (keep 13 tabs, fill the Documents gap only) rather
   than assuming which interpretation of "Vehicle Experience" was intended.
2. Read `GetVehicleAttachments` in full (`Controllers/Vehicle/ReportController.cs`, ~140 lines,
   aggregates attachments from 11 record types) and extracted its aggregation loop into a private
   `GetAttachmentDataForVehicle(vehicleId, exportTabs)`, kept as the exact same logic so the
   existing export action's behavior is unchanged - verified by re-running the export after the
   refactor and confirming it still zips successfully.
3. Added `GetDocumentsByVehicleId`, calling the same helper with every `ImportMode` value.
4. Found and reused three existing helpers rather than reinventing them:
   `StaticHelper.GetImportModeIcon`, `StaticHelper.GetImportModeName`,
   `StaticHelper.GetIconByFileExtension`, plus the existing `openAttachmentPreview()` JS function
   and `_AttachmentPreview.cshtml` modal (already loaded on every vehicle page).
5. Added the "Documents" tab to all three nav renderings and the tab-pane container in
   `Views/Vehicle/Index.cshtml`, plus `vehicle.js` wiring, following the exact patterns Phase 2's
   consolidation established (this is a direct payoff of that refactor - adding a 14th tab was a
   handful of small, mechanical edits instead of hand-copying markup three times).
6. Verified end-to-end via the API against throwaway vehicles before any user involvement: a
   service record with an attachment renders correctly (icon, translated type label, date,
   clickable filename), a vehicle with zero attachments renders the `.ct-empty-state` primitive
   (its first real adoption since being defined unused in Phase 2), and the pre-existing export
   button still works. Confirmed the new tab appears exactly 3 times (desktop/dropdown/mobile) and
   its pane exactly once in the raw page HTML. User confirmed working live in their browser.

## Result

Complete and user-verified. Phase 4's buildable scope (given the keep-current-tabs decision) is
done. Full navigation regrouping into the six spec categories remains a documented option for a
future pass if ever wanted, not attempted here.
