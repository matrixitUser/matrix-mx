angular.module("app", [/*"ui.dashboard"*/]).
controller("main", function ($scope, $http, $log) {
    //$.connection.hub.url = "http://localhost:9001/signalr";
    //var chat = $.connection.notifyHub;
    //chat.client.foo = function (haa) {
    //    $log.debug("server say %s", haa);
    //};
    //chat.server.send("foo");
    //$log.debug("signalr proxy has been started %s", chat);

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