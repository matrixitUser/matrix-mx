function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}


angular.module("app")
.controller("RowEditorCtrl", function ($scope, $rootScope, $log, $uibModalInstance, data, $transport, $drivers, $filter, $q, metaSvc, $uibModal) {

    var folderId;
    var id;
    var isNew = false;

    //var data = $scope.$parent.window.data;
    if (data === undefined) {
        isNew = true;
        id = null;
        folderId = null;
    } else {
        isNew = (data.id === undefined);
        id = data.id;
        folderId = data.folderId;
    }

    function parseDate(d) { //dd.MM.yyyy HH:mm
        var dt = (d && (typeof d == 'string' || d instanceof String)) ? d.split(' ') : [""];
        var date = dt[0].split('.');
        if (date.length === 3) {
            return new Date(date[2], (date[1] - 1), date[0]);
        } else {
            return new Date();
        }
    }

    function getDate(d) { //yyyy-MM-ddTHH....
        var date = new Date(d);
        return ('0' + date.getDate()).slice(-2) + '.' + ('0' + (date.getMonth() + 1)).slice(-2) + '.' + date.getFullYear();
    }


    $scope.getLocation = function (val) {
        if (val == "") return $q.when([""]);

        return $transport.send(new Message({ what: "edit-get-fias" }, { searchText: val }))
            .then(function (response) {
                return response.body.results.map(function (item) {
                    return item.value;
                });
            });
    };

    $scope.fillAddrFias = function()
    {
        if (model1.config != 'teplocom')
        {
            model1.area.addr = (!!model1.area.city ? model1.area.city + ", " : "") + (!!model1.area.street ? model1.area.street + ", " : "") + (!!model1.area.house ? model1.area.house : "");
        }
        else
        {
            model1.area.addr = (!!model1.area.address ? model1.area.address : "");
        }
        //model1.area.addr = $scope.getLocation(model1.area.addr)[0];
    }


    //загрузка модели: area, tube и device
    //далее каскадная загрузка соединений
    var isNewArea = false;
    $transport.send(new Message({ what: "edit-get-row" }, { isNew: isNew, id: id })).then(function (message) {
        $scope.model1.tube = message.body.tube;

        $scope.trashButton = $scope.model1.tube.isDeleted ? "Восстановить" : "Удалить";

        if (!$scope.model1.tube.disabledHistory) {
            $scope.model1.tube.disabledHistory = "[]";
        }
        $scope.model1.tube._disabledHistory = JSON.parse($scope.model1.tube.disabledHistory);

        $scope.model1.tube.startDate1 = parseDate($scope.model1.tube.startDate);

        $scope.model1.tube.syncDate = function () {
            $scope.model1.tube.startDate = getDate($scope.model1.tube.startDate1);
        }
        isNewArea = message.body.areaIsNew;
        $scope.model1.area = message.body.area;
        $scope.model1.device = message.body.device;
        $scope.model1.devices = message.body.devices;
        $scope.isLoaded = true;

        id = $scope.model1.tube.id;
        if (isNew || isNewArea) {
            $scope.model1.area.__viewMode = true;
            $scope.model1.area.__editMode = true;
            rules.push(new SaveRule("add", "relation", { start: $scope.model1.area.id, end: $scope.model1.tube.id, type: "contains", body: {} }));
            if (folderId && (folderId !== "all")) {
                rules.push(new SaveRule("add", "relation", { start: folderId, end: $scope.model1.area.id, type: "contains", body: {} }));
            }
        }

        $scope.model1._prevDevice = $scope.model1.device;

        //загрузка первой "волны" соединений
        $transport.send(new Message({ what: "edit-get-wave" }, { startId: $scope.model1.tube.id })).then(function (message) {
            for (var i = 0; i < message.body.wave.length; i++) {
                var con = message.body.wave[i];
                $scope.model1.connections.push(con);
                con._parent = $scope.model1.tube;
                //последующие волны
                getWave(con);
            }
        });
    });

    var getWave = function (root) {
        return $transport.send(new Message({ what: "edit-get-wave" }, { startId: root.id })).then(function (message) {
            return $q.all(message.body.wave.map(function (con) {
                root._child = con;
                root._oldChild = con;
                con._parent = root;
                return getWave(con);
            }));
        });
    };

    var model1 = {
        connections: [],
        _child: undefined,
        config: metaSvc.config
    };

    $scope.$watch("model1.tube.startDate1", function (newValue, oldValue) {
        if (model1.tube && model1.tube.syncDate) {
            model1.tube.syncDate();
        }
    });
    
    model1.resources = metaSvc.resources;

    $scope.model1 = model1;

    model1.editParameters = function (parameters, isEditable) {
        if (isEditable === undefined) isEditable = true;
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/row-edit-parameters-modal.html",
            controller: "RowEditParametersCtrl",
            size: "lg",
            resolve: {
                data: function () {
                    return { parameters: parameters, isEditable: isEditable };
                }
            }
        });

        modalInstance.result.then(function (parameters) {
            model1.tube["parameters"] = parameters;
        });
    }


    model1.editNetwork = function (object, key) {
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/row-edit-network-modal.html",
            controller: "RowEditNetworkCtrl",
            size: "md",
            resolve: {
                data: function () {
                    return { network: object[key] };
                }
            }
        });

        modalInstance.result.then(function (network) {
            object[key] = network;
        });
    }

    model1.editObises = function (obises, isEditable) {
        if (isEditable === undefined) isEditable = true;
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/row-edit-obises-modal.html",
            controller: "RowEditObisesCtrl",
            size: "lg",
            resolve: {
                data: function () {
                    return { obises: obises, isEditable: isEditable };
                }
            }
        });

        modalInstance.result.then(function (parameters) {
            model1.tube["parameters"] = parameters;
        });
    }
    model1.editMatrixTerminal = function (connection) {
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/matrix-terminal-edit-modal.html",
            controller: "MatrixTerminalEditCtrl",
            size: "lg",
            resolve: {
                data: function () {
                    return { connection: connection, objectId: id};
                }
            }
        });
    }
    var wrapConnection = function (con) {
        switch (con.type) {
            case "MatrixConnection": con._text = "mx: " + con.imei; break;
            case "SimpleMatrixConnection": con._text = con.imei; break;
            case "TeleofisWrxConnection": con._text = "t/o: " + con.imei; break;
            case "MilurConnection": con._text = "mil: " + con.imei; break;
            case "MatrixTerminalConnection": con._text = "m/t: " + con.imei; break;
            case "LanConnection": con._text = "lan: " + con.host + ":" + con.port; break;
            case "TcpClient": con._text = "tcp: " + con.host + ":" + con.port; break;
            case "CsdConnection": con._text = con.phone; break;
            case "Modem": //break; 
            case "ComConnection": con._text = con.port; break;
            case "ZigbeeConnection": con._text = con.mac; break;
            case "HttpConnection": con._text = "СПС " + con.id; break;
            default: con._text = con.name ? con.name : ('[' + con.id + ']'); break;
        }
        return con;
    };

    var rules = [];

    //загружена|не загружена модель
    $scope.isLoaded = false;

    $scope.changeWorkState = function () {
        if ($scope.model1.tube.isDisabled) {
            $scope.open().then(function (reason) {
                $scope.model1.tube.reason = reason.reason;
                $scope.model1.tube.isDisabled = true;
                if (!$scope.model1.tube._disabledHistory) {
                    $scope.model1.tube._disabledHistory = [];
                }
                $scope.model1.tube._disabledHistory.push({ start: reason.date, reason: reason.reason });
            }, function () {
                $scope.model1.tube.isDisabled = false;
            });;
        } else {
            if (!$scope.model1.tube._disabledHistory) {
                $scope.model1.tube._disabledHistory = [];
            }
            var last = $scope.model1.tube._disabledHistory.pop();
            if (!last) return;
            last.end = new Date();
            $scope.model1.tube._disabledHistory.push(last);
        }
    }

    $scope.open = function () {

        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "disable-confirm.html",
            controller: "DisableConfirmCtrl",
            size: "sm"
        });

        return modalInstance.result;
    };

    $scope.changeDevice = function (device) {
        if ($scope.model1._prevDevice !== null && $scope.model1._prevDevice !== undefined) {
            rules.push(new SaveRule("del", "relation", { start: id, end: $scope.model1._prevDevice.id, type: "device", body: {} }));
        }
        rules.push(new SaveRule("add", "relation", { start: id, end: device.id, type: "device", body: {} }));
        $scope.model1._prevDevice = device;
    }

    //обновление соединений по 10 штук с сервера todo картинки по типу соединения
    $scope.refreshConnections = function (filter, connection, types) {
        $transport.send(new Message({ what: "edit-get-connections" }, { filter: filter, types: types })).then(function (message) {
            connection._connections = [];

            //сначала соединения "новые"            
            //if (types.indexOf("MatrixConnection") >= 0) {
            //    connection._connections.push({
            //        type: "MatrixConnection",
            //        _parent: connection,
            //        _text: "<новый матрикс>",
            //        _isNew: true
            //    });
            //}

            if (types.indexOf("CsdConnection") >= 0) {
                connection._connections.push({
                    type: "CsdConnection",
                    _parent: connection,
                    _text: "<новый модем>",
                    _isNew: true
                });
            }

            if (types.indexOf("MatrixSwitch") >= 0) {
                connection._connections.push({
                    type: "MatrixSwitch",
                    _parent: connection,
                    _text: "<новый свитч>",
                    _isNew: true
                });
            }

            if (types.indexOf("ZigbeeConnection") >= 0) {
                connection._connections.push({
                    type: "ZigbeeConnection",
                    _parent: connection,
                    _text: "<новое беспроводное устройство Zigbee>",
                    _isNew: true
                });
            }

            if (types.indexOf("ZliteConnection") >= 0) {
                connection._connections.push({
                    type: "ZliteConnection",
                    _parent: connection,
                    _text: "<новое беспроводное устройство Zlite>",
                    _isNew: true
                });
            }

            if (types.indexOf("HttpConnection") >= 0) {
                connection._connections.push({
                    type: "HttpConnection",
                    _parent: connection,
                    _text: "<новое СПС соединение>",
                    _isNew: true
                });
            }

            //if (types.indexOf("TcpClient") >= 0) {
            //    connection._connections.push({
            //        type: "TcpClient",
            //        _parent: connection,
            //        _text: "<новый tcp-клиент>",
            //        _isNew: true
            //    });
            //}

            if (types.indexOf("LanConnection") >= 0) {
                connection._connections.push({
                    type: "LanConnection",
                    _parent: connection,
                    _text: "<новое lan-соединение>",
                    _isNew: true
                });
            }

            if (types.indexOf("ComConnection") >= 0) {
                connection._connections.push({
                    type: "ComConnection",
                    _parent: connection,
                    _text: "<новый ком порт>",
                    _isNew: true
                });
            }

            if (types.indexOf("MxModbus") >= 0) {
                connection._connections.push({
                    type: "MxModbus",
                    _parent: connection,
                    _text: "<новый modbus-коннектор>",
                    _isNew: true
                });
            }
            

            if (types.indexOf("ZigbeePort") >= 0) {
                connection._connections.push({
                    type: "ZigbeePort",
                    _parent: connection,
                    _text: "<новый порт zigbee>",
                    _isNew: true
                });
            }

            //далее текущее, если есть
            if (connection._child !== undefined) {
                connection._connections.push(wrapConnection(connection._child));
            }

            for (var i = 0; i < message.body.connections.length; i++) {

                var con = message.body.connections[i];
                con._parent = connection;
                if (connection._child !== undefined && con.id === connection._child.id) {
                    continue;
                }

                con = wrapConnection(con);
                connection._connections.push(con);
            }
        });
    };


    $scope.addOrCreateConnection = function (connection) {
        if (connection._child && connection._child._isNew) {

            $transport.send(new Message({ what: "helper-create-guid" }, { count: 1 })).then(function (message) {

                connection._child.id = message.body.guids[0];
                connection._child._child = undefined;
                connection._child._oldChild = undefined;
                connection._child.__editMode = true;

                rules.push(new SaveRule("add", "node", { id: connection._child.id, type: connection._child.type, body: connection._child }));
                joinRelations(connection);
            });
            return;
        } else {
            getWave(connection._child).then(function () {
                joinRelations(connection);
            });
        }
    };

    var joinRelations = function (connection) {
        if (connection === undefined) return;
        var start = connection;
        var end = start._child;

        var old = start._oldChild;

        if (old) {
            rules.push(new SaveRule("del", "relation", { start: start.id, end: old.id, type: "contains", body: {} }));
        }

        if (connection === $scope.model1.tube) {
            $scope.model1.connections.push(end);
        }
        var port = end._connection === undefined ? 1 : end._connection.port;
        rules.push(new SaveRule("add", "relation", { start: start.id, end: end.id, type: "contains", body: { port: port } }));
        //для тюба ребенок всегда undefined
        if (connection === $scope.model1.tube) {
            connection._oldChild = undefined;
            connection._child = undefined;
        } else {
            connection._oldChild = end;
        };
    }

    $scope.pushConnection = function (addedConnection) {
        if (addedConnection === undefined) return;
        $scope.model1.connections.push(addedConnection);
        rules.push(new SaveRule("add", "relation", { start: id, end: addedConnection.id, type: "contains", body: {} }));
    }

    $scope.deleteConnection = function (child) {
        if (child === undefined) return;
        var parent = child._parent;
        if (parent === $scope.model1.tube) {
            var index = $scope.model1.connections.indexOf(child);
            $scope.model1.connections.splice(index, 1);
            rules.push(new SaveRule("del", "relation", { start: parent.id, end: child.id, type: "contains", body: {} }));
        } else {
            rules.push(new SaveRule("del", "relation", { start: parent.id, end: child.id, type: "contains", body: {} }));
            parent._child = undefined;
        }
    };

    //$scope.windows = [];
    //for (var i = 1; i < 24; i++) {
    //    $scope.windows.push(i);
    //}

    function removePropertyTemp(obj) {
        for (var key in obj) {
            if (key !== undefined && obj.hasOwnProperty(key)) {
                if (key.startsWith("__")) {
                    delete obj[key];
                }
            }
        }
    }

    $scope.save = function () {
        $scope.isLoaded = false;
        $scope.model1.tube.disabledHistory = JSON.stringify($scope.model1.tube._disabledHistory);

        removePropertyTemp($scope.model1.area);
        rules.push(new SaveRule((isNew || isNewArea) ? "add" : "upd", "node", { id: $scope.model1.area.id, type: "Area", body: $scope.model1.area }));
        rules.push(new SaveRule(isNew ? "add" : "upd", "node", { id: $scope.model1.tube.id, type: "Tube", body: $scope.model1.tube }));

        for (var i = 0; i < $scope.model1.connections.length; i++) {
            var connection = $scope.model1.connections[i];
            removePropertyTemp(connection);
            rules.push(new SaveRule("upd", "node", { id: connection.id, type: connection.type, body: connection }));
            while (connection._child) {
                connection = connection._child;
                removePropertyTemp(connection);
                rules.push(new SaveRule("upd", "node", { id: connection.id, type: connection.type, body: connection }));
            }
        }

        var x = rules;

        for (var i = 0; i < rules.length; i++) {
            var rule = rules[i];
            var body = rule.content.body;
            for (var prop in body) {
                if (!body.hasOwnProperty(prop)) continue;
                if (prop.indexOf("_") === 0) {
                    delete body[prop];
                }
            }
        }
        var tmpRules = JSON.stringify(rules);
        $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function (message) {
            model.close(message)
        });
        //model.close();
    };

    $scope.meta = {
        MatrixConnection: {
            img: metaSvc.connectionImgFromType("MatrixConnection"),
            name: "Матрикс",
            fields: [{
                name: "imei", title: "IMEI"
            }, {
                name: "phone", title: "Телефон"
            }]
        },
        MatrixSwitch: {
            img: metaSvc.connectionImgFromType("Matrix"),
            name: "Матрикс свитч",
            fields: [{
                name: "name", title: "Название"
            }]
        },
        TeleofisWrxConnection: {
            img: metaSvc.connectionImgFromType("TeleofisWrxConnection"),
            name: "TeleOfis WRX",
            fields: [{
                name: "imei", title: "IMEI"
            }, {
                name: "phone", title: "Телефон"
            }]
        },
        MilurConnection: {
            img: metaSvc.connectionImgFromType("MilurConnection"),
            name: "Milur",
            fields: [{
                name: "imei", title: "IMEI"
            }, {
                name: "phone", title: "Телефон"
            }]
        },
        MatrixTerminalConnection: {
            img: metaSvc.connectionImgFromType("MatrixTerminalConnection"),
            name: "Matrix Terminal",
            fields: [{
                name: "imei", title: "IMEI"
            }, {
                name: "phone", title: "Телефон"
            }]
        },
        LanConnection: {
            img: metaSvc.connectionImgFromType("LanConnection"),
            name: "Соединение LAN",
            fields: [{
                name: "name", title: "Название"
            }, {
                name: "host", title: "IP-адрес"
            }, {
                name: "port", title: "Порт"
            }]
        },
        CsdConnection: {
            img: metaSvc.connectionImgFromType("CsdConnection"),
            name: "Модем CSD",
            fields: [{
                name: "phone", title: "Телефон"
            }]
        },
        HttpConnection: {
            img: metaSvc.connectionImgFromType("HttpConnection"),
            name: "Соединение через интернет",
            fields: []
        },
        ZigbeeConnection: {
            img: metaSvc.connectionImgFromType("ZigbeeConnection"),
            name: "Беспроводное соединение",
            fields: [{
                name: "mac", title: "MAC-адрес"
            }, {
                name: "kind", title: "Вид устройства"
            }]
        },
        ZigbeePort: {
            img: metaSvc.connectionImgFromType("ZigbeePort"),
            name: "Zigbee-порт",
            fields: [{
                name: "name", title: "Название"
            }]
        },
        ComConnection: {
            img: metaSvc.connectionImgFromType("ComConnection"),
            name: "Ком порт",
            fields: [{
                name: "port", title: "Порт"
            }, {
                name: "baudRate", title: "Скорость"
            }]
        },
        TcpClient: {
            img: metaSvc.connectionImgFromType("TcpClient"),
            name: "Клиент TCP",
            fields: [{
                name: "host", title: "IP-адрес"
            }, {
                name: "port", title: "Порт"
            }, {
                name: "keepalive", title: "Keep-Alive"
            }]
        },
        MxModbus: {
            img: metaSvc.connectionImgFromType("MxModbus"),
            name: "Modbus",
            fields: [{
                name: "receiver", title: "Инициатива снизу?"
            }]
        },
        MilurConnection: {
            img: metaSvc.connectionImgFromType("MatrixConnection"),
            name: "Милур",
            fields: [{
                name: "imei", title: "IMEI"
            }, {
                name: "phone", title: "Телефон"
            }]
        }
    };

    //$transport.send(new Message({ what: "rows-get" }, { ids: [id] })).then(function (msg) {
    var model = {
        //editedCounter: 0,
        //deviceId: undefined,
        //devices: undefined,
        modal: undefined,
        //devicesField: {}
    };

    model.delete = function () {
        $scope.isLoaded = true;

        $scope.model1.tube.isDeleted = !$scope.model1.tube.isDeleted;
        var delRules = [
            new SaveRule("upd", "node", { id: $scope.model1.tube.id, type: "Tube", body: $scope.model1.tube })
        ];

        $transport.send(new Message({ what: "edit" }, { rules: delRules })).then(function (message) {
            model.close(message)
        });
    };

    model.clearCache = function () {
        $transport.send(new Message({ what: "cache-clear" }, { id: id }));
    }

    //

    //model.window = $scope.$parent.window,
    //model.modalOpen = function () {
    //    model.modal = $modal.open({
    //        templateUrl: model.window.modalTemplateUrl,
    //        size: 'lg',
    //        scope: $scope
    //    })

    //    model.modalIsOpen = true;

    //    model.modal.result.then(function () {
    //        model.modalIsOpen = false;
    //        if (model.autoclose && model.autoclose()) {
    //            model.close();
    //        }
    //    }, function () {
    //        model.modalIsOpen = false;
    //        if (model.autoclose && model.autoclose()) {
    //            model.close();
    //        }
    //    });
    //}

    //model.close = function () {
    //    model.modal.close();
    //    model.window.close();
    //}

    ////model.reset = function () {
    ////    init();
    ////}


    //model.autoclose = function () { return true; }

    //model.window.open = model.modalOpen;

    //model.modalOpen();

    model.close = function (message) {
        $uibModalInstance.close(message ? isNew : undefined);
    };

    $scope.model = model;
    //$scope.meta = meta;
    //});


});


angular.module("app")
.controller('DisableConfirmCtrl', function ($scope, $uibModalInstance) {

    $scope.model = {
        date: new Date(),
        reason: "снят на поверку"
    };

    $scope.ok = function () {
        $uibModalInstance.close($scope.model);
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };
});