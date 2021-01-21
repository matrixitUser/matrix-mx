'use strict';

angular.module("app");

function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}

app.controller("ListCtrl", function ($log, $timeout, $scope, $rootScope, $filter, $window, $transport, metaSvc, $helper,
    $list, $actions, $settings, $listFilter) {
    
    //Контекстное меню
    var menuActions = [
        {
            title: "Добавить в группу", name: 'add-to-folder', getParam: arrayOfSels, isVisible: true, isEnabled: isSelectedAny, success: function (result) {
                if (result) {
                    $rootScope.$broadcast("listFilter:changed");
                }
            }
        },
        null,
        //{ name: 'poll-ping', getParam: arrayOfSels, isEnabled: isSelectedAny },
        { title: "Запустить опрос", name: 'poll-all', getParam: arrayOfSels, getArg: function () { return { start: undefined, end: undefined, components: "Current:3;Hour:2:60;Day:2:60;" }; }, isEnabled: isSelectedAny, isVisible: true },
        { title: "Остановить опрос", name: 'poll-cancel', getParam: arrayOfSels, isEnabled: isSelectedAny, isVisible: true },
        { title: "Удалить", name: 'delTube', getParam: arrayOfSels, isEnabled: isEnabledDelete, isVisible: true },
        null,
        { name: 'log-show', getParam: arrayOfSels, isEnabled: isSelectedAny, isVisible: true },
        { name: 'report-list', getParam: reportArrayOfSels, isVisible: true },
        { title: "Калькулятор ценовых категорий", name: 'calculator-modal', getParam: arrayOfSels, isEnabled: isSelectedAny, isVisible: true }
    ];

    //Ячейка GRID с действиями
    var panelActions = [
        { name: 'object-card', popover: "Карточка объекта", isVisible: function (data) { return (data.class != 'HouseRoot'); } },
        { name: 'house-show', popover: "Карточка объекта", isVisible: function (data) { return (data.class == 'HouseRoot'); } },
        { name: 'valveControl-card', popover: "Карточка управления задвижками", isVisible: function (data) { return (data.class != 'HouseRoot'); } },
        { name: 'billing-card', popover: "Карточка биллинга", isVisible: function (data) { return (data.class != 'HouseRoot'); } },
        {
            name: 'row-editor',
            popover: "Редактор объекта",
            isVisible: function (data) { return (data.class != 'HouseRoot'); }
        }, {
            name: 'house-editor',
            popover: "Редактор дома",
            isVisible: function (data) { return (data.class == 'HouseRoot'); }
        }, {
            name: 'parameters-edit', popover: "Параметры объекта",
            getIcon: function (data) { return "/img/tag_red.png"; },
            isVisible: function (data) { return (data.class != 'HouseRoot'); }
        }, {
            name: 'parameters-edit-classic', popover: "Параметры объекта",
            getIcon: function (data) { return "/img/tag_red.png"; },
            isVisible: function (data) { return (data.class != 'HouseRoot'); }
        }
    ];

    /////////////////// MODEL /////////////////// MODEL /////////////////// MODEL /////////////////// MODEL /////////////////// MODEL /////////////////// 
    var model = {
        rows: [],
        selIds: {},
        pageSize: 25,
        optStatusText: "",
        isLoading: true,
        counting: 0,
        count: 0,
        filterText: "",
        countOpenCounters: 0,
        //
        options: [],
        optionsView: [],
        panelActionsView: [],
        isMap: false,
        mapHeightMultip: 0,
        listHeightMultip: 1,
        isBtnCloseMapsAdd: false,
        mapHeightDivis: 1,
        listHeightDivis: 1,
        rowPropertiesHeightMultip: 0,
        rowPropertiesHeightDivis: 1,
        row: {},
        audio: null,
        //
        foldersShownNames: [],
        foldersShownIds: [],
        //
        modal: {}
    }
    //model.isHouseList = false;//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    var lightControl = [];


    var splitter, cont1, cont2;
    var last_y, window_height;

    $scope.$watch('model.options', function () {
        model.optionsView = [];
        if ($helper.isArray(model.options) && model.options.length > 0) {
            var isAlmost1Action = false;
            var isDivider = false;
            var sort = $filter('orderBy')(model.options, 'index');
            for (var i = 0; i < sort.length; i++) {
                var option = sort[i];
                if (option.type == "divider") {
                    isDivider = true;
                } else {
                    if (isAlmost1Action && isDivider) {
                        isDivider = false;
                        model.optionsView.push(null);
                    }
                    model.optionsView.push(option);
                    isAlmost1Action = true;
                }
            }
        }
    }, true);

    var connection = $transport.getStatus();

    var filterTimeout;
    var listColumnStateTimeout;
    var lastSelectedId;

    // вспомогательные функции

    function arrayOfSels() {
        return getSelectedIds();
    }

    function reportArrayOfSels() {
        var ids = getSelectedIds();
        var arg = {
            ids: ids,
            header: ids.length === 0 ? "" : ids.length + " объектов"
        };
        return arg;
    }

    var filterHidden = function (target) {
        var res = [];
        if (target && target.length) {//filter           
            for (var i = 0; i < target.length; i++) {
                var obj = target[i];
                if (obj.name && obj.name.search("__") == -1) {
                    res.push(obj);
                }
            }
        }
        return res;
    }    
    var matrixId = "undefined";
    model.rowProperties = function () {
        var ids = $list.getSelectedIds();
        var id = ids[0];
        $list.getRows([id]).then(function (rows) {
            var row = rows[0];
            if (row.MatrixConnection && row.MatrixConnection.length > 0) {
                matrixId = row.MatrixConnection[0].id;
            }
            model.row.quantity = { current: 7, constant: 5, abnormal: 15, gsm: 5 };
            model.row.row = row;
            return $list.getRowCard(row.id, matrixId).then(function (body) {
                model.row.currents = filterHidden(body.currents);
                model.row.days = filterHidden(body.days);
                model.row.abnormals = body.abnormals;
                model.row.constants = body.constants;
                model.row.signal = body.signal;
                model.row.addr = body.addr;
                model.row.phone = body.phone;
                model.row.dev = body.dev;
                model.row.number = body.number;
                model.row.address = body.address;
                model.row.records = [];
                if (model.row.dev == "Меркурий230" || model.row.dev == "СЭТ-4М") {
                    var currents = model.row.currents;
                    for (var i = 0; i < currents.length; i++) {
                        if (currents[i].name == "cos φ (фаза 1)") {
                            model.row.phi1x = 150 + 90 * Math.sin(Math.acos(currents[i].value));
                            model.row.phi1y = 150 - 90 * Math.cos(Math.acos(currents[i].value));
                        }
                        if (currents[i].name == "cos φ (фаза 2)") {
                            model.row.phi2x = 150 + 90 * Math.cos(Math.PI / 6 + Math.acos(currents[i].value));
                            model.row.phi2y = 150 + 90 * Math.sin(Math.PI / 6 + Math.acos(currents[i].value));
                        }
                        if (currents[i].name == "cos φ (фаза 3)") {
                            model.row.phi3x = 150 - 90 * Math.cos(Math.PI / 6 - Math.acos(currents[i].value));
                            model.row.phi3y = 150 + 90 * Math.sin(Math.PI / 6 - Math.acos(currents[i].value));
                        }
                        if (currents[i].name == "cos φ (по сумме фаз)") {
                            model.row.phi4y = 50 - 40 * Math.sin(Math.acos(currents[i].value));
                            model.row.phi4x = 50 + 40 * Math.cos(Math.acos(currents[i].value));
                        }
                    }
                }

                if (model.row.days && (model.row.days.length > 0)) {
                    var dtStart = new Date();
                    var dtEnd = new Date();
                    var countDay = 12;
                    dtStart.setDate(dtEnd.getDate() - countDay);
                    dtEnd.setDate(dtEnd.getDate());
                    $list.getRecords([id], dtStart, dtEnd, "Day").then(function (records) {
                        var recs = [];
                        var recs1 = [];
                        model.row.isContainsEE = false;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "ЭЭ") {
                                model.row.isContainsEE = true;
                                var rec = {};
                                rec.date = records[i].date;
                                rec.valueSum = records[i].d1;
                                recs.push(rec);
                            }
                        }
                        if (recs.length == 0) {
                            for (var i = 0; i < records.length; i++) {
                                if (records[i].s1 == "Тариф1ЭЭ") {    // 01
                                    model.row.isContainsEE = true;
                                    var rec = {};
                                    rec.date = records[i].date;
                                    rec.valueSum = records[i].d1;
                                    recs.push(rec);
                                }
                            }
                        }

                        // сбор данных для ХВС, ГВС, ОТОПЛЕНИЯ, диаграмма суточных данных
                        model.row.recsHvs = [];
                        var maxHvs = 0;
                        model.row.isContainsHvs = false;
                        // Взятие данных для ХВС
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "ХВСV1") {
                                var recHvs = {};
                                model.row.isContainsHvs = true;
                                recHvs.t = records[i].date;
                                recHvs.v = records[i].d1;
                                recHvs.x = 50 + i * 70;
                                if (maxHvs < recHvs.v) {
                                    maxHvs = recHvs.v;
                                }
                                model.row.recsHvs.push(recHvs);
                            }
                        }
                        // Расчеты координат для графика
                        model.row.coordHvs = [];
                        var koefHvs = 160 / maxHvs;
                        for (var i = 0; i < model.row.recsHvs.length; i++) {
                            var coordinatesHvs = {};
                            coordinatesHvs.y = 180 - model.row.recsHvs[i].v * koefHvs;
                            coordinatesHvs.x = 50 + i * 70;
                            coordinatesHvs.h = 180 - coordinatesHvs.y;
                            model.row.coordHvs.push(coordinatesHvs);
                        }
                        model.row.constHvs1 = maxHvs;
                        model.row.constHvs2 = maxHvs / 2;

                        model.row.valueHvs = [];
                        for (var i = 0; i < model.row.coordHvs.length; i++) {
                            var valuesHvs = {};
                            valuesHvs.x = model.row.coordHvs[i].x + 8;
                            if (model.row.coordHvs[i].y > 170) {
                                valuesHvs.y = model.row.coordHvs[i].y - 25;
                            }
                            else {
                                valuesHvs.y = model.row.coordHvs[i].y + 15;
                            }
                            valuesHvs.tmp = model.row.recsHvs[i].v;
                            model.row.valueHvs.push(valuesHvs);
                        }

                        // Отпление Отпление Отпление Отпление Отпление Отпление
                        model.row.isContainsW = false;
                        model.row.recs = [];
                        var records1 = [];
                        var records2 = [];
                        // Взятие данных для Отопления, Тепло по трубе 1
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "ОтоплениеW1") {
                                model.row.isContainsW = true;
                                var recW1 = {};
                                recW1.date = records[i].date;
                                recW1.valueSum = records[i].d1;
                                records1.push(recW1);
                            }
                        }
                        // Взятие данных для Отопления, Тепло по трубе 2
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "ОтоплениеW2") {
                                var recW2 = {};
                                recW2.date = records[i].date;
                                recW2.valueSum = records[i].d1;
                                records2.push(recW2);
                            }
                        }
                        // Расчет разницы Отопления1 - Отопления2
                        var maxValue = 0;
                        for (var i = 0; i < records1.length; i++) {
                            for (var j = 0; j < records2.length; j++) {
                                if (records1[i].date == records2[j].date) {
                                    var rec = {};
                                    rec.tmp = records1[i].valueSum - records2[j].valueSum;
                                    if (maxValue < rec.tmp) {
                                        maxValue = rec.tmp;
                                    }
                                    rec.x = 50 + i * 70;
                                    rec.date = records1[i].date;
                                    model.row.recs.push(rec);
                                }
                            }
                        }
                        // Расчеты координат для графика
                        model.row.coord = [];
                        var koefY = 160 / maxValue;
                        for (var i = 0; i < records1.length; i++) {
                            var coordinates = {};
                            coordinates.y = 180 - model.row.recs[i].tmp * koefY;
                            coordinates.x = 50 + i * 70;
                            coordinates.h = 180 - coordinates.y;
                            model.row.coord.push(coordinates);
                        }
                        model.row.const1 = maxValue;
                        model.row.const2 = maxValue / 2;

                        model.row.valueW = [];
                        for (var i = 0; i < model.row.coord.length; i++) {
                            var values = {};
                            values.x = model.row.coord[i].x + 8;
                            if (model.row.coord[i].y > 170) {
                                values.y = model.row.coord[i].y - 25;
                            }
                            else {
                                values.y = model.row.coord[i].y + 15;
                            }
                            values.tmp = model.row.recs[i].tmp;
                            model.row.valueW.push(values);
                        }

                        //   ГВС ГВС ГВС ГВС ГВС ГВС ГВС ГВС
                        model.row.isContainsGvs = false;
                        model.row.recsGvs = [];
                        var recordsGvs1 = [];
                        var recordsGvs2 = [];
                        // Взятие данных для ГВС, Тепло по трубе 1
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "ГВСW1") {
                                model.row.isContainsGvs = true;
                                var recWGvs1 = {};
                                recWGvs1.date = records[i].date;
                                recWGvs1.valueSum = records[i].d1;
                                recordsGvs1.push(recWGvs1);
                            }
                        }
                        // Взятие данных для ГВС, Тепло по трубе 2
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "ГВСW2") {
                                var recWGvs2 = {};
                                recWGvs2.date = records[i].date;
                                recWGvs2.valueSum = records[i].d1;
                                recordsGvs2.push(recWGvs2);
                            }
                        }
                        // Расчет разницы ГВС1 - ГВС2
                        var maxValueGvs = 0;
                        for (var i = 0; i < recordsGvs1.length; i++) {
                            for (var j = 0; j < recordsGvs2.length; j++) {
                                if (recordsGvs1[i].date == recordsGvs2[j].date) {
                                    var recGvs = {};
                                    recGvs.tmp = recordsGvs1[i].valueSum - recordsGvs2[j].valueSum;
                                    if (maxValueGvs < recGvs.tmp) {
                                        maxValueGvs = recGvs.tmp;
                                    }
                                    recGvs.x = 50 + i * 70;
                                    recGvs.date = recordsGvs1[i].date;
                                    model.row.recsGvs.push(recGvs);
                                }
                            }

                        }
                        // Расчеты координат для графика
                        model.row.coordGvs = [];
                        var koefGvs = 160 / maxValueGvs;
                        for (var i = 0; i < recordsGvs1.length; i++) {
                            var coordinatesGvs = {};
                            coordinatesGvs.y = 180 - model.row.recsGvs[i].tmp * koefGvs;
                            coordinatesGvs.x = 50 + i * 70;
                            coordinatesGvs.h = 180 - coordinatesGvs.y;
                            model.row.coordGvs.push(coordinatesGvs);
                        }
                        model.row.constGvs1 = maxValueGvs;
                        model.row.constGvs2 = maxValueGvs / 2;

                        model.row.valueGvs = [];
                        for (var i = 0; i < model.row.coordGvs.length; i++) {
                            var valuesGvs = {};
                            valuesGvs.x = model.row.coordGvs[i].x + 8;
                            valuesGvs.y = model.row.coordGvs[i].y + 15;
                            valuesGvs.tmp = model.row.recsGvs[i].tmp;
                            model.row.valueGvs.push(valuesGvs);
                        }

                        if (model.row.isContainsEE && recs.length > 1) {
                            for (var i = 0; i < countDay; i++) {
                                var dtTmp1 = new Date();
                                var dtTmp2 = new Date();
                                dtTmp1.setDate(dtEnd.getDate() - countDay + i);
                                dtTmp2.setDate(dtEnd.getDate() - countDay + i + 1);
                                //dtTmp1.setDate(dtStart.getDate() + i);
                                //dtTmp2.setDate(dtStart.getDate() + i + 1);
                                var isContainsDate1 = false;
                                var isContainsDate2 = false;
                                var rec1 = {};
                                var rec2 = {};
                                for (var j = 0; j < recs.length; j++) {
                                    var tmp = new Date(recs[j].date);
                                    if (tmp.getDate() == dtTmp1.getDate()) {
                                        isContainsDate1 = true;
                                        rec1 = recs[j];
                                    }
                                    if (tmp.getDate() == dtTmp2.getDate()) {
                                        isContainsDate2 = true;
                                        rec2 = recs[j];
                                    }
                                }
                                if (isContainsDate1 && isContainsDate2) {
                                    rec2.value = rec2.valueSum - rec1.valueSum;
                                    recs1.push(rec2);
                                }
                            }

                            var minValue = recs1[0].value;
                            model.row.records = recs1;
                            google.charts.load('current', { 'packages': ['corechart'] });
                            google.charts.setOnLoadCallback(drawChart);
                            function drawChart() {
                                var data = new google.visualization.DataTable();
                                data.addColumn('string', 'Month');  // Implicit domain label col.
                                data.addColumn('number');           // Implicit series 1 data col.
                                for (var i = 0; i < recs1.length; i++) {
                                    var tmp = new Date(recs1[i].date);
                                    var month = tmp.getMonth() + 1;
                                    var strTmp = tmp.getDate() + "." + month + "." + tmp.getFullYear();
                                    data.addRow([strTmp, recs1[i].value]);
                                    if (minValue > recs1[i].value) {
                                        minValue = recs1[i].value;
                                    }
                                }
                                //data.addRows(arrData);
                                var options = {
                                    legend: { position: "none" },
                                    vAxis: { minValue: minValue }
                                };

                                var chart = new google.visualization.ColumnChart(document.getElementById('chart_div'));
                                chart.draw(data, options);
                            }
                        }
                    })
                }

                //  ПРОФИЛЬ МОЩНОСТИ
                // Взятие данных
                if (model.row.dev == "Меркурий230" || model.row.dev == "СЭТ-4М") {
                    var dtStart = new Date();
                    var dtEnd = new Date();
                    var arrDt = [];
                    dtStart.setDate(dtEnd.getDate() - 3);
                    dtEnd.setDate(dtEnd.getDate());
                    var KTr = model.row.row.KTr;
                    if (KTr == null) KTr = 1;
                    $list.getRecords([id], dtStart, dtEnd, "Hour").then(function (records) {
                        var recs = [];
                        var recordsMax = 0;
                        model.row.isContains01 = false;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "01") {
                                model.row.isContains01 = true;
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1 * KTr;
                                rec.withoutKtr = records[i].d1;
                                if (records[i].d1*KTr > recordsMax) {
                                    recordsMax = records[i].d1 * KTr;
                                }
                                recs.push(rec);
                            }
                        }
                        var upper = recordsMax / 4;
                        model.row.upper = []; 
                        for (var i = 1; i < 6; i++) {
                            var tmpUpY = {};
                            var up = upper * ( i - 1);
                            var y = (5-i)*180/4
                            tmpUpY.up = up;
                            tmpUpY.y = y;
                            model.row.upper.push(tmpUpY);
                        }
                        // настройка координат, можно двигать график меняя только эти две переменные, пока не работает
                        var coordinateX = 40;
                        var coordinateY = 0;

                        // Расчет координат для графика
                        model.row.recordsHour = recs;
                        var strTmp = "";
                        var koefValue = 180/recordsMax;
                        var koefHour = 800 / 72;
                        model.row.tmps = [];
                        model.row.hours00 = [];
                        for (var i = 0; i < recs.length; i++) {
                            var x = i * koefHour + coordinateX;
                            var y = 180 - recs[i].value * koefValue;
                            var z = recs[i].withoutKtr;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            tmp.z = z;
                            model.row.tmps.push(tmp);
                            if (i == recs.length-1) {
                                strTmp += x + " " + y;
                            }
                            else {
                                strTmp += x + " " + y + ",";
                            }
                            var date = new Date(recs[i].date);
                            if (date.getHours() == 0 || date.getHours() == 24) {
                                model.row.hours00.push(x);
                            } 
                        }
                        if (model.row.hours00[1] == null) model.row.hours00[1] = model.row.hours00[0] + 266.6;
                        if (model.row.hours00[2] == null) model.row.hours00[2] = model.row.hours00[1] + 266.6;
                        var tmpi = 0;
                        for (var dtTmp = new Date(dtStart); dtTmp.getTime() <= dtEnd.getTime(); dtTmp.setDate(dtTmp.getDate() + 1)) {
                            if (model.row.hours00[0] == null) break;
                            var tmp1 = {};
                            if (tmpi == 0) {
                                tmpi++;
                                
                                continue;
                            } 
                            
                            
                            tmp1.x = model.row.hours00[tmpi - 1] - 30;
                            tmp1.date = new Date(dtTmp);
                            tmpi++;
                            arrDt.push(tmp1);
                        }

                        model.row.arrDt = arrDt;
                        model.row.profilPower = strTmp;

                        // расчет координат для шкалы деления по оси Y
                        model.row.shkalaY = [];
                        for (var j = 140; j > 40; j = j - 40) {
                            var tmp1 = {};
                            tmp1.y = j;
                            model.row.shkalaY.push(tmp1);
                        }
                    })
                }

                if (model.row.dev == "MPC-Modbus") {
                    var colorList = ["#FF0000", "#4E1A9C", "#CC4F10", "#1CBD0D", "#148196","#FFF82B"]
                    var tmp;
                    var tmpParam = [];
                    var tmpBlock = [];
                    var tmpFullName = [];
                    var tmpName = [];
                    model.row.row.nameGvs = [];
                    model.row.row.nameHvs = [];
                    model.row.row.nameChannel = [];
                    var channel = [];
                    var tmp = model.row.row.parameters;
                    tmpParam = tmp.split('|');
                    var previGvs = 0;
                    var previHvs = 0;
                    for (var i = 0; i < tmpParam.length; i++) {
                        tmpBlock[i] = tmpParam[i].split(';');
                    }
                    for (var i = 0; i < tmpBlock.length; i++)  tmpFullName[i] = tmpBlock[i][3];
                    for (var i = 0; i < tmpFullName.length; i++)  tmpName[i] = tmpFullName[i].split('_');
                    for (var i = 0; i < tmpName.length; i++) {
                        var tmpChannel = {};
                        tmpChannel.number = tmpBlock[i][0];
                        tmpChannel.type = tmpName[i][1];
                        tmpChannel.name = tmpName[i][0];
                        channel.push(tmpChannel);
                        model.row.row.nameChannel.push(name);
                        if ((tmpName[i][1] == "ГВС" || tmpName[i][1] == "ГВС2") && (tmpName[i][0] != "Фитнес 34 м2")) {
                            var name = {};
                            name.ch = tmpBlock[i][0];
                            name.Gvs = tmpName[i][0];
                            name.y = (i - previGvs) * 25 + 17;
                            name.color = colorList[(i - previGvs)];
                            model.row.row.nameGvs.push(name);
                        }
                        else previGvs++;
                        if ((tmpName[i][1] == "ХВС" || tmpName[i][1] == "ХВС2") && (tmpName[i][0] != "Фитнес 34 м2")) {
                            var name = {};
                            name.Hvs = tmpName[i][0];
                            name.y = (i - previHvs) * 25 + 17;
                            name.color = colorList[(i - previHvs)];
                            model.row.row.nameHvs.push(name);
                        }
                        else previHvs++;
                    }
                }

                model.row.row.checkBox2 = false;
                model.row.row.checkBox4 = false;
                model.row.row.checkBox12 = false;
                model.row.row.checkBox14 = false;
                model.row.row.checkBox16 = false;

                model.row.row.checkBox3 = false;
                model.row.row.checkBox5 = false;
                model.row.row.checkBox6 = false;
                model.row.row.checkBox11 = false;
                model.row.row.checkBox13 = false;
                model.row.row.checkBox15 = false;

                var draw = SVG().addTo('#drawing').size(1200, 300)
                var a = 30; var b = 1180;
                var line = draw.line(a, 50, b, 50).stroke({ width: 1, color: '#7a8491' })
                var line = draw.line(a, 75, b, 75).stroke({ width: 0.5, color: '#7a8491' })
                var line = draw.line(a, 100, b, 100).stroke({ width: 1, color: '#7a8491' })
                var line = draw.line(a, 125, b, 125).stroke({ width: 0.5, color: '#7a8491' })
                var line = draw.line(a, 150, b, 150).stroke({ width: 1, color: '#7a8491' })
                var line = draw.line(a, 175, b, 175).stroke({ width: 0.5, color: '#7a8491' })
                var line = draw.line(a, 200, b, 200).stroke({ width: 1, color: '#7a8491' })
                var line = draw.line(a, 225, b, 225).stroke({ width: 0.5, color: '#7a8491' })
                var line = draw.line(a, 250, b, 250).stroke({ width: 1, color: '#7a8491' })

                //  MPC-MODBUS MPC-MODBUS MPC-MODBUS MPC-MODBUS MPC-MODBUS MPC-MODBUS MPC-MODBUS 
                // Взятие данных
                var recordsMaxGvs = [];
                var recordsMaxHvs = [];
                var recordsAbsMaxGvs = 0;
                var recordsAbsMaxHvs = 0;
                if (model.row.dev == "MPC-Modbus") {
                    var dtStart = new Date();
                    var dtEnd = new Date();
                    var arrDt = [];
                    dtStart.setDate(dtStart.getDate() - 3);
                    dtEnd.setDate(dtEnd.getDate());
                    $list.getRecords([id], dtStart, dtEnd, "Hour").then(function (records) {
                        ////////////  КАНАЛ 2 сбор
                        model.row.Modbus2 = [];
                        var tmp2 = [];
                        var recordsMax2 = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал2") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp2.push(rec);
                                model.row.row.checkBox2 = true;
                            }
                        }
                        for (var i = 0; i < tmp2.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp2[i + 1].value - tmp2[i].value;
                            rec1.date = tmp2[i].date;
                            model.row.Modbus2.push(rec1);
                            if (rec1.value > recordsMax2) recordsMax2 = rec1.value;
                        }
                        recordsMaxGvs.push(recordsMax2);

                        //////////////  КАНАЛ 3 сбор
                        model.row.Modbus3 = [];
                        var tmp3 = [];
                        var recordsMax3 = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал3") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp3.push(rec);
                                model.row.row.checkBox3 = true;
                            }
                        }
                        for (var i = 0; i < tmp3.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp3[i + 1].value - tmp3[i].value;
                            rec1.date = tmp3[i].date;
                            model.row.Modbus3.push(rec1);
                            if (rec1.value > recordsMax3) recordsMax3 = rec1.value;
                        }
                        recordsMaxHvs.push(recordsMax3);

                        ////////////  КАНАЛ 4 сбор
                        model.row.Modbus4 = [];
                        var tmp4 = [];
                        var recordsMax4 = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал4") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp4.push(rec);
                                model.row.row.checkBox4 = true;
                            }
                        }
                        for (var i = 0; i < tmp4.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp4[i + 1].value - tmp4[i].value;
                            rec1.date = tmp4[i].date;
                            model.row.Modbus4.push(rec1);
                            if (rec1.value > recordsMax4) recordsMax4 = rec1.value;
                        }
                        recordsMaxGvs.push(recordsMax4);

                        ////////////  КАНАЛ 5 сбор
                        model.row.Modbus5 = [];
                        var tmp5 = [];
                        var recordsMax5 = 0;
                        var tmpValue = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал5") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp5.push(rec);
                                model.row.row.checkBox5 = true;
                            }
                        }
                        for (var i = 0; i < tmp5.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp5[i + 1].value - tmp5[i].value;
                            rec1.date = tmp5[i].date;
                            model.row.Modbus5.push(rec1);
                            if (rec1.value > recordsMax5) recordsMax5 = rec1.value;
                        }
                        recordsMaxHvs.push(recordsMax5);

                        ////////////  КАНАЛ 6 сбор
                        model.row.Modbus6 = [];
                        var tmp6 = [];
                        var recordsMax6 = 0;
                        var tmpValue = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал6") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp6.push(rec);
                                model.row.row.checkBox6 = true;
                            }
                        }
                        for (var i = 0; i < tmp6.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp6[i + 1].value - tmp6[i].value;
                            rec1.date = tmp6[i].date;
                            model.row.Modbus6.push(rec1);
                            if (rec1.value > recordsMax6) recordsMax6 = rec1.value;
                        }
                        recordsMaxHvs.push(recordsMax6);

                        ////////////  КАНАЛ 11 сбор
                        model.row.Modbus11 = [];
                        var tmp11 = [];
                        var recordsMax11 = 0;
                        var tmpValue = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал11") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp11.push(rec);
                                model.row.row.checkBox11 = true;
                            }
                        }
                        for (var i = 0; i < tmp11.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp11[i + 1].value - tmp11[i].value;
                            rec1.date = tmp11[i].date;
                            model.row.Modbus11.push(rec1);
                            if (rec1.value > recordsMax11) recordsMax11 = rec1.value;
                        }
                        recordsMaxHvs.push(recordsMax11);

                        ////////////  КАНАЛ 12 сбор
                        model.row.Modbus12 = [];
                        var tmp12 = [];
                        var recordsMax12 = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал12") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp12.push(rec);
                                model.row.row.checkBox12 = true;
                            }
                        }
                        for (var i = 0; i < tmp12.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp12[i + 1].value - tmp12[i].value;
                            rec1.date = tmp12[i].date;
                            model.row.Modbus12.push(rec1);
                            if (rec1.value > recordsMax12) recordsMax12 = rec1.value;
                        }
                        recordsMaxGvs.push(recordsMax12);

                        ////////////  КАНАЛ 13 сбор
                        model.row.Modbus13 = [];
                        var tmp13 = [];
                        var recordsMax13 = 0;
                        var tmpValue = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал13") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp13.push(rec);
                                model.row.row.checkBox13 = true;
                            }
                        }
                        for (var i = 0; i < tmp13.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp13[i + 1].value - tmp13[i].value;
                            rec1.date = tmp13[i].date;
                            model.row.Modbus13.push(rec1);
                            if (rec1.value > recordsMax13) recordsMax13 = rec1.value;
                        }
                        recordsMaxHvs.push(recordsMax13);

                        ////////////  КАНАЛ 14 сбор
                        model.row.Modbus14 = [];
                        var tmp14 = [];
                        var recordsMax14 = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал14") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp14.push(rec);
                                model.row.row.checkBox14 = true;
                            }
                        }
                        for (var i = 0; i < tmp14.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp14[i + 1].value - tmp14[i].value;
                            rec1.date = tmp14[i].date;
                            model.row.Modbus14.push(rec1);
                            if (rec1.value > recordsMax4) recordsMax14 = rec1.value;
                        }
                        recordsMaxGvs.push(recordsMax14);

                        ////////////  КАНАЛ 15 сбор
                        model.row.Modbus15 = [];
                        var tmp15 = [];
                        var recordsMax15 = 0;
                        var tmpValue = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал15") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp15.push(rec);
                                model.row.row.checkBox15 = true;
                            }
                        }
                        for (var i = 0; i < tmp15.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp15[i + 1].value - tmp15[i].value;
                            rec1.date = tmp15[i].date;
                            model.row.Modbus15.push(rec1);
                            if (rec1.value > recordsMax15) recordsMax15 = rec1.value;
                        }
                        recordsMaxHvs.push(recordsMax15);

                        ////////////  КАНАЛ 16 сбор
                        model.row.Modbus16 = [];
                        var tmp16 = [];
                        var recordsMax16 = 0;
                        for (var i = 0; i < records.length; i++) {
                            if (records[i].s1 == "Канал16") {
                                var rec = {};
                                rec.date = records[i].date;
                                rec.value = records[i].d1;
                                tmp16.push(rec);
                                model.row.row.checkBox16 = true;
                            }
                        }
                        for (var i = 0; i < tmp16.length - 1; i++) {
                            var rec1 = {};
                            rec1.value = tmp16[i + 1].value - tmp16[i].value;
                            rec1.date = tmp16[i].date;
                            model.row.Modbus16.push(rec1);
                            if (rec1.value > recordsMax16) recordsMax16 = rec1.value;
                        }
                        recordsMaxGvs.push(recordsMax16);

                        for (var i = 0; i < recordsMaxGvs.length; i++){
                            if (recordsAbsMaxGvs < recordsMaxGvs[i]) recordsAbsMaxGvs = recordsMaxGvs[i];
                        }
                        for (var i = 0; i < recordsMaxHvs.length; i++) {
                            if (recordsAbsMaxHvs < recordsMaxHvs[i]) recordsAbsMaxHvs = recordsMaxHvs[i];
                        }

                        ////////////  КАНАЛ 2 расчет
                        var strTmp = "";
                        var koefValue = 260 / recordsAbsMaxGvs;
                        var koefHour = 800 / 72;
                        model.row.tmpsModbus = [];
                        model.row.hours00Modbus = [];
                        for (var i = 0; i < model.row.Modbus2.length; i++) {
                            var x = i * koefHour + 40;
                            var y = 280 - model.row.Modbus2[i].value * koefValue;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus.push(tmp);

                            if (i == model.row.Modbus2.length - 1) {
                                strTmp += x + " " + y;
                            }
                            else {
                                strTmp += x + " " + y + ",";
                            }
                            model.row.ModbusLine = strTmp;
                            var date = new Date(model.row.Modbus2[i].date);
                            if (date.getHours() == 0 || date.getHours() == 24) {
                                model.row.hours00Modbus.push(x);
                            } 
                        }

                        ////////////  КАНАЛ 3 расчет
                        var strTmp3 = "";
                        var koefValue3 = 260 / recordsAbsMaxHvs;
                        var koefHour3 = 800 / 72;
                        model.row.tmpsModbus3 = [];
                        model.row.hours00Modbus3 = [];
                        for (var i = 0; i < model.row.Modbus3.length; i++) {
                            var x = i * koefHour3 + 40;
                            var y = 280 - model.row.Modbus3[i].value * koefValue3;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus3.push(tmp);
                            if (i == model.row.Modbus3.length - 1) {
                                strTmp3 += x + " " + y;
                            }
                            else {
                                strTmp3 += x + " " + y + ",";
                            }
                            model.row.ModbusLine3 = strTmp3;
                        }

                        ////////////  КАНАЛ 4 расчет
                        var strTmp4 = "";
                        var koefValue4 = 260 / recordsAbsMaxGvs;
                        var koefHour4 = 800 / 72;
                        model.row.tmpsModbus4 = [];
                        model.row.hours00Modbus4 = [];
                        for (var i = 0; i < model.row.Modbus4.length; i++) {
                            var x = i * koefHour4 + 40;
                            var y = 280 - model.row.Modbus4[i].value * koefValue4;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus4.push(tmp);
                            if (i == model.row.Modbus4.length - 1) {
                                strTmp4 += x + " " + y;
                            }
                            else {
                                strTmp4 += x + " " + y + ",";
                            }
                            model.row.ModbusLine4 = strTmp4;
                        }

                        ////////////  КАНАЛ 5 расчет
                        var strTmp5 = "";
                        var koefValue5 = 260 / recordsAbsMaxHvs;
                        var koefHour5 = 800 / 72;
                        model.row.tmpsModbus5 = [];
                        model.row.hours00Modbus5 = [];
                        for (var i = 0; i < model.row.Modbus5.length; i++) {
                            var x = i * koefHour5 + 40;
                            var y = 280 - model.row.Modbus5[i].value * koefValue5;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus5.push(tmp);
                            if (i == model.row.Modbus5.length - 1) {
                                strTmp5 += x + " " + y;
                            }
                            else {
                                strTmp5 += x + " " + y + ",";
                            }
                            model.row.ModbusLine5 = strTmp5;
                        }

                        ////////////  КАНАЛ 6 расчет
                        var strTmp6 = "";
                        var koefValue6 = 260 / recordsAbsMaxHvs;
                        var koefHour6 = 800 / 72;
                        model.row.tmpsModbus6 = [];
                        model.row.hours00Modbus6 = [];
                        for (var i = 0; i < model.row.Modbus6.length; i++) {
                            var x = i * koefHour6 + 40;
                            var y = 280 - model.row.Modbus6[i].value * koefValue6;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus6.push(tmp);
                            if (i == model.row.Modbus6.length - 1) {
                                strTmp6 += x + " " + y;
                            }
                            else {
                                strTmp6 += x + " " + y + ",";
                            }
                            model.row.ModbusLine6 = strTmp6;
                        }

                        ////////////  КАНАЛ 11 расчет
                        var strTmp11 = "";
                        var koefValue11 = 260 / recordsAbsMaxHvs;
                        var koefHour11 = 800 / 72;
                        model.row.tmpsModbus11 = [];
                        model.row.hours00Modbus11 = [];
                        for (var i = 0; i < model.row.Modbus11.length; i++) {
                            var x = i * koefHour11 + 40;
                            var y = 280 - model.row.Modbus11[i].value * koefValue11;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus11.push(tmp);
                            if (i == model.row.Modbus11.length - 1) {
                                strTmp11 += x + " " + y;
                            }
                            else {
                                strTmp11 += x + " " + y + ",";
                            }
                            model.row.ModbusLine11 = strTmp11;
                        }

                        ////////////  КАНАЛ 12 расчет
                        var strTmp12 = "";
                        var koefValue12 = 260 / recordsAbsMaxGvs;
                        var koefHour12 = 800 / 72;
                        model.row.tmpsModbus12 = [];
                        model.row.hours00Modbus12 = [];
                        for (var i = 0; i < model.row.Modbus12.length; i++) {
                            var x = i * koefHour12 + 40;
                            var y = 280 - model.row.Modbus12[i].value * koefValue12;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus12.push(tmp);
                            if (i == model.row.Modbus12.length - 1) {
                                strTmp12 += x + " " + y;
                            }
                            else {
                                strTmp12 += x + " " + y + ",";
                            }
                            model.row.ModbusLine12 = strTmp12;
                        }

                        ////////////  КАНАЛ 13 расчет
                        var strTmp13 = "";
                        var koefValue13 = 260 / recordsAbsMaxHvs;
                        var koefHour13 = 800 / 72;
                        model.row.tmpsModbus13 = [];
                        model.row.hours00Modbus13 = [];
                        for (var i = 0; i < model.row.Modbus13.length; i++) {
                            var x = i * koefHour13 + 40;
                            var y = 280 - model.row.Modbus13[i].value * koefValue13;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus13.push(tmp);
                            if (i == model.row.Modbus13.length - 1) {
                                strTmp13 += x + " " + y;
                            }
                            else {
                                strTmp13 += x + " " + y + ",";
                            }
                            model.row.ModbusLine13 = strTmp13;
                        }

                        ////////////  КАНАЛ 14 расчет
                        var strTmp14 = "";
                        var koefValue14 = 260 / recordsAbsMaxGvs;
                        var koefHour14 = 800 / 72;
                        model.row.tmpsModbus14 = [];
                        model.row.hours00Modbus14 = [];
                        for (var i = 0; i < model.row.Modbus14.length; i++) {
                            var x = i * koefHour14 + 40;
                            var y = 280 - model.row.Modbus14[i].value * koefValue14;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus14.push(tmp);
                            if (i == model.row.Modbus14.length - 1) {
                                strTmp14 += x + " " + y;
                            }
                            else {
                                strTmp14 += x + " " + y + ",";
                            }
                            model.row.ModbusLine14 = strTmp14;
                        }

                        ////////////  КАНАЛ 15 расчет
                        var strTmp15 = "";
                        var koefValue15 = 260 / recordsAbsMaxHvs;
                        var koefHour15 = 800 / 72;
                        model.row.tmpsModbus15 = [];
                        model.row.hours00Modbus15 = [];
                        for (var i = 0; i < model.row.Modbus15.length; i++) {
                            var x = i * koefHour15 + 40;
                            var y = 280 - model.row.Modbus15[i].value * koefValue15;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus15.push(tmp);
                            if (i == model.row.Modbus15.length - 1) {
                                strTmp15 += x + " " + y;
                            }
                            else {
                                strTmp15 += x + " " + y + ",";
                            }
                            model.row.ModbusLine15 = strTmp15;
                        }

                        ////////////  КАНАЛ 16 расчет
                        var strTmp16 = "";
                        var koefValue16 = 260 / recordsAbsMaxGvs;
                        var koefHour16 = 800 / 72;
                        model.row.tmpsModbus16 = [];
                        model.row.hours00Modbus16 = [];
                        for (var i = 0; i < model.row.Modbus16.length; i++) {
                            var x = i * koefHour16 + 40;
                            var y = 280 - model.row.Modbus16[i].value * koefValue16;
                            var tmp = {};
                            tmp.x = x;
                            tmp.y = y;
                            model.row.tmpsModbus16.push(tmp);
                            if (i == model.row.Modbus16.length - 1) {
                                strTmp16 += x + " " + y;
                            }
                            else {
                                strTmp16 += x + " " + y + ",";
                            }
                            model.row.ModbusLine16 = strTmp16;
                        }

                        // дополнительные расчеты
                        if (model.row.hours00Modbus[1] == null) model.row.hours00Modbus[1] = model.row.hours00Modbus[0] + 266.6;
                        if (model.row.hours00Modbus[2] == null) model.row.hours00Modbus[2] = model.row.hours00Modbus[1] + 266.6;
                        var tmpi = 0;
                        for (var dtTmp = new Date(dtStart); dtTmp.getTime() <= dtEnd.getTime(); dtTmp.setDate(dtTmp.getDate() + 1)) {
                            var tmp1 = {};
                            if (model.row.hours00Modbus[0] == null) break;
                            if (tmpi == 0) {
                                tmpi++;
                                continue;
                            }
                            tmp1.x = model.row.hours00Modbus[tmpi - 1] - 30;
                            tmp1.x2 = model.row.hours00Modbus[tmpi - 1];
                            tmp1.date = new Date(dtTmp);
                            tmpi++;
                            arrDt.push(tmp1);
                        }
                        model.row.arrDtModbus = arrDt;

                        model.row.row.upGvs = [];
                        var koefValue = 260 / recordsAbsMaxGvs;
                        for (var u = 0; u < 4; u++) {
                            var upY = {};
                            upY.y = 20 + u * 65;
                            upY.value = recordsAbsMaxGvs * (4 - u) / 4;
                            model.row.row.upGvs.push(upY);
                        }

                        model.row.row.upHvs = [];
                        var koefValue = 260 / recordsAbsMaxHvs;
                        for (var u = 0; u < 4; u++) {
                            var upY = {};
                            upY.y = 20 + u * 65;
                            upY.value = recordsAbsMaxHvs * (4 - u) / 4;
                            model.row.row.upHvs.push(upY);
                        }

                    })
                }

                //Параметры - Связь
                {
                    var c = body.days;
                    for (var i = 0; i < c.length; i++) {
                        var par = c[i];
                        if (par) {
                            switch (par.name) {
                                case "__Gsm":
                                    var sig = { date: par.date, level: par.value };
                                    if (!model.row.signal) model.row.signal = [];
                                    mode.rowl.signal.push(sig);
                                    break;
                            }
                        }
                    }
                }
                if (model.row.signal && (model.row.signal.length > 0) && (model.row.signal[0].level)) {
                    model.row.signal[0].img = $helper.getSignalImg(model.row.signal[0].level);
                } 
               
            })
        });
    }



    function isSelectedAny() {
        return arrayOfSels().length > 0;
    }
    function isEnabledDelete() {
        if (selectedDel == null || selectedDel.length == 0) {
            return false;
        } else {
            if (selectedNotDel == null || selectedNotDel.length == 0) {
                return true;
            } else {
                return false;
            }
        }
    }
    function noWrap(data) {
        return data;
    }

    // События грида

    function toggleToolPanel(newstate) {
        var api = getApi();//$scope.opt.api;
        api.showToolPanel(newstate);
    }

    ////
    model.closeRowProperties = function(){
        model.isRowProperties = false;
        model.rowPropertiesHeightMultip = 0;
        model.rowPropertiesHeightDivis = 1;
        if (model.isMap) {
            model.mapHeightMultip = 3;
            model.mapHeightDivis = 4;
            model.listHeightDivis = 4;
        }
        else {
            model.mapHeightMultip = 0;
            model.mapHeightDivis = 1;
            model.listHeightDivis = 1;
        }
        model.listHeightMultip = 1;
        var api = model.grid2.api;
        api.refreshView();
    }
    model.closeMaps = function () {
        model.isMap = false;
        model.mapHeightMultip = 0;
        model.mapHeightDivis = 1;
        if (model.isRowProperties) {
            model.rowPropertiesHeightMultip = 3;
            model.rowPropertiesHeightDivis = 4;
            model.listHeightDivis = 4;
        }
        else {
            model.rowPropertiesHeightMultip = 0;
            model.rowPropertiesHeightDivis = 1;
            model.listHeightDivis = 1;
        }
        model.listHeightMultip = 1;
        var api = model.grid2.api;
        api.refreshView();
    }
    model.options = (function () {

        var options = [];

        for (var o = 0; o < options.length; o++) {
            options[o].index = o;
        }

        for (var i = 0; i < menuActions.length; i++) {
            var menuAction = menuActions[i];
            if (menuAction === null) {
                options.push({ index: o + i, type: "divider" });
                continue;
            }

            (function (i) {
                var ma = menuActions[i];
                $actions.get(ma.name).then(function (action) {
                    if (action == null) return;
                    var isVisible = ma.isVisible;
                    var getParam = ma.getParam;
                    var getArg = ma.getArg;
                    var isEnabled = ma.isEnabled;
                    var success = ma.success;
                    var error = ma.error;



                    var actGetParamWArg = function (a, gp, ga, en, vi, sc, er) {
                        return function ($item) {
                            if (!sc) sc = function () { };
                            if (!er) er = function () { };
                            a.act(gp == undefined ? gp : gp(), ga == undefined ? ga : ga()).then(sc, er);
                        }
                    }

                    var title = ma.title || action.header;

                    options.push({
                        index: o + i,
                        icon: action.icon,
                        title: title,
                        type: 'html',
                        action: actGetParamWArg(action, getParam, getArg, isEnabled, isVisible, success, error),
                        enabled: isEnabled,
                        visibled: isVisible
                    });
                })
            })(i);
        }

        return options;

    })();

    //PANEL ACTIONS
    {
        for (var i = 0; i < panelActions.length; i++) {
            var panelAction = panelActions[i];
            if (panelAction === null) {
                continue;
            }

            (function (pa) {
                $actions.get(pa.name).then(function (a) {
                    if (a != null) {
                        var action = {};
                        action.popover = pa.popover;
                        action.title = pa.title || action.header;
                        action.icon = function (data) {
                            if (pa.getIcon) {
                                return pa.getIcon(data)
                            }
                            return a.icon;
                        }
                        action.visible = pa.isVisible || function () { return true; };
                        action.act = function (data) {
                            a.act(pa.wrap ? pa.wrap(data) : data);
                        }
                        model.panelActionsView.push(action);
                    }
                });
            })(panelAction);

        }
    }

    /////

    $window.listModel = model;
    $scope.model = model;
    $scope.connection = connection;


    ///------new grid options test------

    var columns2 = [];

    columns2.push({
        headerName: "Выбор",
        field: "select",
        width: 40,
        suppressSizeToFit: true,
        suppressSorting: true,
        suppressMenu: true,
        volatile: true,
        checkboxSelection: true,
        hide: false
    });

    columns2.push({
        headerName: "НС",
        field: "abnormals",
        cellRenderer: abnormalsTmpl,
        suppressSizeToFit: true,
        width: 27
    });

    columns2.push({
        headerName: (metaSvc.config === "orenburg")? "Номер площадки" : "Номер договора",
        field: "number",
        cellRenderer: numberTmpl,
        suppressSizeToFit: false,
        width: 50,
        hide: false
    });

    columns2.push({
        headerName: (metaSvc.config === "teplocom")? "Абонент" : "Название объекта учёта",
        field: "name",
        suppressSizeToFit: false,
        cellRenderer: cellNameTmpl,
        width: 200
    });

    columns2.push({
        headerName: "Название точки учёта",
        field: "pname",
        suppressSizeToFit: false,
        cellRenderer: cellPNameTmpl,
        hide: (metaSvc.config === "orenburg")
    });

    columns2.push({
        headerName: "Действия",
        field: "actions",
        cellRenderer: cellActionsTmpl,
        suppressSizeToFit: true,
        suppressSorting: true,
        suppressMenu: true,
        volatile: true,
        width: 98
    });
    columns2.push({
        headerName: "Показания",
        field: "indication",
        suppressSizeToFit: false,
        cellRenderer: cellIndicationTmpl,
        width: 110
    });
    if (metaSvc.config == 'matrix') {
        columns2.push({
            headerName: "Биллинг",
            field: "billing",
            suppressSizeToFit: false,
            cellRenderer: cellBillingTmpl,
            width: 85,
            hide: true
        });
        columns2.push({
            headerName: "Отоп.под",
            field: "heatingSupply",
            suppressSizeToFit: false,
            cellRenderer: cellHeatingSupplyTmpl,
            width: 90
        });
        columns2.push({
            headerName: "Отоп.обр",
            field: "heatingReturn",
            suppressSizeToFit: false,
            cellRenderer: cellHeatingReturnTmpl,
            width: 90
        });
        columns2.push({
            headerName: "ГВС под.",
            field: "hwsSupply",
            suppressSizeToFit: false,
            cellRenderer: cellHWSSupplyTmpl,
            width: 85
        });
        columns2.push({
            headerName: "ГВС обр.",
            field: "hwsReturn",
            suppressSizeToFit: false,
            cellRenderer: cellHWSReturnTmpl,
            width: 85
        });
        columns2.push({
            headerName: "ХВС",
            field: "cws",
            suppressSizeToFit: false,
            cellRenderer: cellCWSTmpl,
            width: 70
        });
    }
    columns2.push({
        headerName: "Статус",
        field: "state",
        suppressSizeToFit: false,
        cellRenderer: cellStatusTmpl,
        width: 105
    });
    columns2.push({
        headerName: "Полнота суточных данных",
        field: "fulness",
        suppressSizeToFit: false,
        cellRenderer: cellFulnessTmpl,
        width: 55
    });
    columns2.push({
        headerName: "Полнота часовых данных",
        field: "fulnessHour",
        suppressSizeToFit: false,
        cellRenderer: cellFulnessHourTmp,
        width: 55
    });
    columns2.push({
        headerName: "Тип прибора",
        field: "device",
        width: 100
    });

    columns2.push({
        headerName: "IMEI",
        field: "imei",
        cellRenderer: cellImeiTmpl
    });

    columns2.push({
        headerName: "Телефон",
        field: "phone",
        suppressSizeToFit: false,
        cellRenderer: cellPhoneTmpl
    });
    columns2.push({
        headerName: "Примечание",
        field: "comment",
        suppressSizeToFit: true,
        suppressSorting: true,
        cellRenderer: cellCommentTmpl,
        width: 200,
        hide: true
    });

    var getColumnsStateDefault = function (columns) {
        if (columns && columns.length) {
            var columnsState = [];
            for (var i = 0; i < columns.length; i++) {
                var column = columns[i];
                columnsState.push({
                    colId: column.field,
                    hide: column.hide || false,
                    width: column.width || 150,
                    aggFunc: null,
                    pivotIndex: null
                });
            }
            return columnsState;
        }
    }

    var ready2 = function () {
        loadColumnState();
        upd($listFilter.getFilter());
    };
    
    var visibleRows = [];

    /**
     * обновление источника данных для грида
     * происходит при изменении фильтра
     */
    var upd = function (filter) {
        //$scope.model.grid2.api.showLoading(true);
        if ($scope.model.grid2.api) {
            $scope.model.grid2.api.showLoadingOverlay();
        }

        selectedIds = {};

        var datasource = {
            pageSize: 100,
            overflowSize: 100,
            maxConcurrentRequests: 1,
            maxPagesInCache: 2,
            getRows: function (params) {
                var current = params.startRow;
                var take = params.endRow - params.startRow;

                filter.page = {
                    offset: current,
                    count: take
                };

                filter.order = [];
                for (var i = 0; i < params.sortModel.length; i++) {
                    var sm = params.sortModel[i];
                    filter.order.push({
                        column: sm.colId,
                        dir: sm.sort
                    });
                }

                selectedIds = {};
                $list.getRowsCacheFiltered(filter).then(function (message) {

                    var rows = message.rows;
                    model.count = message.count;
                    params.successCallback(rows, model.count);
                    datasource.rowCount = model.count;
                    $scope.model.grid2.api.hideOverlay();
                    visibleRows.length = 0;
                    var isValveControl = false;
                    var isAnotherResource = false;
                    var isMapsLocal = false;
                    for (var i = 0; i < rows.length; i++) {

                        model.countOpenCounters = 0;
                        if ((parseInt(rows[i].event, 10) & 0x100) > 0 && (parseInt(rows[i].event, 10) & 0x10000) == 0) {
                            model.countOpenCounters++;
                        }
                        if (model.countOpenCounters > 0) {
                            $list.audio("play");
                        } else {
                            $list.audio("pause");
                        }
                        var resource = rows[i].resource;
                        if (resource == "valveControl") {
                            isValveControl = true;
                        }
                        else {
                            isAnotherResource = true;
                        }
                        if (resource == "light") {
                            isMapsLocal = true;
                        }
                        var id = rows[i].id;
                        visibleRows.push(rows[i]);
                    }
                    $list.mapsShow(isMapsLocal);
                    if (isValveControl && !isAnotherResource) {
                        for (var j = 0; j < $scope.model.grid2.columnDefs.length; j++) {
                            var colDef = $scope.model.grid2.columnDefs[j];
                            if (colDef.field == "fulness" || colDef.field == "phone" || colDef.field == "imei" || colDef.field == "indication" || colDef.field == "device" || colDef.field =="number") {
                                colDef.hide = true;
                            }
                            else if (colDef.field == "heatingSupply" || colDef.field == "heatingReturn" || colDef.field == "hwsSupply" || colDef.field == "hwsReturn" || colDef.field == "cws") {
                                colDef.hide = false;
                            }
                        }
                    } else if (isValveControl && isAnotherResource) {
                        for (var j = 0; j < $scope.model.grid2.columnDefs.length; j++) {
                            var colDef = $scope.model.grid2.columnDefs[j];
                            if (colDef.field == "fulness" || colDef.field == "phone" || colDef.field == "imei" || colDef.field == "indication" || colDef.field == "device" || colDef.field == "number") {
                                colDef.hide = false;
                            }
                            else if (colDef.field == "heatingSupply" || colDef.field == "heatingReturn" || colDef.field == "hwsSupply" || colDef.field == "hwsReturn" || colDef.field == "cws") {
                                colDef.hide = true;
                            }
                        }
                    } else if (!isValveControl && isAnotherResource) {
                        for (var j = 0; j < $scope.model.grid2.columnDefs.length; j++) {
                            var colDef = $scope.model.grid2.columnDefs[j];
                            if (colDef.field == "fulness" || colDef.field == "phone" || colDef.field == "imei" || colDef.field == "indication" || colDef.field == "device" || colDef.field == "number") {
                                colDef.hide = false;
                            }
                            else if (colDef.field == "heatingSupply" || colDef.field == "heatingReturn" || colDef.field == "hwsSupply" || colDef.field == "hwsReturn" || colDef.field == "cws") {
                                colDef.hide = true;
                            }
                        }
                    }
                    restoreColumnState();
                    //update selection
                    updateSelection();
                });
            }
        };
        $scope.model.grid2.api.setDatasource(datasource);
    }
    ////

    function loadColumnState() {
        var state = $settings.getListColumnState();
        if (state) {
            var api = getApi();
            api.setColumnState(state);
        }
    }

    function saveColumnState() {
        var api = getApi();
        var state = api.getColumnState();
        $settings.setListColumnState(state);
    }

    function restoreColumnState() {
        var state = getColumnsStateDefault(columns2);
        if (state) {
            var api = getApi();
            api.setColumnState(state);
        }
    }

    ////

    var selectedIds = {};
    var selectedDel = [];
    var selectedNotDel = [];
    function getSelectedIds() {
        var ids = [];
        for (var id in selectedIds) {
            ids.push(id);
        }
        return ids;
    }
   
    model.selectAll2 = function () {
        if (getSelectedIds().length > 0) {
            selectedIds = {};
            updateSelection();
        } else {
            //load all ids by filter
            var filter = $listFilter.getFilter();
            $list.getRowsCacheIdsFiltered(filter).then(function (ids) {
                for (var i = 0; i < ids.length; i++) {
                    var id = ids[i];
                    if (!selectedIds[id]) selectedIds[id] = true;
                }
                updateSelection();
            });
        }
    };

    function onCellClicked(params) {
        var self = this;

        // We have to wait otherwise it overrides our selection
        setTimeout(function waitForAngularGridToFinish() {
            // Select multiple rows when the shift key was pressed
            if (params.event.shiftKey && self.previousSelectedRowIndex !== undefined) {
                var smallerNumber = params.rowIndex < self.previousSelectedRowIndex ? params.rowIndex : self.previousSelectedRowIndex;
                var biggerNumber = params.rowIndex > self.previousSelectedRowIndex ? params.rowIndex : self.previousSelectedRowIndex;

                for (var rowIndexToSelect = smallerNumber; rowIndexToSelect <= biggerNumber; rowIndexToSelect++) {
                    if ((params.colDef.field !== "checkbox") && (params.colDef.field !== "actions")) {
                        self.api.selectIndex(rowIndexToSelect, true, false);//rowIndexToSelect !== biggerNumber
                    }
                }
            }

            self.previousSelectedRowIndex = params.rowIndex;
        }, 0);

    }
    var selectRow = "";
    function onSelectionChanged(event) {
        //selectedIds[event.node.data.id] = true;
        selectedIds = {};
        selectedNotDel = [];
        selectedDel = [];
        var selectedRows = event.selectedRows;
        for (var i = 0; i < selectedRows.length; i++) {
            var id = selectedRows[i].id;
            var isDeleted = selectedRows[i].isDeleted;
            if (isDeleted == null || isDeleted == false || isDeleted == "False" || isDeleted == "false") {
                selectedNotDel.push(true);
            } else {
                selectedDel.push(true);
            }
            selectedIds[id] = true;
        }
        $list.setSelected(getSelectedIds(), selectedRows);
        if (selectedRows.length > 0) {
            if (selectRow != selectedRows[0].id) {
                model.rowProperties();
                selectRow = selectedRows[0].id;
            }
        }
    };

    var updateSelection = function () {

        var rows = [];

        getApi().deselectAll();
        getApi().forEachNode(function (node) {
            if (selectedIds[node.data.id]) {
                getApi().selectNode(node, true, true);
                rows.push(node.data);
            }

        });

        $list.setSelected(getSelectedIds(), rows);
    };

    model.getSelectedIds = getSelectedIds;

    function getApi() {
        return $scope.model.grid2.api;
    }

    //====рендеры ячеек====

    function numberTmpl(params) {
        return params.data.number;
    }

    function cellNameTmpl(params) {
        var name = "" + (params.data.name || "");
        var recordData = [];
        var img = '<img src="/img/house.png" height="20" />';
        var lightImg = '';
        if (params.data.resource != null && params.data.resource.includes("light")) {
            var textLightReal = "Real";
            var textLightMk = "lightMK";
            var arrLightMk;
            var arrLightReal;
            if (params.data.controllerData != null) {
                var arrControllerData = params.data.controllerData.split(';');
                for (var i = 0; i < arrControllerData.length; i++) {
                    if (arrControllerData[i].includes(textLightMk)) {
                        arrLightMk = arrControllerData[i].split(':');
                    }
                    if (arrControllerData[i].includes(textLightReal)) {
                        arrLightReal = arrControllerData[i].split(':');
                    }   
                }
                if (arrLightMk != null && arrLightReal != null) {
                    for (var i = 1; i < arrLightMk.length; i++) {
                        if (arrLightReal[i] == 1 && arrLightMk[i] == 1) {
                            lightImg += '<img src="/img/enabled.png" height="15" />';
                        } else if (arrLightReal[i] == 0 && arrLightMk[i] == 0) {
                            lightImg += '<img src="/img/disabled.png" height="15" />';
                        } else if (arrLightReal[i] == 1 && arrLightMk[i] == 0) {
                            lightImg += '<img src="/img/enabledError.png" height="15" />';
                        } else if (arrLightReal[i] == 0 && arrLightMk[i] == 1) {
                            lightImg += '<img src="/img/disabledError.png" height="15" />';
                        }
                    }
                    if (lightImg != '' && params.data.resource == "lightV2") {
                        img = '<img src="/img/lighting_poles48.png" height="25" />';
                    }
                    if (lightImg != '' && params.data.resource == "light") {
                        if ($window.listModel.isMap) {
                            if (params.data.coordinates != null) {
                                readyMaps(params.data);
                            }
                        } else {
                            myMap = null;
                        }
                        img = lightImg;
                        lightImg = '';
                    }
                }
            }
        }
        return img + ((name && name !== "undefined") ? name : "") + lightImg;
    }

    function cellCommentTmpl(params) {
        return (params.data.comment || "");
    }

    function cellPNameTmpl(params) {
        var img =  '';
        if ((parseInt(params.data.event, 10) & 0x100) > 0) {
            img = '<img src="/img/door_open.png" height="20" />';
            if ((parseInt(params.data.event, 10) & 0x10000) > 0) {
                img += '<img src="/img/sound_mute.png" height="20" />'
            }
        }
        return img  + (params.data.pname || '');
    }
    
    function abnormalsTmpl(params) {
        var img = ((params.data.isDisabled && params.data.isDisabled == "True") ?
            'application_control_bar' :
            (params.data.abnormals ? (parseInt(params.data.abnormals, 10) == 0 ? 'tick_octagon' :  'stop') : ''));
        return (img ? '<img src="/img/' + img + '.png" height="20" /> ' + (params.data.abnormals || '') : '');
        //return '<img src="/img/bullet_' + (params.data.abnormals ? (parseInt(params.data.abnormals, 10) == 0 ? 'green' : 'red') : 'white') + '.png" height="20" /> '

    }

    function cellPhoneTmpl(params) {
        if (!params.data.phone) return "<i><нет></i>";
        return '<img src="/img/phone.png" height="20" /> '
            + params.data.phone;
    }

    function cellImeiTmpl(params) {
        if (!params.data.imei) return "<i><нет></i>";
        return '<img src="/img/fastrack.png" height="20" /> '
            + params.data.imei;
    }
    function cellActionsTmpl(params) {
        var ret = '';
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.visible(params.data) && action.popover != "Карточка управления задвижками" && action.popover != "Карточка биллинга") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" type="button" class="btn btn-xs btn-default">'
                        + '<img src="' + action.icon(params.data) + '" width="16" />'
                        + '</a> ';
                    //ng-click="' + action.act(params.data) + '"
                }
            }
        }
        return ret;
    }
    function pngValveControl(str) {
        switch (str) {
            case "1":
            case 'Открыто':
            case 'Открытa':
                return 'bullet_green';
            case "0":
            case 'Закрыто':
            case 'Закрытa':
                return 'bullet_red';
            default:
                return 'bullet_yellow';
        }
    }
    function nameValveControl(str) {
        switch (str) {
            case "1":
            case 'Открыто':
            case 'Открытa':
                return 'Открыто';
            case "0":
            case 'Закрыто':
            case 'Закрытa':
                return 'Закрыто';
            default:
                return 'Неизвестно';
        }
    }
    
    function cellBillingTmpl(params) {
        var ret = '';
        var data = params.data.heatingSupply;
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.popover == "Карточка биллинга") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\', headerName: \'' + params.colDef.headerName + '\', field: \'' + params.colDef.field + '\', valveControlValue: \'' + nameValveControl(data) + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter">'
                        + '<img src="/img/counter.png" width="16" />'
                        + 'Счет'
                        + '</a> ';
                }
            }
        }
        return ret;
    }
    function cellHeatingSupplyTmpl(params) {
        var ret = ''; 
        var data = params.data.heatingSupply;
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.popover == "Карточка управления задвижками") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\', headerName: \'' + params.colDef.headerName + '\', field: \'' + params.colDef.field + '\', valveControlValue: \'' + nameValveControl(data) + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" class="for-a">'
                        + '<img src="/img/' + pngValveControl(data) + '.png" width="16" />'
                        + nameValveControl(data)
                        + '</a> ';
                }
            }
        }
        return ret;
    }
    function cellHeatingReturnTmpl(params) {
        var ret = '';
        var data = params.data.heatingReturn;
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.popover == "Карточка управления задвижками") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\', headerName: \'' + params.colDef.headerName + '\', field: \'' + params.colDef.field + '\', valveControlValue: \'' + nameValveControl(data) + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" class="for-a">'
                        + '<img src="/img/' + pngValveControl(data) + '.png" width="16" />'
                        + nameValveControl(data)
                        + '</a> ';
                }
            }
        }
        return ret;
    } 
    function cellHWSSupplyTmpl(params) {
        var ret = '';
        var data = params.data.hwsSupply;
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.popover == "Карточка управления задвижками") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\', headerName: \'' + params.colDef.headerName + '\', field: \'' + params.colDef.field + '\', valveControlValue: \'' + nameValveControl(data) + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" class="for-a">'
                        + '<img src="/img/' + pngValveControl(data) + '.png" width="16" />'
                        + nameValveControl(data)
                        + '</a> ';
                }
            }
        }
        return ret;
    }
    function cellHWSReturnTmpl(params) {
        var ret = '';
        var data = params.data.hwsReturn;
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.popover == "Карточка управления задвижками") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\', headerName: \'' + params.colDef.headerName + '\', field: \'' + params.colDef.field + '\', valveControlValue: \'' + nameValveControl(data) + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" class="for-a">'
                        + '<img src="/img/' + pngValveControl(data) + '.png" width="16" />'
                        + nameValveControl(data)
                        + '</a> ';
                }
            }
        }
        return ret;
    }
    function cellCWSTmpl(params) {
        var ret = '';
        var data = params.data.cws;
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.popover == "Карточка управления задвижками") {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\', headerName: \'' + params.colDef.headerName + '\', field: \'' + params.colDef.field + '\', valveControlValue: \'' + nameValveControl(data) + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" class="for-a">'
                        + '<img src="/img/' + pngValveControl(data) + '.png" width="16" />'
                        + nameValveControl(data)
                        + '</a> ';
                }
            }
        }
        return ret;
    }
    function selectCellTmpl(params) {
        return '<div style="align-content: center; text-align: center">\
                <img src="/img/' + (params.api.isNodeSelected(params.node) ? 'check_box.png' : 'check_box_uncheck.png') + '" width="20" />\
            </div>';
    };
    function cellIndicationTmpl(params) {
        
        var value = parseFloat(params.data.value);
        var valueUnitMeasurement = params.data.valueUnitMeasurement != null ? params.data.valueUnitMeasurement: "";
        if (isNaN(value)) {
            value = "<span class='grey'>нет информации</span>";
        } else {
            if (valueUnitMeasurement == "motor") {
                if (value == 0) {
                    value = '<div style="align-content: center; text-align: right"> STOP </div>';
                }
                else {
                    value = '<div style="align-content: center; text-align: right"> START </div>';
                }
            } else {

                value = '<div style="align-content: center; text-align: right">' + (value.toFixed(2)) + (valueUnitMeasurement) + '</div>';
            }
        }
        return value;
    }
    function cellStatusTmpl(params) {

        var img;
        var title;

        var state = parseInt(params.data.state);
        var colorState = 0; // 0 - def, 1 - зел,
        if (isNaN(state)) {
            img = "application_control_bar.png";
            title = "<span class='grey'>нет информации</span>";
        } else if (state === 0) {
            var dt = params.data.date;
            var date = dt ? $filter("date")(dt, "HH:mm:ss") : "";
            var dateNow = new Date();
            img = "tick.png";
            if (date != "") {
                var dtTmp = new Date(dt);
                colorState = 1;
                if (dtTmp.getDate() != dateNow.getDate()) {
                    date = $filter("date")(dt, "dd/MM/yy HH:mm:ss ");
                    colorState = 0;
                }
                title = date;
            } else {
                title = "Опрос успешно завершен";
            }
        } else if (state > 0 && state < 100) {
            switch (state) {
                case 10:
                    img = "time.png";
                    title = "Ожидание ";
                    break;
                case 20:
                    img = "loader.gif";
                    title = "Идет опрос ";
                    break;
            }
        } else if (state == 666) {
            img = "application_control_bar.png";
            title = (params.data.description) ? params.data.description : metaSvc.getReasonByCode(state);
        } else {
            img = "error.png";
            var reason = metaSvc.getReasonByCode(state);
            title = "№ " + (state || "?") + " " + (reason || "неизвестная ошибка");
        }

        var res = '<img src="/img/' + img + '" width="20"' + '" title="' + title + '"><span style=" margin-left:3px; ' + (colorState == 1 ? ' color: darkgreen;">' : '">') + title + '</span>';
        return res;
    }
    function getRows() {

    }

    function cellFulnessTmpl(params) {

        var fulness = (params.data && params.data.fulness) || "";

        var fulnessParameter = fulness.split(';');

        var res = "";
        if (fulnessParameter.length > 5) {
            var count = parseInt(fulnessParameter[0]);
            var total = parseInt(fulnessParameter[1]);
            var daysInPeriod = parseInt(fulnessParameter[2]);
            var days = {};
            var arrayDaysNA = [];
            var stringDays = fulnessParameter[3].split(',');
            for (var i = 0; i < stringDays.length; i++) {
                days[parseInt(stringDays[i])] = true;
            }
            for (var i = 1; i <= total; i++) {
                if (!days[i]) {
                    arrayDaysNA.push(i);
                }
            }
            var periodMonth = parseInt(fulnessParameter[4]);
            var periodYear = parseInt(fulnessParameter[5]);

            // сборка показометра
            var widthMultiplier = 1;
            var viewArray = [];

            //1-24/31; 000111111..111---..-
            for (var i = 1; i <= daysInPeriod; i++) {
                if ((i == total) || (i == daysInPeriod) || ((i < total) && (days[i] != days[i + 1]))) {
                    viewArray.push({ width: widthMultiplier, type: (i <= total) ? (!!days[i]) : undefined });
                    widthMultiplier = 1;
                } else {
                    widthMultiplier++;
                }
            }

            res = '<table style="width: 100%"><tr><td style="color: ' + (arrayDaysNA.length == 0 ? 'darkgreen' : 'darkred') + '" colspan="' + daysInPeriod + '"><small>' + count + ' / ' + total + '</small></tr><tr style="height: 2px">';

            for (var i = 0; i < viewArray.length; i++) {
                res += '<td style="width: ' + (3.2 * viewArray[i].width) + '%' + (viewArray[i].type === undefined ? '' : ('; background-color: ' + (viewArray[i].type ? 'green' : '#ffcccb'))) + '"></td>';
            }
            res += "</tr></table>";
        }
        return res;
    }

    function cellFulnessHourTmp(params) {

        var fulness = (params.data && params.data.fulnessHour) || "";

        var fulnessParameter = fulness.split(';');

        var res = "";
        if (fulnessParameter.length > 5) {
            var count = parseInt(fulnessParameter[0]);
            var total = parseInt(fulnessParameter[1]);
            var hoursInPeriod = parseInt(fulnessParameter[2]);
            var hours = {};
            var arrayDaysNA = [];
            var stringDays = fulnessParameter[3].split(',');
            for (var i = 0; i < stringDays.length; i++) {
                hours[parseInt(stringDays[i])] = true;
            }
            for (var i = 1; i <= total; i++) {
                if (!hours[i]) {
                    arrayDaysNA.push(i);
                }
            }

            // сборка показометра
            var widthMultiplier = 1;
            var viewArray = [];

            //1-24/31; 000111111..111---..-
            for (var i = 1; i <= hoursInPeriod; i++) {
                if ((i == total) || (i == hoursInPeriod) || ((i < total) && (hours[i] != hours[i + 1]))) {
                    viewArray.push({ width: widthMultiplier, type: (i <= total) ? (!!hours[i]) : undefined });
                    widthMultiplier = 1;
                } else {
                    widthMultiplier++;
                }
            }

            res = '<table style="width: 100%"><tr><td style="color: ' + (arrayDaysNA.length == 0 ? 'darkgreen' : 'darkred') + '" colspan="' + hoursInPeriod + '"><small>' + count + ' / ' + total + '</small></tr><tr style="height: 2px">';

            for (var i = 0; i < viewArray.length; i++) {
                res += '<td style="width: ' + (3.2 * viewArray[i].width) + '%' + (viewArray[i].type === undefined ? '' : ('; background-color: ' + (viewArray[i].type ? 'green' : '#ffcccb'))) + '"></td>';
            }
            res += "</tr></table>";
        }
        return res;
    }

    model.grid2 = {
        angularCompileRows: false,
        toolPanelSuppressPivot: true,
        virtualPaging: true,
        rowSelection: "multiple",
        enableServerSideSorting: true,
        enableServerSideFilter: true,
        enableColResize: true,
        columnDefs: columns2,
        onReady: ready2,
        onCellClicked: onCellClicked,
        onSelectionChanged: onSelectionChanged,
        //onRowSelected: onRowSelected,
        //suppressRowClickSelection: true,
        floatingTopRowData: [],
        myMap: null,
        audio: model.audio,
        getRowClass: function (row) {
            var classes = [];
            if (row.data.isDisabled && row.data.isDisabled == "True") {
                classes.push('cell-disabled');
            }
            if (row.data.selected) {
                classes.push('cell-selected');
            }
            return classes;
        },
        headerCellRenderer: function (params) {

            if (params.colDef.field == 'select') {
                return '<div style="align-content: center; text-align: center;font-size:10px;" class="btn btn-xs btn-default" onclick="window.listModel.selectAll2()">Все</div>';
            }

            return params.colDef.headerName;
        }
    };
    //Подписки на события rootScope
    var listeners = [];

    //Push-сообщение от сервера
    listeners.push($rootScope.$on("transport:message-received", function (e, message) {

        var ids = [];

        if (message.head.what == "ListUpdate") {
            ids = message.body.ids;
        } else if (message.head.what == "edit") {

        } else if (message.head.what == "changes") {
            var rules = message.body.rules;
            for (var i = 0; i < rules.length; i++) {
                var rule = rules[i];
                if (rule.target === "node" && rule.content.type === "Tube") {
                    ids.push(rule.content.id);
                }
            }
        } 
        if (ids && ids.length) {
            $list.getRowsCache(ids).then(function (rows) {
                getApi().rowRenderer.rowModel.forEachInMemory(function (node) {
                    for (var i = 0; i < rows.length; i++) {
                        model.countOpenCounters = 0;
                        if ((parseInt(rows[i].event, 10) & 0x100) > 0 && (parseInt(rows[i].event, 10) & 0x10000) == 0) {
                            model.countOpenCounters++;
                        }
                        if (model.countOpenCounters > 0) {
                            $list.audio("play");
                        } else {
                            $list.audio("pause");
                        }
                        var updatedRow = rows[i];
                        if (updatedRow.id === node.data.id) {
                            node.data = updatedRow;
                            return;
                        }
                    }
                });
                getApi().refreshView();
            });
        }
    }));

    //Изменение фильтра списка объектов
    listeners.push($rootScope.$on("listFilter:changed", function () {
        upd($listFilter.getFilter());
    }));

    ////

    //listeners.push($rootScope.$on("list:filter-changed", function (e, message) {
    //    onFilterChange();
    //}));

    listeners.push($rootScope.$on("list:toggle-toolpanel", function (e, message) {
        if (message) {
            toggleToolPanel(message.newstate);
        } else {
            var api = getApi();//$scope.opt.api;
            toggleToolPanel(!api.isToolPanelShowing());
        }
    }));

    listeners.push($rootScope.$on("list:update-done", function (e, message) {
        var api = getApi();
        api.refreshView();
    }));

    listeners.push($rootScope.$on("list:save-columns-state", function (e, message) {
        saveColumnState();
    }));

    listeners.push($rootScope.$on("list:restore-columns-state", function (e, message) {
        restoreColumnState();
    }));


    $scope.$on('$destroy', function () {
        $log.debug("уничтожается таблица с объектами");
        for (var i = 0; i < listeners.length; i++) {
            var listener = listeners[i];
            listener();
        }
    });

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////yandex-maps//////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    var myMap = null;
    function readyMaps(data) {
        var coordinatesTmp = data.coordinates.split("|");
        var coordinates = new Array();
        for (var i = 0; i < coordinatesTmp.length; i++) {
            var arrTmp = coordinatesTmp[i].split(";");
            coordinates[i] = new Array();
            coordinates[i] = coordinatesTmp[i].split(";");
        }
      
        ymaps.ready(init(data, coordinates));

    }
    
    function init(data, coordinates) {
        if (data.resource == "light") { //Агидель
            var textLightReal = "Real";
            var textLightMk = "lightMK";
            var textControlMetod = "CMetod";
            var strControlMetod = "Метод управления: ";
            var textdtContollers = "dt";
            var textdtContollersFull = "dtContollers";
            var dtContollers = "Данные за: ";
            if (data.controllerData != null) {
                var arrLightMk, arrLightReal, arrControlMetod, arrDtContollers;
                var arrControllerData = data.controllerData.split(';');
                for (var i = 0; i < arrControllerData.length; i++) {
                    if (arrControllerData[i].includes(textLightMk)) {
                        arrLightMk = arrControllerData[i].split(':');
                    }
                    if (arrControllerData[i].includes(textLightReal)) {
                        arrLightReal = arrControllerData[i].split(':');
                    }
                    if (arrControllerData[i].includes(textControlMetod) || arrControllerData[i].includes("ControlMetod")) {
                        arrControlMetod = arrControllerData[i].split(':');
                    }
                }
                var eIndexdtContollers = (data.controllerData.includes("T|")) ? data.controllerData.indexOf("T|") : data.controllerData.length;
                var sIndexdtContollers = data.controllerData.indexOf(textdtContollers);
                var indexdtContollers = sIndexdtContollers + ((data.controllerData.includes(textdtContollersFull)) ? textdtContollersFull.length: textdtContollers.length) + 1;
                dtContollers += data.controllerData.substr(indexdtContollers, eIndexdtContollers - indexdtContollers);
               
                if (arrControlMetod[1] == "0") {
                    strControlMetod += "По расписанию";
                } else if (arrControlMetod[1] == "1") {
                    strControlMetod += "По фотодатчику";
                } else if (arrControlMetod[1] == "2") {
                    strControlMetod += "Ручное управление";
                } else if (arrControlMetod[1] == "3") {
                    strControlMetod += "Астрономический таймер контроллера";
                } else if (arrControlMetod[1] == "16") {
                    strControlMetod += "Астрон.таймер+расписание";
                } else if (arrControlMetod[1] == "18") {
                    strControlMetod += "Ручное управление(hard)";
                }
            }
            var onOff;
            var divOnOff;
            var img = '<img src="/img/house.png" height="20" />';
            if (arrLightReal[1] == 1 && arrLightMk[1] == 1) {
                onOff = "#ffd700";
                divOnOff = '<div><img src="/img/enabled.png" height="20" /><button class="btn btn-secondary btn-sm"" ng-click="model.lightOff(' + data.id + ')">Выкл.</button></div >';
                img = '<img src="/img/enabled.png" height="20" />';
            } else if (arrLightReal[1] == 0 && arrLightMk[1] == 0) {
                onOff = "#1b307d";
                divOnOff = '<div><img src="/img/disabled.png" height="20" /><button class="btn btn-warning btn-sm"" id="lightOn-' + data.id + '">Вкл.</button></div >';
                img = '<img src="/img/disabled.png" height="20" />';
            } else if (arrLightReal[1] == 1 && arrLightMk[1] == 0) {
                onOff = "#802b00";
                divOnOff = '<div><img src="/img/enabledError.png" height="20" /><button class="btn btn-warning btn-sm"" ng-click="model.lightOn()">Вкл.</button><button class="btn btn-secondary btn-sm"" ng-click="model.lightOff()">Выкл.</button></div >';
                img = '<img src="/img/disabledError.png" height="20" />';
            } else if (arrLightReal[1] == 0 && arrLightMk[1] == 1) {
                onOff = "#ff3300";
                divOnOff = '<div><img src="/img/disabledError.png" height="20" /><button class="btn btn-warning btn-sm"" ng-click="model.lightOn()">Вкл.</button><button class="btn btn-secondary btn-sm"" ng-click="model.lightOff()">Выкл.</button></div >';
                img = '<img src="/img/enabledError.png" height="20" />';
            }

            var center = [55.898705001674905, 53.92454624176026];
            if (myMap == null) {
                // Создаем карту.
                myMap = new ymaps.Map("map", {
                    center: center,
                    
                    zoom: 15,
                    // Тип покрытия карты: "Спутник"
                    type: 'yandex#satellite'
                }, {
                    searchControlProvider: 'yandex#search'
                });
            }

            // Создаем ломаную, используя класс GeoObject.
            var myGeoObject = new ymaps.GeoObject({
                // Описываем геометрию геообъекта.
                geometry: {
                    // Тип геометрии - "Ломаная линия".
                    type: "LineString",
                    // Указываем координаты вершин ломаной.
                    coordinates: coordinates
                },
                // Описываем свойства геообъекта.
                properties: {
                    // Содержимое хинта.
                    hintContent: data.name,
                    // Содержимое балуна.
                    balloonContentHeader: img + data.name,
                    balloonContentBody: strControlMetod,
                    balloonContentFooter: dtContollers
                }
            }, {
                    // Задаем опции геообъекта.
                    // Включаем возможность перетаскивания ломаной.
                    draggable: false,
                    // Цвет линии.
                    strokeColor: onOff,
                    // Ширина линии.
                    strokeWidth: 3
                });

            // Добавляем линии на карту.
            myMap.geoObjects
                .add(myGeoObject);
        }
    }
});