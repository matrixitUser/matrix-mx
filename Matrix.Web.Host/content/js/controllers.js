/// <reference path="vendor/angular.js" />
/// <reference path="vendor/angular-route.js" />
'use strict';

angular.module("matrix")

.controller("adminCtrl", function ($scope, transportSvc) {
    var Renderer = function (canvas) {
        {
            var canvas = $(canvas).get(0);
            var ctx = canvas.getContext("2d");
            var particleSystem;

            var that = {
                init: function (system) {
                    //начальная инициализация
                    particleSystem = system;
                    particleSystem.screenSize(canvas.width, canvas.height);
                    particleSystem.screenPadding(80);
                    that.initMouseHandling();
                },

                redraw: function () {
                    //действия при перересовке
                    ctx.fillStyle = "magenta"; //белым цветом
                    ctx.fillRect(0, 0, canvas.width, canvas.height); //закрашиваем всю область

                    particleSystem.eachEdge( //отрисуем каждую грань
                     function (edge, pt1, pt2) { //будем работать с гранями и точками её начала и конца
                         ctx.strokeStyle = "rgba(0,0,0, .333)"; //грани будут чёрным цветом с некой прозрачностью
                         ctx.lineWidth = 1; //толщиной в один пиксель
                         ctx.beginPath();  //начинаем рисовать
                         ctx.moveTo(pt1.x, pt1.y); //от точки один
                         ctx.lineTo(pt2.x, pt2.y); //до точки два
                         ctx.stroke();
                     });

                    particleSystem.eachNode( //теперь каждую вершину
                     function (node, pt) {  //получаем вершину и точку где она
                         var w = 10;   //ширина квадрата
                         ctx.fillStyle = "orange"; //с его цветом понятно
                         ctx.fillRect(pt.x - w / 2, pt.y - w / 2, w, w); //рисуем
                         ctx.fillStyle = "black"; //цвет для шрифта
                         ctx.font = 'italic 13px sans-serif'; //шрифт
                         ctx.fillText(node.name, pt.x + 8, pt.y + 8); //пишем имя у каждой точки
                     });
                },

                initMouseHandling: function () { //события с мышью
                    var dragged = null;   //вершина которую перемещают
                    var handler = {
                        clicked: function (e) { //нажали
                            var pos = $(canvas).offset(); //получаем позицию canvas
                            var _mouseP = arbor.Point(e.pageX - pos.left, e.pageY - pos.top); //и позицию нажатия кнопки относительно canvas
                            dragged = particleSystem.nearest(_mouseP); //определяем ближайшую вершину к нажатию
                            if (dragged && dragged.node !== null) {
                                dragged.node.fixed = true; //фиксируем её
                            }
                            $(canvas).bind('mousemove', handler.dragged); //слушаем события перемещения мыши
                            $(window).bind('mouseup', handler.dropped);  //и отпускания кнопки
                            return false;
                        },
                        dragged: function (e) { //перетаскиваем вершину
                            var pos = $(canvas).offset();
                            var s = arbor.Point(e.pageX - pos.left, e.pageY - pos.top);

                            if (dragged && dragged.node !== null) {
                                var p = particleSystem.fromScreen(s);
                                dragged.node.p = p; //тянем вершину за нажатой мышью
                            }

                            return false;
                        },
                        dropped: function (e) { //отпустили
                            if (dragged === null || dragged.node === undefined) return; //если не перемещали, то уходим
                            if (dragged.node !== null) dragged.node.fixed = false; //если перемещали - отпускаем
                            dragged = null; //очищаем
                            $(canvas).unbind('mousemove', handler.dragged); //перестаём слушать события
                            $(window).unbind('mouseup', handler.dropped);
                            _mouseP = null;
                            return false;
                        }
                    }
                    // слушаем события нажатия мыши
                    $(canvas).mousedown(handler.clicked);
                },

            }
            return that;
        }
    }

    $scope.upd = function () {

        transportSvc.sendMessage2(new Message({ what: "graph-load" }, { foo: 1 }), function (message) {
            var sys = arbor.ParticleSystem(1000); // создаём систему
            sys.parameters({ gravity: true }); // гравитация вкл
            sys.renderer = Renderer("#viewport"); //начинаем рисовать в выбраной области

            var nodes = message.Argument.nodes;
            var relations = message.Argument.relations;

            for (var i = 0; i < nodes.length; i++) {
                var node = nodes[i];
                sys.addNode(node.id, node);
            }

            for (var i = 0; i < relations.length; i++) {
                var relation = relations[i];
                sys.addEdge(relation.s, relation.e);
            }
        });

        //var sys = arbor.ParticleSystem(1000); // создаём систему
        //sys.parameters({ gravity: true }); // гравитация вкл
        //sys.renderer = Renderer("#viewport"); //начинаем рисовать в выбраной области

        //sys.addEdge('a', 'b');
        //sys.addEdge('a', 'c');
        //sys.addEdge('a', 'd');
        //sys.addEdge('a', 'e');
        //sys.addNode('e', { alone: true, mass: .25 });

        //sys.addNode("test1", { name: "foo" });
        //sys.addNode("test2", { name: "foo2" });
        //sys.addEdge("test1", "test2");
    };
})

.service("clipboardSvc", function () {
    var clipboard = {
        rows: []
    }

    var pushRows = function (rows) {
        clipboard.rows = rows;
    };

    var get = function () {
        return clipboard;
    }

    return {
        get: get,
        pushRows: pushRows
    };
})

/**
 * расчет хеша (пароля например),
 * сюда добавлять всякие хелперы
 */
.service("helperSvc", function () {
    var md5 = function (string) {
        var hash = 0, i, chr, len;
        if (string.length == 0) return hash;
        for (i = 0, len = string.length; i < len; i++) {
            chr = string.charCodeAt(i);
            hash = ((hash << 5) - hash) + chr;
            hash |= 0; // Convert to 32bit integer
        }
        return hash;
    };

    return {
        md5: md5
    };
})

