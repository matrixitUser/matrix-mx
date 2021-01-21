angular.module("app")
.service("logSvc", function ($transport, $log, $q, $rootScope) {

    var objects = { ids: [], assoc: {} };
    var loggingCount = 1;// делать connect НЕ нужно. default:0;
    //var messages = [];

    var serverSubscribe = function (ids) {
        return $transport.send(new Message({ what: "log-subscribe" }, { ids: ids }))
            .then(function (message) {
                $log.debug("log-svc ответ с сервера %s", message.head.what);
                if (message.head.what == "log-subscribe") {
                    return message.body.ids;
                } else {
                    return $q.reject("не авторизован");
                }
            });
    }

    var subscribe = function (ids) {
        if (!ids) {
            return $q.reject("выберите объекты из списка");
        }
        var updated = false;

        for (var i = 0; i < ids.length; i++) {
            var id = ids[i];
            if (!objects.assoc[id]) {
                objects.assoc[id] = 1;
                objects.ids.push(id);
                updated = true;
            } else {
                objects.assoc[id]++;
            }
        }

        if (updated && loggingCount > 0) {//есть изменения
            return serverSubscribe(objects.ids);
        }

        return $q.when(objects.ids);
    }

    var unsubscribe = function (ids) {
        if (!ids) {
            return $q.reject("выберите объекты из списка");
        }
        var updated = false;
        for (var i = 0; i < ids.length; i++) {
            var id = ids[i];
            if (objects.assoc[id]) {
                if (objects.assoc[id] == 1) {
                    updated = true;
                    delete objects.assoc[id];
                    for (var j in objects.ids) {
                        if (id == objects.ids[j]) {
                            objects.ids.splice(j, 1);
                            break;
                        }
                    }
                } else {
                    objects.assoc[id]--;
                }
            }
        }

        if (updated && loggingCount > 0) {//есть изменения
            return serverSubscribe(objects.ids);
        }

        return $q.when(objects.ids);
    }

    var unsubscribeAll = function () {                
        objects.assoc = {};
        objects.ids.length = 0;

        if (loggingCount > 0) {
            return serverSubscribe(objects.ids);
        } 
        
        return $q.when(objects.ids);
    }

    var connect = function () {
        if (loggingCount == 0) {
            serverSubscribe(objects.ids);
        }
        loggingCount++;
    }

    var disconnect = function () {
        if (loggingCount > 0) {
            loggingCount--;
        }
        if (loggingCount == 0) {
            serverSubscribe([]);
        }
    }


    //$rootScope.$on("transport:message-received", function (e, message) {

    //    if (message.head.what != "log") return;

    //    $log.debug("пришло логов %s", message.body.messages.length);
    //    for (var i = 0; i < message.body.messages.length; i++) {
    //        var msg = message.body.messages[i];//{id, date, message}

    //        //var tube = model.tubes.aa[msg.id];

    //        //if (tube === undefined) {
    //        //    var t = { id: msg.id, object: msg.object, show: true };
    //        //    model.tubes.show[msg.id] = 1;
    //        //    model.tubes.aa[msg.id] = t;
    //        //    model.tubes.arr.push(t);
    //        //} else {
    //        //    tube.object = msg.object;
    //        //}

    //        messages.push(msg);
    //    }
    //});

    return {
        subscribe: subscribe,       //подписка - добавление в список подписанных
        unsubscribe: unsubscribe,   //отписка - удаление из списка подписанных 
        unsubscribeAll: unsubscribeAll,
        connect: connect,           //запрос сервера на получение подписок
        disconnect: disconnect,     //команда серверу на прекращение получения данных
        getSubscribers: function () { return objects; }, //подписчики
        //getMessages: function () { return messages; } //сообщения
    }


    //    $scope.filterText = "";

    //    $scope.subscribe = function () {

    //    }

    //    $scope.caption = "";

    //    $scope.tubeIds = {};
    //    $scope.tubes = [];

    //    $scope.logMessages = [];

    //    $scope.tubesOpts = {        
    //        angularCompileRows: true,
    //        columnDefs: [{
    //            checkboxSelection: true,
    //            width: 30,
    //            displayName: ""
    //        }, {
    //            displayName: "Объект", field: "object"
    //        }, {
    //            displayName: "Показывать", template: "<input ng-model='data.show' type='checkbox' ng-click='showChanged(data.object)' />"
    //        }],
    //        enableColResize: true,
    //        rowData: $scope.tubes
    //    };

    //    $scope.showChanged = function (obj) {
    //        $log.debug("log показ %s отменен", obj);
    //        $scope.tubeIds[obj] = 0;
    //        $scope.logOpts.columnDefs[1].filterParams.values = assocToShow($scope.tubeIds);
    //    }

    //    $scope.showChangedAll = function () {
    //        $log.debug("log показ изменен для всех");
    //        var assoc = $scope.tubeIds;
    //        for (key in assoc) {
    //            if (assoc.hasOwnProperty(key) && assoc[key] == 1) {
    //                assoc[key] = 0;
    //            }
    //        }
    //        assoc["tube0"] = 1;
    //        assoc["tube5"] = 1;
    //        $scope.logOpts.columnDefs[1].filterParams.values = assocToShow($scope.tubeIds);
    //    }

    //    $scope.logOpts = {
    //        //angularCompileRows: true,
    //        enableFilter: true,
    //        columnDefs: [{
    //            displayName: "Дата", field: "date"
    //        }, {
    //            displayName: "Объект",
    //            field: "object",
    //            filter: 'set',
    //            filterParams: {values: []}
    //        }, {
    //            displayName: "Сообщение", field: "message"
    //        }],
    //        enableColResize: true,
    //        rowData: $scope.logMessages
    //    };



    //    $rootScope.$on("transport:message-received", function (e, message) {

    //        if ($scope.tubeIds[message.object] === undefined) {
    //            $scope.tubeIds[message.object] = 1;
    //            //$scope.tubeIdsArray = assocToArray($scope.tubeIds);
    //            $scope.logOpts.columnDefs[1].filterParams.values = assocToShow($scope.tubeIds);
    //            $scope.tubes.push({ object: message.object, show: true });
    //            $scope.tubesOpts.api.onNewRows();
    //        }

    //        $scope.logMessages.push(message);
    //        $scope.logOpts.api.onNewRows();
    //    });

    //    $scope.clear = function () {
    //        $scope.messages.length = 0;
    //    }
});