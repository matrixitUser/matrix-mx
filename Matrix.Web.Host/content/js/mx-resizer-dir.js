/**
 * директива уведомляющая детей о изменении размера контейнера
 * пример использования: <mx-resizer on-resize="обработчик(size)">контент...</mx-resizer>
 */
angular.module("matrix")

.directive("mxResizer", function () {
    return {
        restrict: "E",
        transclude: true,
        scope: {
            onResize: "&"
        },
        link: function ($scope, $element, attrs) {
            $scope.getElementRect = function () {
                return { h: $element.height(), w: $element.width() };
            };

            $scope.$watch($scope.getElementRect, function (newValue, oldValue) {
                if ((oldValue.h - newValue.h === 0) && (oldValue.w - newValue.w === 0)) return;
                $scope.onResize({ size: newValue });
            }, true);
        },
        template: "<div style='top:0px;left:0px;bottom:0px;right:0px' ng-transclude></div>"
    }
});