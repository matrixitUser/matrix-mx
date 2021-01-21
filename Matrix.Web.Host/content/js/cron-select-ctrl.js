angular.module("app")
.controller('CronSelectCtrl', function ($scope, $uibModalInstance, $window, $list, $log, $helper, $auth, data, $transport, $rootScope) {
    
    var model = {
        isLoading: true,
        counting: 0,
        count: 0,
        //filterText: "",       //!
        //
        selecteds: []           //!
    };
    
    
    model.cron = data.cron;
    model.cronconfig = data.cronconfig;
    model.isEditable = data.isEditable || false;

    
    $scope.ok = function () {
        $uibModalInstance.close(model.isEditable? model.cron : data.cron);
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});