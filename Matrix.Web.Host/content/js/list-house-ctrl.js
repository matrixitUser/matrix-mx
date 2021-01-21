'use strict';

angular.module("app");

function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}

app.controller("ListHouseCtrl", function ($log, $timeout, $scope, $rootScope, $filter,
    $window, $transport, metaSvc, $helper,
    $list, $actions, $settings, $listFilter) {

    //Контекстное меню
    var menuActions = [
        {
            title: "Добавить в группу", name: 'add-to-folder', getParam: arrayOfSels, isEnabled: isSelectedAny, success: function (result) {
                if (result) {
                    $rootScope.$broadcast("listFilter:changed");
                }
            }
        },
        null,
        { name: 'report-list', getParam: reportArrayOfSels }
    ];

    //Ячейка GRID с действиями
    var panelActions = [
        { name: 'house-show', popover: "Карточка объекта", isVisible: function (data) { return (data.class == 'HouseRoot'); } }
    ];

    /////////////////// MODEL /////////////////// MODEL /////////////////// MODEL /////////////////// MODEL /////////////////// MODEL /////////////////// 
    
    var model = {
        rows: [],
        selIds: {},
        pageSize: 25,
        optStatusText: "",
        isLoading: true,
        counting: 0,
        count: 0,
        filterText: "",
        //
        options: [],
        optionsView: [],
        panelActionsView: [],
        //
        foldersShownNames: [],
        foldersShownIds: [],
        //
        modal: {}
    }

    $scope.$watch('model.options', function () {
        model.optionsView = [];
        if ($helper.isArray(model.options) && model.options.length > 0) {
            var isAlmost1Action = false;
            var isDivider = false;
            var sort = $filter('orderBy')(model.options, 'index');
            for (var i = 0; i < sort.length; i++) {
                var option = sort[i];
                if (option.type == "divider") {
                    isDivider = true;
                } else {
                    if (isAlmost1Action && isDivider) {
                        isDivider = false;
                        model.optionsView.push(null);
                    }
                    model.optionsView.push(option);
                    isAlmost1Action = true;
                }
            }
        }
    }, true);

    var connection = $transport.getStatus();

    var filterTimeout;
    var listColumnStateTimeout;
    var lastSelectedId;

    // вспомогательные функции

    function arrayOfSels() {
        return getSelectedIds();
    }

    function reportArrayOfSels() {
        var ids = getSelectedIds();
        var arg = {
            ids: ids,
            header: ids.length === 0 ? "" : ids.length + " объектов"
        };
        return arg;
    }

    function isSelectedAny() {
        return arrayOfSels().length > 0;
    }

    function noWrap(data) {
        return data;
    }

    // События грида

    function toggleToolPanel(newstate) {
        var api = getApi();//$scope.opt.api;
        api.showToolPanel(newstate);
    }

    ////

    model.options = (function () {

        var options = [];

        for (var o = 0; o < options.length; o++) {
            options[o].index = o;
        }

        for (var i = 0; i < menuActions.length; i++) {
            var menuAction = menuActions[i];
            if (menuAction === null) {
                options.push({ index: o + i, type: "divider" });
                continue;
            }

            (function (i) {
                var ma = menuActions[i];
                $actions.get(ma.name).then(function (action) {
                    if (action == null) return;

                    var getParam = ma.getParam;
                    var getArg = ma.getArg;
                    var isEnabled = ma.isEnabled;
                    var success = ma.success;
                    var error = ma.error;

                    var actGetParamWArg = function (a, gp, ga, en, sc, er) {
                        return function ($item) {
                            if (!sc) sc = function () { };
                            if (!er) er = function () { };
                            a.act(gp == undefined ? gp : gp(), ga == undefined ? ga : ga()).then(sc, er);
                        }
                    }

                    var title = ma.title || action.header;

                    options.push({
                        index: o + i,
                        icon: action.icon,
                        title: title,
                        type: 'html',
                        action: actGetParamWArg(action, getParam, getArg, isEnabled, success, error),
                        enabled: isEnabled
                    });
                })
            })(i);
        }

        return options;

    })();

    //PANEL ACTIONS
    {
        for (var i = 0; i < panelActions.length; i++) {
            var panelAction = panelActions[i];
            if (panelAction === null) {
                continue;
            }

            (function (pa) {
                $actions.get(pa.name).then(function (a) {
                    if (a != null) {
                        var action = {};
                        action.popover = pa.popover;
                        action.title = pa.title || action.header;
                        action.icon = function (data) {
                            if (pa.getIcon) {
                                return pa.getIcon(data)
                            }
                            return a.icon;
                        }
                        action.visible = pa.isVisible || function () { return true; };
                        action.act = function (data) {
                            a.act(pa.wrap ? pa.wrap(data) : data);
                        }
                        model.panelActionsView.push(action);
                    }
                });
            })(panelAction);

        }
    }

    /////

    $window.listModel = model;
    $scope.model = model;
    $scope.connection = connection;


    ///------new grid options test------

    var columns2 = [];

    columns2.push({
        headerName: "Выбор",
        field: "select",
        width: 50,
        suppressSizeToFit: true,
        suppressSorting: true,
        suppressMenu: true,
        volatile: true,
        checkboxSelection: true,
        hide: false
    });
    
    columns2.push({
        headerName: (metaSvc.config === "orenburg")? "Номер площадки" : "Номер договора",
        field: "number",
        cellRenderer: numberTmpl,
        suppressSizeToFit: false,
        width: 50,
        hide: false
    });

    columns2.push({
        headerName: (metaSvc.config === "teplocom")? "Абонент" : "Название объекта учёта",
        field: "name",
        suppressSizeToFit: false,
        cellRenderer: cellNameTmpl,
        width: 200
    });

    columns2.push({
        headerName: "Название точки учёта",
        field: "pname",
        suppressSizeToFit: false,
        cellRenderer: cellPNameTmpl,
        hide: (metaSvc.config === "orenburg")
    });

    columns2.push({
        headerName: "Действия",
        field: "actions",
        cellRenderer: cellActionsTmpl,
        suppressSizeToFit: true,
        suppressSorting: true,
        width: 100
    });

    columns2.push({
        headerName: "Примечание",
        field: "comment",
        suppressSizeToFit: true,
        suppressSorting: true,
        cellRenderer: cellCommentTmpl,
        width: 200,
        hide: true
    });

    var getColumnsStateDefault = function (columns) {
        if (columns && columns.length) {
            var columnsState = [];
            for (var i = 0; i < columns.length; i++) {
                var column = columns[i];
                columnsState.push({
                    colId: column.field,
                    hide: column.hide || false,
                    width: column.width || 150,
                    aggFunc: null,
                    pivotIndex: null
                });
            }
            return columnsState;
        }
    }

    var ready2 = function () {
        loadColumnState();
        upd($listFilter.getFilter());
    };
    
    var visibleRows = [];

    /**
     * обновление источника данных для грида
     * происходит при изменении фильтра
     */
    var upd = function (filter) {
        //$scope.model.grid2.api.showLoading(true);
        if ($scope.model.grid2.api) {
            $scope.model.grid2.api.showLoadingOverlay();
        }

        selectedIds = {};

        var datasource = {
            pageSize: 100,
            overflowSize: 100,
            maxConcurrentRequests: 1,
            maxPagesInCache: 2,
            getRows: function (params) {
                var current = params.startRow;
                var take = params.endRow - params.startRow;

                filter.page = {
                    offset: current,
                    count: take
                };

                filter.order = [];
                for (var i = 0; i < params.sortModel.length; i++) {
                    var sm = params.sortModel[i];
                    filter.order.push({
                        column: sm.colId,
                        dir: sm.sort
                    });
                }

                selectedIds = {};

                $list.getRowsCacheFiltered(filter).then(function (message) {

                    var rows = message.rows;
                    model.count = message.count;

                    $scope.model.grid2.api.hideOverlay();
                    params.successCallback(rows, model.count);
                    datasource.rowCount = model.count;

                    visibleRows.length = 0;
                    visibleIds = {};
                    for (var i = 0; i < rows.length; i++) {
                        var id = rows[i].id;
                        visibleRows.push(rows[i]);
                        visibleIds[id] = true;
                    }

                    //update selection
                    updateSelection();
                });
            }
        };
        $scope.model.grid2.api.setDatasource(datasource);
    }

    var visibleIds = {};

    ////

    function loadColumnState() {
        var state = $settings.getState("house-column-state");
        if (state) {
            var api = getApi();
            api.setColumnState(state);
        }
    }

    function saveColumnState() {
        var api = getApi();
        var state = api.getColumnState();
        $settings.setState("house-column-state", state);
    }

    function restoreColumnState() {
        var state = getColumnsStateDefault(columns2);
        if (state) {
            var api = getApi();
            api.setColumnState(state);
        }
    }

    ////

    var selectedIds = {};

    function getSelectedIds() {
        var ids = [];
        for (var id in selectedIds) {
            ids.push(id);
        }
        return ids;
    }

    model.selectAll2 = function () {
        if (getSelectedIds().length > 0) {
            selectedIds = {};
            updateSelection();
        } else {
            //load all ids by filter
            var filter = $listFilter.getFilter();
            $list.getRowsCacheIdsFiltered(filter).then(function (ids) {
                for (var i = 0; i < ids.length; i++) {
                    var id = ids[i];
                    if (!selectedIds[id]) selectedIds[id] = true;
                }
                updateSelection();
            });
        }
    };

    function onCellClicked(params) {
        var self = this;

        // We have to wait otherwise it overrides our selection
        setTimeout(function waitForAngularGridToFinish() {
            // Select multiple rows when the shift key was pressed
            if (params.event.shiftKey && self.previousSelectedRowIndex !== undefined) {
                var smallerNumber = params.rowIndex < self.previousSelectedRowIndex ? params.rowIndex : self.previousSelectedRowIndex;
                var biggerNumber = params.rowIndex > self.previousSelectedRowIndex ? params.rowIndex : self.previousSelectedRowIndex;

                for (var rowIndexToSelect = smallerNumber; rowIndexToSelect <= biggerNumber; rowIndexToSelect++) {
                    if ((params.colDef.field !== "checkbox") && (params.colDef.field !== "actions")) {
                        self.api.selectIndex(rowIndexToSelect, true, false);//rowIndexToSelect !== biggerNumber
                    }
                }
            }

            self.previousSelectedRowIndex = params.rowIndex;
        }, 0);

    }

    function onSelectionChanged(event) {
        //selectedIds[event.node.data.id] = true;
        selectedIds = {};
        var selectedRows = event.selectedRows;
        for (var i = 0; i < selectedRows.length; i++) {
            var id = selectedRows[i].id;
            selectedIds[id] = true;
        }
        $list.setSelected(getSelectedIds(), selectedRows);
    };

    var updateSelection = function () {

        var rows = [];

        getApi().deselectAll();
        getApi().forEachNode(function (node) {
            if (selectedIds[node.data.id]) {
                getApi().selectNode(node, true, true);
                rows.push(node.data);
            }

        });

        $list.setSelected(getSelectedIds(), rows);
    };

    model.getSelectedIds = getSelectedIds;

    function getApi() {
        return $scope.model.grid2.api;
    }

    //====рендеры ячеек====

    function numberTmpl(params) {
        return params.data.number;
    }

    function cellNameTmpl(params) {
        var name = "" + (params.data.name || "");
        return '<img src="/img/house.png" height="20" /> ' + ((name && name !== "undefined") ? name : "");
    }

    function cellCommentTmpl(params) {
        return (params.data.comment || "");
    }

    function cellPNameTmpl(params) {
        return params.data.pname || "";
    }
    
    function cellActionsTmpl(params) {
        var ret = '';
        if (params.data.id && model.panelActionsView && model.panelActionsView.length > 0) {
            var id = params.data.id;
            for (var i = 0; i < model.panelActionsView.length; i++) {
                var action = model.panelActionsView[i];
                if (action.visible(params.data)) {
                    ret += '<a href="#" onclick="window.listModel.panelActionsView[' + i + '].act({id: \'' + id + '\'})" uib-popover="' + action.popover + '" popover-trigger="mouseenter" type="button" class="btn btn-xs btn-default">'
                        + '<img src="' + action.icon(params.data) + '" width="16" />'
                        + '</a> ';
                    //ng-click="' + action.act(params.data) + '"
                }
            }
        }
        return ret;
    }

    function selectCellTmpl(params) {
        return '<div style="align-content: center; text-align: center">\
                <img src="/img/' + (params.api.isNodeSelected(params.node) ? 'check_box.png' : 'check_box_uncheck.png') + '" width="20" />\
            </div>';
    };
    
    model.grid2 = {
        angularCompileRows: false,
        toolPanelSuppressPivot: true,
        virtualPaging: true,
        rowSelection: "multiple",
        enableServerSideSorting: true,
        enableServerSideFilter: true,
        enableColResize: true,
        columnDefs: columns2,
        onReady: ready2,
        onCellClicked: onCellClicked,
        onSelectionChanged: onSelectionChanged,
        floatingTopRowData: [],
        getRowClass: function (row) {
            var classes = [];
            if (row.data.isDisabled && row.data.isDisabled == "True") {
                classes.push('cell-disabled');
            }
            if (row.data.selected) {
                classes.push('cell-selected');
            }
            return classes;
        },
        headerCellRenderer: function (params) {
            if (params.colDef.field == 'select') {
                return '<div style="align-content: center; text-align: center;font-size:10px;" class="btn btn-xs btn-default" onclick="window.listModel.selectAll2()">Все</div>';
            }
            return params.colDef.headerName;
        }
    };


    //Подписки на события rootScope
    var listeners = [];

    //Push-сообщение от сервера
    listeners.push($rootScope.$on("transport:message-received", function (e, message) {

        var ids = [];

        if (message.head.what == "ListUpdate") {
            ids = message.body.ids;
        } else if (message.head.what == "edit") {

        } else if (message.head.what == "changes") {
            var rules = message.body.rules;
            for (var i = 0; i < rules.length; i++) {
                var rule = rules[i];
                if (rule.target === "node" && rule.content.type === "Tube") {
                    ids.push(rule.content.id);
                }
            }
        }

        if (ids && ids.length) {
            $list.getRowsCache(ids).then(function (rows) {
                getApi().rowRenderer.rowModel.forEachInMemory(function (node) {
                    for (var i = 0; i < rows.length; i++) {
                        var updatedRow = rows[i];
                        if (updatedRow.id === node.data.id) {
                            node.data = updatedRow;
                            return;
                        }
                    }
                });
                getApi().refreshView();
            });
        }
    }));

    //Изменение фильтра списка объектов
    listeners.push($rootScope.$on("listFilter:changed", function () {
        upd($listFilter.getFilter());
    }));

    listeners.push($rootScope.$on("list:toggle-toolpanel", function (e, message) {
        if (message) {
            toggleToolPanel(message.newstate);
        } else {
            var api = getApi();
            toggleToolPanel(!api.isToolPanelShowing());
        }
    }));

    listeners.push($rootScope.$on("list:update-done", function (e, message) {
        var api = getApi();
        api.refreshView();
    }));

    listeners.push($rootScope.$on("list:save-columns-state", function (e, message) {
        saveColumnState();
    }));

    listeners.push($rootScope.$on("list:restore-columns-state", function (e, message) {
        restoreColumnState();
    }));


    $scope.$on('$destroy', function () {
        $log.debug("уничтожается таблица с объектами");
        for (var i = 0; i < listeners.length; i++) {
            var listener = listeners[i];
            listener();
        }
    });
});