angular.module("app")
.controller("PollCtrl", function ($scope, $transport, $rootScope, $poll) {
    var now = new Date();

    var model = $scope.$parent.model;

    model.end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), 0, 0, 0);
    model.start = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0, 0);
    model.onlyHoles = true;

    $scope.subscribe = function () {
    }

    var getIds = function () {
        var ids = {
            targets: [],
            inputs: [],
            outputs: []
        };

        for (var i = 0; i < $scope.rows.length; i++) {
            var row = $scope.rows[i];
            ids.targets.push(row.targetId);
            ids.outputs.push(row.outputId);
            ids.inputs.push(row.inputId);
        }

        return ids;
    }

    $scope.caption = "";
    //if ($scope.rows.length == 1) {
    //    $scope.caption = "выбран " + $scope.rows[0].areaName;
    //} else {
    //    $scope.caption = "выбрано " + $scope.rows.length + " объектов";
    //}

    //var allIds = [];
    //for (var i = 0; i < $scope.rows.length; i++) {
    //    var row = $scope.rows[i];
    //    allIds.push(row.targetId);
    //    allIds.push(row.outputId);
    //    allIds.push(row.inputId);
    //}

    //$scope.addCleaner(function () {
    //    $scope.unsubscribe();
    //});

    //var getSubscribers = function () {
    //    var subs = [];
    //    for (var i = 0; i < $scope.rows.length; i++) {
    //        var row = $scope.rows[i];
    //        subs.push(row.targetId);
    //        subs.push(row.outputId);
    //        subs.push(row.inputId);
    //    }
    //    transportSvc.sendMessage2(new Message({ what: "log-subscribe" }, { added: subs, removed: [] }), function (msg) { });
    //}
    //getSubscribers();

    //$scope.unsubscribe = function () {
    //    var subs = [];
    //    for (var i = 0; i < $scope.rows.length; i++) {
    //        var row = $scope.rows[i];
    //        subs.push(row.targetId);
    //        subs.push(row.outputId);
    //        subs.push(row.inputId);
    //    }
    //    transportSvc.sendMessage2(new Message({ what: "log-subscribe" }, { added: [], removed: subs }), function (msg) { });
    //}

    //$scope.setDisposer($scope.unsubscribe);

    $scope.days = function () {
        $poll.poll("day", model.data, { start: model.start, end: model.end, onlyHoles: model.onlyHoles });
    }

    $scope.ping = function () {
        $poll.poll("all", model.data, { foo: "bar" });
    };

    $scope.cancel = function () {
        $poll.cancel();
    };

    $scope.makePollDescription = function (raw) {
        return raw;
    };

    $scope.atCommandText = "at";
    $scope.atCommandSend = function () {
        var targets = getIds().outputs;
        transportSvc.sendMessage2(new Message({ what: "matrix-at" }, {
            at: $scope.atCommandText,
            targets: targets
        }), function (message) { });
    };

    $scope.versionCommandSend = function () {
        var targets = getIds().outputs;
        if (targets.length > 0) {
            transportSvc.sendMessage2(new Message({ what: "matrix-version" }, {
                targets: targets
            }), function (message) { });
        }
    };

    $scope.constants = function () {
        var targets = getIds().targets;
        transportSvc.sendMessage2(new Message({ what: "survey-constant" }, {
            targets: targets
        }), function (message) { });
    };

    $scope.abnormals = function () {
        var targets = getIds().targets;
        transportSvc.sendMessage2(new Message({ what: "survey-abnormal" }, {
            targets: targets,
            start: $scope.start,
            end: $scope.end
        }), function (message) { });
    }

    $scope.special = function () {
        transportSvc.sendMessage2(new Message({ what: "special" }, {}), function (message) { });
    }

    $scope.messages = [];

    $scope.columns = [{
        field: "date",
        displayName: "Дата",
        width: "150",
        type: "string",
        resizable: false,
        sort: {
            direction: "desc",
            priority: 0
        }
    }, {
        field: "object",
        displayName: "Объект",
        width: "300",
        type: "string",
        resizable: true
    }, {
        field: "message",
        displayName: "Сообщение",
        width: "100%",
        type: "string",
        resizable: true
    }];

    //настройки грида
    //см. http://angular-ui.github.io/ng-grid/
    $scope.logOptions = {
        data: $scope.messages,
        columnDefs: $scope.columns,
        enableColumnResizing: true,
        enableColumnReordering: true
    };

    $rootScope.$on("messageReceived", function (e, message) {
        if (message.head.what === "log") {
            var records = message.body.messages;
            for (var i = 0; i < records.length; i++) {
                var record = records[i];
                for (var j = 0; j < allIds.length; j++) {
                    if (allIds[j] === record.objectId) {
                        var newMessage = {
                            message: record.message,
                            date: record.date,
                            object: record.obj,
                            objectId: record.objectId
                        };
                        $scope.messages.splice(0, 0, newMessage);
                    }
                }
            }
            if (!$scope.$$phase) $scope.$apply();
        }
    });

    $scope.clear = function () {
        $scope.messages.length = 0;
    }
});