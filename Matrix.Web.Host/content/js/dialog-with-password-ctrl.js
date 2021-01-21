angular.module("app")
.controller('DialogWithPasswordCtrl', function ($scope, $uibModalInstance, data, $filter) {

    //function 
    var model = {
        password: ""
    };

    model.password = data.password || "";

    $scope.ok = function () {
        $uibModalInstance.close({ password: model.password });
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});