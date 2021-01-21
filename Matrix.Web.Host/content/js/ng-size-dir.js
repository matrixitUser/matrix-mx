angular.module("app")
.directive('ngSize', function () {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            attrs.$observe('ngSize', function setSize(value) {
                attrs.$set('size', attrs.ngSize + "px");
            });
        }
    }
});