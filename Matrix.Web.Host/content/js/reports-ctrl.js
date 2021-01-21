angular.module("app");
app.controller("ReportsCtrl", function ($scope, $log, $reports, $filter, $sce, $modal, cacheSvc, $parse) {//

    var d = new Date();
    var data = $scope.$parent.window.data;

    var names = [];

    for (var i = 0; i < data.ids.length; i++) {
        var id = data.ids[i];
        var row = cacheSvc.get(id);
        //
        var name = $parse('cart.name')(row) + (row.name ? ": " + row.name : "");
        //
        names.push(name || row.id);
    }
    /*
    var model = $scope.$parent.model;
    /*/
    var model = {
        data: data,
        names: names,
        window: $scope.$parent.window,
        modal: undefined,
        report: $reports.getById(data.id),
        autoclose: function(){ 
            return (data.ids.length == 0); 
        }
    }
    // */

    model.wait = false;
    model.success = true;

    var s = new Date();
    s.setHours(0, 0, 0, 0);

    var e = new Date();
    e.setMinutes(0, 0, 0);

    model.start = s;
    model.end = e;

    //$scope.reportId = reportSvc.getReportId();


    //model.startOpen = function ($event) {
    //    $event.preventDefault();
    //    $event.stopPropagation();

    //    model.startOpened = true;
    //};

    //model.endOpen = function ($event) {
    //    $event.preventDefault();
    //    $event.stopPropagation();

    //    model.endOpened = true;
    //};

    model.update = function () {
        if (model.data && model.data.id) {
            model.wait = true;
            //$log.debug("даты отчета %s %s", start.toString(), end.toString());
            $reports.build(model.data.id, model.start, model.end, model.data.ids).then(function (report) {
                model.wait = false;
                $log.debug("строим отчет report=%s", $filter('json')(report));
                if (!report) {
                    model.error = "Отчет не загружен";
                    model.success = false;
                } else {
                    model.error = "";
                    model.success = true;
                    model.reportAsHtml = $sce.trustAsHtml(report);
                    model.reportAsText = report;
                }
            }, function (err) {
                model.error = "Отчет не построен: " + (err || "неизвестная ошибка");
                model.success = false;
                model.wait = false;
                $log.error("отчет не построен: %s", err);
            });
        }
    }

    model.update();

    model.savePdf = function () {
        if (model.reportAsText) {
            $reports.exportToPdf(model.reportAsText.toString());
        }
    };

    model.toExcel = function () {
        if (model.reportAsText) {
            $reports.exportToXls(model.reportAsText.toString());
        }
    };

    //modal

    model.modalOpen = function () {
        model.modal = $modal.open({
            templateUrl: model.window.modalTemplateUrl,
            size: 'lg',
            scope: $scope
        });
        model.modalIsOpen = true;

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

    model.close = function () {
        model.modal.close();
        model.window.close();
    }

    model.window.open = model.modalOpen;

    model.modalOpen();

    //

    $scope.model = model;
});