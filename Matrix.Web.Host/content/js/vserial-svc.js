angular.module("app")
/**
 * формат действия:
 * {name:"имя(уникально)",
 *  header:"заголовок",
 *  act:function(arg){} () //само действие  
 * }
 */
.service("VSerialSvc", function ($q, $log) {
    
    ////Сервис общается с ком-портами посредством веб-сокета
    ////Открытия ком-порта как такового, нет (как и закрытия; надо оставить хвостики, т.к должны быть)
    ////Веб-сокет - один, и он открывается при открытии первого виртуального ком-порта и закрывается при закрытии последнего
    ////Виртуальных ком-портов может быть открыто несколько
    ////Виртуальный ком-порт привязывается к соединению тюба
    ////При открытии вирт. ком-порта он связывается с реальным комом через веб-сокет
    ////

    var service = {};

    ////INIT 
    ////при открытии - запросить у сервера список уже открытых(!)
    //{

    //}

    //service.open = function (p) {
    //    if (service.wsstate == "disconnected") {
    //        //
    //        service.port = p;
    //        service.wsstate = "connecting";

    //        service.ws = new WebSocket("ws://localhost:9999/");

    //        service.ws.addEventListener('open', function () {
    //            $log.debug("connected");
    //            service.ws.send('{"command" : "write", "path" : "COM3", "data" : "hello"}');
    //            service.wsstate = "connected";
    //        });

    //        service.ws.addEventListener('close', function () {
    //            $log.debug("connection lost");
    //            service.wsstate = "disconnected";
    //        });

    //        service.ws.addEventListener('message', function (e) {
    //            //command, port, data
    //            var received = JSON.parse(e.data);
    //            $log.debug("got " + received.data + " from " + received.port);
    //        });

    //        service.ws.addEventListener('error', function (e) {
    //            $log.debug("error " + e);
    //        });
    //    }
    //}

    //service.close = function () {
    //    if (service.status == "connected") {
    //        //service.status = "disconnected";

    //        //DISCONNECT
    //        //закрытие порта на сервере
    //    }
    //}

    //service.send = function (data) {
    //    if (status == "open") {
    //        ws.send('{"command" : "write", "path" : "' + service.port + '", "data" : "' + data + '"}');
    //    }
    //}
    
    return service;

});
