'use strict';

angular.module("app");

app.controller("ObjectCardCtrl", function ($uibModalInstance, $scope, data, $list, $transport, $helper, metaSvc) {

    if (!data || !data.id) $uibModalInstance.close();
    
    var model = {
        row: undefined,
        quantity: { current: (5 - 1), hourly: 5, constant: 5, abnormal: 15, gsm: 5 },

        overlay: $helper.overlayFunc,
        overlayEnabled: true,
        overlayText: "",

        config: metaSvc.config
    };

    var matrixId = "undefined";

    var filterHidden = function (target) {
        var res = [];
        if (target && target.length) {//filter           
            for (var i = 0; i < target.length; i++) {
                var obj = target[i];
                if (obj.name && obj.name.search("__") == -1) {
                    res.push(obj);
                }
            }
        }
        return res;
    }    

    model.overlay(
        $list.getRows([data.id]).then(function (rows) {
            var row = rows[0];
            if (row.MatrixConnection && row.MatrixConnection.length > 0) {
                matrixId = row.MatrixConnection[0].id;
            }
            model.row = row;
            return $list.getRowCard(row.id, matrixId).then(function (body) {
                model.currents = filterHidden(body.currents);
                model.days = filterHidden(body.days);
                model.abnormals = body.abnormals;
                model.constants = body.constants;
                model.signal = body.signal;
                model.addr = body.addr;
                model.phone = body.phone;
                model.dev = body.dev;
                model.number = body.number;
                model.address = body.address;

                //Параметры - Связь
                {
                    var c = body.days;
                    for (var i = 0; i < c.length; i++) {
                        var par = c[i];
                        if (par) {
                            switch (par.name) {
                                case "__Gsm":
                                    var sig = { date: par.date, level: par.value };
                                    if (!model.signal) model.signal = [];
                                    model.signal.push(sig);
                                    break;
                            }
                        }
                    }
                }

                if (model.signal && (model.signal.length > 0) && (model.signal[0].level)) {
                    model.signal[0].img = $helper.getSignalImg(model.signal[0].level);
                }
        })

    }));

    model.close = function () {
        $uibModalInstance.close();
    }

    $scope.model = model;
});