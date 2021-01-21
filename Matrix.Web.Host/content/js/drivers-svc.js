angular.module("app")
.service("$drivers", function ($transport) {

    var all = function () {
        return $transport.send(new Message({ what: "driver-list" }));
    };

    var short = function () {
        return $transport.send(new Message({ what: "driver-list-small" }));
    };

    var save = function (drivers) {
        return $transport.send(new Message({ what: "drivers-save" }, { drivers: drivers }));
    };
    var create = function (name) {
        return $transport.send(new Message({ what: "drivers-create" }, { name: name }));
    };
    return {
        all: all,
        save: save,
        short: short,
        create: create
    }
});