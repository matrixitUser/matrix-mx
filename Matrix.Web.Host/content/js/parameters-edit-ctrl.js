'use strict';

angular.module("app");

function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}

app.controller("ParametersEditCtrl", function ($uibModalInstance, $scope, data, $transport, $timeout, $helper, $parse, $list, $log, metaSvc, $q) {

    if (!data || !data.id) $uibModalInstance.close();

    var id, ids;
    if(angular.isArray(data.id)) {
        ids = data.id;
        id = data.id[0];
    } else {
        ids = [data.id];
        id = data.id;
    }
    
    var model = {
        parameters: [],
        IsToolPanelShow: false
    };

    model.loading = true;

    function parameterEditRenderer(params) {
        params.$scope.parameters = model.parameters;
        var html = '<select ng-options="item for item in parameters" ng-model="data.' + params.colDef.field + '" ></select>'; 
        return html;
    };

    function calcEditRenderer(params) {
        params.$scope.calcs = [{ name: "normal", caption: "нет" }, { name: "total", caption: "итого" }];
        var html = '<select ng-model="data.' + params.colDef.field + '" >' +
            '<option ng-repeat="item in calcs" value="{{item.name}}">{{item.caption}}</option>' +
            '</select>';
        return html;
    };

    var columnDefs = [        
        { headerName: "Параметр", field: "parameter", width: 150, cellRenderer: parameterEditRenderer },
        { headerName: "Расчет", field: "calc", width: 150, cellRenderer: calcEditRenderer, newValueHandler: calcEdited },
        { headerName: "Нач.значение", field: "init", width: 150, cellRenderer: initEditor, hide: true },
        { headerName: "Коэффициент", field: "k", width: 150, cellRenderer: kEditor, hide: true }
    ];

    function calcEdited(newValue) {
        $log.debug(newValue);
    };



    //todo определять теги по ресурсу
    model.tags = metaSvc.gazResource;

    model.opt = {
        columnDefs: columnDefs,
        rowData: model.tags,
        angularCompileRows: true,
        enableSorting: true,
        enableColResize: true,

        groupUseEntireRow: false,
        groupKeys: ["_dataTypeName"],
        groupAggFields: ["_dataTypeName"],
        groupColumnDef: {
            headerName: "Тип",
            field: "_dispName",
            width: 200,
            cellRenderer: {
                renderer: "group",
                footerValueGetter: '"Total (" + x + ")"',
                padding: 5
            }
        },

        ready: function (api) {
            var sort = [
                { field: 'name', sort: 'asc' }
            ];
            api.setSortModel(sort);
            api.sizeColumnsToFit()
        },

    };

    model.toggleToolPanel = function () {
        var api = model.opt.api;
        if (api) {
            model.IsToolPanelShow = !model.IsToolPanelShow;
            api.showToolPanel(model.IsToolPanelShow);
        }
    }

    model.save = function (name) {
        model.loading = true;
        model.description = "идет сохранение...";
        var tags = model.tags;

        $transport.send(new Message({ what: "helper-create-guid" }, { count: tags.length })).then(function (message) {
            var rules = [];

            var guidIndex = 0;

            for (var i = 0; i < tags.length; i++) {
                var tag = tags[i];

                if (tag.id === undefined) {
                    tag.id = message.body.guids[guidIndex++];

                    rules.push(new SaveRule("add", "relation", { start: id, end: tag.id, type: "tag", body: {} }));
                    rules.push(new SaveRule("add", "node", { id: tag.id, type: "Tag", body: tag }));
                } else {
                    rules.push(new SaveRule("upd", "node", { id: tag.id, type: "Tag", body: tag }));
                }
            }

            var bar = rules;

            $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function () {
                $transport.send(new Message({ what: "parameters-save-tags" }, { tubeId: id, tags: tags })).then(function (message) {
                    $log.debug("все");
                    model.loading = false;
                    model.close();
                });
            });
        });
    };

    model.close = function () {
        $uibModalInstance.close();
    };

    model.recalcData = function () {
        model.loading = true;
        model.description = "идет определение параметров...";
        $transport.send(new Message({ what: "parameters-recalc" }, { tubeId: id }))
            .finally(function () {
                reload();
            });
    };

    model.recalcDriver = function () {
        model.loading = true;
        model.description = "идет определение параметров...";
        $transport.send(new Message({ what: "parameters-recalc-driver" }, { tubeId: id }))
            .finally(function () {
                reload();
            });
    };

    $scope.model = model;

    ////copy
    function initEditor(params) {
        var html = '<div ng-show="!editingInit" ng-click="startEditingInit()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingInit(data)" ng-show="editingInit" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingInit = function () {
            params.$scope.editingInit = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingInit = function (data) {
            data.init = parseFloat(data.init);
            if (isNaN(data.init)) {
                data.init = 0.0;
            }
            params.$scope.editingInit = false;
        };

        return domElement;
    }

    function kEditor(params) {
        var html = '<div ng-show="!editingK" ng-click="startEditingK()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingK(data)" ng-show="editingK" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingK = function () {
            params.$scope.editingK = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingK = function (data) {
            data.k = parseFloat(data.k);
            if (isNaN(data.k)) {
                data.k = 1.0;
            }
            params.$scope.editingK = false;
        };

        return domElement;
    }

    var reload = function () {
        model.loading = true;
        model.description = "идет получение данных...";

        $q.all([$transport.send(new Message({ what: "parameters-get-2" }, { tubeId: id })), $list.getRowsCacheFiltered({ ids: [id] })])
        .then(function (messages) {

            model.parameters.length = 0;
            model.parameters.push("<нет>");
            for (var i = 0; i < messages[0].body.parameters.length; i++) {
                model.parameters.push(messages[0].body.parameters[i].name);
            }

            for (var i = 0; i < messages[0].body.tags.length; i++) {
                var tag = messages[0].body.tags[i];
                for (var j = 0; j < model.tags.length; j++) {
                    var en = model.tags[j];
                    if (en.dataType === tag.dataType && en.name === tag.name) {                        
                        for (var property in tag) {
                            en[property] = tag[property];
                        }                        
                    }
                }
            }

            var names = [];
            for (var i = 0; i < messages[1].rows.length; i++) {
                var row = messages[1].rows[i];
                names.push((row.name + " " + row.pname).trim());
            }
            model.names = names;
            model.loading = false;
        });
    }

    reload();
});