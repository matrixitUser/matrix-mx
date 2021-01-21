//agGrid.initialiseAgGridWithAngular1(angular);

'use strict';

var app = angular.module("app", [
	"ui.router",
    "ui.bootstrap",
    "ui.bootstrap.contextMenu",
    "ui.bootstrap.datetimepicker",  // использую в макетах 80020
    "ui.ace",
    "agGrid",
    "base64", "naif.base64",
    "angularMoment",
    "formstamp",
    "xeditable",
    "ui.select",
    "ui.tree",
    "angular-md5",
	"simpleGrid",
    "jsTree.directive",
    "angular-cron-jobs"
]);

var Message = function (head, body) {
    var self = this;
    self.head = head;
    self.body = body;
    return self;
};

app.config(['$stateProvider', '$urlRouterProvider', '$tooltipProvider', '$sceProvider', function ($stateProvider, $urlRouterProvider, $tooltipProvider, $sceProvider) {
    $sceProvider.enabled(false);
    $tooltipProvider.options({ popupDelay: 333 });
    $urlRouterProvider.otherwise("/main");

    $stateProvider
        .state('site', {
            'abstract': true,
            resolve: {
                authorize: function ($auth, $log) {
                    $log.debug("state site resolve /authorize");
                    return $auth.authorize();
                }
            }
        })
        .state("signin", {
            parent: 'site',
            url: '/signin',
            views: {
                "@": {
                    templateUrl: 'tpls/signin.html',
                    controller: 'SigninCtrl'
                }
            }
        })
        .state("main", {
            parent: 'site',
            url: "/main",
            data: { isRestricted: true },
            views: {
                "@": {
                    templateUrl: "tpls/home.html",
                    controller: "HomeCtrl"
                }
            }
        });
}
]);

app.run(function ($rootScope, $state, $stateParams, $log, metaFunctionsSvc, $templateCache, $auth, $transport, editableOptions) {

    $log.debug("старт приложения");

    $rootScope.$on('$stateChangeStart', function (event, toState, toStateParams) {
        $log.debug("statechange смена состояния на %s /authorize /connect", toState.name);

        $rootScope.toState = toState;
        $rootScope.toStateParams = toStateParams;

        $auth.authorize();
    });

    ////

    metaFunctionsSvc.appStart();

    editableOptions.theme = 'bs3';
});

