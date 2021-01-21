angular.module("app")

.controller("MaquetteCtrl", function ($scope, $modal, $transport, $helper, maquetteSvc, $timeout, $filter, $log, $actions) {

    var data = $scope.$parent.window.data;

    var model = {
        window: $scope.$parent.window,
        modal: undefined, 
        isSend: false,
        successSend: false,
        errorSend: "",
        //
        only1: false, //кнопка боковой панели
        showAll: false, //функция показать все (...)
        enable1: false,
        days: []
    }

    var daysSents = {};

    ////

    $actions.get("maquette-edit").then(function (a) {
        if (a) {
            model.actionEdit = function (id) {
                a.act({ id: id }).finally(function () {
                    model.reset();
                });
            };
        }
    });

    ////

    $scope.$watch('model.days', function () {
        //if (model.selected) {
        if (model.day) {
            delete model.day;
        }
        //if (model.days) {
        //    model.enable1 = (model.selected.days.length > 0);
        //}
        //}
    }, true);

    model.addDate = function () {
        if (!model.day) return;
        var date = new Date(model.day);
        for (var i = 0; i < model.days.length; i++) {
            var d = model.days[i];
            if (d.toString() == date.toString()) break;
        }
        if (i == model.days.length) {
            model.days.push(date);
        } else {
            model.removeDate(date);
        }
        //delete model.selectedDate;
    }

    model.removeDate = function (date) {
        for (var i = 0; i < model.days.length; i++) {
            var d = model.days[i];
            if (d.toString() == date.toString()) break;
        }
        if (i != model.days.length) {
            model.days.splice(i, 1);
        }
        //$scope.$apply();
    }

    var wrap = function (obj) {
        if (!obj.daysSent) {
            if (!daysSents[obj.id]) {
                daysSents[obj.id] = [];
            }
            obj.daysSent = daysSents[obj.id];
        }
        obj.selectable = !obj.disable;
        obj.send = function () {
            if (model.days.length == 0) return;
            var days = [];
            for (var i = 0; i < model.days.length; i++) {
                var d = model.days[i];
                days.push(d.toJSON());
            }
            obj.isSending = true;
            maquetteSvc.send(obj.id, days).then(function (message) {
                if (message.body.success) {
                    model.isSend = true;
                    model.errorSend = message.body.error;
                    model.successSend = true;
                } else {
                    model.isSend = true;
                    model.errorSend = message.body.error;
                    model.successSend = false;
                }
                for (var i = 0; i < model.days.length; i++) {
                    var day = model.days[i];
                    obj.daysSent.push(day);
                }
                //model.days.length = 0;
                obj.isSending = false;

                //init();
            }, function () {
                obj.isSending = false;
            });
        }
        obj.getDayClass = function (date, mode) {
            if (mode === 'day') {
                var day = new Date(date.setHours(0, 0, 0, 0)).toJSON();
                var isSelected = false;
                var isSent = false;
                //for send
                for (var i = 0; i < model.days.length; i++) {
                    var d = model.days[i];
                    if (d.toJSON() == day) break;
                }
                if (i != model.days.length) {
                    isSelected = true;
                }
                //already sent
                for (var i = 0; i < obj.daysSent.length; i++) {
                    var d = obj.daysSent[i];
                    if (d.toJSON() == day) break;
                }
                if (i != obj.daysSent.length) {
                    isSent = true;
                }

                if (isSelected && isSent) {
                    return 'selectedsent'
                }
                if (isSelected) {
                    return 'selected';
                }
                if (isSent) {
                    return 'sent';
                }
            }

            return '';
        }
        return obj;
    }

    var init = function () {
        if (model.selected && model.selected.id) {//save selected
            model.selectedId = model.selected.id;
            delete model.selected;
        }
        delete model.objs;
        //
        model.enable1 = false;

        maquetteSvc.all().then(function (answer) {
            model.objs = [];
            for (var i = 0; i < answer.body.maquettes.length; i++) {
                var m = answer.body.maquettes[i];
                if (m.isHidden) continue;
                model.objs.push(wrap(m));
            }
            model.sorted = $filter('orderBy')(model.objs, 'name');

            //select
            if (model.objs.length == 0) {
                //none
            } else if (model.objs.length == 1) {
                if (model.objs[0].selectable) {
                    model.selected = model.objs[0];
                }
            } else if (model.selectedId) {//restore selected
                for (var i = 0; i < model.objs.length; i++) {
                    var d = model.objs[i];
                    if (d.id == model.selectedId) {
                        model.selected = d;
                        break;
                    }
                }
            } else {
                var sel;
                for (var i = 0; i < model.sorted.length; i++) {
                    var r = model.sorted[i];
                    if (r.selectable) {
                        sel = r;
                        break;
                    }
                }
                if (sel) {
                    for (var i = 0; i < model.objs.length; i++) {
                        var r = model.objs[i];
                        if (r.id == sel.id) {
                            model.select(model.objs[i], false);
                            break;
                        }
                    }
                }
            }
        });

    };

    model.select = function (m) {
        if (m.selectable) {
            model.selected = m;
            model.isSend = false;
            //init();
        }
    };

    model.toggleSideList = function () {
        model.only1 = !model.only1;
    }

    init();


    model.reset = function () {
        init();
    }


    // открытие-закрытие окна
    model.modalOpen = function () {
        model.modal = $modal.open({
            templateUrl: model.window.modalTemplateUrl,
            windowTemplateUrl: model.window.windowTemplateUrl,
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

    model.window.open = model.modalOpen;

    model.close = function () {
        model.modal.close();
        model.window.close();
    }

    model.modalOpen();

    $scope.model = model;
});