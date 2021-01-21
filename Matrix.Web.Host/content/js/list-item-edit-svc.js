angular.module("matrix")
.service("$graph", function ($rootScope, $listSelection, $transport) {

    var getGraph = function (tubeId) {
        return $transport.send(new Message({ what: "object-get-branch" }, { startId: tubeId, direction: "all" }));
    };

    var getCandidates = function (types) {
        return $transport.send(new Message({ what: "object-get-candidates" }, { types: types }));
    };

    var getBranch = function (startId) {
        return $transport.send(new Message({ what: "object-get-branch" }, { startId: startId, direction: "forward" }));
    };

    var save = function (items) {
        return $transport.send(new Message({ what: "objects-save" }, { items: items }));
    };

    var remove = function (id) {
        return $transport.send(new Message({ what: "object-remove" }, { id: id }));
    }

    return {
        getGraph: getGraph,
        getCandidates: getCandidates,
        getBranch: getBranch,
        save: save,
        remove: remove
    };
})