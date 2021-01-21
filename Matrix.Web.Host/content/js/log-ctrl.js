angular.module("app")
.controller("LogCtrl", function ($scope, $rootScope, $log, $parse, logSvc, $actions, $uibModal, $list, $filter, $helper, $transport, metaSvc) {

    var rows = [];
    var names = [];
    var devices = {};

    var model = {
        window: $scope.$parent.window,
        modal: undefined,
        //
        only1: true,
        //
        isLoaded: false,
        //
        tubeids: data,
        rows: rows,
        devices: devices,
        // allids: allids,
        //nodes:
        messagesLen: 0,
        lastMessage: undefined,
        messages: [],
        messagesGroup: [],
        messagesGroupLen: 0,
        subscribers: logSvc.getSubscribers(),
        //
        columnState: undefined,
        sortModel: [
            { field: 'type', sort: 'desc' },
            { field: 'number', sort: 'desc' }
        ]
    }

    var matrixIc485 = {
        enable: false,
        active: 0,
        warn: undefined,
        log: { enable: true, level: "2" },
        setna: { enable: false, changeto: "" },
        setmode: { enable: false, changeto: "" },
        setbkp: { enable: false, changeto: "" },
        channels: []
    }

    model.matrixIc485 = matrixIc485;

    $scope.$watch('model.matrixIc485', function () {
        if (model.matrixIc485.log.enable || model.matrixIc485.setna.enable || model.matrixIc485.setmode.enable || model.matrixIc485.setbkp.enable) {
            model.matrixIc485.warn = true;
        } else {
            model.matrixIc485.warn = false;
        }
    }, true);
    

    function parseChannels(parameters) {
        var channels = [];
        if ((parameters === undefined) || (parameters === "")) {
            return channels;
        }

        var channels_text = parameters.split('|');
        //
        for (var i = 0; i < channels_text.length; i++) {
            var channel = { n: 0 };
            var channel_text = channels_text[i].split(';');
            //
            for (var j = 0; j < channel_text.length; j++) {
                var name = matrixIc485.parameters[j].name;
                channel[name] = channel_text[j];
            }
            //
            if (channel.number) {
                var num = parseInt(channel.number);
                if (!isNaN(num)) {
                    channel.n = num;
                }
            }
            //
            channel.enable = !!(channel.number && channel.parameter && channel.k);
            //
            channels.push(channel);
        }
        //
        var sort = $filter('orderBy')(channels, 'n');
        return sort;
    }

    //IDs => get rows array from ids => get all included ids assoc array from rows; get rows name array from rows

    var wrapAction = function (param) {
        var type;
        var action;
        if (typeof param === 'string' || param instanceof String) {
            type = param;
            action = { type: param };
        } else {
            type = param.type;
            action = param;
        }

        if (!action.action) {
            action.promise = $actions.get(type).then(function (a) {
                action.act = a.act;
                action.icon = action.favicon || a.icon;
                action.header = action.caption || a.header;
                return action;
            });
            action.act = function (arg, param) {
                action.promise.then(function (a) {
                    if (a) a.act(arg, param);
                }).catch(function () {
                    action.icon = "/img/error.png";
                    action.header = "";
                });
            }
            action.icon = "/img/loader.gif";
            action.header = "...";
        } else {
            action.icon = action.favicon;
            action.header = action.caption;
            action.act = action.action;
        }
        return action;
    }

    var data = $scope.$parent.window.data;//ids(string) array



    if (data) {
        var ids = data;
        $list.getRows(ids).then(function (crows) {
            model.rows = rows = crows;
            for (var i = 0; i < rows.length; i++) {
                var crow = crows[i];
                var name = (crow.name || "") + (crow.name && crow.pname ? ": " : "") + (crow.pname || "");
                names.push(name || crow.id);
                if (crow.Device && crow.Device.length) {
                    var device = crow.Device[0];
                    devices[device.name] = true;
                }
            }
            //
            //
            if (rows.length === 1) {
                var channels = parseChannels(rows[0].parameters);
                matrixIc485.channels = $filter('filter')(channels, { enable: true });
            }
        }).finally(function () {
            model.isLoaded = true;
        });
    } else {
        model.isLoaded = true;
    }



    var rowsContainer = [{ title: "Все", ids: data, pos: 0 }];//row objects array
    if (data.length > 4) {
        rowsContainer[0].name = data.length + " объектов";
    } else {
        $transport.send(new Message({ what: "edit-get-name-area" }, { ids: data }))
            .then(function (message) {
                if (message.head.what == "edit-get-name-area") {

                    if (message.body.areas.length != 0) {
                        rowsContainer[0].name = message.body.areas.join(', ');
                    }
                }
            });
        // rowsContainer[0].name = "один, два, три, четыре";
    }

    //
    model.applyChanges = function () {
        var cmd = matrixIc485.getCmd();

        var args = {
            cmd: cmd,
            start: model.start,
            end: model.end,
            components: $scope.poll.components(),
            logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
            onlyHoles: $scope.poll.onlyHoles
        }

        if (cmd) {
            var modalInstance = $uibModal.open({
                animation: true,
                templateUrl: "tpls/dialog-with-password-modal.html",
                controller: "DialogWithPasswordCtrl",
                size: "md",
                resolve: {
                    data: function () {
                        return { password: "", text: {} };
                    }
                }
            });

            modalInstance.result.then(function (answer) {
                args.password = answer.password;
                wrapAction("poll-all").act(model.selected.ids, args);
            });
        } else {
            wrapAction("poll-all").act(model.selected.ids, args);
        }
    }

    model.cancelPoll = function () {
        wrapAction("poll-cancel").act(model.selected.ids, {});
    }

    //matrixIc485.applyChanges = function () {
    //    var modalInstance = $uibModal.open({
    //        animation: true,
    //        templateUrl: "tpls/dialog-with-password-modal.html",
    //        controller: "DialogWithPasswordCtrl",
    //        size: "md",
    //        resolve: {
    //            data: function () {
    //                return { password: "", text: {} };
    //            }
    //        }
    //    });

    //    modalInstance.result.then(function (answer) {
    //        //matrixIc485.password = answer ? answer.password : "";
    //        var args = {
    //            cmd: matrixIc485.getCmd(), 
    //            password: answer.password,
    //            components: "Current"
    //        }

    //        if(model.matrixIc485.debug) {
    //            args.start = model.start;
    //            args.end = model.end;
    //            args.components = $scope.poll.components();
    //        }
    //        wrapAction("poll-all").act(model.selected.ids, args);
    //    });
    //}

    matrixIc485.getCmd = function () {
        var cmd = [];
        if (matrixIc485.setbkp.enable) {
            var maxChannel = 0;
            for (var j = 0; j < matrixIc485.channels.length; j++) {
                var channel = matrixIc485.channels[j];
                if (channel.n > maxChannel) {
                    maxChannel = channel.n;
                }
            }

            var channelArray = [];
            for (var i = 1; i <= maxChannel; i++) {
                var j;
                var channel;
                for (j = 0; j < matrixIc485.channels.length; j++) {
                    channel = matrixIc485.channels[j];
                    if (channel.n == i) {
                        break;
                    }
                }
                if (j !== matrixIc485.channels.length) {
                    if (channel.value === undefined || channel.value === "") {
                        channelArray.push("");
                    } else {
                        var value = parseFloat(channel.value.replace(/,/g, '.'));
                        var k = channel.k? parseFloat(channel.k.replace(/,/g, '.')) : 1.0;
                        var imp = 0;
                        if (!isNaN(value) && !isNaN(k)) {
                            imp = Math.trunc(value / k);
                            if (imp < 0) imp = 0;
                        }
                        channelArray.push(imp.toString() || "0");
                    }
                } else {
                    channelArray.push("");
                }
            }

            cmd.push("setbkp=" + channelArray.join(","));
        }
        if (matrixIc485.setna.enable && matrixIc485.setna.changeto) {
            var na = parseInt(matrixIc485.setna.changeto);
            if (na && !isNaN(na)) {
                cmd.push("setna=" + na);
            }
        }
        if (matrixIc485.setmode.enable && matrixIc485.setmode.changeto) {
            var mode = matrixIc485.setmode.changeto;
            cmd.push("setmode" + mode);
        }
        return cmd.join(' ');
    }

    matrixIc485.parameters = metaSvc.rowParameters;
   
    //

    model.selected = rowsContainer[0];
    model.names = rowsContainer[0].name;
    model.rowsContainer = rowsContainer;

    model.buttons = {
        tube: [
            wrapAction({ action: model.applyChanges, caption: "Начать опрос", favicon: "img/play.png" }),
            wrapAction({ action: model.cancelPoll, caption: "Отменить опрос", favicon: "img/cancel.png" })//"poll-cancel")
        ],
        matrix: [wrapAction("poll-matrix-version"), wrapAction("poll-matrix-cancel")],
        //matrixIc485: [wrapAction({ action: matrixIc485.applyChanges, caption: "Применить", favicon: "img/play.png" }), wrapAction({ type: "poll-cancel", caption: "Отмена" })]
    }

    model.actions = {
        matrix: {
            atcmd: wrapAction("poll-matrix-at"), atcmdtext: "",
            chserver: wrapAction("poll-matrix-change"), chservertext: "",
        }
    }

    model.accordion = [{
        template: "tpls/log-accordion-poll.html",
        open: true
    }, {
        template: "tpls/log-accordion-matrix.html"
        //}, {
        //    template: "tpls/log-accordion-matrix-ic485.html"
    }]

    //

    model.tubeIds = [];
    for (var i = 0; i < data.length; i++) {
        model.tubeIds[data[i]] = true;
    }

    model.autoclose = function () {
        return (data.length == 0);
    }

    model.optionCurrent = true;
    model.optionDay = true;
    model.optionHour = true;
    model.optionConstant = true;
    model.optionAbnormal = true;

    $scope.$watch('model.selected', function () {
        if (model.selected && model.selected.name) {
            model.names = model.selected.name;
        }
    }, true);

    function groupRowInnerRendererFunc(params) {
        var img = metaSvc.connectionImgFromType(params.data.type);

        var html = '<i>' + $filter('date')(params.data.date, "dd/MM HH:mm") + '</i> '
            + (img ? '<img src="' + img + '" width="16" /> ' : '(' + params.data.type + ') ')
            + '<span class="badge">' + params.data.messagesLen + '</span> '
            + '<b>' + params.data.object + '</b>: '
            + params.data.message;
        return html;
    }

    /////////////////////////////////////////////////////////////////////////////////////

    $scope.opt = {
        //angularCompileRows: true,
        angularCompileFilters: true,

        enableFilter: true,
        enableSorting: true,

        columnDefs: [{
            //headerName: "Номер п/п",
            //field: "number",
            //width: 30,
            //suppressMenu: true,
            //cellRenderer: {
            //    renderer: 'group',
            //    suppressCount: true
            //}            
            //}, {
            headerName: "Дата",
            field: "date",
            width: 200,
            sort: "desc",
            cellRenderer: function (params) {
                return $filter('date')(params.data.date, "dd.MM.yyyy HH:mm:ss.sss");
            }
            //}, {
            //    headerName: "Объект",
            //    field: "tubeId",
            //    width: 70,
            //    cellRenderer: {
            //        renderer: 'group'//,                
            //        //innerRenderer: innerCellRenderer
            //    }
        }, {
            headerName: "Название",
            field: "name",
            width: 200
        }, {
            headerName: "Сообщение",
            field: "message",
            width: 1500
        }],

        //rowsAlreadyGrouped: true,
        //groupUseEntireRow: true,
        //groupRowInnerRenderer: groupRowInnerRendererFunc,

        enableColResize: true,
        rowData: model.messages,

        afterSortChanged: function () {
            var api = $scope.opt.api;
            model.sortModel = api.getSortModel();
        },
        columnResized: function () {
            var api = $scope.opt.api;
            model.columnState = api.getColumnState();
        },

        ready: function (api) {
            //colums restore
            if (model.columnState) {
                api.setColumnState(model.columnState);
            }
            //sort restore/get default
            api.setSortModel(model.sortModel);
        },

        icons: {
            groupExpanded: '<img src="/img/16/toggle.png" />',
            groupContracted: '<img src="/img/16/toggle_expand.png" />'
        }
    };


    model.refresh = function () {
        if (model.modalIsOpen) {
            if ($scope.opt.api) {
                $scope.opt.api.onNewRows();
            }
        } else {
            $scope.$apply();
        }
    }

    model.clearLog = function () {
        model.messages.length = 0;

        //очистка данных
        delete model.lastMessage;
        for (var i = 0; i < model.messagesGroup.length; i++) {
            delete model.messagesGroup[i];
        }
        model.messagesGroup.length = 0;
        //очистка счетчиков
        model.messagesGroupLen = 0;
        model.messagesLen = 0;
        //обновление вида
        model.refresh();
    }

    ////удалить и отписаться от всего
    //model.unsubscribeAll = function () {
    //    logSvc.unsubscribeAll().then(function () {
    //        model.clearLog();
    //        model.tubes.aa = {};
    //        model.tubes.arr.length = 0;
    //        $scope.tubesOpts.api.onNewRows();
    //    })
    //}

    //modal


    // открытие-закрытие окна
    model.modalOpen = function () {
        model.modal = $uibModal.open({
            templateUrl: model.window.modalTemplateUrl,
            windowTemplateUrl: model.window.windowTemplateUrl,
            size: 'lg',
            scope: $scope
        });
        model.modalIsOpen = true;

        model.modal.result.then(function () {
            model.modalIsOpen = false;
            if (model.autoclose && model.autoclose()) {
                model.close();
            }
        }, function () {
            model.modalIsOpen = false;
            if (model.autoclose && model.autoclose()) {
                model.close();
            }
        });
    }

    model.window.open = model.modalOpen;

    model.close = function () {
        model.modal.close();
        model.window.close();
    }

    model.modalOpen();

    //

    $scope.model = model;


    var listener = $rootScope.$on("transport:message-received", function (e, message) {

        if (message.head.what != "log") return;

        var filteredLength = 0;
        for (var i = 0; i < message.body.messages.length; i++) {
            var msg = message.body.messages[i];

            if (model.tubeIds[msg.tubeId]) {

                model.messages.push(msg);
                model.lastMessage = msg;
                model.messagesLen++;
            }
        }
        model.messagesLen += filteredLength;

        $log.debug("пришло логов %s после фильтра %s", message.body.messages.length, filteredLength);

        model.refresh();
    });

    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    model.select = function (row) {
        model.selected = row;
    };

    $scope.$on('$destroy', function () {
        $log.debug("уничтожается окошко логов");
        listener();
    });


    $scope.poll = {
        viewDetails: false,
        onlyHoles: true
    };

    $scope.poll.rules = [{
        id: 1,
        name: "нет в базе"
    }, {
        id: 2,
        name: "нет за последние часы"
    }, {
        id: 3,
        name: "всегда"
    }];

    $scope.poll.details = [{
        enabled: false,
        title: "Константы",
        name: "Constants",
        rule: $scope.poll.rules[0]
    }, {
        enabled: true,
        title: "Текущие",
        name: "Current",
        rule: $scope.poll.rules[2]
    }, {
        enabled: true,
        title: "Часы",
        name: "Hour",
        rule: $scope.poll.rules[1],
        duration: 60
    }, {
        enabled: true,
        title: "Сутки",
        name: "Day",
        rule: $scope.poll.rules[1],
        duration: 60
    }, {
        enabled: false,
        title: "НС",
        name: "Abnormal",
        rule: $scope.poll.rules[0]
    }];

    $scope.poll.components = function () {
        var comp = "";
        for (var i = 0; i < $scope.poll.details.length; i++) {
            var detail = $scope.poll.details[i];
            if (detail.enabled) {
                comp += detail.name + ":" + detail.rule.id;
                if (detail.duration) {
                    comp += ":" + detail.duration;
                }
                comp += ";";
            }
        }
        return comp;
    }
});