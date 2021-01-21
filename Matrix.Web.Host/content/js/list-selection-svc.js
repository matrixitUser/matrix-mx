angular.module("matrix")
.service("$listSelection", function () {

    var selection = [];
    var set = function (sel) {
        selection = sel;
    }

    var get = function () {
        return selection;
    };

    var getTubeIds = function () {
        var ids = [];
        for (var i = 0; i < selection.length; i++) {
            var selected = selection[i];
            if (selected.Tube) {
                ids.push(selected.Tube.id);
            }
        }
        return ids;
    }

    return {
        set: set,
        get: get,
        getTubeIds: getTubeIds
    }
})
