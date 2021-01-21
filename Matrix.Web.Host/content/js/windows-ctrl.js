'use strict';

angular.module("app");

app.controller("WindowsCtrl", function ($log, $scope, windowsSvc) {

    var model = {
        windows: windowsSvc.windows
    };

    model.closeAll = function () {
        windowsSvc.closeAll();
    }

    $scope.model = model;

    $scope.$on('$destroy', function () {
        $log.debug("уничтожается windows");
        model.closeAll();
    });
});
