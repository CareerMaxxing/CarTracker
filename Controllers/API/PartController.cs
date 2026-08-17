using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class APIController
    {
        // Part (catalog entity, not vehicle-scoped) - see docs/DATA_MODEL.md Phase 1 notes and
        // docs/execution/PHASE_05.md for the design rationale.
        [HttpGet]
        [Route("/api/vehicle/parts/all")]
        public IActionResult AllParts(MethodParameter parameters)
        {
            var parts = _partDataAccess.GetParts();
            if (parameters.Id != default)
            {
                parts.RemoveAll(x => x.Id != parameters.Id);
            }
            if (!string.IsNullOrWhiteSpace(parameters.Tags))
            {
                var tagsFilter = parameters.Tags.Split(' ').Distinct();
                parts.RemoveAll(x => !x.Tags.Any(y => tagsFilter.Contains(y)));
            }
            var result = parts.Select(x => new PartExportModel { Id = x.Id.ToString(), PartNumber = x.PartNumber, Manufacturer = x.Manufacturer, Description = x.Description, Category = x.Category, Notes = x.Notes, ExtraFields = x.ExtraFields, Files = x.Files, Tags = string.Join(' ', x.Tags) });
            return Json(result);
        }
        [HttpGet]
        [Route("/api/parts")]
        public IActionResult GetPartById(int id)
        {
            if (id == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid part id"));
            }
            var part = _partDataAccess.GetPartById(id);
            if (part == null || part.Id == default)
            {
                Response.StatusCode = 404;
                return Json(OperationResponse.Failed("Part not found"));
            }
            return Json(new PartExportModel { Id = part.Id.ToString(), PartNumber = part.PartNumber, Manufacturer = part.Manufacturer, Description = part.Description, Category = part.Category, Notes = part.Notes, ExtraFields = part.ExtraFields, Files = part.Files, Tags = string.Join(' ', part.Tags) });
        }
        [HttpPost]
        [Route("/api/parts/add")]
        [Consumes("application/json")]
        public IActionResult AddPartJson([FromBody] PartExportModel input) => AddPart(input);
        [HttpPost]
        [Route("/api/parts/add")]
        public IActionResult AddPart(PartExportModel input)
        {
            if (string.IsNullOrWhiteSpace(input.PartNumber) && string.IsNullOrWhiteSpace(input.Description))
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, PartNumber or Description must be provided."));
            }
            if (input.Files == null)
            {
                input.Files = new List<UploadedFiles>();
            }
            if (input.ExtraFields == null)
            {
                input.ExtraFields = new List<ExtraField>();
            }
            try
            {
                var part = new Part()
                {
                    PartNumber = input.PartNumber,
                    Manufacturer = input.Manufacturer,
                    Description = input.Description,
                    Category = input.Category,
                    Notes = input.Notes,
                    ExtraFields = input.ExtraFields,
                    Files = input.Files,
                    Tags = string.IsNullOrWhiteSpace(input.Tags) ? new List<string>() : input.Tags.Split(' ').Distinct().ToList()
                };
                _partDataAccess.SavePart(part);
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic($"Added Part - {part.PartNumber}", "part.add.api", User.Identity?.Name ?? string.Empty, part.Id.ToString()));
                return Json(OperationResponse.Succeed("Part Added", new { partId = part.Id }));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(OperationResponse.Failed(ex.Message));
            }
        }
        [HttpPut]
        [Route("/api/parts/update")]
        [Consumes("application/json")]
        public IActionResult UpdatePartJson([FromBody] PartExportModel input) => UpdatePart(input);
        [HttpPut]
        [Route("/api/parts/update")]
        public IActionResult UpdatePart(PartExportModel input)
        {
            if (!int.TryParse(input.Id, out int partId) || partId == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid part id"));
            }
            var existingPart = _partDataAccess.GetPartById(partId);
            if (existingPart == null || existingPart.Id == default)
            {
                Response.StatusCode = 404;
                return Json(OperationResponse.Failed("Part not found"));
            }
            existingPart.PartNumber = input.PartNumber;
            existingPart.Manufacturer = input.Manufacturer;
            existingPart.Description = input.Description;
            existingPart.Category = input.Category;
            existingPart.Notes = input.Notes;
            existingPart.ExtraFields = input.ExtraFields ?? existingPart.ExtraFields;
            existingPart.Files = input.Files ?? existingPart.Files;
            existingPart.Tags = string.IsNullOrWhiteSpace(input.Tags) ? new List<string>() : input.Tags.Split(' ').Distinct().ToList();
            var result = _partDataAccess.SavePart(existingPart);
            if (result)
            {
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic($"Updated Part - {existingPart.PartNumber}", "part.update.api", User.Identity?.Name ?? string.Empty, existingPart.Id.ToString()));
            }
            return Json(OperationResponse.Conditional(result, "Part Updated", StaticHelper.GenericErrorMessage));
        }
        [HttpDelete]
        [Route("/api/parts/delete")]
        public IActionResult DeletePart(int id)
        {
            if (id == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid part id"));
            }
            var result = _partDataAccess.DeletePartById(id);
            if (result)
            {
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic($"Deleted Part - Id: {id}", "part.delete.api", User.Identity?.Name ?? string.Empty, id.ToString()));
            }
            return Json(OperationResponse.Conditional(result, "Part Deleted", StaticHelper.GenericErrorMessage));
        }

        // PartPurchase (transaction entity, vehicle-scoped like SupplyRecord - VehicleId=0 is shop-wide).
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/vehicle/partpurchases/all")]
        public IActionResult PartPurchases(int vehicleId, MethodParameter parameters)
        {
            if (vehicleId == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid vehicle id"));
            }
            var purchases = _partPurchaseDataAccess.GetPartPurchasesByVehicleId(vehicleId);
            if (parameters.Id != default)
            {
                purchases.RemoveAll(x => x.Id != parameters.Id);
            }
            if (!string.IsNullOrWhiteSpace(parameters.Tags))
            {
                var tagsFilter = parameters.Tags.Split(' ').Distinct();
                purchases.RemoveAll(x => !x.Tags.Any(y => tagsFilter.Contains(y)));
            }
            var result = purchases.Select(x => new PartPurchaseExportModel { Id = x.Id.ToString(), PartId = x.PartId.ToString(), VehicleId = x.VehicleId.ToString(), Date = x.Date.ToShortDateString(), Quantity = x.Quantity.ToString(), Cost = x.Cost.ToString(), Supplier = x.Supplier, Notes = x.Notes, ExtraFields = x.ExtraFields, Files = x.Files, Tags = string.Join(' ', x.Tags) });
            if (_config.GetInvariantApi() || Request.Headers.ContainsKey("culture-invariant"))
            {
                return Json(result, StaticHelper.GetInvariantOption());
            }
            else
            {
                return Json(result);
            }
        }
        [HttpGet]
        [Route("/api/parts/purchases")]
        public IActionResult GetPartPurchasesByPartId(int partId)
        {
            if (partId == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid part id"));
            }
            var purchases = _partPurchaseDataAccess.GetPartPurchasesByPartId(partId);
            //filter out purchases the caller can't see, same as any other vehicle-scoped listing.
            var visibleVehicles = _dataAccess.GetVehicles();
            if (!User.IsInRole(nameof(UserData.IsRootUser)))
            {
                visibleVehicles = _userLogic.FilterUserVehicles(visibleVehicles, GetUserID());
            }
            var visibleVehicleIds = visibleVehicles.Select(x => x.Id).Append(0).ToHashSet();
            purchases.RemoveAll(x => !visibleVehicleIds.Contains(x.VehicleId));
            var result = purchases.Select(x => new PartPurchaseExportModel { Id = x.Id.ToString(), PartId = x.PartId.ToString(), VehicleId = x.VehicleId.ToString(), Date = x.Date.ToShortDateString(), Quantity = x.Quantity.ToString(), Cost = x.Cost.ToString(), Supplier = x.Supplier, Notes = x.Notes, ExtraFields = x.ExtraFields, Files = x.Files, Tags = string.Join(' ', x.Tags) });
            return Json(result);
        }
        [TypeFilter(typeof(QueryParamFilter), Arguments = new object[] { new string[] { "vehicleId" } })]
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [TypeFilter(typeof(CollaboratorFilter), Arguments = new object[] { false, true, HouseholdPermission.Edit })]
        [HttpPost]
        [Route("/api/vehicle/partpurchases/add")]
        [Consumes("application/json")]
        public IActionResult AddPartPurchaseJson(int vehicleId, [FromBody] PartPurchaseExportModel input) => AddPartPurchase(vehicleId, input);
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Edit })]
        [TypeFilter(typeof(CollaboratorFilter), Arguments = new object[] { false, true, HouseholdPermission.Edit })]
        [HttpPost]
        [Route("/api/vehicle/partpurchases/add")]
        public IActionResult AddPartPurchase(int vehicleId, PartPurchaseExportModel input)
        {
            if (vehicleId == default && vehicleId != 0)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid vehicle id"));
            }
            if (!int.TryParse(input.PartId, out int partId) || partId == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, PartId must reference an existing part."));
            }
            var existingPart = _partDataAccess.GetPartById(partId);
            if (existingPart == null || existingPart.Id == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("PartId does not reference an existing part."));
            }
            if (string.IsNullOrWhiteSpace(input.Quantity) || string.IsNullOrWhiteSpace(input.Cost))
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Input object invalid, Quantity and Cost cannot be empty."));
            }
            if (input.Files == null)
            {
                input.Files = new List<UploadedFiles>();
            }
            if (input.ExtraFields == null)
            {
                input.ExtraFields = new List<ExtraField>();
            }
            try
            {
                var quantity = decimal.Parse(input.Quantity);
                var partPurchase = new PartPurchase()
                {
                    PartId = partId,
                    VehicleId = vehicleId,
                    Date = string.IsNullOrWhiteSpace(input.Date) ? DateTime.Now.Date : DateTime.Parse(input.Date),
                    Quantity = quantity,
                    QuantityRemaining = quantity,
                    Cost = decimal.Parse(input.Cost),
                    Supplier = input.Supplier,
                    Notes = string.IsNullOrWhiteSpace(input.Notes) ? "" : input.Notes,
                    ExtraFields = input.ExtraFields,
                    Files = input.Files,
                    Tags = string.IsNullOrWhiteSpace(input.Tags) ? new List<string>() : input.Tags.Split(' ').Distinct().ToList()
                };
                _partPurchaseDataAccess.SavePartPurchaseToVehicle(partPurchase);
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic("Added Part Purchase", "partpurchase.add.api", User.Identity?.Name ?? string.Empty, partPurchase.Id.ToString()));
                return Json(OperationResponse.Succeed("Part Purchase Added", new { recordId = partPurchase.Id }));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(OperationResponse.Failed(ex.Message));
            }
        }
        [TypeFilter(typeof(APIKeyFilter), Arguments = new object[] { HouseholdPermission.Delete })]
        [HttpDelete]
        [Route("/api/vehicle/partpurchases/delete")]
        public IActionResult DeletePartPurchase(int id)
        {
            if (id == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid part purchase id"));
            }
            var existingRecord = _partPurchaseDataAccess.GetPartPurchaseById(id);
            if (existingRecord == null || existingRecord.Id == default)
            {
                Response.StatusCode = 404;
                return Json(OperationResponse.Failed("Part purchase not found"));
            }
            if (!_userLogic.UserCanDirectlyEditVehicle(GetUserID(), existingRecord.VehicleId))
            {
                Response.StatusCode = 401;
                return Json(OperationResponse.Failed("Access Denied, you don't have access to this vehicle."));
            }
            var result = _partPurchaseDataAccess.DeletePartPurchaseById(id);
            if (result)
            {
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic("Deleted Part Purchase", "partpurchase.delete.api", User.Identity?.Name ?? string.Empty, id.ToString()));
            }
            return Json(OperationResponse.Conditional(result, "Part Purchase Deleted", StaticHelper.GenericErrorMessage));
        }
    }
}
