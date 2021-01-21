var app = angular.module("app");

app.service("$users", function ($transport) {

    var service = this;

    service.all = function () {
        return $transport.send(new Message({ what: "users-get" }));
    };

    service.save = function (users) {
        return $transport.send(new Message({ what: "users-save" }, { users: users }));
    }

    service.getRules = function (objectId) {
        return $transport.send(new Message({ what: "users-get-rules" }, { objectId: objectId }));
    }

    service.setRules = function (objectId, added) {
        return $transport.send(new Message({ what: "users-set-rules" }, { objectId: objectId, added: added }));
    }

    //return {
    //    all: all,
    //    save: save,
    //    getRules: getRules,
    //    setRules: setRules
    //};
});