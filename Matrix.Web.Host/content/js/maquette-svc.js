angular.module("app")
.service("maquetteSvc", function ($transport) {

    var service = this;

    service.all = function () {
        return $transport.send(new Message({ what: "maquette-list" }));
    };

    service.get = function (id) {
        return $transport.send(new Message({ what: "maquette-get" }, { id: id }));
    };

    service.send = function (id, days) {
        return $transport.send(new Message({ what: "maquette-send" }, { id: id, days: days }));
    }
});