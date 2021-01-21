'use strict';

angular.module("app");

app.controller("FoldersCtrl", function FoldersCtrl($scope, $rootScope, $timeout, $q, $filter, $log,
    $transport,
    $folders, $actions, $listFilter, $drivers) {

    var menuActions = [{
        icon: "/img/folder_add.png",
        title: "Создать подгруппу",
        promise: $actions.get('folder-edit'), getParam: function () { return { id: model.selectedIds.length > 0 ? model.selectedIds[0] : null, isNew: true }; }, isEnabled: function () {
            var sels = $folders.selected();
            return (sels.length > 0) && ((sels[0].type.substring(0, 6) == "Folder") || (sels[0].type == "all"));
        },
        success: function (result) {
            if (result) {
                model.selectFolder($scope.folders[0]);
                $rootScope.$broadcast("folders:changed");
            }
        }
    }, {
        title: "Редактировать группу",
        promise: $actions.get('folder-edit'), getParam: function () { return { id: model.selectedIds[0], isNew: false }; }, isEnabled: function () {
            var sels = $folders.selected();
            return (sels.length > 0) && (sels[0].type.substring(0, 6) == "Folder");
        },
        success: function (result) {
            if (result) {
                model.selectFolder($scope.folders[0]);
                $rootScope.$broadcast("folders:changed");
            }
        }
    }, {
        title: "Редактировать права",
        promise: $actions.get('rights-edit'), getParam: function () { return model.selectedIds[0]; }, isEnabled: function () {
            var sels = $folders.selected();
            return (sels.length > 0) && (sels[0].type.substring(0, 6) == "Folder");
        }
    }, {
        icon: "/img/house_add.png",
        title: "Создать объект учёта",
        promise: $actions.get('row-editor'), getParam: function () { return { folderId: model.selectedIds.length > 0 ? model.selectedIds[0] : null }; }, isEnabled: function () {
            var sels = $folders.selected();
            return (sels.length > 0) && ((sels[0].type.substring(0, 6) == "Folder") || (sels[0].type == "all"));
        },
        success: function (result) {
            if (result) {
                $rootScope.$broadcast("listFilter:changed");
            }
        }
    }];

    var model = {
        selected: $folders.selected(),
        selectedIds: [],
        visible: true,
        filterText: "",
        isLoading: true,

        accordion: {
            folders: {
                enabled: true,
                open: true
            },
            state: {
                enabled: true,
                open: false
            },
            device: {
                enabled: false,
                open: false
            },
        }
    }

    $scope.$watch('model.selected', function () {
        model.selectedIds.length = 0;
        for (var i = 0; i < model.selected.length; i++) {
            var row = model.selected[i];
            model.selectedIds.push(row.id);
        }
    }, true);

    function cellClick(params) {
        //var sel = params.data;
        //sel.selected = true;
        //$scope.rowSelected(sel.id, sel.selected);
        $scope.opt.api.selectNode(params.node, false);
    }

    function innerCellRenderer(params) {
        return ' ';// + params.data.name;
    }


    var foldersLoad = function () {
        model.isLoading = true;

        $scope.folders = [{
            id: "all",
            text: "Все",
            type: "all",
            parent: "#"
        }, {
            text: "Корзина",
            id: "deleted",
            type: "trash",
            parent: "#"
        }];

        //TODO убрать в фолдер-сервис $transport.send(new Message({ what: "folders-get-2" })) + $transport.send(new Message({ what: "edit-get-folders-id" }, { ids: ids }))

        return $transport.send(new Message({ what: "folders-get-2" })).then(function (message) {

            var ids = [];
            var folderNodes = {};

            var root = $filter('orderBy')(message.body.root, 'name');

            for (var i = 0; i < root.length; i++) {
                var node = root[i];
                if (!node.parent) {
                    node.parent = "all";
                }
                //
                if (node.type == "Folder") {
                    ids.push(node.id);
                    folderNodes[node.id] = node;
                }
                //
                node.text = node.name;
                $scope.folders.push(node);
            }

            return $transport.send(new Message({ what: "edit-get-folders-id" }, { ids: ids })).then(function (message) {
                for (var i = 0; i < message.body.folders.length; i++) {
                    var folder = message.body.folders[i];
                    if (folder && folder.Task && folder.Task.length > 0 && folderNodes[folder.id]) {
                        folderNodes[folder.id].type = "FolderTask";
                    }
                }
            });
        })
        .finally(function () {
            model.isLoading = false;
            return true;
        });
    }

    $scope.typesConfig = {
        "Folder": {
            "icon": "/img/16/folder.png"
        },
        "FolderTask": {
            "icon": "/img/16/folder_clock.png"
        },
        "all": {
            "icon": "/img/16/folder_green.png"
        },
        "trash": {
            "icon": "/img/16/recycle_bag.png"
        }
    };

    $scope.onFolderSelect = function (a, b, c) {
        model.selectFolder(b.node.original);

        //для работы контекстного меню
        model.selectedIds.length = 0;
        model.selectedIds.push(b.node.original.id);
    };

    $scope.onFolderDeselect = function (a, b, c) {

        model.selectFolder($scope.folders[0]);
    };

    foldersLoad();


    model.menu = (function () {

        var options = [

        ];

        //var actGetParam = function (a, gp, en) {
        //    return function ($item) {
        //        if (!en || en()) {
        //            a.act(gp == undefined ? gp : gp());
        //        } else {
        //            $log.debug(a.header + ": Не доступно");
        //        }
        //    }
        //}

        for (var i = 0; i < menuActions.length; i++) {
            var menuAction = menuActions[i];
            if (menuAction === null) {
                options.push(null);
                continue;
            }

            (function (ma) {
                ma.promise.then(function (action) {
                    if (action == null) return;

                    var getParam = ma.getParam;
                    var isEnabled = ma.isEnabled;
                    var success = ma.success;
                    var error = ma.error;

                    var actGetParam = function (a, gp, en, sc, er) {
                        return function ($item) {
                            if (!sc) sc = function () { };
                            if (!er) er = function () { };
                            a.act(gp == undefined ? gp : gp()).then(sc, er);
                        }
                    }

                    var icon = ma.icon || action.icon;
                    var title = ma.title || action.header;

                    options.push({
                        icon: icon,
                        title: title,
                        type: 'html',
                        action: actGetParam(action, getParam, isEnabled, success, error),
                        enabled: isEnabled
                    });

                    //model.actions.push({
                    //    type: "action",
                    //    filterTags: ["action", "действие"],
                    //    icon: action.icon,
                    //    title: title,
                    //    action: actGetParam(action, getParam),
                    //    checkEnabled: isEnabled,
                    //    enabled: isEnabled ? isEnabled() : true
                    //});
                })
            })(menuAction);
        }

        return options;

    })();

    /////

    $scope.model = model;

    //model.load();

    model.states = [{
        name: "Все",
        states: [{ all: true }],
        children: [{
            name: "Опрошенные",
            states: [{ min: 0, max: 0 }]
        }, {
            name: "Другие",
            states: [{ min: 1, max: 10000 }],
            children: [{
                name: "Этапы",
                states: [{ min: 1, max: 99 }],
            }, {
                name: "Опрашиваются",
                states: [{ min: 20, max: 20 }],
            }, {
                name: "Ошибка при опросе",
                states: [{ min: 100, max: 199 }],
            }, {
                name: "Непонятно",
                states: [{ min: 200, max: 299 }],
            }, {
                name: "Нет связи",
                states: [{ min: 300, max: 399 }],
            }, {
                name: "Иное",
                states: [{ min: 200, max: 299 }, { min: 400, max: 10000 }],
            }]
        }]
    }];

    model.devices = [{
        name: "Все",
        devices: [{ all: true }],
        children: []
    }];

    $drivers.all().then(function (answer) {
        if (!answer) {
            return $q.reject();
        }
        //
        var message = { devices: [] };
        //
        var n = answer.body.drivers;
        for (var i = 0; i < n.length; i++) {
            var driver = n[i];
            if (driver.isFilter == true)
            {
                var device = { id: driver.id, name: driver.name };
                message.devices.push(device);
            }
        }
        return message;
    })
    .catch(function (error) { return null; })
    .then(function (message) {
        if (message && message.devices && (message.devices.length > 0)) {
            var sorted = $filter('orderBy')(message.devices, 'name');
            for (var i = 0; i < sorted.length; i++) {
                var device = sorted[i];
                model.devices[0].children.push({
                    name: device.name,
                    devices: [device]
                });
            }
            model.accordion.device.enabled = true;
        }
    });

    model.colapseFolders = function (scope) {
        if (model.isLoading) {
            scope.$nodeScope.collapseAll();
        }
    };

    var filterByState = {};
    $listFilter.register(filterByState);
    model.selectState = function (state) {
        model.selectedState = state.name;
        filterByState.states = state.states;
        filterByState.raise();
    };

    //

    var filterByDevice = {};
    $listFilter.register(filterByDevice);
    model.selectDevice = function (device) {
        model.selectedDevice = device.name;
        filterByDevice.devices = device.devices;
        filterByDevice.raise();
    };

    //

    var filterByFolder = {};
    $listFilter.register(filterByFolder);
    model.selectFolder = function (folder) {

        delete filterByFolder.pinned;
        delete filterByFolder.folderId;
        delete filterByFolder.isDeleted;
        model.selectedIds.length = 0;


        if (folder.id === "deleted") {
            filterByFolder.isDeleted = true;
        } else if (folder.id === "all") {

        } else {
            filterByFolder.folderId = folder.id;
        }

        $folders.setSelected([folder]);
        model.selectedFolder = folder.text;

        filterByFolder.raise();
    };



    var listeners = [];

    listeners.push($rootScope.$on("folders:changed", function () {
        model.accordion['folders'].enabled = false;
        foldersLoad().finally(function () {
            model.accordion['folders'].enabled = true;
        })
    }));
    
    $scope.$on('$destroy', function () {
        $log.debug("уничтожается таблица фолдеров");
        for (var i = 0; i < listeners.length; i++) {
            var listener = listeners[i];
            listener();
        }
    });
});

