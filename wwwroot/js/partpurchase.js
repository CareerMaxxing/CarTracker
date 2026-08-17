function getVehiclePartPurchases(vehicleId) {
    $.get(`/Vehicle/GetPartPurchasesByVehicleId?vehicleId=${vehicleId}`, function (data) {
        if (data) {
            $("#parts-tab-pane").html(data);
        }
    });
}
function showAddPartPurchaseModal() {
    var vehicleId = GetVehicleId().vehicleId;
    $.get(`/Vehicle/GetAddPartPurchasePartialView?vehicleId=${vehicleId}`, function (data) {
        if (data) {
            $("#partPurchaseModalContent").html(data);
            initDatePicker($('#partPurchaseDate'));
            initTagSelector($("#partPurchaseTag"));
            $('#partPurchaseModal').modal('show');
        }
    });
}
function showEditPartPurchaseModal(partPurchaseId) {
    $.get(`/Vehicle/GetPartPurchaseForEditById?partPurchaseId=${partPurchaseId}`, function (data) {
        if (data) {
            $("#partPurchaseModalContent").html(data);
            initDatePicker($('#partPurchaseDate'));
            initTagSelector($("#partPurchaseTag"));
            $('#partPurchaseModal').modal('show');
        }
    });
}
function hideAddPartPurchaseModal() {
    $('#partPurchaseModal').modal('hide');
}
function deletePartPurchase(partPurchaseId) {
    $("#workAroundInput").show();
    confirmDelete("Deleted Part Purchases cannot be restored.", (result) => {
        if (result.isConfirmed) {
            $.post(`/Vehicle/DeletePartPurchaseById?partPurchaseId=${partPurchaseId}`, function (data) {
                if (data.success) {
                    hideAddPartPurchaseModal();
                    successToast("Part Purchase Deleted");
                    getVehiclePartPurchases(GetVehicleId().vehicleId);
                } else {
                    errorToast(data.message);
                    $("#workAroundInput").hide();
                }
            });
        } else {
            $("#workAroundInput").hide();
        }
    });
}
function savePartPurchaseToVehicle(isNew) {
    var formValues = getAndValidatePartPurchaseValues();
    if (formValues.hasError) {
        errorToast("Please check the form data");
        return;
    }
    $.post('/Vehicle/SavePartPurchaseToVehicleId', { partPurchase: formValues }, function (data) {
        if (data.success) {
            successToast(isNew ? "Part Purchase Added." : "Part Purchase Updated");
            hideAddPartPurchaseModal();
            getVehiclePartPurchases(formValues.vehicleId);
        } else {
            errorToast(data.message);
        }
    });
}
function getAndValidatePartPurchaseValues() {
    var partId = $("#partPurchasePartId").val();
    var date = $("#partPurchaseDate").val();
    var supplier = $("#partPurchaseSupplier").val();
    var quantity = $("#partPurchaseQuantity").val();
    var cost = $("#partPurchaseCost").val();
    var notes = $("#partPurchaseNotes").val();
    var tags = $("#partPurchaseTag").val();
    var modelData = getPartPurchaseModelData();
    var hasError = false;
    if (!partId) {
        hasError = true;
        $("#partPurchasePartId").addClass("is-invalid");
    } else {
        $("#partPurchasePartId").removeClass("is-invalid");
    }
    if (date.trim() == '') {
        hasError = true;
        $("#partPurchaseDate").addClass("is-invalid");
    } else {
        $("#partPurchaseDate").removeClass("is-invalid");
    }
    if (quantity.trim() == '' || !isValidMoney(quantity) || globalParseFloat(quantity) <= 0) {
        hasError = true;
        $("#partPurchaseQuantity").addClass("is-invalid");
    } else {
        $("#partPurchaseQuantity").removeClass("is-invalid");
    }
    if (cost.trim() == '' || !isValidMoney(cost)) {
        hasError = true;
        $("#partPurchaseCost").addClass("is-invalid");
    } else {
        $("#partPurchaseCost").removeClass("is-invalid");
    }
    return {
        id: modelData.id,
        vehicleId: modelData.vehicleId,
        hasError: hasError,
        partId: partId,
        date: date,
        supplier: supplier,
        quantity: quantity,
        cost: cost,
        notes: notes,
        tags: tags,
        files: uploadedFiles
    };
}
function showAddPartModal() {
    $.get('/Vehicle/GetAddPartPartialView', function (data) {
        if (data) {
            $("#partModalContent").html(data);
            $('#partModal').modal('show');
        }
    });
}
function hideAddPartModal() {
    $('#partModal').modal('hide');
}
function savePart() {
    var partNumber = $("#partPartNumber").val();
    var description = $("#partDescription").val();
    if (partNumber.trim() == '' && description.trim() == '') {
        errorToast("Please provide a part number or description");
        return;
    }
    var formValues = {
        id: getPartModelData().id,
        partNumber: partNumber,
        manufacturer: $("#partManufacturer").val(),
        description: description,
        category: $("#partCategory").val(),
        notes: $("#partNotes").val()
    };
    $.post('/Vehicle/SavePart', formValues, function (data) {
        if (data.success) {
            successToast("Part Added");
            var newOption = new Option(`${formValues.partNumber} - ${formValues.description}`, data.additionalData.partId, true, true);
            $('#partPurchasePartId').append(newOption).trigger('change');
            hideAddPartModal();
        } else {
            errorToast(data.message);
        }
    });
}
