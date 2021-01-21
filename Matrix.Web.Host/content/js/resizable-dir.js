angular.module("app");

app.directive('resizable', function ($window) {
    return function ($scope) {
        $scope.initializeWindowSize = function () {
            $scope.windowHeight = $window.innerHeight;
            $scope.windowWidth = $window.innerWidth;
        };
        angular.element($window).bind("resize", function () {
            $scope.initializeWindowSize();
            $scope.$apply();
        });
        $scope.initializeWindowSize();
    }
});