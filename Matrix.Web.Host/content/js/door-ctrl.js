angular.module("matrix")
.controller("doorCrtl", function ($auth, $scope, $rootScope) {

    $scope.logout = function () {
        $auth.logout();
    };

    $rootScope.$on("auth:authorized", function () {
        $scope.user = $auth.getUser();        
    });
});