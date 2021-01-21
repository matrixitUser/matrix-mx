var app = angular.module("app");

function SaveRule(action, target, content) {
    var self = this;
    self.action = action;
    self.target = target;
    self.content = content;
    return self;
}

app.controller("SetRightsCtrl", function ($uibModalInstance, $scope, data, $users, $log, $transport) {

    var model = {
        data: data,
        roots: []
    };

    var id = model.data;

    $scope.opt = {
        columnDefs: [{
            headerName: "Группа",
            field: "name",
            cellRenderer: {
                renderer: 'group',
                innerRenderer: innerCellRenderer
            },
            width: "500"
        }, {
            headerName: "Права",
            field: "checked",
            template: "<input ng-if=\"data.type==='Group'\" type=\"checkbox\" ng-click=\"data.check()\" ng-model=\"data.checked\" />",
            width: "30"
            //cellRenderer: {
            //    innerRenderer: innerCellRenderer
            //}
        }],
        //groupSelectsChildren: true,
        angularCompileRows: true,
        enableFilter: true,
        rowData: null,
        rowSelection: 'single',
        rowsAlreadyGrouped: true,
        enableColResize: true,
        enableSorting: true,
        icons: {
            groupExpanded: '<img src="/img/16/toggle.png" />',
            groupContracted: '<img src="/img/16/toggle_expand.png" />'
        }
    };

    function innerCellRenderer(params) {

        if (params.node.type === "User") {
            return '<img src="../img/user.png" style="padding-left: 4px;" /> ' + params.data.login;
        } else {
            return '<img src="../img/group.png" style="padding-left: 4px;" /> ' + params.data.name;
        }

        return params.data.name;
    }

    var rules = [];

    $transport.send(new Message({ what: "users-get-rights" }, { targetId: id })).then(function (message) {
        var result = [];
        for (var i = 0; i < message.body.groups.length; i++) {
            var group = message.body.groups[i];
            group.data = group;
            group._id = group.id;

            //group.data.checked = false;
            group.data.check = (function (g) {
                return function () {
                    if (g.checked) {
                        //g.checked = false;
                        rules.push(new SaveRule("add", "relation", { start: g._id, end: id, type: "right", body: {} }));
                    } else {
                        //g.checked = true;
                        rules.push(new SaveRule("del", "relation", { start: g._id, end: id, type: "right", body: {} }));
                    }
                    return g.checked;
                };
            })(group);

            for (var j = 0; j < message.body.rights.length; j++) {
                var right = message.body.rights[j];
                if (right.start === group.id) {
                    group.data.checked = true;
                }
            }
            group.children = [];
            for (var j = 0; j < group._childrenIds.length; j++) {
                var childId = group._childrenIds[j];
                for (var k = 0; k < message.body.groups.length; k++) {
                    var childGroup = message.body.groups[k];
                    if (childGroup.id === childId) {
                        childGroup.isChild = true;
                        group.children.push(childGroup);
                    }
                }
            }

            group.group = group.children.length > 0;
        }
        for (var i = 0; i < message.body.groups.length; i++) {
            var group = message.body.groups[i];
            if (group.isChild === undefined) {
                group.expanded = true;
                result.push(group);
            }
        }
        $scope.opt.rowData = result;
        $scope.opt.api.onNewRows();
    });

    model.save = function () {
        $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function () { model.close() });
        model.close();
    }

    $users.getRules(model.data.id).then(function (message) {
        //model.roots.length = 0;
        //var root = message.body.root;
        ////find(root, function (item) {
        ////    if (!item.rule) {
        ////        item.rule = {
        ////            allow: true,
        ////        };
        ////    }
        ////    if (item.rule.inherited) {
        ////        $log.debug("элемент %s [%s]", item.name, item.rule.inherited);
        ////        item.readOnly = true;
        ////    };
        ////    item.old = (item.rule.allow === true);
        ////    item.allow = (item.rule.allow === true);
        ////});
        //if (root) {
        //    model.roots.push(root);
        //}

        //$scope.opt.rowData = model.roots;
        //if ($scope.opt.api) {
        //    $scope.opt.api.onNewRows();
        //}
    });

    var find = function (root, process) {
        var result = [];
        result.push(root);
        if (process) {
            process(root);
        }
        for (var i = 0; i < root.children.length; i++) {
            var children = find(root.children[i], process);
            for (var j = 0; j < children.length; j++) {
                result.push(children[j]);
            }
        }
        return result;
    }

    //$scope.save = function () {
    //    var added = [];
    //    var old = find($scope.roots[0], function (item) {
    //        if (item.class === "group" && item.old !== item.allow) {
    //            if (item.allow) {
    //                added.push(item.id);
    //            }
    //        }
    //    });

    //    $users.setRules($scope.data[0], added).then(function () {
    //    });
    //};

    //$scope.addCleaner($scope.save);


    model.close = function () {
        $uibModalInstance.close();
    }



    $scope.model = model;
});