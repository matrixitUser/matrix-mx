angular.module("app");

app.service("$transport", function ($settings, $log, $http, $rootScope, $parse, $q, $timeout, $filter) {

    //var sessionMock = { "userSession": 1 };
    //var logSubscribeMock = {};

    //var virtualRows = [];
    //for (var i = 0; i < 500; i++) {
    //    virtualRows.push({
    //        id: "tube" + i, name: "Tube " + i,
    //        Area: [{ id: "area" + i, name: "Area of tube" + i }],
    //        MatrixConnection: (i % 3 == 0) ? [{ imei: "Imei of " + i, phone: "Mx phone " + i, signal: 0 }] : [],
    //        Device: [{ name: "Device of " + i }],
    //        CsdConnection: (i % 3 == 0) ? [] : [{ phone: "Csd phone " + i }],
    //        groups: [(i % 3 == 0) ? "" : "" + (i % 3)],
    //        Current: [{ s1: 'P', d1: 0 }]
    //    });
    //}

    //var receivedN = 0;
    //var updatedN = 0;






    var connection = $.connection("./messageacceptor");

    var model = {
        connectIsEnabled: false,
        isConnected: false
    };

    var reconnect = function () {//запускает прием/передачу сообщений 
        connection.start().done(function () {
            model.connectIsEnabled = true;
            model.isConnected = true;
            $log.debug("соединение установлено");
            //уведомляет о установлении соединения
            $rootScope.$broadcast("transport:connected");
            var sessionId = $settings.getSessionId();
            send(new Message({ what: "signal-bind" }, { sessionId: sessionId, connectionId: connection.id }));
        });
    }

    //соединить с сервером
    var connect = function () {

        if (model.connectIsEnabled === false) {
            $log.debug("начало соединения");
            reconnect();
        } else {
            $log.debug("начало соединения: соединение уже установлено");
        }
    };

    //разъединить связь с сервером
    var disconnect = function (sessionId) {
        $log.debug("соединение окончено");

        if (model.connectIsEnabled == true) {
            model.connectIsEnabled = false;
            //model.isConnected = false;
            send(new Message({ what: "signal-unbind" }, { sessionId: sessionId, connectionId: connection.id }));
            connection.stop();
            $log.debug("соединение разорвано");
            $rootScope.$broadcast("transport:disconnected");
        } else {
            $log.debug("окончание соединения: связь уже разорвана");
        }
    };

    //прием сообщений (возбуждается событие)
    connection.received(function (message) {
        $log.debug("получено сообщение от сервера %s", message.head.what);
        $rootScope.$broadcast("transport:message-received", message);
    });

    connection.error(function (error) {
        $log.error(error);
    });

    connection.disconnected(function () {
        model.isConnected = false;
        if (model.connectIsEnabled) {
            $log.debug("разрыв соединения, повтор через 5 сек.");
            $timeout(function () {
                reconnect();
            }, 5000); // restart connection after 5 seconds.
        } else {
            $log.debug("разрыв соединения");
        }
    });




    //отправка сообщения
    var send = function (message, notSid) {

        $log.debug("на сервер отправлено сообщение: %s", message.head.what);
        if (notSid === undefined) {
            message.head.sessionId = $settings.getSessionId();
        }

        return $http({
            url: "api/transport",
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            data: message
        }).then(function (data, status) {
            var msg = data.data;
            if (!msg) {
                $log.error("на " + message.head.what + " получен пустой ответ (null)");
                return $q.reject("пустой ответ (null)");
            }
            if (msg.head.what == "auth-error") {
                $log.debug("получено сообщение: ошибка аутентификации - %s", msg.body.message);
                return $q.reject(msg.body.message);
            }
            if (msg.head.what == "error") {
                $log.debug("получено сообщение: ошибка - %s (%s)", msg.body.message, msg.body.description);
                return $q.reject(msg.body.message);
            }
            $log.debug("получено сообщение %s", msg.head.what);
            return msg;
        }, function (e) {
            $log.error("send ошибка %s", e.status);
            return $q.reject("ошибка " + e.status);
        });

        //return deferred.promise;
    }

    /////////////////// MOCK /////////////////// MOCK /////////////////// MOCK /////////////////// MOCK /////////////////// MOCK /////////////////// MOCK /////////////////// MOCK /////////////////// MOCK 

    //var sendMock = function (message, notSid) {
    //    //var deferred = $q.defer();

    //    $log.debug("MOCK на сервер отправлено сообщение: %s", message.head.what);
    //    if (notSid === undefined) {
    //        message.head.sessionId = $settings.getSessionId();
    //    }

    //    var promise;
    //    switch (message.head.what) {
    //        case 'users-get-rules':
    //            promise = $timeout(function () {
    //                var data = { head: { what: message.head.what }, body: {} }
    //                $log.debug("эмуляция получено сообщение %s", data.head.what);

    //                var objectId = $parse('body.objectId')(message);

    //                data.body.root = {
    //                    data: { name: "admins" },
    //                    group: true,
    //                    expanded: true,
    //                    children: [{
    //                        data: { name: "Благовар" },
    //                        group: false
    //                    }, {
    //                        data: { name: "УГАТУ" },
    //                        group: false
    //                    }, {
    //                        data: { name: "Шифер" },
    //                        group: false
    //                    }, ]
    //                };

    //                return { data: data };
    //            }, 750);
    //            break;

    //        default:
    //            promise = $http({
    //                url: "api/transport",
    //                method: "POST",
    //                headers: {
    //                    "Content-Type": "application/json"
    //                },
    //                data: message
    //            });
    //            break;
    //    }

    //    return promise.then(function (data, status) {
    //        var msg = data.data;
    //        if (msg.head.what == "auth-error") {
    //            $log.debug("получено сообщение: ошибка аутентификации - %s", msg.body.message);
    //            return $q.reject(msg.body.message);
    //        }
    //        if (msg.head.what == "error") {
    //            $log.debug("получено сообщение: ошибка - %s", msg.body.message);
    //            return $q.reject(msg.body.message);
    //        }
    //        $log.debug("получено сообщение %s", msg.head.what);
    //        return msg;
    //    }, function (e) {
    //        $log.error("send ошибка %s", e.status);
    //        return $q.reject("ошибка " + e.status);
    //    });
    //}

    ////соединение с сервером
    //var connectMock = function () {
    //    if (!model.connectIsEnabled) {
    //        $log.debug("эмуляция начало соединения");
    //        //запускает прием/передачу сообщений
    //        $timeout(function () {
    //            model.connectIsEnabled = true;
    //            model.isConnected = true;
    //            $log.debug("эмуляция соединение установлено");
    //            //уведомляет о установлении соединения
    //            $rootScope.$broadcast("transport:connected");
    //            var sessionId = $settings.getSessionId();
    //            sendMock(new Message({ what: "signal-bind" }, { sessionId: sessionId, connectionId: 0 }));
    //        }, 1500);
    //    }
    //};

    //var disconnectMock = function () {
    //    model.connectIsEnabled = false;
    //    model.isConnected = false;
    //}

    //function signalRMock() {
    //    var i = (receivedN % 12);
    //    var messages = [{ date: new Date(), id: "tube" + i, object: "Tube " + i, message: "Hello" + i + "(" + receivedN + ")" }];
    //    var data = { head: { what: "LogMessage" }, body: { messages: messages } };
    //    receivedN++;
    //    $timeout(signalRMock, 1000);
    //    if (model.connectIsEnabled && logSubscribeMock[messages[0].id]) {
    //        $log.debug("signalr sent " + receivedN);
    //        $rootScope.$broadcast("transport:message-received", data);
    //    }
    //}

    //function rowUpdateMock() {

    //    var uids = [];

    //    for (var i = 0; i < virtualRows.length; i++) {
    //        if (virtualRows[i].MatrixConnection && virtualRows[i].MatrixConnection.length > 0) {
    //            virtualRows[i].MatrixConnection[0].signal = 100 - updatedN + i;
    //        }
    //        if (virtualRows[i].Current && virtualRows[i].Current.length > 0) {
    //            virtualRows[i].Current[0].d1 = i + updatedN * 1000;
    //        }
    //        uids.push(virtualRows[i].id);
    //    }

    //    var data = { head: { what: "ListUpdate" }, body: { ids: uids } };

    //    updatedN++;
    //    $timeout(rowUpdateMock, 30000);
    //    if (model.connectIsEnabled) {
    //        $log.debug("rows updated " + updatedN);
    //        $rootScope.$broadcast("transport:message-received", data);
    //    }
    //}

    $rootScope.$on("auth:authorized", function (e, message) {
        connect();
    });

    $rootScope.$on("auth:deauthorized", function (e, message) {
        disconnect(message.sessionId);
    });

    //// MOCK
    /*
    return {
        connect: connect,
        disconnect: disconnect,
        send: sendMock,
        getStatus: function () { return model; }
    };
    //// UNMOCK
    /*/

    return {
        connect: connect,
        disconnect: disconnect,
        send: send,
        getStatus: function () { return model; }
    };
    // */
});