angular.module("app")
    .controller("ControlCtrl", function ($scope, $rootScope, $log, $parse, logSvc, $actions, $uibModal, $list, $filter, $helper, $transport, metaSvc, controlSvc) {
        
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
            lightBeforeSunRise: [],
            afterSunSetAndBeforeSunRise: [,],
            lightControlMetod: [],
            lightSheduleOn: [,],
            lightSheduleOff: [,],
            coordinates: [],
            afterBeforeSunSetRise: [],
            isAstrTimePlusSheduler: [],
            lightOnOff: [,],
            soundOffEnable: false, 

            //
            cameraEnable: false,
            lightV1Enable: false,
            lightV2Enable: false,
            valveControlEnable: false,
            softStartControlEnable: false,
            softStartControlDebugEnable: false,
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
            debug: { enable: false, changeto: "" },
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
                    if (crow.LanConnection != null && crow.LanConnection.length > 0 && crow.LanConnection[0].host == "192.168.0.124") {
                        model.cameraEnable = true;
                    }
                    switch (crow.resource) {
                        case "light":
                            GetLightControlsAstronomTimerValue(ids[0]);
                            model.lightV1Enable = true;
                            break;
                        case "lightV2":
                            GetLightControlConfig(ids[0]);
                            GetRowCache(ids);
                            break;
                        case "valveControl":
                            model.valveControlEnable = true;
                            break;
                        case "softStartControl":
                            model.softStartControlEnable = true;
                            break;
                    }
               
                
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

        var GetRowCache = function (ids) {
            model.lightOnOff = [];
            $list.getRowsCache(ids).then(function (rows) {
                var controllerData = rows[0].controllerData;
                var textLightReal = "Real";
                var textLightMk = "lightMK";
                var arrLightMk;
                var arrLightReal;
                var img = '';
                if (controllerData != null) {
                    var arrControllerData = controllerData.split(';');
                    for (var i = 0; i < arrControllerData.length; i++) {
                        if (arrControllerData[i].includes(textLightMk)) {
                            arrLightMk = arrControllerData[i].split(':');
                        }
                        if (arrControllerData[i].includes(textLightReal)) {
                            arrLightReal = arrControllerData[i].split(':');
                        }
                    }
                    if (arrLightMk != null && arrLightReal != null) {
                        for (var i = 1; i < arrLightMk.length; i++) {//если что 0-ой элемент это название, поэтому с 1-го элемента
                            if (arrLightReal[i] == 1 && arrLightMk[i] == 1) {
                                model.lightOnOff.push(['success', 'default']);
                            } else if (arrLightReal[i] == 0 && arrLightMk[i] == 0) {
                                model.lightOnOff.push(['default','secondary']);
                            } else if (arrLightReal[i] == 1 && arrLightMk[i] == 0) {
                                model.lightOnOff.push(['default', 'warning']); 
                            } else if (arrLightReal[i] == 0 && arrLightMk[i] == 1) {
                                model.lightOnOff.push(['warning', 'default']);
                            }
                        }
                    }
                }
                if ((parseInt(rows[0].event, 10) & 0x100) > 0 && (parseInt(rows[0].event, 10) & 0x10000) == 0 ) {
                    model.soundOffEnable = true;
                }
            });
        }
        var GetLightSheduleOnOff = function (lightSheduleOnOff) {
            var utcHour = new Date(0).getHours();
            var date0 = null, date1 = null;
            if (lightSheduleOnOff[0] != 0xFFFFFFFF) {
                var tmp = new Date(lightSheduleOnOff[0] * 1000);
                date0 = new Date (Date.UTC(tmp.getUTCFullYear(), tmp.getUTCMonth(), tmp.getUTCDate(), tmp.getUTCHours() - utcHour, tmp.getUTCMinutes(), tmp.getUTCSeconds()));
            }
            if (lightSheduleOnOff[1] != 0xFFFFFFFF) {
                var tmp = new Date(lightSheduleOnOff[1] * 1000);
                date1 = new Date(Date.UTC(tmp.getUTCFullYear(), tmp.getUTCMonth(), tmp.getUTCDate(), tmp.getUTCHours() - utcHour, tmp.getUTCMinutes(), tmp.getUTCSeconds()));
            }
            return [date0, date1]
        }
        var GetLightControlsAstronomTimerValue= function (id) {
            controlSvc.nodeGetAstronomTimer(id).then(function (message) {
                var coordinates = message.body.coordinates;
                var afterBeforeSunSetRise = message.body.afterBeforeSunSetRise;
                model.utc = message.body.utc;
                if (coordinates != null && coordinates.includes(";")) {
                    model.coordinates = coordinates.split(";");
                }
                if (afterBeforeSunSetRise != null && afterBeforeSunSetRise.includes(";")) {
                    model.afterBeforeSunSetRise = afterBeforeSunSetRise.split(";");
                }
            });
        }
        var GetLightControlConfig = function (id) {
            controlSvc.GetLightControlConfig(id).then(function (message) {
                if (message.body.strConfig != null) {
                    model.lightV2Enable = true;
                    model.lightControlMetod = message.body.lightControlMetod;
                    model.afterSunSetAndBeforeSunRise = message.body.afterSunSetAndBeforeSunRise;
                    for (var i = 0; i < model.lightControlMetod.length; i++) {
                        if (model.lightControlMetod[i] == '16') {
                            model.isAstrTimePlusSheduler.push(true);
                        } else {
                            model.isAstrTimePlusSheduler.push(false);
                        }
                    }

                    for (var i = 0; i < message.body.lightSheduleOn.length; i++) {
                        model.lightSheduleOn[i] = GetLightSheduleOnOff(message.body.lightSheduleOn[i]);
                        model.lightSheduleOff[i] = GetLightSheduleOnOff(message.body.lightSheduleOff[i]);
                    }
                } else{
                    var args = {
                        cmd: "",
                        start: model.start,
                        end: model.end,
                        components: "Constants",
                        logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                        onlyHoles: false
                    }
                    wrapAction("poll-all").act(model.selected.ids, args);
                }
            });
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
        }

        var rowlight = {
            shedule: [],
            controlMetod: [],
            name: [],
        }
        for (var i = 0; i < data.length; i++) {
            $transport.send(new Message({ what: "edit-get-row" }, { id: data[i], isNew: false }))
                .then(function (message) {
                    if (message.head.what == "edit-get-row") {
                        if (message.body.area.length != 0) {
                            rowlight.shedule[i] = message.body.area.shedule;
                            rowlight.controlMetod[i] = message.body.area.controlMetod;
                            rowlight.name[i] = message.body.area.name;
                        }
                    }
                });
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
        model.correctTime = function () {
            var cmd = "correcttime";
            var args = {
                cmd: cmd,
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }

        model.current = function () {
            var args = {
                cmd: "",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.startFlash = function () {
            $uibModal.open({
                templateUrl: 'tpls/flash-modal.html',
                controller: 'FlashCtrl',
                size: 'md',
                resolve: {
                    data: function () {
                        return data;
                    }
                }
            });
        }
        model.acknowledge = function () {
            $list.audio("pause");
        
            $list.getRowsCache(ids).then(function (rows) {
                var events = parseInt(rows[0].event, 10) | 0x10000;
                if (events) {
                    controlSvc.nodeEvents(events, rows[0].id);
                    model.soundOffEnable = false;
                }
            });
        
        }
        model.clickDebug = function () {
            if (model.matrixIc485.debug.changeto == "1" && model.softStartControlEnable) {
                model.softStartControlDebugEnable = true;
            }
        }

        model.lightOn = function (index) {
            for (var i = 0; i < rowlight.name.length; i++) {
                if (model.selected.name = rowlight.name[i]) {
                    if (rowlight.controlMetod[i] == null || !rowlight.controlMetod[i].includes('Ручное управление')) {
                        if (model.lightControlMetod[index] == null) {
                            alert('Выберите ручное управление');
                            return;
                        }
                        else {
                            if (model.lightControlMetod[index].includes('Ручное управление') || model.lightControlMetod[index].includes('2') || model.lightControlMetod[index].includes('18')) {
                                rowlight.controlMetod[i] = model.lightControlMetod[index];
                            } else {
                                alert('Выберите ручное управление');
                                return;
                            }
                        }
                    }
                    var index1 = index + 1;
                    var cmd = "lightSoftConfig#on" + index1 + "#" + rowlight.controlMetod[i] + "#" + rowlight.shedule[i];
                    var args = {
                        cmd: cmd,
                        start: model.start,
                        end: model.end,
                        components: "Current:всегда;",
                        logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                        onlyHoles: false
                    }
                    wrapAction("poll-all").act(model.selected.ids, args);
                }
            }
        }

        model.setAstronomicalTimerControllers = function () {
            var coordinates = model.coordinates[0] + ";" + model.coordinates[1];
            var afterBeforeSunSetRise = model.afterBeforeSunSetRise[0] + ";" + model.afterBeforeSunSetRise[1];
            controlSvc.nodeUpdateAstronomTimer(coordinates, model.utc, afterBeforeSunSetRise, model.selected.ids[0]);
            controlSvc.pollSetLightAstronomTimer(model.coordinates, model.utc, model.afterBeforeSunSetRise, model.selected.ids[0]);
        }
        model.controlMetod = function () {
            for (var i = 0; i < rowlight.name.length; i++) {
                if (model.selected.name = rowlight.name[i]) {
                    if (model.lightControlMetod[0] == "") {
                        alert("Выберите метод управления");
                    }
                    else {
                        var cmd = "lightSoftConfig#null#" + model.lightControlMetod[0] + "#" + rowlight.shedule[i];
                        var args = {
                            cmd: cmd,
                            start: model.start,
                            end: model.end,
                            components: "Current:всегда;",
                            logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                            onlyHoles: false
                        }
                        wrapAction("poll-all").act(model.selected.ids, args);
                    }
                }
            }
        }
        model.lightOff = function (index) {
            for (var i = 0; i < rowlight.name.length; i++) {
                if (model.selected.name = rowlight.name[i]) {
                    if (rowlight.controlMetod[i] == null ||!rowlight.controlMetod[i].includes('Ручное управление')) {
                        if (model.lightControlMetod[index].includes('Ручное управление') || model.lightControlMetod[index].includes('2') || model.lightControlMetod[index].includes('18')) {
                            rowlight.controlMetod[i] = model.lightControlMetod[index];
                        } else {
                            alert('Выберите ручное управление');
                            return;
                        }
                    }
                    var index1 = index + 1;
                    var cmd = "lightSoftConfig#off" + index1 + "#" + rowlight.controlMetod[i] + "#" + rowlight.shedule[i];
                    var args = {
                        cmd: cmd,
                        start: model.start,
                        end: model.end,
                        components: "Current:всегда;",
                        logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                        onlyHoles: false
                    }
                    wrapAction("poll-all").act(model.selected.ids, args);
                }
            }
        }
        model.LightControlMetod = function (index) {
            for (var i = 0; i < model.lightControlMetod.length; i++) {
                if (model.lightControlMetod[i] == '16') {
                    model.isAstrTimePlusSheduler[i] = true;
                    model.lightSheduleOn[i][0] = null;
                    model.lightSheduleOff[i][1] = null;
                } else {
                    model.isAstrTimePlusSheduler[i] = false;
                }
            }
        }
        model.saveConfig = function () {
            for (var i = 0; i < rowlight.name.length; i++) {
                if (model.selected.name = rowlight.name[i]) {
                    if (model.lightControlMetod.includes('2')) {
                        alert('Сохранение ручного управления недоступно');
                        return;
                    }
                    controlSvc.pollLightControlConfig(model.lightControlMetod, model.afterSunSetAndBeforeSunRise, model.lightSheduleOn, model.lightSheduleOff, model.selected.ids[0]);
                }
            }
        }
        model.softStartControlQuery = function () {
            var args = {
                cmd: "softStartControl#uppRequest",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.manualControl = function () {
            var args = {
                cmd: "softStartControl#control#manual",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.controllerControl = function () {
            var args = {
                cmd: "softStartControl#control#controller",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.waterTowerQuery = function () {
            var args = {
                cmd: "softStartControl#waterTowerRequest",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.pumpStop = function () {
            var args = {
                cmd: "softStartControl#pumpStartStop#0",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.levelMax = function () {
            var args = {
                cmd: "softStartControl#controllerSet#levelmax",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: "3",
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.levelMin = function () {
            var args = {
                cmd: "softStartControl#controllerSet#levelmin",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: "3",
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.monitoring = function () {
            var args = {
                cmd: "softStartControl#controllerSet#monitoring",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: "3",
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.pumpStart = function () {
            var args = {
                cmd: "softStartControl#pumpStartStop#1",
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);
        }
        model.valveOpen = function () {
            $actions.get("valveControl-card").then(function (a) {
                if (a != null) {
                    a.visible = true;
                    a.act({ ids: model.selected.ids, headerName: "all", field: "all", control: "Открыть" });
                }
            });
            /*var cmd = "valveSoftConfig#open";
            var args = {
                cmd: cmd,
                start: model.start,
                end: model.end,
                components: "Current:всегда;",
                logLevel: model.matrixIc485.log.enable ? model.matrixIc485.log.level : undefined,
                onlyHoles: false
            }
            wrapAction("poll-all").act(model.selected.ids, args);*/
        }
        model.valveClose = function () {
            $actions.get("valveControl-card").then(function (a) {
                if (a != null) {
                    a.visible = true;
                    a.act({ ids: model.selected.ids, headerName: "all", field: "all" , control:"Закрыть"});
                }
            });
        }
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
        model.buttonsControl = {
            tube: [
                wrapAction({ action: model.correctTime, caption: "Корректировка времени", favicon: "img/clock_edit.png" }),
                wrapAction({ action: model.startFlash, caption: "Обновление ПО контроллера", favicon: "img/setting_tools.png" }),
                wrapAction({ action: model.current, caption: "", favicon: "img/play.png" })

            ],
            matrix: [wrapAction("poll-matrix-version")],
        }
        model.buttonsLight = {
            tube: [
                //wrapAction({ action: model.controlMetod, caption: "Метод управления", favicon: "img/bullet_edit.png" }),
                wrapAction({ action: model.lightOn, caption: "Включить", favicon: "img/enabled.png" }),
                wrapAction({ action: model.lightOff, caption: "Выключить", favicon: "img/disabled.png" })
            ],
            matrix: [wrapAction("poll-matrix-version")],
        }
        model.buttonsValveControl = {
            tube: [
                wrapAction({ action: model.valveOpen, caption: "Открыть все", favicon: "img/bullet_green.png" }),
                wrapAction({ action: model.valveClose, caption: "Закрыть все", favicon: "img/bullet_red.png" })
            ],
            matrix: [wrapAction("poll-matrix-version")],
        }
        model.buttonsSoftStartControl = {
            tube: [
                wrapAction({ action: model.manualControl, caption: "Ручное управление", favicon: "img/control_play_blue.png" }),
                wrapAction({ action: model.controllerControl, caption: "Управление контроллером", favicon: "img/control_play_blue.png" }),
                wrapAction({ action: model.softStartControlQuery, caption: "Опрос УПП", favicon: "img/control_play_blue.png" }),
                wrapAction({ action: model.waterTowerQuery, caption: "Опрос башни", favicon: "img/control_play_blue.png" }),
                wrapAction({ action: model.pumpStart, caption: "Start pump", favicon: "img/pump_start_32.png" }),
                wrapAction({ action: model.pumpStop, caption: "Stop pump", favicon: "img/pump_stop_32.png" })
            ],
            matrix: [wrapAction("poll-matrix-version")],
        }
        model.buttonsSoftStartControlDebug = {
            tube: [
                wrapAction({ action: model.levelMax, caption: "max", favicon: "img/control_play_blue.png" }),
                wrapAction({ action: model.levelMin, caption: "min", favicon: "img/control_play_blue.png" }),
                wrapAction({ action: model.monitoring, caption: "monitoring", favicon: "img/action_log.png" })
            ],
            matrix: [wrapAction("poll-matrix-version")],
        }
        model.actions = {
            matrix: {
                atcmd: wrapAction("poll-matrix-at"), atcmdtext: "",
                chserver: wrapAction("poll-matrix-change"), chservertext: "",
            }
        }

        model.accordion = [{
            template: "tpls/control-accordion-poll.html",
            open: true
        }, {
            template: "tpls/control-accordion-matrix.html"
        }]
        

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
                headerName: "Дата",
                field: "date",
                width: 200,
                sort: "desc",
                cellRenderer: function (params) {
                    return $filter('date')(params.data.date, "dd.MM.yyyy HH:mm:ss.sss");
                }
            }, {
                headerName: "Название",
                field: "name",
                width: 200
            }, {
                headerName: "Сообщение",
                field: "message",
                width: 1500
            }],
            
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
            
            if (message.head.what == "log") {
                for (var i = 0; i < message.body.messages.length; i++) {
                    if (message.body.messages[i].message == "update") {
                        for (var j = 0; j < model.rows.length; j++) {
                            if (model.rows[i].id == message.body.messages[i].tubeId) {
                                switch (model.rows[i].resource) {
                                    case "lightV2":
                                        GetLightControlConfig(message.body.messages[i].tubeId);
                                        break;
                                }
                            }
                        }
                        //GetLightControlConfig(message.body.messages[i].tubeId);
                    }
                }
            }
            else if (message.head.what == "ListUpdate") {
                ids = message.body.ids;
                if (ids[0] == data[0]) {
                    GetRowCache(data);
                }
            }
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