'use strict';

angular.module("app");

app.controller("ActionsCtrl", function ActionsCtrl($log, $rootScope, $scope, $window, $helper, $filter, $actions, $reports, $list, $parse, $timeout, $drivers, $listFilter) {

    var menuActions = [
        { name: 'log-show', getParam: arrayOfSels, isEnabled: isSels },
        { name: 'control-show', getParam: arrayOfSels, isEnabled: isSels },
        { name: 'maps-show', getParam: arrayOfSels, isEnabled: isMaps },
        { name: 'rowProperties-show', getParam: arrayOfSels, isEnabled: isSels },
        null,
        { name: 'poll-ping', title: 'Проверить доступность', getParam: arrayOfSels, isEnabled: isSels },
        { name: 'poll-all', title: 'Начать опрос', getParam: arrayOfSels, getArg: function () { return { start: undefined, end: undefined, components: "Current:3;Hour:2:60;Day:2:60;" }; }, isEnabled: isSels },
        { name: 'poll-cancel', title: 'Остановить опрос', getParam: arrayOfSels, isEnabled: isSels },
        null,
        { name: 'data-table', getParam: arrayOfSels },
        null,
        { name: 'report-list', getParam: reportArrayOfSels },
        null,
    ];

    var contextActions = [
        { name: 'report-edit', getParam: arrayOfSels, isEnabled: isSels }
    ];

    var model = {
        actionsAct: [],
        actionsReport: [],
        actions: [],
        reports: [],
        devices: []        
    };

    $drivers.all().then(function (data) {
        
        model.devices = [];
        for (var i = 0; i < data.body.drivers.length; i++) {
            var driver = data.body.drivers[i];
            var device = { id: driver.id, name: driver.name, reference: driver.reference };
            model.devices.push(device);
        }

    });


    $scope.$watch('model.actions', function (newval, oldval) {
        model.actionsView = [];
        if ($helper.isArray(model.actions) && model.actions.length > 0) {
            var isAlmost1Action = false;
            var isDivider = false;
            var sort = $filter('orderBy')(model.actions, 'index');
            for (var i = 0; i < sort.length; i++) {
                var action = sort[i];
                if (action.type == "divider") {
                    isDivider = true;
                } else {
                    if (isAlmost1Action && isDivider) {
                        isDivider = false;
                        model.actionsView.push({ divider: i });
                    }
                    model.actionsView.push(action);
                    isAlmost1Action = true;
                }

            }
        }
    }, true);


    function arrayOfSels() {
        return $list.getSelectedIds();
    }


    function reportArrayOfSels() {
        return { ids: $list.getSelectedIds() };
    }

    function isMaps() {
        return $list.getIsMapsShow();
    }

    function isSels() {
        return $list.getSelectedIds().length > 0;
    }


    model.load = function () {

        model.actionsAct = [];
        model.actions = [];
        model.actionsReport = []; 
        
        for (var i = 0; i < menuActions.length; i++) {
            var menuAction = menuActions[i];

            if (menuAction === null) {
                model.actionsAct.push({ type: "divider", index: i, divider: i });
                continue;
            }

            (function (i) {
                var ma = menuActions[i];
                $actions.get(ma.name).then(function (action) {
                    if (action == null) return;

                    var getParam = ma.getParam;
                    var getArg = ma.getArg;
                    var isEnabled = ma.isEnabled;

                    var actGetParamWArg = function (a, gp, ga) {
                        return function ($item) {
                            a.act(gp === undefined ? gp : gp(), ga === undefined ? ga : ga());
                        }
                    }

                    var title = ma.title || action.header;

                    model.actionsAct.push({
                        index: i,
                        type: "action",
                        filterTags: ["action", "действие"],
                        icon: action.icon,
                        title: title,
                        action: actGetParamWArg(action, getParam, getArg),
                        checkEnabled: isEnabled,
                        enabled: isEnabled ? isEnabled() : true
                    });
                })
            })(i);

        }
        //$q.all([$actions.get("report-list"), $reports.all()]).then(function(arr)){var action = arr[0]; var data = arr[1];}
        $actions.get("report-list").then(function (action) {
            $reports.all().then(function (data) {
                var sorted = $filter('orderBy')(data.reports, "name");

                for (var k = 0; k < sorted.length; k++) {

                    var report = sorted[k];

                    if (report.isHidden) continue;

                    report.icon = report.icon || "./img/report.png";

                    var isReportEnabled = ((function (r) {
                        return function () {
                            if (r.target && r.target != "Common") {
                                if (r.target == "Single") {
                                    return true;
                                }

                                var sels = $list.getSelectedRows();

                                for (var i = 0; i < sels.length; i++) {
                                    var sel = sels[i];

                                    switch (r.target) {
                                        case "HouseRoot":
                                            if (sel.class == "HouseRoot") return true;
                                            break;

                                        case "Resource":
                                            if (sel.resource && r.resources && (r.resources != "") && (r.resources.indexOf(sel.resource) != -1)) return true;
                                            break;

                                        case "Device":
                                            if (sel.deviceId && r.devices && (r.devices != "") && (r.devices.indexOf(sel.deviceId) != -1)) return true;
                                            break;
                                    }
                                }
                                return false;
                            }
                            return isSels();
                        }
                    })(report));

                    model.actionsReport.push({
                        index: i + k,
                        type: "report",
                        report: report,
                        filterTags: ["report", "отчет"],
                        icon: report.icon,
                        title: report.name,
                        action: (function (r) {
                            return function () {
                                action.act({ reportId: r.id, ids: arrayOfSels() })
                            }
                        })(report),
                        checkEnabled: isReportEnabled,
                        enabled: isReportEnabled ? isReportEnabled() : true
                    });
                }
                model.refresh();
                model.reports = sorted;
            }, function (err) {
                $log.error("actions-ctrl ошибка при получении данных: ", err);
            });
        })

    }
    var onlyReport = function ($item) {
        return $parse('action.type')($item) == "report";
    }

    model.menu = (function () {

        var options = [];

        $actions.get("report-edit").then(function (action) {

            if (action) {
                var actReport = function (a, gp) {
                    return function ($item) {
                        var rid = $parse("action.report.id")($item);
                        a.act({ reportId: rid });
                    }
                }

                //var title = action.header;

                options.push({
                    icon: action.icon,
                    title: "Редактировать шаблон отчёта",
                    type: 'html',
                    action: actReport(action),
                    enabled: onlyReport
                });
            }

        });

        $actions.get("rights-edit").then(function (action) {

            if (action) {
                var actReport = function (a, gp) {
                    return function ($item) {
                        var rid = $parse("action.report.id")($item);
                        a.act(rid);
                    }
                }

                //var title = action.header;

                options.push({
                    icon: action.icon,
                    title: "Редактировать права",
                    type: 'html',
                    action: actReport(action),
                    enabled: onlyReport
                });
            }

        });
        
        return options;
        //}
    })();

    model.menuView = [];

    model.refresh = function () {
        var actions = [];
        model.actions = [];
        for (var i = 0; i < model.actionsAct.length; i++) {
            var a = model.actionsAct[i];
            if (a.checkEnabled) {
                a.enabled = !!a.checkEnabled();
            }
            model.actions.push(a);
        }
        for (var i = 0; i < model.actionsReport.length; i++) {
            var a = model.actionsReport[i];
            if (a.checkEnabled) {
                a.enabled = !!a.checkEnabled();
                if (a.enabled) {
                    model.actions.push(a);
                }
            }
        }
        
        //for (var i = 0; i < model.actions.length; i++) {
        //    var a = model.actions[i];
        //    if (a.checkEnabled) {
        //        a.enabled = !!a.checkEnabled();
        //        if (a.enabled) {
        //            actions.push(a);
        //        }
        //        actions1.push(a);
        //    }
        //}
        $timeout(function () { $scope.$apply(model.actions); }, 0);
    }

    /////
    var listener = $rootScope.$on("list:selection-changed", function (e, message) {
        model.refresh();
    });


    $scope.model = model;

    $scope.$on('$destroy', function () {
        $log.debug("уничтожается actions");
        listener();
    });

    model.load();
});
