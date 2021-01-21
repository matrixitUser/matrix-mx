angular.module("app")
.service("taskSvc", function ($transport, $log, $q) {

    var service = this;
    

    service.tasks = function () {
        return $transport.send(new Message({ what: "edit-get-tasks" })).then(function (message) {
            $log.debug("edit-get-tasks ответ с сервера %s", message.head.what);
            if (message.head.what == "edit-get-tasks") {
                return { tasks: message.body.tasks };
            } else {
                return $q.reject("не авторизован");
            }
        });
    };

    
    service.all = function () {
        return $transport.send(new Message({ what: "task-list" }));
    }

    service.get = function (id) {
        return $transport.send(new Message({ what: "task-get" }, { id: id }));
    }
});