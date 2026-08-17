# PHASE_10 — Documents

## Scope

Per `REQUIREMENTS.md` FR-DOC-01 (Phase 10, deferred from Phase 4): documents must be categorizable
by type (Invoice, MOT, V5C, Insurance, Photograph, Datasheet, Other).

## Task packet

```
TASK ID: PHASE-10-01
TITLE: Document type categorization
OBJECTIVE: Give every uploaded file a category, settable by the user, visible and filterable on the
  Documents tab, with existing/untyped files defaulting safely to Other.
INPUTS: Models/Shared/UploadedFiles.cs (embedded directly - not a separate Export/Input model like
  every record type has - in nearly every record's file list), Views/Vehicle/_UploadedFiles.cshtml
  and _FilesToUpload.cshtml (shared partials included by all 14 record modals), wwwroot/js/
  shared.js's editFileName (existing rename dialog), Views/Vehicle/Documents/_Documents.cshtml
  (Phase 4's cross-record document browser).
ALLOWED SCOPE: DocumentType enum; Type field on UploadedFiles; a way to set it (extending the
  existing rename dialog rather than adding new UI to all 14 modals); visible category badge/icon
  everywhere files are listed; a category filter on the Documents tab; fixing any place that
  silently drops the field when moving/reconstructing UploadedFiles objects.
NON-SCOPE: A dedicated document-management screen beyond the existing Documents tab; CSV/API
  Add-model exposure of Type as a first-class settable field on every record's *ExportModel (Type
  rides along automatically since Files is UploadedFiles directly, not a separate DTO - already
  sufficient); bulk re-categorization tooling for existing attachments.
IMPLEMENTATION REQUIREMENTS:
  - DocumentType: Other(0)/Invoice/MOT/V5C/Insurance/Photograph/Datasheet. Other=0 so existing rows
    without the field deserialize safely (FR-DOC-01's explicit acceptance criterion).
  - UploadedFiles.Type, JSON-serialized as its string name (not the default numeric value) via a
    JsonStringEnumConverter scoped to just this property - UploadedFiles is used directly as the
    wire format in most record Add/Update JSON payloads (unlike the string-typed *ExportModel DTOs
    used for the outer record fields), so it needs to accept "Invoice" in a JSON body, not just a
    number.
  - Extend the existing editFileName rename dialog (shared.js) with a Type <select>, rather than
    adding a new per-file control to all 14 record modals - one shared touchpoint already exists for
    "manage this uploaded file", extending it is far less scope than 14 separate additions.
  - Category badge/icon in _UploadedFiles.cshtml/_FilesToUpload.cshtml (the two shared partials every
    modal includes) and a Category column + filter pills (reusing the existing filterTable/data-tags
    mechanism, not new JS) on the Documents tab.
DELIVERABLES: DocumentType enum, Type field, rename-dialog extension, visible categorization
  everywhere files list, Documents tab filtering by category.
ACCEPTANCE CRITERIA:
  - A file can be tagged with one of the 7 target types via the existing rename dialog.
  - The type survives a full save round-trip (add, and critically, edit) through the actual MVC
    save actions real users hit, not just a raw API call.
  - Existing/untyped files read as Other, no errors, no forced re-categorization.
  - The Documents tab shows a Category column and filter pills, both driven by real data.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against a throwaway vehicle (deleted after, real vehicle confirmed
  untouched): added files with an explicit Type via both JSON API and form-encoded MVC save actions;
  fetched them back; rendered the Documents tab and a record modal's file list HTML.
STOP CONDITION: Acceptance criteria met, verified via curl, changes committed.
```

## What was done

1. Read `REQUIREMENTS.md` FR-DOC-01 and the current `UploadedFiles {Name, Location, IsPending}`
   model, and traced every place it's constructed, rendered, and round-tripped: 14 record modals all
   including two shared partials (`_UploadedFiles.cshtml` for already-saved files,
   `_FilesToUpload.cshtml` for pending-upload files), a shared client-side `uploadedFiles` JS array
   posted wholesale as `files:` on every record save, and the Phase 4 Documents tab.
