angular.module("app")
.service("commonSvc", function ($uibModal, $log, $helper, $list) {
    var service = this;
    
    //Общие функции контроллеров(!), такие как Выбор точек учёта, тасков и т.д.; удаление точек учёта, тасков и т.д. и т.п.
    
    service.wrapTaskChoose = function (selected, tasks) {
        if (!selected.Task) selected.Task = [];
        selected._taskAddIds = [];

        selected._deleteTask = function (id) {
            if (id == undefined) {
                return;
            }

            //delete from taskIds
            for (var i = selected.taskIds.length; i > 0; i--) {
                if (selected.taskIds[i - 1] == id) {
                    selected.taskIds.splice(i - 1, 1);
                }
            }

            //delete from Task
            for (var i = selected.Task.length; i > 0; i--) {
                if (selected.Task[i - 1].id == id) {
                    selected.Task.splice(i - 1, 1);
                }
            }

            //delete send period
            selected._toggleTaskState("delete", tasks);
        }

        selected._taskState = "idle";
        selected._toggleTaskState = function (taskState, tasks) {
            if (selected._taskState == "idle") {
                //in
                switch (taskState) {
                    case "add":
                        break;
                    case "delete":
                        break;
                }
                selected._taskState = taskState;
            } else if (selected._taskState == taskState) {
                //out
                switch (selected._taskState) {
                    case "add":
                        for (var i = 0; i < selected._taskAddIds.length; i++) {
                            var id = selected._taskAddIds[i];

                            for (var j = 0; j < selected.taskIds.length; j++) {
                                var existId = selected.taskIds[j];
                                if (id == existId) break;
                            }

                            if (j != selected.taskIds.length) continue; //повтор

                            selected.taskIds.push(id);

                            var tk;

                            angular.forEach(tasks, function (s) {
                                if (s.id == id) {
                                    tk = s;
                                    selected.Task.push(s);
                                }
                            });

                            if (tk && tk.id) {

                            }
                        }
                        break;
                    case "delete":
                        break;
                }
                selected._taskState = "idle";
            }
        }

        selected.taskIds = [];

        for (var i = 0; i < selected.Task.length; i++) {
            var task = selected.Task[i];
            selected.taskIds.push(task.id);
        }
        return selected;
    }

    //


    service.wrapDeviceChoose = function (selected, devices) {
        if (!selected.Device) selected.Device = [];
        selected._deviceAddIds = [];

        selected._deleteDevice = function (id) {
            if (id == undefined) {
                return;
            }

            //delete from deviceIds
            for (var i = selected.deviceIds.length; i > 0; i--) {
                if (selected.deviceIds[i - 1] == id) {
                    selected.deviceIds.splice(i - 1, 1);
                }
            }

            //delete from Device
            for (var i = selected.Device.length; i > 0; i--) {
                if (selected.Device[i - 1].id == id) {
                    selected.Device.splice(i - 1, 1);
                }
            }

            //delete send period
            selected._toggleDeviceState("delete", devices);
        }

        selected._deviceState = "idle";
        selected._toggleDeviceState = function (deviceState, devices) {
            if (selected._deviceState == "idle") {
                //in
                switch (deviceState) {
                    case "add":
                        break;
                    case "delete":
                        break;
                }
                selected._deviceState = deviceState;
            } else if (selected._deviceState == deviceState) {
                //out
                switch (selected._deviceState) {
                    case "add":
                        for (var i = 0; i < selected._deviceAddIds.length; i++) {
                            var id = selected._deviceAddIds[i];

                            for (var j = 0; j < selected.deviceIds.length; j++) {
                                var existId = selected.deviceIds[j];
                                if (id == existId) break;
                            }

                            if (j != selected.deviceIds.length) continue; //повтор

                            selected.deviceIds.push(id);
                            
                            angular.forEach(devices, function (s) {
                                if (s.id == id) {
                                    selected.Device.push(s);
                                }
                            });
                        }
                        break;
                    case "delete":
                        break;
                }
                selected._deviceState = "idle";
            }
        }

        selected.deviceIds = [];

        for (var i = 0; i < selected.Device.length; i++) {
            var n = selected.Device[i];
            selected.deviceIds.push(n.id);
        }
        return selected;
    }

    //

    service.updateRelations = function (id, undoRelations, currentRelations, relationType) {
        var rules = [];
        var del = $helper.arrayDiff(undoRelations, currentRelations);
        for (var i = 0; i < del.length; i++) {
            rules.push({ action: "del", target: "relation", content: { start: id, end: del[i], type: relationType, body: {} } });
        }
        var add = $helper.arrayDiff(currentRelations, undoRelations);
        for (var i = 0; i < add.length; i++) {
            rules.push({ action: "add", target: "relation", content: { start: id, end: add[i], type: relationType, body: {} } });
        }
        return rules;
    }

    //

    service.updateRelationsTask = function (selected) {
        return service.updateRelations(selected.id, selected.undo.taskIds, selected.taskIds, "task");
    }

    service.deleteRelationsTask = function (selected) {
        return service.updateRelations(selected.id, selected.undo.taskIds, [], "task");
        //var del = selected.undo.taskIds;
        //for (var i = 0; i < del.length; i++) {
        //    rules.push({ action: "del", target: "relation", content: { start: selected.id, end: del[i], type: "task", body: {} } });
        //}
    }

    //

    service.updateRelationsTube = function (selected) {
        return service.updateRelations(selected.id, selected.undo.tubeIds, selected.tubeIds, "contains");
    }

    service.deleteRelationsTube = function (selected) {
        return service.updateRelations(selected.id, selected.undo.tubeIds, [], "contains");
        //var del = selected.undo.taskIds;
        //for (var i = 0; i < del.length; i++) {
        //    rules.push({ action: "del", target: "relation", content: { start: selected.id, end: del[i], type: "task", body: {} } });
        //}
    }

});
