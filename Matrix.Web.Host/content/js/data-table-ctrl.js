angular.module("app")
.controller("dataTableCtrl", function ($scope, $log, $transport, $uibModalInstance, ids) {

    var model = {
    };

    model.close = function () {
        $uibModalInstance.close();
    }

    model.update = function () {
        $transport.send(new Message({ what: "export-data" }, { start: model.start, end: model.end, type: model.type, ids: ids })).then(function (message) {

            var fields = {};
            for (var j = 0; j < message.body.data.length; j++) {
                var rec = message.body.data[j];

                for (var property in rec) {
                    if (!fields[property]) {
                        fields[property] = 1;
                    }
                }
            }

            model.grid.columnDefs.length = 0;
            for (var property in fields) {
                var caption = property;
                if (property === "date") caption = "Дата";
                model.grid.columnDefs.push({
                    field: property,
                    headerName: caption
                });
            }
            model.grid.rowData = message.body.data;
            model.grid.api.onNewRows();
        });
    }

    model.start = new Date();
    model.end = new Date();
    model.type = "Hour";

    model.grid = {
        columnDefs:
            //    {
        //    headerName: "Название",
        //    field: "name"
        //},
        [{
            headerName: "Дата",
            field: "date"
        }, {
            headerName: "Q р.у.",
            field: "Qру"
        }, {
            headerName: "Q н.у.",
            field: "Qну"
        }, {
            headerName: "P",
            field: "P"
        }, {
            headerName: "T",
            field: "T"
        }],
        enableFilter: true,
        rowData: null,
        rowSelection: "multiple",
        enableColResize: true,
        enableSorting: true
    };

    $scope.model = model;
});