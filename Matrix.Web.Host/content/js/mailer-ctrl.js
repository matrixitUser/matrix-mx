angular.module("app")

.controller("MailerCtrl", function ($scope, $uibModal, $transport, $helper, mailerSvc, $timeout, $filter, $log, $list, $reports, $actions) {

    var contractHour = 10;

    var data = $scope.$parent.window.data;

    var model = {
        window: $scope.$parent.window,
        modal: undefined,
        //
        only1: false, //кнопка боковой панели
        showAll: false, //функция показать все (...)
        enable1: false,
        filter: {}
    }

    model.doShowAll = function () {
        model.showAll = false;
        model.filter = {};
    }

    model.kinds = mailerSvc.kinds;

    model.ranges = mailerSvc.ranges;
    //[
    //    { text: "Отправка запрещена", value: "disabled" },
    //    { text: "Не задано", value: "manual" },//по умолчанию
    //    { text: "По расписанию", value: "auto" }
    //];

    //model.schedules = [
    //    { text: "Каждый день утром", value: "hour0" },//по умолчанию
    //    { text: "В начале месяца утром", value: "day1" }
    //];//[
    //    { text: "День", value: "Day" },//по умолчанию
    //    { text: "Месяц", value: "Month" }
    //];

    //model.periods = [
    //    { name: "Daily", caption: "Сутки" },
    //    { name: "Monthly", caption: "Месяц" }
    //];

    model.getStart = function (date, period) {
        var res = new Date(date);
        if (period == 'Daily') {
            res.setHours(contractHour - 24, 0, 0, 0);//отчётный час на вчера
        } else if (period == 'DailyMonthly') {
            res.setHours(contractHour - 24, 0, 0, 0);//отчётный час на вчера
            res.setDate(1);//1-е число месяца
        } else {
            res.setHours(0, 0, 0, 0);
            res.setDate(0);//пред. месяц
            res.setDate(1);//1-е число пред. месяца
        }
        return res;
    };

    model.getEnd = function (date, period) {
        var res = new Date(date);
        if ((period == 'Daily') || (period == 'DailyMonthly')) {
            res.setHours(contractHour, 0, 0, 0);
        } else {
            res.setHours(0, 0, 0, 0);
            res.setDate(1);//1-е число месяца
        }
        return res;
    };

    model.checkIsMonthly = function(date)
    {
        //var d = new Date(date);
        return (date.getDate() == 1);
    }


    $actions.get("mailer-edit").then(function (a) {
        if (a) {
            model.actionEdit = function (id) {
                a.act({ id: id }).finally(function () {
                    model.reset();
                });
            };
        }
    });

    $actions.get("report-list").then(function (a) {
        if (a) {
            model.actionBuildReport = function (m, rid, period) {
                //m.lastError = "";
                rid = rid || (m.reports.length > 0 ? m.reports[0] : undefined);
                period = period || "Daily";
                var ids = [];
                for (var i = 0; i < m.tubeRows.length; i++)
                {
                    if(m.tubeRows[i].isDisabled !== true)
                    {
                        ids.push(m.tubeRows[i].id);
                    }
                }
                //if (ids.length > 0)
                {
                    a.act({ reportId: rid, ids: ids || [], start: model.getStart(m.date, period), end: model.getEnd(m.date, period) }).finally(function () {
                        model.reset();
                    });
                }
            };
        }
    });


    model.reports = [];

    //for (var i = 0; i < $reports.list.length; i++) {
    //    var report = $reports.list[i];
    //    model.reports.push(report);
    //}

    $reports.all().then(function (answer) {
        for (var i = 0; i < answer.reports.length; i++) {
            var report = answer.reports[i];
            model.reports.push(report);
        }
    });

    
    if (data && data.id) {
        model.selectedId = data.id;
        model.only1 = true;
    } 

    ////

    model.showReportName = function (rid) {
        for (var i = 0; i < model.reports.length; i++) {
            var r = model.reports[i];
            if (r.id == rid) {
                return r.name;
            }
        }
        return "[" + rid + "]";
    }

    var wrap = function (obj) {

        obj._reportDaily = {};
        obj._reportMonthly = {};
        obj._reportPdf = {};
        obj._reportXls = {};

        //unpack report details
        if (obj.reportDaily) {
            var daily = obj.reportDaily.split(";");
            for (var i = 0; i < daily.length; i++) {
                var day = daily[i];
                obj._reportDaily[day] = true;
            }
        }
        if (obj.reportMonthly) {
            var monthly = obj.reportMonthly.split(";");
            for (var i = 0; i < monthly.length; i++) {
                var month = monthly[i];
                obj._reportMonthly[month] = true;
            }
        }
        if (obj.reportPdf) {
            var reps = obj.reportPdf.split(";");
            for (var i = 0; i < reps.length; i++) {
                var r = reps[i];
                obj._reportPdf[r] = true;
            }
        }
        if (obj.reportXls) {
            var reps = obj.reportXls.split(";");
            for (var i = 0; i < reps.length; i++) {
                var r = reps[i];
                obj._reportXls[r] = true;
            }
        }

        //умолчания
        obj.range = obj.range || model.ranges[0].value;
        obj.kind = obj.kind || model.kinds[1].value;
        obj.date = new Date();

        //if (obj.kind == "auto") {
        //if (obj.range == "Month") {
        //    var e = new Date();
        //    //e.setDate(-1);              //23 часа за "вчера"
        //    e.setHours(-1, 0, 0, 0);        //23 часа за "вчера"

        //    var s = new Date(e);
        //    s.setDate(1);
        //    s.setHours(0);

        //    obj.start = s;
        //    obj.end = e;
        //} else {  /* (obj.range == "Day") */
        //    var e = new Date();
        //    e.setHours(contractHour - (e.getHours() < contractHour ? 24 : 0), 0, 0, 0);    //полные коммерческие сутки: если сейчас <CH, то end = вчера в CH, иначе - сегодня в CH

        //    var s = new Date(e);
        //    s.setHours(contractHour - 24);

        //    obj.start = s;
        //    obj.end = e;
        //}
        //} else {
        //    var s = new Date();
        //    var e = new Date();

        //    s.setHours(0, 0, 0, 0);
        //    e.setMinutes(0, 0, 0);

        //    obj.start = s;
        //    obj.end = e;
        //}

        obj.selectable = obj.kind && (obj.kind != "disabled");
        
        obj.lastError = "";
        obj.send = function () { 
            obj.isSending = true;
            obj.lastError = "";
            mailerSvc.send(obj.id, obj.date).then(function (result) {
                obj.lastError = (result.body.success == false) ? result.body.error : "";
                obj.isSending = false;
            }, function () {
                obj.lastError = "Неизвестная ошибка";
                obj.isSending = false;
            });
        }

        obj.showRange = function () {
            var selected = $filter('filter')(model.ranges, { value: obj.range });
            return (obj.range && selected.length) ? selected[0].text : model.ranges[0].text;
        }

        //obj.showSchedule = function () {
        //    var selected = $filter('filter')(model.schedules, { value: obj.schedule });
        //    return (obj.schedule && selected.length) ? selected[0].text : model.schedules[0].text;
        //}

        obj.showKind = function () {
            var selected = $filter('filter')(model.kinds, { value: obj.kind });
            return (obj.kind && selected.length) ? selected[0].text : model.kinds[1].text;
        }

        obj.showReports = function () {
            var selected = [];
            angular.forEach(model.reports, function (s) {
                if (obj.reports.indexOf(s.id) >= 0) {
                    selected.push(s.name);
                }
            });

            //return selected.length ? selected.join('<br />') : 'не выбрано';
            if (!selected.length) return 'не выбрано';
            var ret = "";
            for (var i = 0; i < selected.length; i++) {
                ret += "<li>" + selected[i] + "</li>";
            }
            return "<ul>" + ret + "</ul>";
        };


        //

        delete obj.obj;
        obj.tubeIds = [];
        obj.tubeRows = [];
        obj.reports = [];

        mailerSvc.get(obj.id).then(function (answer) {
            if (answer.body.mailer) {
                obj.obj = answer.body.mailer;
                //
                if ($helper.isArray(obj.obj.Report)) {
                    for (var i = 0; i < obj.obj.Report.length; i++) {
                        var report = obj.obj.Report[i];
                        obj.reports.push(report.id);
                    }
                }
                //
                if ($helper.isArray(obj.obj.Tube)) {
                    for (var i = 0; i < obj.obj.Tube.length; i++) {
                        var tube = obj.obj.Tube[i];
                        obj.tubeIds.push(tube.id);
                        //obj.tubeRows.push($list.getRow(tube.id));
                    }
                    return obj.tubeIds.length > 0 ? $list.getRowsCacheFiltered({ ids: obj.tubeIds }) : $q.when([]);
                }
            }
            return $q.reject(null);
        }).then(function (message) {
            for (var i = 0; i < message.rows.length; i++)
            {
                var row = message.rows[i];
                obj.tubeRows.push(row);
            }
        }).catch(function (err) {
            obj.obj = null;
        });
        
        return obj;
    }

    var init = function () {
        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objs;
        //
        model.enable1 = false;

        mailerSvc.all().then(function (answer) {
            model.objs = [];
            for (var i = 0; i < answer.body.mailers.length; i++) {
                var m = answer.body.mailers[i];
                if (m.isHidden) continue;
                model.objs.push(wrap(m));
            }
            model.sorted = $filter('orderBy')(model.objs, 'name');
            
            //select
            if (model.objs.length == 0) {
                //none
            } else if (model.objs.length == 1) {
                if (model.objs[0].selectable) {
                    model.selected = model.objs[0];
                }
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

    };

    model.select = function (m) {
        if (m.selectable) {
            model.selected = m;
            //init();
        }
    };

    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    init();


    model.reset = function () {
        init();
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
