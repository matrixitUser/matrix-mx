angular.module("app")
.controller('RowEditNetworkCtrl', function ($scope, $uibModalInstance, data, $filter) {

    //Формат: 
    //<NETWORK> = <NODE>|<NODE>|...
    //<NODE> = <ADDR>;<ROUTE>
    //<ROUTE> = <ADDR>,<ADDR>,...
    var nodeContent = [{
        name: "addr",
        caption: "Адрес",
        type: "string",
        order: 1,
        unique: true,
        required: true,
        width: 250
    }, {
        name: "route",
        caption: "Маршрут (через запятую)",
        type: "string",
        order: 3,
        init: "",
        unique: false,
        required: false,
        width: 150
    }];

    function parseNodes(network) {
        var nodes = [];
        if ((network === undefined) || (network === "")) {
            return nodes;
        }

        var network_text = network.split('|');
        //
        for (var i = 0; i < network_text.length; i++) {
            var node = {};
            var node_text = network_text[i].split(':');
            //
            for (var j = 0; j < node_text.length; j++) {
                var name = nodeContent[j].name;
                node[name] = node_text[j];
            }
            //
            nodes.push(node);
        }
        //
        return nodes;
    }

    function makeNetwork(nodes) {
        var net = [];
        //
        for (var i = 0; i < nodes.length; i++) {
            var node = nodes[i];
            var parameter = [];
            for (var j = 0; j < nodeContent.length; j++) {
                var name = nodeContent[j].name;
                parameter.push(node[name] || "");
            }
            net.push(parameter.join(':'));
        }
        //
        return net.join('|');
    }


    //function 


    var model = {
        nodeContent: nodeContent,
        lastError: ""
    };

    model.network = data.network;
    model.isEditable = true;

    model.nodes = parseNodes(data.network);

    model.deleteNode = function (index) {
        model.nodes.splice(index, 1);
    }

    model.addNode = function () {
        model.nodes.push({});
    }

    var checkNodes = function () {
        for (var j = 0; j < nodeContent.length; j++) {
            var parameter = nodeContent[j];
            var required = parameter.required || false;
            var unique = parameter.unique || false;

            for (var i = 0; i < model.nodes.length; i++) {
                //проверка на ошибки:
                var node = model.nodes[i];
                var p = node[parameter.name];

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

                    if (p !== node[parameter.name]) {
                        node[parameter.name] = p;
                    }
                }
            }

            //-уникальность
            if (unique) {
                for (var i = 0; i < model.nodes.length; i++) {
                    var p = model.nodes[i][parameter.name];
                    for (var k = i + 1; k < model.nodes.length; k++) {
                        var p1 = model.nodes[k][parameter.name];
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
        if (checkNodes()) {
            model.nodes = makeNetwork(model.nodes);
            $uibModalInstance.close(model.nodes);
        }
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});