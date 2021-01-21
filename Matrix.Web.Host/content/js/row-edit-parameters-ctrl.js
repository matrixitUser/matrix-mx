angular.module("app")
.controller('RowEditParametersCtrl', function ($scope, $uibModalInstance, data, $filter, metaSvc) {

    var channelContent = metaSvc.rowParameters;

    function parseChannels(parameters) {
        var channels = [];
        if ((parameters === undefined) || (parameters === "")) {
            return channels;
        }

        var channels_text = parameters.split('|');
        //
        for (var i = 0; i < channels_text.length; i++) {
            var channel = { n: 0 };
            var channel_text = channels_text[i].split(';');
            //
            for (var j = 0; j < channel_text.length; j++) {
                var name = channelContent[j].name;
                channel[name] = channel_text[j];
            }
            //
            if (channel.number) {
                var num = parseInt(channel.number);
                if (!isNaN(num)) {
                    channel.n = num;
                }
            }
            //
            channels.push(channel);
        }
        //
        var sort = $filter('orderBy')(channels, 'n');
        return sort;
    }

    function makeParameters(channels) {
        var parameters = [];
        //
        for (var i = 0; i < channels.length; i++) {
            var channel = channels[i];
            var parameter = [];
            for (var j = 0; j < channelContent.length; j++) {
                var name = channelContent[j].name;
                parameter.push(channel[name] || "");
            }
            parameters.push(parameter.join(';'));
        }
        //
        return parameters.join('|');
    }


    //function 


    var model = {
        channelContent: channelContent,
        lastError: ""
    };

    model.parameters = data.parameters;
    model.isEditable = data.isEditable || false;

    model.channels = parseChannels(data.parameters);

    model.deleteChannel = function (index) {
        model.channels.splice(index, 1);
    }

    model.addChannel = function () {
        var maxn = 0;
        for (var i = 0; i < model.channels.length; i++) {
            var channel = model.channels[i];
            if (channel.n && (maxn < channel.n)) {
                maxn = channel.n;
            }
        }
        model.channels.push({ n: maxn + 1, number: (maxn + 1).toString() });
    }

    var checkChannels = function () {
        for (var j = 0; j < channelContent.length; j++) {
            var parameter = channelContent[j];
            var required = parameter.required || false;
            var unique = parameter.unique || false;

            for (var i = 0; i < model.channels.length; i++) {
                //проверка на ошибки:
                var channel = model.channels[i];
                var p = channel[parameter.name];

                if (p === undefined) {
                    p = "";
                }

                //+не пустота
                if (p === "") {
                    if (required) {
                        model.lastError = "Параметр \"" + parameter.caption + "\" не должен быть пустым";
                        return false;
                    } else if (parameter.init !== undefined) {
                        p = parameter.init;
                    }
                }

                p = p.toString();

                if (p !== "") {
                    //+проверка типов       
                    switch (parameter.type) {
                        case 'float':
                            var num = parseFloat(p.replace(/,/g, '.'));
                            if (isNaN(num)) {
                                model.lastError = "Параметр \"" + parameter.caption + "\" не является числом";
                                return false;
                            } else {
                                p = num.toString().replace(/\./g, ',');
                            }
                            break;

                        case 'integer':
                            var num = parseInt(p);
                            if (isNaN(num)) {
                                model.lastError = "Параметр \"" + parameter.caption + "\" не является целым числом";
                                return false;
                            } else {
                                p = num.toString();
                            }
                            break;
                    }

                    if (p !== channel[parameter.name]) {
                        channel[parameter.name] = p;
                    }
                }
            }

            //-уникальность
            if (unique) {
                for (var i = 0; i < model.channels.length; i++) {
                    var p = model.channels[i][parameter.name];
                    for (var k = i + 1; k < model.channels.length; k++) {
                        var p1 = model.channels[k][parameter.name];
                        if (!!p && p !== "" && p === p1) {
                            model.lastError = "Параметр \"" + parameter.caption + "\" должен быть уникальным";
                            return false;
                        }
                    }
                }
            }
        }
        model.lastError = "";
        return true;
    }

    $scope.ok = function () {
        if (checkChannels()) {
            model.parameters = makeParameters(model.channels);
            $uibModalInstance.close(model.isEditable ? model.parameters : data.parameters);
        }
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.model = model;
});