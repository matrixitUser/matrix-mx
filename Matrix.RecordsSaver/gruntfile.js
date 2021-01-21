/// <binding />
/// <vs AfterBuild='ui' />
module.exports = function (grunt) {
    grunt.initConfig({
        clean: [
            "bin/Debug/ui",
        ],
        concat: {
            jsLibs: {
                src: [
                    "ui/bower_components/jquery/dist/jquery.js",
                    "ui/bower_components/bootstrap/dist/js/bootstrap.js",
                    "ui/bower_components/angular/angular.js",
                    "ui/bower_components/angular-bootstrap/ui-bootstrap.js",
                    "ui/bower_components/angular-bootstrap/ui-bootstrap-tpls.js",
                ],
                dest: 'bin/Debug/ui/lib.js'
            },
            js: {
                src: [
                    "ui/ui.js"
                ],
                dest: 'bin/Debug/ui/ui.js'
            },
            css: {
                src: [
                    "ui/bower_components/bootstrap/dist/css/bootstrap.css",
                    "ui/bower_components/bootstrap/dist/css/bootstrap-theme.css",
                    "ui/bower_components/angular-bootstrap/ui-bootstrap-csp.css",
                    "ui/app.css"
                ],
                dest: 'bin/Debug/ui/ui.css'
            }
        },

        uglify: {
            options: {
                mangle: false
            },
            dist: {
                files: {
                    "bin/Debug/ui/lib.min.js": "bin/Debug/ui/lib.js",
                    "bin/Debug/ui/ui.min.js": "bin/Debug/ui/ui.js"
                }
            }
        },
        cachebreaker: {
            lib: {
                options: {
                    match: ["lib.min.js"],
                    replacement: "md5",
                    src: {
                        path: "bin/Debug/ui/lib.min.js"
                    }
                },
                files: {
                    src: ["bin/Debug/ui/index.html"]
                }
            },
            app: {
                options: {
                    match: ["ui.min.js"],
                    replacement: "md5",
                    src: {
                        path: "bin/Debug/ui/ui.min.js"
                    }
                },
                files: {
                    src: ["bin/Debug/ui/index.html"]
                }
            },
            templates: {
                options: {
                    match: ["templates.js"],
                    replacement: "md5",
                    src: {
                        path: "bin/Debug/ui/templates.js"
                    }
                },
                files: {
                    src: ["bin/Debug/ui/index.html"]
                }
            },
            css: {
                options: {
                    match: ["ui.css"],
                    replacement: "md5",
                    src: {
                        path: "bin/Debug/ui/ui.css"
                    }
                },
                files: {
                    src: ["bin/Debug/ui/index.html"]
                }
            }
        },
        ngtemplates: {
            app: {
                cwd: "ui",
                src: "tpls/**.html",
                dest: "bin/Debug/ui/templates.js",
                options: {
                    htmlmin: { collapseWhitespace: true, collapseBooleanAttributes: true }
                }
            }
        },
        copy: {
            index: {
                files: [
                  { expand: false, src: 'ui/index.html', dest: 'bin/Debug/ui/index.html' },
                  { expand: false, src: 'ui/favicon.ico', dest: 'bin/Debug/ui/favicon.ico' },
                ]
            }
        }
    });

    grunt.registerTask("all", ["clean", "copy", "ngtemplates", "concat", "uglify", "cachebreaker"]);

    grunt.loadNpmTasks("grunt-contrib-clean");
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-jshint');
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-cache-breaker');
    grunt.loadNpmTasks('grunt-angular-templates');
    grunt.loadNpmTasks('grunt-contrib-copy');
};
