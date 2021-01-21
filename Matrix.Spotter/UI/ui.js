angular.module("app", ["formstamp"]).
controller("main", function ($scope, $http, $log, $filter) {

    $scope.objects = [];

    var now = new Date();
    now.setDate(now.getDate() - 1);
    $scope.start = now;
    $scope.end = new Date(2017, 00, 01, 00, 00, 00);

    $scope.object = undefined;
    $scope.select = function (obj) {
        $scope.object = obj;
        $scope.update(obj);
    };

    function dateToString(date) {
        var str = date.getDate() + "-" + (date.getMonth() + 1) + "-" + date.getFullYear();
        str = $filter("date")(date, "dd-MM-yyyy");
        return str;        
    };

    $scope.update = function (obj) {
        $http.get("/data/" + obj.id + "/" + dateToString($scope.start) + "/" + dateToString($scope.end)).then(function (response) {
            obj.rows = response.data;
        });
    };

    $http.get("/objects").then(function (response) {
        $scope.objects = response.data;
    });
});