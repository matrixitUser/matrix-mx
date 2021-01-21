function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}


angular.module("app")
.controller("HouseEditorCtrl", function ($scope, $rootScope, $log, $uibModalInstance, data, $transport, $drivers, $filter, $q, metaSvc, $uibModal, $list, $helper) {

    var folderId;
    var id;
    var isNew = false;

    //var data = $scope.$parent.window.data;
    if (data === undefined) {
        isNew = true;
        id = null;
        folderId = null;
    } else {
        isNew = (data.id === undefined);
        id = data.id;
        folderId = data.folderId;
    }

    function parseDate(d) { //dd.MM.yyyy HH:mm
        var dt = (d && (typeof d == 'string' || d instanceof String)) ? d.split(' ') : [""];
        var date = dt[0].split('.');
        if (date.length === 3) {
            return new Date(date[2], (date[1] - 1), date[0]);
        } else {
            return new Date();
        }
    }

    function getDate(d) { //yyyy-MM-ddTHH....
        var date = new Date(d);
        return ('0' + date.getDate()).slice(-2) + '.' + ('0' + (date.getMonth() + 1)).slice(-2) + '.' + date.getFullYear();
    }


    $scope.getLocation = function (val) {
        if (val == "") return $q.when([""]);

        return $transport.send(new Message({ what: "edit-get-fias" }, { searchText: val }))
            .then(function (response) {
                return response.body.results.map(function (item) {
                    return item.value;
                });
            });
    };

    $scope.fillAddrFias = function()
    {
        if (model1.config != 'teplocom')
        {
            model1.area.addr = (!!model1.area.city ? model1.area.city + ", " : "") + (!!model1.area.street ? model1.area.street + ", " : "") + (!!model1.area.house ? model1.area.house : "");
        }
        else
        {
            model1.area.addr = (!!model1.area.address ? model1.area.address : "");
        }
        //model1.area.addr = $scope.getLocation(model1.area.addr)[0];
    }


    var model1 = {
        config: metaSvc.config,
        tubeIds0: [],
        tubeIds: [],
        Tube: [],
        _deletingTube: false
    };

    var loadTubes = function (ids) {
        var prom = ids.length > 0 ? $list.getRows(ids) : $q.when(null);
        return prom.then(function (message) {
            return (message == null ? [] : message);
        }, function (error) {
            return [];
        })
        .then(function (rows) {
            var newIds = [];
            for (var i = 0; i < ids.length; i++) {
                var id = ids[i];

                for (var j = 0; j < model1.tubeIds.length; j++) {
                    var existId = model1.tubeIds[j];
                    if (id == existId) break;
                }

                if (j != model1.tubeIds.length) continue; //повтор

                newIds.push(id);
                model1.tubeIds.push(id);
                for (var j = 0; j < rows.length; j++) {
                    if (rows[j].id == id) {
                        model1.Tube.push(rows[j]);
                        break;
                    }
                }
            }
            return newIds;
        });
    }

    //загрузка модели
    $transport.send(new Message({ what: "edit-get-row" }, { isNew: isNew, id: id, isHouse: true })).then(function (message) {

        var tubeIds = [];
        var Tube = message.body.Tube;
        if (!Tube) Tube = [];

        for (var i = 0; i < Tube.length; i++) {
            var id = Tube[i].id;
            tubeIds.push(id);
            model1.tubeIds0.push(id);
        }

        loadTubes(tubeIds).then(function (newIds) {
            $scope.model1.tube = message.body.tube;
            $scope.trashButton = $scope.model1.tube.isDeleted ? "Восстановить" : "Удалить";

            if (!$scope.model1.tube.disabledHistory) {
                $scope.model1.tube.disabledHistory = "[]";
            }
            $scope.model1.tube._disabledHistory = JSON.parse($scope.model1.tube.disabledHistory);

            $scope.model1.area = message.body.area;
            $scope.isLoaded = true;

            id = $scope.model1.tube.id;
            if (isNew) {
                $scope.model1.area.__viewMode = true;
                $scope.model1.area.__editMode = true;
                rules.push(new SaveRule("add", "relation", { start: $scope.model1.area.id, end: $scope.model1.tube.id, type: "contains", body: {} }));
                if (folderId && (folderId !== "all")) {
                    rules.push(new SaveRule("add", "relation", { start: folderId, end: $scope.model1.area.id, type: "contains", body: {} }));
                }
            }
        });

    });
           
    model1.resources = metaSvc.resources;

    $scope.model1 = model1;

    model1.editHouse = function (parameters, isEditable) {
        if (isEditable === undefined) isEditable = true;
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/house-edit-parameters-modal.html",
            controller: "HouseEditParametersCtrl",
            size: "lg",
            resolve: {
                data: function () {
                    return { parameters: parameters, isEditable: isEditable };
                }
            }
        });

        modalInstance.result.then(function (parameters) {
            model1.tube["apts"] = parameters;
        });
    }

    var rules = [];

    model1.chooseTubes = function () {
        var modalInstance = $uibModal.open({
            animation: true,
            templateUrl: "tpls/list-select-modal.html",
            controller: "ListSelectCtrl",
            size: "md",
            resolve: {
                data: function () {
                    var tubeIds = [];
                    for (var i = 0; i < model1.tubeIds.length; i++) {
                        var id = model1.tubeIds[i];
                        tubeIds.push(id);
                    }
                    return tubeIds;
                }
            }
        });

        modalInstance.result.then(loadTubes);
    }

    model1._deleteTube = function (id) {
        if (id == "all") {
            model1.tubeIds = [];
            model1.Tube.length = 0;
        } else {
            //delete from tubeIds
            for (var i = model1.tubeIds.length; i > 0; i--) {
                if (model1.tubeIds[i - 1] == id) {
                    model1.tubeIds.splice(i - 1, 1);
                }
            }

            //delete from Tube
            for (var i = model1.Tube.length; i > 0; i--) {
                if (model1.Tube[i - 1].id == id) {
                    model1.Tube.splice(i - 1, 1);
                }
            }
        }

        //model1._deletingTube = false;
    }

    //загружена|не загружена модель
    $scope.isLoaded = false;
    
    function removePropertyTemp(obj) {
        for (var key in obj) {
            if (key !== undefined && obj.hasOwnProperty(key)) {
                if (key.startsWith("__")) {
                    delete obj[key];
                }
            }
        }
    }

    $scope.save = function () {
        $scope.isLoaded = false;
        $scope.model1.tube.disabledHistory = JSON.stringify($scope.model1.tube._disabledHistory);
        $scope.model1.tube.class = "HouseRoot";

        removePropertyTemp($scope.model1.area);

        rules.push(new SaveRule(isNew ? "add" : "upd", "node", { id: $scope.model1.area.id, type: "Area", body: $scope.model1.area }));
        rules.push(new SaveRule(isNew ? "add" : "upd", "node", { id: $scope.model1.tube.id, type: "Tube", body: $scope.model1.tube }));
        
        var x = rules;

        for (var i = 0; i < rules.length; i++) {
            var rule = rules[i];
            var body = rule.content.body;
            for (var prop in body) {
                if (!body.hasOwnProperty(prop)) continue;
                if (prop.indexOf("_") === 0) {
                    delete body[prop];
                }
            }
        }
        
        var del = $helper.arrayDiff(model1.tubeIds0, model1.tubeIds);
        for (var i = 0; i < del.length; i++) {
            rules.push({ action: "del", target: "relation", content: { start: $scope.model1.tube.id, end: del[i], type: "reference", body: {} } });
        }
        var add = $helper.arrayDiff(model1.tubeIds, model1.tubeIds0);
        for (var i = 0; i < add.length; i++) {
            rules.push({ action: "add", target: "relation", content: { start: $scope.model1.tube.id, end: add[i], type: "reference", body: {} } });
        }

        $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function (message) {
            model.close(message)
        });
    };
    
    var model = {
        modal: undefined
    };

    model.delete = function () {
        $scope.isLoaded = true;

        $scope.model1.tube.isDeleted = !$scope.model1.tube.isDeleted;
        var delRules = [
            new SaveRule("upd", "node", { id: $scope.model1.tube.id, type: "Tube", body: $scope.model1.tube })
        ];

        $transport.send(new Message({ what: "edit" }, { rules: delRules })).then(function (message) {
            model.close(message)
        });
    };

    model.clearCache = function () {
        $transport.send(new Message({ what: "cache-clear" }, { id: id }));
    }
    
    model.close = function (message) {
        $uibModalInstance.close(message ? isNew : undefined);
    };

    $scope.model = model;
});
