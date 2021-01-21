var app = angular.module("app");

function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}

app.controller("FolderEditCtrl", function ($uibModalInstance, $uibModal, $scope, data, $users, $log, $parse, $transport, $helper, $folders, $q, commonSvc, taskSvc) {

    var model = {
        data: data,
        roots: [],
        selected: undefined,
        //
        overlayEnabled: false,
        overlayText: "",
        overlay: $helper.overlayFunc
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

    //Список параметров, используется при загруке и сохранении нода, а также для UNDO и определения, есть ли изменения
    var propertiesSimple = ["id", "type", "name"];
    var propertiesArray = ["tubeIds", "taskIds"];
    var properties = ["id", "type", "name", "tubeIds", "taskIds"];
    

    var id = model.data.id;
    var isNew = model.data.isNew;
    //
    if (id == "all") id = null;
    //


    model.tasks = [];

    taskSvc.tasks().then(function (answer) {
        for (var i = 0; i < answer.tasks.length; i++) {
            var task = answer.tasks[i];
            model.tasks.push(task);
        }
    });


    var wrap = function(selected)
    {
        selected.deleteEnable = false;

        //task
        selected = commonSvc.wrapTaskChoose(selected, model.tasks);

        //
        selected.undo = {};
        $helper.copyToFrom(selected.undo, selected, properties);

        selected.selectable = true;

        selected.reload = function () {
            $helper.copyToFrom(selected, selected.undo, properties);
        }

        selected.edited = function () {
            return isNew || !$helper.areEqual(selected, selected.undo, properties);
        }

        selected.save = function () {
            var rules = [];
            selected.toSave = {};
            $helper.copyToFrom(selected.toSave, selected, propertiesSimple);

            //
            if (isNew) {
                rules.push(new SaveRule("add", "node", { id: selected.id, type: "Folder", body: selected.toSave }));
                if (selected.root) {
                    rules.push(new SaveRule("add", "relation", { start: selected.root.id, end: selected.id, type: "contains", body: {} }));
                }
            } else {
                rules.push(new SaveRule("upd", "node", { id: selected.id, type: "Folder", body: selected.toSave }));
            }
            //tasks
            rules = rules.concat(commonSvc.updateRelationsTask(selected));
            //

            return $transport.send(new Message({ what: "edit" }, { rules: rules }));
        }

        selected.delete = function () {
            var rules = [];

            //tasks
            rules = rules.concat(commonSvc.deleteRelationsTask(selected));

            if (selected.root) {
                rules.push(new SaveRule("del", "relation", { start: selected.root.id, end: selected.id, type: "contains", body: {} }));
            }
            rules.push(new SaveRule("del", "node", { id: selected.id, type: selected.type, body: model.selected }));

            return $transport.send(new Message({ what: "edit" }, { rules: rules }));
        }
        
        return selected;
    }
    
    model.update = function () {
        model.selected = undefined;
        
        //

        if (isNew) {
            model.overlay($transport.send(new Message({ what: "edit-get-folder" }, { isNew: true, id: id })).then(function (message) {
                model.selected = message.body.folderNew;
                model.selected.root = message.body.folder;
                model.selected = wrap(model.selected);
            })
            .catch(function (err) {

            }));

        } else {
            model.overlay(
                $transport.send(new Message({ what: "edit-get-folder" }, { isNew: false, id: id })).then(function (message) {
                    $transport.send(new Message({ what: "edit-get-folder-id" }, { id: id })).then(function (message1) {
                        model.selected = message.body.folder;
                        model.selected.root = message.body.parent;
                        model.selected.Task = message1.body.folder.Task;
                        model.selected = wrap(model.selected);
                        return $transport.send(new Message({ what: "rows-get-ids" }, { filter: { text: "", groups: [id] } }))
                    })
                    .then(function (message) {
                        if (message.head.what == "rows-get-ids") {
                            var ids = $parse('body.ids')(message) || [];
                            if (ids.length == 0) {
                                model.selected.deleteEnable = true;
                            }
                        }
                    });
                })
                .catch(function (err) {

                })
            );
        }
    }

    model.update();


    model.save = function () {
        model.overlay(model.selected.save().finally(function () { model.close(true); }), "Сохранение...");
    };

    model.delete = function () {
        model.overlay(model.selected.delete().finally(function () { model.close(true); }), "Удаление...");
    }

    model.close = function (result) {
        $uibModalInstance.close(result);
    }

    $scope.model = model;
});