# PHASE_12 — Local Reliability / Offline Hardening

## Scope

Per `REQUIREMENTS.md` NFR-REL-01/02 and the roadmap's "reliable startup, DB integrity, attachment
integrity, backup, restore, export/import, recovery from interrupted operations." NFR-REL-01
specifically calls out extending backup/cleanup coverage to Part/Purchase data introduced in later
phases; NFR-REL-02 (the `/health` endpoint) needs no change.

## Task packet

```
TASK ID: PHASE-12-01
TITLE: Part/PartPurchase reliability gaps + attachment integrity diagnostic
OBJECTIVE: Verify and fix backup/restore, orphaned-record, and unlinked-file coverage for the
  Part/PartPurchase entities added in Phase 5 (after the baseline reliability mechanisms were
  written), and add a read-only diagnostic for the reverse integrity problem (DB records pointing at
  attachment files that no longer exist).
INPUTS: Helper/FileHelper.cs (MakeBackup/RestoreBackup/ClearUnlinkedDocuments), Logic/
  VehicleLogic.cs (GetVehicleDocuments/GetStoreSupplyDocuments/DeleteVehicleRecords),
  Controllers/APIController.cs (CleanUp action).
ALLOWED SCOPE: Auditing MakeBackup/RestoreBackup for Part/PartPurchase coverage; fixing any gap
  found in the unlinked-file cleanup and vehicle-deletion cascade for Part/PartPurchase; a new
  broken-attachment-link diagnostic reusing the same document-enumeration infrastructure.
NON-SCOPE: A general DB-corruption repair tool (NFR-REL-02 explicitly says "no change required" for
  the health check); Postgres-specific backup coverage (NFR-REL-01's acceptance criteria names
  Part/Purchase/Government-adapter data specifically, not a general cross-backend backup audit -
  the existing whole-DB-file-copy approach doesn't select by backend anyway, and Postgres data isn't
  file-based so it was never in scope for this file-based backup mechanism, before or after Car
  Tracker); recovery UI for a failed restore (RestoreBackup already returns a clear boolean and logs
  the exception on failure - a UI-level concern, not a data-integrity one).
IMPLEMENTATION REQUIREMENTS:
  - Audit MakeBackup/RestoreBackup: confirm they whole-file-copy the LiteDB database file rather than
    selecting specific collections, so no code change is needed there for Part/PartPurchase (verify
    via an actual backup/restore round-trip, don't just read the code and assume).
  - Audit VehicleLogic.GetVehicleDocuments (feeds the deep-clean unlinked-file deletion) for
    PartPurchase.Files coverage, and DeleteVehicleRecords for a PartPurchase cascade-delete step -
    fix both if missing.
  - Add a GetPartDocuments-equivalent for the global Part catalog's own Files plus shop-wide
    (VehicleId=0) PartPurchases, mirroring the existing GetStoreSupplyDocuments pattern for
    SupplyRecord.
  - Add IFileHelper.GetBrokenAttachmentLinks(linkedDocuments): reports (never deletes) DB-referenced
    filenames with no corresponding file on disk, reusing the same linked-documents lists already
    being assembled for the unlinked-file cleanup.
DELIVERABLES: Fixed cleanup/cascade-delete coverage for Part/PartPurchase, a new broken-link
  diagnostic surfaced in the existing /api/cleanup?deepClean=true response.
ACCEPTANCE CRITERIA:
  - A PartPurchase's attachment survives a deep-clean unlinked-file sweep.
  - Deleting a vehicle also deletes its PartPurchase records (no orphans left in the DB).
  - A backup, followed by deleting a vehicle, followed by restoring that backup, brings the vehicle
    back exactly as it was.
  - Manually removing a referenced attachment file from disk is detected and reported (not silently
    ignored, not auto-deleted/auto-repaired) by the next deep-clean call.
VALIDATION COMMANDS:
  dotnet build
  dotnet run, then via curl against throwaway vehicles/parts (created and deleted after, real
  vehicle confirmed untouched): reproduced both bugs before fixing (deep-clean would have deleted a
  live PartPurchase attachment; deleting a vehicle left its PartPurchase orphaned), confirmed both
  fixed after; a full backup-delete-restore round trip; a manually-deleted attachment file correctly
  flagged by the new diagnostic.
STOP CONDITION: Acceptance criteria met, verified via curl, changes committed.
```

