function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}

angular.module("app")
.controller("EventsCtrl", function (eventsSvc, $timeout, $filter, $rootScope, $scope, $uibModal, $log, $settings) {

    var model = {
        window: $scope.$parent.window,
        modal: undefined,
        //
        events: eventsSvc.events,
        viewEvents: eventsSvc.viewEvents,
        eventsViewCount: eventsSvc.events.length,
        //
        alertType: 'danger',
        alarmsCounter: 0,
        alarmsNotViewedCounter: 0,
        //
        columnState: undefined,
        sortModel: [
            { field: 'dateStart', sort: 'desc' },
            { field: 'dateEnd', sort: 'desc' }
        ],

        IsToolPanelShow: false,
        IsEndedShow: false,
        IsViewedShow: false
    }

    model.refresh = function () {
        if (model.modalIsOpen) {
            eventsSvc.setEventsView();
        }

        model.alarmsCounter = eventsSvc.alarmsEval();
        model.alarmsNotViewedCounter = eventsSvc.alarmsEval(true);
        model.alertType = (model.alarmsCounter == 0) ? 'success' : ((model.alarmsNotViewedCounter == 0) ? 'info' : 'danger');

        if ($scope.opt.api) {
            $scope.opt.api.onNewRows();
        }
    }

    eventsSvc.subscribe("events-ctrl", function () {
        model.refresh();
    });

    var columnStateTimeout;

    function loadColumnState() {
        var state = $settings.getEventsColumnState();
        if (state) {
            $scope.opt.api.setColumnState(state);
        }
    }

    function saveColumnState() {
        var state = $scope.opt.api.getColumnState();
        $settings.setEventsColumnState(state);
    }

    function columsStateChanged() {
        $timeout.cancel(columnStateTimeout);
        columnStateTimeout = $timeout(saveColumnState, 500);
    }

    var waitGridReadyTime = 0;

    function waitGridReady() {
        if ($scope.opt && $scope.opt.api) {
            gridReady($scope.opt.api);
        } else {
            if (waitGridReadyTime < 500)
            {
                $timeout(waitGridReady, 1);
                waitGridReadyTime++;
            }
        }
    }

    function gridReady(api) {
        if (!model.isGridReady) {
            model.isGridReady = true;

            ////colums restore
            loadColumnState();

            if (!api) api = $scope.opt.api;
            //sort restore/get default
            api.setSortModel(model.sortModel);
            //
            api.showToolPanel(model.IsToolPanelShow);
            setEndedFilter(api);
        }
    }

    function setEndedFilter(api)
    {
        if (api) {
            var filterApi = api.getFilterApi('dateEnd');
            if (model.IsEndedShow == true) {
                filterApi.selectEverything();
            } else {
                filterApi.selectNothing();
                filterApi.selectValue(null);
            }
            api.onFilterChanged();
        }
    }

    $scope.opt = {
        enableFilter: true,
        enableSorting: true,
        toolPanelSuppressPivot: true,

        columnDefs: [{
            headerName: "Объект",
            field: "objectId",
            width: 200,
            cellRenderer: function (params) {
                var row = params.data.link.row;
                if (row) {
                    return (row.name + " " + row.pname).trim();
                }
                return params.data.link.objectId ? params.data.link.objectId : '';
            }
        }, {
            headerName: "Параметр",
            field: "param",
            width: 70,
            valueGetter: function (params) {
                return params.data.link.param ? params.data.link.param : '';
            }
        }, {
            headerName: "Событие",
            field: "message",
            width: 250,
            valueGetter: function (params) {
                return params.data.link.message ? params.data.link.message : '';
            }
        }, {
            headerName: "Дата начала",
            field: "dateStart",
            width: 150,
            valueGetter: function (params) {
                return params.data.link.start ? $filter('date')(params.data.link.start, "dd.MM.yyyy HH:mm:ss") : '';
            }
        }, {
            headerName: "Дата окончания",
            field: "dateEnd",
            width: 150,
            valueGetter: function (params) {
                return params.data.link.end ? $filter('date')(params.data.link.end, "dd.MM.yyyy HH:mm:ss") : '';
            }
        }],

        enableColResize: true,

        rowData: model.viewEvents,

        rowClass: function (row) {
            var classes = [];
            if (!row.data.link.end) {
                classes.push('cell-highlight');
            }
            return classes;
        },

        ready: gridReady,
        gridReady: gridReady,

        columnVisibilityChanged: columsStateChanged,
        columnOrderChanged: columsStateChanged,
        columnResized: columsStateChanged,

        afterSortChanged: function () {
            var api = $scope.opt.api;
            model.sortModel = api.getSortModel();
            setEndedFilter(api);
        }
    };

    waitGridReady();
    
    model.toggleToolPanel = function () {
        var api = $scope.opt.api;
        if (api) {
            model.IsToolPanelShow = !model.IsToolPanelShow;
            api.showToolPanel(model.IsToolPanelShow);
            setEndedFilter(api);
        }
    }

    model.toggleEndedShow = function () {
        var api = $scope.opt.api;
        if (api) {
            model.IsEndedShow = !model.IsEndedShow;
            setEndedFilter(api);
        }
    }
    model.toggleViewedShow = function () {
        //model.viewEvents = model.events;
        if (model.IsViewedShow) {
            $scope.opt.rowData = model.viewEvents;
        }
        else {
            $scope.opt.rowData = model.events;
        }
        if ($scope.opt.api) {
            $scope.opt.api.onNewRows();
        }
        $scope.opt.api.refreshView();
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
        model.refresh();

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
    
    //

    var listeners = [];

    listeners.push($rootScope.$on("list:update-done", function (e, message) {
        var api = $scope.opt.api;
        if (api) {
            //api.refreshView();//onNewRows();
            api.onNewRows();
            setEndedFilter(api);
        }
    }));

    $scope.$on('$destroy', function () {
        $log.debug("уничтожается events");
        for (var i = 0; i < listeners.length; i++) {
            listeners[i]();
        }
    });

    //

    $scope.model = model;
});
