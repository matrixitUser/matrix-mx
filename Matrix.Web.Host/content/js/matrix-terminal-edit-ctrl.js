angular.module("app")
    .controller('MatrixTerminalEditCtrl', function ($scope, $rootScope, $uibModalInstance, $transport, data, $filter, metaSvc) {

    var connection = data.connection;
    var objectId = data.objectId;
    $scope.connection = connection;
    var model = {
        config: {},
        uarts:[],
        profiles: [],
        serverClient: []
    };
    var modelCheck = {};

    $transport.send(new Message({ what: "parse-matrix-terminal-config-from-string" }, { strConfig: connection.config})).then(function (message) {
        model.config = message.body.config;
        model.profiles = message.body.profiles;
        model.APNs = message.body.APNs;

        model.uarts = [];
        model.serverClient = [];
        for (var i = 0; i < model.config.sUart.length; i++) {
            var uart = {};
            uart.BaudRate = model.config.sUart[i].u32BaudRate;
            uart.WordLen = 8;//model.config.sUart[i].u8WordLen;
            uart.StopBits = 1;//model.config.sUart[i].u8StopBits;
            uart.Parity = 'none';//model.config.sUart[i].u8Parity;
            model.uarts.push(uart);
        }
        for (var i = 0; i < model.profiles.length; i++) {
            if (model.profiles[i].ip == "listener") {
                model.serverClient.push("server");
            } else if (model.profiles[i].ip == "") {
                model.serverClient.push("");
            } else {
                model.serverClient.push("client");
            }
        }
        modelCheck = JSON.parse(JSON.stringify(model));
    });
    $scope.changed = function (index) {
        if (index != undefined) {
            if (model.serverClient[index] == "server") {
                model.profiles[index].ip = "listener";
                model.profiles[index].port = ""
            } else if (model.serverClient[index] == "") {
                model.profiles[index].ip = "";
                model.profiles[index].port = ""
            } else if (model.profiles[index].ip == "listener") {
                model.profiles[index].ip = "";
            }
        }
        var isEdit = false;
        for (var i = 0; i < model.profiles.length; i++) {
            if (model.profiles[i].port != modelCheck.profiles[i].port) {
                isEdit = true;
                break;
            }
            if (model.profiles[i].ip != modelCheck.profiles[i].ip) {
                isEdit = true;
                break;
            }
            if (model.serverClient[i] != modelCheck.serverClient[i]) {
                isEdit = true;
                break;
            }
        }
        if (!isEdit) {
            for (var i = 0; i < model.uarts.length; i++) {
                if (model.uarts[i].BaudRate != modelCheck.uarts[i].BaudRate) {
                    isEdit = true;
                    break;
                }
            }
        }
        model.isEditable = isEdit;
    };

    var listener = $rootScope.$on("transport:message-received", function (e, message) {
        if (message.head.what == "log") {
            for (var i = 0; i < message.body.messages.length; i++) {
                if (message.body.messages[i].message == "update") {
                    GetLightControlConfig(message.body.messages[i].tubeId);
                }
            }
        }
    });
        
    $scope.getConfig = function () {
        $transport.send(new Message({ what: "node-get-matrix-terminal-config" }, { objectId: connection.id  })).then(function (message) {
            model.config = message.body.config;
            model.profiles = message.body.profiles;
            model.APNs = message.body.APNs;

            model.uarts = [];
            model.serverClient = [];
            
            for (var i = 0; i < model.config.sUart.length; i++) {
                var uart = {};
                uart.BaudRate = model.config.sUart[i].u32BaudRate;
                uart.WordLen = 8;//model.config.sUart[i].u8WordLen;
                uart.StopBits = 1;//model.config.sUart[i].u8StopBits;
                uart.Parity = 'none';//model.config.sUart[i].u8Parity;
                model.uarts.push(uart);
            }
            for (var i = 0; i < model.profiles.length; i++) {
                if (model.profiles[i].ip == "listener") {
                    model.serverClient.push("server");
                } else if (model.profiles[i].ip == "") {
                    model.serverClient.push("");
                } else {
                    model.serverClient.push("client");
                }
            }

            modelCheck = JSON.parse(JSON.stringify(model));
        });


    };
    $scope.ok = function () {
        for (var i = 0; i < model.config.sUart.length; i++) {
            model.config.sUart[i].u32BaudRate = model.uarts[i].BaudRate;
        }
        $transport.send(new Message({ what: "parse-matrix-terminal-get-string-from-config" }, { config: model.config, profiles: model.profiles, APNs: model.APNs, strConfig: connection.config })).then(function (message) {
            var args = {
                cmd: "setConfig" + message.body.strConfig,
                components: "Current",
                onlyHoles: false
            }
            $transport.send(new Message({ what: "poll" }, { objectIds: [objectId], what: "matrixterminal", arg: args })).then(function (message) { });;
            listener();
        });

      
    };
    $scope.cancel = function () {
        model = JSON.parse(JSON.stringify(modelCheck));
        $scope.model = model;
    };
    $scope.exit = function () {
        $uibModalInstance.dismiss('cancel');
    };
    $scope.model = model;
});