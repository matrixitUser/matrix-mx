'use strict';

angular.module("app");

app.controller("ValveControlCardCtrl", function ($uibModalInstance, $scope, $auth, $valve, data, $list, $transport, $helper, metaSvc) {

    if (!data || !data.ids) $uibModalInstance.close();

    var model = {
        field: data.field,
        headerName: data.headerName,
        dataValue: data.valveControlValue,
        objectIds: data.ids != null? data.ids : [],
        objectId: data.id,
        login: $scope.$parent.$$childHead.user.login,
        password: "",

        success: false,
        isHaveControls: false,
        overlay: $helper.overlayFunc,
        overlayEnabled: true,
        overlayText: "",
        control: data.control != null? data.control: "Опрос",
        btnColor: "btn-default",
        messageForPassword: "",
        config: metaSvc.config
    };
    if (model.objectId != null) {
        model.objectIds.push(model.objectId);
    }
    if (model.dataValue != null) {
        switch (model.dataValue) {
            case "Открыто":
                model.control = "Закрыть";
                model.isHaveControls = true;
                break;
            case "Закрыто":
                model.control = "Открыть";
                model.isHaveControls = true;
                break;
        }
    }
    switch (model.control) {
        case "Закрыть":
            model.btnColor = "btn-danger";
            break;
        case "Открыть":
            model.btnColor = "btn-success";
            break;
    }

    var matrixId = "undefined";

    model.overlay(
        $list.getRows(model.objectIds).then(function (rows) {
            model.rows = rows;
        })
    );

    model.okforpassword = function () {
        $valve.isPassword(model.login, model.password).then(function (message) { //$auth.verification
            if (message.body.success) {
                model.messageForPassword = "";
                model.success = true;
            } else {
                model.messageForPassword = "Неправильный пароль";
                model.success = false;
            }
        });
    }
    model.poll = function () {
        $valve.valveControl(model.field, model.control, model.objectIds).then(function () { });
    }
    model.openValve = function () {
        $valve.valveControl(model.field, "открыть", model.objectIds).then(function () { });
    }
    model.closeValve = function () {
        $valve.valveControl(model.field, "закрыть", model.objectIds).then(function () { });
    }
    model.close = function () {
        $uibModalInstance.close();
    }

    $scope.model = model;
});