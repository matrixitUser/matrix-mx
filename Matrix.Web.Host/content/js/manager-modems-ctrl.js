angular.module("app")
	.controller("managerModemsCtrl", function ($rootScope, $scope, $uibModalInstance, $transport){
	$scope.model = {};
	$scope.model.close = function(){
		 $uibModalInstance.close();
	};


	$scope.myGridConfig = {
		getData: function () { return $scope.myData; }, 

		options: { 
			//"showEditButton": true,
			dynamicColumns: true,
			editable: true,
			perRowEditModeEnabled: false,
			showDelete: true,
			columns: [
						{ 
							title:	"Порт",	
							field: 'port', 
							width: 50,
							required: true
						},
						{
							field: "baudRate",
							title: "Скорость",
							selected: true,
							required: true
						},
						{
							field: "enabled",
							inputType: "checkbox",
							title: "Включен",
							disabled: false,
							required: true
						},
			]},
			
			// optional - callbacks for actions on rows
			editRequested: function (row) { },
			rowDeleted: function (row) { },
			cellFocused: function (row, column) { },
			rowSelected: function (row) { },
	}

	var setSelectionOptions = ['2400','4800','9600','14400','19200'];

	//Настройки грида
    //    $scope.opt = {
	//	columnDefs: columnDefs,
	//	angularCompileRows: true,
   // };

	$transport.send(new Message({ what: "modems-get-all" },{})).then(function (message) {
		var x = message;
		$scope.myData= message.body.modems;
		//$scope.opt.rowData = message.body.modems;
	//	if ($scope.opt.api) {
	//		$scope.opt.api.onNewRows();
    //    }
	});
});