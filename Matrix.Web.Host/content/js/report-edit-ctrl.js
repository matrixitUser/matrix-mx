angular.module("app")
.controller("ReportEditCtrl", function ($scope, $transport, $reports, $modal, $log, $helper, $filter, $drivers, $q, commonSvc, metaSvc) {

    var data = $scope.$parent.window.data;

    var model = {
        window: $scope.$parent.window,
        editedCounter: 0,
        only1: false
    };

    model.ranges = [
        { text: "Часовой", value: "Hour" },
        { text: "Суточный", value: "Day" },
        { text: "Месячный", value: "Month" }
    ];

    model.targets = [
        { text: "Универсальный", value: "Common" },
        { text: "Одиночный", value: "Single" },
        { text: "Ресурсы", value: "Resource" },
        { text: "Вычислители", value: "Device" },
        { text: "ОДН", value: "HouseRoot" }
    ];
    //

    model.devices = [];

    $drivers.all().then(function (answer) {
        var n = answer.body.drivers;
        for (var i = 0; i < n.length; i++) {
            var driver = n[i];
            var device = { id: driver.id, name: driver.name };
            model.devices.push(device);
        }
    });

    model.resources = metaSvc.resources;


    //Список параметров, используется при загруке и сохранении нода, а также для UNDO и определения, есть ли изменения
    var propertiesSimple = ["id", "type", "name", "template", "range", "target", "isHidden", "isOrientationAlbum", "devices", "resources"];
    var propertiesArray = [];
    var properties = ["id", "type", "name", "template", "range", "target", "isHidden", "isOrientationAlbum", "devices", "resources"];

    //model.autoclose = function () {
    //    return (model.editedCounter == 0);
    //}

    model.newId = null;

    if (data && data.reportId) {
        model.selectedId = data.reportId;
        model.only1 = true;
    } else if (data && data.isNew) {
        //создание нового отчёта
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

    var wrap = function (selected) {

        selected._resources = {};
        selected._devices = {};

        //unpack resources
        if (selected.resources) {
            var resources = selected.resources.split(";");
            for (var i = 0; i < resources.length; i++) {
                var resource = resources[i];
                selected._resources[resource] = true;
            }
        }
        
        //unpack resources
        if (selected.devices) {
            var devices = selected.devices.split(";");
            for (var i = 0; i < devices.length; i++) {
                var device = devices[i];
                selected._devices[device] = true;
            }
        }



        //было: {id, name, template}
        //стало: {id, name, template, edit: {name, template}}

        if (!selected.type) selected.type = "Report";

        //devices
        //selected = commonSvc.wrapDeviceChoose(selected, model.devices);


        selected._resourcesRecalc = function () {
            var result = "";
            for (var resource in selected._resources) {
                if (resource && selected._resources.hasOwnProperty(resource) && selected._resources[resource]) {
                    if (result != "") result += ";";
                    result += resource;
                }
            }
            selected.resources = result;
        }

        selected._devicesRecalc = function () {
            var result = "";
            for (var device in selected._devices) {
                if (device && selected._devices.hasOwnProperty(device) && selected._devices[device]) {
                    if (result != "") result += ";";
                    result += device;
                }
            }
            selected.devices = result;
        }

        //

        selected.undo = {};
        $helper.copyToFrom(selected.undo, selected, properties);

        selected.selectable = true;
        //
        selected.showRange = function () {
            var selected = $filter('filter')(model.ranges, { value: model.selected.range });
            return (model.selected.range && selected.length) ? selected[0].text : 'н/д';
        };
        selected.showTarget = function () {
            var sel = $filter('filter')(model.targets, { value: model.selected.target });
            return (model.selected.target && sel.length) ? sel[0].text : 'н/д';
        };
        
        selected.reload = function () {
            $helper.copyToFrom(selected, selected.undo, properties);
        }

        selected.edited = function () {
            return selected.isNew || !$helper.areEqual(selected, selected.undo, properties);
        }

        selected.save = function () {
            var rules = [];
            selected.toSave = {};
            $helper.copyToFrom(selected.toSave, selected, propertiesSimple);
            
            rules.push({ action: selected.isNew ? "add" : "upd", target: "node", content: { id: selected.id, type: "Report", body: selected.toSave } });
            delete selected.toSave;
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

    var unwrap = function (report) {
        var newReport = {};
        $helper.copyToFrom(newReport, report, properties);
        return newReport;
    }
    
    //

    model.editedCount = function () {
        var count = 0;
        if (!model.objs) return count;
        for (var i = 0; i < model.objs.length; i++) {
            var m = model.objs[i];
            if (m.edited()) {//изменен текст
                count++;
            }
        }
        return count;
    }

    model.aceLoaded = function (e) {
        e.$blockScrolling = Infinity;
    }

    model.aceChanged = function (e) {

    }

    ////

    function init() {
        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objs;
        //
        model.enable1 = false;
        ////
        $q.all([$q.when(model.newId), $reports.all()]).then(function (datas) {
            model.objs = [];
            var newId = datas[0];
            var data = datas[1];

            for (var i = 0; i < data.reports.length; i++) {
                var report = data.reports[i];
                if (newId && report.id == newId) newId = null;//проверка на существование
                model.objs.push(wrap(report));
            }

            if (newId) {
                var newReport = { id: newId, name: "", template: "", type: "Report", isNew: true };
                model.objs.push(wrap(newReport));
                model.selectedId = newId;
            }

            model.sorted = $filter('orderBy')(model.objs, 'name');

            //select
            if (model.objs.length == 0) {
                //none
            } else if (model.objs.length == 1) {
                model.selected = model.objs[0];
            } else if (model.selectedId) { //restore selected
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


    ///

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
        if (model.state == "save") return;
        model.state = "init";
        init();
    }


    model.saveDoneState = function () {
        if (model.state != "save") return;
        model.state = "saveDone";
        init();
    }


    ////

    model.addState = function () {
        if (model.state != "init") return;
        model.state == "add";
        //создание нового 
        model.newId = $helper.createGuid(1).then(function (message) {
            var guids = message.body.guids;
            if ($helper.isArray(guids) && guids.length > 0) {
                return guids[0];
            }
            return null;
        });

        model.newId.then(function (message) {
            if (message != null) {
                model.initState();
            }
        });
    }

    model.select = function (m) {
        if (m.selectable) {
            model.selected = m;
        }
    };
    
    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    model.initState();


    /*
    model.save = function () {
        if (!model.objs) return;
        var reports = [];
        for (var i = 0; i < model.objs.length; i++) {
            var report = model.objs[i];
            if (report.edited) {//изменен
                reports.push(unwrap(report));
            }
        }
        if (reports.length > 0) {
            $reports.save(reports).then(function () {
                model.objs.length = 0;
                model.newId = null;
                model.initState();
            }).catch(function (err) {
                $log.debug("ошибка при сохранении отчетов: " + err);
            });
        }
    }*/

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
                model.saveDoneState();
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
                        $log.debug("ошибка при сохранении отчёта: " + err);
                        saveProcessDone(1);
                    });
            } else {
                saveProcessDone(0);
            }
        }
    }


    //modal

    model.modalOpen = function () {
        model.modal = $modal.open({
            templateUrl: model.window.modalTemplateUrl,
            windowTemplateUrl: model.window.windowTemplateUrl,
            size: 'lg',
            scope: $scope
        })

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

    model.close = function () {
        model.modal.close();
        model.window.close();
    }

    model.window.open = model.modalOpen;

    model.modalOpen();

    $scope.model = model;
});