/**
 * редакторы 
 */
.service("editorsSvc", function ($modal, $rootScope, transportSvc, listSvc, $templateCache, jsPanelSvc) {
    /**
     * редактор параметров
     * @tube { id, deviceType }
     */
    var editParameters = function (tube) {

        transportSvc.sendMessage2(new Message({ what: "parameters-get" }, { tubeId: tube.id }), function (message) {

            var modal = $modal({
                template: "editParametersTmpl.html",
                show: true,
                animation: "am-fade-and-slide-top",
                keyboard: true
            });

            modal.$scope.close = function () {
                modal.hide();
            }

            modal.$scope.selected = message.body.parameters[0];

            modal.$scope.calculations = [{
                label: "Средний",
                type: "Average"
            }, {
                label: "Нет расчета",
                type: "NotCalculated"
            }, {
                label: "Итого",
                type: "Total"
            }];

            modal.$scope.parameters = message.body.parameters;
            modal.$scope.tags = message.body.tags;
            modal.$scope.groups = message.body.groups;

            modal.$scope.copyForAll = false;

            modal.$scope.name = message.body.name;
            modal.$scope.save = function (name) {

                var tubeIds = [
                    tube.id
                ];

                var selecteds = listSvc.getSelecteds();

                if (modal.$scope.copyForAll) {
                    for (var i = 0; i < selecteds.length; i++) {
                        var selected = selecteds[i];
                        if (selected.targetId !== tube.id && selected.deviceType === tube.deviceType) {
                            tubeIds.push(selected.targetId);
                        }
                    }
                }

                transportSvc.sendMessage2(new Message({ what: "parameters-save" }, { parameters: modal.$scope.parameters, tubeIds: tubeIds }), function (message) { });
                modal.hide();
            }
        });
    }

    var editFolder = function (folder) {
        var modal = $modal({
            template: "editGroupTmpl.html",
            show: true,
            animation: "am-fade-and-slide-top",
            keyboard: true
        });

        modal.$scope.close = function () {
            modal.hide();
        }

        modal.$scope.name = folder.name;
        modal.$scope.save = function (name) {
            if (folder.type === "all") {
                return;
            }

            transportSvc.sendMessage2(new Message({ what: "group-save" }, {
                isNew: false,
                id: folder.id,
                name: name
            }), function (message) {

            });
            modal.hide();
        }
    };

    /**
     * настройки строки
     * @row { areaId, tubeId, connectionId }
     */
    var editOptions = function (ids) {
        transportSvc.sendMessage2(new Message({ what: "row-get" }, { ids: ids }), function (message) {
            proccessOptions(message.body.model);
        });
    }

    var newRow = function () {
        //var model = [{
        //    type: "Area",
        //    name: "новый",
        //    city: ""
        //}];
        //proccessOptions(model);
        transportSvc.sendMessage2(new Message({ what: "list-get-new" }), function (msg) {
            var list = msg;
        });
    }

    var proccessOptions = function (model) {
        var $scope = $rootScope.$new();

        $scope.model = model;

        $scope.save = function (name) {
            transportSvc.sendMessage2(new Message({ what: "row-save" }, {
                model: $scope.model
            }), function (message) { });
        }

        $scope.current = $scope.model[0];

        jsPanelSvc.show($scope, {
            selector: "#m-maximize",
            title: "Настройки",
            position: "center",

            size: { width: 500, height: 500 },
            overflow: "scroll",
            toolbarFooter: $templateCache.get("footer2Tmpl.html"),
            content: $templateCache.get("editRowTmpl.html")
        });

        $scope.addCleaner($scope.save);
    }

    var editNotes = function (row) {
        transportSvc.sendMessage2(new Message({ what: "notes-get" }, { tubeId: row.tubeId }), function (message) {

            var modal = $modal({
                template: "notesTmpl.html",
                show: true,
                animation: "am-fade-and-slide-top",
                keyboard: true
            });

            modal.$scope.close = function () {
                modal.hide();
            }

            modal.$scope.history = message.body.notes;
            modal.$scope.new = {
                message: "",
                date: new Date(),
                tubeId: row.tubeId,
                end: new Date()
            };

            modal.$scope.endNote = function (note) {
                note.end = new Date();
            }

            modal.$scope.add = function () {
                transportSvc.sendMessage2(new Message({ what: "note-add" }, {
                    start: modal.$scope.new.date,
                    message: modal.$scope.new.message,
                    tubeId: modal.$scope.new.tubeId
                }), function (message) {
                    modal.$scope.history.splice(0, 0, modal.$scope.new);
                    modal.$scope.new = {
                        message: "",
                        date: new Date(),
                        tubeId: row.tubeId
                    };
                });
            }
        });
    };

    //var pollTubes = function (rows) {

    //    var $scope = $rootScope.$new();
    //    $scope.rows = rows;

    //    jsPanelSvc.show($scope, {
    //        selector: "#m-maximize",
    //        title: "Опрос",
    //        position: "center",

    //        size: { width: 900, height: 500 },
    //        //bootstrap: "primary",
    //        overflow: "scroll",
    //        toolbarFooter: $templateCache.get("footer1Tmpl.html"),
    //        content: $templateCache.get("actionsTmpl.html")
    //    });
    //};

    var editDeviceTypes = function () {
        var $scope = $rootScope.$new();
        //$scope.uploader = new FileUploader();
        $scope.deviceTypes = [{
            name: "Ирвис",
            driver: {}
        }, {
            name: "ЕК270",
            driver: {}
        }];
        $scope.selected = $scope.deviceTypes[0];
        jsPanelSvc.show($scope, {
            selector: "#m-maximize",
            title: "Типы вычислителей",
            position: "center",
            size: { width: 900, height: 500 },
            overflow: "scroll",
            toolbarFooter: $templateCache.get("footer1Tmpl.html"),
            content: $templateCache.get("deviceTypesTmpl.html")
        });
        $scope.addCleaner(function () {
            var x = $scope.deviceTypes;
        });
    }

    return {
        editParameters: editParameters,
        editFolder: editFolder,
        editOptions: editOptions,
        editNotes: editNotes,
        pollTubes: pollTubes,
        editDeviceTypes: editDeviceTypes,
        newRow: newRow
    };
})

