angular.module("app")

.controller("MaquetteEditCtrl", function ($scope, $uibModal, $transport, $helper, maquetteSvc, $filter, $log, $list, $q, commonSvc, taskSvc) {

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

    model.cronconfig = { allowMultiple: true };

    model.chooseCron = function (cron) {
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/cron-select-modal.html",
            controller: "CronSelectCtrl",
            size: "md",
            resolve: {
                data: function () {
                    return { cron: cron, cronconfig: model.cronconfig };
                }
            }
        });
    }

    ////


    //Список параметров, используется при загруке и сохранении нода, а также для UNDO и определения, есть ли изменения
    var propertiesSimple = ["id", "type", "name", "Inn", "organization", "receiver", "lastNumber", "isHidden", "disable"];
    var propertiesArray = ["tubeIds", "taskIds"];
    var properties = ["id", "type", "name", "Inn", "organization", "receiver", "lastNumber", "isHidden", "tubeIds", "taskIds", "disable"];
    
    ////

    model.tasks = [];

    taskSvc.tasks().then(function (answer) {
        for (var i = 0; i < answer.tasks.length; i++) {
            var task = answer.tasks[i];
            model.tasks.push(task);
        }
    });

    ////

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
        if (!selected.Tube) selected.Tube = [];

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

        selected._deleteTube = function (id) {
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

        //task
        selected = commonSvc.wrapTaskChoose(selected, model.tasks);

        ////умолчания
        //
        selected.undo = {};
        $helper.copyToFrom(selected.undo, selected, properties);

        selected.selectable = true;

        selected.toggleHide = function () {
            selected.isHidden = !(selected.isHidden == true);
        }

        selected.reload = function () {
            $helper.copyToFrom(selected, selected.undo, properties);
        }

        selected.edited = function () {
            return selected.isNew || !$helper.areEqual(selected, selected.undo, properties);
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
            if(selected.isHidden == true) {
                selected.disable = true;
            }
            $helper.copyToFrom(selected.toSave, selected, propertiesSimple);
            rules.push({ action: selected.isNew ? "add" : "upd", target: "node", content: { id: selected.id, type: "Maquette", body: selected.toSave } });
            delete selected.toSave;
            //tubes
            {
                var del = $helper.arrayDiff(selected.undo.tubeIds, selected.tubeIds);
                for (var i = 0; i < del.length; i++) {
                    rules.push({ action: "del", target: "relation", content: { start: selected.id, end: del[i], type: "maquette", body: {} } });
                }
                var add = $helper.arrayDiff(selected.tubeIds, selected.undo.tubeIds);
                for (var i = 0; i < add.length; i++) {
                    rules.push({ action: "add", target: "relation", content: { start: selected.id, end: add[i], type: "maquette", body: {} } });
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
            var m = model.objs[i];
            if (m.edited()) {//изменен текст
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
        if (model.state == "save") return;

        model.state = "init";

        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objs;
        //
        model.enable1 = false;

        maquetteSvc.all()
            .then(function (answer) {
                var proms = [$q.when(model.newId)];
                for (var i = 0; i < answer.body.maquettes.length; i++) {
                    var m = answer.body.maquettes[i];
                    proms.push(maquetteSvc.get(m.id));
                }
                return $q.all(proms);
            })
            .then(function (answers) {
                model.objs = [];
                var newId = answers[0];
                var tubeIds = {};

                for (var i = 1; i < answers.length; i++) {
                    var m = answers[i].body.maquette;
                    if (!m.Tube) continue;

                    for (var j = 0; j < m.Tube.length; j++) {
                        tubeIds[m.Tube[j].id] = 1;
                    }
                }

                var ids = $helper.assocToArray(tubeIds);
                var prom = ids.length > 0 ? $list.getRows(ids) : $q.when(null);

                prom.then(function (message) {
                    return (message == null? [] : message);
                }, function (error) {
                    return [];
                })
                .then(function(rows) {
                    for (var i = 1; i < answers.length; i++) {
                        var m = answers[i].body.maquette;
                        if (newId && m.id == newId) newId = null;//проверка на существование
                        model.objs.push(wrap(m, rows));
                    }

                    if (newId) {
                        var newM = { id: newId, name: "Новый макет", isHidden: false, type: "Maquette", isNew: true };
                        model.objs.push(wrap(newM, rows));
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

    model.select = function (m) {
        if (m.selectable) {
            model.selected = m;
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
                        $log.debug("ошибка при сохранении рассылки: " + err);
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