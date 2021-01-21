angular.module("app")
    .service("$billing", function ($transport, md5, metaSvc) {

    var recordSave = function (date, count, comment, objectId) {
        
        return $transport.send(new Message({ what: "records-save-count" }, { count: count, date: date, comment: comment, objectId: objectId }));
    }
    return {
        recordSave: recordSave,
    }
});