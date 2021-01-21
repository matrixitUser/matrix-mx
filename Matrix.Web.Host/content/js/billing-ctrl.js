'use strict';

angular.module("app");

app.controller("BillingCardCtrl", function ($uibModalInstance, $scope, $auth, $billing, data, $list, $transport, $helper, metaSvc) {

    if (!data || !data.ids) $uibModalInstance.close();

    var model = {
        field: data.field,
        headerName: data.headerName,
        dataValue: data.valveControlValue,
        objectIds: data.ids != null? data.ids : [],
        objectId: data.id,
        login: $scope.$parent.$$childHead.user.login,
        password: "",
        count: 0,
        comment: "",
        date: null,
        success: false,
        isHaveControls: false,
        overlay: $helper.overlayFunc,
        btnColor: "btn-default",
        messageForPassword: "",
        config: metaSvc.config
    };
    if (model.objectId != null) {
        model.objectIds.push(model.objectId);
    }
    

    model.overlay(
        $list.getRows(model.objectIds).then(function (rows) {
            model.rows = rows;
        })
    );

    model.okBilling = function () {
        $billing.recordSave(model.date, model.count, model.comment, model.objectId).then(function () { });
    }
    
    model.close = function () {
        $uibModalInstance.close();
    }

    $scope.model = model;
});