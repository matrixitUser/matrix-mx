'use strict';

var app = angular.module("app");
app.controller("HomeCtrl", function ($scope, $log, $rootScope, $state, $filter, $settings, $auth, $parse, $actions, $list, $folders, $listFilter, eventsSvc, metaSvc) {

    var filterDelayTimer;

    var menuActions = [
        { promise: $actions.get('device-list') },
        { promise: $actions.get('report-edit') },
        { promise: $actions.get('maquette-list') },
        { promise: $actions.get('mailer') },
        { promise: $actions.get('task-edit') },
        { promise: $actions.get('user-list') },
		//{ promise: $actions.get('manager-modems') },
        null,
        { promise: $actions.get('service') },
        { promise: $actions.get('vserial') },
        null,
        { promise: $actions.get('about') }
    ];

    var user = { name: '', login: '', isAdmin: false };
    var model = {
        navbarCollapsed: false,
        config: metaSvc.config,
        //
        foldersAreHidden: false,
        actionsAreHidden: false,
        windowsAreHidden: false,
        leftIsHidden: false,
        rightIsHidden: false,
        onlyLeftOrRightIsHidden: false,
        //
        toolpanelState: false,
        listColumnsStateSaved: false,
        listColumnsStateRestored: false,
        //
        filterText: "",
        //
        eventsAlarmType: 'none',
        eventsBadgeCounter: 0,
        //
        isHouseList: false
    };

    function reportArrayOfSels() {
        return { ids: $list.getSelectedIds() };
    }

    var reevalEventAlarms = function () {
        model.eventsBadgeCounter = eventsSvc.alarmsEval();
        model.eventsAlarmType = (model.eventsBadgeCounter == 0) ? 'none' : ((eventsSvc.alarmsEval(true) == 0) ? 'info' : 'danger');
    }

    eventsSvc.subscribe("home-ctrl", function () {
        reevalEventAlarms();
    });
    //reevalEventAlarms();


    var saveSettings = function () {
        $settings.setHomeVisibility({
            foldersAreHidden: model.foldersAreHidden,
            actionsAreHidden: model.actionsAreHidden,
            windowsAreHidden: model.windowsAreHidden,
            leftIsHidden: model.leftIsHidden,
            rightIsHidden: model.rightIsHidden,
            onlyLeftOrRightIsHidden: model.onlyLeftOrRightIsHidden
        });
    }

    var loadSettings = function () {
        var obj = $settings.getHomeVisibility();
        if (obj) {
            model.foldersAreHidden = obj.foldersAreHidden;
            model.actionsAreHidden = obj.actionsAreHidden;
            model.windowsAreHidden = obj.windowsAreHidden;
            model.leftIsHidden = obj.leftIsHidden;
            model.rightIsHidden = obj.rightIsHidden;
            model.onlyLeftOrRightIsHidden = obj.onlyLeftOrRightIsHidden;
        }
    }

    model.panelsVisibilityUpdateAndSave = function (toggle) {
        switch (toggle) {
            case 'folders':
                model.foldersAreHidden = !model.foldersAreHidden;
                break;
            case 'actions':
                model.actionsAreHidden = !model.actionsAreHidden;
                break;
            case 'windows':
                model.windowsAreHidden = !model.windowsAreHidden;
                break;
            default:
                return;
        }
        model.rightIsHidden = model.actionsAreHidden && model.windowsAreHidden;
        model.leftIsHidden = model.foldersAreHidden;
        model.onlyLeftOrRightIsHidden = (model.leftIsHidden && !model.rightIsHidden) || (!model.leftIsHidden && model.rightIsHidden);
        //
        saveSettings();
    }

    $scope.$watch('model.menu', function () {
        model.menuView = [];
        if (angular.isArray(model.menu) && model.menu.length > 0) {
            var isAlmost1Action = false;
            var isDivider = false;
            var sort = $filter('orderBy')(model.menu, 'index');
            for (var i = 0; i < sort.length; i++) {
                var action = sort[i];
                if (action.type == "divider") {
                    isDivider = true;
                } else {
                    if (isAlmost1Action && isDivider) {
                        isDivider = false;
                        model.menuView.push({ divider: i });
                    }
                    model.menuView.push(action);
                    isAlmost1Action = true;
                }

            }
        }
    }, true);


    model.getHeight = function (windowHeight, isFull) {
        if (isFull) {
            return windowHeight - 60;
        }
        return (windowHeight / 2) - 30;
    }

    var filterByText = {
        text: ""
    };
    $listFilter.register(filterByText);

    model.onFilterTextChange = function () {
        clearTimeout(filterDelayTimer);
        filterDelayTimer = setTimeout(function () {
            filterByText.text = model.filterText;
            filterByText.raise();
        }, 1000);
    }

    model.onFilterTextClear = function () {
        model.filterText = '';
        model.onFilterTextChange();
    }

    model.toggleToolPanel = function () {
        model.toolpanelState = !model.toolpanelState;
        model.listColumnsStateSaved = false;
        model.listColumnsStateRestored = false;
        $rootScope.$broadcast("list:toggle-toolpanel", { newstate: model.toolpanelState });
    }

    model.listSaveColumnsState = function () {
        model.listColumnsStateSaved = true;
        model.listColumnsStateRestored = false;
        $rootScope.$broadcast("list:save-columns-state", null);
    }

    model.listRestoreColumnsState = function () {
        model.listColumnsStateSaved = false;
        model.listColumnsStateRestored = true;
        $rootScope.$broadcast("list:restore-columns-state", null);
    }
    
    $actions.get("row-editor").then(function (a) {
        if (a) {
            model.addNewObject = function () {
                a.act().then(function (isNew) {
                    $rootScope.$broadcast("listFilter:changed");
                });
            };
        }
    });

    $actions.get("house-editor").then(function (a) {
        if (a) {
            model.addNewHouse = function () {
                a.act().then(function (isNew) {
                    $rootScope.$broadcast("listFilter:changed");
                });
            };
        }
    });

    $actions.get("calculator-modal").then(function (a) {
        if (a) {
            model.calculatorModal = function () {
                a.act(reportArrayOfSels()).then(function (isNew) {
                    $rootScope.$broadcast("listFilter:changed");
                });
            };
        }
    });
    $actions.get("report-edit").then(function (a) {
        if (a) {
            model.addNewReport = function () {
                a.act({ isNew: true });
            };
        }
    });


    $actions.get("mailer-edit").then(function (a) {
        if (a) {
            model.addNewMailer = function () {
                a.act({ isNew: true });
            };
        }
    });

    $actions.get("events-show").then(function (a) {
        if (a) {
            model.showEvents = function () {
                a.act();
            };
        }
    });

    $actions.get("folders").then(function (a) {
        if (a) {
            model.showFolders = function () {
                a.act();
            };
        }
    });

    $actions.get("actions").then(function (a) {
        if (a) {
            model.showActions = function () {
                a.act();
            };
        }
    });

    $actions.get("windows").then(function (a) {
        if (a) {
            model.showWindows = function () {
                a.act();
            };
        }
    });
    
    model.selectAll = function () {
        $rootScope.$broadcast("list:select-all", null);
    }

    $scope.signout = function () {
        $auth.signout();
    }

    $auth.getSession().then(function (session) {
        user.isAdmin = $parse("user.isAdmin")(session);
        user.login = $parse("user.login")(session);
        user.name = $parse("user.name")(session);
    });


    model.menu = (function () {

        var options = [

        ];

        var actGetParam = function (a, gp, en) {
            return function ($item) {
                if (!en || en()) {
                    a.act(gp == undefined ? gp : gp());
                } else {
                    $log.debug(a.header + ": Не доступно");
                }
            }
        }

        for (var i = 0; i < menuActions.length; i++) {
            var menuAction = menuActions[i];
            if (menuAction === null) {
                options.push({
                    index: i,
                    type: 'divider'
                });
                continue;
            }

            (function (i) {
                var ma = menuActions[i];
                ma.promise.then(function (action) {
                    if (action == null) return;

                    var getParam = ma.getParam;

                    var actGetParam = function (a, gp) {
                        return function ($item) {
                            a.act(gp == undefined ? gp : gp());
                        }
                    }

                    var title = ma.title || action.header;

                    options.push({
                        index: i,
                        icon: action.icon,
                        title: title,
                        type: 'html',
                        action: actGetParam(action, getParam)
                    });
                })
            })(i);
        }

        return options;

    })();

    loadSettings();

    $scope.model = model;
    $scope.user = user;

    $scope.$on('$destroy', function () {
        $log.debug("уничтожается home");
    });
});

