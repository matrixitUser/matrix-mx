angular.module("app")
.controller('ListSelectCtrl', function ($scope, $uibModalInstance, $window, $list, $log, data, $rootScope, metaSvc, $filter, $transport) {

    var lastSelectedId;
    //ид всех выбранных
    var selectedIds = {};
    var visibleRows = [];
    var visibleIds = {};

    var model = {
        rows: [],
        //pinIds: {},
        optionsView: [],
        pageSize: 25,
        isLoading: true,
        counting: 0,
        count: 0
    };

    var columns2 = [{
        headerName: "Выбор",
        field: "select",
        width: 50,
        floatingCellRenderer: function (params) {
            return '<input class="ag-selection-checkbox" type="checkbox" checked="' + params.data._frozenchecked + '"></input>';
        },
        suppressSizeToFit: true,
        suppressSorting: true,
        suppressMenu: true,
        volatile: true,
        checkboxSelection: true
    }, {
        headerName: "Название",
        field: "name",
        suppressSizeToFit: false,
        cellRenderer: cellNameTmpl
    }, {
        headerName: "Название точки учёта",
        field: "pname",
        suppressSizeToFit: false,
        cellRenderer: cellPNameTmpl
    }, {
        headerName: "Телефон",
        field: "phone",
        suppressSizeToFit: false,
        cellRenderer: cellPhoneTmpl
        //width: 30
    }, {
        headerName: "Статус",
        field: "state",
        suppressSizeToFit: false,
        cellRenderer: cellStatusTmpl
        //width: 30
    }, {
        headerName: "Тип прибора",
        field: "device"
    }, {
        headerName: "IMEI",
        field: "imei",
        cellRenderer: cellImeiTmpl
    }, {
        headerName: "№ договора",
        field: "imei"
    }];

    function getApi() {
        return $scope.model.grid2.api;
    }

    // Обновление выбранных
    var updateSelection = function () {

        getApi().deselectAll();
        getApi().forEachNode(function (node) {
            if (selectedIds[node.data.id]) {
                getApi().selectNode(node, true, true);
            }
        });
    };

    // Выбрать всё / ничего
    model.selectAll2 = function () {

        if (model.getSelectedIds().length > 0) {
            selectedIds = {};
            updateSelection();
        } else {
            //load all ids by filter
            $list.getRowsCacheIdsFiltered({}).then(function (ids) {
                for (var i = 0; i < ids.length; i++) {
                    var id = ids[i];
                    if (!selectedIds[id]) selectedIds[id] = true;
                }
                updateSelection();
            });
        }
    };

    /**
     * обновление источника данных для грида
     * происходит при изменении фильтра
     */
    var upd = function () {
        //$scope.model.grid2.api.showLoading(true);
        $scope.model.grid2.api.showLoadingOverlay();
        selectedIds = {}; //обнуление выбора

        var datasource = {
            pageSize: 100,
            overflowSize: 100,
            maxConcurrentRequests: 1,
            maxPagesInCache: 2,
            getRows: function (params) {
                var current = params.startRow;
                var take = params.endRow - params.startRow;

                var filter = {};
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

                selectedIds = {}; //обнуление выбора

                $list.getRowsCacheFiltered(filter).then(function (message) {
                    //$scope.model.grid2.api.showLoading(false);
                    $scope.model.grid2.api.hideOverlay();
                    var rows = message.rows;

                    model.count = message.count;
                    params.successCallback(rows, model.count);
                    datasource.rowCount = model.count;

                    visibleRows.length = 0;
                    visibleIds = {};
                    for (var i = 0; i < rows.length; i++) {
                        var id = rows[i].id;
                        visibleRows.push(rows[i]);
                        visibleIds[id] = true;
                    }

                    updateSelection();
                });
            }
        };
        $scope.model.grid2.api.setDatasource(datasource);
    }

    var ready2 = function () {
        upd();
    };

    //обновление выбора
    function onSelectionChanged(event) {
        //selectedIds[event.node.data.id] = true;
        selectedIds = {};
        var selectedRows = event.selectedRows;
        for (var i = 0; i < selectedRows.length; i++) {
            var id = selectedRows[i].id;
            selectedIds[id] = true;
        }
    };

    //получение ид выбранных
    model.getSelectedIds = function () {
        var ids = [];
        for (var id in selectedIds) {
            ids.push(id);
        }
        return ids;
    }
   
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
        onSelectionChanged: onSelectionChanged,
        floatingTopRowData: [],
        getRowClass: function (row) {
            var classes = [];
            if (row.data.isDisabled) {
                classes.push('cell-disabled');
            }
            if (row.data.selected) {
                classes.push('cell-selected');
            }
            return classes;
        },
        headerCellRenderer: function (params) {
            if (params.colDef.field == 'select') {
                return '<div style="align-content: center; text-align: center;font-size:10px;" class="btn btn-xs btn-default" onclick="window.mailerListModel.selectAll2()">Все</div>';
            }
            return params.colDef.headerName;
        }
    };

    function cellNameTmpl(params) {
        return '<img src="/img/house.png" height="20" /> '
            + params.data.name;
    }

    function cellPNameTmpl(params) {
        return params.data.pname || "";
    }

    function cellPhoneTmpl(params) {
        if (!params.data.phone) return "<i><нет></i>";
        return '<img src="/img/phone.png" height="20" /> '
            + params.data.phone;
    }

    function cellImeiTmpl(params) {
        if (!params.data.imei) return "<i><нет></i>";
        return '<img src="/img/fastrack.png" height="20" /> '
            + params.data.imei;
    }

    function cellStatusTmpl(params) {

        var img;
        var title;

        var state = parseInt(params.data.state);

        var dt = state.date;
        var date = dt ? $filter("date")(dt, "dd.MM.yy HH:mm:ss") : "";
        if (isNaN(state)) {
            img = "application_control_bar.png";
            title = "<span class='grey'>нет информации</span>";
        } else if (state === 0) {
            img = "tick.png";
            title = "Опрос успешно завершен " + date;
        } else if (state > 0 && state < 100) {
            switch (state) {
                case 10:
                    img = "time.png";
                    title = "Ожидание " + date;
                    break;
                case 20:
                    img = "loader.gif";
                    title = "Идет опрос " + date;
                    break;
            }
        } else if (state == 666) {
            img = "application_control_bar.png";
            title = (params.data.description) ? params.data.description : metaSvc.getReasonByCode(state);
        } else {
            img = "error.png";
            var reason = metaSvc.getReasonByCode(state);
            title = "Ошибка " + (state || "?") + " " + (reason || "неизвестная ошибка") + date;
        }

        var res = '<img src="/img/' + img + '" width="20" title="' + title + '"> ' + title;
        return res;
    }






    var listener = $rootScope.$on("transport:message-received", function (e, message) {

        if (message.head.what == "ListUpdate") {
            var ids = message.body.ids;
            $transport.send(new Message({ what: "rows-get-4" }, { ids: ids })).then(function (message) {
                var rows = message.body.rows;

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

        if (message.head.what == "edit") {

        }

        if (message.head.what == "changes") {

            var idsToUpdate = [];

            var rules = message.body.rules;
            for (var i = 0; i < rules.length; i++) {
                var rule = rules[i];
                if (rule.target === "node" && rule.content.type === "Tube") {
                    idsToUpdate.push(rule.content.id);
                }
            }

            //todo refresh rows here
            var x = idsToUpdate;
            if (idsToUpdate.length > 0) {
                //reload 
                $transport.send(new Message({ what: "rows-get-4" }, { ids: idsToUpdate })).then(function (message) {
                    var rows = message.body.rows;

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

                //upd($listFilter.getFilter());
            }
        }
    });


    $scope.$on('$destroy', function () {
        $log.debug("уничтожается выбор объектов");
        listener();
    });

    $scope.ok = function () {
        $uibModalInstance.close(model.getSelectedIds());
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $window.mailerListModel = model;
    $scope.model = model;
});