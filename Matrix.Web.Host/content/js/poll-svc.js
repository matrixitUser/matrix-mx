function isEmpty(obj) {
    for (var prop in obj) {
        if (obj.hasOwnProperty(prop))
            return false;
    }

    return true;
};
angular.module("app")
.service("$poll", function ($transport) {
    var poll = function (what, ids, arg, redirect) {
        var auto = false;
        if (isEmpty(arg)) {
            arg = {
                //components: "Day:2:60;Hour:2:60",
                components: "Day;Hour",
                auto:true // костыль, чтобы не засорять приоритетными задачами опросы
            };
        }
        return $transport.send(new Message({ what: "poll" }, { objectIds: ids, arg: arg, what: what, redirect: redirect }));
    };

    var cancel = function (ids) {
        return $transport.send(new Message({ what: "poll-cancel" }, { objectIds: ids }));
    };

    return {
        poll: poll,
        cancel: cancel
    };
})