.controller("editTubeCtrl", function ($scope) {
    alert("i am in!");
})


/**
 * авторизация, показ окна при нехватке прав,
 * логаут, имя пользователя
 */
.service("loginSvc", function ($modal, $log, transportSvc, $rootScope, storageSvc) {

    var user = {};

    /**
     * отправка логина или сессии
     */
    var sendAuthInfo = function (info, callback) {

        var request;
        if ("sessionId" in info) {
            request = new Message({ what: "login-by-session" }, {
                sessionId: info.sessionId
            });
        } else if ("login" in info) {
            request = new Message({ what: "login-by-login" }, {
                login: info.login === undefined ? "" : info.login,
                password: info.password === undefined ? "" : info.password
            });
        }

        transportSvc.sendMessage2(request, callback, true);
    }

    /**
     * показ окна авторизации
     */
    var displayWindow = function (error) {
        setTimeout(function () {
            var modal = $modal({
                template: "loginTmpl.html",
                show: false,
                animation: "am-fade-and-slide-top",
                backdrop: "static",
                keyboard: false
            });

            //modal.$scope.login = "";
            //modal.$scope.password = "";
            modal.$scope.error = error;

            modal.$scope.ok = function () {
                sendAuthInfo({
                    login: $("#login").val(),
                    password: $("#password").val()
                }, function (message) {
                    if (message.head.what === "login-error") {
                        modal.$scope.error = message.body.message;
                        //modal.$scope.$apply(function () {
                        //    modal.$scope.error = message.Argument.message;
                        //});
                    } else {
                        modal.hide();
                        user = message.body.user;
                        raiseAuthorised(message.body.sessionId);
                    }
                });
            };

            modal.$promise.then(modal.show);
        }, 2000);
    }

    /**
     * уведомление об успешной авторизации
     */
    var raiseAuthorised = function (sessionId) {
        localStorage.setItem(SESSION_ID_KEY, sessionId);
        transportSvc.connect();
        $rootScope.$broadcast("authorized");
    };

    /**
     * переаторизация
     */
    var relogin = function () {
        var sessionId = localStorage.getItem(SESSION_ID_KEY);
        if (sessionId === undefined || sessionId === "") {
            displayWindow("");
            return;
        }

        sendAuthInfo({ sessionId: sessionId }, function (message) {
            if (message.head.what === "login-confirmed") {
                user = message.body.user;
                raiseAuthorised(message.body.sessionId);
            } else {
                displayWindow(message.body.message);
            }
        });
    };

    /**
     * запуск приложения
     */
    $rootScope.$on("app-init", function () {
        relogin();
    });

    var logout = function () {
        transportSvc.sendMessage2(new Message({ what: "login-close-session" }, {
            sessionId: localStorage.getItem(SESSION_ID_KEY)
        }), function (message) { });
        localStorage.setItem(SESSION_ID_KEY, "");
        relogin();
    };

    var getSessionId = function () {
        return localStorage.getItem(SESSION_ID_KEY);
    }

    var getUser = function () {
        return user;
    }

    return {
        logout: logout,
        getSessionId: getSessionId,
        getUser: getUser
    };
})

/**
 * действия по отношению к объектам,
 * опросы, АТ-команды и т.п.
 */
.controller("actionCtrl", function ($scope, listSvc, transportSvc) {
    var now = new Date();
    $scope.end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), 0, 0, 0);
    $scope.start = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0, 0);
    $scope.onlyHoles = true;

    var getIds = function () {
        listSvc.getObjectIdsProvider()
        var rows = listSvc.getSelectedRows();
        var ids = {
            targets: [],
            inputs: [],
            outputs: []
        };
        for (var id in rows.targets) {
            ids.targets.push(id);
        }
        for (var id in rows.inputs) {
            ids.inputs.push(id);
        }
        for (var id in rows.outputs) {
            ids.outputs.push(id);
        }
        return ids;
    }

    $scope.days = function () {
        var targets = getIds().targets;
        transportSvc.sendMessage2(new Message({ what: "survey-days" }, {
            targets: targets,
            start: $scope.start,
            end: $scope.end,
            "only-holes": $scope.onlyHoles
        }), function (message) { });
    }

    $scope.hours = function () {
        var targets = getIds().targets;
        transportSvc.sendMessage2(new Message({ what: "survey-hours" }, {
            targets: targets,
            start: $scope.start,
            end: $scope.end,
            "only-holes": $scope.onlyHoles
        }), function (message) { });
    }

    $scope.currents = function () {
        var targets = getIds().targets;
        transportSvc.sendMessage2(new Message({ what: "survey-current" }, {
            targets: targets
        }), function (message) { });
    }

    $scope.ping = function () {
        var targets = getIds().targets;
        transportSvc.sendMessage2(new Message({ what: "survey-ping" }, {
            targets: targets
        }), function (message) { });
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
})

/**
 * сохраняет состояние
 */
