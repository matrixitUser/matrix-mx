angular.module("matrix")

.directive("mxMenuItem", function ($actions) {
    return {
        //require: "^mxPanelContainer",
        restrict: "E",
        transclude: true,
        replace: true,
        scope: {
            name: "@",
            arg: "=",
            header: "@",
            icon: "@"
        },
        link: function ($scope, element, attrs) {
            var action = $actions.get(attrs.name);
            if (!action) {
                return;
            }
            $scope.header = attrs.header || action.header;
            $scope.icon = attrs.icon || action.icon;
            $scope.click = function () {
                var foo = attrs;
                var bar = $scope.arg;
                action.act(bar);
            }
        },
        templateUrl: "tpls/mx-menu-item.html"
    };
});
