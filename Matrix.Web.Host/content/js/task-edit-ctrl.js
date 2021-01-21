angular.module("app")

.controller("TaskEditCtrl", function ($scope, $uibModal, $transport, $helper, taskSvc, $timeout, $filter, $log, $list, $reports, $q, $actions) {

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

    //Список параметров, используется при загруке и сохранении нода, а также для UNDO и определения, есть ли изменения
    var propertiesSimple = ["id", "type", "cron", "name", "kind", "poll", "maquette", "mailer", "isHidden", "components", "onlyHoles", "hoursDaily"];
    var propertiesArray = [];
    var properties = ["id", "type", "cron", "name", "kind", "poll", "maquette", "mailer", "isHidden", "components", "onlyHoles", "hoursDaily"];

    //

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

    ////

    var wrap = function (selected) {

        //умолчания

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

        selected.save = function () {
            var rules = [];
            selected.toSave = {};
            if (selected.isHidden == true) {
                selected.kind = "disable";
            }
            $helper.copyToFrom(selected.toSave, selected, propertiesSimple);
            rules.push({ action: selected.isNew ? "add" : "upd", target: "node", content: { id: selected.id, type: "Task", body: selected.toSave } });
            delete selected.toSave;

            return $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function (message) {
                selected.undo = {};
                selected.isNew = false;
                $helper.copyToFrom(selected.undo, selected, properties);
                return message;
            });
        }

        selected.chooseCron = function () {
            var modalInstance = $uibModal.open({
                animation: true,
                templateUrl: "tpls/cron-select-modal.html",
                controller: "CronSelectCtrl",
                size: "md",
                resolve: {
                    data: function () {
                        return { cron: selected.cron, cronconfig: model.cronconfig, isEditable: true };
                    }
                }
            });

            modalInstance.result.then(function (cron) {
                selected.cron = cron;
            });
        }
        
        return selected;
    }

    //

    model.editedCount = function () {
        var count = 0;
        if (!model.objects) return count;
        for (var i = 0; i < model.objects.length; i++) {
            var m = model.objects[i];
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
        //if (model.state == "init") return;
        model.state = "init";

        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objects;
        //
        model.enable1 = false;

        taskSvc.all()
            .then(function (answer) {
                var proms = [$q.when(model.newId)];
                for (var i = 0; i < answer.body.objs.length; i++) {
                    var m = answer.body.objs[i];
                    proms.push(taskSvc.get(m.id));
                }
                return $q.all(proms);
            })
            .then(function (answers) {
                model.objects = [];
                var newId = answers[0]
                for (var i = 1; i < answers.length; i++) {
                    var m = answers[i].body.obj;
                    if (newId && m.id == newId) newId = null;//проверка на существование
                    model.objects.push(wrap(m));
                }

                if (newId) {
                    var nm = { id: newId, name: "Новое расписание", type: "Task", isNew: true };
                    model.objects.push(wrap(nm));
                }

                model.sorted = $filter('orderBy')(model.objects, 'name');


                //select
                if (model.objects.length == 0) {
                    //none
                } else if (model.objects.length == 1) {
                    model.selected = model.objects[0];
                } else if (model.selectedId) {//restore selected
                    for (var i = 0; i < model.objects.length; i++) {
                        var d = model.objects[i];
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
                        for (var i = 0; i < model.objects.length; i++) {
                            var r = model.objects[i];
                            if (r.id == sel.id) {
                                model.select(model.objects[i], false);
                                break;
                            }
                        }
                    }
                }

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

    model.select = function (m) {
        if (m.selectable) {
            model.selected = m;
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
        if (!model.objects) return;
        if (model.state != "edit") return;
        model.state = "save";
        model.saveCounterMax = model.objects.length;
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

        var objects = [];
        for (var i = 0; i < model.objects.length; i++) {
            var m = model.objects[i];
            if (m.edited()) {//изменен
                m.save()
                    .then(function () {
                        saveProcessDone(0);
                    })
                    .catch(function (err) {
                        $log.debug("ошибка при сохранении расписания: " + err);
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
