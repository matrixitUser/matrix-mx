angular.module("app")
.controller("TestCtrl", function ($scope, $log, $modal, $list, $parse) {//, data

    var data = $scope.$parent.window.data;

    var rows = [];
    var names = [];

    for (var i = 0; i < data.length; i++) {
        var id = data[i];
        var row = $list.getRow(id);
        //
        var name = $parse('cart.name')(row) + (row.name ? ": " + row.name : "");
        //
        rows.push(row);
        names.push(name || row.id);
    }


    var model = {
        counter: 0,
        window: $scope.$parent.window,
        modal: undefined,
        ids: data,
        names: names
    }

    model.increment = function () {
        model.counter++;
    }

    model.decrement = function () {
        model.counter--;
    }

    //modal

    model.modalOpen = function () {
        model.modal = $modal.open({
            templateUrl: model.window.modalTemplateUrl,
            size: 'lg',
            scope: $scope
        });
    }

    model.close = function () {
        model.modal.close();
        model.window.close();
    }

    model.modalOpen();

    //

    $scope.model = model;
});