angular.module("matrix")

.directive("mxWaiting", function ($log) {
    return {
        restrict: "E",
        transclude: true,
        scope: {
            waitFor: "=",
        },
        replace: true,
        controller: function ($scope) {
            $scope.$watch("waitFor", function (value) {
                for (var i = 0; i < self.subscribers.length; i++) {
                    self.subscribers[i](value);
                }
            });
            var self = this;
            self.subscribers = [];
            self.subscribeToWaitChanged = function (subscriber) {
                self.subscribers.push(subscriber);
            }
        },
        templateUrl: "/tpls/mx-waiting.html"
    };
})

.directive("mxWaitingContent", function ($log) {
    return {
        require: "^mxWaiting",
        restrict: "E",
        transclude: true,
        replace: true,
        scope: {},
        link: function ($scope, element, attrs, ctrl) {
            ctrl.subscribeToWaitChanged(function (value) {
                $scope.display = value;
            });
        },
        templateUrl: "/tpls/mx-waiting-content.html"
    };
})

.directive("mxWaitingGif", function () {
    return {
        require: "^mxWaiting",
        restrict: "E",
        transclude: true,
        replace: true,
        scope: {},
        link: function ($scope, element, attrs, ctrl) {
            ctrl.subscribeToWaitChanged(function (value) {
                $scope.display = !value;
            });
        },
        templateUrl: "/tpls/mx-waiting-gif.html"
    };
});
