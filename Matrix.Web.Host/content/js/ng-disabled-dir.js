angular.module("app")
.directive('ngType', function () {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            attrs.$observe('ngDisabled', function setType(value) {
                attrs.$set('disabled', attrs.ngDisabled? "disabled" : "");
            });
        }
    }
});