.service("logSvc", function ($rootScope, listSvc) {
    var messages = [];
    var getMessages = function () {
        return messages;
    }

    var applyMessages = function () {

    };

    var objectIdsProvider = function () {
        return null;
    };

    var setObjectIdsProvider = function (value) {
        objectIdsProvider = value;
    }

    var getObjectIdsProvider = function () {
        return objectIdsProvider;
    }

    $rootScope.$on("messageReceived", function (e, message) {
        if (message.head.what === "log") {
            var records = message.body.messages;
            var rows = listSvc.getSelectedRows();
            for (var i = 0; i < records.length; i++) {
                var record = records[i];
                if (rows.targets[record.objectId] || rows.outputs[record.objectId] || rows.inputs[record.objectId]) {
                    var newMessage = {
                        message: record.message,
                        date: record.date,
                        object: record.obj,
                        objectId: record.objectId
                    };
                    messages.splice(0, 0, newMessage);
                }
            }
            $rootScope.$broadcast("logChanged", messages);
        }
    });

    var clear = function () {
        messages.length = 0;
        $rootScope.$broadcast("logChanged", messages);
    }

    return {
        clear: clear,
        getMessages: getMessages,
        getObjectIdsProvider: getObjectIdsProvider,
        setObjectIdsProvider: setObjectIdsProvider
    };
})

/**
 * логи опросов, действий пользователей и т.п. 
 */
.controller("logCtrl", function ($scope, $rootScope, $log, logSvc) {
    $scope.messages = logSvc.getMessages();

    var columns = [{
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
        width: "30%",
        type: "string",
        resizable: true
    }, {
        field: "message",
        displayName: "Сообщение",
        width: "70%",
        type: "string",
        resizable: true
    }];

    $scope.test = "test111";

    //настройки грида
    //см. http://angular-ui.github.io/ng-grid/
    $scope.options = {
        data: "messages",
        columnDefs: columns,
        enableColumnResizing: true,
        enableColumnReordering: true
    };

    $rootScope.$on("logChanged", function (e, messages) {
        if (messages.length === 0) {
            $scope.messages.length = 0;
        } else {
            $scope.$apply(function () {
                $scope.messages = messages;
            });
        }
    });

    $scope.menuOptions = [
        {
            caption: "Help",
            action: function ($itemScope) {
                $scope.clear();
            }
        },
        null,
        {
            caption: "Help",
            action: function ($itemScope) {
                $scope.clear();
            }
        },
    ];

    $scope.clear = function () {
        logSvc.clear();
    }
})

/**
 * биндинг нативного HTML, с поддержкой скриптов и т.п.
 */
.directive("html", function ($compile) {
    return function ($scope, element, attrs) {
        $scope.$watch(attrs.html, function (newValue, oldValue) {
            if (newValue) {
                element.html(newValue);
                $compile(element.contents())($scope);
            }
        });
    }
})

.controller("alarmsCtrl", function ($scope, ngAudio, $rootScope, transportSvc, $templateCache, $log) {

    $scope.limits = [];

    $scope.sound = ngAudio.load("./Content/sounds/song.mp3"); // returns ngAudioObject

    //перезагрузка списка при авторизации
    $rootScope.$on("authorized", function (e) {
        transportSvc.sendMessage2(new Message({ what: "limits-get" }), function (message) {
            if ($scope.limits.length != message.body.limits.length) {
                $scope.sound.play();
            }
            $scope.limits = message.body.limits;
        });
    });

    $scope.refresh = function () {
        transportSvc.sendMessage2(new Message({ what: "limits-get" }), function (message) {
            if ($scope.limits.length != message.body.limits.length) {
                $scope.sound.play();
            }
            $scope.limits = message.body.limits;
        });
    };

    var limitsMapping = {};

    $rootScope.$on("messageReceived", function (e, message) {
        if (message.What === "limit-changed") {
            var lim = message.Argument.limits;
            var play = false;
            for (var j = 0; j < lim.length; j++) {
                var nw = lim[j];
                var isNew = true;
                for (var i = 0; i < $scope.limits.length; i++) {
                    var old = $scope.limits[i];
                    if (old.id === nw.id) {
                        $scope.$apply(function () {
                            old.status = nw.status;
                            old.changeDate = nw.changeDate;
                        });
                        isNew = false;
                        break;
                    }
                }
                if (isNew) {
                    $scope.limits.push(nw);
                    play = true;
                }
            }
            if (play) {
                $scope.sound.play();
            }
        }
    });

    //настройки грида
    //см. http://angular-ui.github.io/ng-grid/
    $scope.options = {
        data: "limits",
        columnDefs: [{
            field: "date",
            displayName: "Дата",
            cellTemplate: $templateCache.get("dateCellTmpl.html"),
            width: "160",
            type: "string"
        }, {
            field: "objectName",
            displayName: "Объект",
            width: "200",
            type: "string"
        }, {
            field: "message",
            displayName: "Сообщение",
            //cellTemplate: $templateCache.get("areaCellTmpl.html"),
            width: "400",
            type: "string"
        }, {
            field: "status",
            displayName: "Состояние",
            cellTemplate: $templateCache.get("limitState.html"),
            width: "160",
            type: "number"
        }],
        enableColumnResizing: true,
        enableColumnReordering: true,
        multiSelect: false
    };

    //модель для ячеек
    $scope.cellModel = {
        apply: function (limit) {
            $log.debug("на квитирование %s", limit.message);
            limit.status = 1;
            limit.changeDate = new Date();
            transportSvc.sendMessage2(new Message({ what: "limits-save" }, { "limits": limit }), function () { });
        }
    }

    $scope.apply = function (limit) {
        $log.debug("на квитирование %s", limit.message);
        limit.status = 1;
        limit.changeDate = new Date();
        transportSvc.sendMessage2(new Message({ what: "limits-save" }, { "limits": limit }), function () { });
    }
})

/**
 * хранит состояние отчета,
 * типы, выбранный, даты, тело отчета
 */
