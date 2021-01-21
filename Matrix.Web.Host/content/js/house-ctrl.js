angular.module("app")
.controller("HouseCtrl", function ($rootScope, $scope, $log, $modal, $list, $parse, $transport, $helper, $filter, $timeout, $actions, $q, $reports, metaSvc) {//, data

    $scope.Math = window.Math;

    function getNow()
    {
        return new Date();
    }
    
    var data = $scope.$parent.window.data;
    if (!data || !data.id) $scope.$parent.window.close();
    var id = data.id;

    var model = {
        window: $scope.$parent.window,
        modal: undefined,
        view: "values",
        //
        Section: [],
        sections: 0,
        //
        //Tube: [],
        ids: [],
        wids: [],
        eids: [],
        //
        overlay: $helper.overlayFunc,
        overlayEnabled: true,
        viewDisabled: { values: undefined, odn: undefined },
        overlayText: ""
    }

    //коренной элемент с указанием количества подъездов, этажей на подъездах 
    $list.getRows([id]).then(function (rows) {
        var houseRoot;
        var parentCandidate = rows[0];

        if (parentCandidate && parentCandidate.class == "HouseRoot") {
            houseRoot = parentCandidate;
        }

        //постройка виртуального дома
        if (houseRoot) {

            //section=1,2,3...
            if (!houseRoot.sections) {
                houseRoot.sections = 1;
            }

            //floors=[floors of sect.1, floors of sect.2 etc]
            if (!houseRoot.floors) {
                houseRoot.floors = 1;
            }

            var floors;
            if (!$helper.isArray(houseRoot.floors)) {
                floors = houseRoot.floors;
            } else if (houseRoot.floors.length == 0) {
                floors = 1;
            } else {
                floors = houseRoot.floors[0];
            }

            if (floors) {
                houseRoot.floors = [];
                for (var s = 0; s < houseRoot.sections; s++) {
                    houseRoot.floors.push(floors);
                }

            }

            //house structure { Section: [{ index: 1, Floor: [{ index: 1, Apt: [{ index: 1, ...}, { index: 2, ...}, { index: 3, ...}, { index: 4, ...}, ] }, {}, {}, ...] }, {}, ...] }
            //var apt_index = 1;
            var Apt = {};
            var Section = [];
            var square = 0.0;

            for (var s = 0; s < houseRoot.sections; s++) {
                var floors = houseRoot.floors[s];
                //
                var Floor = [];
                for (var i = 0; i < floors; i++) {
                    var floor = { index: i + 1, Apt: [] };
                    Floor[i] = floor;
                }
                //
                var section = { index: s + 1, Floor: Floor };
                Section.push(section);
            }

            if (houseRoot.apts) {
                var apts = houseRoot.apts.split("|");
                for (var i = 0; i < apts.length; i++) {

                    var apt_param = apts[i].split(":");
                    if (apt_param.length < 3) continue;

                    var apt_section = apt_param[0];
                    var apt_floor = apt_param[1];
                    var apt_index = apt_param[2];
                    var apt_counters = apt_param[3];
                    var apt_square = parseFloat(apt_param[4]);

                    if (apt_section > Section.length) continue;
                    if (apt_floor > Section[apt_section - 1].Floor.length) continue;

                    var apt = {
                        index: apt_index,
                        values: {
                            energy: "",
                            cw: (apt_counters > 0 ? "" : undefined),
                            hw: (apt_counters > 0 ? "" : undefined),
                            cw2: (apt_counters > 1 ? "" : undefined),
                            hw2: (apt_counters > 1 ? "" : undefined),
                        },
                        odn: {
                            energy: "",
                            cw: (apt_counters > 0 ? "" : undefined),
                            hw: (apt_counters > 0 ? "" : undefined),
                        },

                        S: apt_square,
                        percent: function () {
                            return (this.S && houseRoot.S) ? this.S / houseRoot.S : 0;
                        },
                        view: function () {
                            return this[model.view];
                        }

                    };

                    Apt[apt_index] = apt;
                    Section[apt_section - 1].Floor[apt_floor - 1].Apt.push(apt);
                    square += apt_square;
                }
            }

            if (!houseRoot.S) {
                houseRoot.S = square;
            }

            model.sections = houseRoot.sections;

            model.Apt = Apt;
            model.Section = Section;

            model.csection = Section.length > 0 ? Section[0] : undefined;

            // объекты, которые считывают показания по дому (по квартирам дома)
            if ($helper.isArray(houseRoot.Tube) && houseRoot.Tube.length > 1) {

                for (var i = 0; i < houseRoot.Tube.length; i++) {
                    var tube = houseRoot.Tube[i];

                    if (tube && tube.id) {
                        if (!tube.class) {
                            model.ids.push(tube.id);
                        } else if (tube.class == "HouseWater") {
                            model.wids.push(tube.id);
                        } else if (tube.class == "HouseEnergy") {
                            model.eids.push(tube.id);
                        }
                    }
                }

            }
        }

        function processAptMessage(message, type) {
            for (var i = 0; i < message.body.data.length; i++) {
                var record = message.body.data[i];
                for (var key in record) {
                    if (record.hasOwnProperty(key)) {//example: record["Кв10_ЭЭ"] = 00001
                        var par = key.split("_");
                        if (par[0].slice(0, 2) == "Кв" && par.length > 1) {//startswith Кв: yes, par[0]=Кв10,par[1]=ЭЭ
                            var apt_index = par[0].slice(2);
                            var apt = model.Apt[apt_index];

                            var value = record[key];//$filter('number')(, 2);

                            if (apt) {
                                var aptdata = apt[type];
                                if (!aptdata) {
                                    aptdata = {};
                                    apt[type] = aptdata;
                                }

                                switch (par[1]) {
                                    case "ЭЭ":
                                        aptdata.energy = value;
                                        break;
                                    case "ХВС":
                                        aptdata.cw = value;
                                        break;
                                    case "ГВС":
                                        aptdata.hw = value;
                                        break;
                                    case "ХВС2":
                                        aptdata.cw2 = value;
                                        break;
                                    case "ГВС2":
                                        aptdata.hw2 = value;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        model.houseRoot = houseRoot;

        model.init = function () {

            var now = getNow();

            if (model.view != "values") {
                model.view = "values";
                if (model.viewDisabled["values"] !== undefined) {
                    return;
                }
            }

            if (model.viewDisabled["values"] == true)
            {
                return;
            }

            model.viewDisabled["values"] = true;

            var dt = now;
            var dts1 = new Date((new Date(dt)).setHours(dt.getHours() - 18));
            var dte1 = new Date((new Date(dt)).setHours(dt.getHours() + 3));

            //var dts2 = new Date((new Date(dt)).setHours(dt.getHours() - 9));
            //var dte2 = new Date((new Date(dt)).setHours(dt.getHours() - 3));

            //var dts3 = new Date((new Date(dt)).setDate(dt.getDate() - 2));
            //var dte3 = new Date((new Date(dt)).setHours(dt.getHours() - 9));

            return $transport.send(new Message({ what: "export-data" }, { ids: model.ids, type: "Current", start: dts1.toJSON(), end: dte1.toJSON() }))
            .then(function (message) {

                for (var i = 0; i < message.body.data.length; i++) {
                    var record = message.body.data[i];
                    for (var key in record) {
                        if (record.hasOwnProperty(key)) {//example: record["Кв10_ЭЭ"] = 00001
                            var par = key.split("_");
                            if (par[0].slice(0, 2) == "Кв" && par.length > 1) {//startswith Кв: yes, par[0]=Кв10,par[1]=ЭЭ
                                var apt_index = par[0].slice(2);
                                var apt = model.Apt[apt_index];

                                var value = $filter('number')(record[key], 2);
                                var date = record.date ? new Date(record.date) : undefined;
                                var dateStr = "";

                                if (date) {
                                    var today = new Date((now).setHours(0, 0, 0, 0));
                                    var tomorrow = new Date((new Date(today)).setDate(today.getDate() + 1));
                                    var yesterday = new Date((new Date(today)).setDate(today.getDate() - 1));

                                    var day = new Date((new Date(date)).setHours(0, 0, 0, 0));
                                    if (day >= tomorrow || day < yesterday) {
                                        dateStr = $filter('date')(date, 'dd.MM.yy HH:mm');
                                    } else if (day >= today) {
                                        dateStr = "" + $filter('date')(date, 'HH:mm');
                                    } else {
                                        dateStr = "вчера " + $filter('date')(date, 'HH:mm');
                                    }
                                }

                                if (dateStr != "") {
                                    value += "<small style='color: grey'>" + " /" + dateStr + "</small>";
                                }

                                if (apt) {
                                    var val = apt.values;
                                    switch (par[1]) {
                                        case "ЭЭ":
                                            if (val.energy !== undefined) {
                                                val.energy = value;
                                            }
                                            break;
                                        case "ХВС":
                                            if (val.cw !== undefined) {
                                                val.cw = value;
                                            }
                                            break;
                                        case "ГВС":
                                            if (val.hw !== undefined) {
                                                val.hw = value;
                                            }
                                            break;
                                        case "ХВС2":
                                            if (val.cw2 !== undefined) {
                                                val.cw2 = value;
                                            }
                                            break;
                                        case "ГВС2":
                                            if (val.hw2 !== undefined) {
                                                val.hw2 = value;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }


                ////переключение вида
                //for (var key in model.Apt) {
                //    if (model.Apt.hasOwnProperty(key)) {
                //        var apt = model.Apt[key];
                //        if (!apt) continue;
                //        apt.view = apt.values;
                //    }
                //}

                model.viewDisabled["values"] = false;
            })
            .catch(function (err) {
                model.viewDisabled["values"] = false;
            });

        }

        model.refresh = function () {
            model.init();
        }

        model.init();

        model.openReport = function () {
            var dt = now;
            //var dtend = new Date(2016, 1, 25, 8);//(new Date(dt)).setDate(dt.getDate() + 1));
            var dtend = new Date((new Date(dt)).setHours(0, 0, 0, 0));//dt.getHours() - 1
            var dtstartmin = new Date((new Date(dtend)).setHours(dtend.getHours() - 1));
            var dtstart = new Date(new Date((new Date(dtstartmin)).setDate(1)).setHours(0));

            //.reportId - открыть отчёт, .ids - выбранные объекты, .header, .start .end
            $actions.getWrap("report-list").act({ reportId: '', ids: [id], start: dtstart, end: dtend, header: undefined });
        }

        model.init2 = function () {

            var now = getNow();

            if (model.view != "odn") {
                model.view = "odn";
                if (model.viewDisabled["odn"] !== undefined)
                {
                    return;
                }
            }

            if (model.viewDisabled["odn"] == true) {
                return;
            }

            model.viewDisabled["odn"] = true;

            var dt = now;
            //var dtend = new Date(2016, 1, 25, 8);//(new Date(dt)).setDate(dt.getDate() + 1));
            var dtend = new Date((new Date(dt)).setHours(0, 0, 0, 0));//dt.getHours() - 1
            var dtstartmin = new Date((new Date(dtend)).setHours(dtend.getHours() - 1));
            var dtstart = new Date(new Date((new Date(dtstartmin)).setDate(1)).setHours(0));
            
            //сбор данных:
            // квартирные - моменты на начало и конец периода
            // общемдомовой - сумма почасовых потреблений
            return $q.all([
                $transport.send(new Message({ what: "export-data" }, { ids: model.ids, type: "Hour", start: dtstart.toJSON(), end: dtstart.toJSON() })),
                $transport.send(new Message({ what: "export-data" }, { ids: model.ids, type: "Hour", start: dtend.toJSON(), end: dtend.toJSON() })),
                $transport.send(new Message({ what: "export-data" }, { ids: model.wids, type: "Hour", start: dtstart.toJSON(), end: dtend.toJSON() }))
            ])
            .then(function (messages) {
                processAptMessage(messages[0], "start");
                processAptMessage(messages[1], "end");

                var elems = ["cw", "hw"];

                var commons = {
                    period: { start: dtstart, end: dtend },
                    house: { cw: 0.0, hw: 0.0 },
                    apts: { cw: 0.0, hw: 0.0 },
                    //odn: { cw: 0.0, hw: 0.0 }
                    odn: function (elem) {
                        if ((this.house[elem] !== undefined) && (this.apts[elem] !== undefined)) {
                            return (this.house[elem] - this.apts[elem]);
                        }
                    },
                    percent: { cw: undefined, hw: undefined }
                };

                //1. Общедомовой - ХВС,ГВС (от ТСРВ)
                var data = messages[2].body.data;
                for (var i = 0; i < data.length; i++) {
                    var rec = data[i];
                    commons.house.cw += rec["ХВСV1"];
                    commons.house.hw += rec["ГВСV"];
                }
                //sum.cw2 = sum.cw;
                //sum.hw2 = sum.hw;

                //2. Общеквартирный
                for (var key in model.Apt) {
                    if (model.Apt.hasOwnProperty(key)) {
                        var apt = model.Apt[key];
                        if (!apt || !apt.end || !apt.start) continue;

                        var D = {};
                        var st = apt.start;
                        var end = apt.end;

                        for (var i = 0; i < elems.length; i++) {
                            var elem = elems[i];
                            var elem2 = elem + "2";

                            if ((end[elem] !== undefined) && (st[elem] !== undefined)) {
                                D[elem] = end[elem] - st[elem];
                                commons.apts[elem] += D[elem];

                                if ((end[elem2] !== undefined) && (st[elem2] !== undefined)) {
                                    D[elem2] = end[elem2] - st[elem2];
                                    commons.apts[elem] += D[elem2];
                                }
                            }
                        }

                        apt.delta = D;
                    }
                }

                ////3. ОДН 
                //for (var i = 0; i < elems.length; i++) {
                //    var elem = elems[i];
                //    commons.odn[elem] = 
                //}

                //4. Поквартирный
                for (var key in model.Apt) {
                    if (model.Apt.hasOwnProperty(key)) {
                        var apt = model.Apt[key];
                        if (!apt || !apt.delta) continue;

                        var D = apt.delta;
                        var odn = apt.odn;

                        for (var i = 0; i < elems.length; i++) {
                            var elem = elems[i];
                            var elem2 = elem + "2";

                            if (D[elem] !== undefined) {
                                odn[elem] = $filter('number')(D[elem], 2);

                                if (D[elem2] !== undefined) {
                                    odn[elem] += " + " + $filter('number')(D[elem2], 2) + " = " + $filter('number')(D[elem] + D[elem2], 2);
                                }

                                if (commons.odn(elem) !== undefined) {
                                    var odn_perc = commons.odn(elem) * apt.percent();
                                    odn[elem] += ' <small style="color: grey">' + (odn_perc > 0 ? '+' : '') + $filter('number')(odn_perc, 2) + '</small>';
                                }
                            } else {
                                odn[elem] = undefined;
                            }

                            odn[elem2] = undefined;
                        }

                        //apt.view = apt.odn;

                        ////new
                        //if ((apt.end.cw !== undefined) && (apt.start.cw !== undefined)) {
                        //    var cw = apt.end.cw - apt.start.cw;
                        //    apt.cw = $filter('number')(cw, 2);

                        //    if ((apt.end.cw2 !== undefined) && (apt.start.cw2 !== undefined)) {
                        //        var cw2 = apt.end.cw2 - apt.start.cw2;
                        //        apt.cw += " + " + $filter('number')(cw2, 2) + " = " + $filter('number')(cw + cw2, 2);
                        //    }

                        //    if (sum.cw) {
                        //        var perc = sum.cw * apt.percent();
                        //        apt.cw += ' <small style="color: grey">' + (perc > 0 ? '+' : '') + $filter('number')(perc, 2) + '</small>';
                        //    }
                        //} else {
                        //    apt.cw = undefined;
                        //    apt.cw2 = undefined;
                        //}

                        ////old
                        //var elems = ["energy", "cw", "cw2", "hw", "hw2"];
                        //for (var i = 0; i < elems.length; i++) {
                        //    var elem = elems[i];
                        //    if ((apt.end[elem] !== undefined) && (apt.start[elem] !== undefined)) {
                        //        apt[elem] = $filter('number')(apt.end[elem] - apt.start[elem], 2);
                        //        if (sum[elem]) {
                        //            var perc = sum[elem] * apt.percent();
                        //            apt[elem] += ' <small style="color: grey">' + (perc > 0? '+' : '') + $filter('number')(perc, 2) + '</small>';
                        //        }
                        //    } else {
                        //        apt[elem] = undefined;
                        //    }
                        //}
                    }
                }

                commons.percent.cw = (commons.odn("cw") && (commons.odn("cw") > 0) && commons.house.cw) ? (commons.house.cw / commons.odn("cw")) : undefined;
                commons.percent.hw = (commons.odn("hw") && (commons.odn("hw") > 0) && commons.house.hw) ? (commons.house.hw / commons.odn("hw")) : undefined;

                model.commons = commons;
                model.viewDisabled["odn"] = false;
            })
            .catch(function (err) {
                model.viewDisabled["odn"] = false;
            });

        }


        model.select = function (section) {
            model.csection = section;
        }

        //CONTENT-SAVE

        model.savePdf = function () {
            var reportAsHtml = angular.element($('#report-content')).html();
            if (reportAsHtml) {
                $reports.exportToPdf(reportAsHtml);
            }
        };

        model.toExcel = function () {
            var reportAsHtml = angular.element($('#report-content')).html();
            if (reportAsHtml) {
                $reports.exportToXls(reportAsHtml);
            }
        };
        //getDailyData();

        //function refreshWrapper() {
        //    model.refresh();
        //    refresher = $timeout(refreshWrapper, 5 * 60 * 1000);
        //}

        //refreshWrapper();

        model.overlayEnabled = false;

        //modal

        model.modalOpen = function () {
            model.modal = $modal.open({
                templateUrl: model.window.modalTemplateUrl,
                windowTemplateUrl: model.window.windowTemplateUrl,
                size: 'lg',
                scope: $scope
            });
        }

        model.window.open = model.modalOpen;

        model.close = function () {
            model.modal.close();
            model.window.close();
        }

        model.modalOpen();

        //

        $scope.model = model;

        $scope.$on('$destroy', function () {
            //listener();
        });
    }).catch(function (error) {

    });
});