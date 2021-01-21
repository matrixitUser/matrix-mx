angular.module("app", []).
controller("main", function ($scope, $http) {
    $scope.tasks = [];
    $scope.refresh = function () {
        $http.get("tasks").then(function (response) {
            $scope.tasks = response.data;
        });
    };
    $scope.refresh();

    $scope.stop = function () {
        $http.get("stop");
    };

    $scope.start = function () {
        $http.get("start");
    };

    $scope.restart = function () {
        $http.get("restart");
    };
});