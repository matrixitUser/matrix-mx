angular.module("app")
    .service("$valve", function ($transport, md5, metaSvc) {
    var poll = function (cmd, ids) {
        var arg = {
            cmd: cmd,
            components: "Current",
            onlyHoles: false
        }
        return $transport.send(new Message({ what: "poll" }, { objectIds: ids, what: "all", arg: arg}));
    };
    var valveControl = function (dataName, control, objectIds) {
        var cmd = "valveControl#" + dataName + ":" + control;
        var arg = {
            cmd: cmd,
            components: "Current",
            onlyHoles: false
        }
        return $transport.send(new Message({ what: "poll" }, { objectIds: objectIds, what: "all", arg: arg }));
    };
    var isPassword = function (login, password) {
        var passwordHash = (metaSvc.config === "orenburg") ? password : md5.createHash(password || '');
        //разбор ответа от сервера: сессия при успехе или reject при неудаче
        //return $transport.send(new Message({ what: "auth-by-login1" }, { login: login === undefined ? "" : login, password: password === undefined ? "" : passwordHash }));
        return $transport.send(new Message({ what: "users-by-login-password" }, { login: login === undefined ? "" : login, password: password === undefined ? "" : passwordHash  }));
    }
    return {
        poll: poll,
        valveControl: valveControl,
        isPassword: isPassword
    }
});