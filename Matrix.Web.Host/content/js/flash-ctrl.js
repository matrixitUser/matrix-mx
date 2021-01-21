angular.module("app")

.controller("FlashCtrl", function ($scope, $transport, $flash, $helper, $uibModalInstance, $timeout, $filter, $log, $q, data) {

    var model = {
        objectIds: data,
        password: "",
        success: false,
        messageForPassword: ""
    };
    
    model.cancelUpload = function (flash) {
        delete flash._file;
    }
  
    model.select = function (flash) {
        model.selected = flash;
    };
    
    model.flash = function () {
        var cmd = model.selected._file.base64;
        $flash.flash(cmd, model.objectIds).then(function () { });
    }

    model.okforpassword = function () {
        $flash.isPassword(model.password).then(function (message) {
            model.success = message.body.success;
            if (message.body.success) {
                model.messageForPassword = "";
            } else {
                model.messageForPassword = "Неправильный пароль";
            }
        });
    }

    model.close = function () {
        $uibModalInstance.close();
    }

    $scope.model = model;
});