.service("reportSvc", function ($rootScope, transportSvc, $log, $base64, $modal, $templateCache, jsPanelSvc) {
    //список отчетов
    var reports = [];
    var getReports = function (types) {
        if (!types) return reports;
        for (var typeIndex = 0; typeIndex < types.length; typeIndex++) {
            var type = types[typeIndex];

        }
        return reports;
    }

    var getReport = function (reportId) {
        for (var i = 0; i < reports.length; i++) {
            var report = reports[i];
            if (report.id.toLocaleLowerCase() === reportId.toLocaleLowerCase()) {
                return report;
            }
        }
        return null;
    };

    //перезагрузка списка при авторизации
    $rootScope.$on("authorized", function (e) {
        //transportSvc.sendMessage(new Message("reports-get"));
        transportSvc.sendMessage2(new Message({ what: "reports-get" }), function (message) {
            reports = message.body.reports;
            $rootScope.$broadcast("reportsUpdated", reports);
        });
    });

    var build = function (reportId, start, end, objectIds, callback) {
        //var objectIds = objectIdsProvider();

        if (objectIds !== null && objectIds.length === 0) {
            return false;
        }
        transportSvc.sendMessage2(new Message({ what: "report-build" }, {
            start: start,
            end: end,
            report: reportId,
            targets: objectIds
        }), function (message) {
            var body = message.body.reportBody;
            //$rootScope.$broadcast("reportsBuilded", body);
            callback(body);
        });
        return true;
    }

    var exportToPdf = function (body) {
        transportSvc.sendMessage2(new Message({ what: "report-export" }, {
            type: "pdf",
            text: body
        }), function (message) {
            var bytes = message.body.bytes;
            var url = "data:application/pdf;base64," + bytes;
            window.open(url, "_blank");
        });
    };

    var exportToXls = function (body) {
        var uri = "data:application/vnd.ms-excel;base64,"
        var template = "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body>{table}</body></html>"
        var b64 = function (s) { return $base64.encode(unescape(encodeURIComponent(s))) }
        var format = function (s, c) { return s.replace(/{(\w+)}/g, function (m, p) { return c[p]; }) }
        var ctx = { worksheet: "Отчет", table: body }
        uri = uri + b64(format(template, ctx))
        window.open(uri, "_blank");
    }

    var objectIdsProvider = function () {
        return null;
    };

    var setObjectIdsProvider = function (value) {
        objectIdsProvider = value;
    }

    var getObjectIdsProvider = function () {
        return objectIdsProvider;
    }

    var showReport = function (reportId) {
        rid = reportId.toLocaleLowerCase();

        var $scope = $rootScope.$new();

        jsPanelSvc.show($scope, {
            selector: "#m-maximize",
            title: "Отчет",
            position: "center",
            overflow: "scroll",
            size: { width: 800, height: 500 },
            //bootstrap: "primary",
            toolbarFooter: $templateCache.get("footer1Tmpl.html"),
            content: $templateCache.get("reportWindow.html")
        });
    };

    var rid;
    var getReportId = function () {
        return rid;
    };

    return {
        getReports: getReports,
        getReport: getReport,
        build: build,
        exportToPdf: exportToPdf,
        exportToXls: exportToXls,
        setObjectIdsProvider: setObjectIdsProvider,
        getObjectIdsProvider: getObjectIdsProvider,
        showReport: showReport,
        getReportId: getReportId,
        close: close
    }
})

/**
 * опции отчета, диапазон дат
 */
.controller("reportCtrl", function ($scope, $rootScope, reportSvc, listSvc, $log, $base64) {

    var objectIds = [];
    var rows = listSvc.getSelectedRows();

    for (var id in rows.targets) {
        objectIds.push(id);
    }

    $scope.reportText = "";

    $scope.wait = true;
    $scope.reportId = reportSvc.getReportId();
    $scope.noObjects = false;

    var report = reportSvc.getReport($scope.reportId);
    if (report !== null) {
        switch (report.diapasone) {
            case "month":
                var d = new Date();
                var month = d.getMonth();
                var day = d.getDate();
                if (day <= 3) {
                    month = d.getMonth() - 1;
                    if (month < 0) month = 11;
                }
                $scope.start = new Date(d.getFullYear(), month, 1);
                $scope.end = d;
                break;
            case "day":
                var d = new Date();
                $scope.start = new Date(d.getFullYear(), d.getMonth(), d.getDate());
                $scope.end = d;
                break;
            default:
                $scope.start = new Date();
                $scope.end = new Date();
                break;
        }
    }

    $scope.update = function () {
        if ($scope.reportId) {
            $scope.wait = true;
            //$log.debug("даты отчета %s %s", start.toString(), end.toString());
            $scope.noObjects = !reportSvc.build($scope.reportId, $scope.start, $scope.end, objectIds, function (body) {
                $scope.wait = false;
                $scope.reportText = body;
            });
        }
    }

    $scope.update();

    $scope.savePdf = function () {
        reportSvc.exportToPdf($scope.reportText);
    };

    $scope.toExcel = function () {
        reportSvc.exportToXls($scope.reportText);
    };
})

