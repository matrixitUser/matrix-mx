angular.module("app")
/**
 * формат действия:
 * {name:"имя(уникально)",
 *  header:"заголовок",
 *  act:function(arg){} () //само действие  
 * }
 */
.service("windowsSvc", function ($rootScope, $q) {

    var service = this;
    var lastId = 0;

    service.windows = [];

    var newId = function () {
        return lastId++;
    }

    var isEqualData = function (type, d1, d2) {
        if (type == "report-list") {
            if ((d1.id != d2.id) || (d1.id != d2.id))
                return false;
        }
        var d1ids = type == "report-list" ? d1.ids : d1;
        var d2ids = type == "report-list" ? d2.ids : d2;

        if (d1ids) {
            if (!d2ids || d1ids.length != d2ids.length)
                return false;
            for (var i = 0; i < d1ids.length; i++) {
                var d1id = d1ids[i];
                for (var j = 0; j < d2ids.length; j++) {
                    var d2id = d2ids[j];
                    if (d1id == d2id)
                        break;
                }
                if (j == d2ids.length) return false;
            }
        }
        return true;
    }

    //OPEN

    service.open = function (window) {

        var data;

        //проверка на существование
        for (var i = 0; i < service.windows.length; i++) {
            var w = service.windows[i];
            if (w.type == window.type && (w.only1 == true || isEqualData(w.type, w.data, window.data))) {//совпадает тип
                data = window.data;
                window = w;
                break;
            }
        }
        if (i == service.windows.length) {
            //новое окно
            window.id = newId();
            window.state = "ok";
            window.deffered = $q.defer();
            if (service.windows.length == 6) {//ограничение количества открытых окон
                var del = service.windows.shift();
                del.close();
            }
            service.windows.push(window);//автооткрытие ч/з контроллер
            window.close = (function (w) {
                return function () {
                    if (window.state == "closing") return;
                    service.close(w.id);
                    window.state = "closing";
                    window.deffered.resolve({});
                }
            })(window);
            window.open = function () { }//заглушка. должна быть переопределена в контроллере
        } else {
            //существующее окно
            window.open(data);
        }

        return window.deffered.promise;
    };

    //CLOSE

    service.close = function (id) {
        for (var i = service.windows.length; i > 0; i--) {
            if (service.windows[i - 1].id == id) {
                service.windows.splice(i - 1, 1);
            }
        }
    }

    service.closeAll = function () {
        for (var i = service.windows.length; i > 0; i--) {
            service.windows[i - 1].close();
        }
    }

    //DEINIT

    $rootScope.$on("auth:deauthorized", function (e, message) {
        service.closeAll();
    });
});
