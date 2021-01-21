angular.module("app")
.service("$flash", function ($transport) {
    var poll = function (cmd, ids) {
        var arg = {
            cmd: cmd,
            components: "Current",
            onlyHoles: false
        }
        return $transport.send(new Message({ what: "poll" }, { objectIds: ids, what: "all", arg: arg}));
    };
    var flash = function (flash, ids) {
        var cmd = "startFlash|" + flash;
        var arg = {
            cmd: cmd,
            components: "Current",
            onlyHoles: false
        }
        return $transport.send(new Message({ what: "poll" }, { objectIds: ids, what: "all", arg: arg }));
    };
    var isPassword = function (password) {
        return $transport.send(new Message({ what: "users-password" }, { password: password }));
    };
    return {
        poll: poll,
        flash: flash,
        isPassword: isPassword
    }
});