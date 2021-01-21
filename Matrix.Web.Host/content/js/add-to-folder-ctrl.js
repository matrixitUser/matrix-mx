var app = angular.module("app");

app.controller("AddToFolderCtrl", function ($uibModalInstance, $scope, data, $log, $parse, $transport, $helper, $list, $folders, $q) {

    var ids = data;

    var names = [];
    var rows = [];
    var tubeIds = [];
    var rowsByGroups = {};

    //folders-by-tubes

    var model = {
        data: data,
        names: names,
        folderRoots: [],
        folders: [],
        //
        overlayEnabled: true,
        overlayText: "",
        overlay: $helper.overlayFunc
    };

    var wrap = function (root) {

        if (root.children && root.children.length > 0) {
            root.group = true;
            for (var i = 0; i < root.children.length; i++) {
                wrap(root.children[i]);
            }
        } else {
            root.group = false;
        }

        if (root.data && root.data.id && root.data.type == 'Folder') {
            model.folders.push(root.data);
            root.data._rows = rowsByGroups[root.data.id];
            root.data.rows = root.data._rows;
        }

        return root;
    };
    

    model.update = function () {
        rowsByGroups = {};

        model.folders.length = 0;
        model.folderRoots.length = 0;
        rows = [];

        //model.overlayText = "Загрузка...";
        //model.overlayEnabled = true;

        model.overlay(
            $list.getRowsCache(ids).then(function (crows) {
                rows = crows;
                var asc = {};
                for (var i = 0; i < crows.length; i++) {
                    var crow = crows[i];
                    var name = (crow.name || "") + (crow.name && crow.pname ? ": " : "") + (crow.pname || "");
                    names.push(name || crow.id);
                    asc[crow.id] = crow;
                }
                return $transport.send(new Message({ what: "folders-by-tubes" }, { tubeIds: ids }))
                   .then(function (message) {
                       //model.folderRoots.push(message.body.root);
                       for (var i = 0; i < message.body.folders.length; i++) {
                           var folder = message.body.folders[i];
                           var rows = [];
                           for (var j = 0; j < folder.Tube.length; j++) {
                               var tube = folder.Tube[j];
                               var row = asc[tube.id];
                               rows.push(row);
                           }
                           rowsByGroups[folder.id] = rows;
                       }

                       return $folders.resolve().then(function (root) {
                           model.folderRoots.push(wrap(root));
                           $scope.opt.rowData = model.folderRoots;
                           if ($scope.opt.api) {
                               $scope.opt.api.onNewRows();
                           }
                       })
                   })
            })
        );
    }

    $scope.opt = {
        columnDefs: [{
            headerName: "Группа",
            field: "name",
            cellRenderer: {
                renderer: 'group',
                innerRenderer: innerCellRenderer
            },
            width: 500
        }, {
            headerName: "Выбор",
            field: "checked",
            //template: "<input ng-if=\"data.type==='Folder'\" type=\"checkbox\" ng-click=\"data.check()\" ng-model=\"data.checked\" />",
            width: 30,
            template: "<span ng-if=\"data.type==='Folder'\" style=\"align-content: center; text-align: center\">\
                <img ng-src=\"/img/{{$parent.$parent.getChecked(data)}}\" ng-click=\"$parent.$parent.toggleSelected(data)\" width=\"20\" />\
            </span>"
        }],
        //groupSelectsChildren: true,
        angularCompileRows: true,
        enableFilter: true,
        rowData: [],
        rowSelection: 'single',
        rowsAlreadyGrouped: true,
        enableColResize: true,
        enableSorting: true,
        icons: {
            groupExpanded: '<img src="/img/16/toggle.png" />',
            groupContracted: '<img src="/img/16/toggle_expand.png" />'
        }
    };

    $scope.getChecked = function (data) {
        //return data.checkType == 0 ? 'check_box_uncheck.png' : (data.checkType == 1 ? 'check_box_mix.png' : 'check_box.png');
        if (!data.rows || data.rows.length == 0) return 'check_box_uncheck.png';
        if (data.rows.length == rows.length) return 'check_box.png';
        return 'check_box_mix.png';
    }

    $scope.toggleSelected = function (data) {
        if (data.rows && data.rows.length > 0) {
            delete data.rows;
        } else {
            data.rows = rows;
        }
    }

    function innerCellRenderer(params) {

        if (params.node.data.type === "Folder") {
            //<img src="/img/folder.png" />
            return '<img src="/img/folder.png" style="padding-left: 4px;" /> ' + params.data.name;
        }

        return params.data.name;
    }

    model.save = function () {
        var rules = [];

        var promises = [];
        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];
            promises.push($transport.send(new Message({ what: "edit-get-area-id" }, { id: row.id })));
        }

        $q.all(promises).then(function (messages) {
            for (var m = 0; m < messages.length; m++) {
                var message = messages[m];
                var areaId = message.body.areaId;

                for (var i = 0; i < model.folders.length; i++) {
                    var folder = model.folders[i];
                    if (folder.rows !== folder._rows) {//есть изменения
                        if (!folder.rows) folder.rows = [];
                        if (!folder._rows) folder._rows = [];
                        //поиск и удаление дубликатов _rows vs rows
                        for (var j = folder._rows.length; j > 0; j--) {
                            var _row = folder._rows[j - 1];
                            for (var k = folder.rows.length; k > 0; k--) {
                                var row = folder.rows[k - 1];
                                if (row.id == _row.id) {
                                    //удаление из _rows
                                    folder._rows.splice(j - 1, 1);
                                    //удаление из rows
                                    folder.rows.splice(k - 1, 1);
                                }
                            }
                        }

                        //удаление
                        for (var j = 0; j < folder._rows.length; j++) {
                            var _row = folder._rows[j];
                            rules.push(new SaveRule("del", "relation", { start: folder.id, end: areaId, type: "contains", body: {} }));
                        }
                        //добавление
                        for (var j = 0; j < folder.rows.length; j++) {
                            var row = folder.rows[j];
                            rules.push(new SaveRule("add", "relation", { start: folder.id, end: areaId, type: "contains", body: {} }));
                        }
                    }
                }
            }

            if (rules.length > 0) {
                model.overlay($transport.send(new Message({ what: "edit" }, { rules: rules })).finally(function () { model.close(true); }), "Сохранение...");
            } else {
                model.close(null);
            }
        });
    }
        
    model.update();

    model.close = function (result) {
        $uibModalInstance.close(result);
    }

    ///todo как сказано в баге
    //$scope.folders = [];
    //$transport.send(new Message({ what: "folders-get-2" }, { tubeIds: ids })).then(function (message) {        
    //    for (var i = 0; i < message.body.root.length; i++) {
    //        var node = message.body.root[i];
    //        if (!node.parent) {
    //            node.parent = "#";
    //        }

    //        node.text = node.name;
    //        $scope.folders.push(node);
    //    }
    //});

    $scope.model = model;
});