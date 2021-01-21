angular.module("app");

app.controller("ReportListCtrl", function ($scope, $log, $sce, $reports, $filter, $modal, $parse, $timeout, $list, $settings) {

    var contractHour = 10;
    
    var d = new Date();
    var data = $scope.$parent.window.data;  /* .reportId - открыть отчёт, .ids - выбранные объекты, .header, .start .end */

    var names = [];
    var references = [];
    var rows = [];

    var header = "";
    if (data && data.header) {
        header = data.header;
    }
    
    /*
    var model = $scope.$parent.model;
    /*/
    var model = {
        window: $scope.$parent.window,
        idleCounter: 0,
        waitCounter: 0,
        doneCounter: 0,
        errorCounter: 0,
        only1: false, //кнопка боковой панели
        showAll: true, //функция показать все (...)
        state: 'idle',

        data: data,
        names: names,
        modal: undefined,
        header: header,
        filter: {}
    }

    model.seDates = [{
        name: "hour",
        caption: "Часы",
        range: "Hour",
        update: function (report, start) {
            if (start) {
                //report.start = (start) ? new Date(start) : new Date();
                //report.start.setHours(0, 0, 0, 0);

                report.end = new Date(start);
                report.end.setHours((report.range == 'Hour' ? 24 + contractHour : 24), 0, 0, 0);
            }
            report.range = "Hour";
        },
        prev: function (date) {
            var d = new Date(date);
            d.setHours(d.getHours() - 24, 0, 0, 0);
            return d;
        },
        next: function (date) {
            var d = new Date(date);
            d.setHours(d.getHours() + 24, 0, 0, 0);
            return d;
        }
    },  {
        name: "day0",
        caption: "Сутки",
        range: "Day",
        update: function (report, start) {
            if (start) {
                report.end = new Date(start);
                report.end.setHours((report.range == 'Day' ? 24 + contractHour : 24), 0, 0, 0);
            }
            report.range = "Day";
        },
        prev: function (date) {
            var d = new Date(date);
            d.setHours(d.getHours() - 24, 0, 0, 0);
            return d;
        },
        next: function (date) {
            var d = new Date(date);
            d.setHours(d.getHours() + 24, 0, 0, 0);
            return d;
        }
    },  {
        name: "month1",
        caption: "Месяц",
        range: "Month",
        update: function (report, start) {
            if (start) {
                //report.start = (start) ? new Date(start) : new Date();
                //report.start.setDate(1);
                //report.start.setHours(0, 0, 0, 0);

                report.end = new Date(start);
                report.end.setMonth(report.end.getMonth() + 1, 1);
                report.end.setHours((report.range == 'Hour' ? -1 : 0), 0, 0, 0);
            } else {

                report.selectMonth = report.start.getMonth().toString();
                report.selectYear = report.start.getFullYear().toString();
                //report.start = new Date();
                //report.start.setDate(1);
                //report.start.setHours(0, 0, 0, 0);

                //report.end = new Date();
                //report.end.setMonth(report.end.getMonth() + 1, 1);
                //report.end.setHours((report.range == 'Hour' ? -1 : 0), 0, 0, 0);
            }
            report.range = "Month";
        },
        prev: function (date) {
            var d = new Date(date);
            d.setMonth(d.getMonth() - 1, 1);
            d.setHours(0, 0, 0, 0);
            return d;
        },
        next: function (date) {
            var d = new Date(date);
            d.setMonth(d.getMonth() + 1, 1);
            d.setHours(0, 0, 0, 0);
            return d;
        }
    }, {
        name: "custom",
        caption: "Произвольный",
        update: function (report, start) {
            report.range = "Hour";
        }
    }];

    model.seFind = function (find) {
        var target;
        if (angular.isString(find)) { //find by name
            target = { name: find }
        } else if (angular.isObject(find)) { // find by 
            target = find;
        }
        for (var i = 0; i < model.seDates.length; i++) {
            var se = model.seDates[i];
            for (var key in target) {
                if (key !== undefined && target.hasOwnProperty(key)) {
                    if (se[key] == target[key]) return se;
                }
            }
        }
        return model.seDates[0];
    }

    //
    var reportId, reportStart, reportEnd, reportIds;
    var wrap = function (report) {
        //
        delete report.template;

        if (!report.range) {
            report.range = "Hour";
        }

        report.isOrientationAlbum = (report.isOrientationAlbum === true);

        //State
        report.state = "idle";
        report.wait = false;
        report.success = true;

        report.seSelect = function (se) {           // при выборе se:
            if (se.update) {
                se.update(report);                  // обновление start+end
            }
            report.seDate = se;
        }

        report.prevDate = function () {
            var se = report.seDate;
            if (se && se.prev && report.start) {
                if (report.range == "Month") {
                    var start = new Date();
                    start.setDate(1);
                    start.setMonth(report.selectMonth);
                    start.setFullYear(report.selectYear);
                    start.setHours(0, 0, 0, 0);
                    report.start = start;
                }
                report.start = se.prev(report.start);
                report.end = se.prev(report.end);

                report.selectYear = report.start.getFullYear().toString();
                report.selectMonth = report.start.getMonth().toString();

                report.startChanged();
                report.updateWithTimeout();
            }
        }

        report.canDoPrevDate = function () {
            var se = report.seDate;
            return (se && se.prev && report.start);
        }

        report.nextDate = function () {
            var se = report.seDate;
            if (se && se.next && report.start) {
                if (report.range == "Month") {
                    var start = new Date();
                    start.setDate(1);
                    start.setMonth(report.selectMonth);
                    start.setFullYear(report.selectYear);
                    start.setHours(0, 0, 0, 0);
                    report.start = start;
                }
                report.start = se.next(report.start);
                report.end = se.next(report.end);

                report.selectYear = report.start.getFullYear().toString();
                report.selectMonth = report.start.getMonth().toString();

                report.startChanged();
                report.updateWithTimeout();
            }
        }

        report.canDoNextDate = function () {
            var se = report.seDate;
            return (se && se.next && report.start);
        }
        
        var start;
        var end;

        if (data && data.start && data.end && data.reportId && (data.reportId == report.id)) {
            start = new Date(data.start);
            end = new Date(data.end);
        } else {
            var range = $settings.getReportlistRange(report.range);
            if (range && range.start && range.end) {
                start = new Date(range.start);
                end = new Date(range.end);
            } else {
                start = new Date();
                end = new Date();
                if (report.range == "Day") {                // суточный диапазон отчёта
                    //3.02 13:24->1.02 0:00
                    //1.02 00:35->31.01 00:00
                    start.setHours(-24, 0, 0, 0);
                    start.setDate(1);
                    //start.setHours(0);
                    end.setHours(-1, 0, 0, 0);
                } else if (report.range == "Month") {       // месячный диапазон отчёта
                    start.setDate(0);
                    start.setMonth(1, 1);
                    start.setHours(0, 0, 0, 0);
                    end.setDate(0);
                    end.setHours(0, 0, 0, 0);
                } else { //if (report.range == "Hour") {    // часовой диапазон отчёта
                    start.setHours(start.getHours() - contractHour - 1, 0, 0, 0);
                    start.setHours(contractHour);
                    end.setHours(end.getHours() - 1, 0, 0, 0);
                }
            }
        }

        report.start = start;
        report.end = end;
        report.selectMonth = start.getMonth().toString();
        report.selectYear = start.getFullYear().toString();
        report.seDate = model.seDates[model.seDates.length - 1];
        for (var i = 0; i < model.seDates.length; i++) {
            if (model.seDates[i].range == report.range) {
                report.seDate = model.seDates[i];
            }
        }

        report.seSelect(report.seDate);

        report.startChanged = function () {
            var se = report.seDate;
            if (report.range == "Month") {
                var start = new Date();
                start.setDate(1);
                start.setMonth(report.selectMonth);
                start.setFullYear(report.selectYear);
                start.setHours(0, 0, 0, 0);
                report.start = start;
                var end = new Date();
                end.setDate(1);
                end.setMonth(report.selectMonth);
                end.setFullYear(report.selectYear);
                end.setHours(0, 0, 0, 0);
                end.setMonth(end.getMonth() + 1);
                report.end = end;
            }
            var range = { start: report.start, end: report.end };
            $settings.setReportlistRange(report.range, range);
            //if(se.name == "custom") {                
            //    var range = { start: report.start, end: report.end };
            //    $settings.setReportlistRange(report.range, range);
            //} else if (se.update) {
            //    se.update(report, report.start);                  // обновление end
            //}
        }

        report.endChanged = function () {
            var se = report.seDate;
            if (report.range == "Month") {
                var start = new Date();
                start.setDate(1);
                start.setMonth(report.selectMonth);
                start.setFullYear(report.selectYear);
                start.setHours(0, 0, 0, 0);
                report.start = start;
                var end = new Date();
                end.setDate(1);
                end.setMonth(report.selectMonth);
                end.setFullYear(report.selectYear);
                end.setHours(0, 0, 0, 0);
                end.setMonth(end.getMonth() + 1);
                report.end = end;
            }
            var range = { start: report.start, end: report.end };
            $settings.setReportlistRange(report.range, range);
            //if (se.name == "custom") {
            //    var range = { start: report.start, end: report.end };
            //    $settings.setReportlistRange(report.range, range);
            //}
        }

        report.ids = filterReportIds(report, rows);
        //
        report.selectable = ((report.target == "Single") || (report.ids.length > 0));

        report.isItHidden = function () {
            return (report.isHidden && !model.showAll);
        }

        //
        report.updateTimeout = null;

        report.updateWithTimeout = function () {
            if (model.data) {
                //report.state = "wait";
                $timeout.cancel(report.updateTimeout);
                report.updateTimeout = $timeout(report.update, 750);
            }
        }
        //
        
        report.update = function () {
            if (model.data) {
                if (report.range == "Month") {
                    var start = new Date();
                    start.setDate(1);
                    start.setMonth(report.selectMonth);
                    start.setFullYear(report.selectYear);
                    start.setHours(0, 0, 0, 0);
                    report.start = start;
                    var end = new Date();
                    end.setDate(1);
                    end.setMonth(report.selectMonth);
                    end.setFullYear(report.selectYear);
                    end.setHours(0, 0, 0, 0);
                    end.setMonth(end.getMonth() + 1);
                    report.end = end;
                }
                report.state = "wait";
                //$log.debug("даты отчета %s %s", start.toString(), end.toString());
                $reports.build(report.id, report.start, report.end, report.ids).then(function (r) {
                    reportStart = report.start;
                    reportId = report.id;
                    reportEnd = report.end;
                    reportIds = report.ids;
                    report.wait = false;
                    //$log.debug("строим отчет report=%s", $filter('json')(r));
                    if (!r.report || !r.options.success) {
                        report.state = "error";
                        report.error = r.options.errorText;
                        report.success = false;
                    } else {
                        report.warningText = r.options.warningText;
                        report.state = "success";
                        report.error = "";
                        report.success = true;
                        report.reportAsHtml = $sce.trustAsHtml(r.report);
                        report.reportAsText = r.report;
                    }
                    $reports.setDates(report.range, { start: report.start, end: report.end });
                }, function (err) {
                    report.state = "error";
                    report.error = "Отчет не построен: " + (err || "неизвестная ошибка");
                    report.success = false;
                    report.wait = false;
                    $log.error("отчет не построен: %s", err);
                });
                //}
            }
        }

        return report;
    }

    model.savePdf = function (reportAsText, isOrientationAlbum) {
        if (reportAsText) {
            $reports.exportToPdf(reportAsText.toString(), isOrientationAlbum === true);
        }
    };

    model.toExcel = function (reportAsText) {
        if (reportAsText) {
            $reports.exportToXls(reportAsText.toString());
        }
    };
    model.sendMailToPdf = function (reportAsText, isOrientationAlbum) {
        if (reportAsText) {
            $reports.sendMailToPdf(reportId, reportStart, reportEnd, reportIds, reportAsText.toString(), isOrientationAlbum === true);
        }
    };
    $scope.$watch('model.objs', function () {
        model.idleCounter = 0;
        model.waitCounter = 0;
        model.doneCounter = 0;
        model.errorCounter = 0;
        if (!model.objs) return;

        for (var i = 0; i < model.objs.length; i++) {
            var report = model.objs[i];
            report.done = false;
            switch (report.state) {
                case "idle":
                    model.idleCounter++;
                    break;
                case "wait":
                    model.waitCounter++;
                    break;
                case "success":
                    report.done = true;
                    model.doneCounter++;
                    break;
                default:
                    model.errorCounter++;
                    break;
            }
        }

        if (model.errorCounter > 0) {
            model.state = "error";
        } else if (model.waitCounter > 0) {
            model.state = "wait";
        } else if (model.doneCounter > 0) {
            model.state = "success";
        } else {
            model.state = "idle";
        }
    }, true);


    var init = function () {
        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objs;
        ////
        $reports.all().then(function (data) {
            model.objs = [];
            for (var i = 0; i < data.reports.length; i++) {
                var report = data.reports[i];
                model.objs.push(wrap(report));
            }
            model.sorted = $filter('orderBy')(model.objs, 'name');

            if ($filter('filter')(model.sorted, { isHidden: 'true' }).length > 0) {
                model.showAll = false;
            }

            //select
            if (model.objs.length == 0) {
                //none
            } else if (model.objs.length == 1) {
                model.select(model.objs[0].id, true);
            } else if (model.selectedId) {
                //restore selected
                if (!model.select(model.selectedId, true)) {
                    delete model.only1;
                }
            } else {
                var sel;
                for (var i = 0; i < model.sorted.length; i++) {
                    var r = model.sorted[i];
                    if (r.selectable && !r.isItHidden()) {
                        model.select(r.id);
                        break;
                    }
                }
            }
        });
    };

    model.select = function (reportId, update) {

        if (!model.objs || model.objs.length == 0) return;

        for (var i = 0; i < model.objs.length; i++) {
            var d = model.objs[i];
            if (d.id == reportId) break;
        }

        if (i == model.objs.length) return;

        var report = model.objs[i];

        if (!report.selectable) return;

        model.selected = report;

        if (update && report.update && report.state == "wait") {
            report.update();
        }

        return report;
    };

    if (data && data.reportId) {
        model.selectedId = data.reportId;
        model.only1 = true;
    }

    //init();


    if (data && data.ids) {
        $list.getRowsCache(data.ids).then(function (crows) {
            rows = crows;
            for (var i = 0; i < rows.length; i++) {
                var crow = crows[i];
                var name = (crow.name || "") + (crow.name && crow.pname ? ": " : "") + (crow.pname || "");
                names.push(name || crow.id);
            }
        }).finally(function () {
            init();
        });
    } else {
        init();
    }


    ////



    //

    function filterReportIds(r, rows) {
        if (r.target == "Single") {
            return rows;
        }

        var selected = [];
        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];                
            if (r.target) {
                switch (r.target) {
                    case "HouseRoot":
                        if (row.class == "HouseRoot") {
                            selected.push(row.id);
                        }
                        break;

                    case "Resource":
                        if (row.resource && r.resources && (r.resources != "") && (r.resources.indexOf(row.resource) != -1)) {
                            selected.push(row.id);
                        }
                        break;

                    case "Device":
                        if (row.deviceId && r.devices && (r.devices != "") && (r.devices.indexOf(row.deviceId) != -1)) {
                            selected.push(row.id);
                        }
                        break;

                    case "Common":
                    case "Single":
                    default:
                        selected.push(row.id);
                        break;
                }
            } else {
                selected.push(row.id);
            }
        }
        return selected;
    }


    //modal

    model.modalOpen = function (newData) {
        
        if (newData && newData.reportId) {
            model.only1 = false;
            model.select(newData.reportId, true);
        }

        model.modal = $modal.open({
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

    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    model.close = function () {
        model.modal.close();
        model.window.close();
    }

    model.window.open = model.modalOpen;

    model.modalOpen();

    //

    $scope.model = model;
});


app.directive('bindHtmlCompile', ['$compile', function ($compile) {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            scope.$watch(function () {
                return scope.$eval(attrs.bindHtmlCompile);
            }, function (value) {
                // Incase value is a TrustedValueHolderType, sometimes it
                // needs to be explicitly called into a string in order to
                // get the HTML string.
                element.html(value && value.toString());
                // If scope is provided use it, otherwise use parent scope
                var compileScope = scope;
                if (attrs.bindHtmlScope) {
                    compileScope = scope.$eval(attrs.bindHtmlScope);
                }
                $compile(element.contents())(compileScope);
            });
        }
    };
}]);


app.directive("compileHtml", function ($parse, $sce, $compile) {
    return {
        restrict: "A",
        link: function (scope, element, attributes) {

            var expression = $sce.parseAsHtml(attributes.compileHtml);

            var getResult = function () {
                return expression(scope);
            };

            scope.$watch(getResult, function (newValue) {
                var linker = $compile(newValue);
                element.append(linker(scope));
            });
        }
    }
});