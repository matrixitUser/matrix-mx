angular.module("app")
.service("$reports", function ($transport, $log, $q, $base64, $helper) {

    var self = this;
    self.cache = {};
    self.list = [];

    var all = function () {
        return $transport.send(new Message({ what: "reports-list" })).then(function (message) {
            $log.debug("reports-svc ответ с сервера %s", message.head.what);
            if (message.head.what == "reports-list") {
                self.list = [];
                for (var i = 0; i < message.body.reports.length; i++) {
                    var report = message.body.reports[i];
                    self.cache[report.id] = report;
                    self.list.push(report);
                }

                //for (var i = 0; i < 15; i++) {
                //    var head = { id: "report" + i, name: "Report #" + i, template: "" };
                //    self.cache["report" + i] = head;
                //    self.list.push(head);
                //}

                return { reports: self.list };
            } else {
                return $q.reject("не авторизован");
            }
        });
    };

    var getById = function (id) {
        return self.cache[id];
    }

    var save = function (reports) {
        return $transport.send(new Message({ what: "reports-save" }, { reports: reports }));
    };

    var names = function () {
        return $transport.send(new Message({ what: "reports-names" }));
    }

    var build = function (reportId, start, end, objectIds) {
        //var objectIds = objectIdsProvider();

        //if (objectIds !== null && objectIds.length === 0) {
        //    return $q.reject("не выбраны объекты");
        //}

        return $transport.send(new Message({ what: "report-build" }, {
            start: start,
            end: end,
            report: reportId,
            targets: objectIds
        }))
            .then(function (message) {
                if (message.head.what == "report-build") {
                    return message.body;
                } else {
                    return $q.reject("данные не распознаны");
                }
            });
    }
    var sendMailToPdf = function (reportId, reportStart, reportEnd, reportIds, body, isOrientationAlbum) {
        $transport.send(new Message({ what: "report-mail-send" }, {
            type: "pdf",
            reportid: reportId,
            tubeIds: reportIds,
            dateStart: reportStart,
            dateEnd: reportEnd,
            text: body,
            isOrientationAlbum: isOrientationAlbum
        })).then(function (message) {
            var messageInAlert = (message.body.success == false) ? message.body.error : "Отчет успешно отправлен";
            alert(messageInAlert);
        });
    };

    var exportToPdf = function (body, isOrientationAlbum) {
        $transport.send(new Message({ what: "report-export" }, {
            type: "pdf",
            text: body,
            isOrientationAlbum: isOrientationAlbum
        })).then(function (message) {
            var bytes = message.body.bytes;
            var uri = "data:application/pdf;base64," + bytes;
            //window.open(url, "_blank");            
            //var now = new Date();
            //var formated_date = now.format("dd-mm-yyyy HH-MM-ss");
            var formated_date = "";
            var fileName = "Отчет" + formated_date + ".pdf";
            saveAs(uri, fileName);
        });
    };

    function saveAs(uri, filename) {
        var link = document.createElement('a');
        if (typeof link.download === 'string') {
            link.href = uri;

            link.download = filename;

            //Firefox requires the link to be in the body
            document.body.appendChild(link);

            //simulate click
            link.click();

            //remove the link when done
            document.body.removeChild(link);
        } else {
            window.open(uri);
        }
    };

    var exportToXls = function (body) {
        var uri = "data:application/vnd.ms-excel;base64,"
        var template = "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body>{table}</body></html>"
        var b64 = function (s) { return $base64.encode(unescape(encodeURIComponent(s))) }
        var format = function (s, c) { return s.replace(/{(\w+)}/g, function (m, p) { return c[p]; }) }
        var ctx = { worksheet: "Отчет", table: body }
        uri = uri + b64(format(template, ctx))
        //window.open(uri, "_blank");
        //var now = new Date();
        //var formated_date = now.format("dd-mm-yyyy HH-MM-ss");
        var formated_date = "";
        var fileName = "Отчет" + formated_date + ".xls";
        saveAs(uri, fileName);
    };

    var dates = {};
    var getDates = function (type) {
        return dates[type];
    };

    var setDates = function (type, date) {
        dates[type] = date;
    };

    return {
        all: all,
        build: build,
        save: save,
        exportToPdf: exportToPdf,
        exportToXls: exportToXls,
        sendMailToPdf: sendMailToPdf,
        getById: getById,
        getDates: getDates,
        setDates: setDates
    }
})
/**
 * биндинг нативного HTML, с поддержкой скриптов и т.п.
 */
.directive("html", function ($compile) {
    return function ($scope, element, attrs) {
        $scope.$watch(attrs.html, function (newValue, oldValue) {
            if (newValue) {
                element.html(newValue);
                $compile(element.contents())($scope);
            }
        });
    }
});