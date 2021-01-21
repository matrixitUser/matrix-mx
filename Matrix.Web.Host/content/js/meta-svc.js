angular.module("app")
    
.service("metaSvc", function () {
    var service = this;

    service.configs = ["orenburg", "matrix", "teplocom", "agidel", "gst"];
    service.config = "matrix";
    service.version = "3.1.1";
    

    //РЕСУРСЫ

    var HOUR_NAME = "Часы";
    var DAY_NAME = "Сутки";
    var CURRENT_NAME = "Текущие";

    service.gazResource = [{
        name: "Q н.у.",
        _dispName: "Расход при н.у.",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "Qnt",
        _dispName: "Расход при н.у. (итог)",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "Q р.у.",
        _dispName: "Расход при р.у.",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "Qwt",
        _dispName: "Расход при р.у. (итог)",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "dP",
        _dispName: "Перепад давления",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "P",
        _dispName: "Давление",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "T",
        _dispName: "Температура",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "ВНР",
        _dispName: "Время наработки",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    },
    ///----часы----///
    {
        name: "Q н.у.",
        _dispName: "Расход при н.у.",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "Qnt",
        _dispName: "Расход при н.у. (итог)",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "Q р.у.",
        _dispName: "Расход при р.у.",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "Qwt",
        _dispName: "Расход при р.у. (итог)",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "dP",
        _dispName: "Перепад давления",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "P",
        _dispName: "Давление",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "T",
        _dispName: "Температура",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "ВНР",
        _dispName: "Время наработки",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }];



    service.energyResource = [{
        name: "Тариф1ЭЭ",
        _dispName: "Потребление по тарифу 1",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "Тариф2ЭЭ",
        _dispName: "Потребление по тарифу 2",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "Тариф3ЭЭ",
        _dispName: "Потребление по тарифу 3",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    }, {
        name: "ЭЭ",
        _dispName: "Потребление по сумме тарифов",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Day",
        _dataTypeName: DAY_NAME
    },
    ///----часы----///
    {
        name: "01",
        _dispName: "Q+",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "02",
        _dispName: "Q-",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "03",
        _dispName: "A+",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }, {
        name: "04",
        _dispName: "A-",
        parameter: "",
        calc: "",
        type: "Tag",
        dataType: "Hour",
        _dataTypeName: HOUR_NAME
    }];

    service.resources = [{
        name: "gaz",
        caption: "Газ",
        parameters: service.gazResource
    }, {
        name: "energy",
        caption: "Электроэнергия",
        parameters: service.energyResource
    }, {
        name: "water",
        caption: "Вода"
    },{
        name: "heat",
        caption: "Тепло"
    },{
        name: "valveControl",
        caption: "Управление задвижками"
    }, {
        name: "light",
        caption: "Освещение v1"
    }, {
        name: "lightV2",
        caption: "Освещение v2"
    }, {
        name: "softStartControl",
        caption: "УПП"
    }
    ];



    service.getReasonByCode = function (code) {
        //var date = Date.now();
        var reason = "";
        switch (code) {
            case 0: reason = "Опрос успешно завершён "; break;
            case 10: reason = "Ожидание с "; break;
            case 20: reason = "Идёт опрос "; break;

            case 100: reason = "нет ответа"; break;
                //case 101: reason = "паспорт не прочитан"; break;
                //case 102: reason = "текущие не прочитаны"; break;
                //case 103: reason = "константы не прочитаны"; break;
                //case 104: reason = "сутки не прочитаны"; break;
                //case 105: reason = "часы не прочитаны"; break;
                //case 106: reason = "НС не прочитаны	"; break;

                //case 200: reason = "отмена задачи"; break;
                //case 201: reason = "команда не поддерживается"; break;
                //case 202: reason = "параметр опроса не задан"; break;
                //case 203: reason = "параметр опроса задан с ошибкой"; break;
                //case 205: reason = "некорректный драйвер"; break;
                //case 206: reason = "пустая задача"; break;
                //case 207: reason = "опрос не требуется"; break;

                //case 300: reason = "узел заблокирован"; break;
                //case 301: reason = "контроллер матрикс не на связи"; break;
                //case 302: reason = "не удалось дозвониться до модема, нет несущей"; break;
                //case 303: reason = "не удалось дозвониться до модема, модем занят"; break;
                //case 304: reason = "окно опроса закрыто (устареет вскоре)"; break;
                //case 305: reason = "модем не отвечает на АТ комманды"; break;
                //case 306: reason = "нет HTTP соединения"; break;
                //case 307: reason = "сокет не соединен"; break;

                //case 666: reason = "в опросе не участвует"; break;
                //case 999: reason = "неизвестная ошибка"; break;
            case 101: reason = "паспорт не прочитан"; break;
            case 102: reason = "текущие не прочитаны"; break;
            case 103: reason = "константы не прочитаны"; break;
            case 104: reason = "сутки не прочитаны"; break;
            case 105: reason = "часы не прочитаны"; break;
            case 106: reason = "НС не прочитаны	"; break;

            case 200: reason = "отмена задачи"; break;
            case 201: reason = "команда не поддерживается"; break;
            case 202: reason = "параметр опроса не задан"; break;
            case 205: reason = "некорректный драйвер"; break;
            case 206: reason = "пустая задача"; break;
            case 207: reason = "опрос не требуется"; break;

            case 300: reason = "узел заблокирован"; break;
            case 301: reason = "контроллер матрикс не на связи"; break;
            case 302: reason = "не удалось дозвониться до модема, нет несущей"; break;
            case 303: reason = "не удалось дозвониться до модема, модем занят"; break;
            case 304: reason = "окно опроса закрыто (устареет вскоре)"; break;
            case 305: reason = "модем не отвечает на АТ комманды"; break;
            case 306: reason = "нет HTTP соединения"; break;
            case 307: reason = "сокет не соединен"; break;
            case 308: reason = "не найдена директория"; break;
            case 309: reason = "ресурс занят или недоступен"; break;
            case 310: reason = "нет ответа от вычислителя"; break;

            case 666: reason = "отключен"; break;
            case 999: reason = "неизвестная ошибка"; break;
        }
        return reason;
    }

    service.getImgByCode = function (code) {
        if (code === 0) {
            return "tick.png";
        }

        if (code < 100) {
            switch (code) {
                case 10: return "time.png";
                case 20: return "loader.gif";
            }
        }

        return "warning.png";
    }

    service.connectionImgFromType = function (type) {
        var img = '/img/cog.png';
        switch (type) {
            case 'Tube':
                img = '/img/counter.png';
                break;
            case 'MatrixConnection':
                img = '/img/fastrack.png';
                break;
            case 'TeleofisWrxConnection':
                img = '/img/teleofiswrx.png';
                break;
            case 'MatrixTerminalConnection':
                img = '/img/teleofiswrx.png';
                break;
            case 'ComConnection':
            case 'ComPort':
                img = '/img/port.png';
                break;
            case 'CsdConnection':
                img = '/img/phone_vintage.png';
                break;
            case 'LanConnection':
            case 'TcpClient':
                img = '/img/network_adapter.png'
                break;
            case 'HttpConnection':
                img = '/img/globe_network.png';
                break;
            case 'ZigbeePort':
            case 'ZigbeeConnection':
                img = '/img/network_wireless.png';
                break;
            case 'MxModbus':
                img = img;
                break;
        }
        return img;
    }




    //Формат: 
    //<PARAMETERS> = <CHANNEL>|<CHANNEL>|...
    //<CHANNEL> = <CH_N>;<START>;<K>;<PARAMETER>;<UNIT>
    
    service.rowParameters = [{
        name: "number",
        caption: "Номер канала",
        type: "integer",
        order: 1,
        unique: true,
        required: true
    }, {
        name: "start",
        caption: "Начальное значение",
        type: "float",
        order: 4,
        init: 0.0
    }, {
        name: "k",
        caption: "Коэффициент",
        type: "float",
        order: 5,
        init: 1.0
    }, {
        name: "parameter",
        caption: "Параметр",
        type: "string",
        order: 2,
        unique: true,
        required: true
    }, {
        name: "unit",
        caption: "Ед. измерения",
        type: "string",
        order: 3
    }, {
        name: "snumber",
        caption: "Заводской номер счётчика",
        type: "string",
        order: 6,
        unique: true
    }, {
        name: "comment",
        caption: "Примечание",
        type: "string",
        order: 7
    }, {
        name: "cntType",
        caption: "Тип счетчика",
        type: "string",
        order: 8
    }, {
        name: "square",
        caption: "Площадь квартиры",
        type: "string",
        order: 9
    }, {
        name: "fio",
        caption: "ФИО",
        type: "string",
        order: 10
    }, {
        name: "unknown",
        caption: "Доп.поле",
        type: "string",
        order: 11
    }];

    service.rowObises= [{
        name: "obis",
        caption: "OBIS",
        type: "string",
        order: 1,
        unique: true,
        required: true
    }, {
        name: "objectType",
        caption: "Type",
        type: "string",
        order: 2,
        required: true
    }];
})
    .service("metaFunctionsSvc", function ($actions, $poll, logSvc, controlSvc, mapsSvc, $uibModal, $transport, $window, $log, windowsSvc, $helper, $filter, metaSvc) {
    var service = this;

    service.appStart = function () {

        $actions.push({
            name: "log-subscribe",
            header: "Подписка на логи",
            icon: "/img/action_log.png",
            previlegied: true,
            act: function (arg) {
                logSvc.subscribe(arg).then(function (msg) {
                    $window.alert("Успешно подписан на логи!");
                }, function (err) {
                    $window.alert("Подписка не удалась: " + err);
                });
            }
        });

        $actions.push({
            name: "log-unsubscribe",
            header: "Отписка от логов",
            icon: "/img/action_log.png",
            previlegied: true,
            act: function (arg) {
                logSvc.unsubscribe(arg).then(function (msg) {
                    $window.alert("Успешно отписан от логов!");
                }, function (err) {
                    $window.alert("Отписка не удалась: " + err);
                });
            }
        });


        $actions.push({
            name: "poll-cancel",
            header: "Отменить опрос",
            icon: "/img/cancel.png",
            previlegied: true,
            act: function (arg) {
                $poll.cancel(arg);
            }
        });
        $actions.push({
            name: "delTube",
            header: "Удалить",
            icon: "/img/cancel.png",
            previlegied: true,
            act: function (arg) {
                $transport.send(new Message({ what: "edit-delate-tube" }, { objectIds: arg }));
            }
        });
        $actions.push({
            name: "poll-current",
            header: "Опрос: текущие",
            icon: "/img/ping_refresh.png",
            previlegied: true,
            act: function (arg) {
                $poll.poll("current", arg, {});
            }
        });

        $actions.push({
            name: "poll-all",
            header: "Начать опрос",
            icon: "/img/play.png",
            previlegied: true,
            act: function (arg, param) {
                if (!param) param = {};
                $poll.poll("all", arg, param);
            }
        });

        $actions.push({
            name: "poll-matrix-cancel",
            header: "Матрикс: отмена",
            icon: "/img/cancel.png",
            previlegied: true,
            act: function (arg) {
                $poll.cancel(arg, { redirect: "MatrixConnection" });
            }
        });

        $actions.push({
            name: "poll-matrix-at",
            header: "Матрикс: АТ",
            icon: "/img/fastrack.png",
            previlegied: true,
            act: function (arg, command) {
                if (!command || command == "") command = "AT";
                $poll.poll("at", arg, { command: command }, "MatrixConnection");
            }
        });

        $actions.push({
            name: "poll-matrix-version",
            header: "Матрикс: версия",
            icon: "/img/fastrack.png",
            previlegied: true,
            act: function (arg) {
                $poll.poll("version", arg, {}, "MatrixConnection");
            }
        });

        $actions.push({
            name: "poll-matrix-change",
            header: "Матрикс: смена сервера",
            icon: "/img/fastrack.png",
            previlegied: true,
            act: function (arg, server) {
                $poll.poll("change", arg, { server: server }, "MatrixConnection");
            }
        });

        // Windows

        $actions.push({
            name: "log-show",
            header: "Опрос",
            icon: "/img/ping_icon.png",
            previlegied: true,
            act: function (arg) {
                logSvc.subscribe(arg).then(function (msg) {
                    $log.debug("Успешно подписан на логи!");
                }, function (err) {
                    $log.debug("Подписка не удалась: " + err);
                });

                var unsubscribe = function (arg) {
                    logSvc.unsubscribe(arg).then(function (msg) {
                        $log.debug("Успешно отписан от логов!");
                    }, function (err) {
                        $log.debug("Отписка не удалась: " + err);
                    });
                }
                
                windowsSvc.open({
                    type: "log-show",
                    templateUrl: 'tpls/log-mini.html',
                    modalTemplateUrl: 'tpls/log-modal.html',
                    windowTemplateUrl: 'tpls/modal-tpl-full.html',
                    data: arg
                })
                .finally(function () {
                    unsubscribe(arg);
                });
            }
        });
        
        $actions.push({
            name: "rowProperties-show",
            header: "Свойства",
            icon: "/img/application.png",
            previlegied: false,
            act: function (arg) {
                mapsSvc.subscribe(arg).then(function (msg) {
                    $log.debug("Успешно подписан на логи!");
                }, function (err) {
                    $log.debug("Подписка не удалась: " + err);
                });

                var unsubscribe = function (arg) {
                    mapsSvc.unsubscribe(arg).then(function (msg) {
                        $log.debug("Успешно отписан от логов!");
                    }, function (err) {
                        $log.debug("Отписка не удалась: " + err);
                    });
                }
                if ($window.listModel.isRowProperties) {
                    $window.listModel.isRowProperties = false;
                    $window.listModel.rowPropertiesHeightMultip = 0;
                    $window.listModel.rowPropertiesHeightDivis = 1;
                    if ($window.listModel.isMap) {
                        $window.listModel.mapHeightMultip = 3;
                        $window.listModel.mapHeightDivis = 4;
                        $window.listModel.listHeightDivis = 4;
                    }
                    else {
                        $window.listModel.mapHeightMultip = 0;
                        $window.listModel.mapHeightDivis = 1;
                        $window.listModel.listHeightDivis = 1;
                    }
                    $window.listModel.listHeightMultip = 1;
                    var api = $window.listModel.grid2.api;
                    api.refreshView();
                }
                else {
                    $window.listModel.isRowProperties = true;
                    if ($window.listModel.isMap) {
                        $window.listModel.rowPropertiesHeightMultip = 2;
                        $window.listModel.rowPropertiesHeightDivis = 5;
                        $window.listModel.mapHeightMultip = 2;
                        $window.listModel.mapHeightDivis = 5;
                        $window.listModel.listHeightDivis = 5;
                    }
                    else {
                        $window.listModel.rowPropertiesHeightMultip = 3;
                        $window.listModel.rowPropertiesHeightDivis = 4;
                        $window.listModel.mapHeightMultip = 0;
                        $window.listModel.mapHeightDivis = 1;
                        $window.listModel.listHeightDivis = 4;
                    }
                    $window.listModel.listHeightMultip = 1;
                    $window.listModel.rowProperties();
                    var api = $window.listModel.grid2.api;
                    api.refreshView();
                }
            }
        });
        $actions.push({
            name: "report-edit",
            header: "Редактор отчётов",
            icon: "/img/report_edit.png",
            previlegied: true,
            act: function (arg) {
                windowsSvc.open({
                    type: "report-edit",
                    only1: true,
                    templateUrl: 'tpls/report-edit-mini.html',
                    modalTemplateUrl: 'tpls/report-edit-modal.html',
                    windowTemplateUrl: 'tpls/modal-tpl-full.html',
                    data: arg
                });
            }
        });

        $actions.push({
            name: "report-list",
            header: "Отчёты",
            icon: "/img/report.png",
            previlegied: false,
            act: function (arg) {
                windowsSvc.open({
                    type: "report-list",
                    templateUrl: 'tpls/report-list-mini.html',
                    modalTemplateUrl: 'tpls/report-list-modal.html',
                    windowTemplateUrl: 'tpls/modal-tpl-full.html',
                    data: arg
                });
            }
        });

        $actions.push({
            name: "mailer",
            header: "Рассылка отчётов",
            icon: "/img/report_stack.png",
            previlegied: true,
            act: function (arg) {
                return windowsSvc.open({
                    type: "mailer",
                    templateUrl: 'tpls/mailer-mini.html',
                    modalTemplateUrl: 'tpls/mailer-modal.html',
                    data: arg
                });
            }
        });

        $actions.push({
            name: "mailer-edit",
            header: "Редактор рассылок",
            icon: "/img/report_stack.png",
            previlegied: true,
            act: function (arg) {
                return windowsSvc.open({
                    type: "mailer-edit",
                    templateUrl: 'tpls/mailer-edit-mini.html',
                    modalTemplateUrl: 'tpls/mailer-edit-modal.html',
                    data: arg
                });
            }
        });

        // Modal 

        $actions.push({
            name: "folder-edit",
            header: "Изменить",
            icon: "/img/folder_edit.png",
            previlegied: true,
            act: function (arg) {
                var modal = $uibModal.open({
                    templateUrl: 'tpls/folder-edit-modal.html',
                    controller: 'FolderEditCtrl',
                    size: 'md',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
                return modal.result;
            }
        });

        $actions.push({
            name: "add-to-folder",
            header: "Группы",
            icon: "/img/folders.png",
            previlegied: true,
            act: function (arg) {
                var modal = $uibModal.open({
                    templateUrl: 'tpls/add-to-folder-modal.html',
                    controller: 'AddToFolderCtrl',
                    size: 'md',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
                return modal.result;
            }
        });

        $actions.push({
            name: "rights-edit",
            header: "Права",
            icon: "/img/group_key.png",
            previlegied: true,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/rights-edit-modal.html',
                    controller: 'SetRightsCtrl',
                    size: 'md',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
            }
        });



        $actions.push({
            name: "device-list",
            header: "Типы вычислителей",
            icon: "/img/counter.png",
            previlegied: true,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/device-list-modal.html',
                    controller: 'DriversCtrl',
                    size: 'lg',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
            }
        });

        $actions.push({
            name: "object-card",
            header: "Карточка объекта",
            icon: "/img/infocard.png",
            previlegied: false,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/object-card.html',
                    controller: 'ObjectCardCtrl',
                    size: 'lg',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
            }
        });

        $actions.push({
            name: "acknowledge-card",
            header: "Карточка квитирования",
            icon: "/img/infocard.png",
            previlegied: false,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/billing-card.html',
                    controller: 'BillingCardCtrl',
                    size: 'md',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
            }
        });
        $actions.push({
            name: "about",
            header: "О программе",
            icon: "/img/information.png",
            previlegied: false,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/about-modal.html',
                    controller: 'AboutCtrl',
                    size: 'md'
                });
            }
        });

        $actions.push({
            name: "row-editor",
            header: "Редактировать",
            icon: "/img/edit_button.png",
            previlegied: true,
            act: function (arg) {
                var modal = $uibModal.open({
                    templateUrl: 'tpls/row-editor.html',
                    controller: 'RowEditorCtrl',
                    size: 'lg',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
                return modal.result;
            }
        });

        $actions.push({
            name: "house-editor",
            header: "Редактировать",
            icon: "/img/edit_button.png",
            previlegied: true,
            act: function (arg) {
                var modal = $uibModal.open({
                    templateUrl: 'tpls/house-editor.html',
                    controller: 'HouseEditorCtrl',
                    size: 'lg',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
                return modal.result;
            }
        });
       
        $actions.push({
            name: "user-list",
            header: "Пользователи",
            icon: "/img/users_3.png",
            previlegied: true,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/users-list.html',
                    controller: 'usersCtrl',
                    size: 'lg'
                });
            }
        });

        $actions.push({
            name: "vserial",
            header: "Виртуальный COM-порт",
            icon: "/img/port.png",
            previlegied: true,
            act: function (arg) {
                //$uibModal.open({
                //    templateUrl: 'tpls/vserial-modal.html',
                //    controller: 'VSerialCtrl',
                //    size: 'md',
                //    data: arg
                //});
                windowsSvc.open({
                    type: "vserial",
                    templateUrl: 'tpls/vserial-mini.html',
                    modalTemplateUrl: 'tpls/vserial-modal.html',
                    size: 'md',
                    data: arg
                });
            }
        });

        $actions.push({
            name: "folders",
            header: "Группы",
            icon: "/img/folders.png",
            previlegied: false,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/folders-modal.html',
                    controller: 'ModalCtrl',
                    size: 'md'
                });
            }
        });

        $actions.push({
            name: "actions",
            header: "Действия",
            icon: "/img/action_log.png",
            previlegied: false,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/actions-modal.html',
                    controller: 'ModalCtrl',
                    size: 'md'
                });
            }
        });

        $actions.push({
            name: "windows",
            header: "Окна",
            icon: "/img/application_view_columns.png",
            previlegied: false,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/windows-modal.html',
                    controller: 'ModalCtrl',
                    size: 'md'
                });
            }
        });

        $actions.push({
            name: "task-edit",
            header: "Редактор расписаний",
            icon: "/img/clock.png",
            previlegied: true,
            act: function (arg) {
                return windowsSvc.open({
                    type: "task-edit",
                    templateUrl: 'tpls/task-edit-mini.html',
                    modalTemplateUrl: 'tpls/task-edit-modal.html',
                    data: arg
                });
            }
        });
        $actions.push({
            name: "parameters-edit-classic",
            header: "Параметры",
            icon: "/img/tag_blue.png",
            previlegied: true,
            act: function (arg) {
                $uibModal.open({
                    templateUrl: 'tpls/parameters-edit-classic.html',
                    controller: 'ParametersEditClassicCtrl',
                    size: 'md',
                    resolve: {
                        data: function () {
                            return arg;
                        }
                    }
                });
            }
        });
        $actions.push({
            name: "service",
            header: "Сервис",
            icon: "/img/cog.png",
            previlegied: true,
            act: function (arg) {
                var modalInstance = $uibModal.open({
                    animation: true,
                    templateUrl: "tpls/service-modal.html",
                    controller: "ServiceCtrl",
                    size: "md"
                });
            }
        });
        switch (metaSvc.config)
        {
            case "orenburg":
                $actions.push({
                    name: "parameters-edit",
                    header: "Параметры",
                    icon: "/img/tag_blue.png",
                    previlegied: true,
                    act: function (arg) {
                        $uibModal.open({
                            templateUrl: 'tpls/parameters-edit.html',
                            controller: 'ParametersEditCtrl',
                            size: 'md',
                            resolve: {
                                data: function () {
                                    return arg;
                                }
                            }
                        });
                    }
                });
                break;

            case "teplocom":
                $actions.push({
                    name: "parameters-edit-classic",
                    header: "Параметры",
                    icon: "/img/tag_blue.png",
                    previlegied: true,
                    act: function (arg) {
                        $uibModal.open({
                            templateUrl: 'tpls/parameters-edit-classic.html',
                            controller: 'ParametersEditClassicCtrl',
                            size: 'md',
                            resolve: {
                                data: function () {
                                    return arg;
                                }
                            }
                        });
                    }
                });
                break;

            case "matrix": 
                $actions.push({
                    name: "house-show",
                    header: "Поквартирный учёт",
                    icon: "/img/house_two.png",
                    previlegied: false,
                    act: function (arg) {
                        windowsSvc.open({
                            type: "house-show",
                            templateUrl: 'tpls/house-mini.html',
                            modalTemplateUrl: 'tpls/house-modal.html',
                            windowTemplateUrl: 'tpls/modal-tpl-full.html',
                            size: 'lg',
                            data: arg
                        });
                    }
                });
                
                $actions.push({
                    name: "maquette-list",
                    header: "Отправка макетов 80020",
                    icon: "/img/xml_exports.png",
                    previlegied: true,
                    act: function (arg) {
                        windowsSvc.open({
                            type: "maquette-list",
                            templateUrl: 'tpls/maquette-list-mini.html',
                            modalTemplateUrl: 'tpls/maquette-list-modal.html',
                            data: arg
                        });
                    }
                });

                $actions.push({
                    name: "maquette-edit",
                    header: "Редактор макетов 80020",
                    icon: "/img/xml_exports.png",
                    previlegied: true,
                    act: function (arg) {
                        return windowsSvc.open({
                            type: "maquette-edit",
                            templateUrl: 'tpls/maquette-edit-mini.html',
                            modalTemplateUrl: 'tpls/maquette-edit-modal.html',
                            data: arg
                        });
                    }
                });

                $actions.push({
                    name: "events-show",
                    header: "События",
                    icon: "/img/lightning.png",
                    previlegied: false,
                    act: function (arg) {
                        windowsSvc.open({
                            type: "events-show",
                            templateUrl: 'tpls/events-show-mini.html',
                            modalTemplateUrl: 'tpls/events-show.html',
                            size: 'lg',
                            data: arg
                        });
                    }
                });

                $actions.push({
                    name: "poll-ping",
                    header: "Опрос: пинг",
                    icon: "/img/ping_check.png",
                    previlegied: true,
                    act: function (arg) {
                        $poll.poll("ping", arg, {});
                    }
                });
                                
                $actions.push({
                    name: "control-show",
                    header: "Управление",
                    icon: "/img/cog.png",
                    previlegied: true,
                    act: function (arg) {
                        controlSvc.subscribe(arg).then(function (msg) {
                            $log.debug("Успешно подписан на логи!");
                        }, function (err) {
                            $log.debug("Подписка не удалась: " + err);
                        });

                        var unsubscribe = function (arg) {
                            controlSvc.unsubscribe(arg).then(function (msg) {
                                $log.debug("Успешно отписан от логов!");
                            }, function (err) {
                                $log.debug("Отписка не удалась: " + err);
                            });
                        }

                        windowsSvc.open({
                            type: "control-show",
                            templateUrl: 'tpls/control-mini.html',
                            modalTemplateUrl: 'tpls/control-modal.html',
                            windowTemplateUrl: 'tpls/modal-tpl-full-control.html',
                            data: arg
                        })
                            .finally(function () {
                                unsubscribe(arg);
                            });
                    }
                });
                $actions.push({
                    name: "maps-show",
                    header: "Карта",
                    icon: "/img/globe_place.png",
                    previlegied: true,
                    act: function (arg) {
                        mapsSvc.subscribe(arg).then(function (msg) {
                            $log.debug("Успешно подписан на логи!");
                        }, function (err) {
                            $log.debug("Подписка не удалась: " + err);
                        });

                        var unsubscribe = function (arg) {
                            mapsSvc.unsubscribe(arg).then(function (msg) {
                                $log.debug("Успешно отписан от логов!");
                            }, function (err) {
                                $log.debug("Отписка не удалась: " + err);
                            });
                        }
                        if ($window.listModel.isMap) {
                            $window.listModel.isMap = false;
                            $window.listModel.mapHeightMultip = 0;
                            $window.listModel.mapHeightDivis = 1;
                            if ($window.listModel.isRowProperties) {
                                $window.listModel.rowPropertiesHeightMultip = 3;
                                $window.listModel.rowPropertiesHeightDivis = 4;
                                $window.listModel.listHeightDivis = 4;
                            }
                            else {
                                $window.listModel.rowPropertiesHeightMultip = 0;
                                $window.listModel.rowPropertiesHeightDivis = 1;
                                $window.listModel.listHeightDivis = 1;
                            }
                            $window.listModel.listHeightMultip = 1;
                            var api = $window.listModel.grid2.api;
                            api.refreshView();
                        }
                        else {
                            $window.listModel.isMap = true;
                            if ($window.listModel.isRowProperties) {
                                $window.listModel.rowPropertiesHeightMultip = 2;
                                $window.listModel.rowPropertiesHeightDivis = 5;
                                $window.listModel.mapHeightMultip = 2;
                                $window.listModel.mapHeightDivis = 5;
                                $window.listModel.listHeightDivis = 5;
                            }
                            else {
                                $window.listModel.mapHeightMultip = 3;
                                $window.listModel.mapHeightDivis = 4;
                                $window.listModel.rowPropertiesHeightMultip = 0;
                                $window.listModel.rowPropertiesHeightDivis = 1;
                                $window.listModel.listHeightDivis = 4;
                            }
                            $window.listModel.listHeightMultip = 1;
                            var api = $window.listModel.grid2.api;
                            api.refreshView();
                        }
                    }
                });
                $actions.push({
                    name: "calculator-modal",
                    header: "Калькулятор",
                    icon: "/img/edit_button.png",
                    previlegied: false,
                    act: function (arg) {
                        windowsSvc.open({
                            type: "report-list",
                            templateUrl: 'tpls/calculator-mini.html',
                            modalTemplateUrl: 'tpls/calculator-modal.html',
                            windowTemplateUrl: 'tpls/modal-tpl-full.html',
                            data: arg
                        });
                    }
                });
                $actions.push({
                    name: "valveControl-card",
                    header: "Карточка управления задвижками",
                    icon: "/img/infocard.png",
                    previlegied: false,
                    act: function (arg) {
                        $uibModal.open({
                            templateUrl: 'tpls/valve-control-card.html',
                            controller: 'ValveControlCardCtrl',
                            size: 'md',
                            resolve: {
                                data: function () {
                                    return arg;
                                }
                            }
                        });
                    }
                });
                $actions.push({
                    name: "billing-card",
                    header: "Карточка биллинга",
                    icon: "/img/infocard.png",
                    previlegied: false,
                    act: function (arg) {
                        $uibModal.open({
                            templateUrl: 'tpls/billing-card.html',
                            controller: 'BillingCardCtrl',
                            size: 'md',
                            resolve: {
                                data: function () {
                                    return arg;
                                }
                            }
                        });
                    }
                });
                break;
        }

        //$actions.push({
        //    name: "report-build",
        //    header: "Отчёт",
        //    icon: "/img/report.png",
        //    previlegied: false,
        //    act: function (arg) {
        //        windowsSvc.open({
        //            type: "report-build",
        //            templateUrl: 'tpls/report-mini.html',
        //            modalTemplateUrl: 'tpls/report-modal.html',
        //            data: arg
        //        });
        //    }
        //});

        //$actions.push({
        //    name: "test",
        //    header: "Тест многозадачности",
        //    icon: "/img/action_log.png",
        //    previlegied: false,
        //    act: function (arg) {
        //        windowsSvc.open({ templateUrl: 'tpls/test-mini.html', modalTemplateUrl: 'tpls/test-modal.html', data: arg });
        //    }
        //});
        
        //$actions.push({
        //    name: "data-table",
        //    header: "Таблица",
        //    icon: "/img/table.png",
        //    previlegied: false,
        //    act: function (arg) {
        //        $uibModal.open({
        //            templateUrl: 'tpls/data-table.html',
        //            controller: 'dataTableCtrl',
        //            size: 'lg',
        //            resolve: {
        //                ids: function () {
        //                    return arg;
        //                }
        //            }
        //        });
        //    }
        //});

        //$actions.push({
        //    name: "folders-aside",
        //    header: "Группы",
        //    icon: "/img/explorer.png",
        //    act: function (arg) {
        //        $uibModal.open({
        //            templateUrl: 'tpls/folders-aside.html',
        //            windowTemplateUrl: 'tpls/modal-tpl-aside.html',
        //            controller: 'FoldersAsideCtrl'
        //        });
        //    }
        //});
        //$actions.push({
        //    name: "folder-edit",
        //    header: "Каталог",
        //    icon: "/img/folder.png",
        //    previlegied: true,
        //    act: function (arg) {
        //        $uibModal.open({
        //            templateUrl: 'tpls/rights-edit-modal.html',
        //            controller: 'SetRightsCtrl',
        //            size: 'md',
        //            resolve: {
        //                data: function () {
        //                    return arg;
        //                }
        //            }
        //        });
        //    }
        //});


        //$actions.push({
        //    name: "rights-edit",
        //    header: "Права",
        //    icon: "/img/group_key.png",
        //    previlegied: true,
        //    act: function (arg) {
        //        $uibModal.open({
        //            templateUrl: 'tpls/rights-edit-modal.html',
        //            controller: 'SetRightsCtrl',
        //            size: 'md',
        //            resolve: {
        //                data: function () {
        //                    return arg;
        //                }
        //            }
        //        });
        //    }
        //});

        //$actions.push({
        //    name: "poll-all-auto",
        //    header: "Опрос: всё",
        //    icon: "/img/play.png",
        //    previlegied: true,
        //    act: function (arg, param) {
        //        if (!param) param = {};
        //        arg.auto = true;
        //        $poll.poll("all", arg, param);
        //    }
        //});

        //$actions.push({
        //    name: "manager-modems",
        //    header: "Менеджер модемов",
        //    icon: "/img/phone_vintage.png",
        //    previlegied: true,
        //    act: function (arg) {
        //        $uibModal.open({
        //            templateUrl: 'tpls/manager-modems-modal.html',
        //            controller: 'managerModemsCtrl',
        //            size: 'lg'
        //        });
        //    }
        //});
    }
});
