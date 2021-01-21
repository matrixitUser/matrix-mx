angular.module("app")

.controller("DriversCtrl", function ($scope, $transport, $drivers, $helper, $uibModalInstance, $timeout, $filter, $log, $q, data) {

    var model = {
        only1: false,
        editedCounter: 0,
        lastErr: ""
    };

    model.references = [
        { text: "Нет", value: "Common" },
        { text: "Электричество", value: "Energy" },
        { text: "Вода", value: "Water" },
        { text: "Тепло", value: "Heat" },
        { text: "Газ", value: "Gas" }
    ];

    model.calcs = [
        { text: "Нет", value: "normal" },
        { text: "Итого", value: "total" }
    ];

    model.dataTypes = [
        { text: "Часы", value: "Hour" },
        { text: "Сутки", value: "Day" }
    ];

    ////

    var tagKeys = ["name", "parameter", "calc", "dataType"];

    model.checkField = function (data) {
        if (data == '') {
            return "Имя не введено!";
        }
    }

    model.checkTag = function (data) {
        if (data == '') {
            return "Имя не введено!";
        }
    }

    model.showCalc = function(tag) {
        var selected = [];
        if (tag.calc) {
            selected = $filter('filter')(model.calcs, { value: tag.calc });
        }
        return selected.length ? selected[0].text : model.calcs[0].text;
    }

    model.showDataType = function (tag) {
        var selected = [];
        if (tag.dataType) {
            selected = $filter('filter')(model.dataTypes, { value: tag.dataType });
        }
        return selected.length ? selected[0].text : model.dataTypes[0].text;
    }
    

    model.deviceNewId = $q.when(null);

    if (data && data.deviceId) {
        model.selectedId = data.deviceId;
        model.only1 = true;
    //} else if (data && data.isNew) {
    //    //создание нового драйвера
    //    model.only1 = true;
    //    model.deviceNewId = $helper.createGuid(1).then(function (message) {
    //        var guids = message.body.guids;
    //        if ($helper.isArray(guids) && guids.length > 0) {
    //            model.selectedId = guids[0];
    //            return guids[0];
    //        }
    //        return null;
    //    });
    }

    var wrap = function (driver) {

        //было: driver = {id, name, ?filename, ?uploadDate, driver='text in base64'}
        //стало: driver = {id, name, driver='text in base64', *filename, *uploadDate, *._filesize, *edit: {name}}
        driver._edit = {};

        //

        driver._fields = [];
        if (driver.fieldNames) {
            for (var i = 0; i < driver.fieldNames.length; i++) {
                var name = driver.fieldNames[i];
                var capt = driver.fieldCaptions && (i < driver.fieldCaptions.length) ? driver.fieldCaptions[i] : "";
                var descr = driver.fieldDescriptions && (i < driver.fieldDescriptions.length) ? driver.fieldDescriptions[i] : "";
                driver._fields.push({ name: name, caption: capt, description: descr });
            }
        }

        delete driver.fieldNames;
        delete driver.fieldCaptions;
        delete driver.fieldDescriptions;

        driver._addField = function () {
            driver._edit._fields.push({ name: "", caption: "", description: "" });
        }

        driver._deleteField = function (name) {
            for (var i = driver._edit._fields.length; i > 0; i--) {
                var field = driver._edit._fields[i - 1];
                if (field.name == name) {
                    driver._edit._fields.splice(i - 1, 1);
                }
            }
        }

        driver._checkFields = function () {
            if (driver._edit._fields.length > 0) {
                for (var i = driver._edit._fields.length; i > 1; i--) {
                    var field = driver._edit._fields[i - 1];
                    var found = false;
                    if (field.name != "") {
                        for (var j = 0; j < (i - 1) ; j++) {
                            var orig = driver._edit._fields[j];
                            if (field.name == orig.name) {
                                found = true;
                                break;
                            }
                        }
                    }
                    //no duplicate names
                    if (field.name == "" || found) {//to delete
                        driver._edit._fields.splice(i - 1, 1);
                    }
                }
                //}

                if (driver._edit._fields[0].name == "") {
                    driver._edit._fields.splice(0, 1);
                }
            }
        }

        driver._reLoadFields = function () {
            driver._edit._fields = [];
            for (var i = 0; i < driver._fields.length; i++) {
                var current = driver._fields[i];
                var target = {};
                $helper.copyToFrom(target, current, ["name", "caption", "description"]);
                driver._edit._fields.push(target);
            }
        }

        driver._reLoadFields();

        //

        driver._tags = [];
        if (driver.tags) {
            for (var i = 0; i < driver.tags.length; i++) {
                var tag = driver.tags[i];
                driver._tags.push(JSON.parse(tag));
            }
        }

        delete driver.tags;

        driver._addTag = function () {
            driver._edit._tags.push({ name: "", parameter: "", calc: "", dataType: "" });
        }

        driver._deleteTag = function (name) {
            for (var i = driver._edit._tags.length; i > 0; i--) {
                var tag = driver._edit._tags[i - 1];
                if (tag.name == name) {
                    driver._edit._tags.splice(i - 1, 1);
                }
            }
        }

        driver._checkTags = function () {
            if (driver._edit._tags.length > 0) {
                for (var i = driver._edit._tags.length; i > 1; i--) {
                    var tag = driver._edit._tags[i - 1];
                    var found = false;
                    if (tag.name != "") {
                        for (var j = 0; j < (i - 1) ; j++) {
                            var orig = driver._edit._tags[j];
                            if ((tag.name == orig.name) && (tag.dataType == orig.dataType)) {
                                found = true;
                                break;
                            }
                        }
                    }
                    //no duplicate names
                    if (tag.name == "" || found) {//to delete
                        driver._edit._tags.splice(i - 1, 1);
                    }
                }
                //}

                if (driver._edit._tags[0].name == "") {
                    driver._edit._tags.splice(0, 1);
                }
            }
        }

        driver._reLoadTags = function () {
            driver._edit._tags = [];
            for (var i = 0; i < driver._tags.length; i++) {
                var current = driver._tags[i];
                var target = {};
                $helper.copyToFrom(target, current, tagKeys);
                driver._edit._tags.push(target);
            }
        }

        driver._fillTagsFromResource = function () {
            if (driver._edit.reference == "Gas") {
                driver._edit._tags = [];
                for (var i = 0; i < metaSvc.gazResource.length; i++) {
                    var resTag = metaSvc.gazResource[i];
                    var tag = {};
                    $helper.copyToFrom(tag, resTag, tagKeys);
                    driver._edit._tags.push(tag);
                }
            }
        }

        driver._reLoadTags();

        //

        $helper.copyToFrom(driver._edit, driver, ["name", "reference", "isFilter"]);

        //

        driver.filename = driver.filename || driver.name;
        driver.uploadDate = driver.uploadDate || "н/д";
        driver._filesize = driver.driver ? (driver.driver.length * 0.75) : 0;

        driver._showReference = function () {
            var selected = $filter('filter')(model.references, { value: driver._edit.reference });
            return (driver._edit.reference && selected.length) ? selected[0].text : 'н/д';
        };

        return driver;
    }

    var unwrap = function (driver) {
        //было: driver = {id, name, *filename, !._filesize, *driver='text in base64', !file: {filename, ._filesize, base64}, !edit: {name}}
        if (driver._file) {
            driver.driver = driver._file.base64;
            driver.filename = driver._file.filename;
            delete driver._file;
        }

        if (driver._edit) {
            driver.name = driver._edit.name;
            driver.reference = driver._edit.reference;
            driver.isFilter = driver._edit.isFilter;
            driver._fields = driver._edit._fields;
            driver._tags = driver._edit._tags;
            delete driver._edit;
        }

        //обновление даты
        driver.uploadDate = $filter('date')(new Date(), "dd.MM.yyyy HH:mm:ss");

        //восстановление fields: fieldNames, fieldCaptions, fieldDescriptions
        driver.fieldNames = [];
        driver.fieldCaptions = [];
        driver.fieldDescriptions = [];
        for (var i = 0; i < driver._fields.length; i++) {
            var field = driver._fields[i];
            if (!field.name || field.name == "") continue;

            driver.fieldNames.push(field.name);
            driver.fieldCaptions.push(field.caption || "");
            driver.fieldDescriptions.push(field.description || "");
        }

        //восстановление tags
        driver.tags = []; 
        for (var i = 0; i < driver._tags.length; i++) {
            var tag = driver._tags[i];
            if (!tag.name || tag.name == "") continue;
            driver.tags.push(JSON.stringify(tag));
        }

        for (var key in driver)
        {
            if((key !== undefined) && driver.hasOwnProperty(key) && key.startsWith("_"))
            {
                delete driver[key];
            }
        }

        /*
        //удаление параметров
        delete driver._filesize;
        delete driver._edited;
        delete driver._editedFields;
        delete driver._isEqual;

        delete driver._fields;
        delete driver._tags;

        //удаление функций
        delete driver._showReference;
        delete driver._addField;
        delete driver._deleteField;
        delete driver._checkFields;
        delete driver._reLoadFields;
        delete driver._addTag;
        delete driver._deleteTag;
        delete driver._checkTags;
        delete driver._reLoadTags;*/

        return driver;
    }

    var fieldsCheckAreEqual = function (f1, f2) {
        if (!$helper.isArray(f1) || !$helper.isArray(f2) || (f1.length != f2.length)) return false;

        for (var i = 0; i < f1.length; i++) {
            var o1 = f1[i];
            for (var j = 0; j < f2.length; j++) {
                var o2 = f2[j];
                if ((o1.name == o2.name) && (o1.caption == o2.caption) && (o1.description == o2.description))
                    break;
            }
            if (j == f2.length) {//not found
                return false;
            }
        }

        return true;
    }

    var checkAreEqualArrays = function (f1, f2, keys) {
        if (!$helper.isArray(f1) || !$helper.isArray(f2) || !$helper.isArray(keys) || (f1.length != f2.length)) return false;

        for (var i = 0; i < f1.length; i++) {
            var o1 = f1[i];
            for (var j = 0; j < f2.length; j++) {
                var o2 = f2[j];
                for (var k = 0; k < keys.length; k++) {
                    if (o1[keys[k]] != o2[keys[k]]) {
                        break;
                    }
                }
                if (k == keys.length)//all params are equal
                    break;
            }
            if (j == f2.length) {//not found
                return false;
            }
        }

        return true;
    }

    $scope.$watch('model.drivers', function () {
        model.editedCounter = 0;
        if (!model.drivers) return;
        for (var i = 0; i < model.drivers.length; i++) {
            var driver = model.drivers[i];

            driver._editedFields = !fieldsCheckAreEqual(driver._fields, driver._edit._fields);

            driver._editedTags = !checkAreEqualArrays(driver._tags, driver._edit._tags, tagKeys);

            driver._edited =
                (driver.name != driver._edit.name)
                || (driver.reference != driver._edit.reference)
                || (driver.isFilter != driver._edit.isFilter)
                || driver._editedFields
                || driver._editedTags
                || (!!driver._file);

            if (driver._edited) {//выбран новый драйвер
                model.editedCounter++;
            }
        }
    }, true);

    $scope.$watch('model.selected', function () {
        if (model.selected && model.selected._file) {
            model.selected._isEqual = (model.selected.driver === model.selected._file.base64);
        }
    }, true);

    ////

    var init = function () {
        model.lastErr = "";
        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.drivers;
        //        
        $q.all([$drivers.all(), model.deviceNewId]).then(function (datas) {

            var data = datas[0];
            var newId = datas[1];

            model.drivers = [];
            for (var i = 0; i < data.body.drivers.length; i++) {
                var driver = data.body.drivers[i];
                if (newId && driver.id == newId) newId = null;//проверка на существование
                model.drivers.push(wrap(driver));
            }
            
            if (newId) {
                var newDevice = { id: newId, name: "", driver: "" };
                model.drivers.push(wrap(newDevice));
            }

            model.sorted = $filter('orderBy')(model.drivers, 'name');
            //select
            if (model.drivers.length == 0) {
                //none
            } else if (model.drivers.length == 1)
                model.select(model.drivers[0]);
            else if (model.selectedId) {//restore selected
                for (var i = 0; i < model.drivers.length; i++) {
                    var d = model.drivers[i];
                    if (d.id == model.selectedId) {
                        model.select(d);
                        break;
                    }
                }
            } else {
                //var find = model.sorted[0];
                //for (var i = 0; i < model.drivers.length; i++) {
                //    var d = model.drivers[i];
                //    if (d.id == find.id) {
                //        model.select(d);
                //        break;
                //    }
                //}
            }
        });
    };

    


    model.select = function (driver) {
        model.selected = driver;
    };

    model.cancelUpload = function (driver) {
        delete driver._file;
    }

    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    init();

    model.create = function () {
        var name = prompt('Название вычислителя', 'New device');
        if (name != null) {
            $drivers.create(name).then(function () { });
        }
    }


    model.save = function () {
        if (!model.drivers) return;
        var drivers = [];
        for (var i = 0; i < model.drivers.length; i++) {
            var driver = model.drivers[i];
            if (driver._edited) {//выбран новый драйвер
                drivers.push(unwrap(driver));
            }
        }
        if (drivers.length > 0) {
            model.drivers.length = 0;
            $drivers.save(drivers).then(function () {
                init();
            }, function (err) {
                model.lastErr = err;
            });
        }
    }

    model.reset = function (driver) {
        init();
    }

    model.close = function () {
        $uibModalInstance.close();
    }

    $scope.model = model;
});