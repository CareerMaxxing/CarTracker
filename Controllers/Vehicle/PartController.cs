using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class VehicleController
    {
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetPartPurchasesByVehicleId(int vehicleId)
        {
            var result = _partPurchaseDataAccess.GetPartPurchasesByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            result = _useDescending ? result.OrderByDescending(x => x.Date).ToList() : result.OrderBy(x => x.Date).ToList();
            var parts = _partDataAccess.GetParts().ToDictionary(x => x.Id);
            var viewModel = result.Select(x => new PartPurchaseListItem
            {
                Purchase = x,
                Part = parts.TryGetValue(x.PartId, out var part) ? part : new Part()
            }).ToList();
            return PartialView("Parts/_PartPurchases", viewModel);
        }
        [HttpGet]
        public IActionResult GetAddPartPurchasePartialView(int vehicleId)
        {
            var viewModel = new PartPurchaseModalViewModel
            {
                Input = new PartPurchaseInput { VehicleId = vehicleId },
                AvailableParts = _partDataAccess.GetParts()
            };
            return PartialView("Parts/_PartPurchaseModal", viewModel);
        }
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetPartPurchaseForEditById(int partPurchaseId)
        {
            var result = _partPurchaseDataAccess.GetPartPurchaseById(partPurchaseId);
            var viewModel = new PartPurchaseModalViewModel
            {
                Input = new PartPurchaseInput
                {
                    Id = result.Id,
                    PartId = result.PartId,
                    VehicleId = result.VehicleId,
                    Date = result.Date.ToShortDateString(),
                    Quantity = result.Quantity,
                    Cost = result.Cost,
                    Supplier = result.Supplier,
                    Notes = result.Notes,
                    Files = result.Files,
                    Tags = result.Tags,
                    ExtraFields = result.ExtraFields
                },
                AvailableParts = _partDataAccess.GetParts()
            };
            return PartialView("Parts/_PartPurchaseModal", viewModel);
        }
        [HttpPost]
        public IActionResult SavePartPurchaseToVehicleId(PartPurchaseInput partPurchase)
        {
            if (partPurchase.VehicleId != default)
            {
                if (!_userLogic.UserCanEditVehicle(GetUserID(), partPurchase.VehicleId, HouseholdPermission.Edit))
                {
                    return Json(OperationResponse.Failed("Access Denied"));
                }
            }
            var isNew = partPurchase.Id == default;
            var convertedRecord = partPurchase.ToPartPurchase();
            if (isNew)
            {
                convertedRecord.QuantityRemaining = convertedRecord.Quantity;
            }
            else
            {
                //preserve QuantityRemaining - editing a purchase must not undo tracked consumption.
                var existingRecord = _partPurchaseDataAccess.GetPartPurchaseById(partPurchase.Id);
                convertedRecord.QuantityRemaining = existingRecord.QuantityRemaining;
                convertedRecord.RequisitionHistory = existingRecord.RequisitionHistory;
            }
            //move files from temp.
            convertedRecord.Files = convertedRecord.Files.Select(x => new UploadedFiles { Name = x.Name, Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/") }).ToList();
            var result = _partPurchaseDataAccess.SavePartPurchaseToVehicle(convertedRecord);
            if (result)
            {
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic(isNew ? "Added Part Purchase" : "Updated Part Purchase", isNew ? "partpurchase.add" : "partpurchase.update", User.Identity?.Name ?? string.Empty, convertedRecord.Id.ToString()));
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }
        [HttpPost]
        public IActionResult DeletePartPurchaseById(int partPurchaseId)
        {
            var existingRecord = _partPurchaseDataAccess.GetPartPurchaseById(partPurchaseId);
            if (existingRecord.VehicleId != default && !_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            var result = _partPurchaseDataAccess.DeletePartPurchaseById(partPurchaseId);
            if (result)
            {
                _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic("Deleted Part Purchase", "partpurchase.delete", User.Identity?.Name ?? string.Empty, partPurchaseId.ToString()));
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        // Part catalog quick-add/edit (used inline from the purchase modal's part picker).
        [HttpGet]
        public IActionResult GetAddPartPartialView()
        {
            return PartialView("Parts/_PartModal", new Part());
        }
        [HttpPost]
        public IActionResult SavePart(Part part)
        {
            var isNew = part.Id == default;
            var result = _partDataAccess.SavePart(part);
            if (!result)
            {
                return Json(OperationResponse.Failed(StaticHelper.GenericErrorMessage));
            }
            _eventLogic.PublishEvent(GetUserID(), WebHookPayload.Generic(isNew ? "Added Part" : "Updated Part", isNew ? "part.add" : "part.update", User.Identity?.Name ?? string.Empty, part.Id.ToString()));
            return Json(OperationResponse.Succeed(string.Empty, new { partId = part.Id, partNumber = part.PartNumber, description = part.Description }));
        }
    }
}
