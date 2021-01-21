/// <binding />
/// <vs AfterBuild='all' />
module.exports = function (grunt) {
    grunt.initConfig({
        clean: [
            "content/dist/*",
        ],
        concat: {
            lib: {
                src: [
                    "content/lib/jquery-2.1.0.js",
                    "content/lib/angular/angular.js",
                    "content/lib/angular-i18n/angular-locale_ru-ru.js",
                    "content/lib/sha1/sha1.js",
                    "content/lib/chart.js/chart.js",
                    "content/lib/chart.js/drawer.js",
                    "content/lib/amcharts/amcharts/amcharts.js",
                    "content/lib/amcharts/amcharts/serial.js",
                    "content/lib/amcharts/amcharts/themes/light.js",
                    "content/lib/amcharts/amcharts/lang/ru.js",
                    "content/lib/amcharts/amchart-adapter.js",
                    "content/lib/amcharts/amcharts/plugins/export/export.min.js",
                    "content/lib/moment/moment-with-locales.js",
                    "content/lib/moment/angular-moment.min.js",
                    "content/lib/jspanel-2.5.2/vendor/jquery-2.1.4.min.js",
                    "content/lib/signalr-2.1.2/jquery.signalR-2.1.2.min.js",
                    "content/lib/angular-websocket-1.0.13/angular-websocket.min.js",
                    "content/lib/base64/angular-base64.min.js",
                    "content/lib/angular-base64-upload/angular-base64-upload.min.js",
                    "content/lib/angular-bootstrap/ui-bootstrap-tpls.min.js",
                    "content/lib/angular-bootstrap-datetimepicker/datetimepicker.js",
                    "content/lib/angular-formstamp/formstamp.js",
                    "content/lib/ag-grid/dist/ag-grid.js",
                    "content/lib/simple-grid/src/simple-grid.js",
                    "content/lib/ace-20150711/src-min-noconflict/ace.js",
                    "content/lib/ace-20150711/src-min-noconflict/mode-liquid.js",
                    "content/lib/ui-ace-0.2.3/ui-ace.min.js",
                    "content/lib/angular-xeditable-0.1.8/js/xeditable.min.js",
                    "content/lib/ui-bootstrap-contextmenu/contextMenu.js",
                    "content/lib/ui-router/angular-ui-router.min.js",
                    "content/lib/ui-select/select.min.js",
                    "content/lib/md5/md5.js",
                    "content/lib/angular-ui-tree/dist/angular-ui-tree.min.js",
                    "content/lib/angular-md5/angular-md5.min.js",
                    "content/lib/jstree/dist/jstree.js",
                    "content/lib/jsTree-directive/jsTree.directive.js",
                    //"content/lib/bootstrap-4.4.1/js/bootstrap.min.js",
                    //"content/lib/bootstrap-4.4.1/js/bootstrap.bundle.js",
                    "content/lib/angular-cron-jobs/angular-cron-jobs.js",
                    "content/lib/svg/polyfills.js",
                    "content/lib/svg/polyfillsIE.js",
                    "content/lib/svg/svg.js",
                    "content/lib/svg/svg.min.js",
                ],
                dest: 'content/dist/lib.js'
            },
            js: {
                src: [                    
                    "content/js/app.js",
                    "content/js/print-div-dir.js",
                    "content/js/resizable-dir.js",
                    "content/js/ng-type-dir.js",
                    "content/js/ng-disabled-dir.js",
                    "content/js/users-svc.js",
                    "content/js/users-ctrl.js",
                    "content/js/set-rights-ctrl.js",
                    "content/js/maquette-svc.js",
                    "content/js/maquette-ctrl.js",
                    "content/js/maquette-edit-ctrl.js",
                    "content/js/poll-svc.js",
                    "content/js/poll-ctrl.js",
                    "content/js/reports-svc.js",
                    "content/js/reports-ctrl.js",
                    "content/js/report-list-ctrl.js",
                    "content/js/report-edit-ctrl.js",
                    "content/js/actions-svc.js",
                    "content/js/actions-ctrl.js",
                    "content/js/settings-svc.js",
                    "content/js/transport-svc.js",
                    "content/js/vserial-svc.js",
                    "content/js/vserial-ctrl.js",
                    "content/js/flash-svc.js",
                    "content/js/flash-ctrl.js",
                    "content/js/folders-svc.js",
                    "content/js/folders-ctrl.js",
                    "content/js/folder-edit-ctrl.js",
                    "content/js/add-to-folder-ctrl.js",
                    "content/js/list-svc.js",
                    "content/js/list-filter-svc.js",
                    "content/js/list-ctrl.js",
                    "content/js/list-house-ctrl.js",
                    "content/js/object-card-ctrl.js",
                    "content/js/auth-svc.js",
                    "content/js/calculator-ctrl.js",
                    "content/js/control-svc.js",
                    "content/js/control-ctrl.js",
                    "content/js/log-svc.js",
                    "content/js/log-ctrl.js",
                    "content/js/home-ctrl.js",
                    "content/js/signin-ctrl.js",
                    "content/js/drivers-svc.js",
                    "content/js/drivers-ctrl.js",
                    "content/js/parameters-edit-ctrl.js",
                    "content/js/parameters-edit-classic-ctrl.js",
                    "content/js/house-ctrl.js",
                    "content/js/house-editor-ctrl.js",
                    "content/js/house-edit-parameters-ctrl.js",
                    "content/js/data-table-ctrl.js",
                    "content/js/test-ctrl.js",
                    "content/js/about-ctrl.js",
                    "content/js/windows-svc.js",
                    "content/js/windows-ctrl.js",
                    "content/js/row-editor-ctrl.js",
                    "content/js/helper-svc.js",
                    "content/js/manager-modems-ctrl.js",
                    "content/js/mailer-ctrl.js",
                    "content/js/mailer-edit-ctrl.js",
                    "content/js/mailer-svc.js",
                    "content/js/events-ctrl.js",
                    "content/js/events-svc.js",
                    "content/js/list-select-ctrl.js",
                    "content/js/meta-svc.js",
                    "content/js/task-svc.js",
                    "content/js/task-edit-ctrl.js",
                    "content/js/cron-select-ctrl.js",
                    "content/js/row-edit-parameters-ctrl.js",
                    "content/js/row-edit-network-ctrl.js",
                    "content/js/dialog-with-password-ctrl.js",
                    "content/js/service-ctrl.js",
                    "content/js/common-svc.js",
                    "content/js/modal-ctrl.js",
                    "content/js/maps-svc.js",
                    "content/js/billing-ctrl.js",
                    "content/js/billing-svc.js",
                    "content/js/valve-control-ctrl.js",
                    "content/js/valve-control-svc.js",
                    "content/js/row-edit-obises-ctrl.js",
                    "content/js/matrix-terminal-edit-ctrl.js"
                ],
                dest: 'content/dist/matrix.js'
            },
            css: {
                src: [
                    "content/lib/jspanel-2.5.2/vendor/jquery-ui-1.11.4.complete/jquery-ui.min.css",
                    "content/lib/jspanel-2.5.2/jquery.jspanel.min.css",
                    "content/lib/bootstrap-3.3.0/css/bootstrap.min.css",
                    "content/lib/bootstrap-3.3.0/css/bootstrap-theme.min.css",
                    //"content/lib/bootstrap-4.4.1/css/bootstrap.min.css",
                    //"content/lib/bootstrap-4.4.1/css/bootstrap-grid.min.css",
                    //"content/lib/bootstrap-4.4.1/css/bootstrap-reboot.min.css",
                    "content/lib/aside.min.css",
                    "content/lib/angular-xeditable-0.1.8/css/xeditable.css",
                    "content/lib/ui-layout/ui-layout.css",
                    "content/lib/ag-grid/dist/ag-grid.css",
                    "content/lib/ag-grid/dist/theme-fresh.css",
                    "content/lib/angular-formstamp/formstamp.css",
                    "content/lib/angular-bootstrap-datetimepicker/datetimepicker.css",
                    "content/lib/ui-select/select.min.css",
                    "content/lib/angular-ui-tree/dist/angular-ui-tree.min.css",
                    "content/lib/simple-grid/src/simple-grid.css",
                    "content/lib/amcharts/amcharts/plugins/export/export.css",
                    "content/lib/jstree/dist/themes/default/style.css",
                    "content/lib/angular-cron-jobs/angular-cron-jobs.css",
                    "content/site.css"
                ],
                dest: "content/dist/matrix.css"
            }
        },
        uglify: {
            options: {
                mangle: false
            },
            dist: {
                files: {
                    "content/dist/matrix.min.js": "content/dist/matrix.js",
                    "content/dist/lib.min.js": "content/dist/lib.js",
                    "content/dist/template.js": "content/dist/template.js"
                }
            }
        },
        cachebreaker: {
            logic: {
                options: {
                    match: ["lib.min.js"],
                    replacement: "md5",
                    src: {
                        path: "content/dist/lib.min.js"
                    }
                },
                files: {
                    src: ["content/dist/index.html"]
                }
            },
            logic: {
                options: {
                    match: ["matrix.min.js"],
                    replacement: "md5",
                    src: {
                        path: "content/dist/matrix.min.js"
                    }
                },
                files: {
                    src: ["content/dist/index.html"]
                }
            },
            template: {
                options: {
                    match: ["template.js"],
                    replacement: "md5",
                    src: {
                        path: "content/dist/template.js"
                    }
                },
                files: {
                    src: ["content/dist/index.html"]
                }
            },
            css: {
                options: {
                    match: ["matrix.css"],
                    replacement: "md5",
                    src: {
                        path: "content/dist/matrix.css"
                    }
                },
                files: {
                    src: ["content/dist/index.html"]
                }
            }
        },
        ngtemplates: {
            app: {
                cwd: 'content',
                src: 'tpls/**.html',
                dest: 'content/dist/template.js',
                options: {
                    htmlmin: { collapseWhitespace: true, collapseBooleanAttributes: true }
                }
            }
        },
        copy: {
            img: {
                expand: true,
                cwd: 'content/img/',
                src: '**',
                dest: 'content/dist/img/'
            },
            index: {
                files: [
                    { expand: false, src: 'content/index.html', dest: 'content/dist/index.html' },
                    { expand: false, src: 'content/favicon.ico', dest: 'content/dist/favicon.ico' },
                ]
            },
            wiki: {
                expand: true,
                cwd: 'content/wiki/',
                src: '**',
                dest: 'content/dist/wiki/'
            },
            fonts: {
                expand: true,
                cwd: 'content/lib/bootstrap-3.3.0/fonts/',
                src: '**',
                dest: 'content/dist/fonts/'
            },
            amcharts: { //????
                expand: true,
                cwd: 'content/lib/amcharts/amcharts',
                src: '**',
                dest: 'content/dist/lib/amcharts/amcharts/'
            },
            media: {
                expand: true,
                cwd: 'content/media/',
                src: ['**', "consuption.xlsx"],
                dest: 'content/dist/media/'
            }
        }
    });

    grunt.registerTask("all", ["clean", "copy", "ngtemplates", "concat", "uglify", "cachebreaker"]);
    grunt.registerTask("debug", ["clean", "copy", "ngtemplates", "concat"]);

    grunt.loadNpmTasks("grunt-contrib-clean");
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-jshint');
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-cache-breaker');
    grunt.loadNpmTasks('grunt-angular-templates');
    grunt.loadNpmTasks('grunt-contrib-copy');
};
