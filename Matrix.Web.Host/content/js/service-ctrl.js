angular.module("app")
.controller('ServiceCtrl', function ($scope, $uibModalInstance, $uibModal, md5, $transport, $timeout) {

    //function 
    var model = {
        textMessage: ""
    };

    var resetMessageTimeout = null;
    var showMessage = function(msg)
    {
        model.textMessage = msg;
        if (resetMessageTimeout !== null)
        {
            $timeout.cancel(resetMessageTimeout);
        }
        resetMessageTimeout = $timeout(function () {
            model.textMessage = "";
            resetMessageTimeout = null;
        }, 5000);
    }
    
    var performServiceOperation = function (operation) {
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/dialog-with-password-modal.html",
            controller: "DialogWithPasswordCtrl",
            size: "md",
            resolve: {
                data: function () {
                    return { password: "", text: {} };
                }
            }
        });

        modalInstance.result.then(function (answer) {
            var passwordHash = md5.createHash(answer.password || '');
            $transport.send(new Message({ what: "managment-service-operation" }, { password: passwordHash, operation: operation })).then(function (answer) {
                showMessage(answer.body.message);
            }).catch(function (message) {
                showMessage("при отправке запроса произошла ошибка");
            });
        })
    }
    
    var rebuildCache = function () {
        $transport.send(new Message({ what: "managment-rebuild-cache" }, { })).then(function (answer) {
            showMessage(answer.body.message);
        }).catch(function (message) {
            showMessage("при отправке запроса произошла ошибка");
        });
    }
    model.performServiceOperation = performServiceOperation;
    model.rebuildCache = rebuildCache;


    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});