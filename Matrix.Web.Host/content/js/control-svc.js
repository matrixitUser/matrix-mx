﻿angular.module("app")
.service("controlSvc", function ($transport, $log, $q, $rootScope) {

    var objects = { ids: [], assoc: {} };
    var loggingCount = 1;// делать connect НЕ нужно. default:0;

    var serverSubscribe = function (ids) {
        return $transport.send(new Message({ what: "log-subscribe" }, { ids: ids }))
            .then(function (message) {
                $log.debug("control-svc ответ с сервера %s", message.head.what);
                if (message.head.what == "log-subscribe") {
                    return message.body.ids;
                } else {
                    return $q.reject("не авторизован");
                }
            });
    }

    var subscribe = function (ids) {
        if (!ids) {
            return $q.reject("выберите объекты из списка");
        }
        var updated = false;

        for (var i = 0; i < ids.length; i++) {
            var id = ids[i];
            if (!objects.assoc[id]) {
                objects.assoc[id] = 1;
                objects.ids.push(id);
                updated = true;
            } else {
                objects.assoc[id]++;
            }
        }

        if (updated && loggingCount > 0) {//есть изменения
            return serverSubscribe(objects.ids);
        }

        return $q.when(objects.ids);
    }

    var unsubscribe = function (ids) {
        if (!ids) {
            return $q.reject("выберите объекты из списка");
        }
        var updated = false;
        for (var i = 0; i < ids.length; i++) {
            var id = ids[i];
            if (objects.assoc[id]) {
                if (objects.assoc[id] == 1) {
                    updated = true;
                    delete objects.assoc[id];
                    for (var j in objects.ids) {
                        if (id == objects.ids[j]) {
                            objects.ids.splice(j, 1);
                            break;
                        }
                    }
                } else {
                    objects.assoc[id]--;
                }
            }
        }

        if (updated && loggingCount > 0) {//есть изменения
            return serverSubscribe(objects.ids);
        }

        return $q.when(objects.ids);
    }

    var unsubscribeAll = function () {                
        objects.assoc = {};
        objects.ids.length = 0;

        if (loggingCount > 0) {
            return serverSubscribe(objects.ids);
        } 
        
        return $q.when(objects.ids);
    }

    var connect = function () {
        if (loggingCount == 0) {
            serverSubscribe(objects.ids);
        }
        loggingCount++;
    }

    var disconnect = function () {
        if (loggingCount > 0) {
            loggingCount--;
        }
        if (loggingCount == 0) {
            serverSubscribe([]);
        }
    }
    var pollLightControlConfig = function (lightControlMetod, afterSunSetAndBeforeSunRise, lightSheduleOn, lightSheduleOff, objectId) {
        return $transport.send(new Message({ what: "poll-light-control-config" }, { lightControlMetod: lightControlMetod, afterSunSetAndBeforeSunRise: afterSunSetAndBeforeSunRise, lightSheduleOn: lightSheduleOn, lightSheduleOff: lightSheduleOff, objectId: objectId }));
    }
    var nodeGetAstronomTimer = function (objectId) {
        return $transport.send(new Message({ what: "node-get-astronomTimer" }, {objectId: objectId }));
    }
    var nodeUpdateAstronomTimer = function (coordinates, utc, afterBeforeSunSetRise, objectId) {
        return $transport.send(new Message({ what: "node-update-astronomTimer" }, { coordinates: coordinates, utc: utc, afterBeforeSunSetRise: afterBeforeSunSetRise, objectId: objectId }));
    }
    var pollSetLightAstronomTimer = function (coordinates, utc, afterBeforeSunSetRise, objectId) {
        return $transport.send(new Message({ what: "poll-set-light-astronomtimer" }, { coordinates: coordinates, utc: utc, afterBeforeSunSetRise: afterBeforeSunSetRise, objectId: objectId }));
    }
    var nodeEvents = function (events, objectId) {
        return $transport.send(new Message({ what: "node-events" }, { events: events, objectId: objectId }));
    }
    var GetLightControlConfig = function (objectId) {

        return $transport.send(new Message({ what: "node-get-light-control-config" }, {objectId: objectId }));
    }
    
    return {
        subscribe: subscribe,       //подписка - добавление в список подписанных
        unsubscribe: unsubscribe,   //отписка - удаление из списка подписанных 
        unsubscribeAll: unsubscribeAll,
        pollLightControlConfig: pollLightControlConfig,
        GetLightControlConfig: GetLightControlConfig,
        pollSetLightAstronomTimer: pollSetLightAstronomTimer,
        nodeGetAstronomTimer: nodeGetAstronomTimer,
        nodeUpdateAstronomTimer: nodeUpdateAstronomTimer,
        nodeEvents: nodeEvents,
        connect: connect,           //запрос сервера на получение подписок
        disconnect: disconnect,     //команда серверу на прекращение получения данных
        getSubscribers: function () { return objects; }, //подписчики
    }

});