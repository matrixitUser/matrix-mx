angular.module("app")
/**
 * формат действия:
 * {name:"имя(уникально)",
 *  header:"заголовок",
 *  act:function(arg){} () //само действие  
 * }
 */
.service("$actions", function ($helper, $auth, $parse, $q) {

    var service = this;
    var actions = {};

    service.push = function (a) {

        var wrap = {};
        $helper.copyToFrom(wrap, a, ["name", "header", "icon", "previlegied"]);
        wrap.action = a;

        wrap.checkAdmin = function () {
            //var self = this;
            var p;
            if (wrap.previlegied) {
                p = $auth.getSession().then(function (session) {
                    var isAdmin = $parse("user.isAdmin")(session) || false;
                    if (!isAdmin) {
                        return $q.reject("Нет доступа");
                    }
                });
            } else {
                p = $q.when(true);
            }
            return p;
        }

        wrap.act = function (arg, param) {
            return wrap.checkAdmin().then(function () {
                return wrap.action.act(arg, param);
            });
        }

        actions[a.name] = wrap;
    };


    //при получении action возвращает promise. 
    //не существует - NULL
    //нет доступа - reject("Нет доступа")
    service.get = function (name) {
        var a = actions[name];
        if (!a) return $q.when(null);
        return a.checkAdmin().then(function () {
            return a;
        });
    };

    service.getWrapped = function (name) {
        var a = actions[name];
        //обёртка
        var wrap = {};
        wrap.loading = true;
        wrap.visible = false;
        wrap.error = "";
        wrap.action = {
            name: name,
            header: "",
            icon: "/img/action_log.png",
            act: function (arg) {
                if (a) {
                    a.checkAdmin().then(function () {
                        a.act(arg);
                    });
                }
            }
        }

        if (!a) {
            wrap.loading = false;
            wrap.visible = false;
            wrap.error = "Действия не существует";
            //wrap.action = null;
        } else {
            a.checkAdmin().then(function () {
                wrap.loading = false;
                wrap.visible = true;
                wrap.error = "";
                wrap.action = a;
            }, function (err) {
                wrap.loading = false;
                wrap.visible = false;
                wrap.error = err;
                //wrap.action;
            });
        }

    }

    service.getWrap = function (param)
    {
        var type;
        var action;
        if (typeof param === 'string' || param instanceof String) {
            type = param;
            action = { type: param };
        } else {
            type = param.type;
            action = param;
        }

        if (!action.action) {
            action.promise = service.get(type).then(function (a) {
                action.act = a.act;
                action.icon = action.favicon || a.icon;
                action.header = action.caption || a.header;
                return action;
            });
            action.act = function (arg, param) {
                action.promise.then(function (a) {
                    if (a) a.act(arg, param);
                }).catch(function () {
                    action.icon = "/img/error.png";
                    action.header = "";
                });
            }
            action.icon = "/img/loader.gif";
            action.header = "...";
        } else {
            action.icon = action.favicon;
            action.header = action.caption;
            action.act = action.action;
        }
        return action;
    }
});
