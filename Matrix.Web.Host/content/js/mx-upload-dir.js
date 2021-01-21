angular.module("matrix")
.directive("mxUpload", function () {
    return {
        restrict: "A",
        require: "ngModel",
        scope: {
            base64: "="
        },
        replace: true,
        controller: function ($scope, $element) {
            $element.on("change", function () {
                var file = $element[0].files[0];
                fileObject.filetype = file.type;
                fileObject.filename = file.name;
                fileObject.filesize = file.size;
                reader.readAsArrayBuffer(file);
            });
        },
        template: "<span class='btn btn-default btn-file form-control'>Обзор<input type='file' onchange='change()' /></span>"
    };
});