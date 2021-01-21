app.service("$listFilter", function ($rootScope) {
    var filters = [];

    var register = function (filter) {
        filter.raise = function () {
            $rootScope.$broadcast("listFilter:changed");
        };
        filters.push(filter);
    };

    var getFilter = function () {
        var filter = {};
        for (var i = 0; i < filters.length; i++) {
            var f = filters[i];
            if (f.text !== undefined) {
                filter.text = f.text;
            }
            if (f.folderId !== undefined) {
                filter.folderId = f.folderId;
            }
            if (f.isDisabled !== undefined) {
                filter.isDisabled = f.isDisabled;
            }
            if (f.isDeleted !== undefined) {
                filter.isDeleted = f.isDeleted;
            }
            if (f.states !== undefined) {
                if (filter.states === undefined) {
                    filter.states = [];
                }
                for (var j = 0; j < f.states.length; j++) {
                    filter.states.push(f.states[j]);
                }
            }
            if (f.devices !== undefined) {
                if (filter.devices === undefined) {
                    filter.devices = [];
                }
                for (var j = 0; j < f.devices.length; j++) {
                    filter.devices.push(f.devices[j]);
                }
            }
        }
        return filter;
    };

    return {
        register: register,
        getFilter: getFilter
    };
});
