angular.module("app")

.controller("MailerEditCtrl", function ($scope, $uibModal, $transport, $helper, mailerSvc, $timeout, $filter, $log, $list, $reports, $q, $actions, commonSvc, taskSvc) {

    var data = $scope.$parent.window.data;

    var model = {
        window: $scope.$parent.window,
        modal: undefined,
        //
        only1: false,
        enable1: false,
        //
        editedCounter: 0,
        state: ""
    };

    model.kinds = mailerSvc.kinds;
        //[
    //    { text: "Запрещена", value: "disabled" },
    //    { text: "Вручную", value: "manual" },//по умолчанию
    //    { text: "Автоматически", value: "auto" }
    //];
    
    model.ranges = mailerSvc.ranges;
    //[
    //    { text: "День", value: "Day" },//по умолчанию
    //    { text: "Месяц", value: "Month" }
    //];

    //Список параметров, используется при загруке и сохранении нода, а также для UNDO и определения, есть ли изменения
    var propertiesSimple = ["id", "type", "name", "receiver", "range", "kind", "isHidden", "nullAllowed", "reportMonthly", "reportDaily", "reportPdf", "reportXls", "reportSpecificDay", "dateSpecificDay"];
    var propertiesArray = ["tubeIds", "reportIds", "taskIds"];
    var properties = ["id", "type", "name", "receiver", "range", "kind", "isHidden", "nullAllowed", "tubeIds", "reportIds", "taskIds", "reportMonthly", "reportDaily", "reportPdf", "reportXls", "reportSpecificDay", "dateSpecificDay"];



    //$actions.get("mailer").then(function (a) {
    //    if (a) {
    //        model.actionListMailer = function (id) {
    //            model.close();
    //            a.act({ id: id });
    //        };
    //    }
    //});


    ////

    model.reports = [];
    
    $reports.all().then(function (answer) {
        for (var i = 0; i < answer.reports.length; i++) {
            var report = answer.reports[i];
            model.reports.push(report);
        }
    });

    ////

    model.tasks = [];

    taskSvc.tasks().then(function (answer) {
        for (var i = 0; i < answer.tasks.length; i++) {
            var task = answer.tasks[i];
            model.tasks.push(task);
        }
    });

    ///////

    model.newId = null;

    if (data && data.id) {
        model.selectedId = data.id;
        model.only1 = true;
    } else if (data && data.isNew) {
        //создание нового 
        model.only1 = true;
        model.newId = $helper.createGuid(1).then(function (message) {
            var guids = message.body.guids;
            if ($helper.isArray(guids) && guids.length > 0) {
                model.selectedId = guids[0];
                return guids[0];
            }
            return null;
        });
    }

    if (data.ref) {

    }


    ////

    var wrap = function (selected, rows) {

        selected.tubeIds = [];
        selected._reportDaily = {};
        selected._reportMonthly = {};
        selected._reportSpecificDay = {};
        selected._reportPdf = {};
        selected._reportXls = {};
        selected._dateSpecificDay = {};
        //unpack report periods
        if (selected.reportDaily) {
            var daily = selected.reportDaily.split(";");
            for (var i = 0; i < daily.length; i++) {
                var day = daily[i];
                selected._reportDaily[day] = true;
            }
        }
        if (selected.reportMonthly) {
            var monthly = selected.reportMonthly.split(";");
            for (var i = 0; i < monthly.length; i++) {
                var month = monthly[i];
                selected._reportMonthly[month] = true;
            }
        }
        if (selected.reportSpecificDay) {
            var specificDays = selected.reportSpecificDay.split(";");
            for (var i = 0; i < specificDays.length; i++) {
                var specificDay = specificDays[i];
                selected._reportSpecificDay[specificDay] = true;
            }
        }
        if (selected.dateSpecificDay) {
            var specificDays = selected.dateSpecificDay.split(";");
            for (var i = 0; i < specificDays.length; i++) {
                var specificDay = specificDays[i].split(",")[0];
                var day = specificDays[i].split(",")[1];
                if (day > 28) day = 28;
                selected._dateSpecificDay[specificDay] = day;
            }
        }
        if (selected.reportPdf) {
            var reps = selected.reportPdf.split(";");
            for (var i = 0; i < reps.length; i++) {
                var r = reps[i];
                selected._reportPdf[r] = true;
            }
        }
        if (selected.reportXls) {
            var reps = selected.reportXls.split(";");
            for (var i = 0; i < reps.length; i++) {
                var r = reps[i];
                selected._reportXls[r] = true;
            }
        }

        if (!selected.Tube) selected.Tube = [];
        if (!selected.Report) selected.Report = [];

        for (var i = 0; i < selected.Tube.length; i++) {
            var id = selected.Tube[i].id;
            selected.tubeIds.push(id);
            for (var j = 0; j < rows.length; j++) {
                if (rows[j].id == id) {
                    selected.Tube[i] = rows[j];
                    break;
                }
            }
        }

        selected._deletingTube = false;

        selected._deleteTube = function(id)
        {
            if (id == "all") {
                selected.tubeIds = [];
                selected.Tube.length = 0;
            } else {
                //delete from tubeIds
                for (var i = selected.tubeIds.length; i > 0; i--) {
                    if (selected.tubeIds[i - 1] == id) {
                        selected.tubeIds.splice(i - 1, 1);
                    }
                }

                //delete from Tube
                for (var i = selected.Tube.length; i > 0; i--) {
                    if (selected.Tube[i - 1].id == id) {
                        selected.Tube.splice(i - 1, 1);
                    }
                }
            }
            
            selected._deletingTube = false;
        }

        //report
        selected._reportAddIds = [];


        selected._setDefaultReportDaily = function (report) {
            //selected._reportDaily[report.id] = !(report.range && report.range != "Hour");
        }
        selected._setDefaultReportSpecificDay = function (report) {
            //selected._reportDaily[report.id] = !(report.range && report.range != "Hour");
        }
        selected._setDefaultDateSpecificDay = function (report) {
            //selected._reportDaily[report.id] = !(report.range && report.range != "Hour");
        }
        selected._setDefaultReportMonthly = function (report) {
            //selected._reportMonthly[report.id] = (report.range && report.range != "Hour");
        }

        selected._setDefaultReportPdf = function (report) {
            selected._reportPdf[report.id] = true;
        }

        selected._setDefaultReportXls = function (report) {
            //selected._reportMonthly[report.id] = (report.range && report.range != "Hour");
        }


        selected._reportDailyRecalc = function () {
            var res = "";
            for (var id in selected._reportDaily) {
                if (id && selected._reportDaily.hasOwnProperty(id) && selected._reportDaily[id]) {
                    if (res != "") res += ";";
                    res += id;
                }
            }
            selected.reportDaily = res;
        }

        selected._reportMonthlyRecalc = function () {
            var res = "";
            for (var id in selected._reportMonthly) {
                if (id && selected._reportMonthly.hasOwnProperty(id) && selected._reportMonthly[id]) {
                    if (res != "") res += ";";
                    res += id;
                }
            }
            selected.reportMonthly = res;
        }
        selected._reportSpecificDayRecalc = function () {
            var res = "";
            for (var id in selected._reportSpecificDay) {
                if (id && selected._reportSpecificDay.hasOwnProperty(id) && selected._reportSpecificDay[id]) {
                    if (res != "") res += ";";
                    res += id;
                }
            }
            selected.reportSpecificDay = res;
        }
        selected._dateSpecificDayRecalc = function () {
            var res = "";
            for (var id in selected._dateSpecificDay) {
                if (id && selected._dateSpecificDay.hasOwnProperty(id) && selected._dateSpecificDay[id]) {
                    if (res != "") res += ";";
                    res += id + "," + selected._dateSpecificDay[id];
                }
            }
            selected.dateSpecificDay = res;
        }
        selected._reportPdfRecalc = function () {
            var res = "";
            for (var id in selected._reportPdf) {
                if (id && selected._reportPdf.hasOwnProperty(id) && selected._reportPdf[id]) {
                    if (res != "") res += ";";
                    res += id;
                }
            }
            selected.reportPdf = res;
        }

        selected._reportXlsRecalc = function () {
            var res = "";
            for (var id in selected._reportXls) {
                if (id && selected._reportXls.hasOwnProperty(id) && selected._reportXls[id]) {
                    if (res != "") res += ";";
                    res += id;
                }
            }
            selected.reportXls = res;
        }


        selected._deleteReport = function (id) {
            if (id == undefined) {
                return;
            }

            //delete from reportIds&ranges
            for (var i = selected.reportIds.length; i > 0; i--) {
                if (selected.reportIds[i - 1] == id) {
                    selected.reportIds.splice(i - 1, 1);
                }
            }

            //delete from Report
            for (var i = selected.Report.length; i > 0; i--) {
                if (selected.Report[i - 1].id == id) {
                    selected.Report.splice(i - 1, 1);
                }
            }

            //delete send period
            delete selected._reportDaily[id];
            delete selected._reportMonthly[id];
            delete selected._reportSpecificDay[id];
            delete selected._dateSpecificDay[id];
            delete selected._reportXls[id];
            delete selected._reportPdf[id];
            
            selected._toggleReportState("delete");
        }

        selected._reportState = "idle";
        selected._toggleReportState = function (reportState) {
            if (selected._reportState == "idle") {
                //in
                switch (reportState) {
                    case "add":
                        break;
                    case "delete":
                        break;
                }
                selected._reportState = reportState;
            } else if (selected._reportState == reportState) {
                //out
                switch (selected._reportState) {
                    case "add":
                        for (var i = 0; i < selected._reportAddIds.length; i++) {
                            var id = selected._reportAddIds[i];

                            for (var j = 0; j < selected.reportIds.length; j++) {
                                var existId = selected.reportIds[j];
                                if (id == existId) break;
                            }

                            if (j != selected.reportIds.length) continue; //повтор
                            
                            selected.reportIds.push(id);

                            var rep;

                            angular.forEach(model.reports, function (s) {
                                if (s.id == id) {
                                    rep = s;
                                    selected.Report.push(s);
                                }
                            });

                            if (rep && rep.id) {
                                selected._setDefaultReportDaily(rep);
                                selected._reportDailyRecalc();
                                selected._setDefaultReportMonthly(rep);
                                selected._reportMonthlyRecalc();
                                selected._setDefaultReportSpecificDay(rep);
                                selected._reportSpecificDay();
                                selected._setDefaultDateSpecificDay(rep);
                                selected._dateSpecificDay();
                                selected._setDefaultReportPdf(rep);
                                selected._reportPdfRecalc();
                                selected._setDefaultReportXls(rep);
                                selected._reportXlsRecalc();
                            }
                        }
                        break;
                    case "delete":
                        break;
                }
                selected._reportState = "idle";
            }
        }

        selected.reportIds = [];

        for (var i = 0; i < selected.Report.length; i++) {
            var report = selected.Report[i];
            selected.reportIds.push(report.id);

            if (!selected._reportDaily[report.id] && !selected._reportMonthly[report.id] && !selected._reportSpecificDay[report.id]) {
                selected._setDefaultReportDaily(report);
                selected._setDefaultReportMonthly(report);
                selected._setDefaultReportSpecificDay(report);
                selected._reportSpecificDayRecalc();
                selected._setDefaultDateSpecificDay(report);
                selected._dateSpecificDayRecalc();
                selected._reportDailyRecalc();
                selected._reportMonthlyRecalc();
            }
            
            if (!selected._reportPdf[report.id] && !selected._reportXls[report.id]) {
                selected._setDefaultReportPdf(report);
                selected._setDefaultReportXls(report);
                selected._reportPdfRecalc();
                selected._reportXlsRecalc();
            }
        }
        ////


        //task
        selected = commonSvc.wrapTaskChoose(selected, model.tasks);


        //умолчания
        selected.range = selected.range || model.ranges[0].value;
        selected.kind = selected.kind || model.kinds[1].value;

        //
        selected.undo = {};
        $helper.copyToFrom(selected.undo, selected, properties);
        
        selected.selectable = true;

        selected.reload = function () {
            $helper.copyToFrom(selected, selected.undo, properties);
        }

        selected.edited = function () {
            return  selected.isNew || !$helper.areEqual(selected, selected.undo, properties);
        }

        selected.showRange = function () {
            var s = $filter('filter')(model.ranges, { value: selected.range });
            return (selected.range && s.length) ? s[0].text : model.ranges[0].text;
        }

        selected.showKind = function () {
            var s = $filter('filter')(model.kinds, { value: selected.kind });
            return (selected.kind && s.length) ? s[0].text : model.kinds[1].text;
        }

        selected.toggleHide = function () {
            selected.isHidden = !(selected.isHidden == true);
        }
        
        selected.chooseTubes = function () {

            var modalInstance = $uibModal.open({
                animation: true,
                templateUrl: "tpls/list-select-modal.html",
                controller: "ListSelectCtrl",
                size: "md",
                resolve: {
                    data: function () {
                        var tubeIds = [];
                        for (var i = 0; i < selected.tubeIds.length; i++) {
                            var id = selected.tubeIds[i];
                            tubeIds.push(id);
                        }
                        return tubeIds;
                    }
                }
            });

            modalInstance.result.then(function (selectedIds) {
                var prom = selectedIds.length > 0 ? $list.getRows(selectedIds) : $q.when(null);
                prom.then(function (message) {
                    return (message == null ? [] : message);
                }, function (error) {
                    return [];
                })
                .then(function (rows) {
                    for (var i = 0; i < selectedIds.length; i++) {
                        var id = selectedIds[i];

                        for (var j = 0; j < selected.tubeIds.length; j++) {
                            var existId = selected.tubeIds[j];
                            if (id == existId) break;
                        }

                        if (j != selected.tubeIds.length) continue; //повтор

                        selected.tubeIds.push(id);
                        for (var j = 0; j < rows.length; j++) {
                            if (rows[j].id == id) {
                                selected.Tube.push(rows[j]);
                                break;
                            }
                        }
                    }
                });
            });
        }


        selected.save = function () {
            var rules = [];
            selected.toSave = {};
            if (selected.isHidden) {
                selected.kind = "disabled";
            }
            $helper.copyToFrom(selected.toSave, selected, propertiesSimple);
            rules.push({ action: selected.isNew? "add" : "upd", target: "node", content: { id: selected.id, type: "Mailer", body: selected.toSave } });
            delete selected.toSave;
            //reports
            {
                var del = $helper.arrayDiff(selected.undo.reportIds, selected.reportIds);
                for (var i = 0; i < del.length; i++) {
                    rules.push({ action: "del", target: "relation", content: { start: selected.id, end: del[i], type: "based", body: {} } });
                }
                var add = $helper.arrayDiff(selected.reportIds, selected.undo.reportIds);
                for (var i = 0; i < add.length; i++) {
                    rules.push({ action: "add", target: "relation", content: { start: selected.id, end: add[i], type: "based", body: {} } });
                }
            }
            //tubes
            {
                var del = $helper.arrayDiff(selected.undo.tubeIds, selected.tubeIds);
                for (var i = 0; i < del.length; i++) {
                    rules.push({ action: "del", target: "relation", content: { start: selected.id, end: del[i], type: "using", body: {} } });
                }
                var add = $helper.arrayDiff(selected.tubeIds, selected.undo.tubeIds);
                for (var i = 0; i < add.length; i++) {
                    rules.push({ action: "add", target: "relation", content: { start: selected.id, end: add[i], type: "using", body: {} } });
                }
            }
            //tasks
            rules = rules.concat(commonSvc.updateRelationsTask(selected));
            //
            return $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function (message) {
                selected.isNew = false;
                selected.undo = {};
                $helper.copyToFrom(selected.undo, selected, properties);
                return message;
            });
        }

        return selected;
    }

    model.editedCount = function () {
        var count = 0;
        if (!model.objs) return count;
        for (var i = 0; i < model.objs.length; i++) {
            var mailer = model.objs[i];
            if (mailer.edited()) {//изменен текст
                count++;
            }
        }
        return count;
    }

    $scope.$watch(model.editedCount, function (count) {
        model.editedCounter = count;
        switch (model.state) {
            case "init":
                if (count > 0) {
                    model.editState();
                }
                break;
            case "edit":
                if (count == 0) {
                    model.initState();
                }
                break;
        }
    });


    model.editState = function () {
        if ((model.state != "init") && (model.state != "save")) return;
        model.state = "edit";
    }

    model.initState = function () {
        //if (model.state == "init") return;
        model.state = "init";

        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objs;
        //
        model.enable1 = false;

        mailerSvc.all()
            .then(function (answer) {
                var proms = [$q.when(model.newId)];
                for (var i = 0; i < answer.body.mailers.length; i++) {
                    var m = answer.body.mailers[i];
                    proms.push(mailerSvc.get(m.id));
                }
                return $q.all(proms);
            })
            .then(function (answers) {
                model.objs = [];
                var newId = answers[0];
                var tubeIds = {};

                for (var i = 1; i < answers.length; i++) {
                    var m = answers[i].body.mailer;
                    if (!m.Tube) continue;

                    for (var j = 0; j < m.Tube.length; j++) {
                        tubeIds[m.Tube[j].id] = 1;
                    }
                }
                
                var ids = $helper.assocToArray(tubeIds);
                var prom = ids.length > 0 ? $list.getRows(ids) : $q.when(null);

                prom.then(function (message) {
                    return (message == null ? [] : message);
                }, function (error) {
                    return [];
                })
                .then(function (rows) {
                    for (var i = 1; i < answers.length; i++) {
                        var mailer = answers[i].body.mailer;
                        if (newId && mailer.id == newId) newId = null;//проверка на существование
                        model.objs.push(wrap(mailer, rows));
                    }

                    if (newId) {
                        var n = { id: newId, name: "Новая рассылка", receiver: "", kind: model.kinds[1].value, range: model.ranges[0].value, isHidden: false, nullAllowed: false, type: "Mailer", isNew: true };
                        model.objs.push(wrap(n, rows));
                    }

                    model.sorted = $filter('orderBy')(model.objs, 'name');

                    //select
                    if (model.objs.length == 0) {
                        //none
                    } else if (model.objs.length == 1) {
                        model.selected = model.objs[0];
                    } else if (model.selectedId) {//restore selected
                        for (var i = 0; i < model.objs.length; i++) {
                            var d = model.objs[i];
                            if (d.id == model.selectedId) {
                                model.selected = d;
                                break;
                            }
                        }
                    } else {
                        var sel;
                        for (var i = 0; i < model.sorted.length; i++) {
                            var r = model.sorted[i];
                            if (r.selectable) {
                                sel = r;
                                break;
                            }
                        }
                        if (sel) {
                            for (var i = 0; i < model.objs.length; i++) {
                                var r = model.objs[i];
                                if (r.id == sel.id) {
                                    model.select(model.objs[i], false);
                                    break;
                                }
                            }
                        }
                    }
                });
            });
    };

    model.addNew = function () {
        //создание нового 
        //model.only1 = true;
        model.newId = $helper.createGuid(1).then(function (message) {
            var guids = message.body.guids;
            if ($helper.isArray(guids) && guids.length > 0) {
                model.selectedId = guids[0];
                return guids[0];
            }
            return null;
        });
        //
        model.initState();
    }

    model.select = function (mailer) {
        if (mailer.selectable) {
            model.selected = mailer;
            //init();
        }
    };

    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    model.initState();


    model.resetAll = function () {
        model.newId = null;
        model.initState();
    }


    model.saveState = function () {
        if (!model.objs) return;
        if (model.state != "edit") return;
        model.state = "save";
        model.saveCounterMax = model.objs.length;
        model.saveCounter = 0;
        model.saveOkCounter = 0;
        model.saveErrCounter = 0;

        var saveProcessDone = function (isErr) {
            model.saveCounter++;
            if (isErr) {
                model.saveErrCounter++;
            } else {
                model.saveOkCounter++;
            }

            if (model.saveCounter == model.saveCounterMax) {//processed all
                if (model.saveErrCounter == 0) {
                    model.newId = null;
                    model.initState();
                } else {
                    model.editState();
                }
            }
        }

        for (var i = 0; i < model.objs.length; i++) {
            var m = model.objs[i];
            if (m.edited()) {//изменен
                m.save()
                    .then(function () {
                        saveProcessDone(0);
                    })
                    .catch(function (err) {
                        $log.debug("ошибка при сохранении: " + err);
                        saveProcessDone(1);
                    });
            } else {
                saveProcessDone(0);
            }
        }
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

    $scope.model = model;
});
