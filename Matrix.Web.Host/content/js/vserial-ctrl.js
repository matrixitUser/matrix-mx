angular.module("app")
.controller("VSerialCtrl", function ($rootScope, $scope, $log, $modal, $transport, $base64, $timeout, $http, $q, $list, $helper, $filter, metaSvc) {//, data

    var listeners = [];
    var data = $scope.$parent.window.data;
    
    var accordion = {
        logInx: 0,
        open: false,
        log: [],
        makeLog: function (dir, b64) {
            var rec = { index: this.logInx };
            this.logInx++;
            rec.text = dir + ' ' + $helper.base64ToHex(b64);
            this.log.push(rec);
        }
    };

    //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  //  SERIAL  

    var serial = {
        state: "",
        ports: [],
        current: "",
        target: "",

        isReceiving: false,
        isTransmitting: false,

        isReceivingTimer: null,
        isTransmittingTimer: null,

        status: function () {
            send(new Message({ what: "com-status" }, {})).then(function (answer) {
                serial.state = answer.body.state;
                serial.target = answer.body.target;
                serial.ports.length = 0;
                for (var i = 0; i < answer.body.ports.length; i++) {
                    var port = answer.body.ports[i];
                    serial.ports.push(port);
                }
            });
        },

        open: function () {
            if (serial.current.length == 0 || this.state != "closed") return;

            this.state = "opening";
            send(new Message({ what: "com-open", connectionId: connection.id }, { port: serial.current })).then(function (answer) {
                serial.state = answer.body.state;
                serial.target = answer.body.target;
            }, function (err) {
                serial.state = "closed";
                serial.target = "";
            });
        },

        close: function () {
            if (this.state != "opened") $q.when();

            this.state = "closing";
            return send(new Message({ what: "com-close", connectionId: connection.id }, {})).then(function (answer) {
                serial.state = answer.body.state;
                serial.target = answer.body.target;
            }, function (err) {
                serial.state = "opened";
            });
        },

        send: function (bytes) {
            if (this.state != "opened") return;

            send(new Message({ what: "com-bytes", connectionId: connection.id }, { bytes: bytes }));
            this.indicateOut();
        },

        receive: function (bytes) {
            this.indicateIn();
            server.send(bytes);
        },

        indicateIn: function () {
            this.isReceiving = true;
            if (this.isReceivingTimer != null) {
                $timeout.cancel(this.isReceivingTimer);
            }
            this.isReceivingTimer = $timeout(function () { serial.isReceiving = false; }, 200);
        },

        indicateOut: function () {
            this.isTransmitting = true;
            if (this.isTransmittingTimer != null) {
                $timeout.cancel(this.isTransmittingTimer);
            }
            this.isTransmittingTimer = $timeout(function () { serial.isTransmitting = false; }, 200);
        }
    }

    //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  //  VCOM  

    var vcom = {
        state: "disconnected",
        host: "localhost",
        port: 9999,


        //соединить с сервером
        connect: function () {
            if (this.state === "disconnected") {
                this.state = "connecting";
                $log.debug("начало соединения");
                reconnect(connected);
            } else {
                $log.debug("начало соединения: соединение уже установлено");
            }
        },

        //разъединить связь с сервером
        disconnect: function () {
            if (vcom.state === "connected") {
                (function (self) {
                    return self.serial.close().then(function () {
                        self._close();
                        return {};
                    }, function (err) {
                        self._close();
                        $q.reject(err);
                    })
                })(this);
            } else {
                $log.debug("окончание соединения: связь уже разорвана");
                return $q.when({});
            }
        },

        _close: function () {
            this.state = "disconnecting";
            $log.debug("закрытие соединения");
            connection.stop();
        },

        serial: serial
    }

    var lastSelectedOrDefault = function () {
        var sels = $list.getSelectedIds();
        if (sels.length > 0) return sels[0];
        return "";
    }

    //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  //  SERVER  

    var server = {
        state: "disconnected",
        target: "",

        isReceiving: false,
        isTransmitting: false,

        isReceivingTimer: null,
        isTransmittingTimer: null,

        connect: function () {
            if (!this.current || !this.state == "disconnected") return;

            this.target = this.current;
            this.state == "connecting";
            $transport.send(new Message({ what: "poll-vcom-request" }, { what: "vcom-open", objectId: this.target }))
            .then(function (answer) {
                server.state = "connected";
            }, function (err) {
                server.state = "disconnected";
            });
        },

        disconnect: function () {
            if (!this.target || !this.state == "connected") return $q.when({});

            this.state == "disconnecting";
            return $transport.send(new Message({ what: "poll-vcom-request" }, { what: "vcom-close", objectId: this.target }))
            .then(function (answer) {
                server.state = "disconnected";
                server.target = "";
            }, function (err) {
                server.state = "connected";
            });
        },

        send: function (bytes) {//bytes IN BASE64
            if (!this.target || !bytes || bytes == "" || !this.state == "connected") return;
            $transport.send(new Message({ what: "poll-vcom-request" }, { what: "vcom-bytes", objectId: this.target, bytes: bytes }));
            this.indicateOut();
        },

        receive: function (body) {
            if (body.what == "vcom-bytes") {
                var base64 = body.bytes;
                $log.debug("пришли байты " + base64);
                this.indicateIn();
                serial.send(base64);
                accordion.makeLog('<-', base64);
            }
        },

        indicateIn: function () {
            this.isReceiving = true;
            if (this.isReceivingTimer != null) {
                $timeout.cancel(this.isReceivingTimer);
            }
            this.isReceivingTimer = $timeout(function () { server.isReceiving = false; }, 200);
        },

        indicateOut: function () {
            this.isTransmitting = true;
            if (this.isTransmittingTimer != null) {
                $timeout.cancel(this.isTransmittingTimer);
            }
            this.isTransmittingTimer = $timeout(function () { server.isTransmitting = false; }, 200);
        }
    }

    $scope.$watch(function () { return server.current; }, function () {
        server.currentName = "[" + server.current + "]";
        if (server.current == "") return;
        $list.getRowsCacheFiltered({ ids: [server.current] }).then(function (msg) {
            if (msg.rows.length > 0) {
                var row = msg.rows[0];
                server.currentName = (row.name + " " + row.pname).trim();
            }
        });
    });

    $scope.$watch(function () { return server.target; }, function () {
        server.targetName = "[" + server.target + "]";
        if (server.target == "") return;
        $list.getRowsCacheFiltered({ ids: [server.target] }).then(function (msg) {
            if (msg.rows.length > 0) {
                var row = msg.rows[0];
                server.targetName = (row.name + " " + row.pname).trim();
            }
        }); 
    }, true);

    $scope.$watch(function () { return server.targetStatus; }, function () {
        server.targetStatusImg = "error.png";
        server.targetStatusTitle = "";

        if (server.targetStatus) {
            var state = server.targetStatus;

            var dt = state.date;
            var date = dt ? $filter("date")(dt, "dd.MM.yy HH:mm:ss") : "";
            server.targetStatusImg = metaSvc.getImgByCode(state.code);

            var reason = metaSvc.getReasonByCode(state.code);
            if (state.code < 100) {
                server.targetStatusTitle = reason + (state.description ? " " + state.description : "") + " " + date;
            } else {
                server.targetStatusTitle = "Ошибка " + (state.code || "?") + " " + (reason || "неизвестная ошибка") + " " + date;
            }
        }
    }, true);

    var model = {
        window: $scope.$parent.window,
        modal: undefined,

        vcom: vcom,
        serial: serial,
        server: server,

        accordion: accordion
    }

    // VCOM

    model.toBase64 = function () {
        model.sendBase64 = $base64.encode(model.sendText);
    }

    // COM-SERVER

    //var con = $.connection.hub;
    //con.url = "http://127.0.0.1:8099/signalr";

    //con.start(function () {
    //    console.log('connection started!');
    //});


    var getHost = function () {
        return "http://" + vcom.host + ":" + vcom.port;
    }

    var connection = $.connection(getHost() + "/messageacceptor");

    //прием сообщений (возбуждается событие)

    //SERIAL-RECEIVE
    connection.received(function (message) {
        $log.debug("получено сообщение от COM-порта %s: %s", message.head.what, message.body.bytes);
        serial.receive(message.body.bytes);
        accordion.makeLog('->', message.body.bytes);
    });

    connection.error(function (error) {
        $log.error(error);
    });

    //disconnected -> connecting -> connected -> disconnecting
    //      ^-----------|   ^-----------|             |
    //      ^-----------------------------------------|

    connection.disconnected(function () {
        if (vcom.state == "connected") {
            $log.debug("разрыв соединения, повтор через 5 сек.");
            $timeout(function () {
                reconnect(connected);
            }, 5000); // restart connection after 5 seconds.
        } else {
            $timeout(function () {
                vcom.state = "disconnected";
                $log.debug("разрыв соединения");
            }, 0);
        }
    });

    var reconnect = function (callback) {//запускает прием/передачу сообщений 
        //if (vcom.state == "disconnected") {
        vcom.state = "connecting";
        connection.start().done(function () {
            if (callback) {
                $timeout(callback, 0);
            }
            ////уведомляет о установлении соединения
            //$rootScope.$broadcast("transport:connected");
            //var sessionId = $settings.getSessionId();
            //send(new Message({ what: "signal-bind" }, { sessionId: sessionId, connectionId: connection.id }));
        });
        //}
    }

    function connected() {
        vcom.state = "connected";
        $log.debug("соединение установлено");
        vcom.serial.status();
    }


    //отправка сообщения
    var send = function (message) {

        $log.debug("на сервер отправлено сообщение: %s", message.head.what);
        //if (notSid === undefined) {
        //    message.head.sessionId = $settings.getSessionId();
        //}

        return $http({
            url: getHost() + "/api/transport",
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            data: message
        }).then(function (data, status) {
            var msg = data.data;
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

    model.openServerConnection = function () {
        connect();
    }

    model.closeServerConnection = function () {
        disconnect();
    }

    //


    model.testComPort = function () {

    }

    //

    //SERVER-RECEIVE
    listeners.push($rootScope.$on("transport:message-received", function (e, message) {

        if (message.head.what == "poll-vcom-response") {
            server.receive(message.body);
        }

    }));

    //modal

    model.modalOpen = function () {
        server.current = lastSelectedOrDefault();
        model.modal = $modal.open({
            templateUrl: model.window.modalTemplateUrl,
            size: 'lg',
            scope: $scope
        });
    }

    model.close = function () {
        var __close = function () {
            model.modal.close();
            model.window.close();
        }

        $q.all([server.disconnect(), vcom.disconnect()]).then(__close, __close);
    }

    model.modalOpen();

    //

    $scope.model = model;

    $scope.$on('$destroy', function () {
        $log.debug("уничтожается vserial");
        for (var i = 0; i < listeners.length; i++) {
            listeners[i]();
        }
    });
});