.controller("mnemoCtrl", function ($scope, transportSvc, $rootScope, $templateCache, $log, reportSvc, editorsSvc, ngAudio) {

    $scope.limits = [];

    $scope.sound = ngAudio.load("./media/song.mp3"); // returns ngAudioObject

    //перезагрузка списка при авторизации
    $rootScope.$on("authorized", function (e) {
        transportSvc.sendMessage2(new Message({ what: "limits-get" }), function (message) {
            if ($scope.limits.length != message.body.limits.length) {
                $scope.sound.play();
            }
            $scope.limits = message.body.limits;
            //$scope.$apply(function () {

            //});
        });
    });

    $scope.refresh = function () {
        transportSvc.sendMessage2(new Message({ what: "limits-get" }), function (message) {
            if ($scope.limits.length != message.body.limits.length) {
                $scope.sound.play();
            }
            $scope.limits = message.body.limits;
        });
    };

    var limitsMapping = {};

    $rootScope.$on("messageReceived", function (e, message) {

        if (message.head.what === "limit-changed") {
            var lim = message.body.limits;
            var play = false;
            for (var j = 0; j < lim.length; j++) {
                var nw = lim[j];
                var isNew = true;
                for (var i = 0; i < $scope.limits.length; i++) {
                    var old = $scope.limits[i];
                    if (old.id === nw.id) {
                        $scope.$apply(function () {
                            old.status = nw.status;
                            old.changeDate = nw.changeDate;
                        });
                        isNew = false;
                        break;
                    }
                }
                if (isNew) {
                    $scope.limits.push(nw);
                    play = true;
                }
            }
            if (play) {
                $scope.sound.play();
            }
        }

        if (message.head.what === "current-changed") {
            var cur = message.body.currents;
            $scope.$apply(function () {
                for (var i = 0; i < cur.length; i++) {
                    var c = cur[i];
                    if (!$scope.cur[c.tubeId]) {
                        $scope.cur[c.tubeId] = {};
                    }
                    $scope.cur[c.tubeId][c.parameter] = c.value;
                }
            });
        }
    });

    $scope.cur = {};

    $scope.cur['486dbc52-aadc-4fd6-bd13-91e37495bba9'] = {};
    $scope.cur['486dbc52-aadc-4fd6-bd13-91e37495bba9']['ВНР по 1-му каналу'] = 3.4561;
    $scope.cur['486dbc52-aadc-4fd6-bd13-91e37495bba9']['V по 2-му каналу'] = 1425.457;
    $scope.cur['486dbc52-aadc-4fd6-bd13-91e37495bba9']['V по 1-му каналу'] = 0.2574;

    $scope.foobar = 33;

    //настройки грида
    //см. http://angular-ui.github.io/ng-grid/
    $scope.options = {
        data: "limits",
        columnDefs: [{
            field: "date",
            displayName: "Дата",
            cellTemplate: $templateCache.get("dateCellTmpl.html"),
            width: "160",
            type: "string"
        }, {
            field: "message",
            displayName: "Сообщение",
            //cellTemplate: $templateCache.get("areaCellTmpl.html"),
            width: "400",
            type: "string"
        }, {
            field: "status",
            displayName: "Состояние",
            cellTemplate: $templateCache.get("limitState.html"),
            width: "160",
            type: "number"
        }],
        enableColumnResizing: true,
        enableColumnReordering: true,
        multiSelect: false
    };

    reportSvc.setObjectIdsProvider(function () {
        var ids = [
            "486dbc52-aadc-4fd6-bd13-91e37495bba9"
        ];
        return ids;
    });

    $scope.menuOptions = [
       {
           caption: "График V1",
           icon: "./img/report.png",
           action: function ($itemScope) {
               //$scope.clear();

               reportSvc.showReport("13CC0E4A-C799-47D2-B389-5162FD3A6C26");
           }
       }, {
           caption: "График Q",
           icon: "./img/report.png",
           action: function ($itemScope) {
               //$scope.clear();

               reportSvc.showReport("A6C0B375-DD8A-455A-B062-00D5A39E2555");
           }
       }, {
           caption: "Текущие",
           icon: "./img/report.png",
           action: function ($itemScope) {
               reportSvc.showReport("5AFAB170-7538-4A0F-A02D-913F3AB243CD");
           }
       }, {
           caption: "Расход Q",
           icon: "./img/report.png",
           action: function ($itemScope) {
               reportSvc.showReport("27EEF808-58D0-46DE-BF04-205683D4DFC5");
           }
       }, {
           caption: "Параметры",
           icon: "./img/tags.png",
           action: function ($itemScope) {
               editorsSvc.editParameters({
                   id: "486dbc52-aadc-4fd6-bd13-91e37495bba9",
                   deviceType: ""
               });
           }
       }
    ];

    $scope.p1 = 23;

    //модель для ячеек
    $scope.cellModel = {
        apply: function (limit) {
            $log.debug("на квитирование %s", limit.message);
            limit.status = 1;
            limit.changeDate = new Date();
            transportSvc.sendMessage2(new Message({ what: "limits-save" }, { "limits": limit }), function (message) { });
        }
    }

    $scope.apply = function (limit) {
        $log.debug("на квитирование %s", limit.message);
        limit.status = 1;
        limit.changeDate = new Date();
        transportSvc.sendMessage2(new Message({ what: "limits-save" }, { "limits": limit }), function (message) { });
    }

})

.controller("shellCtrl", function ($scope, $rootScope, loginSvc, editorsSvc) {

    $scope.logout = function () {
        loginSvc.logout();
    };

    $scope.userName = "";

    $rootScope.$on("authorized", function () {
        $scope.userName = loginSvc.getUser().login + "(" + loginSvc.getUser().name + ")";
    });

    $scope.showDeviceTypes = function () {
        editorsSvc.editDeviceTypes();
    };
})

