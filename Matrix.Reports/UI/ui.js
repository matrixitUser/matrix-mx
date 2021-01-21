angular.module("app", []).
controller("main", function ($scope, $http) {
    $scope.sessions = [];
    $scope.refresh = function () {
        $http.get("sessions").then(function (response) {
            $scope.sessions = response.data;
        });
    };
    $scope.refresh();
});