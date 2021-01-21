angular.module("app");

app.service("$list", function ($transport, $rootScope, $log, $q, $parse, $helper, $timeout, $folders) {

    /////////////////// SERVICE /////////////////// SERVICE /////////////////// SERVICE /////////////////// SERVICE /////////////////// SERVICE 
    var service = this;
            
    //selected
    var selectedIds = [];   //tubeid of selected rows
    var selectedRows = [];  //rows of selected rows
    var isMapsShow = false;
    var audio = new Audio();
    audio.src = '/media/trevoga.mp3';
    audio.loop = true;

    service.getSelectedIds = function () {
        return selectedIds;
    };
    service.getIsMapsShow= function () {
        return isMapsShow;
    };
    service.getSelectedRows = function () {
        return selectedRows;
    };

    service.setSelected = function (ids, rows) {
        selectedIds = ids;
        selectedRows = rows;
        $rootScope.$broadcast("list:selection-changed", null);
    };

    service.audio = function (cmd) {
        if (cmd == "play") {
            audio.play();
        }
        else if (cmd == "pause") {
            audio.pause();
        }
    }
    service.mapsShow = function (isShow) {
        isMapsShow = isShow;
    }
    //по массиву id отдаёт массив обёрнутых строк rows
    service.getRows = function (ids) {
        return $transport.send(new Message({ what: "rows-get" }, { ids: ids }))
            .then(function (message) {
                if (message.head.what == "rows-get") {
                    var rows = $parse('body.rows')(message) || [];
                    $log.debug("list-svc get rows " + rows.length);
                    return rows;
                } else {
                    return $q.reject($parse('body.message')(message) || "неизвестная ошибка");
                }
            });
    }
    
    //по фильтру отдаёт объект, содержащий в себе массив строк rows и общее количество count из кэша RowsCache
    service.getRowsCacheFiltered = function (filter) {
        return $transport.send(new Message({ what: "rows-get-2" }, { filter: filter })).then(function (message) {
            var rows = message.body.rows;
            var count = message.body.count;
            return { rows: rows, count: count };
        });
    }
    //по фильтру отдаёт массив id строк из кэша RowsCache
    service.getRowsCacheIdsFiltered = function (filter) {
        return $transport.send(new Message({ what: "rows-get-3" }, { filter: filter })).then(function (message) {
            var ids = message.body.ids;
            return ids;
        });
    }

    //по массиву id отдаёт массив строк rows из кэша RowsCache
    service.getRowsCache = function (ids) {
        return $transport.send(new Message({ what: "rows-get-4" }, { ids: ids })).then(function (message) {
            var rows = message.body.rows;
            return rows;
        });

    }
    //
    service.getRecords = function (ids, start, end, type) {
        return $transport.send(new Message({ what: "records-get-load-pretty" }, { targets: ids, start: start, end: end, type: type })).then(function (message) {
            var records = message.body.records;
            return records;
        });

    }


    service.poll = function (ids, cmd) {
        var arg = {
            cmd: cmd,
            components: "Current",
            onlyHoles: false
        }
        return $transport.send(new Message({ what: "poll" }, { objectIds: ids, what: "all", arg: arg }));
    }
    //
    service.getRowCard = function (id, matrixId) {
        return $transport.send(new Message({ what: "row-get-card" }, { rowId: id, matrixId: matrixId })).then(function (message) {
            return message.body;
        });
    }
})

