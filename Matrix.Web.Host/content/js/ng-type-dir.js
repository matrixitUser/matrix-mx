angular.module("app")
.directive('ngType', function () {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            attrs.$observe('ngType', function setType(value) {
                attrs.$set('type', attrs.ngType);
            });
        }
    }
});