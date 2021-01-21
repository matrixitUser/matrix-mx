angular.module("app")
.service("$settings", function ($log, $filter, metaSvc) {

    var service = this;

    var version = metaSvc.version || "";
    var SESSION_ID_KEY = "session-id";
    var USER_ID_KEY = "user-id";
    var COLUMN_STATE_KEY = "column-state" + version;
    var HOME_VISIBILITY = "home-visibility" + version;
    var ROW_PINS = "row-pins" + version;
    var PARAM_COLUMN_STATE_KEY = "param-column-state" + version;
    var EVENTS_COLUMN_STATE_KEY = "events-column-state" + version;
    var EVENTS_VIEWED = "events-viewed";
    var REPORTLIST_DATE = "events-viewed";

    var getReportlistRangeString = function (type) {
        return REPORTLIST_DATE + type + "_" + version;
    }

    //

    service.getSessionId = function () {
        return localStorage.getItem(SESSION_ID_KEY);
    };

    service.setSessionId = function (sessionId) {
        localStorage.setItem(SESSION_ID_KEY, sessionId);
    };

    //

    service.getUser = function () {
        var user = localStorage.getItem(USER_ID_KEY);
        $log.debug("settings запрос юзера " + user);
        return user ? angular.fromJson(user) : { name: '', login: '' };
    };

    service.setUser = function (user) {
        var jsonuser = angular.toJson(user);//$filter('json')(user);
        $log.debug("settings сохранение юзера " + jsonuser);
        localStorage.setItem(USER_ID_KEY, jsonuser);
    };

    //

    service.getListColumnState = function () {
        var jsoned = localStorage.getItem(COLUMN_STATE_KEY);
        $log.debug("settings запрос настроек колонок грида");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setListColumnState = function (state) {
        var jsoned = angular.toJson(state);
        $log.debug("settings сохранение настроек колонок грида");
        localStorage.setItem(COLUMN_STATE_KEY, jsoned);
    };


    service.getState = function (ofObject) {
        var jsoned = localStorage.getItem(ofObject + version);
        $log.debug("settings запрос настроек " + ofObject);
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setState = function (ofObject, state) {
        var jsoned = angular.toJson(state);
        $log.debug("settings сохранение настроек " + ofObject);
        localStorage.setItem(ofObject + version, jsoned);
    };



    service.getParametersColumnState = function () {
        var jsoned = localStorage.getItem(PARAM_COLUMN_STATE_KEY);
        $log.debug("settings запрос настроек колонок табл. параметров");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setParametersColumnState = function (state) {
        var jsoned = angular.toJson(state);
        $log.debug("settings сохранение настроек колонок табл. параметров");
        localStorage.setItem(PARAM_COLUMN_STATE_KEY, jsoned);
    };

    service.getEventsColumnState = function () {
        var jsoned = localStorage.getItem(EVENTS_COLUMN_STATE_KEY);
        $log.debug("settings запрос настроек колонок табл. событий");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setEventsColumnState = function (state) {
        var jsoned = angular.toJson(state);
        $log.debug("settings сохранение настроек колонок табл. событий");
        localStorage.setItem(EVENTS_COLUMN_STATE_KEY, jsoned);
    };

    //

    service.getHomeVisibility = function () {
        var jsoned = localStorage.getItem(HOME_VISIBILITY);
        $log.debug("settings запрос настроек показа home");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setHomeVisibility = function (obj) {
        var jsoned = angular.toJson(obj);
        $log.debug("settings сохранение настроек показа home");
        localStorage.setItem(HOME_VISIBILITY, jsoned);
    };

    // events

    service.getEventsViewed = function () {
        var jsoned = localStorage.getItem(EVENTS_VIEWED);
        $log.debug("settings запрос просмотренных событий");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setEventsViewed = function (obj) {
        var jsoned = angular.toJson(obj);
        $log.debug("settings сохранение просмотренных событий");
        localStorage.setItem(EVENTS_VIEWED, jsoned);
    };

    //

    //

    service.getRowPins = function () {
        var jsoned = localStorage.getItem(ROW_PINS);
        $log.debug("settings запрос пинов");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setRowPins = function (obj) {
        var jsoned = angular.toJson(obj);
        $log.debug("settings сохранение пинов");
        localStorage.setItem(ROW_PINS, jsoned);
    };

    //

    service.hasReportlistRange = function (type) {
        var jsoned = localStorage.getItem(getReportlistRangeString(type));
        return jsoned ? true : false;
    };

    service.getReportlistRange = function (type) {
        var jsoned = localStorage.getItem(getReportlistRangeString(type));
        $log.debug("settings запрос настроек reportlist-date");
        return jsoned ? angular.fromJson(jsoned) : undefined;
    };

    service.setReportlistRange = function (type, obj) {
        var jsoned = angular.toJson(obj);
        $log.debug("settings сохранение настроек reportlist-date");
        localStorage.setItem(getReportlistRangeString(type), jsoned);
    };
});