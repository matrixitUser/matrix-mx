angular.module("app")
.controller("usersCtrl", function ($scope, $users, $helper, $log, $transport, $uibModalInstance, md5, $filter, $timeout) {

    var model = {
    };

    var rules = [];

    var applyPassTimeout = undefined;

    model.close = function () {
        $uibModalInstance.close();
    }

    model.isLoaded = false;

    model.users = [];

    model.update = function () {
        $transport.send(new Message({ what: "users-get" }, {})).then(function (message) {
            var groups = $filter('orderBy')(message.body.groups, [ 'type', 'name' ]);
            for (var i = 0; i < groups.length; i++) {
                var group = groups[i];
                group._children = [];
                group._dirty = false;

                if (group.password) {
                    group._password = "*****";
                }

                for (var k = 0; k < groups.length; k++) {
                    var childGroup = groups[k];
                    for (var j = 0; j < group._childrenIds.length; j++) {
                        var childId = group._childrenIds[j];
                        if (childGroup.id === childId) {
                            childGroup._isChild = true;
                            group._children.push(childGroup);
                        }
                    }
                }
            }
            model.users.length = 0;
            for (var i = 0; i < groups.length; i++) {
                var group = groups[i];
                if (group._isChild === undefined) {
                    model.users.push(group);
                }
            }
            model.isLoaded = true;
        });
    }

    model.selectNode = function (node) {
        if (model.form) {
            if (node._dirty) {
                model.form.$setDirty();
            } else {
                model.form.$setPristine();
            }
        }
        //$scope.$parent.editForm.$setPristine();

        model.selected = node;
    };

    model.setForm = function (form) {
        model.form = form;
    }

    model.selected = undefined;

    model.addUser = function (group) {
        $transport.send(new Message({ what: "helper-create-guid" }, { count: 1 })).then(function (message) {
            var newUser = {
                id: message.body.guids[0],
                type: "User",
                name: "Новый пользователь",
                _dirty: true
            };

            group._children.push(newUser);
            rules.push(new SaveRule("add", "relation", { start: group.id, end: newUser.id, type: "contains", body: {} }));
            rules.push(new SaveRule("add", "node", { id: newUser.id, type: "User", body: newUser }));
            model.selectNode(newUser);
        });
    }

    //$scope.filter = "";
    //$scope.$watch("filter", function (newValue, oldValue) {
    //    console.log(newValue);//$log.debug(newValue);
    //});

    model.addGroup = function (group) {
        $transport.send(new Message({ what: "helper-create-guid" }, { count: 1 })).then(function (message) {
            var newGroup = {
                id: message.body.guids[0],
                type: "Group",
                name: "Новая группа",
                _dirty: true
            };

            group._children.push(newGroup);
            rules.push(new SaveRule("add", "relation", { start: group.id, end: newGroup.id, type: "contains", body: {} }));
            rules.push(new SaveRule("add", "node", { id: newGroup.id, type: "Group", body: newGroup }));
            model.selectNode(newGroup);
        });
    }

    model.applyPass = function (node) {

        $timeout.cancel(applyPassTimeout);
        applyPassTimeout = undefined;

        if (node._password == "*****") return;

        applyPassTimeout = $timeout(function () { model.applyPassDelayed(node);  }, 500);
    };

    model.applyPassDelayed = function (node) {
        $transport.send(new Message({ what: "helper-create-md5" }, { text: node._password })).then(function (message) {
            node.password = message.body.md5;
        });
    }

    model.save = function () {
        $timeout(function () {
            model.isLoaded = false;
            collectChanges(model.users);
            var x = rules;
            $transport.send(new Message({ what: "edit" }, { rules: rules })).then(function () {
                model.close();
            });
        }, applyPassTimeout ? 500 : 0);
    }

    //model.visible = function (node) {
    //    return !(model.filter && model.filter.length > 0
    //    && (node.login.indexOf(model.filter) == -1 || node.name.indexOf(model.filter) == -1 || node.patronymic.indexOf(model.filter) == -1 ||
    //        node.surname.indexOf(model.filter) == -1 || node.name.indexOf(model.filter) == -1));
    //}

    function collectChanges(roots) {
        for (var i = 0; i < roots.length; i++) {
            var root = roots[i];
            if (root._dirty) {
                //if (root._password && root._password.length > 0) {
                //    root.password = md5.createHash(root._password);
                //}
                rules.push(new SaveRule("upd", "node", { id: root.id, type: root.type, body: root }));
            }
            if (root._children) {
                collectChanges(root._children);
            }
        }
    }

    $scope.model = model;

    model.update();
});