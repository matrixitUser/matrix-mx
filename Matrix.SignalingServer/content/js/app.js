angular.module("matrix", [
    "ui.bootstrap",
    "ui.grid", "ui.grid.selection", "ui.grid.i18n", "ui.grid.resizeColumns", "ui.grid.edit", "ui.grid.autoResize",
    "angularMoment",
    'ngTouch',
    "ngAudio",
    "ui.layout",
    "ngToast"
]
)
    .run(function ($log) {
        $log.debug("старт приложения");
    })

    /**
        * настройка маршрутизации
        */
    .config(function ($logProvider) {
        moment.locale("ru");
        $logProvider.debugEnabled(true);
    })
    .controller("mainCtrl", function ($scope, $window, $http, $modal, $log, ngAudio, uiGridConstants, ngToast, $filter) {

        var Message = function (head, body) {
            var self = this;
            self.head = head;
            self.body = body;
            return self;
        };
        var dtStart = new Date();
        dtStart.setDate(dtStart.getDate() - 10);
        var model = {
            height: $window.innerHeight,
            dtStart: dtStart,
            dtEnd: new Date(),
            //ARCHIVE
            archive: false,
            onChangeArchive: function () {
                model.lastSuccessGetList = (new Date(0)).toJSON();
                selectAcrhiveOrActive();
            },

            //GRID replay
            columns: [{
                field: "objName",
                displayName: "Объект",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "parameter",
                displayName: "Параметр",
                width: "110",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "parameterSetPoint",
                displayName: "Параметр уставки",
                width: "120",
                type: "string",
                resizable: true,
                visible: false
            }, {
                field: "value",
                displayName: "Текущие",
                width: "90",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "message",
                displayName: "Сообщение",
                width: "180",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "setPoint",
                displayName: "Уставка",
                width: "90",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "dateStart",
                displayName: "Дата начала события",
                cellFilter: 'date:\'dd.MM.yyyy HH:mm:ss\'',
                sort: {
                    direction: uiGridConstants.DESC,
                    priority: 0
                },
                width: "170",
                resizable: true,
                visible: true
            }, {
                field: "dateNormalize",
                displayName: "Дата окончания события",
                cellFilter: 'date:\'dd.MM.yyyy HH:mm:ss\'',
                sort: {
                    direction: uiGridConstants.DESC,
                    priority: 1
                },
                width: "170",
                resizable: true,
                visible: true
            }, {
                field: "dateQuit",
                displayName: "Дата квитирования",
                cellTemplate: "/tpls/state.html",
                width: "180",
                type: "string",
                resizable: true,
                visible: true
            }],
            rows: [],
            //

            //GRID not replay
            columnsNotReplay: [{
                field: "objName",
                displayName: "Объект",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "parameter",
                displayName: "Параметр",
                width: "120",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "value",
                displayName: "Текущие",
                width: "110",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "message",
                displayName: "Сообщение",
                width: "200",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "setPoint",
                displayName: "Уставка",
                width: "110",
                type: "string",
                resizable: true,
                visible: true
            }, {
                field: "dateStart",
                displayName: "Дата начала события",
                cellFilter: 'date:\'dd.MM.yyyy HH:mm:ss\'',
                sort: {
                    direction: uiGridConstants.DESC,
                    priority: 0
                },
                width: "210",
                resizable: true,
                visible: true
            }],
            rowsNotReplay: [],
            //
            lastSuccessGetList: (new Date(0)).toJSON(), //вначале берется минимальная дата, чтобы получить все события. тут можно оставить дату "текущая минус время устаревания", но, думаю, не стоит. хотя получать все события - тоже плохо
            isNoServerConnection: false,
            IsDbOk: true,

            //
            settingsRefresh: function () {
                $http({
                    method: 'POST',
                    url: '/api/transport?' + ToParams({ 'type': 'settingsRefresh' }),
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
                }).success(function (data, status) {
                    $log.info("settingsRefresh = " + data);
                    if (data === true) {
                        ngToast.create("Уставки скоро будут обновлены");
                    }
                }).error(function (e) {
                    $log.error("ошибка");
                });
            },

            getArchive: function () {
                model.rows = [];
                cache = {};
                $scope.gridOptions = {
                    data: model.rows,
                    columnDefs: model.columns,
                    enableColumnMenus: false,
                    rowTemplate: '/tpls/row.html',
                    showGridFooter: true
                };
                getListAchive();
            },
            
            openModal: function () {
                var modalInstance = $modal.open({
                    templateUrl: '/tpls/modal.html',
                    controller: 'ModalInstanceCtrl',
                    size: 'lg'
                });

                modalInstance.result.then(function () {
                }, function () {
                    $log.info('Modal dismissed at: ' + new Date());
                });
            }

        };

        $scope.model = model;

        $scope.gridOptions = {
            data: model.rows,
            columnDefs: model.columns,
            enableColumnMenus: false,
            rowTemplate: '/tpls/row.html',
            showGridFooter: true
        };
        $scope.gridOptionsNotReplay = {
            data: model.rowsNotReplay,
            columnDefs: model.columnsNotReplay,
            enableColumnMenus: false,
            showGridFooter: true
        };
        $scope.sound = ngAudio.load("/media/song.mp3");
        $scope.sound.loop = true;


        //Init

        var cache = {};
        var cacheNotReplay = {};

        //FUNCTIONS

        var qvitirovat = function (id) {
            params = { 'id': id };
            $log.debug("псевдоквитирование " + id);
            if (cache[id] && cache[id].dateQuit == null) {
                cache[id].dateQuit = "...";
            }
            send(new Message({ what: "signal-quit" }, { id: id })).then(function (message) {
                if (message.head.what != "signal-quit") {
                    getList();
                    return;
                }
                if (message.body.rows.length > 0) {
                    eventHandler(message.body.rows[0]);
                } else {
                    getList();
                }
                playAlarmIfNeed();
            });
        }
        var selectAcrhiveOrActive = function () {
            if (!model.archive) return;
            getList();
        }
        var eventHandler = function (row) {
            var isActive = model.archive || row.dateEnd == null || row.dateQuit == null; //не нормализовавшийся или не квитированный
            var rowInfo = row.id + "|" + row.objName + "|" + row.parameter + "|" + row.parameterSetPoint + "|" + row.value + "|" + row.message + "|" + row.setPoint + "|" + row.dateStart + "|" + row.dateEnd + "|" + row.dateQuit + "|" + row.dateNormalize;
            var found = (row.replay == true || row.replay == null) ? cache[row.id] : cacheNotReplay[row.id];
            if (!found) {
                if (isActive) {
                    //rows.push(adapt(row));//добавление в конец
                    if (row.replay == true || row.replay == null) {
                        model.rows.unshift(adapt(row));//добавление в начало кэша
                        cache[row.id] = row;
                    } else {
                        model.rowsNotReplay.unshift(adapt(row));//добавление в начало кэша
                        cacheNotReplay[row.id] = row;
                    }
                    $log.debug(rowInfo + " добавление в кэш");
                } else {
                    $log.debug(rowInfo + " неактивен");
                }
            } else {
                if (isActive) {
                    found.dateEnd = row.dateEnd;
                    found.dateQuit = row.dateQuit;
                    found.dateNormalize = row.dateNormalize;
                    found.value = row.value;
                    found.setPoint = row.setPoint;
                    $log.debug(rowInfo + " обновление");
                } else {
                    $log.debug(rowInfo + " удаление");
                    if (row.replay == true || row.replay == null) {
                        for (var i in model.rows) {
                            if (row.id == model.rows[i].id) {
                                $log.debug(row.id + " удаление из scope.rows #" + i);
                                model.rows.splice(i, 1);
                                break;
                            }
                        }
                        delete cache[row.id];
                    } else {
                        for (var i in model.rowsNotReplay) {
                            if (row.id == model.rowsNotReplay[i].id) {
                                $log.debug(row.id + " удаление из scope.rows #" + i);
                                model.rowsNotReplay.splice(i, 1);
                                break;
                            }
                        }
                        delete cacheNotReplay[row.id];
                    }
                    
                }
            }
        }
        
        //отправка сообщения
        var send = function (message) {

            $log.debug("на сервер отправлено сообщение: %s", message.head.what);

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

        }
        var getListAchive = function () {
            var dtStart = model.dtStart;
            var dtEnd = model.dtEnd;
            send(new Message({ what: "signal-get-by-date-startend" }, { startDate: dtStart, endDate: dtEnd })).then(function (message) {
                if (message.head.what != "signal-get-by-date-startend") {
                    return;
                }
                var rows = message.body.rows;
                if (rows) {

                    $log.debug("получено сообщение: событий " + rows.length);
                    for (var i = 0; i < rows.length; i++) {// цикл по новым событиям
                        var row = rows[i];
                        eventHandler(row);
                    }
                }
            });
        }
        var getList = function () {//запрос на сервер: получить все события, произошедшие от последнего успешного запроса
            model.rows = [];
            cache = {};
            $scope.gridOptions = {
                data: model.rows,
                columnDefs: model.columns,
                enableColumnMenus: false,
                rowTemplate: '/tpls/row.html',
                showGridFooter: true
            };
            model.rowsNotReplay = [];
            cacheNotReplay = {};
            $scope.gridOptionsNotReplay = {
                data: model.rowsNotReplay,
                columnDefs: model.columnsNotReplay,
                enableColumnMenus: false,
                showGridFooter: true
            };
            send(new Message({ what: "signal-get-active-events-all" }, {})).then(function (message) {
                if (message.head.what != "signal-get-active-events-all") {
                    return;
                }
                if (model.isNoServerConnection) {//соединение восстановлено
                    var dateLocale = new Date();
                    eventHandler({
                        id: '00000000-0000-0000-0000-000000000000',
                        objName: 'Нет связи с сервером',
                        parameter: '',
                        parameterSetPoint: '',
                        message: 'Потеряна связь с сервером',
                        dateStart: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                        dateNormalize: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                        dateEnd: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                        dateQuit: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss")
                    });
                    model.isNoServerConnection = false;
                    model.IsDbOk = true;
                }

                if (model.IsDbOk != message.body.IsDbOk) {//что-то с БД
                    var dateLocale = new Date();
                    if (message.body.IsDbOk == true) {
                        eventHandler({
                            id: '00000000-0000-0000-0000-000000000001',
                            objName: 'Сервер не работает',
                            parameter: '',
                            parameterSetPoint: '',
                            message: 'Ошибка на сервере',
                            dateStart: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                            dateNormalize: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                            dateEnd: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                            dateQuit: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss")
                        });
                    } else {
                        eventHandler({
                            id: '00000000-0000-0000-0000-000000000001',
                            objName: 'Сервер не работает',
                            parameter: '',
                            parameterSetPoint: '',
                            message: 'Ошибка на сервере',
                            dateStart: $filter("date")(dateLocale, "dd.MM.yyyy HH:mm:ss"),
                            dateNormalize: null,
                            dateEnd: null,
                            dateQuit: null
                        });
                    }
                    model.IsDbOk = message.body.IsDbOk;
                }
                var rows = message.body.rows;
                if (rows) {

                    $log.debug("получено сообщение: событий " + rows.length);
                    for (var i = 0; i < rows.length; i++) {// цикл по новым событиям
                        var row = rows[i];
                        eventHandler(row);
                    }
                }
                if (model.lastSuccessGetList > message.body.date) {//дату на сервере передвинули назад? не интересно, ничего не делаем
                    $log.debug("обнаружено расхождение даты со времени получения последнего сообщения");
                }
                model.lastSuccessGetList = message.body.date;

                //finally
                playAlarmIfNeed();

            });
        }

        var playAlarmIfNeed = function () {
            var isAlarmSoundNeed = false;
            for (var i in model.rows) {//цикл по кэшу
                var row = model.rows[i];
                if (row.dateNormalize == null && row.dateQuit == null) {
                    isAlarmSoundNeed = true;
                    break;
                }
            }
            isAlarmSoundNeed ? $scope.sound.play() : $scope.sound.pause();
        }

        //

        var ToParams = function (obj) {
            var p = [];
            for (var key in obj) {
                p.push(key + '=' + encodeURIComponent(obj[key]));
            }
            return p.join('&');
        };

        //

        var adapt = function (row) {
            row.quit = qvitirovat;
            return row;
        }

        $scope.ext = { model: model };

        //Timer

        var timerInterval = null;
        var timerOnce = null;

        var touchTimerGetList = function () {
            if (timerInterval != null) {
                clearInterval(timerInterval);
                timerInterval = null;
            }

            timerInterval = setInterval(function () {
                if (!model.archive) {
                    getList();
                    //getListNotReplayEvents();
                }
            }, 1000 * 60);
            //
            if (timerOnce != null) {
                clearInterval(timerOnce);
                timerOnce = null;
            }
            timerOnce = setTimeout(function () {
                getList();
                //getListNotReplayEvents();
            }, 750);
        }

        touchTimerGetList();

    });