.service("usersSvc", function (transportSvc, $rootScope, $modal, $log) {
    //перезагрузка списка при авторизации
    $rootScope.$on("authorized", function (e) {
        transportSvc.sendMessage2(new Message({ what: "users-get" }), function (message) {
            roots = message.body.tree;
        });
    });

    var roots = [];

    var addedGroups = [];
    var removedGroups = [];

    var wrap = function (src, selection) {
        var wrapped = {
            id: src.id,
            name: src.name,
            type: src.type,
            parentId: src.parentId,
            locked: src.locked,
            children: []
        }

        wrapped.allowWrite = selection.write[wrapped.id];
        wrapped.allowRead = selection.read[wrapped.id];

        wrapped.originalWrite = wrapped.allowWrite;
        wrapped.originalRead = wrapped.allowRead;

        wrapped.isDirty = function () {
            return wrapped.originalRead !== wrapped.allowRead ||
                wrapped.originalWrite !== wrapped.allowWrite;
        }

        for (var i = 0; i < src.children.length; i++) {
            var child = wrap(src.children[i], selection);
            child.parent = wrapped;
            wrapped.children.push(child);
        }

        wrapped.onReadChange = function () {
            if (!wrapped.allowRead) {
                wrapped.allowWrite = false;
            }
        }

        wrapped.onWriteChange = function () {
            if (wrapped.allowWrite) {
                wrapped.allowRead = true;
            }
        }

        return wrapped;
    }

    var showWindow = function (selection) {
        var modal = $modal({
            template: "editRightsTmpl.html",
            show: true,
            animation: "am-fade-and-slide-top",
            keyboard: true
        });
        var wrappedRoots = [];
        for (var i = 0; i < roots.length; i++) {
            wrappedRoots.push(wrap(roots[i], selection));
        }

        //var collapseAll = function () {
        //    var scope = angular.element(document.getElementById("tree-root")).scope().$treeScope;
        //    scope.collapseAll();
        //};

        /**
         * поиск кодов групп по указанному критерию
         * @roots узлы дерева
         * @predicate условие отбора
         * @return
         */
        var findIdsInTree = function (roots, predicate) {
            var result = [];
            for (var i = 0; i < roots.length; i++) {
                var root = roots[i];
                if (predicate(root)) {
                    result.push({
                        groupId: root.id,
                        allowRead: root.allowRead,
                        allowWrite: root.allowWrite
                    });
                }
                var children = findIdsInTree(root.children, predicate);
                for (var j = 0; j < children.length; j++) {
                    result.push(children[j]);
                }
            }
            return result;
        }

        modal.$scope.roots = wrappedRoots;
        modal.$scope.items = modal.$scope.roots;

        modal.$scope.treeOptions = {

        };

        modal.$scope.filter = "";
        modal.$scope.visible = function (item) {
            if (modal.$scope.filter && modal.$scope.filter.length > 0 &&
                item.name.toLocaleLowerCase().indexOf(modal.$scope.filter.toLocaleLowerCase()) == -1) {
                var childCheck = false;
                for (var i = 0; i < item.children.length; i++) {
                    childCheck |= modal.$scope.visible(item.children[i]);
                }
                return childCheck;
            }

            return true;
        };

        modal.$scope.name = selection.objectId;

        modal.$scope.close = function () {
            modal.hide();
        };
        modal.$scope.save = function () {
            var ids = findIdsInTree(wrappedRoots, function (item) { return item.isDirty(); });
            transportSvc.sendMessage2(new Message({ what: "acl-set" }, {
                objectId: selection.objectId,
                changes: ids
            }), function (message) { });
            modal.hide();
        }
    }

    var displayRights = function (objectId) {
        transportSvc.sendMessage2(new Message({ what: "acl-get" }, { objectId: objectId }), function (message) {
            var foo = message.body;
            showWindow(foo);
        });
    }

    /**
     * установка прав
     * @objectId объект
     * @writeGroups выбранные группы
     */
    var setAcl = function (objectId, groups) {

    };

    return {
        displayRights: displayRights,
        setAcl: setAcl
    }
})

.directive("reportList", function () {
    return {
        restrict: "E",
        translude: true,
        scope: {
            types: "="
        },
        controller: function ($scope, reportSvc, $modal, $log) {
            var upd = function (reports) {
                var filtered = [];
                for (var i = 0; i < reports.length; i++) {
                    var report = reports[i];
                    for (var k = 0; k < report.types.length; k++) {
                        var reportType = report.types[k];
                        for (var j = 0; j < $scope.types.length; j++) {
                            var type = $scope.types[j];
                            if (reportType === type) {
                                filtered.push(report);
                            }
                        }
                    }
                }
                return filtered;
            }

            $scope.reports = upd(reportSvc.getReports());

            $scope.$on("reportsUpdated", function (e, reports) {
                $scope.reports = upd(reports);
            });

            $scope.showReport = function (reportId) {
                reportSvc.showReport(reportId);
            };
        },
        templateUrl: "views/report-list.html"
    };
})

/**
 * позволяет распечатать указанный див
 */
.directive("printDiv", function () {
    return {
        restrict: "A",
        link: function (scope, element, attrs) {
            element.bind("click", function (evt) {
                evt.preventDefault();
                PrintElem(attrs.printDiv);
            });

            function PrintElem(elem) {
                PrintWithIframe($(elem).html());
            }

            function PrintWithIframe(data) {
                if ($('iframe#printf').size() == 0) {
                    $('html').append('<iframe style="display:none" id="printf" name="printf"></iframe>');  // an iFrame is added to the html content, then your div's contents are added to it and the iFrame's content is printed

                    var mywindow = window.frames["printf"];
                    mywindow.document.write(data);
                    //('<html><head><title></title><style>@page {margin: 25mm 0mm 25mm 5mm}</style>'  // Your styles here, I needed the margins set up like this
                    //            + '</head><body><div>'
                    //            + data
                    //            + '</div></body></html>');

                    $(mywindow.document).ready(function () {
                        mywindow.print();
                        setTimeout(function () {
                            $('iframe#printf').remove();
                        },
                        2000);  // The iFrame is removed 2 seconds after print() is executed, which is enough for me, but you can play around with the value
                    });
                }

                return true;
            }
        }
    };
})

.directive("onBeforePrint", function onBeforePrint($window, $rootScope, $timeout) {
    var beforePrintDirty = false;
    var listeners = [];

    var beforePrint = function () {
        if (beforePrintDirty) return;

        beforePrintDirty = true;

        if (listeners) {
            for (var i = 0, len = listeners.length; i < len; i++) {
                listeners[i].triggerHandler('beforePrint');
            }

            var scopePhase = $rootScope.$$phase;

            // This must be synchronious so we call digest here.
            if (scopePhase != '$apply' && scopePhase != '$digest') {
                $rootScope.$digest();
            }
        }

        $timeout(function () {
            // This is used for Webkit. For some reason this gets called twice there.
            beforePrintDirty = false;
        }, 100, false);
    };

    if ($window.matchMedia) {
        var mediaQueryList = $window.matchMedia('print');
        mediaQueryList.addListener(function (mql) {
            if (mql.matches) {
                beforePrint();
            }
        });
    }

    $window.onbeforeprint = beforePrint;

    return function (scope, iElement, iAttrs) {
        function onBeforePrint() {
            scope.$eval(iAttrs.onBeforePrint);
        }

        listeners.push(iElement);
        iElement.on('beforePrint', onBeforePrint);

        scope.$on('$destroy', function () {
            iElement.off('beforePrint', onBeforePrint);

            var pos = -1;

            for (var i = 0, len = listeners.length; i < len; i++) {
                var currentElement = listeners[i];

                if (currentElement === iElement) {
                    pos = i;
                    break;
                }
            }

            if (pos >= 0) {
                listeners.splice(pos, 1);
            }
        });
    };
})

