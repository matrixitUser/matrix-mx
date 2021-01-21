'use strict';

angular.module("app");

app.controller("ModalCtrl", function FoldersCtrl($scope, $uibModalInstance) {

    var model = {

    }

    /////

    $scope.model = model;

    model.close = function () {
        $uibModalInstance.close();
    };
});

