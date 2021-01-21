angular.module("app")
.service("eventsSvc", function ($actions, $transport, $log, $q, $rootScope, $timeout, $filter, $list, $settings, $parse) {

    var service = this;

    var subs = {};

    service.events = [];
    service.eventsCache = {};
    service.eventsViewCount = 0;
    service.eventsAlarmCount = 0;
    service.viewEvents = [];

    var viewedCache = $settings.getEventsViewed();

    if (!viewedCache) {
        viewedCache = {};
    }
    service.alarmsEval = function (isNotViewed) {
        var count = 0;
        for (var i = 0; i < service.events.length; i++) {
            var ev = service.events[i];
            if (!ev.link.end) {
                if (!isNotViewed || !ev.link.view) {
                    count++;
                }
            }
        }
        return count;
    }

    var whiteIds = {};

    $transport.send(new Message({ what: "rows-get-ids" }, { filter: { text: "", groups: [] } }))

		.then(function (message) {

		    if (message.head.what == "rows-get-ids") {
		        var ids = $parse('body.ids')(message) || [];
		        for (var i = 0; i < ids.length; i++) {
		            whiteIds[ids[i]] = 1;
		        }
		    }
		})

		.finally(function () {
		    $transport.send(new Message({ what: "setpoint-event" }))
				.then(function (message) {
				    var events = message.body.events;
				    var ids = [];
				    if (!events) return;
				    for (var key in events) {
				        if (!events.hasOwnProperty(key)) continue;

				        var ev = events[key];
				        if (!whiteIds[ev.objectId]) continue;

				        ids.push(ev.objectId);
				    }

				    if (ids.length > 0) {
				        $list.getRowsCacheFiltered({ ids: ids }).then(function (msg) {
				            var rows = msg.rows;
				            var count = msg.count;

				            for (var key in events) {
				                if (!events.hasOwnProperty(key)) continue;

				                var ev = events[key];
				                if (!whiteIds[ev.objectId]) continue;

				                var row = undefined;
				                for (var i = 0; i < rows.length; i++) {
				                    if (rows[i].id == ev.objectId) {
				                        row = rows[i];
				                        break;
				                    }
				                }

				                var start = $filter('date')(ev.start ? ev.start : Date(0), 'yyyyMMdd-HHmmss');
				                ev.id = ev.objectId + "-" + ev.param + "-" + start;
				                ev.row = row;
				                ev.view = viewedCache[ev.id] ? true : false;

				                if (!service.eventsCache[ev.id])      // не было
				                {
				                    var evLink = {};
				                    service.eventsCache[ev.id] = evLink;
                                    service.events.push(evLink);
                                    if (!ev.view) {
                                        service.viewEvents.push(evLink);
                                    }
				                }
				                service.eventsCache[ev.id].link = ev;
				            }

				            for (var key in subs) {
				                if (subs.hasOwnProperty(key)) {
				                    var sub = subs[key];
				                    if (sub.onUpdate) {
				                        $timeout(sub.onUpdate);
				                    }
				                }
				            }
				        });
				    }
				});
		});

    service.subscribe = function (id, onUpdate) {
        if (id && onUpdate) {
            if (!subs[id]) {
                subs[id] = {};
            }
            subs[id].onUpdate = onUpdate;
        }
    }

    service.unsubscribe = function (id) {
        if (id && subs[id]) {
            delete subs[id];
        }
    }

    service.setEventsViewCount = function (newvalue) {
        if ((newvalue <= service.eventsViewCount) || (newvalue > service.events.length)) return;
        service.eventsViewCount = newvalue;

        for (var key in subs) {
            if (subs.hasOwnProperty(key)) {
                var sub = subs[key];
                if (sub.onUpdate) {
                    sub.onUpdate();
                }
            }
        }
    }

    service.setEventsView = function () {

        var updated = false;
        for (var i = 0; i < service.events.length; i++) {
            var ev = service.events[i];
            if (!ev.link.view) {
                ev.link.view = true;
                updated = true;
                viewedCache[ev.link.id] = true;
            }
        }

        if (!updated) return;

        for (var key in subs) {
            if (subs.hasOwnProperty(key)) {
                var sub = subs[key];
                if (sub.onUpdate) {
                    sub.onUpdate();
                }
            }
        }

        $settings.setEventsViewed(viewedCache);
    }

    var listener = ($rootScope.$on("transport:message-received", function (e, message) {
        if (message.head.what == "setpoint") {
            var events = message.body.events;               // новые события

            if (events.length == 0) return;

            var ids = [];
            var hasNewEvents = false;
            for (var i = 0; i < events.length; i++) {
                var ev = events[i];
                var start = $filter('date')(ev.start ? ev.start : Date(0), 'yyyyMMdd-HHmmss');
                if (!whiteIds[ev.objectId]) continue;

                ids.push(ev.objectId);
            }
            
            $list.getRowsCacheFiltered({ ids: ids }).then(function (msg) {
                var rows = msg.rows;
                var count = msg.count;

                var hasNewEvents = false;
                for (var i = 0; i < events.length; i++) {
                    var ev = events[i];
                    var start = $filter('date')(ev.start ? ev.start : Date(0), 'yyyyMMdd-HHmmss');
                    if (!whiteIds[ev.objectId]) continue;

                    var row = undefined;
                    for (var i = 0; i < rows.length; i++) {
                        if (rows[i].id == ev.objectId) {
                            row = rows[i];
                            break;
                        }
                    }

                    ev.id = ev.objectId + "-" + ev.param + "-" + start;
                    ev.row = row;
                    ev.view = false;

                    if (!service.eventsCache[ev.id]) {     // уже было - обновить                
                        var evLink = {};
                        service.eventsCache[ev.id] = evLink;
                        service.events.push(evLink);
                        if (!ev.view) {
                            service.viewEvents.push(evLink);
                        }
                        hasNewEvents = true;
                    }
                    service.eventsCache[ev.id].link = ev;
                }

                if (hasNewEvents) {
                    $actions.get("events-show").then(function (action) {
                        if (action && action.act) { action.act(); }
                    });
                }

                for (var key in subs) {
                    if (subs.hasOwnProperty(key)) {
                        var sub = subs[key];
                        if (sub.onUpdate) {
                            $timeout(sub.onUpdate);
                        }
                    }
                }
            });
        }
    }));
});