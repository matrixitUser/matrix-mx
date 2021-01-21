angular.module("app")
.controller('RowEditObisesCtrl', function ($scope, $uibModalInstance, data, $filter, metaSvc) {

    var obisContent = metaSvc.rowObises;

    function parseObises(strObises) {
        var obises = [];
        if ((strObises === undefined) || (strObises === "")) {
            return obises;
        }

        var obises_text = strObises.split('|');
        //
        for (var i = 0; i < obises_text.length; i++) {
            var obis = { n: 0 };
            var obis_text = obises_text[i].split(';');
            //
            for (var j = 0; j < obis_text.length; j++) {
                var name = obisContent[j].name;
                obis[name] = obis_text[j];
            }
            //
            if (obis.number) {
                var num = parseInt(obis.number);
                if (!isNaN(num)) {
                    obis.n = num;
                }
            }
            //
            obises.push(obis);
        }
        //
        var sort = $filter('orderBy')(obises, 'n');
        return sort;
    }

    function makeParameters(channels) {
        var parameters = [];
        //
        for (var i = 0; i < channels.length; i++) {
            var channel = channels[i];
            var parameter = [];
            for (var j = 0; j < obisContent.length; j++) {
                var name = obisContent[j].name;
                parameter.push(channel[name] || "");
            }
            parameters.push(parameter.join(';'));
        }
        //
        return parameters.join('|');
    }


    //function 


    var model = {
        channelContent: obisContent,
        lastError: ""
    };

    model.parameters = data.parameters;
    model.isEditable = data.isEditable || false;

    model.obises = parseObises(data.obises);

    model.deleteOBIS = function (index) {
        model.obises.splice(index, 1);
    }

    model.addOBIS= function () {
        var maxn = 0;
        for (var i = 0; i < model.obises.length; i++) {
            var channel = model.obises[i];
            if (channel.n && (maxn < channel.n)) {
                maxn = channel.n;
            }
        }
        model.obises.push({ n: maxn + 1, number: (maxn + 1).toString() });
    }

    var checkChannels = function () {
        for (var j = 0; j < obisContent.length; j++) {
            var parameter = obisContent[j];
            var required = parameter.required || false;
            var unique = parameter.unique || false;

            for (var i = 0; i < model.obises.length; i++) {
                //проверка на ошибки:
                var channel = model.obises[i];
                var p = channel[parameter.name];

                if (p === undefined) {
                    p = "";
                }

                //+не пустота
                if (p === "") {
                    if (required) {
                        model.lastError = "Параметр \"" + parameter.caption + "\" не должен быть пустым";
                        return false;
                    } else if (parameter.init !== undefined) {
                        p = parameter.init;
                    }
                }

                p = p.toString();

                if (p !== "") {
                    //+проверка типов       
                    switch (parameter.type) {
                        case 'float':
                            var num = parseFloat(p.replace(/,/g, '.'));
                            if (isNaN(num)) {
                                model.lastError = "Параметр \"" + parameter.caption + "\" не является числом";
                                return false;
                            } else {
                                p = num.toString().replace(/\./g, ',');
                            }
                            break;

                        case 'integer':
                            var num = parseInt(p);
                            if (isNaN(num)) {
                                model.lastError = "Параметр \"" + parameter.caption + "\" не является целым числом";
                                return false;
                            } else {
                                p = num.toString();
                            }
                            break;
                    }

                    if (p !== channel[parameter.name]) {
                        channel[parameter.name] = p;
                    }
                }
            }

            //-уникальность
            if (unique) {
                for (var i = 0; i < model.obises.length; i++) {
                    var p = model.obises[i][parameter.name];
                    for (var k = i + 1; k < model.obises.length; k++) {
                        var p1 = model.obises[k][parameter.name];
                        if (!!p && p !== "" && p === p1) {
                            model.lastError = "OBIS \"" + parameter.caption + "\" должен быть уникальным";
                            return false;
                        }
                    }
                }
            }
        }
        model.lastError = "";
        return true;
    }

    $scope.ok = function () {
        if (checkChannels()) {
            model.parameters = makeParameters(model.obises);
            $uibModalInstance.close(model.isEditable ? model.parameters : data.parameters);
        }
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});