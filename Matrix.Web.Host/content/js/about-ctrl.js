angular.module("app")
.controller("AboutCtrl", function ($uibModalInstance, $scope, $log, $auth) {

    var model = {

    }

    $auth.getSession().then(function (session) {
        model.user = session.user.name;
    }, function (err) {
        model.user = "???";
    });

    model.close = function () {
        $uibModalInstance.close();
    }

    //

    $scope.model = model;
});