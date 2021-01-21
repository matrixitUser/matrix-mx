var app = angular.module("app");

// попытка авторизоваться через auth, зная логин и пароль. в ответ получаем либо ошибку, либо идет переход на главную страницу
// возможно, юзер уже авторизован. проверяем это при загрузке контроллера

app.controller('SigninCtrl', function ($scope, $auth, $log, $state, $settings, $parse) {

    var model = {
        loginError: ""			//текст ошибки
    };

    model.signin = function (obj) {
        var login = $("#login").val();
        var password = $("#password").val();
        $auth.signin(login, password)
    		.then(function (msg) {
    		    model.loginError = "";
    		})
            .catch(function (err) {
    		    model.password = "";
    		    model.loginError = err;
    		});
    }

    model.demo = function () {
        $auth.signin('demo', 'demo')
    		.then(function (msg) {
    		    model.loginError = "";
    		})
            .catch(function (err) {
                model.loginError = err;
            });
    }

    //

    $scope.model = model;

    //

    //(function () {
    //    $log.debug("signin-ctrl проверка наличия авторизации по сессии");
    //    if ($auth.isAuthenticated()) {
    //        $auth.getSession().then(function (session) {
    //            model.login = $parse('user.login')(session);
    //        });
    //        //model.login = $parse('user.login')();
    //        //$state.go('main');
    //    }
    //})();

    //$auth.signin();
});