2. Added `DocumentType` (`Other=0`/Invoice/MOT/V5C/Insurance/Photograph/Datasheet) and
   `UploadedFiles.Type`.
3. Considered adding a new upload-time Type picker to every modal, and rejected it - the codebase
   already has exactly one shared "manage this file" touchpoint (the rename dialog triggered by the
   pencil icon next to every listed file, driven by `editFileName` in `shared.js`), used identically
   by all 14 modals via the two shared partials. Extended that one dialog with a Type `<select>`
   instead of adding new markup to 14 separate views.
4. Hit a real bug immediately when testing via a raw JSON API call: `UploadedFiles` is used directly
   as the wire type for `Files` in most record `*ExportModel`/`*Input` DTOs (unlike the outer record
   fields, which are all lenient strings) - `System.Text.Json` doesn't accept a named string
   ("Invoice") for a plain enum property by default, so `{"type":"Invoice"}` in a request body threw
   a deserialization exception, which (since these controllers don't use `[ApiController]`
   auto-validation) surfaced as a confusing downstream `NullReferenceException` on an unrelated field
   rather than a clear 400. Fixed by scoping a `JsonStringEnumConverter` to just the `Type` property
   (not a global JSON option change, which would've altered every other enum's JSON shape app-wide).
5. Found a second, more serious bug while testing the *actual* browser-facing save path (not just the
   raw API): every one of the 13 controllers' `Save*ToVehicleId` MVC actions has a "move files out of
   temp/ into documents/" step that reconstructs each `UploadedFiles` object explicitly copying only
   `Name` and a freshly-computed `Location` - silently dropping any field not in that list. Since
   `Type` isn't `Name`/`Location`, every single record save (not just edits - first-time saves too)
   would have silently reset every file's category back to `Other` immediately after the user set it,
   making the whole feature non-functional through the primary UI path despite working fine over raw
   API calls. Fixed all 15 occurrences (13 controller files, 2 of them - `InspectionController.cs`,
   `PlanController.cs` - with two occurrences each) by adding `Type = x.Type` to each reconstruction.
6. Added a category badge/icon (`StaticHelper.GetDocumentTypeIcon`, mirroring the existing
   `GetImportModeIcon` pattern) to both shared file-list partials, and extended their embedded
   `uploadedFiles.push(...)` scripts to carry `type` into the client-side array so `editFileName` can
   read a file's current type before showing the rename dialog.
7. Extended the Phase 4 Documents tab (`_Documents.cshtml`): added a "Category" column (distinct from
   the pre-existing "Record Type" column, renamed from "Type" to avoid colliding with the new,
   differently-scoped concept - the old column shows which *record* a file is attached to, e.g.
   Service/Repair; the new one shows the file's own *document type*), and category filter pills
   reusing the existing `filterTable`/`data-tags` mechanism used for tag filtering elsewhere - no new
   JS needed.
8. Verified via curl against a throwaway vehicle (created and deleted after, real vehicle confirmed
   untouched throughout):
   - JSON API add with `"files":[{"type":"Invoice",...}]` round-tripped correctly after the
     JsonStringEnumConverter fix.
   - A file added without a `type` field defaulted to `Other`.
   - Form-encoded save through the actual MVC `SaveOdometerRecordToVehicleId` action (the real save
     path a browser hits) initially silently reset Type to `Other` - reproduced the bug, applied the
     fix across all 13 controllers, then confirmed a Type set via the same form-encoded path (
     `Files[0].Type=Datasheet`) now survives correctly.
   - Rendered the Documents tab HTML and confirmed the Category column, filter pills, and
     `data-tags` attributes all reflect the real per-file types (Invoice/Other/Datasheet all showed
     correctly for the three test files across two different record types).
   - `dotnet build`: 0 errors throughout.

## Result

Complete. `REQUIREMENTS.md` FR-DOC-01 satisfied, including a systemic bug (Type silently dropped on
every MVC save) caught and fixed before it could have made the whole feature non-functional in
normal browser use despite looking correct over the raw API.
