angular.module("matrix")
/**
 * строка поиска
 */
.controller("searchCtrl", function ($listFilter, $scope) {
    $scope.filterText = "";

    var self = this;
    self.lowerText = "";

    $listFilter.add({
        name: "text",
        filter: function (row) {
            if (self.lowerText.length < 3) return true;

            return row.hasOwnProperty("Area") && row.Area.length>0 && row.Area[0].name && row.Area[0].name.toLocaleLowerCase().indexOf(self.lowerText) >= 0;
        }
    });

    var filterDelayTimer;
    $scope.$watch("filterText", function (newValue) {
        clearTimeout(filterDelayTimer);
        filterDelayTimer = setTimeout(function () {
            self.lowerText = $scope.filterText.toLocaleLowerCase();
            $scope.$apply(function () {
                $listFilter.refresh();
            });
        }, 500);
    });
});
