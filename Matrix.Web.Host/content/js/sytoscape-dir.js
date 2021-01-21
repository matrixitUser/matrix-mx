angular.module("matrix")

.directive("sytoscape", function ($log) {
    return {
        restrict: "E",
        transclude: true,
        scope: {
            elements: "=",
            height: "=",
            width: "=",
            selected: "=",
            onSelect: "&"
        },
        replace: true,
        template: "<div ng-style='{height:height,width:width}'></div>",
        link: function ($scope, $element, attrs) {
            $scope.getElementRect = function () {
                return { h: $element.height(), w: $element.width() };
            };

            $scope.$watch($scope.getElementRect, function (newValue, oldValue) {
                cy.resize();
            }, true);

            var container = $element[0];
            var cy = cytoscape({
                selectionType: "single",
                container: container,
                elements: $scope.elements,
                style: [{
                    selector: "node",
                    css: {
                        'height': 40,
                        'width': 40,
                        "background-color": "blue",
                        //'background-fit': 'cover',
                        //'border-color': '#000',
                        'border-width': 3,
                        'border-opacity': 0.5
                    }
                }, {
                    selector: "edge",
                    css: {
                        "width": 6,
                        "target-arrow-shape": "triangle",
                        "line-color": "#ffaaaa",
                        "target-arrow-color": "#ffaaaa",
                        "content": "data(port)",
                    }
                }, {
                    selector: ":selected",
                    css: {
                        "overlay-color": "gray",
                        "overlay-padding": "5",
                        "overlay-opacity": "0.4"
                    }
                }, {
                    selector: "node[type = 'Area']",
                    css: {
                        "content": "data(name)",
                        "background-image": "img/house.png",
                        "background-color": "gray"
                    }
                }, {
                    selector: "node[type = 'Tube']",
                    css: {
                        "content": "data(networkAddress)",
                        "background-image": "img/counter.png",
                    }
                }, {
                    selector: "node[type = 'CsdConnection']",
                    css: {
                        "content": "data(phone)",
                        "background-image": "img/phone.png"
                    }
                }, {
                    selector: "node[type = 'CsdPort']",
                    css: {
                        "content": "data(name)",
                        "background-image": "img/phone.png"
                    }
                }, {
                    selector: "node[type = 'SurveyServer']",
                    css: {
                        "content": "data(name)",
                        "background-image": "img/server.png"
                    }
                }, {
                    selector: "node[type = 'Folder']",
                    css: {
                        "content": "data(name)",
                        "background-image": "img/folder.png",
                        "background-color": "gray"
                    }
                }, {
                    selector: "node[type = 'MatrixConnection']",
                    css: {
                        "content": "data(imei)",
                        "background-image": "img/fastrack.png"
                    }
                }, {
                    selector: "node[type = 'MatrixPort']",
                    css: {
                        "content": "data(name)",
                        "background-image": "img/fastrack.png"
                    }
                }, {
                    selector: "node[type = 'ComConnection']",
                    css: {
                        "content": "data(port)",
                        "background-image": "img/port.png"
                    }
                }],

                layout: {
                    //name: "breadthfirst",
                    //name: "cose",
                    directed: true,
                    padding: 10,
                    name: "arbor",
                    animate: true
                }
            }); // cy init

            cy.on("select", function (evt) {
                $scope.onSelect({ selected: evt.cyTarget._private.data });             
            });

            var wrap = function (element) {
                
                
                if (element.class === "relation") {
                    element.source = element.start;
                    element.target = element.end;
                }

                return {
                    group: element.class === "relation" ? "edges" : "nodes",
                    data: element,
                    selectable: true,
                    mark: true
                };
            }           

            $scope.$watchCollection("elements", function (newValue, oldValue) {
                if (!newValue) return;
                var elements = [];
                for (var i = 0; i < newValue.length; i++) {
                    elements.push(wrap(newValue[i]));
                }
                cy.load(elements);
            });
        }
    };
})
