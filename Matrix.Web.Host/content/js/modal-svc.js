angular.module("app")
/**
 * рисует окно, 
 * в скоупе есть метод addCleaner
 */
.service("$mxModal", function ($compile, $rootScope, $templateCache, $http, $q) {

    var show2 = function (options) {
        options.title = options.title || "";
        options.size = options.size || { width: 500, height: 500 };
        options.resizeable = options.resizeable || "disabled";
        options.tpl = options.tpl || "";
        options.controls = options.controls || { buttons: true };
        options.footerTpl = options.footerTpl || "/tpls/footer-close.html";
        options.headerTpl = options.headerTpl || "";
        options.onOpen = options.onOpen || function () { };
        options.onClose = options.onClose || function () { };

        var $scope = $rootScope.$new();
        $scope.onOpen = options.onOpen;
        $scope.onClose = options.onClose;

        $scope.model = { data: options.data };

        var display = function () {
            show($scope, {
                selector: "#m-maximize",
                position: "center",
                title: options.title,
                size: options.size,
                show: options.show,
                resizeable: options.resizeable,
                controls: options.controls,
                //bootstrap: "primary",
                overflow: "scroll",
                toolbarHeader: options.headerTpl == "" ? [] : $templateCache.get(options.headerTpl),
                toolbarFooter: $templateCache.get(options.footerTpl),
                content: $templateCache.get(options.tpl)
            });
        };

        var loadTemplate = function (tpl) {
            var ret = $templateCache.get(tpl);
            if (ret) return ret;

            return $http({ method: "GET", url: tpl }).then(function (result) {
                var templateHtml = result.data;
                $templateCache.put(tpl, templateHtml);
                return result;
            }, function (err) {
                return $q.reject("не удалось загрузить шаблон окна " + template + ": " + err);
            });
        }

        $q.all([$q.when(loadTemplate(options.tpl)), $q.when(loadTemplate(options.headerTpl)), $q.when(loadTemplate(options.footerTpl))]).then(display);
    }

    var show = function ($scope, options) {
        var ha = $.jsPanel(options);
        var cleaners = [];
        $scope.addCleaner = function (cleaner) {
            cleaners.push(cleaner);
        }
        $scope.close = function () {
            for (var i = 0; i < cleaners.length; i++) {
                var cleaner = cleaners[i];
                cleaner();
            }
            $scope.onClose();
            ha.close();
        };
        $scope.cancel = function () {
            $scope.onClose();
            ha.close();
        };
        $compile(angular.element(ha[0]))($scope);

        $scope.onOpen();
    };
    return {
        show: show,
        show2: show2
    }
});