## What was done

1. Read `REQUIREMENTS.md` NFR-REL-01/02 and the roadmap's broader "DB integrity, attachment
   integrity, backup, restore" framing, and scoped the phase to what's concretely testable and
   named in the acceptance criteria, rather than open-ended hardening (that's explicitly Phase 14's
   job).
2. Audited `FileHelper.MakeBackup`/`RestoreBackup`: both operate on the whole LiteDB database file
   (`File.Copy`/`File.Move` on `StaticHelper.DbName`) rather than selecting specific collections, so
   Part/PartPurchase data was already structurally included in every backup/restore with no code
   change needed there - confirmed with an actual round-trip test rather than trusting the read.
3. Found the real gap by tracing where `MakeBackup`'s data actually gets *used* elsewhere:
   `VehicleLogic.GetVehicleDocuments` (which feeds `/api/cleanup?deepClean=true`'s "delete every
   file not referenced by a real record" sweep) enumerates `.Files` for every record type except
   `PartPurchase` - meaning a live PartPurchase's invoice/receipt attachment would have been silently
   and permanently deleted the next time anyone ran a deep clean. Reproduced this before fixing:
   created a PartPurchase with a real attachment file on disk, ran deep-clean, watched it survive
   only after the fix (would have been deleted before it).
4. Found a second, independent gap in the same audit: `VehicleLogic.DeleteVehicleRecords` (called
   when a vehicle is deleted) cascade-deletes every other record type but never called the
   already-existing `IPartPurchaseDataAccess.DeleteAllPartPurchasesByVehicleId` - meaning deleting a
   vehicle left its PartPurchase rows permanently orphaned in the database, invisible and
   unreachable from the UI (no vehicle left to view them from) but never cleaned up either.
   Reproduced before fixing: deleted a vehicle with a PartPurchase, confirmed the PartPurchase
   survived the delete (querying it directly still returned it); fixed by adding the missing call,
   confirmed the PartPurchase was gone afterward.
5. Added `VehicleLogic.GetPartDocuments()` (global Part catalog's own files + shop-wide,
   `VehicleId=0`, PartPurchases - mirroring the existing `GetStoreSupplyDocuments` pattern for
   `SupplyRecord`) and wired it into the `/api/cleanup` deep-clean sweep alongside the existing
   `GetVehicleDocuments`/`GetStoreSupplyDocuments` calls.
6. Added `IFileHelper.GetBrokenAttachmentLinks(linkedDocuments)`: the reverse check of the existing
   `ClearUnlinkedDocuments` (which deletes files with no DB reference) - reports DB-referenced
   filenames with no corresponding file on disk, without touching the referencing record. Surfaced
   as `broken_attachment_links_found` in `/api/cleanup?deepClean=true`'s response, reusing the exact
   same `vehicleDocuments` list already being assembled for the unlinked-file sweep - no new
   enumeration logic needed, matches the roadmap's "attachment integrity" phrase directly.
7. Verified via curl against throwaway vehicles/parts (created and deleted after, real vehicle
   confirmed untouched throughout):
   - Reproduced and then fixed the unlinked-file-cleanup bug: a PartPurchase attachment survived
     `deepClean=true` after the fix (`unlinked_documents_deleted: 0`).
   - Manually deleted the same file from disk afterward and confirmed the new diagnostic caught it
     (`broken_attachment_links_found: 1`) without crashing or touching the DB record.
   - Reproduced and then fixed the vehicle-deletion cascade bug: confirmed a PartPurchase was fully
     gone (not just hidden) after deleting its vehicle, post-fix.
   - Full round trip: created a vehicle, backed up, deleted the vehicle, restored the backup,
     confirmed the vehicle came back exactly as it was (a curl encoding quirk on my end - not a Car
     Tracker bug - caused one restore attempt to silently no-op via a legitimate early-return path;
     confirmed by retrying with correctly-encoded form data, which succeeded).
   - `dotnet build`: 0 errors throughout.

## Result

Complete. `REQUIREMENTS.md` NFR-REL-01 satisfied: two real, previously-undetected data-loss/orphan
bugs affecting Part/PartPurchase (introduced by Phase 5, never wired into the pre-existing cleanup/
cascade-delete mechanisms) were found and fixed, plus a new attachment-integrity diagnostic.
