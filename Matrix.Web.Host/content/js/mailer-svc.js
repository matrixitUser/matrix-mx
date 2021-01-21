angular.module("app")
.service("mailerSvc", function ($transport) {

    var service = this;

    service.kinds = [
        { text: "Отправка запрещена", value: "disabled" },
        { text: "Не задано (вручную)", value: "manual" },//по умолчанию
        { text: "По расписанию (автоматически)", value: "auto" }
    ];

    service.ranges = [
        { text: "День", value: "Day" },//по умолчанию
        { text: "Месяц", value: "Month" }
    ];

    service.all = function () {
        return $transport.send(new Message({ what: "mailer-list" }));
    };

    service.get = function (id) {
        return $transport.send(new Message({ what: "mailer-get" }, { id: id }));
    };

    service.send = function (id, date) {
        return $transport.send(new Message({ what: "mailer-send" }, { id: id, date: date }));
    }
});