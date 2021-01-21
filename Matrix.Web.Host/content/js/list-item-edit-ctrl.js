angular.module("matrix")
.controller("listItemEditCtrl", function ($scope, $graph, $rootScope, $log, $helper, $drivers, $modems, $mxModal, $templateCache, $http) {

    $scope.loadComplete = false;

    //$scope.data = { end: null };

    $scope.resize = function (size) {
        $scope.height = size.h;
        $scope.width = size.w;
    };

    $graph.getGraph($scope.data.tubeId).then(function (message) {
        $scope.loadComplete = true;
        var elements = [];
        for (var i = 0; i < message.body.elements.length; i++) {
            elements.push(wrap(message.body.elements[i]));
        }
        $scope.elements = elements;
    });

    /**
     * все декораторы прячем в свойствет w, 
     * для далнейшей очистки при отправке на сервер
     */
    var wrap = function (item) {
        item.w = {};
        item.w.dirty = false;
        return item;
    };

    var unwrap = function (item) {
        delete item.w
        //delete item.candidates;
        delete item.target;
        delete item.source;
        //delete item.isNew;
        return item;
    };

    $scope.delete = function (element) {
        $graph.remove(element.id).then(function (ans) {
            for (var i = 0; i < $scope.elements.length; i++) {
                if ($scope.elements[i].id === element.id) {
                    $scope.elements.splice(i, 1);
                    return;
                }
            }
        });
    }

    /**
     * добавить узел в граф
     * возможны варианты:
     * 1. вставка нового узла (тогда добавляются новое соединение и новый узел)
     * 2. вставка существующего объеста (грозди) (создается новое соединение и запрашивается гроздь)
     */
    $scope.add = function (start, end) {
        $helper.createGuid(2).then(function (guidMsg) {
            var guid = guidMsg.body.guids[0];
            var relation = {
                id: guid,
                "class": "relation",
                start: start.id,
                end: end.id,                
            };

            if (end.isNew) {
                var node = {
                    id: end.id,
                    type: end.type,
                    "class": "node"
                };
                node.id = guidMsg.body.guids[1];
                relation.end = node.id;
                var wNode = wrap(node);
                wNode.w.dirty = true;
                $scope.elements.push(wNode);
            } else {
                $graph.getBranch(end.id).then(function (branchItems) {
                    skip:
                        for (var i = 0; i < branchItems.body.elements.length; i++) {
                            var branchItem = branchItems.body.elements[i];
                            for (var j = 0; j < $scope.elements.length; j++) {
                                var oldItem = $scope.elements[j];
                                if (oldItem.id === branchItem.id) continue skip;
                            }
                            $scope.elements.push(wrap(branchItem));
                        }
                });
            }

            var wRel = wrap(relation);
            wRel.w.dirty = true;
            $scope.elements.push(wRel);
        });
    };

    var typesMap = {
        "Tube": ["CsdConnection", "MatrixConnection", "MatrixSwitch", "ComConnection", "LanConnection"],
        "MatrixSwitch": ["MatrixConnection"],
        "ComConnection": ["ComPortt"],
        "LanConnection": ["LanPort"],
        "LanPort": ["SurveyServer"],
        "MatrixConnection": ["MatrixPort"],
        "MatrixPort": ["SurveyServer"],
        "CsdConnection": ["CsdPort"],
        "CsdPort": ["SurveyServer"],
        "Area": ["Tube"],
        "Folder": ["Area", "Folder"]
    };

    /**
     * каллбак срабатывающий при выделении элемента (узла или дуги) в графе
     */
    $scope.onSelect = function (selected) {
        $scope.selected = selected;
        $scope.data = { end: null };
        $scope.frm.$dirty = $scope.selected.w.dirty;

        if ($scope.selected.type === "Tube" && !$scope.selected.w.deviceTypes) {
            $drivers.short().then(function (dts) {
                $scope.selected.w.deviceTypes = dts.body.drivers;
            });
        }

        if ($scope.selected.type === "CsdPort" && !$scope.selected.w.modems) {
            
            $scope.selected.w.editModems = function () {
                $mxModal.show2({
                    tpl: "tpls/modems-list.html",
                    title: "Модемы",
                    data: { id: $scope.selected.id }
                });
            };
        }

        if (!$scope.selected.w.candidates) {
            $graph.getCandidates(typesMap[$scope.selected.type]).then(function (candidates) {
                $scope.selected.w.candidates = candidates.body.items;
                var seenTypes = {};
                var newGuidCount = 0;
                for (var i = 0; i < $scope.selected.w.candidates.length; i++) {
                    var candidate = $scope.selected.w.candidates[i];
                    if (!seenTypes[candidate.type]) {
                        seenTypes[candidate.type] = true;
                        newGuidCount++;
                    }
                }
                for (var type in seenTypes) {
                    $scope.selected.w.candidates.push({
                        id: "",
                        type: type,
                        "class": "node",
                        name: "<новый " + type + ">",
                        isNew: true
                    });
                }
                $scope.candidates = candidates.body.items;
            });
        } else {
            $scope.candidates = $scope.selected.w.candidates;
        }
    };

    //вынес отдельно из-за бага в комбобоксе
    $scope.candidates = [];

    /**
     * сохранение "грязных" элементов
     */
    $scope.save = function () {
        var elements = [];
        for (var i = 0; i < $scope.elements.length; i++) {
            var element = $scope.elements[i];
            if (element.w.dirty) {
                elements.push(unwrap(element));
            }
        }
        if (elements.length > 0) {
            $graph.save(elements).then(function () {
                $log.debug("сохранение элементов прошло успешно");
            });
        }
    }

    /**
     * используется для декорирования групп в контроле ui-select
     */
    $scope.groupBy = function (item) {
        switch (item.type) {
            case "Tube": return "Вычислители";
            case "CsdConnection": return "Модемы";
            case "ComConnection": return "Ком порты";
            case "MatrixConnection": return "Контроллеры Матрикс";
            case "MatrixSwitch": return "Свитчи Матрикс";
            case "CsdPort": return "Модемные пулы";
            case "MatrixPort": return "Порты Матрикс";
            case "LanPort": return "Порты";
            case "SurveyServer": return "Серверы опроса";
            default: return item.type;
        }
    };
});