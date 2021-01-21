angular.module("matrix")
.service("$modems", function ($transport) {
    var modemsOfPool = function (poolId) {
        return $transport.send(new Message({ what: "modems-of-pool" }, { poolId: poolId }));
    };

    var save = function (modems) {
        return $transport.send(new Message({ what: "modems-save" }, { modems: modems }));
    };

    return {
        modemsOfPool: modemsOfPool,
        save: save
    }
});