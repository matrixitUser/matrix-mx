angular.module("app")
.service("$auth", function ($settings, $transport, $rootScope, $log, $parse, $state, $q, $timeout, $filter, md5, metaSvc) {

    function isUndefined(somewhat) {
        return (somewhat == undefined || somewhat == null || somewhat == "");
    }

    //

    var session = null;

    function isAuthenticated() {
        return !isUndefined(session);
    }

    function authenticate(newsession) {
        if (!isUndefined(newsession)) {//пришла новая сессия
            if (!isUndefined(session)) {//какая-то сессия уже есть
                //$log.debug("СЕССИЯ существует %s==%s", session.id, newsession.id);
                if (session.id !== newsession.id) {//новая сессия! 
                    deauthenticate();
                }
            }

            if (isUndefined(session)) {
                //$log.debug("СЕССИЯ новая");
                session = newsession;
                $settings.setSessionId(session.id);
                $rootScope.$broadcast("auth:authorized");
            }
        } else {
            //$log.debug("СЕССИИ oldsession=%s newsession=%s", session, newsession);
        }
    }

    function deauthenticate() {
        if (!isUndefined(session)) {
            //$log.debug("СЕССИЯ уничтожена");
            $rootScope.$broadcast("auth:deauthorized", { sessionId: session.id });
            $settings.setSessionId("");
            session = null;
        } else {
            //$log.debug("СЕССИЯ УЖЕ уничтожена");
        }
    }

    /**
     * отправка логина или сессии
     */

    var sendAuthInfo = function (info) {
        var request;
        if ("sessionId" in info) {
            $log.debug("auth начало авторизации по сессии " + info.sessionId);
            request = new Message({ what: "auth-by-session" }, {
                sessionId: info.sessionId
            });
        } else if ("login" in info) {
            var passwordHash = (metaSvc.config === "orenburg")? info.password : md5.createHash(info.password || '');
            $log.debug("auth начало авторизации по логину " + info.login);
            request = new Message({ what: "auth-by-login" }, {
                login: info.login === undefined ? "" : info.login,
                password: info.password === undefined ? "" : passwordHash
            });
        }


        //разбор ответа от сервера: сессия при успехе или reject при неудаче
        return $transport.send(request, true).then(function (answer) {
            if (answer.head.what === "auth-success") {
                var session = {
                    id: $parse('body.sessionId')(answer),
                    user: {
                        login: $parse('body.user.login')(answer),
                        name: $parse('body.user.name')(answer),
                        isAdmin: $parse('body.user.isAdmin')(answer) || false //по умолчанию: НЕ админ
                    }
                }
                $log.debug("auth получена сессия " + session.id + " логин " + session.user.login);
                return session;
            }
            return $q.reject(answer.body.message === undefined ? "Произошла ошибка при авторизации" : answer.body.message);
        });
    };

    //получение сессии (с инфо о юзере)
    var getSession = function (force) {
        $log.debug("auth получение сессии %s => по-быстрому %s", session, isAuthenticated());
        var deferred = $q.defer();
        if (force === true) deauthenticate();

        //по-быстрому?
        if (isAuthenticated()) {
            deferred.resolve(session);
            return deferred.promise;
        }

        //получение сессии
        var sessionId = $settings.getSessionId();
        if (isUndefined(sessionId)) {
            $log.debug("auth не найдена сессия");
            deferred.resolve(undefined);
            return deferred.promise;
        }

        //запрос с сервера
        return sendAuthInfo({ sessionId: sessionId }).then(function (answer) {
            authenticate(answer);
            deferred.resolve(session);
            return deferred.promise;
        }, function (err) {
            deauthenticate();
            deferred.resolve(session);
            return deferred.promise;
        });
    }

    var authorize = function () {
        return getSession().then(function (session) {
            if ($rootScope.toState.data && $rootScope.toState.data.isRestricted && !isAuthenticated()) {
                $log.debug("authorize fail: смена на signin");
                $state.go('signin');
            }
        });
    }

    var signin = function (login, password) {
        if (!login) {
            $log.debug("auth не введены авторизационные данные");
            return $q.reject("введите логин и пароль");
        }

        return sendAuthInfo({ login: login, password: password }).then(function (answer) {
            authenticate(answer);
            $log.debug("auth получена сессия " + session.id + " логин " + session.user.login);
            $state.go('main');
        });
    }

    var verification = function (login, password) {
        var request;
        var passwordHash = (metaSvc.config === "orenburg") ? password : md5.createHash(password || '');
        request = new Message({ what: "auth-by-login1" }, {
            login: login === undefined ? "" : login,
            password: password === undefined ? "" : passwordHash
        });
       
        //разбор ответа от сервера: сессия при успехе или reject при неудаче
        return $transport.send(request, true).then(function (answer) {
            if (answer.head.what === "auth-success") {
                return answer;
            }
            return $q.reject(answer.body.message === undefined ? "Произошла ошибка при авторизации" : answer.body.message);
        });
    }

    var signout = function () {
        return getSession().then(function (session) {
            var p = $transport.send(new Message({ what: "auth-close-session" }, { sessionId: session.id })).then(function (message) {
                deauthenticate();
                $state.go('signin');
                if (message.head.what === "auth-session-closed") {
                    $log.debug("auth сервер потвердил закрытие сессии");
                } else {
                    var head = $parse('head.what')(message);
                    var ans = $parse('body.message')(message);
                    $log.debug("auth сервер вернул " + head + " с сообщением " + ans);
                    //return ans;
                }
            });
            return p;
        }, function (err) {
            $log.debug("auth выходим с ошибкой: не найдена сессия - " + err);
            deauthenticate();
            $state.go('signin');
        });
    };

    //////////////////////////

    /**
     * уведомление об успешной авторизации
     */
    //var raiseAuthorised = function (sessionId) {
    //    $settings.setSessionId(sessionId);
    //    $transport.connect();
    //    $rootScope.$broadcast("auth:authorized");
    //};

    ////////

    return {
        signin: signin,
        signout: signout,
        authorize: authorize,
        verification: verification,
        isAuthenticated: isAuthenticated,
        getSession: getSession
    };
});