angular.module("matrix").controller('ModalInstanceCtrl', function ($scope, $modalInstance, $http, $log, uiGridConstants) {

    $scope.itemsTitle = "Доп. информация";
    $scope.items = [];
    $scope.itemcols = [];

    var ToParams = function (obj) {
        var p = [];
        for (var key in obj) {
            p.push(key + '=' + encodeURIComponent(obj[key]));
        }
        return p.join('&');
    };

    $scope.show = function (type) {
        $log.debug('запрос отчета ' + type);

        $scope.itemsTitle = "Доп. информация";
        $scope.items = [];
        $scope.itemcols = [];

        if (type == 'events') {
            params = { 'start': '2000-01-01', 'end': '2050-01-01' };
        } else {
            params = { 'type': type };
        }
        $http({
            method: 'POST',
            url: '/api/transport?' + ToParams(params),
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        }).success(function (data, status) {
            $scope.itemsOpt = { 'date': data.Date, 'type': data.Type };
            $log.debug("получено строк: " + (data.Rows ? data.Rows.length : null)
                + " для " + data.Type);
            //
            $scope.items = data.Rows;

            if ($scope.itemsOpt.type == "Event") {
                $scope.itemsTitle = "Архив событий";
                $scope.itemcols = [{
                    field: "objName",
                    displayName: "Объект",
                    width: "200",
                    type: "string",
                    resizable: true,
                    visible: true
                }, {
                    field: "parameter",
                    displayName: "Параметр",
                    width: "75",
                    type: "string",
                    resizable: true,
                    visible: true
                }, {
                    field: "setPoint",
                    displayName: "Уставка",
                    width: "75",
                    type: "string",
                    resizable: true,
                    visible: true
                }, {
                    field: "message",
                    displayName: "Сообщение",
                    width: "150",
                    type: "string",
                    resizable: true,
                    visible: true
                }, {
                    field: "dateStart",
                    displayName: "Начало события",
                    width: "150",
                    type: "string",
                    resizable: true,
                    visible: true,
                    sort: {
                        direction: uiGridConstants.DESC,
                        priority: 0
                    }
                }, {
                    field: "dateEnd",
                    displayName: "Конец события",
                    width: "150",
                    type: "string",
                    resizable: true,
                    visible: true
                }, {
                    field: "dateQuit",
                    displayName: "Дата квитирования",
                    width: "150",
                    type: "string",
                    resizable: true,
                    visible: true
                }];
            }
            else {
                $scope.itemsTitle = "Статус на " + $scope.itemsOpt.date;
                $scope.itemcols = [{
                    field: "objName",
                    displayName: "Объект",
                    type: "string",
                    width: "200",
                    resizable: true,
                    visible: true,
                    sort: {
                        direction: uiGridConstants.ASC,
                        priority: 0
                    }
                }, {
                    field: "parameter",
                    displayName: "Параметр",
                    width: "75",
                    type: "string",
                    resizable: true,
                    visible: true,
                    sort: {
                        direction: uiGridConstants.ASC,
                        priority: 2
                    }
                }, {
                    field: "min",
                    displayName: "Мин.",
                    width: "50",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Value" ? false : true
                }, {
                    field: "max",
                    displayName: "Макс.",
                    width: "75",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Value" ? false : true
                }, {
                    field: "nightMin",
                    displayName: "Мин.(ночь)",
                    width: "50",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Value" ? false : true
                }, {
                    field: "nightMax",
                    displayName: "Макс.(ночь)",
                    width: "75",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Value" ? false : true
                }, {
                    field: "dayStart",
                    displayName: "Нач.дня",
                    width: "50",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Value" ? false : true
                }, {
                    field: "dayEnd",
                    displayName: "Конец дня",
                    width: "50",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Value" ? false : true
                }, {
                    field: "value",
                    displayName: "Текущее значение",
                    width: "100",
                    type: "string",
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Set" ? false : true
                }, {
                    field: "date",
                    displayName: "Дата",
                    width: "150",
                    cellFilter: 'date:\'dd.MM.yyyy HH:mm:ss\'',
                    resizable: true,
                    visible: $scope.itemsOpt.type == "Set" ? false : true
                }];
            }

        }).error(function (e) {
            $log.error("ошибка");
        });
    }

    $scope.ok = function () {
        $modalInstance.close();
    };
});