.directive("ngContextMenu", function ($parse) {
    var renderContextMenu = function ($scope, event, options) {
        if (!$) { var $ = angular.element; }
        $(event.target).addClass("context");
        var $contextMenu = $("<div>");
        $contextMenu.addClass("dropdown clearfix");
        var $ul = $("<ul>");
        $ul.addClass("dropdown-menu");
        $ul.attr({ "role": "menu" });
        $ul.css({
            display: "block",
            position: "absolute",
            left: event.pageX + "px",
            top: event.pageY + "px"
        });
        angular.forEach(options, function (item, i) {
            var $li = $("<li>");
            if (item === null) {
                $li.addClass("divider");
            } else {
                var $a = $("<a>");
                $a.attr({ tabindex: "-1" });

                var $imgSpan = $("<span>");
                $imgSpan.css({
                    "margin-right": "6px"
                });
                $a.append($imgSpan);

                var $txtSpan = $("<span>");
                $a.append($txtSpan);
                $txtSpan.text(item.caption);

                if (item.icon) {
                    var $img = $("<img>");
                    $img.attr({ src: item.icon, height: "20px" });
                    $imgSpan.append($img);
                }

                $li.append($a);
                $li.on("click", function () {
                    $scope.$apply(function () {
                        item.action.call($scope, $scope);
                    });
                });
            }
            $ul.append($li);
        });
        $contextMenu.append($ul);
        $contextMenu.css({
            width: "100%",
            height: "100%",
            position: "absolute",
            top: 0,
            left: 0,
            zIndex: 9999
        });
        $(document).find("body").append($contextMenu);
        $contextMenu.on("click", function (e) {
            $(event.target).removeClass("context");
            $contextMenu.remove();
        }).on("contextmenu", function (event) {
            $(event.target).removeClass("context");
            event.preventDefault();
            $contextMenu.remove();
        });
    };
    return function ($scope, element, attrs) {
        element.on("contextmenu", function (event) {
            $scope.$apply(function () {
                event.preventDefault();
                var options = $scope.$eval(attrs.ngContextMenu);
                if (options instanceof Array) {
                    renderContextMenu($scope, event, options);
                } else {
                    throw '"' + attrs.ngContextMenu + '" not an array';
                }
            });
        });
    };
})

.directive("mxRights", function ($compile, $templateCache) {
    return {
        restrict: "E",
        template: "<button class='btn btn-primary' ng-click='show()'>Права</button>",
        replace: true,
        link: function ($scope, element, attrs) {
            $scope.show = function () {
                $scope.deviceTypes = [{
                    name: "Ирвис",
                    driver: {}
                }, {
                    name: "ЕК270",
                    driver: {}
                }];
                $scope.selected = $scope.deviceTypes[0];

                $scope.oid = attrs.objectId;

                var ha = $.jsPanel({
                    selector: "#m-maximize",
                    title: "Типы вычислителей",
                    position: "center",
                    size: { width: 900, height: 500 },
                    overflow: "scroll",
                    toolbarFooter: $templateCache.get("footer1Tmpl.html"),
                    content: "<h1>hello {{oid}}</h1>"
                });

                $scope.close = function () {
                    ha.close();
                };
                $scope.cancel = function () {
                    ha.close();
                };
                $compile(angular.element(ha[0]))($scope);
            }
        }
    };
})

.controller("skeletonCtrl", function ($scope, $window, $log) {
    //$scope.leftWidth = 300;
    //$scope.centerWidth = $window.innerWidth - $scope.leftWidth;

    //$scope.foobar = 1;
    //$scope.$watch($scope.foobar, function (newValue) {
    //    $log.debug("foobar=%s", newValue);
    //});

    //$scope.height = $window.innerHeight - 200;

    //$scope.resizePanel = function () {
    //    $log.debug("был ресайз");
    //};

    //setTimeout(function () {
    //    $log.debug("время действовать");

    //}, 5000);

    //$scope.$watch("height", function (value) {
    //    $log.debug("изменение высоты %s", value);
    //});
})

.directive("mxPanelContainer", function ($mxPanelContainer, $window) {
    var self = this;

    self.panels = [];
    self.activeLeft;

    self.getLeftPanels = function () {
        return self.panels;
    }

    return {
        restrict: "E",
        transclude: true,
        scope: {},
        link: function (scope, element, attrs) {
            scope.border = 40;
            scope.height = $window.innerHeight;
            scope.width = $window.innerWidth;

            scope.contentWidth = scope.width - scope.border * 2;
            scope.contentHeight = scope.height - scope.border;

            scope.leftPanels = $mxPanelContainer.getPanels();

            scope.foo = "hahaha";

            //$mxPanelContainer.
        },
        templateUrl: "mxPanelContainerTmpl.html"
    }
})

.directive("mxPanel", function ($mxPanelContainer) {
    return {
        require: "^mxPanelContainer",
        restrict: "E",
        transclude: true,
        scope: {
            caption: "@",
            orientation: "@k"
        },
        link: function (scope, element, attrs) {
            scope.isMinified = true;
            $mxPanelContainer.addPanel(scope);
        },
        templateUrl: "mxPanelTmpl.html"
    }
});
