angular.module("app");

app.controller("CalculatorCtrl", function ($scope, $log, $sce, $reports, $filter, $modal, $parse, metaSvc, $timeout, $list, $settings, $transport) {

    var contractHour = 10;

    var data = $scope.$parent.window.data;  /* .reportId - открыть отчёт, .ids - выбранные объекты, .header, .start .end */

    var ids = $list.getSelectedIds();

    var dt = new Date();
    var dtNow = new Date();
    dt.setMonth(dt.getMonth() - 1);
    var model = {
        window: $scope.$parent.window,
        only1: false, //кнопка боковой панели
        showAll: true, //функция показать все (...)
        isLoaded: false,
        data: data,
        modal: undefined,
        isResult: false,
        isMyEnterprise: false,
        isSystemEnterprise: (ids.length > 0) ? true : false,
        isThisYear: true,
        month: dtNow.getMonth(),
        isCat2DN: false,
        message: ""
    };
    
    model.savePdf = function () {
        google.charts.load('current', { 'packages': ['corechart'] });
        google.charts.setOnLoadCallback(drawChartForPrint);
    }

    var model2 = {
        enterprises: (ids.length > 0) ? "systemEnterprise" : "",
        year: dt.getFullYear().toString(),
        month: dt.getMonth().toString(),
        region: "",
        provider: "",
        contract: "0",
        ratio: "1000",
        voltageLevel: "hh",
        maxPower: "150",
        timeInMeter: "local",
        currentPriceCategory: "3",
        planningError: "20",
        objectIds: [],
        file: null
    };

    $transport.send(new Message({ what: "calculator-get-parameters" }, { })).then(function (message) {
        if (message.head.what != "calculator-get-parameters") {

            model.isLoaded = true;
            return;
        }

        model.regions = message.body.regions;
        model.dates = message.body.dates;
        if (model2.enterprises == "systemEnterprise") {
            model2.region = "bashkortostan";
            for (var i = 0; i < model.regions.length; i++) {
                if (model.regions[i].tag == model2.region) {
                    model.providers = model.regions[i].providers;
                    model2.provider = (model.providers.length > 1) ? "" : model.providers[0].id;
                }
            }
        }

        var years = [];
        var lastYear = 0; 
        for (var i = 0; i < model.dates.length; i++) {
            var tmpDate = new Date(model.dates[i]);
            if (!years.includes(tmpDate.getFullYear().toString())) {
                years.push(tmpDate.getFullYear().toString());
            }
            if (lastYear < tmpDate.getFullYear()) {
                lastYear = tmpDate.getFullYear();
            }
        }
        model.years = years;
        model2.year = lastYear.toString();
        var months = [];
        var lastMonth = 0; 
        for (var i = 0; i < model.dates.length; i++) {
            var tmpDate = new Date(model.dates[i]);
            if (model2.year == tmpDate.getFullYear().toString()) {
                var month = {};
                month.number = tmpDate.getMonth();
                month.name = parseMonth(tmpDate.getMonth());
                months.push(month);
                
                if (lastMonth < tmpDate.getMonth()) {
                    lastMonth = tmpDate.getMonth();
                }

            }
        }
        
        model2.month = lastMonth.toString();
        model.months = months;
    });


   

    model.calculate = function () {
        if (model2.enterprises == "systemEnterprise") {
            if (ids == null || ids.length == 0) {
                alert("Выберите объекты из системы");
                return;
            }
            else {
                model2.objectIds = ids;
            }
        }
        if (model2.provider == "") {
            alert("Выберите поставщика");
            return;
        }
        if (model2.contract == "") {
            alert("Выберите тип договора");
            return;
        }
        model.isLoaded = false;
        $transport.send(new Message({ what: "calculator-get-data" }, { data: model2 })).then(function (message) {
            if (message.head.what != "calculator-get-data") {
                model.isLoaded = true;
                return;
            }

            model.message = message.body.message;
            model.categories = message.body.categories;
            model.consumption = message.body.consumption;
            var total = [];
            total.push(model.categories[0].energy);
            model.total = [];
            model.total.push(model.categories[0].energy);
            model.total.push(model.categories[1].energy);
            model.total.push(model.categories[2].energy + model.categories[2].power);
            model.total.push(model.categories[3].energy + model.categories[3].power + model.categories[3].network);
            model.total.push(model.categories[4].energy + model.categories[4].power + model.categories[4].factPlan + model.categories[4].planFact + model.categories[4].vSumPlan + model.categories[4].difFactPlan);
            model.total.push(model.categories[5].energy + model.categories[5].power + model.categories[5].factPlan + model.categories[5].planFact + model.categories[5].vSumPlan + model.categories[5].difFactPlan + model.categories[5].network);

            if (model2.contract == "0" && Number(model2.maxPower) < 670) {
                model.isCat2DN = true;
                model.total.push(model.categories[1].energyDayNight);  //[6]
            } else {
                model.isCat2DN = false;
            }

            model.totalCurrent = model.total[model2.currentPriceCategory - 1];
            model.economy = [];
            model.color = [];
            model.textCur = [];

            for (var i = 0; i < model.total.length; i++) {
                model.economy.push(model.totalCurrent - model.total[i]);
                model.color.push((model.totalCurrent - model.total[i] > 0) ? "#70F365" : "F36565 ");
                model.textCur.push("");
            }
            model.color[model2.currentPriceCategory - 1] = "#6988F8";
            model.textCur[model2.currentPriceCategory - 1] = "\n(действующая)";
            google.charts.load('current', { 'packages': ['corechart'] });
            google.charts.setOnLoadCallback(drawChart);
            model.isLoaded = true;
            model.isResult = true;
        });
    }
    function drawChartForPrint() {

        var view = fillDataForGoogleCarts();
        var options = {
            width: 1250,
            height: 300,
            legend: { position: "none" },
            vAxis: {
                minValue: 0,
                title: '',
                titleTextStyle: {
                    color: 'blue'
                }
            },
        };
        var chart = new google.visualization.ColumnChart(document.getElementById('chart_div_calculator'));
        chart.draw(view, options);
        var innerContents = document.getElementById('result-calculator').innerHTML;

        var report = "<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><style> @media print { hr { page-break-after: always; }} .report table { width: 100%; border-collapse: collapse;} .report th { text-align: center; padding: 5px; border: none;} .report td {text-align: center;padding: 5px; border: none; }</style></head><body><div class='report'>"
            + innerContents
            + "</body></html>";
        $reports.exportToPdf(report, true);

        var options = {
            height: 300,
            legend: { position: "none" },
            vAxis: {
                minValue: 0,
                title: '',
                titleTextStyle: {
                    color: 'blue'
                }
            },
        };
        var chart = new google.visualization.ColumnChart(document.getElementById('chart_div_calculator'));
        chart.draw(view, options);
    }
    function drawChart() {

        var view = fillDataForGoogleCarts();
        var options = {
            height: 300,
            legend: { position: "none" },
            vAxis: {
                minValue: 0,
                title: '',
                titleTextStyle: {
                    color: 'blue'
                }
            },
        };
        var chart = new google.visualization.ColumnChart(document.getElementById('chart_div_calculator'));
        chart.draw(view, options);
    }

    function fillDataForGoogleCarts() {
        var data;
        if (model.isCat2DN) {
            data = google.visualization.arrayToDataTable([
                ["Категории", "", { role: "style" }],
                ["Первая" + model.textCur[0], model.total[0] / model.consumption, model.color[0]],
                ["Вторая\n(пик, полупик, ночь)" + model.textCur[1], model.total[1] / model.consumption, model.color[1]],
                ["Вторая\n(день, ночь)" + model.textCur[6], model.total[6] / model.consumption, model.color[6]],
                ["Третья" + model.textCur[2], model.total[2] / model.consumption, model.color[2]],
                ["Четвёртая" + model.textCur[3], model.total[3] / model.consumption, model.color[3]],
                ["Пятая" + model.textCur[4], model.total[4] / model.consumption, model.color[4]],
                ["Шестая" + model.textCur[5], model.total[5] / model.consumption, model.color[5]]
            ]);
        } else {
            data = google.visualization.arrayToDataTable([
                ["Категории", "", { role: "style" }],
                ["Первая" + model.textCur[0], model.total[0] / model.consumption, model.color[0]],
                ["Вторая" + model.textCur[1], model.total[1] / model.consumption, model.color[1]],
                ["Третья" + model.textCur[2], model.total[2] / model.consumption, model.color[2]],
                ["Четвёртая" + model.textCur[3], model.total[3] / model.consumption, model.color[3]],
                ["Пятая" + model.textCur[4], model.total[4] / model.consumption, model.color[4]],
                ["Шестая" + model.textCur[5], model.total[5] / model.consumption, model.color[5]]
            ]);
        }

        var view = new google.visualization.DataView(data);
        view.setColumns([0, 1,
            {
                calc: "stringify",
                sourceColumn: 1,
                type: "string",
                role: "annotation"
            },
            2]);
        return view;
    }
    function parseMonth (num) {
        switch (num) {
            case 0:
                return "Январь";
            case 1:
                return "Февраль";
            case 2:
                return "Март";
            case 3:
                return "Апрель";
            case 4:
                return "Май";
            case 5:
                return "Июнь";
            case 6:
                return "Июль";
            case 7:
                return "Август";
            case 8:
                return "Сентябрь";
            case 9:
                return "Октябрь";
            case 10:
                return "Ноябрь";
            case 11:
                return "Декабрь";
            default:
                return "";
        }
    }

    model.selectEnterprises = function () {
        if (model2.enterprises == "mine") {
            model.isMyEnterprise = true;
            model.isSystemEnterprise = false;
        } else if (model2.enterprises == "systemEnterprise") {
            model.isMyEnterprise = false;
            model.isSystemEnterprise = true;
        } else {
            model.isMyEnterprise = false;
            model.isSystemEnterprise = false;
        }
    }

    model.selectRegion = function () {
        if (model2.region == "") {
            model.providers = null;
            model2.provider = "";
        } else {
            for (var i = 0; i < model.regions.length; i++) {
                if (model2.region == model.regions[i].tag) {
                    model.providers = model.regions[i].providers;
                    model2.provider = "";
                }
            }
        }
    }
    model.selectYear = function () {
        var months = [];
        var lastMonth = 0;
        for (var i = 0; i < model.dates.length; i++) {
            var tmpDate = new Date(model.dates[i]);
            if (model2.year == tmpDate.getFullYear().toString()) {
                var month = {};
                month.number = tmpDate.getMonth();
                month.name = parseMonth(tmpDate.getMonth());
                months.push(month);

                if (lastMonth < tmpDate.getMonth()) {
                    lastMonth = tmpDate.getMonth();
                }
            }
        }
        model2.month = lastMonth.toString();
        model.months = months;
    }

    model.back = function () {
        model.isResult = false;
    }

    model.modalOpen = function (newData) {

        if (model.isResult) {
            google.charts.load('current', { 'packages': ['corechart'] });
            google.charts.setOnLoadCallback(drawChart);
        }

        model.modal = $modal.open({
            templateUrl: model.window.modalTemplateUrl,
            windowTemplateUrl: model.window.windowTemplateUrl,
            size: 'md',
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

    model.isLoaded = true;
    $scope.model = model;
    $scope.model2 = model2;
});