angular.module("matrix")
.controller("modemsCtrl", function ($scope, $modems, $helper) {
    $scope.modems = [];

    $modems.modemsOfPool($scope.data.id).then(function (modems) {
        for (var i = 0; i < modems.body.modems.length; i++) {
            $scope.modems.push(wrap(modems.body.modems[i]));
        }
        if ($scope.modems.length > 0) {
            $scope.selected = $scope.modems[0];
        }
    });

    $scope.add = function () {
        $helper.createGuid(1).then(function (guidMsg) {
            var guid = guidMsg.body.guids[0];
            var newModem = wrap({
                "class": "modem",
                id: guid,
                comPort: "COM",
                csdPortId: $scope.data.id
            });
            newModem.w.dirty = true;
            $scope.modems.push(newModem);
        });
    }

    $scope.save = function () {
        var modems = [];
        for (var i = 0; i < $scope.modems.length; i++) {
            var modem = $scope.modems[i];
            if (modem.w.dirty) {
                modems.push(unwrap(modem));
            }
        }
        if (modems.length > 0) {
            $modems.save(modems);
        }
    };

    $scope.addCleaner($scope.save);

    var wrap = function (clear) {
        clear.w = {};
        clear.w.dirty = false;
        return clear;
    };

    var unwrap = function (dirty) {
        delete dirty.w;
        return dirty;
    };

    $scope.select = function (modem) {
        $scope.selected = modem;
        $scope.frm.$dirty = modem.w.dirty;
    };
});