angular.module("app")
.controller("HouseCtrl", function ($rootScope, $scope, $log, $modal, $list, $parse, $transport, $helper, $filter, $timeout, $actions, $q, $reports) {//, data

    //var Cache = function () {
    //    var self = this;
    //    self.data = {};
    //    self.newObject = function (id) {
    //        return { id: id };
    //    }
    //    self.getArray = function () {
    //        var arr = [];
    //        for (var key in self.data) {
    //            if (self.data.hasOwnProperty(key)) {
    //                arr.push(self.data[key]);
    //            }
    //        }
    //        return arr;
    //    }
    //    self.getIds = function () {
    //        var arr = [];
    //        for (var key in self.data) {
    //            if (self.data.hasOwnProperty(key)) {
    //                arr.push(key);
    //            }
    //        }
    //        return arr;
    //    }
    //    self.get = function (id) {
    //        if (!self.data[id]) {
    //            self.data[id] = self.newObject(id);
    //        }
    //        return self.data[id];
    //    }
    //}

    var data = $scope.$parent.window.data;
    var id = data;

    var model = {
        window: $scope.$parent.window,
        modal: undefined,
        //
        Section: [],
        sections: 0,
        //
        Tube: [],
        ids: [],
        //
        overlay: $helper.overlayFunc,
        overlayEnabled: false,
        overlayText: ""
    }

    var houseRoot;

    //коренной элемент с указанием количества подъездов, этажей на подъездах 
    var parentCandidate = $list.getRow(id);
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

        for (var s = 0; s < houseRoot.sections; s++) {
            var floors = houseRoot.floors[s];
            //
            var Floor = [];
            for (var i = 0; i < floors; i++) {
                //var AptAtFloor = [];
                //for (var j = 0; j < houseRoot.aptsAtFloor; j++) {
                //    var apt = {
                //        index: apt_index,
                //        energy: undefined,
                //        cw: undefined,
                //        hw: undefined,
                //        cw2: undefined,
                //        hw2: undefined
                //    };

                //    AptAtFloor.push(apt);
                //    Apt[apt_index] = apt;
                //    apt_index++;
                //}
                //
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
                var apt_index = i + 1;                
                var apt_param = apts[i].split(":");
                if (apt_param.length < 3) continue;

                var apt_section = apt_param[0];
                var apt_floor = apt_param[1];
                var apt_counters = apt_param[2];

                if (apt_section > Section.length) continue;
                if (apt_floor > Section[apt_section - 1].Floor.length) continue;

                var apt = {
                    index: apt_index,
                    energy: "",
                    cw: "",
                    hw: "",
                    cw2: (apt_counters > 1 ? "" : undefined),
                    hw2: (apt_counters > 1 ? "" : undefined)
                };

                Apt[apt_index] = apt;
                Section[apt_section - 1].Floor[apt_floor - 1].Apt.push(apt);
            }
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
                    model.Tube.push(tube);
                    model.ids.push(tube.id);
                }
            }

        }
    }

    model.init = function () {

        var dt = new Date();
        var dtstart = new Date((new Date(dt)).setDate(dt.getDate() - 3));
        var dtend = new Date((new Date(dt)).setDate(dt.getDate() + 1));

        return $transport.send(new Message({ what: "export-data" }, { ids: model.ids, type: "Current", start: dtstart.toJSON(), end: dtend.toJSON() }))
        .then(function (message) {

            //var c = {};

            //for (var i = 0; i < message.body.data.length; i++) {
            //    var record = message.body.data[i];
            //    for (var key in record) {
            //        if (record.hasOwnProperty(key)) {//example: record["Кв10_ЭЭ"] = 00001
            //            if (key == 'date' || key == 'objectId') continue;
            //            if (!c[key]) {
            //                c[key] = { records: [], obj: {}};
            //            }
            //            c[key].value = record[key];
            //            c[key].date = record.date;
            //            c[key].obj[record.objectId] = record[key];
            //            c[key].records.push(record);
            //        }
            //    }
            //}

            for (var i = 0; i < message.body.data.length; i++) {
                var record = message.body.data[i];
                for (var key in record) {
                    if (record.hasOwnProperty(key)) {//example: record["Кв10_ЭЭ"] = 00001
                        var par = key.split("_");
                        if (par[0].slice(0, 2) == "Кв" && par.length > 1) {//startswith Кв: yes, par[0]=Кв10,par[1]=ЭЭ
                            var apt_index = par[0].slice(2);
                            var apt = model.Apt[apt_index];

                            var value = $filter('number')(record[key], 0);
                            var date = record.date ? new Date(record.date) : undefined;
                            var dateStr = "";

                            if (date) {
                                var now = new Date();
                                var today = new Date((new Date()).setHours(0, 0, 0, 0));
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
                                switch (par[1]) {
                                    case "ЭЭ":
                                        if (apt.energy !== undefined) {
                                            apt.energy = value;
                                        }
                                        break;
                                    case "ХВС":
                                        if (apt.cw !== undefined) {
                                            apt.cw = value;
                                        }
                                        break;
                                    case "ГВС":
                                        if (apt.hw !== undefined) {
                                            apt.hw = value;
                                        }
                                        break;
                                    case "ХВС2":
                                        if (apt.cw2 !== undefined) {
                                            apt.cw2 = value;
                                        }
                                        break;
                                    case "ГВС2":
                                        if (apt.hw2 !== undefined) {
                                            apt.hw2 = value;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                // record = [{objectId, date, Кв1_ЭЭ, Кв2_ЭЭ итп}, ...]
                //var tube = cache.get(record.objectId);
                //tube.data.push(record);
            }
            ////sort
            //for (var id in cache.data) {
            //    if (cache.data.hasOwnProperty(id)) {
            //        var tube = cache.data[id];
            //        if ($helper.isArray(tube.data) && tube.data.length > 0) {
            //            tube.data = $filter('orderBy')(tube.data, '-date');
            //            tube.current = tube.data[0];
            //        }
            //    }
            //}
        });

    }

    model.refresh = function () {
        model.init();
    }

    model.init();


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
});