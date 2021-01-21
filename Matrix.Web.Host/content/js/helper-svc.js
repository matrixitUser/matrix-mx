angular.module("app")
.service("$helper", function ($transport) {
    var service = this;

    service.createGuid = function (count) {
        if (count === undefined) count = 1;
        return $transport.send(new Message({ what: "helper-create-guid" }, { count: count }));
    }

    var tableStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    var table = tableStr.split("");

    var atob = function (base64) {
        if (/(=[^=]+|={3,})$/.test(base64)) throw new Error("String contains an invalid character");
        base64 = base64.replace(/=/g, "");
        var n = base64.length & 3;
        if (n === 1) throw new Error("String contains an invalid character");
        for (var i = 0, j = 0, len = base64.length / 4, bin = []; i < len; ++i) {
            var a = tableStr.indexOf(base64[j++] || "A"), b = tableStr.indexOf(base64[j++] || "A");
            var c = tableStr.indexOf(base64[j++] || "A"), d = tableStr.indexOf(base64[j++] || "A");
            if ((a | b | c | d) < 0) throw new Error("String contains an invalid character");
            bin[bin.length] = ((a << 2) | (b >> 4)) & 255;
            bin[bin.length] = ((b << 4) | (c >> 2)) & 255;
            bin[bin.length] = ((c << 6) | d) & 255;
        };
        return String.fromCharCode.apply(null, bin).substr(0, bin.length + n - 4);
    };

    var btoa = function (bin) {
        for (var i = 0, j = 0, len = bin.length / 3, base64 = []; i < len; ++i) {
            var a = bin.charCodeAt(j++), b = bin.charCodeAt(j++), c = bin.charCodeAt(j++);
            if ((a | b | c) > 255) throw new Error("String contains an invalid character");
            base64[base64.length] = table[a >> 2] + table[((a << 4) & 63) | (b >> 4)] +
                                    (isNaN(b) ? "=" : table[((b << 2) & 63) | (c >> 6)]) +
                                    (isNaN(b + c) ? "=" : table[c & 63]);
        }
        return base64.join("");
    };

    service.hexToBase64 = function (str) {
        return btoa(String.fromCharCode.apply(null,
          str.replace(/\r|\n/g, "").replace(/([\da-fA-F]{2}) ?/g, "0x$1 ").replace(/ +$/, "").split(" "))
        );
    }

    service.base64ToHex = function (str) {
        for (var i = 0, bin = atob(str.replace(/[ \r\n]+$/, "")), hex = []; i < bin.length; ++i) {
            var tmp = bin.charCodeAt(i).toString(16);
            if (tmp.length === 1) tmp = "0" + tmp;
            hex[hex.length] = tmp;
        }
        return hex.join(" ");
    }

    service.months0 = ["январь", "февраль", "март", "апрель", "май", "июнь", "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь"];
    service.months1 = ["января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря"];

    service.getMonthAsString = function(month, case0) {
        if (month && month >= 1 && month <= 12) {
            month--;
            if (case0 == 1) {
                return service.months1[month];
            } else {
                return service.months0[month];
            }
        }
        return "";
    }

    service.assocToArray = function (assoc) {
        var key, arr = [];
        for (key in assoc) {
            if (assoc.hasOwnProperty(key)) {
                arr.push(key);
            }
        }
        return arr;
    }


    service.isArray = function (check) {
        if (Object.prototype.toString.call(check) === '[object Array]') {
            return true;
        }
        return false;
    }


    service.isObject = function (check) {
        if (Object.prototype.toString.call(check) === '[object Object]') {
            return true;
        }
        return false;
    }


    service.isString = function (check) {
        if (typeof (check) === 'string') {
            return true;
        }
        return false;
    }


    service.arrayToAssoc = function (array, key, body) {//array of objects in assoc.array with key and body fields in body
        var result = {};
        if (service.isArray(array) && array.length > 0 && service.isString(key) && key != "") {
            for (var i = 0; i < array.length; i++) {
                var item = array[i];
                if (service.isObject(item) && item.hasOwnProperty(key)) {
                    var keyValue = item[key];
                    var itemValue = {};
                    if (service.isArray(body)) {
                        service.copyToFrom(itemValue, item, body);
                    } else {
                        itemValue = item;
                    }
                    result[keyValue] = itemValue;
                }
            }
        }
        return result;
    }


    service.areStringArraysEqual = function (a1, a2) {
        if (!service.isArray(a1) || !service.isArray(a2) || a1.length != a2.length) return false;

        if (a1 == a2) return true;

        for (var i = 0; i < a1.length; i++) {
            for (var j = 0; j < a2.length; j++) {
                if (a1[i] == a2[j]) break;
            }
            if (j == a2.length) return false;
        }

        return true;
    }


    service.rowParameterIsTagged = function (data) {
        if (data.Parameter && data.Parameter.length > 0) {
            for (var i = 0; i < data.Parameter.length; i++) {
                var par = data.Parameter[i];
                if (par.tag) {
                    return true;
                }
            }
        }
        return false;
    };

    service.copyToFrom = function (to, from, keys) {
        var key;
        if (keys) {
            for (var i = 0; i < keys.length; i++) {
                key = keys[i];
                if (from.hasOwnProperty(key)) {
                    if (service.isArray(from[key])) {
                        to[key] = [];
                        for (var j = 0; j < from[key].length; j++) {
                            var kval = from[key][j];
                            to[key].push(kval);
                        }
                    } else {
                        to[key] = from[key];
                    }
                }
            }
        } else {
            for (key in from) {
                if (from.hasOwnProperty(key)) {
                    to[key] = from[key];
                }
            }
        }
    };

    service.areEqual = function (to, from, keys) {
        var key;
        if (keys) {
            for (var i = 0; i < keys.length; i++) {
                key = keys[i];
                if (from.hasOwnProperty(key)) {
                    if (service.isArray(from[key])) {
                        if (from[key].length != to[key].length) return false;
                        for (var j = 0; j < from[key].length; j++) {
                            //var kval = from[key][j];
                            //to[key].push(kval);
                            if (to[key][j] !== from[key][j]) {
                                return false;
                            }
                        }
                    } else {
                        if (to[key] !== from[key]) {
                            return false;
                        }
                    }
                } else if (to.hasOwnProperty(key)) {
                    return false;
                }
            }
        } else {
            for (key in from) {
                if (from.hasOwnProperty(key)) {
                    if (to[key] !== from[key]) {
                        return false;
                    }
                } else if (to.hasOwnProperty(key)) {
                    return false;
                }
            }
        }
        return true;
    };

    //A1 - A2 = [1,2,3] - [3,4,5] = [1,2]
    service.arrayDiff = function (a1, a2) {
        if (!service.isArray(a1) || !service.isArray(a2)) return [];
        var diff = [];

        for (var i = 0; i < a1.length; i++) {
            for (var j = 0; j < a2.length; j++) {
                if (a1[i] === a2[j]) {
                    break;
                }
            }
            //
            if (j == a2.length) {//not found in a1
                diff.push(a1[i]);
            }
        }

        return diff;
    }

    service.getSignalImg = function (signal) {
        var s = !signal || signal < 10 ? 'no' :
            (signal < 25 ? '1-6' :
            (signal < 40 ? '2-6' :
            (signal < 55 ? '3-6' :
            (signal < 70 ? '4-6' :
            (signal < 85 ? '5-6' : '6-6')))));
        return "signal_" + s + ".png";
    }

    service.overlayFunc = function (promise, text) {
        return (function (m) {
            m.overlayText = text || "Загрузка...";
            m.overlayEnabled = true;
            return promise.finally(function () {
                m.overlayEnabled = false;
                m.overlayText = "";
            })
        })(this);
    }
});
