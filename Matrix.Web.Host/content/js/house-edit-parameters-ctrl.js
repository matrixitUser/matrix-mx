angular.module("app")
.controller('HouseEditParametersCtrl', function ($scope, $uibModalInstance, data, $filter) {

    var houseContent = [{
        name: "entrance",
        caption: "Номер подъезда",
        type: "integer",
        order: 1,
        unique: false,
        required: true
    }, {
        name: "floor",
        caption: "Номер этажа",
        type: "integer",
        order: 2,
        unique: false,
        required: true
    }, {
        name: "apt",
        caption: "Номер квартиры",
        type: "string",
        order: 3,
        unique: true,
        required: true
    }, {
        name: "waterCnts",
        caption: "Кол-во стояков (ХВС/ГВС)",
        type: "integer",
        order: 5,
        init: 0,
        required: true
    }, {
        name: "square",
        caption: "Площадь квартиры",
        type: "float",
        order: 6
    }, {
        name: "unknown",
        caption: "Доп.поле",
        type: "string",
        hidden: true
    }, {
        name: "aptView",
        caption: "Псевдоним квартиры",
        type: "string",
        order: 4,
        unique: true
    }, {
        name: "lsnumber",
        caption: "Номер ЛС",
        type: "string"
    }, {
        name: "lsnumber1",
        caption: "Стороний ЛС",
        type: "string"
    }, {
        name: "fio",
        caption: "ФИО",
        type: "string"
    }, {
        name: "cntType",
        caption: "Тип счетчика",
        type: "string"
    }, {
        name: "snumber",
        caption: "Заводской номер счётчика",
        type: "string"
    }, {
        name: "service",
        caption: "Услуга",
        type: "string"
    }, {
        name: "tarif",
        caption: "Тарифность",
        type: "string"
    }, {
        name: "zone",
        caption: "Зона суток",
        type: "string"
    }, {
        name: "comment",
        caption: "Примечание",
        type: "string"
    }];

    function parseApts(parameters) {
        var apts = [];
        if ((parameters === undefined) || (parameters === "")) {
            return apts;
        }

        var apts_text = parameters.split('|');
        //
        for (var i = 0; i < apts_text.length; i++) {
            var apt = { n: 0 };
            var apt_text = apts_text[i].split(':');
            //
            for (var j = 0; j < apt_text.length; j++) {
                var name = houseContent[j].name;
                apt[name] = apt_text[j];
            }
            
            if (apt.number) {
                var num = parseInt(apt.number);
                if (!isNaN(num)) {
                    apt.n = num;
                }
            }
            
            apts.push(apt);
        }
        return apts;
        //var sort = $filter('orderBy')(apts, 'n');
        //return sort;
    }

    function makeParameters(apts) {
        var parameters = [];
        //
        for (var i = 0; i < apts.length; i++) {
            var apt = apts[i];
            var parameter = [];
            for (var j = 0; j < houseContent.length; j++) {
                var name = houseContent[j].name;
                parameter.push(apt[name] || "");
            }
            parameters.push(parameter.join(':'));
        }
        //
        return parameters.join('|');
    }


    //function 


    var model = {
        houseContent: houseContent,
        lastError: ""
    };

    model.parameters = data.parameters;
    model.isEditable = data.isEditable || false;

    model.apts = parseApts(data.parameters);

    model.deleteApt = function (index) {
        model.apts.splice(index, 1);
    }

    model.addApt = function () {
        var maxn = 0;
        for (var i = 0; i < model.apts.length; i++) {
            var apt = model.apts[i];
            if (apt.n && (maxn < apt.n)) {
                maxn = apt.n;
            }
        }
        model.apts.push({ n: maxn + 1, number: (maxn + 1).toString() });
    }

    var checkApts = function () {
        for (var j = 0; j < houseContent.length; j++) {
            var parameter = houseContent[j];
            var required = parameter.required || false;
            var unique = parameter.unique || false;

            for (var i = 0; i < model.apts.length; i++) {
                //проверка на ошибки:
                var apt = model.apts[i];
                var p = apt[parameter.name];

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

                    if (p !== apt[parameter.name]) {
                        apt[parameter.name] = p;
                    }
                }
            }

            //-уникальность
            if (unique) {
                for (var i = 0; i < model.apts.length; i++) {
                    var p = model.apts[i][parameter.name];
                    for (var k = i + 1; k < model.apts.length; k++) {
                        var p1 = model.apts[k][parameter.name];
                        if (!!p && p !== "" && p === p1) {
                            model.lastError = "Параметр \"" + parameter.caption + "\" должен быть уникальным";
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
        if (checkApts()) {
            model.parameters = makeParameters(model.apts);
            $uibModalInstance.close(model.isEditable ? model.parameters : data.parameters);
        }
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});