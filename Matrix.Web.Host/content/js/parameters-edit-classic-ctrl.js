'use strict';

angular.module("app");

app.controller("ParametersEditClassicCtrl", function ($uibModalInstance, $scope, data, $transport, $timeout, $helper, $parse, $list, $settings, metaSvc) {

    if (!data || !data.id) $uibModalInstance.close();

    var id = data.id;
    var names = [];
    var columnStateTimeout; //timeout for column state save
    var isHide = true;
    if (metaSvc.config == 'agidel') {
        isHide = false;
    }


    var model = {
        parameters: [],
        row: undefined,
        names: names,
        IsToolPanelShow: false
    };

    model.loading = true;

    var loadParameters = function () {
        model.loading = true;
        model.parameters.length = 0;
        $transport.send(new Message({ what: "parameters-get" }, { tubeId: id })).then(function (message) {
            for (var i = 0; i < message.body.parameters.length; i++) {
                var parameter = message.body.parameters[i];
                model.parameters.push(parameter);
            }
            if (model.parameters.length > 0) {
                model.selected = model.parameters[0];
            }
            if (model.opt.api) {
                model.opt.api.onNewRows();
            }
            model.loading = false;
        });
    }
    
    model.calculations = [{
        label: "Нет расчета",
        type: "NotCalculated"
    }, {
        label: "Итого",
        type: "Total"
    }];

    model.calcs = ["NotCalculated", "total"];
    model.calcLabels = { NotCalculated: "Нет расчета", total: "Итого" };

    model.getCalcLabel = function (type) {
        if (type && model.calcLabels[type]) {
            return model.calcLabels[type];
        }
        return type || "";
    }

    var columnDefs = [
        { headerName: "Имя", field: "name", width: 225, sort: 'asc' },
        { headerName: "Тип", field: "calc", width: 130, cellRenderer: calcEditor },
        { headerName: "Тег", field: "tag", width: 130, cellRenderer: tagEditor },
        { headerName: "Нач.значение", field: "init", width: 110, cellRenderer: initEditor, hide: true },
        { headerName: "Коэффициент", field: "k", width: 100, cellRenderer: kEditor, hide: true },
        { headerName: "Начало дня(ч)", field: "startDay", width: 110, cellRenderer: startDayEditor, hide: isHide },
        { headerName: "Конец дня(ч)", field: "endDay", width: 110, cellRenderer: endDayEditor, hide: isHide },
        { headerName: "Мин.значение", field: "min", width: 110, cellRenderer: minEditor, hide: isHide },
        { headerName: "Макс.значение", field: "max", width: 110, cellRenderer: maxEditor, hide: isHide },
        { headerName: "Мин.значение (ночь)", field: "minNight", width: 150, cellRenderer: minNightEditor, hide: isHide },
        { headerName: "Макс.значение (ночь)", field: "maxNight", width: 150, cellRenderer: maxNightEditor, hide: isHide },
        { headerName: "Сообщение при выходе за уставки", field: "alertMsg", width: 150, cellRenderer: alertMsgEditor }
    ];

    model.opt = {
        columnDefs: columnDefs,
        rowData: model.parameters,
        angularCompileRows: true,
        enableSorting: true,
        enableColResize: true,
        ready: function (api) {
            //var sort = [ { field: 'name', sort: 'asc' } ];
            //api.setSortModel(sort);
            //api.sizeColumnsToFit();
            loadColumnState();
        },
        columnVisibilityChanged: columsStateChanged,
        columnOrderChanged: columsStateChanged,
        columnResized: columsStateChanged
    };

    function columsStateChanged() {
        $timeout.cancel(columnStateTimeout);
        columnStateTimeout = $timeout(saveColumnState, 500);
    }

    function loadColumnState() {
        var state = $settings.getParametersColumnState();
        if (state) {
            model.opt.api.setColumnState(state);
        }
    }

    function saveColumnState() {
        var state = model.opt.api.getColumnState();
        $settings.setParametersColumnState(state);
    }

    model.toggleToolPanel = function () {
        var api = model.opt.api;
        if (api) {
            model.IsToolPanelShow = !model.IsToolPanelShow;
            api.showToolPanel(model.IsToolPanelShow);
        }
    }

    model.save = function (name) {
        $transport.send(new Message({ what: "parameters-save" }, { parameters: model.parameters, tubeId: id }));
        $uibModalInstance.close();
    };

    model.close = function () {
        $uibModalInstance.close();
    };

    model.recalc = function () {
        model.loading = true;
        $transport.send(new Message({ what: "parameters-recalc" }, { tubeId: id })).then(function (message) {
            loadParameters();
        });
    };

    $scope.model = model;


    if (data && data.id) {
        $list.getRowsCacheFiltered({ ids: [data.id] }).then(function (msg) {
            var rows = msg.rows;
            if (rows.length > 0 && rows[0].id == data.id) {
                var row = rows[0];
                names.push((row.name + " " + row.pname).trim());
                model.row = row;
            }
        }).finally(function () {
            loadParameters();
        });
    } else {
        loadParameters();
    }
    ///

    function calcEditor(params) {
        params.$scope.calcs = model.calcs;
        params.$scope.getCalcLabel = model.getCalcLabel;

        var html = '<div ng-show="!editing" ng-click="startEditing()"><img src="/img/16/page_edit.png" /> {{getCalcLabel(data.' + params.colDef.field + ')}}</div> ' +
            '<select style="width: 100%" ng-blur="editing=false" ng-change="editing=false" ng-show="editing" ng-options="cal as getCalcLabel(cal) for cal in calcs" ng-model="data.' + params.colDef.field + '">';

        // we could return the html as a string, however we want to add a 'onfocus' listener, which is no possible in AngularJS
        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditing = function () {
            params.$scope.editing = true; // set to true, to show dropdown

            // put this into $timeout, so it happens AFTER the digest cycle,
            // otherwise the item we are trying to focus is not visible
            $timeout(function () {
                var select = domElement.querySelector('select');
                select.focus();
            }, 0);
        };

        return domElement;
    }

    function tagEditor(params) {
        var html = '<div ng-show="!editingTag" ng-click="startEditingTag()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="editingTag=false" ng-show="editingTag" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingTag = function () {
            params.$scope.editingTag = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        return domElement;
    }

    function alertMsgEditor(params) {
        var html = '<div ng-show="!editingAlertMsg" ng-click="startEditingAlertMsg()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="editingAlertMsg=false" ng-show="editingAlertMsg" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingAlertMsg = function () {
            params.$scope.editingAlertMsg = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        return domElement;
    }

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
    
    function startDayEditor(params) {
        var html = '<div ng-show="!editingStartDay" ng-click="startEditingStartDay()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingStartDay(data)" ng-show="editingStartDay" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingStartDay = function () {
            params.$scope.editingStartDay = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingStartDay = function (data) {
            data.startDay = parseFloat(data.startDay);
            if (isNaN(data.startDay)) {
                data.startDay = "";
            }
            params.$scope.editingStartDay = false;
        };

        return domElement;
    }
    function endDayEditor(params) {
        var html = '<div ng-show="!editingStartDay" ng-click="startEditingEndDay()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingEndDay(data)" ng-show="editingEndDay" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingEndDay = function () {
            params.$scope.editingEndDay = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingEndDay = function (data) {
            data.endDay = parseFloat(data.endDay);
            if (isNaN(data.endDay)) {
                data.endDay = "";
            }
            params.$scope.editingEndDay = false;
        };

        return domElement;
    }
    function minEditor(params) {
        var html = '<div ng-show="!editingMin" ng-click="startEditingMin()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingMin(data)" ng-show="editingMin" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingMin = function () {
            params.$scope.editingMin = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingMin = function (data) {
            data.min = parseFloat(data.min);
            if (isNaN(data.min)) {
                data.min = "";
            }
            params.$scope.editingMin = false;
        };

        return domElement;
    }
    function minNightEditor(params) {
        var html = '<div ng-show="!editingMinNight" ng-click="startEditingMinNight()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingMinNight(data)" ng-show="editingMinNight" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingMinNight = function () {
            params.$scope.editingMinNight = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingMinNight = function (data) {
            data.minNight = parseFloat(data.minNight);
            if (isNaN(data.minNight)) {
                data.minNight = "";
            }
            params.$scope.editingMinNight = false;
        };

        return domElement;
    }
    function maxEditor(params) {
        var html = '<div ng-show="!editingMax" ng-click="startEditingMax()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingMax(data)" ng-show="editingMax" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingMax = function () {
            params.$scope.editingMax = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingMax = function (data) {
            data.max = parseFloat(data.max);
            if (isNaN(data.max)) {
                data.max = "";
            }
            params.$scope.editingMax = false;
        };

        //params.$scope.stopEditingMax = function (data) {
        //    var parsed = parseFloat(data.max);
        //    if (isNaN(parsed)) {
        //        data.max = "";
        //    } else {
        //        data.max = parsed.toString();
        //    }
        //    params.$scope.editingMax = false;
        //};

        return domElement;
    }
    function maxNightEditor(params) {
        var html = '<div ng-show="!editingMaxNight" ng-click="startEditingMaxNight()"><img src="/img/16/page_edit.png" /> {{data.' + params.colDef.field + '}}</div> ' +
            '<input type="text" style="width: 100%" ng-blur="stopEditingMaxNight(data)" ng-show="editingMaxNight" ng-model="data.' + params.colDef.field + '"></input>';

        var domElement = document.createElement("div");
        domElement.innerHTML = html;

        params.$scope.startEditingMaxNight = function () {
            params.$scope.editingMaxNight = true;

            $timeout(function () {
                var input = domElement.querySelector('input');
                input.focus();
            }, 0);
        };

        params.$scope.stopEditingMaxNight = function (data) {
            data.maxNight = parseFloat(data.maxNight);
            if (isNaN(data.maxNight)) {
                data.maxNight = "";
            }
            params.$scope.editingMaxNight = false;
        };

        return domElement;
    }
});