angular.module("app");

app.service("$folders", function ($rootScope, $transport, $log, $q) {

    var self = this;
    self.datas = {};
    self.root = {};
    self.selection = [];

    var wrap = function (root) {
        root.data.type = "Folder";
        if (!root.data.id) {
            root.data.id = "root";
            root.data.type = "all";
            root.data.name = "Всё";
        }
        else
        {
            self.datas[root.data.id] = root.data;
            //root.body = self.bodys[root.data.id];
        }

        if (root.children) {
            for (var i = 0; i < root.children.length; i++) {
                wrap(root.children[i]);
            }
        }

        return root;
    };

    var resolve = function () {
        return $transport.send(new Message({ what: "folders-get" }))
			.then(function (message) {
			    self.datas = {};
			    self.root = wrap(message.body.root);
			    //
			    var ids = [];
			    for (var k in self.datas) {
			        if (k && self.datas.hasOwnProperty(k)) {
			            ids.push(k);
			        }
			    }
			    return $transport.send(new Message({ what: "edit-get-folders-id" }, { ids: ids })).then(function (messages) {
			        //self.datas[0] <==> message[0].body.folder
			        for (var i = 0; i < messages.length; i++) {
			            var message = messages[i].body;
			            if (self.datas[message.folder.id]) {
			                self.datas[message.folder.id].Task = message.folder.Task;
			                self.datas[message.folder.id].Tube = message.folder.Tube;
			            }
			            //self.datas[message.folder.id] = message.folder;
			        }
			        return self.root;
			    })
			    //return self.root;
			});
    }

    var getRoot = function () {
        return self.root;
    }

    var selected = function () {
        return self.selection;
    }

    var setSelected = function (folders) {
        self.selection = folders;
    }

    var init = function () {
        self.root = {};
        self.selection.length = 0;
    }

    $rootScope.$on("auth:deauthorized", function (e, message) {
        init();
    });

    self.selectedIds = [];
    var getSelectedIds = function () {
        return self.selectedIds;
    }

    var setSelectedIds = function (selectedIds) {
        self.selectedIds = selectedIds;
    }

    var frozenRows = [];

    var getFrozenRows = function () {
        return frozenRows;
    };

    var setFrozenRows = function (rows) {
        frozenRows = rows;
    };

    return {
        selected: selected,
        setSelected: setSelected,
        getRoot: getRoot,
        resolve: resolve,
        setSelectedIds: setSelectedIds,
        getSelectedIds: getSelectedIds,
        getFrozenRows: getFrozenRows,
        setFrozenRows: setFrozenRows
    }
});