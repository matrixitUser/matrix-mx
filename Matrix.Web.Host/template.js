angular.module('app').run(['$templateCache', function($templateCache) {
  'use strict';

  $templateCache.put('tpls/about-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/information.png\" width=\"32\"> О программе</h3></div><div class=\"modal-body\"><h3 class=\"modal-title\">Программно-аппаратный комплекс &laquo;Матрикс&raquo;</h3><div class=\"row\" style=\"margin:5px\">Версия: 3.0.0<br>ООО Газпром межрегионгаз Уфа. Все права защищены.<br>Текущий пользователь: <span ng-bind=\"model.user\"></span></div></div><div class=\"modal-footer\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div>"
  );


  $templateCache.put('tpls/add-to-folder-modal.html',
    "<div class=\"modal-header\"><h3 class=\"modal-title modal-preview-head\"><span class=\"media\" tooltip=\"{{model.names.join('; ')}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"><img src=\"/img/folders.png\" width=\"32\"> <span class=\"badge\" ng-bind=\"model.names.length\"></span></span> Настройка групп <span ng-if=\"model.names.length>0\" class=\"smallergrey\">для {{model.names.join(', ')}}</span> <span ng-if=\"model.names.length==0\" class=\"red\">нет объектов</span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><div ng-if=\"model.overlayEnabled\"><div style=\"display: table; height: 70%; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"> <span ng-bind=\"model.overlayText\"></span></div></div></div><div ng-if=\"!model.overlayEnabled\"><div ag-grid=\"opt\" class=\"ag-fresh\" style=\"height:70%\"></div></div><!--<div ui-tree id=\"tree-root\" data-drag-enabled=\"false\" class=\"well well-sm pre-scrollable\">\r" +
    "\n" +
    "        <ol ui-tree-nodes ng-model=\"model.folders\">\r" +
    "\n" +
    "            <li ng-repeat=\"node in model.folders\" ui-tree-node data-nodrag data-collapsed ng-include=\"'folders-nodes-renderer.html'\"></li>\r" +
    "\n" +
    "        </ol>\r" +
    "\n" +
    "    </div>--></div><div class=\"modal-footer\"><button class=\"btn btn-primary\" ng-click=\"model.save()\" ng-disabled=\"model.overlayEnabled\">Сохранить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div><script type=\"text/ng-template\" id=\"folders-nodes-renderer.html\"><div ui-tree-handle class=\"tree-node tree-node-content\" ng-click=\"model.selectNode(node)\">\r" +
    "\n" +
    "        <a class=\"btn\" ng-if=\"node.children && node.children.length > 0\" data-nodrag ng-click=\"toggle(this)\">\r" +
    "\n" +
    "            <img ng-if=\"!collapsed\" src=\"/img/16/toggle.png\" />\r" +
    "\n" +
    "            <img ng-if=\"collapsed\" src=\"/img/16/toggle_expand.png\" />\r" +
    "\n" +
    "        </a>\r" +
    "\n" +
    "        <a class=\"btn\" ng-if=\"!(node.children && node.children.length > 0)\" data-nodrag>\r" +
    "\n" +
    "            <img src=\"/img/16/toggle.png\" />\r" +
    "\n" +
    "        </a>\r" +
    "\n" +
    "        <span ng-if=\"node.data.type==='Folder'\">\r" +
    "\n" +
    "            <img src=\"/img/folder.png\" /> {{node.data.name}}<small ng-if=\"node._dirty\">*</small>\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-if=\"node.data.type==='all'\">\r" +
    "\n" +
    "            <img src=\"/img/folder.png\" /> Всё\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <!--<img src=\"/img/folder.png\" /> {{node.data.name}}-->\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <ol ui-tree-nodes=\"\" ng-model=\"node.children\" ng-class=\"{hidden: collapsed}\">\r" +
    "\n" +
    "        <li ng-repeat=\"node in node.children\" ui-tree-node data-nodrag data-collapsed ng-include=\"'folders-nodes-renderer.html'\">\r" +
    "\n" +
    "        </li>\r" +
    "\n" +
    "    </ol></script>"
  );


  $templateCache.put('tpls/aside.html',
    "<div class=\"aside-dialog\"><div class=\"aside-content\"><div class=\"aside-header\"><button type=\"button\" class=\"close\">*</button><h4 class=\"aside-title\">Menu</h4></div><div class=\"aside-body bs-sidebar\" style=\"float:right\" ng-controller=\"foldersCtrl\"><!--Include--><div class=\"nav\"><a href=\"\" ng-click=\"load()\">Загрузить дерево</a></div><div class=\"bs-sidebar hidden-print\" role=\"complementary\" data-offset-top=\"-100\" bs-affix=\"\" bs-scrollspy-list=\"\"><div ui-tree=\"options\" data-drag-enabled=\"false\"><ul class=\"nav bs-sidenav\" ui-tree-nodes ng-model=\"roots\" id=\"tree-root\"><li ng-repeat=\"node in roots\" ui-tree-node ng-include=\"'nodes-renderer.html'\"></li><!--\r" +
    "\n" +
    "				<li>\r" +
    "\n" +
    "				  <a href=\"#getting-started\">Getting started</a>\r" +
    "\n" +
    "				</li>\r" +
    "\n" +
    "				<hr style=\"margin: 2px 0;\">\r" +
    "\n" +
    "				<li>\r" +
    "\n" +
    "				  <a href=\"#modals\">Modal</a>\r" +
    "\n" +
    "				  <ul class=\"nav\">\r" +
    "\n" +
    "					<li><a href=\"#modals-examples\">Examples</a></li>\r" +
    "\n" +
    "					<li><a href=\"#modals-usage\">Usage</a></li>\r" +
    "\n" +
    "				  </ul>\r" +
    "\n" +
    "				</li>\r" +
    "\n" +
    "				<li>\r" +
    "\n" +
    "				  <a href=\"#asides\">Aside</a>\r" +
    "\n" +
    "				  <ul class=\"nav\">\r" +
    "\n" +
    "					<li><a href=\"#asides-examples\">Examples</a></li>\r" +
    "\n" +
    "					<li><a href=\"#asides-usage\">Usage</a></li>\r" +
    "\n" +
    "				  </ul>\r" +
    "\n" +
    "				</li>--></ul><!--<div ui-tree=\"options\" data-drag-enabled=\"false\">\r" +
    "\n" +
    "					<ol ui-tree-nodes ng-model=\"roots\" id=\"tree-root\">\r" +
    "\n" +
    "						<li ng-repeat=\"node in roots\" ui-tree-node ng-include=\"'nodes-renderer.html'\"></li>\r" +
    "\n" +
    "					</ol>\r" +
    "\n" +
    "				</div>--></div></div><!--/Include--></div></div></div>"
  );


  $templateCache.put('tpls/card-cell.html',
    "<a href=\"#\" ng-click=\"$parent.$parent.rowClick(data)\" popover-template=\"'/tpls/object-card-popover.html'\" popover-append-to-body=\"true\" popover-trigger=\"mouseenter\" popover-placement=\"right\"><img src=\"/img/16/cards_bind_address.png\"></a>"
  );


  $templateCache.put('tpls/cell-actions.html',
    "<!--<span>\r" +
    "\n" +
    "    <a href=\"#\" ng-click=\"$parent.$parent.objectCardOpen(data)\">\r" +
    "\n" +
    "        <img ng-src=\"/img/{{data.class == 'HouseRoot'?'house_two.png':'infocard.png'}}\" width=\"20\" />\r" +
    "\n" +
    "    </a>\r" +
    "\n" +
    "</span>\r" +
    "\n" +
    "\r" +
    "\n" +
    "<a href=\"#\" ng-click=\"$parent.$parent.rowEdit(data)\">\r" +
    "\n" +
    "    <img ng-src=\"/img/edit_button.png\" width=\"20\" />\r" +
    "\n" +
    "</a>\r" +
    "\n" +
    "\r" +
    "\n" +
    "<span ng-if=\"data.Parameter && data.Parameter.length>0\">\r" +
    "\n" +
    "    <a href=\"#\" ng-click=\"$parent.$parent.rowParameterEdit(data)\">\r" +
    "\n" +
    "        <img ng-src=\"/img/tag_{{$parent.$parent.rowParameterIsTagged(data)? 'blue':'red'}}.png\" width=\"20\" />\r" +
    "\n" +
    "    </a>\r" +
    "\n" +
    "</span>--> <span ng-repeat=\"action in $parent.$parent.panelActionsView\" ng-if=\"action.visible(data)\"><a href=\"#\" ng-click=\"action.act(data)\"><img ng-src=\"{{action.icon(data)}}\" width=\"20\"></a></span><!-- Параметры  tooltip=\"Параметры\" tooltip-append-to-body=\"true\" -->"
  );


  $templateCache.put('tpls/cell-area.html',
    "<div ng-if=\"data.Area.length>0\"><img src=\"/img/house.png\" height=\"20\"> <span ng-bind=\"data.Area[0].name\"></span></div>"
  );


  $templateCache.put('tpls/cell-connection-detail.html',
    "<div ng-if=\"data.ComConnection.length>0\"><span ng-bind=\"data.ComConnection[0].baudRate + ' ' + data.ComConnection[0].dataBits + '-' +  data.ComConnection[0].parity + '-' + data.ComConnection[0].stopBits\"></span></div><div ng-if=\"data.LanConnection.length>0\"><span ng-bind=\"data.LanConnection[0].name\"></span></div><div ng-if=\"data.MatrixConnection.length>0\"><img src=\"/img/phone.png\" height=\"20\"> <span ng-bind=\"data.MatrixConnection[0].phone\"></span></div><div ng-if=\"data.CsdPort.length>0\"><span ng-bind=\"data.CsdPort[0].name\"></span></div>"
  );


  $templateCache.put('tpls/cell-connection.html',
    "<div ng-if=\"data.ComConnection.length>0\"><img src=\"/img/port.png\" height=\"20\"> <span ng-bind=\"data.ComConnection[0].port\"></span></div><div ng-if=\"data.LanConnection.length>0\"><img src=\"/img/network_adapter.png\" height=\"20\"> <span ng-bind=\"data.LanConnection[0].host + ':' + data.LanConnection[0].port\"></span></div><div ng-if=\"data.MatrixConnection.length>0\"><img src=\"/img/fastrack.png\" height=\"20\"> <span ng-bind=\"data.MatrixConnection[0].imei\"></span></div><div ng-if=\"data.CsdConnection.length>0\"><img src=\"/img/phone_vintage.png\" height=\"20\"> <span ng-bind=\"data.CsdConnection[0].phone\"></span></div><div ng-if=\"data.HttpConnection.length>0\"><img src=\"/img/globe_network.png\" height=\"20\"> <i>Интернет</i></div>"
  );


  $templateCache.put('tpls/cell-device.html',
    "<span ng-if=\"data.Device && data.Device.length>0\"><img src=\"/img/counter.png\" height=\"20\"> <span ng-bind=\"data.Device[0].name\"></span></span>"
  );


  $templateCache.put('tpls/cell-gsm-pool.html',
    "<span ng-bind=\"data.CsdPort[0].name\"></span>"
  );


  $templateCache.put('tpls/cell-imei.html',
    "<div ng-if=\"data.MatrixConnection.length>0\"><img src=\"/img/fastrack.png\" height=\"20\"> <span ng-bind=\"data.MatrixConnection[0].imei\"></span></div>"
  );


  $templateCache.put('tpls/cell-last-value.html',
    "<span ng-if=\"data.column_parameter\"><span ng-bind=\"data.column_alias || data.column_parameter\"></span>=<span ng-bind=\"data.cart.lastValue.d1 | number: 3\"></span> <span ng-bind=\"data.cart.lastValue.s2\"></span></span>"
  );


  $templateCache.put('tpls/cell-parameters.html',
    "<div ng-if=\"data.Parameter && data.Parameter.length>0\"><a href=\"#\" ng-click=\"$parent.$parent.rowParameterEdit(data)\"><img ng-src=\"/img/tag_{{data.cart.isTagged? 'blue':'red'}}.png\" width=\"20\"></a></div><!-- popover-template=\"'/tpls/status-card-popover.html'\" popover-append-to-body=\"true\" popover-trigger=\"mouseenter\" popover-placement=\"right\"-->"
  );


  $templateCache.put('tpls/cell-phone.html',
    "<div ng-if=\"data.cart.Phone.length>0\"><img src=\"/img/phone.png\" width=\"20\"> <span ng-bind=\"data.cart.Phone[0]\"></span></div>"
  );


  $templateCache.put('tpls/cell-pin.html',
    "<div style=\"align-content: center; text-align: center\"><img ng-src=\"/img/{{data.pinned? 'unpin_red.png':'pin.png'}}\" ng-click=\"data.pinned = !data.pinned; $parent.$parent.rowPinned(data.id, data.pinned)\" width=\"20\"></div>"
  );


  $templateCache.put('tpls/cell-server.html',
    "<span ng-if=\"data.SurveyServer && data.SurveyServer.length>0\"><img src=\"/img/server.png\" width=\"20\"> <span ng-bind=\"data.SurveyServer[0].name\"></span></span>"
  );


  $templateCache.put('tpls/cell-signal.html',
    "<div ng-if=\"data.MatrixConnection && data.MatrixConnection.length > 0 && data.MatrixConnection[0].signal\"><img ng-src=\"{{data.cart.signalImg}}\" width=\"20\" title=\"{{data.MatrixConnection[0].signal}}\"></div>"
  );


  $templateCache.put('tpls/cell-status-popover.html',
    "<div ng-if=\"data.cart && data.cart.status\"><div ng-if=\"data.cart.status.state == 'idle'\"><span ng-bind=\"data.cart.status.comment\"></span> <span ng-bind=\"data.cart.status.date | date: 'dd.MM.yy HH:mm:ss'\"></span></div><div ng-if=\"data.cart.status.state == 'wait'\"><span ng-bind=\"data.cart.status.comment\"></span> с <span ng-bind=\"data.cart.status.date | date: 'dd.MM.yy HH:mm:ss'\"></span></div><div ng-if=\"data.cart.status.state == 'process'\"><span ng-bind=\"data.cart.status.comment\"></span> начался <span ng-bind=\"data.cart.status.date | date: 'dd.MM.yy HH:mm:ss'\"></span></div><div ng-if=\"data.cart.status.state == 'error'\"><span ng-bind=\"data.cart.status.comment\"></span> от <span ng-bind=\"data.cart.status.date | date: 'dd.MM.yy HH:mm:ss'\"></span></div></div>"
  );


  $templateCache.put('tpls/cell-status.html',
    "<div ng-if=\"data.cart && data.cart.status && data.cart.status.state\"><div popover-template=\"'/tpls/status-card-popover.html'\" popover-append-to-body=\"true\" popover-trigger=\"mouseenter\" popover-placement=\"right\"><progressbar ng-type=\"{{data.cart.status.state == 'idle'? 'success' : (data.cart.status.state == 'wait'? 'default' : (data.cart.status.state == 'process'? 'warning' : 'danger'))}}\" ng-class=\"{'progress-striped': data.cart.status.state == 'wait' || data.cart.status.state == 'process', 'active': data.cart.status.state == 'process' }\" max=\"100\" value=\"100\" style=\"width: 20px; height: 20px\"><span ng-if=\"data.cart.status.count>0\">{{data.cart.status.count>9? '9+' : data.cart.status.count}}</span></progressbar></div></div>"
  );


  $templateCache.put('tpls/cell-tube.html',
    "<span ng-bind=\"data.name\"></span>"
  );


  $templateCache.put('tpls/column-cell.html',
    "<div class=\"ui-grid-cell-contents\" ng-click=\"getExternalScopes().updateCurrent(row.entity)\"><div ng-if=\"row.entity['columnDate'] != null\"><img src=\"./img/arrow_refresh_small.png\" width=\"20\" alt=\"Обновить\"> <span title=\"{{row.entity['columnDate'] | amDateFormat: 'DD.MM.YYYY HH:mm:ss'}}\">{{row.entity[col.field] | number:2}} {{row.entity['columnUnit']}}</span></div><div ng-if=\"row.entity['columnDate'] == null\"><img src=\"./img/arrow_refresh_small.png\" width=\"20\" alt=\"Обновить\"> <span>нет данных</span></div></div>"
  );


  $templateCache.put('tpls/data-table.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/table.png\" width=\"32\"> Архивы</h3></div><div class=\"modal-body\" style=\"padding:5px\"><form fs-form-for=\"\" class=\"form-horizontal\"><div class=\"form-group row\"><div class=\"col-lg-4 col-md-4\" style=\"padding-right:0px\"><div ng-if=\"model.type==='Hour'\" fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div><div ng-if=\"model.type==='Day'\" fs-date=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div></div><div class=\"col-lg-4 col-md-4\" style=\"padding-right:0px\"><div ng-if=\"model.type==='Hour'\" fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div><div ng-if=\"model.type==='Day'\" fs-date=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div></div><div class=\"col-lg-3 col-md-3\" style=\"padding-right:0px\"><select class=\"form-control\" name=\"singleSelect\" ng-model=\"model.type\"><option value=\"Hour\">Часы</option><option value=\"Day\">Сутки</option></select></div><div class=\"col-lg-1 col-md-1\"><div class=\"hidden-md btn-group\"><button class=\"btn btn-default\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button></div></div></div></form><div ag-grid=\"model.grid\" class=\"ag-fresh\" style=\"height:70%\"></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-9 col-md-6\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div>"
  );


  $templateCache.put('tpls/device-list-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/counter.png\" width=\"32\"> <span ng-if=\"!model.selected\">Типы вычислителей <span class=\"smallergrey\">вычислитель не выбран</span></span> <span ng-if=\"model.selected\">Вычислитель: <a href=\"#\" editable-text=\"model.selected.edit.name\" buttons=\"no\">{{ model.selected.edit.name || 'без названия' }}</a><span class=\"red\" ng-bind=\"model.selected.edited? &quot;*&quot;:&quot;&quot;\"></span> <span class=\"smallergrey\">выбор драйвера</span></span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><!-- 1/1: загрузка [.v.] --><div ng-if=\"!model.drivers\"><div style=\"display: table; height: 55%; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><!-- загружен --><div ng-if=\"model.drivers\"><!-- 1/1: нет списка [.v.] --><div ng-if=\"!model.drivers.length\"><div style=\"display: table; height: 55%; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><h3 ng-if=\"!model.lastErr\">Нет вычислителей</h3><h3 ng-if=\"model.lastErr\" class=\"red\">Произошла ошибка: {{model.lastErr}}</h3><h4 ng-if=\"model.lastErr\">Нажмите на кнопку &laquo;Сброс&raquo;</h4></div></div></div><!-- есть список [.|.] --><div ng-if=\"model.drivers.length\" class=\"row\" style=\"margin:5px\"><!-- 1/2: выбор объекта [v|.] --><div ng-if=\"!model.only1\" class=\"col-md-5\"><div style=\"overflow: auto; height: 55%\"><ul ng-repeat=\"driver in model.sorted\" class=\"nav nav-pills nav-stacked\"><li ng-class=\"{'active': model.selected == driver}\"><a style=\"overflow: hidden\" ng-class=\"{'red': driver.edited}\" ng-click=\"model.select(driver)\"><span ng-bind=\"driver.name || '&lt;без названия&gt;'\"></span><span ng-bind=\"driver.edited? &quot;*&quot;:&quot;&quot;\"></span></a></li></ul></div></div><!-- 2/2: объект [.|v] --><div ng-class=\"{&quot;col-md-7&quot;: !model.only1, &quot;col-md-12&quot;: model.only1 }\"><div ng-if=\"model.selected\"><div style=\"overflow: auto; height: 55%\"><h4>Вычислитель</h4><table class=\"table table-hover\"><tr><td>Имя</td><td><a href=\"#\" editable-text=\"model.selected.edit.name\" buttons=\"no\">{{ model.selected.edit.name || 'без названия' }}</a></td></tr><tr><td style=\"width: 100px\">Вид</td><td><a href=\"#\" editable-select=\"model.selected.edit.reference\" e-ng-options=\"s.value as s.text for s in model.references\" buttons=\"no\">{{ model.selected.showReference() }}</a></td></tr></table><h4>Параметры <span ng-if=\"!tableform.$visible\"><button type=\"button\" class=\"btn btn-default\" ng-show=\"!tableform.$visible\" ng-click=\"tableform.$show()\"><img src=\"/img/16/page_edit.png\"></button> <button type=\"button\" ng-disabled=\"!model.selected.editedFields || tableform.$waiting\" ng-click=\"model.selected.reLoadFields()\" class=\"btn btn-default\"><img src=\"/img/16/cancel.png\"></button></span></h4><form editable-form name=\"tableform\" onaftersave=\"model.selected.checkParameters()\" oncancel=\"model.selected.checkParameters()\"><table class=\"table table-hover\"><tr><th>Имя параметра</th><th>Наименование</th><th><span ng-show=\"tableform.$visible\" style=\"width: 30%\">Действие</span></th></tr><tr ng-if=\"model.selected.edit.fields.length == 0\"><td colspan=\"3\"><i>Нет параметров</i></td></tr><tr ng-repeat=\"field in model.selected.edit.fields\"><td><span editable-text=\"field.name\" e-form=\"tableform\" onbeforesave=\"model.checkParameter($data)\">{{ field.name || 'Введите имя' }}</span></td><td><span editable-text=\"field.caption\" e-form=\"tableform\">{{ field.caption || field.name }}</span></td><td><button type=\"button\" ng-show=\"tableform.$visible\" ng-click=\"model.selected.deleteParameter(field.name)\" class=\"btn btn-default pull-right\"><img src=\"/img/16/cross.png\"></button></td></tr><tr ng-if=\"tableform.$visible\"><td><button type=\"button\" ng-disabled=\"tableform.$waiting\" ng-click=\"model.selected.addParameter()\" class=\"btn btn-default\"><img src=\"/img/16/add.png\"> Добавить...</button></td><td colspan=\"2\" style=\"text-align: right\"><button type=\"submit\" ng-disabled=\"tableform.$waiting\" class=\"btn btn-primary\">ОК</button> <button type=\"button\" ng-disabled=\"tableform.$waiting\" ng-click=\"tableform.$cancel()\" class=\"btn btn-default\">Отмена</button></td></tr></table></form><h4>Текущий драйвер</h4><table class=\"table table-hover\"><tr><td style=\"width: 100px\">Имя файла</td><td>{{model.selected.filename || model.selected.name}}</td></tr><tr><td>Размер</td><td>{{model.selected.filesize}} байт</td></tr><tr><td>Дата загрузки</td><td>{{model.selected.uploadDate}}</td></tr></table><h4>Загрузка драйвера</h4><div ng-if=\"!model.selected.file\"><input type=\"file\" ng-model=\"model.selected.file\" base-sixty-four-input></div><div ng-if=\"model.selected.file\"><table class=\"table table-hover\"><tr><td style=\"width: 100px\">Имя файла</td><td>{{model.selected.file.filename}}</td></tr><tr><td>Размер</td><td>{{model.selected.file.filesize}} байт</td></tr><tr ng-if=\"model.selected.isEqual\"><td colspan=\"2\"><span class=\"red\">Внимание! Загружаемый драйвер совпадает с текущим!</span></td></tr></table><button type=\"button\" class=\"btn btn-danger\" ng-click=\"model.cancelUpload(model.selected)\">× Отмена</button><!--<span ng-if=\"model.selected.file && (model.selected.driver === model.selected.file.base64)\">\r" +
    "\n" +
    "                                Без изменений\r" +
    "\n" +
    "                            </span>--></div></div></div><div ng-if=\"!model.selected\"><div style=\"display: table; height: 55%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle\"><h3>Выберите вычислитель из списка</h3></div></div></div></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-click=\"model.reset()\">Сброс</button><!--<button class=\"btn btn-default\" ng-class=\"{'active': !model.only1}\" ng-click=\"model.toggleSideList()\">\r" +
    "\n" +
    "                <img src=\"../img/application_side_list.png\" height=\"20\" />\r" +
    "\n" +
    "            </button>--></div><div class=\"col-xs-9 col-md-6\"><span ng-if=\"model.editedCounter>0\"><span class=\"red\" ng-bind=\"&quot;Непринятые изменения: &quot; + model.editedCounter\"></span> <button class=\"btn btn-primary\" ng-click=\"model.save()\">Сохранить</button></span> <span ng-if=\"model.editedCounter==0\"><button class=\"btn btn-primary\" ng-click=\"model.save()\" disabled>Сохранить</button></span> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div>"
  );


  $templateCache.put('tpls/drivers-list.html',
    "<div ng-controller=\"driversCtrl\"><div ui-layout=\"{flow:'column',dividerSize:'10'}\"><div ui-layout-container size=\"30%\"><input type=\"text\" class=\"form-control\" ng-model=\"driverFilter\" placeholder=\"фильтр\"><ul class=\"nav nav-pills nav-stacked\"><li ng-repeat=\"driver in drivers | filter:driverFilter\" ng-class=\"{'active': $parent.selected==driver}\"><a style=\"overflow:hidden\" ng-click=\"$parent.select(driver);\"><span ng-if=\"driver.dirty\" style=\"color:red\">*</span>{{ driver.name}}</a></li></ul></div><div ui-layout-container><form class=\"form-horizontal\" name=\"frm\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">имя</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\" ng-required=\"true\"> <span class=\"input-group-addon\"></span></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">файл</span> <span class=\"form-control\">{{selected.file.filesize}},байт</span> <span class=\"btn btn-default input-group-addon btn-file\">Обзор <input type=\"file\" ng-model=\"selected.file\" base-sixty-four-input></span></div><input type=\"hidden\" value=\"{{selected.dirty=frm.$dirty}}\"></form></div></div></div>"
  );


  $templateCache.put('tpls/fill-cell.html',
    "<div class=\"ui-grid-cell-contents\"><progressbar animate=\"false\" value=\"row.entity[col.field]\" type=\"success\"><b>{{row.entity[col.field]}}%</b></progressbar></div>"
  );


  $templateCache.put('tpls/folder-edit-modal.html',
    "<div class=\"modal-header\"><h3 class=\"modal-title\"><img src=\"/img/folder_edit.png\" width=\"32\"> <span ng-if=\"!model.folder\">Редактор группы <span class=\"smallergrey\">группа не выбрана</span></span> <span ng-if=\"model.folder\">Группа: <span ng-if=\"model.folderName\" ng-bind=\"model.folderName\"></span> <span ng-if=\"!model.folderName\" class=\"red\">Новая группа</span> <span ng-if=\"model.folderRootName\">в {{model.folderRootName}}</span> <span class=\"smallergrey\">редактор</span></span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><div ng-if=\"model.overlayEnabled\"><div style=\"display: table; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"> <span ng-bind=\"model.overlayText\"></span></div></div></div><div ng-if=\"!model.overlayEnabled\"><form editable-form name=\"folderFrm\"><!--<h4>Группа<span ng-if=\"folderFrm.$dirty\">*</span></h4>--><table class=\"table table-hover\"><tr><td>Имя</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model.folder.name\"></td></tr></table></form></div></div><div class=\"modal-footer\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn\" ng-class=\"{'btn-default' : !model.deleteEnable, 'btn-danger': model.deleteEnable}\" ng-click=\"model.delete()\" ng-disabled=\"!model.deleteEnable\">Удалить</button></div><div class=\"col-xs-9 col-md-6\"><button class=\"btn btn-primary\" ng-click=\"model.save()\" ng-disabled=\"model.overlayEnabled\">Сохранить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div>"
  );


  $templateCache.put('tpls/folders-aside.html',
    "<div class=\"aside-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h4 class=\"aside-title\">Группы</h4></div><div class=\"aside-body\"><div ag-grid=\"opt\" class=\"ag-fresh\" resizable ng-style=\"{height:windowHeight-160,width:'100%'}\"></div></div><div class=\"aside-footer\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div>"
  );


  $templateCache.put('tpls/footer-close.html',
    "<button ng-click=\"close()\" class=\"btn btn-primary\">Закрыть</button>"
  );


  $templateCache.put('tpls/footer-okcancel.html',
    "<button ng-click=\"cancel()\" class=\"btn\">Отмена</button> <button ng-click=\"close()\" class=\"btn btn-primary\">Ок</button>"
  );


  $templateCache.put('tpls/group-edit.html',
    "<div class=\"modal\" tabindex=\"-1\" role=\"dialog\"><div class=\"modal-dialog\"><div class=\"modal-content\"><div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\">Группа</h3></div><form class=\"modal-body form-horizontal\" role=\"form\"><label for=\"name\">Название</label><input id=\"name\" class=\"form-control\" placeholder=\"название\" type=\"text\" ng-model=\"name\" required auto-fill></form><div class=\"modal-footer\"><input type=\"button\" class=\"btn\" value=\"Отмена\" ng-click=\"close()\"> <input type=\"button\" class=\"btn btn-success btn-default\" ng-click=\"save(name)\" value=\"Сохранить\"></div></div></div></div>"
  );


  $templateCache.put('tpls/house-mini.html',
    "<div ng-controller=\"HouseCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div class=\"modal-preview-head\"><img src=\"/img/house_two.png\" width=\"16\"> Поквартирный учёт</div><div class=\"modal-preview-head\"><span ng-if=\"model.rowParent\"><!--<span class=\"badge\" ng-bind=\"model.names.length\" tooltip=\"{{model.names.join('; ')}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"></span>--><small>{{model.rowParent.cart.name}} {{model.rowParent.name}}</small></span> <span ng-if=\"!model.rowParent\"><small>нет объектов</small></span></div><!-- body --><div class=\"modal-preview-body alert alert-info\"></div></a></div>"
  );


  $templateCache.put('tpls/house-modal%20-%20Copy.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" aria-hidden=\"true\">×</button><!--<button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.modal.dismiss()\" aria-hidden=\"true\">-</button>--><h3 class=\"modal-title modal-preview-head\"><img src=\"/img/house_two.png\" width=\"32\"> <span ng-if=\"!model.rowParent\">Поквартирный учёт</span> <span ng-if=\"model.rowParent\"><span ng-bind=\"model.rowParent.cart.name\"></span> <span ng-bind=\"model.rowParent.name\"></span> <span class=\"smallergrey\">поквартирный учёт</span></span></h3></div><div class=\"modal-body\"><!-- 1/1: загрузка [.v.] --><div ng-if=\"!model.house\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><!-- загружен --><div ng-if=\"model.house\"><form fs-form-for=\"\" class=\"form-horizontal\"><div class=\"form-group row\"><div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\"><div fs-date=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div></div><div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\"><div fs-date=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div></div><div class=\"col-lg-4 col-md-2\"><div class=\"visible-md btn-group\" dropdown><button class=\"btn btn-default\" ng-click=\"model.dailyUpdate()\" ng-disabled=\"model.dailyIsUpdating\"><img ng-src=\"/img/{{!model.dailyIsUpdating? &quot;table_refresh.png&quot; : &quot;loader.gif&quot;}}\" width=\"20\" title=\"Обновить\"></button> <button type=\"button\" class=\"btn btn-default\" dropdown-toggle><span class=\"caret\"></span> <span class=\"sr-only\">Дополнительно</span></button><ul class=\"dropdown-menu\" role=\"menu\" aria-labelledby=\"split-button\"><li role=\"menuitem\"><a href=\"#\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\">Экспорт в PDF</a></li><li role=\"menuitem\"><a href=\"#\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\">Экспорт в XLS</a></li><li role=\"menuitem\"><a href=\"#\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\">Печать</a></li></ul></div><div class=\"hidden-md btn-group\"><button class=\"btn btn-default\" ng-click=\"model.dailyUpdate()\" ng-disabled=\"model.dailyIsUpdating\"><img ng-src=\"/img/{{!model.dailyIsUpdating? &quot;table_refresh.png&quot; : &quot;loader.gif&quot;}}\" width=\"20\" title=\"Обновить\"></button> <button class=\"btn btn-default\" ng-click=\"model.savePdf(model.selected.reportAsHtml)\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\"></button> <button class=\"btn btn-default\" ng-click=\"model.toExcel(model.selected.reportAsHtml)\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\"></button> <button class=\"btn btn-default\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\"></button></div></div></div></form><div style=\"overflow: auto; width: 100%; height: 75%\" id=\"report-content\"><style>@media print {\r" +
    "\n" +
    "                    hr {\r" +
    "\n" +
    "                        page-break-after: always;\r" +
    "\n" +
    "                    }\r" +
    "\n" +
    "                }\r" +
    "\n" +
    "\r" +
    "\n" +
    "                .report table {\r" +
    "\n" +
    "                    width: 100%; /* Ширина таблицы */ /*border: 1px solid black;*/ /* Рамка вокруг таблицы */\r" +
    "\n" +
    "                    border-collapse: collapse; /* Отображать только одинарные линии */\r" +
    "\n" +
    "                }\r" +
    "\n" +
    "\r" +
    "\n" +
    "                .report th {\r" +
    "\n" +
    "                    text-align: center; /* Выравнивание по левому краю */\r" +
    "\n" +
    "                    background: #ccc; /* Цвет фона ячеек */\r" +
    "\n" +
    "                    padding: 5px; /* Поля вокруг содержимого ячеек */\r" +
    "\n" +
    "                    border: 1px solid black; /* Граница вокруг ячеек */\r" +
    "\n" +
    "                }\r" +
    "\n" +
    "\r" +
    "\n" +
    "                .report td {\r" +
    "\n" +
    "                    padding: 5px; /* Поля вокруг содержимого ячеек */\r" +
    "\n" +
    "                    border: 1px solid black; /* Граница вокруг ячеек */\r" +
    "\n" +
    "                }</style><div class=\"report\"><!-- SECTIONS: 1 --><table class=\"table table-hover\"><!-- FLOORS --><tr ng-repeat-start=\"floor in model.house.Floor | orderBy: 'index' : true\"><th rowspan=\"3\">{{floor.index}} этаж</th><!-- APTS --><td rowspan=\"3\" ng-repeat-start=\"apt in floor.Apt\"><i>Кв. {{apt.row[\"apt\"]}}</i></td><td style=\"background-color: #b3ffbc\"><small>Показание</small></td><td style=\"background-color: #b3ffbc\" ng-repeat-end><small>{{apt.current[\"Показание\"] | number: 0}}</small></td></tr><tr><td style=\"background-color: #ffb3b3\" ng-repeat-start=\"apt in floor.Apt\"><small>Потребление за период</small></td><td style=\"background-color: #ffb3b3\" ng-repeat-end><small>{{(apt.value.end - apt.value.start) | number: 0}}</small></td></tr><tr ng-repeat-end><td style=\"background-color: #b3e5ff\" ng-repeat-start=\"apt in floor.Apt\"><small></small></td><td style=\"background-color: #b3e5ff\" ng-repeat-end><small ng-class=\"{'red' : (!apt.value.start || !apt.value.end)}\">{{(!apt.value.start || !apt.value.end)? \"нет данных\" : \"\"}}</small></td></tr><!-- COMMONS --><!--<tr>\r" +
    "\n" +
    "                        <th rowspan=\"3\" colspan=\"2\">Общедомовой</th>\r" +
    "\n" +
    "                        <td style=\"background-color: #b3ffbc\">\r" +
    "\n" +
    "                            <small>ЭЭ</small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                        <td>\r" +
    "\n" +
    "                            <small></small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                    </tr>\r" +
    "\n" +
    "                    <tr>\r" +
    "\n" +
    "                        <td style=\"background-color: #ffb3b3\">\r" +
    "\n" +
    "                            <small>ГВС</small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                        <td></td>\r" +
    "\n" +
    "                    </tr>\r" +
    "\n" +
    "                    <tr ng-repeat-end>\r" +
    "\n" +
    "                        <td style=\"background-color: #b3e5ff\">\r" +
    "\n" +
    "                            <small>ХВС</small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                        <td></td>\r" +
    "\n" +
    "                    </tr>--></table><div>Период: с {{model.start | date: 'dd.MM.yyyy 23:59:59'}} по {{model.end | date: 'dd.MM.yyyy 23:59:59'}}</div><table class=\"table table-hover\"><tr><th>Название</th><th>Показание на начало периода</th><th>Показание на конец периода</th><th>Разность показаний</th><th>Коэффициент трансформации</th><th>Потребление за период</th></tr><tr ng-repeat=\"energy in model.house.Common.Energy\"><td>{{energy.row.Area[0].name}} {{energy.row.name}}</td><td>{{energy.value.start | number: 0}}</td><td>{{energy.value.end | number: 0}}</td><td>{{(energy.value.end - energy.value.start) | number: 0}}</td><td>{{energy.row.KTr | number: 0}}</td><td>{{(energy.value.end - energy.value.start) * energy.row.KTr | number: 0}}</td></tr><tr><td>Сумма по дому</td><td>---</td><td>---</td><td>---</td><td>---</td><td>{{model.house.Common.energy | number: 0}}</td></tr><tr><td>Сумма по квартирам</td><td>---</td><td>---</td><td>---</td><td>---</td><td>{{model.house.Apt.energy | number: 0}}</td></tr></table><!--<form fs-form-for=\"\" class=\"form-horizontal\">--><!--<div class=\"form-group row\">\r" +
    "\n" +
    "                    <div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\">\r" +
    "\n" +
    "                        <div fs-date=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "                    <div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\">\r" +
    "\n" +
    "                        <div fs-date=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "                    <div class=\"col-lg-4 col-md-2\">\r" +
    "\n" +
    "                        <div class=\"btn-group\">\r" +
    "\n" +
    "                            <button class=\"btn btn-default\" ng-click=\"model.selected.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\" /></button>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "                </div>--><!--</form>--></div></div></div></div><div class=\"modal-footer\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-click=\"model.refresh()\"><img ng-src=\"/img/{{!model.isRefresh? &quot;arrow_refresh_small.png&quot; : &quot;loader.gif&quot;}}\" width=\"20\"> Обновить</button></div><div class=\"col-xs-9 col-md-6\"><button class=\"btn btn-primary\" ng-click=\"model.modal.dismiss()\">Скрыть</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div>"
  );


  $templateCache.put('tpls/house-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" aria-hidden=\"true\">×</button><!--<button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.modal.dismiss()\" aria-hidden=\"true\">-</button>--><h3 class=\"modal-title modal-preview-head\"><img src=\"/img/house_two.png\" width=\"32\"> <span ng-if=\"!model.rowParent\">Поквартирный учёт</span> <span ng-if=\"model.rowParent\"><span ng-bind=\"model.rowParent.cart.name\"></span> <span ng-bind=\"model.rowParent.name\"></span> <span class=\"smallergrey\">поквартирный учёт</span></span></h3></div><div class=\"modal-body\"><!-- 1/1: загрузка [.v.] --><div ng-if=\"model.overlayEnabled\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><!-- загружен, нет данных о строении дома --><div ng-if=\"!model.overlayEnabled && model.Section.length == 0\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><h3>Нет информации о структуре дома!</h3></div></div></div><!-- загружен --><div ng-if=\"!model.overlayEnabled && model.Section.length > 0\"><!-- period picker --><!--<form fs-form-for=\"\" class=\"form-horizontal\">\r" +
    "\n" +
    "            <div class=\"form-group row\">\r" +
    "\n" +
    "                <div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\">\r" +
    "\n" +
    "                    <div fs-date=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "                <div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\">\r" +
    "\n" +
    "                    <div fs-date=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "\r" +
    "\n" +
    "                <div class=\"col-lg-4 col-md-2\">\r" +
    "\n" +
    "\r" +
    "\n" +
    "                    <div class=\"visible-md btn-group\" dropdown>\r" +
    "\n" +
    "                        <button class=\"btn btn-default\" ng-click=\"model.dailyUpdate()\" ng-disabled=\"model.dailyIsUpdating\">\r" +
    "\n" +
    "                            <img ng-src='/img/{{!model.dailyIsUpdating? \"table_refresh.png\" : \"loader.gif\"}}' width=\"20\" title=\"Обновить\" />\r" +
    "\n" +
    "                        </button>\r" +
    "\n" +
    "                        <button type=\"button\" class=\"btn btn-default\" dropdown-toggle>\r" +
    "\n" +
    "                            <span class=\"caret\"></span>\r" +
    "\n" +
    "                            <span class=\"sr-only\">Дополнительно</span>\r" +
    "\n" +
    "                        </button>\r" +
    "\n" +
    "                        <ul class=\"dropdown-menu\" role=\"menu\" aria-labelledby=\"split-button\">\r" +
    "\n" +
    "                            <li role=\"menuitem\">\r" +
    "\n" +
    "                                <a href=\"#\" ng-click=\"model.savePdf()\">\r" +
    "\n" +
    "                                    <img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\" />Экспорт в PDF\r" +
    "\n" +
    "                                </a>\r" +
    "\n" +
    "                            </li>\r" +
    "\n" +
    "                            <li role=\"menuitem\">\r" +
    "\n" +
    "                                <a href=\"#\" ng-click=\"model.toExcel()\">\r" +
    "\n" +
    "                                    <img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\" />Экспорт в XLS\r" +
    "\n" +
    "                                </a>\r" +
    "\n" +
    "                            </li>\r" +
    "\n" +
    "                            <li role=\"menuitem\">\r" +
    "\n" +
    "                                <a href=\"#\" print-div=\"#report-content\">\r" +
    "\n" +
    "                                    <img src=\"./img/print.png\" height=\"20\" title=\"Печать\" />Печать\r" +
    "\n" +
    "                                </a>\r" +
    "\n" +
    "                            </li>\r" +
    "\n" +
    "                        </ul>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "                    <div class=\"hidden-md btn-group\">\r" +
    "\n" +
    "                        <button class=\"btn btn-default\" ng-click=\"model.dailyUpdate()\" ng-disabled=\"model.dailyIsUpdating\">\r" +
    "\n" +
    "                            <img ng-src='/img/{{!model.dailyIsUpdating? \"table_refresh.png\" : \"loader.gif\"}}' width=\"20\" title=\"Обновить\" />\r" +
    "\n" +
    "                        </button>\r" +
    "\n" +
    "                        <button class=\"btn btn-default\" ng-click=\"model.savePdf(model.selected.reportAsHtml)\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\" /></button>\r" +
    "\n" +
    "                        <button class=\"btn btn-default\" ng-click=\"model.toExcel(model.selected.reportAsHtml)\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\" /></button>\r" +
    "\n" +
    "                        <button class=\"btn btn-default\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\" /></button>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "            </div>\r" +
    "\n" +
    "        </form>--><div style=\"overflow: auto; width: 100%; height: 75%\" id=\"report-content\"><style>@media print {\r" +
    "\n" +
    "                    hr {\r" +
    "\n" +
    "                        page-break-after: always;\r" +
    "\n" +
    "                    }\r" +
    "\n" +
    "                }\r" +
    "\n" +
    "\r" +
    "\n" +
    "                .report table {\r" +
    "\n" +
    "                    width: 100%; /* Ширина таблицы */ /*border: 1px solid black;*/ /* Рамка вокруг таблицы */\r" +
    "\n" +
    "                    border-collapse: collapse; /* Отображать только одинарные линии */\r" +
    "\n" +
    "                }\r" +
    "\n" +
    "\r" +
    "\n" +
    "                .report th {\r" +
    "\n" +
    "                    text-align: center; /* Выравнивание по левому краю */\r" +
    "\n" +
    "                    background: #ccc; /* Цвет фона ячеек */\r" +
    "\n" +
    "                    padding: 5px; /* Поля вокруг содержимого ячеек */\r" +
    "\n" +
    "                    border: 1px solid black; /* Граница вокруг ячеек */\r" +
    "\n" +
    "                }\r" +
    "\n" +
    "\r" +
    "\n" +
    "                .report td {\r" +
    "\n" +
    "                    padding: 5px; /* Поля вокруг содержимого ячеек */\r" +
    "\n" +
    "                    border: 1px solid black; /* Граница вокруг ячеек */\r" +
    "\n" +
    "                }</style><div class=\"report\"><ul class=\"nav nav-pills\"><li ng-repeat=\"section in model.Section\" ng-class=\"{'active': model.csection == section}\"><a style=\"overflow: hidden\" ng-click=\"model.select(section)\">Подъезд <span ng-bind=\"section.index\"></span></a></li></ul><!-- SECTIONS --><table class=\"table table-hover\"><!-- FLOORS --><tr ng-repeat-start=\"floor in model.csection.Floor | orderBy: 'index' : true\"><th rowspan=\"3\">{{floor.index}} этаж</th><!-- APTS --><td rowspan=\"3\" ng-repeat-start=\"apt in floor.Apt\" style=\"width: 100px\"><i>Кв. <span style=\"font-size: 30px\" ng-bind=\"apt.index\"></span></i></td><td style=\"background-color: lightyellow; width: 30px\"><img src=\"/img/16/lightning.png\" width=\"16\"></td><td colspan=\"2\" style=\"background-color: lightyellow\"><small ng-bind-html=\"apt.energy\"></small></td><td ng-repeat-end style=\"display:none\"></td></tr><tr><td style=\"background-color: #b3e5ff\" ng-repeat-start=\"apt in floor.Apt\"><img src=\"/img/16/cold.png\" width=\"16\"></td><td ng-if=\"apt.cw2 === undefined\" colspan=\"2\" style=\"background-color: #b3e5ff\"><small ng-bind-html=\"apt.cw\"></small></td><td ng-if=\"apt.cw2 !== undefined\" style=\"background-color: #b3e5ff\"><small ng-bind-html=\"apt.cw\"></small></td><td ng-if=\"apt.cw2 !== undefined\" style=\"background-color: #b3e5ff\"><small ng-bind-html=\"apt.cw2\"></small></td><td ng-repeat-end style=\"display:none\"></td></tr><tr ng-repeat-end><td style=\"background-color: #ffb3b3\" ng-repeat-start=\"apt in floor.Apt\"><img src=\"/img/16/hot.png\" width=\"16\"></td><td ng-if=\"apt.hw2 === undefined\" colspan=\"2\" style=\"background-color: #ffb3b3\"><small ng-bind-html=\"apt.hw\"></small></td><td ng-if=\"apt.hw2 !== undefined\" style=\"background-color: #ffb3b3\"><small ng-bind-html=\"apt.hw\"></small></td><td ng-if=\"apt.hw2 !== undefined\" style=\"background-color: #ffb3b3\"><small ng-bind-html=\"apt.hw2\"></small></td><td ng-repeat-end style=\"display:none\"></td></tr><!-- COMMONS --><!--<tr>\r" +
    "\n" +
    "                        <th rowspan=\"3\" colspan=\"2\">Общедомовой</th>\r" +
    "\n" +
    "                        <td style=\"background-color: #b3ffbc\">\r" +
    "\n" +
    "                            <small>ЭЭ</small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                        <td>\r" +
    "\n" +
    "                            <small></small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                    </tr>\r" +
    "\n" +
    "                    <tr>\r" +
    "\n" +
    "                        <td style=\"background-color: #ffb3b3\">\r" +
    "\n" +
    "                            <small>ГВС</small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                        <td></td>\r" +
    "\n" +
    "                    </tr>\r" +
    "\n" +
    "                    <tr ng-repeat-end>\r" +
    "\n" +
    "                        <td style=\"background-color: #b3e5ff\">\r" +
    "\n" +
    "                            <small>ХВС</small>\r" +
    "\n" +
    "                        </td>\r" +
    "\n" +
    "                        <td></td>\r" +
    "\n" +
    "                    </tr>--></table></div></div></div></div><div class=\"modal-footer\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-click=\"model.refresh()\" ng-disabled=\"model.overlayEnabled || model.Section.length == 0\"><img ng-src=\"/img/{{!model.isRefresh? &quot;arrow_refresh_small.png&quot; : &quot;loader.gif&quot;}}\" width=\"20\"> Обновить</button></div><div class=\"col-xs-9 col-md-6\"><button class=\"btn btn-primary\" ng-click=\"model.modal.dismiss()\">Скрыть</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div>"
  );


  $templateCache.put('tpls/list-item-edit.html',
    "<!--master-detail форма для редактирования графа--><div ng-controller=\"listItemEditCtrl\"><div ui-layout=\"{flow:'column',dividerSize:'10'}\"><!--<div ui-layout-container size=\"20%\" >\r" +
    "\n" +
    "            <button class=\"btn btn-info\" ng-click=\"addNew('CsdConnection')\">Добавить модем</button>\r" +
    "\n" +
    "        </div>--><div ui-layout-container><mx-resizer on-resize=\"resize(size)\"><mx-waiting wait-for=\"loadComplete\"><mx-waiting-gif><div class=\"row text-center\"><img src=\"./img/loading.gif\" width=\"140\"></div></mx-waiting-gif><mx-waiting-content><sytoscape elements=\"elements\" height=\"400\" width=\"width\" on-select=\"onSelect(selected)\"></sytoscape></mx-waiting-content></mx-waiting></mx-resizer></div><div ui-layout-container size=\"30%\"><form class=\"form-horizontal\" name=\"frm\"><span ng-switch on=\"selected.type\"><span ng-switch-when=\"Folder\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div></span> <span ng-switch-when=\"Area\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">город</span> <input type=\"text\" class=\"form-control\" placeholder=\"Город\" ng-model=\"selected.city\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">улица</span> <input type=\"text\" class=\"form-control\" placeholder=\"Улица\" ng-model=\"selected.street\"></div></span> <span ng-switch-when=\"Tube\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">адрес</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.networkAddress\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">пароль</span> <input type=\"text\" class=\"form-control\" placeholder=\"Пароль\" ng-model=\"selected.password\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">канал</span> <input type=\"number\" class=\"form-control\" placeholder=\"Канал\" ng-model=\"selected.channel\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">тип</span><select class=\"form-control\" ng-model=\"selected.deviceTypeId\" ng-options=\"dt.id as dt.name for dt in selected.w.deviceTypes\"></select></div></span> <span ng-switch-when=\"CsdConnection\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">телефон</span> <input type=\"text\" class=\"form-control\" placeholder=\"Телефон\" ng-model=\"selected.phone\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">приоритет</span> <input type=\"text\" class=\"form-control\" placeholder=\"Приоритет\" ng-model=\"selected.priority\"></div></span> <span ng-switch-when=\"MatrixConnection\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">IMEI</span> <input type=\"text\" class=\"form-control\" placeholder=\"IMEI\" ng-model=\"selected.imei\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">телефон</span> <input type=\"text\" class=\"form-control\" placeholder=\"Телефон\" ng-model=\"selected.phone\"></div></span> <span ng-switch-when=\"MatrixSwitch\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">свитч</span> <span class=\"form-control\">укажите порт в соединении</span></div></span> <span ng-switch-when=\"MatrixPort\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">порт</span> <input type=\"text\" class=\"form-control\" placeholder=\"Порт\" ng-model=\"selected.port\"></div></span> <span ng-switch-when=\"CsdPort\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">модемы</span> <button class=\"form-control btn btn-default\" ng-click=\"selected.w.editModems()\">Модемы</button></div></span> <span ng-switch-when=\"LanPort\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div></span> <span ng-switch-when=\"ComConnection\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">порт</span> <input type=\"text\" class=\"form-control\" placeholder=\"Порт\" ng-model=\"selected.port\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">приоритет</span> <input type=\"text\" class=\"form-control\" placeholder=\"Приоритет\" ng-model=\"selected.priority\"></div></span> <span ng-switch-when=\"LanConnection\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">ip</span> <input type=\"text\" class=\"form-control\" placeholder=\"ip\" ng-model=\"selected.ip\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">порт</span> <input type=\"text\" class=\"form-control\" placeholder=\"Порт\" ng-model=\"selected.port\"></div></span> <span ng-switch-when=\"SurveyServer\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">название</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\"></div></span> <span ng-switch-when=\"Relation\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">порт</span> <input type=\"text\" class=\"form-control\" placeholder=\"Порт\" ng-model=\"selected.port\"></div></span> <span ng-switch-default><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">тип</span> <span class=\"form-control\">{{selected.type}}</span></div></span></span> <input type=\"hidden\" value=\"{{selected.w.dirty=frm.$dirty}}\"> <span style=\"color:white\">{{selected.id}}</span></form><hr><button ng-click=\"delete(selected)\" class=\"btn btn-default\"><img src=\"img/recycle.png\" height=\"20\"> <span>удалить</span></button><ui-select ng-model=\"data.end\" class=\"mx-margin\"><ui-select-match><span ng-switch on=\"$select.selected.type\"><img ng-switch-when=\"Tube\" src=\"img/counter.png\" height=\"20\"> <img ng-switch-when=\"CsdConnection\" src=\"img/phone.png\" height=\"20\"> <img ng-switch-when=\"ComConnection\" src=\"img/port.png\" height=\"20\"> <img ng-switch-when=\"LanConnection\" src=\"img/lan.png\" height=\"20\"> <img ng-switch-when=\"MatrixConnection\" src=\"img/fastrack.png\" height=\"20\"> <img ng-switch-when=\"SurveyServer\" src=\"img/server.png\" height=\"20\"> <img ng-switch-when=\"Folder\" src=\"img/folder.png\" height=\"20\"> <img ng-switch-when=\"Area\" src=\"img/house.png\" height=\"20\"> <img ng-switch-default src=\"img/add.png\" height=\"20\"></span> {{$select.selected.name}}</ui-select-match><ui-select-choices repeat=\"candidate in candidates | filter: $select.search\" group-by=\"groupBy\"><span ng-switch on=\"candidate.type\"><img ng-switch-when=\"Tube\" src=\"img/counter.png\" height=\"20\"> <img ng-switch-when=\"CsdConnection\" src=\"img/phone.png\" height=\"20\"> <img ng-switch-when=\"ComConnection\" src=\"img/port.png\" height=\"20\"> <img ng-switch-when=\"LanConnection\" src=\"img/lan.png\" height=\"20\"> <img ng-switch-when=\"MatrixConnection\" src=\"img/fastrack.png\" height=\"20\"> <img ng-switch-when=\"SurveyServer\" src=\"img/server.png\" height=\"20\"> <img ng-switch-when=\"Folder\" src=\"img/folder.png\" height=\"20\"> <img ng-switch-when=\"Area\" src=\"img/house.png\" height=\"20\"> <img ng-switch-default src=\"img/add.png\" height=\"20\"></span> <span>{{candidate.name}}</span></ui-select-choices></ui-select><span class=\"btn btn-default\" ng-click=\"add(selected, data.end)\">+</span> <button ng-click=\"save()\" class=\"btn btn-default\">save</button></div></div></div>"
  );


  $templateCache.put('tpls/log-accordion-matrix.html',
    "<uib-accordion-heading>Контроллер матрикс <i class=\"pull-right glyphicon\" ng-class=\"{'glyphicon-chevron-down': accordion.open, 'glyphicon-chevron-right': !accordion.open}\"></i></uib-accordion-heading><div class=\"form-group row\"><div class=\"col-lg-3 col-md-4\"><div class=\"input-group\"><input type=\"text\" class=\"form-control\" ng-model=\"model.actions.matrix.atcmdtext\" placeholder=\"AT+...\"> <span class=\"input-group-btn\"><button class=\"btn btn-default\" type=\"button\" ng-click=\"model.actions.matrix.atcmd.act(model.selected.ids, model.actions.matrix.atcmdtext)\" tooltip=\"{{model.actions.matrix.atcmd.header}}\" tooltip-append-to-body=\"true\"><img ng-src=\"{{model.actions.matrix.atcmd.icon}}\" height=\"20\"></button></span></div></div><div class=\"col-lg-3 col-md-4\"><div class=\"input-group\"><input type=\"text\" class=\"form-control\" ng-model=\"model.actions.matrix.chservertext\" placeholder=\"IP-адрес : порт\"> <span class=\"input-group-btn\"><button class=\"btn btn-default\" type=\"button\" ng-click=\"model.actions.matrix.chserver.act(model.selected.ids, model.actions.matrix.chservertext)\" tooltip=\"{{model.actions.matrix.chserver.header}}\" tooltip-append-to-body=\"true\"><img ng-src=\"{{model.actions.matrix.chserver.icon}}\" height=\"20\"></button></span></div></div><div class=\"col-lg-3 col-md-4\"><span ng-repeat=\"button in model.buttons.matrix\"><button ng-click=\"button.act(model.selected.ids)\" tooltip=\"{{button.header}}\" tooltip-append-to-body=\"true\" class=\"btn btn-default\"><img ng-src=\"{{button.icon}}\" height=\"20\"></button></span></div></div>"
  );


  $templateCache.put('tpls/log-accordion-poll.html',
    "<uib-accordion-heading>Вычислитель <i class=\"pull-right glyphicon\" ng-class=\"{'glyphicon-chevron-down': accordion.open, 'glyphicon-chevron-right': !accordion.open}\"></i></uib-accordion-heading><form fs-form-for=\"\" class=\"form-horizontal\"><div class=\"form-group row\"><div class=\"col-lg-3 col-md-3\" fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div><div class=\"col-lg-3 col-md-3\" fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div><div class=\"col-lg-3 col-md-3\"><button uib-popover-template=\"'poll-details-popover.html'\" popover-title=\"Опрос\" type=\"button\" popover-placement=\"bottom\" popover-append-to-body class=\"btn btn-default\">Детали опроса</button><!--<ul>\r" +
    "\n" +
    "                <li>\r" +
    "\n" +
    "                    <div class=\"checkbox\">\r" +
    "\n" +
    "                        <label>\r" +
    "\n" +
    "                            <input type=\"checkbox\" ng-model=\"model.all.current\" /> Текущие\r" +
    "\n" +
    "                        </label>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "                    <select>\r" +
    "\n" +
    "\r" +
    "\n" +
    "                    </select>\r" +
    "\n" +
    "                </li>\r" +
    "\n" +
    "            </ul>--><!--<label class=\"checkbox-inline\">\r" +
    "\n" +
    "                <input type=\"checkbox\" ng-model=\"model.optionCurrent\"> Текущие\r" +
    "\n" +
    "            </label>\r" +
    "\n" +
    "            <label class=\"checkbox-inline\">\r" +
    "\n" +
    "                <input type=\"checkbox\" ng-model=\"model.optionConstant\"> Константы\r" +
    "\n" +
    "            </label>\r" +
    "\n" +
    "            <label class=\"checkbox-inline\">\r" +
    "\n" +
    "                <input type=\"checkbox\" ng-model=\"model.optionDay\"> Сутки\r" +
    "\n" +
    "            </label>\r" +
    "\n" +
    "            <label class=\"checkbox-inline\">\r" +
    "\n" +
    "                <input type=\"checkbox\" ng-model=\"model.optionHour\"> Часы\r" +
    "\n" +
    "            </label>\r" +
    "\n" +
    "            <label class=\"checkbox-inline\">\r" +
    "\n" +
    "                <input type=\"checkbox\" ng-model=\"model.optionAbnormal\"> НС\r" +
    "\n" +
    "            </label>--></div><div class=\"col-lg-3 col-md-3\"><span ng-repeat=\"button in model.buttons.tube\"><button ng-click=\"button.act(model.selected.ids, {start: model.start, end: model.end,components:poll.components()})\" tooltip=\"{{button.header}}\" tooltip-append-to-body=\"true\" class=\"btn btn-default\"><img ng-src=\"{{button.icon}}\" height=\"20\"></button></span></div></div></form><script type=\"text/ng-template\" id=\"poll-details-popover.html\"><div>\r" +
    "\n" +
    "        <table>\r" +
    "\n" +
    "            <tr>\r" +
    "\n" +
    "                <th>Архив</th>\r" +
    "\n" +
    "                <th>Опросить, если</th>\r" +
    "\n" +
    "                <th></th>\r" +
    "\n" +
    "            </tr>\r" +
    "\n" +
    "            <tr ng-repeat=\"detail in poll.details\">\r" +
    "\n" +
    "                <td>\r" +
    "\n" +
    "                    <div class=\"checkbox\">\r" +
    "\n" +
    "                        <label>\r" +
    "\n" +
    "                            <input type=\"checkbox\" ng-model=\"detail.enabled\">\r" +
    "\n" +
    "                            {{detail.title}}\r" +
    "\n" +
    "                        </label>\r" +
    "\n" +
    "                    </div>\r" +
    "\n" +
    "                </td>\r" +
    "\n" +
    "                <td>\r" +
    "\n" +
    "                    <select ng-model=\"detail.rule\" class=\"form-control\" ng-disabled=\"!detail.enabled\" ng-options=\"rule.name for rule in poll.rules track by rule.id\"></select>\r" +
    "\n" +
    "                    <!--<ui-select ng-model=\"detail.rule\" ng-disabled=\"!detail.enabled\">\r" +
    "\n" +
    "                        <ui-select-match>\r" +
    "\n" +
    "                            <span ng-bind=\"$select.selected.name\"></span>\r" +
    "\n" +
    "                        </ui-select-match>\r" +
    "\n" +
    "                        <ui-select-choices repeat=\"rule in poll.rules track by $index\"\r" +
    "\n" +
    "                                           refresh-delay=\"0\">\r" +
    "\n" +
    "                            <span>\r" +
    "\n" +
    "                                <span>{{rule.name}}</span>\r" +
    "\n" +
    "                            </span>\r" +
    "\n" +
    "                        </ui-select-choices>\r" +
    "\n" +
    "                    </ui-select>-->\r" +
    "\n" +
    "                </td>\r" +
    "\n" +
    "                <td>\r" +
    "\n" +
    "                    <input type=\"number\" class=\"form-control\" ng-disabled=\"!detail.enabled || detail.rule.id!==2\" ng-model=\"detail.duration\" max=\"999\" min=\"0\" style=\"max-width:100px\" />\r" +
    "\n" +
    "                </td>\r" +
    "\n" +
    "            </tr>\r" +
    "\n" +
    "        </table>\r" +
    "\n" +
    "    </div></script>"
  );


  $templateCache.put('tpls/log-eye-cell.html',
    "<a href=\"\" ng-click=\"data.show = !data.show; $parent.$parent.showChanged(data.id, data.show)\"><i class=\"glyphicon\" ng-class=\"data.show? 'glyphicon-eye-open' : 'glyphicon-eye-close'\"></i></a><!--<a href=\"\" ng-click=\"data.show = !data.show; $parent.$parent.showChanged(data.id, data.show)\"><img ng-src=\"/img/16/eye{{!data.show?\\'_close\\':\\'\\'}}.png\" /></a>--><!--<input ng-model='data.show' type='checkbox' ng-click='$parent.$parent.showChanged(data.object, data.show)' />-->"
  );


  $templateCache.put('tpls/log-mini.html',
    "<div ng-controller=\"LogCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div><div class=\"modal-preview-head\"><img src=\"/img/action_log.png\" width=\"16\"> Опрос</div><div class=\"modal-preview-head\"><span ng-if=\"model.names != ''\"><span class=\"badge\" ng-bind=\"model.tubeids.length\" tooltip=\"{{model.names}}\" tooltip-appe tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"></span> <small>{{model.names}}</small></span> <span ng-if=\"model.names.length==0\"><small>нет объектов</small></span></div></div><!-- body --><div ng-if=\"model.lastMessage != undefined\" class=\"modal-preview-body alert alert-info\"><small>({{model.messagesLen}}) <span ng-bind=\"model.lastMessage.date | date: 'dd.MM.yy HH:mm:ss'\"></span> <span ng-bind=\"model.lastMessage.name\"></span><br><span ng-bind=\"model.lastMessage.message\"></span></small></div><div ng-if=\"model.lastMessage == undefined\" class=\"modal-preview-body alert alert-warning\">Нет сообщений</div></a></div>"
  );


  $templateCache.put('tpls/log-modal.html',
    "<div class=\"modal-content\"><div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title modal-preview-head\"><span class=\"media\" tooltip=\"{{model.names}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"><img src=\"/img/action_log.png\" width=\"32\"> <span class=\"badge\" ng-bind=\"model.tubeids.length\"></span></span> Опрос <span ng-if=\"model.names != ''\" class=\"smallergrey\">{{model.names}}</span> <span ng-if=\"model.names == ''\" class=\"red\">нет объектов</span></h3></div><div class=\"modal-body\"><!-- есть список [.|.] --><div class=\"row\" style=\"margin:5px\"><!-- 1/2: выбор объекта [v|.] --><div ng-if=\"!model.only1\" class=\"col-md-3\"><div style=\"overflow: auto; height: 70%\"><ul ng-repeat=\"row in model.rowsContainer | orderBy:['pos','title']\" class=\"nav nav-pills nav-stacked\"><li ng-class=\"{'active': model.selected == row}\"><a style=\"overflow: hidden\" ng-click=\"model.select(row)\"><span ng-bind=\"row.title\"></span></a></li></ul></div></div><!-- 2/2: объект [.|v] --><div ng-class=\"{&quot;col-md-9&quot;: !model.only1, &quot;col-md-12&quot;: model.only1 }\"><uib-accordion close-others=\"true\"><uib-accordion-group ng-repeat=\"accordion in model.accordion\" is-open=\"accordion.open\"><ng-include src=\"accordion.template\"></ng-include></uib-accordion-group></uib-accordion><div ag-grid=\"opt\" class=\"ag-fresh\" style=\"height:60%; width:100%\"></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-class=\"{'active': !model.only1}\" ng-click=\"model.toggleSideList()\"><img src=\"../img/application_side_list.png\" height=\"20\"></button> <button class=\"btn btn-default\" ng-click=\"model.clearLog()\"><img src=\"./img/eraser.png\" alt=\"Очистить\" height=\"20\"> Очистить</button> Сообщений: <span ng-bind=\"model.messagesLen\" tooltip-append-to-body=\"true\"></span></div><div class=\"col-md-6\"><button class=\"btn btn-primary\" ng-click=\"model.modal.dismiss()\">Скрыть</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div></div>"
  );


  $templateCache.put('tpls/log-remove-cell.html',
    "<a href=\"\" ng-click=\"$parent.$parent.removeItem(data.id)\"><i class=\"glyphicon glyphicon-remove\"></i></a>"
  );


  $templateCache.put('tpls/login.html',
    "<div class=\"modal\" tabindex=\"-1\" role=\"dialog\"><div class=\"modal-dialog\" style=\"width: 350px\"><div class=\"modal-content\"><div class=\"modal-header\"><h3 class=\"modal-title\">Авторизация</h3></div><form class=\"modal-body form-horizontal\" role=\"form\"><label for=\"login\">Логин</label><input id=\"login\" class=\"form-control\" placeholder=\"логин\" type=\"text\" required auto-fill><label for=\"password\">Пароль</label><input id=\"password\" class=\"form-control\" placeholder=\"пароль\" type=\"password\" required auto-fill><div class=\"alert alert-danger\" style=\"margin-top:10px\" ng-if=\"error\"><strong>Ошибка!</strong> {{error}}</div><div class=\"modal-footer\"><input type=\"submit\" class=\"btn btn-success\" ng-click=\"ok(login,password,rememberMe)\" value=\"Логин\"></div></form></div></div></div>"
  );


  $templateCache.put('tpls/manager-modems-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/phone_vintage.png\" width=\"32\"> <span>Модемы</span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><!--<div ag-grid=\"opt\" class=\"ag-fresh\" resizable=\"\" style=\"width: 100%\" ng-style=\"{height:windowHeight-80}\"></div>--><div simple-grid=\"myGridConfig\"></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-3\" style=\"text-align: left\"><div class=\"col-xs-9 col-md-9\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div>"
  );


  $templateCache.put('tpls/maquette-list-mini.html',
    "<div ng-controller=\"MaquetteCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div><div class=\"modal-preview-head\"><img src=\"/img/xml_exports.png\" width=\"16\"> Отправка шаблонов макетов 80020</div><div class=\"modal-preview-head\"><span ng-if=\"model.selected\"><small>{{model.selected.name}}</small></span> <span ng-if=\"!model.selected\"><small>макет не выбран</small></span></div></div><!-- body --><div class=\"modal-preview-body alert alert-info\"><img ng-if=\"model.selected.isSending\" src=\"img/loader.gif\" height=\"32\"> <small><span ng-if=\"!model.selected.isSending\" ng-bind=\"&quot;Выбрано дней для отправки: &quot; + model.days.length\"></span></small></div></a></div>"
  );


  $templateCache.put('tpls/maquette-list-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/xml_exports.png\" width=\"32\"> <span ng-if=\"!model.selected\">Отправка шаблонов макетов 80020 <span class=\"smallergrey\">макет не выбран</span></span> <span ng-if=\"model.selected\">Макет 80020: <span ng-bind=\"model.selected.name || '???'\"></span> <span class=\"smallergrey\">отправить в сбытовую команию</span><br><small style=\"color: lightgrey\">[{{model.selected.id}}]</small></span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><!-- 1/1: загрузка [.v.] --><div ng-if=\"!model.maquettes\"><div style=\"display: table; width: 100%; height: 45%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><!-- загружен --><div ng-if=\"model.maquettes\"><!-- 1/1: нет макетов [.v.] --><div ng-if=\"!model.maquettes.length\"><div style=\"display: table; width: 100%; height: 45%; overflow: hidden; text-overflow: ellipsis\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><h3>Нет макетов</h3></div></div></div><!-- есть макеты [.|..] --><div ng-if=\"model.maquettes.length\" class=\"row\" style=\"margin:5px\"><!-- 1/2: выбор макета [v|..] --><div ng-if=\"!model.only1\" class=\"col-md-5\"><div style=\"overflow: auto; height: 45%\"><ul ng-repeat=\"maquette in model.sorted\" class=\"nav nav-pills nav-stacked\"><li ng-class=\"{'active': model.selected == maquette, 'disabled': !maquette.selectable}\"><a style=\"overflow: hidden\" ng-click=\"model.select(maquette)\"><span ng-bind=\"maquette.name\"></span> <span ng-if=\"maquette.daysSent && (maquette.daysSent.length > 0)\" class=\"badge\"><span ng-bind=\"maquette.daysSent.length\"></span></span></a></li></ul></div></div><!-- 2/2: макет [.|.v.] --><div ng-class=\"{&quot;col-md-7&quot;: !model.only1, &quot;col-md-12&quot;: model.only1 }\"><!-- отчет выбран --><div ng-if=\"model.selected\"><div style=\"overflow: auto; height: 45%\"><!--<h4>Текущий драйвер</h4>  style=\"width: 100px\"--><table class=\"table table-hover\"><!--<tr>\r" +
    "\n" +
    "                                <td>Включен</td>\r" +
    "\n" +
    "                                <td>{{!model.selected.disable? 'Да' : 'Нет'}}</td>\r" +
    "\n" +
    "                            </tr>--><tr><td>ИНН</td><td>{{model.selected.Inn}}</td></tr><tr><td>Организация</td><td>{{model.selected.organization}}</td></tr><tr><td>Адрес отправителя</td><td>{{model.selected.sender}}</td></tr><tr><td>Адрес сбытовой компании</td><td>{{model.selected.receiver}}</td></tr><tr><td>Номер последнего макета</td><td>{{model.selected.lastNumber}}</td></tr></table><div style=\"display:inline-block\"><datepicker starting-day=\"1\" ng-model=\"model.day\" ng-click=\"model.addDate()\" class=\"well well-sm\" custom-class=\"model.selected.getDayClass(date, mode)\"></datepicker></div><!--<div ng-if=\"model.selected.days.length > 0\">\r" +
    "\n" +
    "                            <div>\r" +
    "\n" +
    "                                <button ng-click=\"model.selected.days.length = 0\" class=\"btn btn-danger\">Сброс</button>\r" +
    "\n" +
    "                                <button class=\"btn btn-primary\" ng-click=\"model.selected.send()\" ng-disabled=\"model.selected.isSending\">Отправить</button>\r" +
    "\n" +
    "                                <img ng-if=\"model.selected.isSending\" src=\"img/loader.gif\" height=\"32\" />\r" +
    "\n" +
    "                            </div>\r" +
    "\n" +
    "                            <div ng-if=\"!model.selected.isSending\" class=\"red\" ng-bind='\"Макетов для отправки: \" + model.selected.days.length'></div>\r" +
    "\n" +
    "                        </div>--></div></div><!-- отчет НЕ выбран --><div ng-if=\"!model.selected\"><div style=\"display: table; height: 45%; overflow: hidden; text-overflow: ellipsis\"><div style=\"display: table-cell; vertical-align: middle\"><h3>Выберите макет из списка</h3></div></div></div></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-3\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-click=\"model.reset()\">Сброс</button></div><div class=\"col-xs-9 col-md-9\"><img ng-if=\"model.selected.isSending\" src=\"img/loader.gif\" height=\"32\"> <span ng-if=\"model.days.length > 0 && !model.selected.isSending\" class=\"red\" ng-bind=\"&quot;Выбрано дней для отправки: &quot; + model.days.length\"></span> <button class=\"btn btn-primary\" ng-click=\"model.selected.send()\" ng-disabled=\"!model.selected || !model.days || model.days.length == 0 || model.selected.isSending || model.selected.disable\">Отправить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div>"
  );


  $templateCache.put('tpls/modal-tpl-aside.html',
    "<div modal-render=\"{{$isRendered}}\" tabindex=\"-1\" role=\"dialog\" class=\"aside\" modal-animation-class=\"fade\" ng-class=\"{'in': animate}\" ng-style=\"{'z-index': 1050 + index*10, display: 'block' }\" ng-click=\"close($event)\"><div class=\"aside-dialog\"><div class=\"aside-content\" modal-transclude></div></div></div>"
  );


  $templateCache.put('tpls/modal-tpl-full.html',
    "<div modal-render=\"{{$isRendered}}\" tabindex=\"-1\" role=\"dialog\" class=\"modal\" ng-class=\"{in: animate}\" ng-style=\"{'z-index': 1050 + index*10, display: 'block' }\" ng-click=\"close($event)\"><div class=\"modal-dialog\" ng-class=\"size? 'modal-' + size : ''\" style=\"width: 95%\"><div class=\"modal-content\" uib-modal-transclude></div></div></div>"
  );


  $templateCache.put('tpls/modems-list.html',
    "<div ng-controller=\"modemsCtrl\"><div ui-layout=\"{flow:'column',dividerSize:'10'}\"><div ui-layout-container size=\"30%\"><button class=\"btn btn-small\" ng-click=\"add()\">+</button><ul class=\"nav nav-pills nav-stacked\"><li ng-repeat=\"modem in modems\" ng-class=\"{'active': $parent.selected==modem}\"><a style=\"overflow:hidden\" ng-click=\"$parent.select(modem);\"><span ng-if=\"modem.w.dirty\" style=\"color:red\">*</span>{{ modem.comPort}}</a></li></ul></div><div ui-layout-container><form class=\"form-horizontal\" name=\"frm\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">порт</span> <input type=\"text\" class=\"form-control\" placeholder=\"Порт\" ng-model=\"selected.comPort\" ng-required=\"true\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">скорость</span> <input type=\"text\" class=\"form-control\" placeholder=\"Скорость\" ng-model=\"selected.baudRate\" ng-required=\"true\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">дата биты</span> <input type=\"text\" class=\"form-control\" placeholder=\"Дата биты\" ng-model=\"selected.dataBits\" ng-required=\"true\"></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">четность</span><select class=\"form-control\" ng-model=\"selected.parity\"><option value=\"none\" selected>None</option><option value=\"even\">Even</option><option value=\"mark\">Mark</option><option value=\"odd\">Odd</option><option value=\"space\">Space</option></select></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">стоп биты</span><select class=\"form-control\" ng-model=\"selected.stopBits\"><option value=\"none\" selected>None</option><option value=\"one\">One</option><option value=\"onePointFive\">OnePointFive</option><option value=\"two\">Two</option></select></div><input type=\"hidden\" value=\"{{selected.w.dirty=frm.$dirty}}\"></form></div></div></div>"
  );


  $templateCache.put('tpls/mx-menu-item.html',
    "<li><a class=\"pointer\" role=\"menuitem\" tabindex=\"-1\" ng-click=\"click()\"><span><img src=\"{{icon}}\" width=\"20\"></span> <span>{{header}}</span></a></li>"
  );


  $templateCache.put('tpls/mx-panel.tpl.html',
    "<div ng-style=\"width:width,heigth:heigth\"><!--header--><div>{{caption}}</div><ng-transclude></ng-transclude></div>"
  );


  $templateCache.put('tpls/mx-waiting-content.html',
    "<div ng-show=\"display\" ng-transclude></div>"
  );


  $templateCache.put('tpls/mx-waiting-gif.html',
    "<div ng-show=\"display\" ng-transclude></div>"
  );


  $templateCache.put('tpls/mx-waiting.html',
    "<div ng-transclude></div>"
  );


  $templateCache.put('tpls/object-card-cell.html',
    "<a href=\"#\" ng-click=\"$parent.$parent.objectCardOpen(data)\"><img ng-src=\"/img/infocard.png\" width=\"20\"></a>"
  );


  $templateCache.put('tpls/object-card-popover.html',
    "<small>{{data.id}}</small><br>{{data.name}}<br><small>Сетевой адрес: {{data.networkAddress}}</small><br><div ng-if=\"data.Current && data.Current.length>0\"><div>Текущие:</div><ul><li ng-repeat=\"folder in data.Current\"><small><span tooltip=\"{{folder.date}}\">{{folder.s1}}={{folder.d1 | number: 3}} {{folder.s2}}</span></small></li></ul><!--<div ng-repeat=\"folder in data.Current\">\r" +
    "\n" +
    "        <small>\r" +
    "\n" +
    "            <img src=\"/img/folder.png\" height=\"16\" tooltip=\"{{folder.id}}\" />\r" +
    "\n" +
    "            <span tooltip=\"{{folder.date}}\">{{folder.s1}}={{folder.d1}} {{folder.s2}}</span>\r" +
    "\n" +
    "        </small>\r" +
    "\n" +
    "    </div>--></div><div ng-if=\"data.Folder && data.Folder.length>0\"><div>Группы:</div><div ng-repeat=\"folder in data.Folder\"><small><img src=\"/img/folder.png\" height=\"16\" tooltip=\"{{folder.id}}\"> <span ng-bind=\"folder.name\"></span></small></div></div><div ng-if=\"data.Area && data.Area.length>0\"><div>Площадки:</div><div ng-repeat=\"area in data.Area\"><small><img src=\"/img/house.png\" height=\"16\" tooltip=\"{{area.id}}\"> <span ng-bind=\"area.name\"></span></small></div></div><div ng-if=\"data.CsdConnection && data.CsdConnection.length>0\"><div>CSD:</div><div ng-repeat=\"csd in data.CsdConnection\"><small><img src=\"/img/phone.png\" height=\"16\" tooltip=\"{{csd.id}}\"> <span ng-bind=\"csd.phone\"></span></small></div></div><div ng-if=\"data.Device && data.Device.length>0\"><div>Вычислители:</div><div ng-repeat=\"dev in data.Device\"><small><img src=\"/img/counter.png\" height=\"16\" tooltip=\"{{dev.id}}\"> <span ng-bind=\"dev.name\"></span></small></div></div><div ng-if=\"data.CsdPort && data.CsdPort.length>0\"><div>Порты CSD:</div><div ng-repeat=\"port in data.CsdPort\"><small><img src=\"/img/port.png\" height=\"16\" tooltip=\"{{port.id}}\"> <span ng-bind=\"port.name\"></span></small></div></div>"
  );


  $templateCache.put('tpls/object-card.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/infocard.png\" width=\"32\"> {{model.row.cart.name}}{{model.row.name? \": \" + model.row.name : \"\"}}<!--<span ng-if=\"model.status\">\r" +
    "\n" +
    "            <span class=\"media\">\r" +
    "\n" +
    "                <img ng-src=\"/img/{{model.status.state == 'idle'? 'tick.png' : (model.status.state == 'wait'? 'time.png' : (model.status.state == 'process'? 'loader.gif' : (model.status.state == 'disabled'? 'cross.png' : 'error.png')))}}\" width=\"32\" />\r" +
    "\n" +
    "                <span ng-if=\"model.status.count>0\" class=\"badge\">{{model.status.count > 9? '9+' : model.status.count}}</span>\r" +
    "\n" +
    "            </span>\r" +
    "\n" +
    "        </span>--></h3><small style=\"color: lightgrey\">[{{model.row.id}}]</small></div><div class=\"modal-body\" style=\"padding:5px\"><div ng-if=\"model.overlayEnabled\"><div style=\"display: table; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"> <span ng-bind=\"model.overlayText\"></span></div></div></div><div class=\"row\" ng-if=\"!model.overlayEnabled\"><div class=\"col-md-4\"><div class=\"panel panel-default\"><div class=\"panel-heading\">Общие сведения</div><div class=\"panel-body\" style=\"overflow-x: auto\"><table class=\"table table-hover\"><!--<tr>\r" +
    "\n" +
    "                            <td>Название</td>\r" +
    "\n" +
    "                            <td>{{model.row.Area[0].name}}</td>\r" +
    "\n" +
    "                        </tr>--><tr><td>Адрес</td><td>{{model.addr}}</td></tr><tr><td>Тип прибора</td><td>{{model.dev}}</td></tr><tr><td>Телефоны</td><td>{{model.phone}}</td></tr><!--<tr ng-if=\"model.row.networkAddress\">\r" +
    "\n" +
    "                            <td>Сетевой адрес</td>\r" +
    "\n" +
    "                            <td>{{model.row.networkAddress}}</td>\r" +
    "\n" +
    "                        </tr>\r" +
    "\n" +
    "                        <tr ng-if=\"model.row.KTr\">\r" +
    "\n" +
    "                            <td>Коэффициент трансформации</td>\r" +
    "\n" +
    "                            <td>{{model.row.KTr}}</td>\r" +
    "\n" +
    "                        </tr>--><tr><td>Уровень сигнала {{model.signal[0].date | amDateFormat:\"DD.MM.YY HH:mm:ss\"}}</td><td ng-if=\"model.signal[0].img\"><img ng-src=\"/img/{{model.signal[0].img}}\" width=\"20\"> {{model.signal[0].level | number:2}}%</td><td ng-if=\"!model.signal[0].img\">{{model.signal[0].level | number:2}}%</td></tr></table></div></div><!--<div class=\"panel panel-default\" ng-if=\"model.cache.Gsm.length > 0\">\r" +
    "\n" +
    "                <div class=\"panel-heading\">\r" +
    "\n" +
    "                    Связь1\r" +
    "\n" +
    "                    <span ng-if=\"model.cache.Gsm.length>0\" class=\"badge\">\r" +
    "\n" +
    "                        <span ng-bind=\"model.cache.Gsm.length\"></span>\r" +
    "\n" +
    "                    </span>\r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "                <div class=\"panel-body\" style=\"overflow-x: auto;\">\r" +
    "\n" +
    "                    <table class=\"table table-hover\">\r" +
    "\n" +
    "                        <tr ng-if=\"model.loading\">\r" +
    "\n" +
    "                            <td>ХАХАХА</td>\r" +
    "\n" +
    "                            <td></td>\r" +
    "\n" +
    "                        </tr>\r" +
    "\n" +
    "                        <tr ng-if=\"!model.loading\">\r" +
    "\n" +
    "                            <td>Уровень GSM1 сигнала</td>\r" +
    "\n" +
    "                            <td>{{model.signal[0].level}} %</td>\r" +
    "\n" +
    "                        </tr>\r" +
    "\n" +
    "                        <tr ng-if=\"model.quantity.gsm < model.cache.Gsm.length\">\r" +
    "\n" +
    "                            <td colspan=\"3\"><a href=\"#\" ng-click=\"model.quantity.gsm = model.cache.Gsm.length\">Показать полностью</a></td>\r" +
    "\n" +
    "                        </tr>\r" +
    "\n" +
    "                    </table>                    \r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "            </div>--><div class=\"panel panel-default\"><div class=\"panel-heading\">Константы <span ng-if=\"model.constants.length>0\" class=\"badge\"><span ng-bind=\"model.constants.length\"></span></span></div><div class=\"panel-body\" style=\"overflow-x: auto\"><table class=\"table table-hover\"><tr ng-if=\"!model.constants || (model.constants.length == 0)\"><td colspan=\"3\"><i>Нет данных</i></td></tr><tr ng-repeat=\"const in model.constants | orderBy:'s1' | limitTo:model.quantity.constant\"><td>{{const.name}}</td><td>{{const.value}}</td></tr><tr ng-if=\"model.quantity.constant < model.constants.length\"><td colspan=\"3\"><a href=\"#\" ng-click=\"model.quantity.constant = model.constants.length\">Показать полностью</a></td></tr></table></div></div></div><div class=\"col-md-4\"><div class=\"panel panel-default\"><div class=\"panel-heading\">Текущие <span ng-if=\"model.currents.length>0\">на <img src=\"/img/16/server.png\">{{model.currents[0].serverDate | amDateFormat:\"DD.MM.YY HH:mm:ss\"}} <span class=\"badge\"><span ng-bind=\"model.currents.length + 1\"></span></span></span></div><div class=\"panel-body\" style=\"overflow-x: auto\"><table class=\"table table-hover table-striped\"><!--style=\"height:300px\"--><tr><th>Пар.</th><th>Знач.</th><th>Ед.</th></tr><tr ng-if=\"!model.currents || (model.currents.length == 0)\"><td colspan=\"3\"><i>Нет данных</i></td></tr><tr ng-if=\"model.currents && (model.currents.length > 0)\"><td>Время на приборе</td><td colspan=\"2\">{{model.currents[0].date | date: \"dd.MM.yy HH:mm:ss\"}}</td></tr><!--<tr>\r" +
    "\n" +
    "                            <td>Разница</td>\r" +
    "\n" +
    "                            <td>\r" +
    "\n" +
    "                                {{model.currents[0].date | amDifference : null : 'days'}}\r" +
    "\n" +
    "                            </td>\r" +
    "\n" +
    "                            <td>минут</td>\r" +
    "\n" +
    "                        </tr>--><tr ng-repeat=\"current in model.currents | orderBy:'s1' | limitTo:model.quantity.current\"><td>{{current.name}}</td><td>{{current.value | number:3}}</td><td>{{current.unit}}</td></tr><tr ng-if=\"model.quantity.current < model.currents.length\"><td colspan=\"3\"><a href=\"#\" ng-click=\"model.quantity.current = model.currents.length\">Показать полностью</a></td></tr></table></div></div><div class=\"panel panel-default\"><div class=\"panel-heading\">Суточный архив <span ng-if=\"model.days.length>0\">на {{model.days[0].date | amDateFormat:\"DD.MM.YYYY\" }} <span ng-if=\"model.days.length>0\" class=\"badge\"><span ng-bind=\"model.days.length\"></span></span></span></div><div class=\"panel-body\" style=\"overflow-x: auto\"><table class=\"table table-hover table-striped\"><tr><th>Пар.</th><th>Знач.</th><th>Ед.</th></tr><tr ng-if=\"!model.days || (model.days.length == 0)\"><td colspan=\"3\"><i>Нет данных</i></td></tr><tr style=\"display:table-row\" ng-repeat=\"day in model.days | orderBy:'name' | limitTo:model.quantity.hourly\"><td>{{day.name}}</td><td ng-if=\"day.name=='Фото'\" colspan=\"2\"><img ng-src=\"{{day.unit}}\" width=\"160\"></td><td ng-if=\"day.name!='Фото'\">{{day.value | number: 3}}</td><td ng-if=\"day.name!='Фото'\">{{day.unit}}</td></tr><tr ng-if=\"model.quantity.hourly < model.days.length\"><td colspan=\"3\"><a href=\"#\" ng-click=\"model.quantity.hourly = model.days.length\">Показать полностью</a></td></tr></table></div></div></div><div class=\"col-md-4\"><div class=\"panel panel-default\"><div class=\"panel-heading\">Нештатные ситуации <span ng-if=\"model.abnormals.length>0\" class=\"badge\"><span ng-bind=\"model.abnormals.length\"></span></span></div><div class=\"panel-body\" style=\"overflow-x: auto\"><table class=\"table table-hover\"><tr ng-if=\"!model.abnormals || (model.abnormals.length == 0)\"><td colspan=\"3\"><i>Нет данных</i></td></tr><tr ng-repeat=\"abnormal in model.abnormals | orderBy:'-date' | limitTo:model.quantity.abnormal\"><td>{{abnormal.date | amDateFormat: \"DD.MM.YY HH:mm:ss\"}}</td><td>{{abnormal.name}}</td></tr><tr ng-if=\"model.quantity.abnormal < model.abnormals.length\"><td colspan=\"3\"><a href=\"#\" ng-click=\"model.quantity.abnormal = model.abnormals.length\">Показать полностью</a></td></tr></table></div></div></div></div></div><div class=\"modal-footer\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div>"
  );


  $templateCache.put('tpls/parameters-edit.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title modal-preview-head\"><span class=\"media\" tooltip=\"{{model.names.join('; ')}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"><img ng-src=\"/img/tag_{{model.row.cart.isTagged? 'blue':'red'}}.png\" width=\"32\"> <span class=\"badge\" ng-bind=\"model.names.length\"></span></span> Параметры <span ng-if=\"model.names.length>0\" class=\"smallergrey\">для {{model.names.join(', ')}}</span> <span ng-if=\"model.names.length==0\" class=\"red\">нет объектов</span></h3><!--<h3 class=\"modal-title\">\r" +
    "\n" +
    "        <img ng-src=\"/img/tag_{{model.row.cart.isTagged? 'blue':'red'}}.png\" width=\"20\" />\r" +
    "\n" +
    "        Параметры для {{model.row.Area[0].name}}: {{model.row.name}}\r" +
    "\n" +
    "    </h3>--></div><div class=\"modal-body\"><div ng-if=\"model.loading\"><div style=\"display: table; height: 55%; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"> <span>{{model.description}}</span></div></div></div><div ng-if=\"!model.loading\"><!--<div ng-if=\"model.row.Parameter.length===0\">\r" +
    "\n" +
    "            <span>Параметров нет</span>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-if=\"model.row.Parameter.length!==0\">\r" +
    "\n" +
    "            <div ag-grid=\"opt\" class=\"ag-fresh\" style=\"height:70%\"></div>\r" +
    "\n" +
    "        </div>--><div ag-grid=\"model.opt\" class=\"ag-fresh\" style=\"height:70%\"></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-3\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-class=\"{'active': model.IsToolPanelShow}\" ng-click=\"model.toggleToolPanel()\"><img src=\"../img/table_gear.png\" height=\"20\"></button></div><div class=\"col-xs-9 col-md-9\"><button class=\"btn btn-primary btn-default\" ng-click=\"model.recalc()\">Определить из данных</button> <button class=\"btn btn-primary btn-default\" ng-click=\"model.save()\">Сохранить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Отмена</button></div></div></div>"
  );


  $templateCache.put('tpls/poll-days-header.html',
    "<div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><label>От</label><input size=\"6\" class=\"form-control\" ng-model=\"model.start\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\"><label>До</label><input size=\"6\" class=\"form-control\" ng-model=\"model.end\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\"><label class=\"checkbox\"><checkbox ng-model=\"onlyHoles\">Только пропущенные</label></div></div>"
  );


  $templateCache.put('tpls/poll-days.html',
    "<div ng-controller=\"PollCtrl\" style=\"margin:5px\"><div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><div class=\"btn-group\"><button class=\"btn btn-default\" ng-click=\"days()\">Опрос</button><button class=\"btn btn-default\" ng-click=\"cancel()\">Отмена</button></div></div></div></div>"
  );


  $templateCache.put('tpls/poll-hours-header.html',
    "<div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><label>От</label><input size=\"6\" class=\"form-control\" ng-model=\"start\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\"> <input size=\"4\" class=\"form-control\" ng-model=\"start\" data-autoclose=\"1\" placeholder=\"Время\" bs-timepicker type=\"text\"><label>До</label><input size=\"6\" class=\"form-control\" ng-model=\"end\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\"> <input size=\"4\" class=\"form-control\" ng-model=\"end\" data-autoclose=\"1\" placeholder=\"Время\" bs-timepicker type=\"text\"><label class=\"checkbox\"><checkbox ng-model=\"onlyHoles\">Только пропущенные</label></div></div>"
  );


  $templateCache.put('tpls/poll-hours.html',
    "<div ng-controller=\"PollCtrl\" style=\"margin:5px\"><div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><div class=\"btn-group\"><button class=\"btn btn-default\" ng-click=\"hours()\">Опрос</button><button class=\"btn btn-default\" ng-click=\"cancel()\">Отмена</button></div></div></div></div>"
  );


  $templateCache.put('tpls/poll-ping.html',
    "<div ng-controller=\"PollCtrl\" style=\"margin:5px\"><div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><div class=\"btn-group\"><button class=\"btn btn-default\" ng-click=\"ping()\">Пинг</button><button class=\"btn btn-default\" ng-click=\"cancel()\">Отмена</button></div></div></div></div>"
  );


  $templateCache.put('tpls/poll.html',
    "<div ng-controller=\"PollCtrl\" style=\"margin:5px\"><h3 class=\"modal-title\">Опрос архивов ({{caption}})</h3><div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><label>От</label><input size=\"6\" class=\"form-control\" ng-model=\"start\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\"> <input size=\"4\" class=\"form-control\" ng-model=\"start\" data-autoclose=\"1\" placeholder=\"Время\" bs-timepicker type=\"text\"><label>До</label><input size=\"6\" class=\"form-control\" ng-model=\"end\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\"> <input size=\"4\" class=\"form-control\" ng-model=\"end\" data-autoclose=\"1\" placeholder=\"Время\" bs-timepicker type=\"text\"><label class=\"checkbox\"><checkbox ng-model=\"onlyHoles\">Только пропущенные</label></div></div><div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><div class=\"btn-group\"><button class=\"btn btn-default\" ng-click=\"hours()\">Часовой архив</button> <button class=\"btn btn-default\" ng-click=\"days()\">Суточный архив</button> <button class=\"btn btn-default\" ng-click=\"abnormals()\">Нештатные ситуации</button> <button class=\"btn btn-default\" ng-click=\"currents()\">Текущие показания</button> <button class=\"btn btn-default\" ng-click=\"constants()\">Константы</button> <button class=\"btn btn-default\" ng-click=\"ping()\">Пинг</button> <button class=\"btn btn-default\" ng-click=\"cancel()\">Отмена</button></div></div></div><div class=\"navbar navbar-default\" style=\"margin-bottom:5px\" bs-navbar><div class=\"navbar-form navbar-left\"><div class=\"input-group\"><input type=\"text\" class=\"form-control\" placeholder=\"at+...\" ng-model=\"atCommandText\"> <span class=\"input-group-btn\"><button type=\"button\" class=\"btn btn-default\" ng-click=\"atCommandSend()\">Go</button></span></div><button class=\"btn btn-default\" ng-click=\"versionCommandSend()\">Версия</button></div></div><div class=\"row\" style=\"margin:5px\"><button ng-click=\"clear()\" class=\"btn btn-default\"><img src=\"./img/eraser.png\" alt=\"Очистить\" height=\"20\"></button><div style=\"height: 300px\" ui-grid=\"logOptions\" ui-grid-resize-columns></div></div></div>"
  );


  $templateCache.put('tpls/report-edit-mini.html',
    "<div ng-controller=\"ReportEditCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div><div class=\"modal-preview-head\"><img src=\"/img/report_edit.png\" width=\"16\"> Редактор шаблонов отчётов</div><div class=\"modal-preview-head\"><span ng-if=\"!model.selected\"><span class=\"smaller\">отчёт не выбран</span></span> <span ng-if=\"model.selected\"><small>{{ model.selected.name || '???' }}<span class=\"red\" ng-bind=\"model.selected.edited? &quot;*&quot;:&quot;&quot;\"></span></small></span></div></div><!-- body --><div ng-if=\"model.editedCounter>0\" class=\"modal-preview-body alert alert-danger\"><span ng-bind=\"&quot;Непринятые изменения: &quot; + model.editedCounter\"></span></div><div ng-if=\"model.editedCounter==0\" class=\"modal-preview-body alert alert-success\">Нет непринятых изменений</div></a></div>"
  );


  $templateCache.put('tpls/report-edit-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/report_edit.png\" width=\"32\"> <span ng-if=\"!model.selected\">Редактор шаблонов отчётов <span class=\"smallergrey\">отчёт не выбран</span></span> <span ng-if=\"model.selected\">Отчёт: <a href=\"#\" editable-text=\"model.selected.name\" buttons=\"no\">{{ model.selected.name || 'Введите название отчёта' }}</a><span class=\"red\" ng-bind=\"model.selected.edited? &quot;*&quot;:&quot;&quot;\"></span> <span class=\"smallergrey\">редактор</span></span></h3></div><!--var html = '<div ng-show=\"!editing\" ng-click=\"startEditing()\"><img src=\"/img/16/page_edit.png\" /> {{getCalcLabel(data.' + params.colDef.field + ')}}</div> ' +\r" +
    "\n" +
    "'<select style=\"width: 100%\" ng-blur=\"editing=false\" ng-change=\"editing=false\" ng-show=\"editing\" ng-options=\"cal as getCalcLabel(cal) for cal in calcs\" ng-model=\"data.' + params.colDef.field + '\">\r" +
    "\n" +
    "    ';--><div class=\"modal-body\" style=\"padding:5px\"><!-- 1/1: загрузка [.v.] --><div ng-if=\"!model.reports\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><!-- загружен --><div ng-if=\"model.reports\"><!-- 1/1: нет отчётов [.v.] --><div ng-if=\"!model.reports.length\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden; text-overflow: ellipsis\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><h3>Нет отчётов</h3></div></div></div><!-- есть отчёты [.|..] --><div ng-if=\"model.reports.length\" class=\"row\" style=\"margin:5px\"><!-- 1/2: выбор отчёта [v|..] --><div ng-if=\"!model.only1\" class=\"col-md-3\"><div style=\"overflow: auto; height: 75%\"><ul ng-repeat=\"report in model.reports | orderBy:'name'\" class=\"nav nav-pills nav-stacked\"><li ng-class=\"{'active': model.selected == report}\"><a style=\"overflow: hidden\" ng-class=\"{'red': report.edited}\" ng-click=\"model.select(report)\"><span ng-bind=\"report.undo.name\"></span><span ng-bind=\"report.edited? &quot;*&quot;:&quot;&quot;\"></span></a></li></ul></div></div><!-- 2/2: отчёт [.|.v.] --><div ng-class=\"{&quot;col-md-9&quot;: !model.only1, &quot;col-md-12&quot;: model.only1 }\"><!-- отчёт выбран --><div ng-if=\"model.selected\"><!--<h4>Вычислитель</h4>--><table class=\"table table-hover\"><tr><td>Имя отчёта</td><td><a href=\"#\" editable-text=\"model.selected.name\" buttons=\"no\">{{ model.selected.name || 'без названия' }}</a></td></tr><tr><td style=\"width: 100px\">Диапазон</td><td><a href=\"#\" editable-select=\"model.selected.range\" e-ng-options=\"s.value as s.text for s in model.ranges\" buttons=\"no\">{{ model.selected.showRange() }}</a></td></tr><tr><td style=\"width: 100px\">Вид отчёта</td><td><a href=\"#\" editable-select=\"model.selected.target\" e-ng-options=\"s.value as s.text for s in model.targets\" buttons=\"no\">{{ model.selected.showTarget() }}</a></td></tr><tr><td style=\"width: 100px\">Видимость</td><td><a href=\"#\" editable-checkbox=\"model.selected.isHidden\" e-title=\"Скрыть из списка отчётов?\" buttons=\"no\">{{ model.selected.isHidden && \"Скрыт в списке отчётов\" || \"Виден в списке отчётов\" }}</a></td></tr></table><!--<p>\r" +
    "\n" +
    "                        Диапазон: \r" +
    "\n" +
    "                        <a href=\"#\" editable-select=\"model.selected.range\" e-ng-options=\"s.value as s.text for s in model.ranges\">\r" +
    "\n" +
    "                            {{ model.selected.showRange() }}\r" +
    "\n" +
    "                        </a>                        \r" +
    "\n" +
    "                        Вид отчёта:\r" +
    "\n" +
    "                        <a href=\"#\" editable-select=\"model.selected.target\" e-ng-options=\"s.value as s.text for s in model.targets\">\r" +
    "\n" +
    "                            {{ model.selected.showTarget() }}\r" +
    "\n" +
    "                        </a>\r" +
    "\n" +
    "                    </p>--><div style=\"overflow: auto; height: 60%\"><div ui-ace=\"{\r" +
    "\n" +
    "                              useWrapMode: true,\r" +
    "\n" +
    "                              showGutter: false,\r" +
    "\n" +
    "                              mode: 'liquid',\r" +
    "\n" +
    "                              onLoad: model.aceLoaded,\r" +
    "\n" +
    "                              onChange: model.aceChanged\r" +
    "\n" +
    "                            }\" ng-model=\"model.selected.template\"></div></div></div><!-- отчёт НЕ выбран --><div ng-if=\"!model.selected\"><div style=\"display: table; height: 75%; overflow: hidden; text-overflow: ellipsis\"><div style=\"display: table-cell; vertical-align: middle\"><h3>Выберите отчёт из списка</h3></div></div></div></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-class=\"{'active': !model.only1}\" ng-click=\"model.toggleSideList()\"><img src=\"../img/application_side_list.png\" height=\"20\"></button> <button class=\"btn btn-default\" ng-click=\"model.resetAll()\">Сброс</button></div><div class=\"col-xs-9 col-md-6\"><span ng-if=\"model.editedCounter>0\"><span class=\"red\" ng-bind=\"&quot;Непринятые изменения: &quot; + model.editedCounter\"></span> <button class=\"btn btn-primary\" ng-click=\"model.save()\">Сохранить</button></span> <span ng-if=\"model.editedCounter==0\"><button class=\"btn btn-primary\" ng-click=\"model.save()\" disabled>Сохранить</button></span> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div>"
  );


  $templateCache.put('tpls/report-head.html',
    "<div class=\"navbar navbar-default\" role=\"navigation\" bs-navbar style=\"margin-bottom:5px\"><!--<div class=\"container-fluid\">\r" +
    "\n" +
    "        <form class=\"navbar-form navbar-left\">\r" +
    "\n" +
    "            <input type=\"date\" class=\"form-control\" datepicker-popup ng-model=\"model.start\" ng-required=\"true\" />\r" +
    "\n" +
    "            <timepicker ng-model=\"model.start\" hour-step=\"1\" minute-step=\"30\" show-meridian=\"false\"></timepicker>\r" +
    "\n" +
    "            <input type=\"date\" class=\"form-control\" datepicker-popup ng-model=\"model.end\" ng-required=\"true\" />\r" +
    "\n" +
    "            <timepicker ng-model=\"model.end\" hour-step=\"1\" minute-step=\"30\" show-meridian=\"false\"></timepicker>--><!--<input size=\"6\" class=\"form-control\" ng-model=\"model.start\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\">\r" +
    "\n" +
    "    <input size=\"4\" class=\"form-control\" ng-model=\"model.start\" data-autoclose=\"1\" placeholder=\"Время\" bs-timepicker type=\"text\">\r" +
    "\n" +
    "    <input size=\"6\" class=\"form-control\" ng-model=\"model.end\" data-autoclose=\"1\" placeholder=\"Дата\" bs-datepicker data-use-native=\"true\" data-start-week=\"1\" type=\"text\">\r" +
    "\n" +
    "    <input size=\"4\" class=\"form-control\" ng-model=\"model.end\" data-autoclose=\"1\" placeholder=\"Время\" bs-timepicker type=\"text\">--><!--<p class=\"input-group\">\r" +
    "\n" +
    "                <input type=\"date\" class=\"form-control\" datepicker-popup ng-model=\"model.start\" is-open=\"model.startOpened\" ng-required=\"true\" close-text=\"Close\" />\r" +
    "\n" +
    "                <span class=\"input-group-btn\">\r" +
    "\n" +
    "                    <button type=\"button\" class=\"btn btn-default\" ng-click=\"model.startOpen($event)\"><i class=\"glyphicon glyphicon-calendar\"></i></button>\r" +
    "\n" +
    "                </span>\r" +
    "\n" +
    "            </p>\r" +
    "\n" +
    "            <p class=\"input-group\">\r" +
    "\n" +
    "                <timepicker ng-model=\"model.start\" hour-step=\"1\" minute-step=\"30\" show-meridian=\"false\"></timepicker>\r" +
    "\n" +
    "            </p>\r" +
    "\n" +
    "            <p class=\"input-group\">\r" +
    "\n" +
    "                <input type=\"date\" class=\"form-control\" datepicker-popup ng-model=\"model.end\" is-open=\"model.endOpened\" ng-required=\"true\" close-text=\"Close\" />\r" +
    "\n" +
    "                <span class=\"input-group-btn\">\r" +
    "\n" +
    "                    <button type=\"button\" class=\"btn btn-default\" ng-click=\"model.endOpen($event)\"><i class=\"glyphicon glyphicon-calendar\"></i></button>\r" +
    "\n" +
    "                </span>\r" +
    "\n" +
    "            </p>\r" +
    "\n" +
    "            <p class=\"input-group\">\r" +
    "\n" +
    "                <timepicker ng-model=\"model.end\" hour-step=\"1\" minute-step=\"30\" show-meridian=\"false\"></timepicker>\r" +
    "\n" +
    "            </p>\r" +
    "\n" +
    "    </div>--><fs-form-for><div fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div><div fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div><button class=\"btn btn-default navbar-btn\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button> <button class=\"btn btn-default navbar-btn\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\"></button> <button class=\"btn btn-default navbar-btn\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\"></button> <button class=\"btn btn-default navbar-btn\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\"></button></fs-form-for></div>"
  );


  $templateCache.put('tpls/report-list-mini.html',
    "<div ng-controller=\"ReportListCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div><div class=\"modal-preview-head\"><img src=\"/img/report.png\" width=\"16\"> <span ng-if=\"!model.selected\">Построитель отчётов <span class=\"smallergrey\">отчёт не выбран</span></span> <span ng-if=\"model.selected\">Отчёт: <span ng-bind=\"model.selected.name\"></span> <span ng-if=\"model.selected.state=='wait'\"><img src=\"img/16/loader.gif\" width=\"16\"></span> <span ng-if=\"model.selected.state=='success'\"><img src=\"img/16/tick.png\" width=\"16\"></span> <span ng-if=\"model.selected.state=='error'\"><img src=\"img/16/cross.png\" width=\"16\"></span></span></div><div class=\"modal-preview-head\"><span ng-if=\"model.names.length>0\"><span class=\"badge\" ng-bind=\"model.names.length\" tooltip=\"{{model.header}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"></span> <small>{{model.names.join(', ')}}</small></span> <span ng-if=\"model.names.length==0\"><small>нет объектов</small></span></div></div><!-- body --><div class=\"modal-preview-body alert alert-info\" ng-class=\"{'alert-info': model.state=='idle' || model.state=='wait', 'alert-success': model.state=='success', 'alert-danger': model.state=='error' }\"><div ng-if=\"model.state=='idle'\">Ожидание...</div><uib-progressbar ng-if=\"model.errorCounter > 0\" class=\"progress-striped\" value=\"100\" type=\"danger\" style=\"margin-bottom: 0px\"><i>{{model.errorCounter}} не построен(ы)</i></uib-progressbar><uib-progressbar ng-if=\"model.doneCounter > 0\" class=\"progress-striped\" value=\"100\" type=\"success\" style=\"margin-bottom: 0px\"><i>{{model.doneCounter}} готов(ы)</i></uib-progressbar><uib-progressbar ng-if=\"model.waitCounter > 0\" class=\"progress-striped active\" value=\"100\" type=\"info\" style=\"margin-bottom: 0px\"><i>{{model.waitCounter}} строятся...</i></uib-progressbar></div><!--<div ng-if=\"model.selected\">\r" +
    "\n" +
    "            <div ng-if=\"!model.success\" class=\"modal-preview-body alert alert-danger\">\r" +
    "\n" +
    "                <strong>{{model.error}}</strong>\r" +
    "\n" +
    "            </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "            <div ng-if=\"model.success && model.wait\" class=\"modal-preview-body alert alert-info\">\r" +
    "\n" +
    "                <progressbar class=\"progress-striped active\" value=\"100\" type=\"info\" style=\"margin-bottom: 0px\">\r" +
    "\n" +
    "                    <i>построение...</i>\r" +
    "\n" +
    "                </progressbar>\r" +
    "\n" +
    "                с {{model.start | date:\"dd.MM.yyyy HH:mm\"}} по {{model.end | date:\"dd.MM.yyyy HH:mm\"}}\r" +
    "\n" +
    "            </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "            <div ng-if=\"model.success && !model.wait\" class=\"modal-preview-body alert alert-success\">\r" +
    "\n" +
    "                <progressbar class=\"progress-striped\" value=\"100\" type=\"success\" style=\"margin-bottom: 0px\">\r" +
    "\n" +
    "                    <i>Готово!</i>\r" +
    "\n" +
    "                </progressbar>\r" +
    "\n" +
    "                с {{model.start | date:\"dd.MM.yyyy HH:mm\"}} по {{model.end | date:\"dd.MM.yyyy HH:mm\"}}\r" +
    "\n" +
    "            </div>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-if=\"model.doneCounter>0\">\r" +
    "\n" +
    "            <span class=\"darkgreen bold\" ng-bind='\"Построено отчётов: \" + model.doneCounter'></span>\r" +
    "\n" +
    "        </div>--></a></div>"
  );


  $templateCache.put('tpls/report-list-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title modal-preview-head\"><span class=\"media\" tooltip=\"{{model.header}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"><img src=\"/img/report.png\" width=\"32\"> <span class=\"badge\" ng-bind=\"model.names.length\"></span></span> <span ng-if=\"!model.selected\">Построитель отчётов <span class=\"smallergrey\">выберите отчёт</span></span> <span ng-if=\"model.selected\">Отчёт: <span ng-bind=\"model.selected.name\"></span> <span ng-if=\"model.names.length>0\" class=\"smallergrey\">для {{model.names.join(', ')}}</span> <span ng-if=\"model.names.length==0\" class=\"red\">нет объектов</span></span></h3></div><div class=\"modal-body\"><!-- 1/1: загрузка [.v.] --><div ng-if=\"!model.reports\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><!-- загружен --><div ng-if=\"model.reports\"><!-- 1/1: нет отчётов [.v.] --><div ng-if=\"!model.reports.length\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden; text-overflow: ellipsis\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><h3>Нет отчётов</h3></div></div></div><!-- есть отчёты [.|..] --><div ng-if=\"model.reports.length\" class=\"row\" style=\"margin:5px\"><!-- 1/2: выбор отчёта [v|..] --><div ng-if=\"!model.only1\" class=\"col-md-3\"><div style=\"overflow: auto; height: 75%\"><ul class=\"nav nav-pills nav-stacked\"><li ng-repeat=\"report in model.sorted\" ng-class=\"{'active': model.selected == report, 'disabled': !report.selectable}\"><a style=\"overflow: hidden\" ng-class=\"{'darkgreen': report.done, 'bold': report.done}\" ng-click=\"model.select(report)\"><span ng-bind=\"report.name\"></span> <span ng-if=\"report.state=='wait'\"><img src=\"img/16/loader.gif\" width=\"16\"></span> <span ng-if=\"report.state=='success'\"><img src=\"img/16/tick.png\" width=\"16\"></span> <span ng-if=\"report.state=='error'\"><img src=\"img/16/cross.png\" width=\"16\"></span></a></li></ul></div></div><!-- 2/2: отчёт [.|.v.] --><div ng-class=\"{&quot;col-md-9&quot;: !model.only1, &quot;col-md-12&quot;: model.only1 }\"><!-- отчёт НЕ выбран --><div ng-if=\"!model.selected\"><div style=\"display: table; height: 75%; overflow: hidden; text-overflow: ellipsis\"><div style=\"display: table-cell; vertical-align: middle\"><h3>Выберите отчёт из списка</h3></div></div></div><!-- отчёт выбран --><div ng-if=\"model.selected\"><!--<div style=\"overflow: auto; height: 75%\">--><form fs-form-for=\"\" class=\"form-horizontal\"><div class=\"form-group row\"><div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\"><div ng-if=\"!model.selected.range || model.selected.range=='Hour'\" fs-datetime=\"\" ng-model=\"model.selected.start\" ng-disabled=\"false\"></div><div ng-if=\"model.selected.range=='Day'\" fs-date=\"\" ng-model=\"model.selected.start\" ng-disabled=\"false\"></div></div><div class=\"col-lg-4 col-md-5\" style=\"padding-right:0px\"><div ng-if=\"!model.selected.range || model.selected.range=='Hour'\" fs-datetime=\"\" ng-model=\"model.selected.end\" ng-disabled=\"false\"></div><div ng-if=\"model.selected.range=='Day'\" fs-date=\"\" ng-model=\"model.selected.end\" ng-disabled=\"false\"></div></div><div class=\"col-lg-4 col-md-2\"><div class=\"visible-md btn-group\" dropdown><button id=\"split-button\" type=\"button\" class=\"btn btn-default\" ng-click=\"model.selected.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button> <button type=\"button\" class=\"btn btn-default\" dropdown-toggle><span class=\"caret\"></span> <span class=\"sr-only\">Дополнительно</span></button><ul class=\"dropdown-menu\" role=\"menu\" aria-labelledby=\"split-button\"><li role=\"menuitem\"><a href=\"#\" ng-click=\"model.savePdf(model.selected.reportAsHtml)\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\">Экспорт в PDF</a></li><li role=\"menuitem\"><a href=\"#\" ng-click=\"model.toExcel(model.selected.reportAsHtml)\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\">Экспорт в XLS</a></li><li role=\"menuitem\"><a href=\"#\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\">Печать</a></li></ul></div><div class=\"hidden-md btn-group\"><button class=\"btn btn-default\" ng-click=\"model.selected.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button> <button class=\"btn btn-default\" ng-click=\"model.savePdf(model.selected.reportAsHtml)\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\"></button> <button class=\"btn btn-default\" ng-click=\"model.toExcel(model.selected.reportAsHtml)\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\"></button> <button class=\"btn btn-default\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\"></button></div></div></div></form><div ng-if=\"model.selected.state == 'error'\" style=\"overflow: auto; width: 100%; height: 70%\"><div class=\"alert alert-danger\" style=\"margin-top: 0px; margin-bottom: 0px\"><strong>{{model.selected.error}}</strong></div></div><div ng-if=\"model.selected.state != 'error'\" style=\"overflow: auto; width: 100%; height: 70%\"><div ng-if=\"model.selected.state == 'idle'\" class=\"alert alert-info\" style=\"margin-top: 0px; margin-bottom: 0px\">Выберите дату и нажмите <button class=\"btn btn-default\" ng-click=\"model.selected.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button></div><uib-progressbar ng-if=\"model.selected.state == 'wait'\" class=\"progress-striped active\" value=\"100\" type=\"info\" style=\"margin-bottom: 0px\"><i>Идет построение отчёта, ожидайте</i></uib-progressbar><div ng-if=\"model.selected.reportAsHtml\" ng-class=\"{'disabled' : model.selected.state == 'wait'}\" ng-bind-html=\"model.selected.reportAsHtml\" id=\"report-content\"></div><!--bind-html-compile=\"model.selected.reportAsHtml\"--></div><!--</div>--></div></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-class=\"{'active': !model.only1}\" ng-click=\"model.toggleSideList()\"><img src=\"../img/application_side_list.png\" height=\"20\"></button></div><div class=\"col-xs-9 col-md-6\"><span ng-if=\"model.doneCounter>0\"><span class=\"darkgreen bold\" ng-bind=\"&quot;Построено отчётов: &quot; + model.doneCounter\"></span></span> <button class=\"btn btn-primary\" ng-click=\"model.modal.dismiss()\">Скрыть</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div><!--<div class=\"navbar navbar-default\" role=\"navigation\" style=\"margin-bottom:5px\">\r" +
    "\n" +
    "        <div class=\"container-fluid\">\r" +
    "\n" +
    "            <fs-form-for>\r" +
    "\n" +
    "                <div fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                <div fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "            </fs-form-for>\r" +
    "\n" +
    "\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\" /></button>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "    </div>-->"
  );


  $templateCache.put('tpls/report-mini.html',
    "<div ng-controller=\"ReportsCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div><div class=\"modal-preview-head\"><img src=\"/img/report.png\" width=\"16\"> Отчет: <span ng-bind=\"model.report.name\"></span></div><div class=\"modal-preview-head\"><span ng-if=\"model.names.length>0\"><span class=\"badge\" ng-bind=\"model.names.length\" tooltip=\"{{model.names.join('; ')}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"></span> <small>{{model.names.join(', ')}}</small></span> <span ng-if=\"model.names.length==0\"><small>нет объектов</small></span></div></div><!-- body --><div ng-if=\"!model.success\" class=\"modal-preview-body alert alert-danger\"><strong>{{model.error}}</strong></div><div ng-if=\"model.success && model.wait\" class=\"modal-preview-body alert alert-info\"><progressbar class=\"progress-striped active\" value=\"100\" type=\"info\" style=\"margin-bottom: 0px\"><i>построение...</i></progressbar>с {{model.start | date:\"dd.MM.yyyy HH:mm\"}} по {{model.end | date:\"dd.MM.yyyy HH:mm\"}}</div><div ng-if=\"model.success && !model.wait\" class=\"modal-preview-body alert alert-success\"><progressbar class=\"progress-striped\" value=\"100\" type=\"success\" style=\"margin-bottom: 0px\"><i>Готово!</i></progressbar>с {{model.start | date:\"dd.MM.yyyy HH:mm\"}} по {{model.end | date:\"dd.MM.yyyy HH:mm\"}}</div></a></div>"
  );


  $templateCache.put('tpls/report-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title modal-preview-head\"><span class=\"media\" tooltip=\"{{model.names.join('; ')}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\"><img src=\"/img/report.png\" width=\"32\"> <span class=\"badge\" ng-bind=\"model.names.length\"></span></span> Отчет: <span ng-bind=\"model.report.name\"></span> <span ng-if=\"model.names.length>0\" class=\"smallergrey\">для {{model.names.join(', ')}}</span> <span ng-if=\"model.names.length==0\" class=\"red\">нет объектов</span></h3></div><div class=\"modal-body\"><form fs-form-for=\"\" class=\"form-horizontal\"><div class=\"form-group row\"><!--<input type=\"datetime-local\" class=\"form-control\" ng-model=\"model.start\" required />\r" +
    "\n" +
    "            <input type=\"datetime-local\" class=\"form-control\" ng-model=\"model.end\" required />--><div class=\"col-lg-3 col-md-4\" style=\"padding-right:0px\"><div fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div></div><div class=\"col-lg-3 col-md-4\" style=\"padding-right:0px\"><div fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div></div><div class=\"col-lg-6 col-md-4\"><button class=\"btn btn-default\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button> <button class=\"btn btn-default\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\"></button> <button class=\"btn btn-default\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\"></button> <button class=\"btn btn-default\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\"></button></div></div></form><div ng-if=\"!model.success\" class=\"alert alert-danger\" style=\"margin-top: 0px; margin-bottom: 0px\"><strong>{{model.error}}</strong></div><div ng-if=\"model.success && model.wait\"><progressbar class=\"progress-striped active\" value=\"100\" type=\"info\" style=\"margin-bottom: 0px\"><i>Идет построение отчета, ожидайте</i></progressbar></div><div ng-if=\"model.success && !model.wait\"><div style=\"overflow: auto; width: 100%; height: 75%\"><div ng-bind-html=\"model.reportAsHtml\" id=\"report-content\"></div></div></div></div><div class=\"modal-footer\"><button class=\"btn btn-primary\" ng-click=\"model.modal.dismiss()\">Скрыть</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div><!--<div class=\"navbar navbar-default\" role=\"navigation\" style=\"margin-bottom:5px\">\r" +
    "\n" +
    "        <div class=\"container-fluid\">\r" +
    "\n" +
    "            <fs-form-for>\r" +
    "\n" +
    "                <div fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                <div fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "            </fs-form-for>\r" +
    "\n" +
    "\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\" /></button>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "    </div>-->"
  );


  $templateCache.put('tpls/report.html',
    "<div class=\"modal-header\" style=\"padding-bottom: 0px\"><!--<button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button>--><h3 class=\"modal-title\"><img src=\"/img/report.png\" width=\"32\"> Отчет: <span ng-bind=\"model.report.name\"></span> <span class=\"badge\" tooltip=\"{{model.data.ids | json}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\">{{model.data.ids.length}}</span></h3><form fs-form-for=\"\" class=\"form-horizontal\"><div class=\"form-group row\"><!--<input type=\"datetime-local\" class=\"form-control\" ng-model=\"model.start\" required />\r" +
    "\n" +
    "            <input type=\"datetime-local\" class=\"form-control\" ng-model=\"model.end\" required />--><div class=\"col-lg-3 col-md-4\" style=\"padding-right:0px\"><div fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div></div><div class=\"col-lg-3 col-md-4\" style=\"padding-right:0px\"><div fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div></div><div class=\"col-lg-6 col-md-4\"><button class=\"btn btn-default\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\"></button> <button class=\"btn btn-default\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\"></button> <button class=\"btn btn-default\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\"></button> <button class=\"btn btn-default\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\"></button></div></div></form></div><div class=\"modal-body\"><div ng-if=\"!model.success\" class=\"alert alert-danger\" style=\"margin-top: 0px; margin-bottom: 0px\"><strong>{{model.error}}</strong></div><div ng-if=\"model.success && model.wait\"><progressbar class=\"progress-striped active\" value=\"100\" type=\"info\" style=\"margin-bottom: 0px\"><i>Идет построение отчета, ожидайте</i></progressbar></div><div ng-if=\"model.success && !model.wait\"><div style=\"overflow: auto; width: 100%; height: 75%\"><div ng-bind-html=\"model.reportText\" id=\"report-content\"></div></div></div></div><div class=\"modal-footer\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div><!--<div class=\"navbar navbar-default\" role=\"navigation\" style=\"margin-bottom:5px\">\r" +
    "\n" +
    "        <div class=\"container-fluid\">\r" +
    "\n" +
    "            <fs-form-for>\r" +
    "\n" +
    "                <div fs-datetime=\"\" ng-model=\"model.start\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "                <div fs-datetime=\"\" ng-model=\"model.end\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "            </fs-form-for>\r" +
    "\n" +
    "\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.update()\"><img src=\"./img/table_refresh.png\" height=\"20\" title=\"Обновить\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.savePdf()\"><img src=\"./img/pdf.png\" height=\"20\" title=\"Экспорт в PDF\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" ng-click=\"model.toExcel()\"><img src=\"./img/xls.png\" height=\"20\" title=\"Экспорт в XLS\" /></button>\r" +
    "\n" +
    "            <button class=\"btn btn-default navbar-btn\" print-div=\"#report-content\"><img src=\"./img/print.png\" height=\"20\" title=\"Печать\" /></button>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "    </div>-->"
  );


  $templateCache.put('tpls/reports-list.html',
    "<div ng-controller=\"reportsCtrl\"><div ui-layout=\"{flow:'column',dividerSize:'10'}\"><div ui-layout-container size=\"30%\"><input type=\"text\" class=\"form-control\" ng-model=\"reportFilter\" placeholder=\"фильтр\"> <button class=\"btn btn-default\" ng-click=\"add()\">+</button><ul class=\"nav nav-pills nav-stacked\"><li ng-repeat=\"report in reports | filter:reportFilter\" ng-class=\"{'active': $parent.selected==report}\"><a style=\"overflow:hidden\" ng-click=\"$parent.select(report);\"><span ng-if=\"report.w.dirty\" style=\"color:red\">*</span>{{ report.name}}</a></li></ul></div><div ui-layout-container><form class=\"form-horizontal\" name=\"frm\"><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">имя</span> <input type=\"text\" class=\"form-control\" placeholder=\"Название\" ng-model=\"selected.name\" ng-required=\"true\"></div><div class=\"mx-margin\"><label for=\"tpl\">Шаблон</label><textarea id=\"tpl\" class=\"form-control\" ng-model=\"selected.template\"></textarea></div><div class=\"input-group mx-margin\"><span class=\"input-group-addon mx-label\">права</span> <button class=\"btn btn-default\" ng-click=\"setRights(selected.id)\">Права</button></div><!--<div hljs source=\"selected.template\" language=\"xml\"></div>--><input type=\"hidden\" value=\"{{selected.w.dirty=frm.$dirty}}\"></form></div></div></div>"
  );


  $templateCache.put('tpls/rights-cell.html',
    "<a href=\"#\" ng-click=\"$parent.$parent.rightsEdit(data)\"><img ng-src=\"/img/group_key.png\" width=\"20\"></a>"
  );


  $templateCache.put('tpls/rights-edit-modal.html',
    "<div class=\"modal-content\"><div class=\"modal-header\"><h3 class=\"modal-title\"><img src=\"/img/group_key.png\" width=\"32\"> Права</h3></div><div class=\"modal-body\" style=\"padding:5px\"><div ag-grid=\"opt\" class=\"ag-fresh\" style=\"height:70%\"></div><!--<input type=\"text\" class=\"form-control\" ng-model=\"filter\" placeholder=\"Фильтр\" />\r" +
    "\n" +
    "        <div ui-tree=\"options\" data-drag-enabled=\"false\">\r" +
    "\n" +
    "            <ol ui-tree-nodes ng-model=\"roots\" id=\"tree-root\">\r" +
    "\n" +
    "                <li ng-repeat=\"node in roots\" ui-tree-node ng-include=\"'right-nodes-renderer.html'\"></li>\r" +
    "\n" +
    "            </ol>\r" +
    "\n" +
    "        </div>--></div><div class=\"modal-footer\"><button class=\"btn btn-default\" ng-click=\"model.save()\">Сохранить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div><!--<script type=\"text/ng-template\" id=\"right-nodes-renderer.html\">\r" +
    "\n" +
    "    <div ui-tree-handle class=\"tree-node tree-node-content\" style=\"cursor:pointer\">\r" +
    "\n" +
    "        <span ng-if=\"node.children.length>0\" data-nodrag ng-click=\"toggle(this)\">\r" +
    "\n" +
    "            <img ng-if=\"collapsed\" src=\"./img/bullet_toggle_plus.png\" />\r" +
    "\n" +
    "            <img ng-if=\"!collapsed\" src=\"./img/bullet_toggle_minus.png\" />\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-if=\"node.children.length===0\" data-nodrag ng-click=\"toggle(this)\">\r" +
    "\n" +
    "            <img src=\"./img/bullet_toggle_empty.png\" />\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "\r" +
    "\n" +
    "        <span ng-switch on=\"node.class\" ng-click=\"select(this)\">\r" +
    "\n" +
    "            <span ng-switch-when=\"user\">\r" +
    "\n" +
    "                <img src=\"./img/user.png\" width=\"20\" />\r" +
    "\n" +
    "                <span style=\"text-overflow:ellipsis;white-space:normal;overflow:hidden\">\r" +
    "\n" +
    "                    {{node.name}} [{{node.login}}]\r" +
    "\n" +
    "                </span>\r" +
    "\n" +
    "            </span>\r" +
    "\n" +
    "            <span ng-switch-when=\"group\">\r" +
    "\n" +
    "                <span ng-if=\"node.allow !== node.old\" style=\"color:red\">*</span>\r" +
    "\n" +
    "                <img src=\"./img/group.png\" width=\"20\" />\r" +
    "\n" +
    "                <span style=\"text-overflow:ellipsis;white-space:normal;overflow:hidden\">\r" +
    "\n" +
    "                    {{node.name}}\r" +
    "\n" +
    "                </span>\r" +
    "\n" +
    "                <input type=\"checkbox\" ng-disabled=\"node.readOnly\" ng-model=\"node.allow\" />\r" +
    "\n" +
    "\r" +
    "\n" +
    "            </span>\r" +
    "\n" +
    "            <span ng-switch-default>\r" +
    "\n" +
    "                <img src=\"./img/eraser.png\" width=\"20\" />\r" +
    "\n" +
    "            </span>\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <ol ui-tree-nodes=\"\" ng-model=\"node.children\" ng-class=\"{hidden: collapsed}\">\r" +
    "\n" +
    "        <li ng-repeat=\"node in node.children\" ui-tree-node ng-include=\"'right-nodes-renderer.html'\" collapsed=\"true\">\r" +
    "\n" +
    "        </li>\r" +
    "\n" +
    "    </ol>\r" +
    "\n" +
    "</script>--><script type=\"text/ng-template\" id=\"userRightTpl.html\"><span>HAHAHA</span>\r" +
    "\n" +
    "    <input type=\"checkbox\" class=\"form-control\" ng-model=\"data.checked\" /></script>"
  );


  $templateCache.put('tpls/row-editor-mini.html',
    "<div ng-controller=\"RowEditorCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div class=\"modal-preview-head\">Редактор объекта</div><!-- body --><div class=\"modal-preview-body alert alert-info\"></div></a></div>"
  );


  $templateCache.put('tpls/row-editor.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/edit_button.png\" width=\"32\"> {{model1.area.name}} <span class=\"smallergrey\">редактор объекта</span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><div ng-if=\"!isLoaded\"><div style=\"display: table; width: 100%; height: 75%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><div ng-if=\"isLoaded\"><div class=\"row\" style=\"margin:5px\"><div class=\"col-md-6\"><div style=\"overflow: auto; height: 75%\"><form editable-form name=\"areaFrm\"><h4>Площадка<span ng-if=\"areaFrm.$dirty\">*</span></h4><table class=\"table table-hover\"><tr><td style=\"width:100px\">Название</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.area.name\"></td></tr><tr><td>Город</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.area.city\"></td></tr><tr><td>Улица</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.area.street\"></td></tr><tr><td>Дом</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.area.house\"></td></tr><tr><td>Телефон ответственного</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.area.respPhone\"></td></tr></table></form><form name=\"tubeFrm\" editable-form><h4>Точка учёта<span ng-if=\"tubeFrm.$dirty\">*</span></h4><!--{{isTubeDirty = tubeFrm.$dirty}}--><!--<label>\r" +
    "\n" +
    "                            <input type=\"checkbox\" ng-model=\"model1.tube.traceMode\" />\r" +
    "\n" +
    "                            <td>Режим трассировки (показывать все логи)</td>\r" +
    "\n" +
    "                        </label>--><div class=\"checkbox\"><label><input type=\"checkbox\" ng-model=\"model1.tube.traceMode\"> Режим трассировки (показывать все логи)</label></div><div class=\"checkbox\"><label><input type=\"checkbox\" ng-click=\"changeWorkState()\" ng-model=\"model1.tube.isDisabled\"> Отключен</label></div><alert ng-if=\"model1.tube._disabledHistory.length>0\"><span ng-repeat=\"history in model1.tube._disabledHistory | orderBy:'-start' | limitTo:3\"><div>с <i>{{history.start | amDateFormat:\"DD.MM.YYYY\"}}</i> <span ng-if=\"history.end\">по <i>{{history.end | amDateFormat:\"DD.MM.YYYY\"}}</i></span> <b>{{history.reason}}</b></div></span></alert><table class=\"table table-hover\"><tr><td style=\"width:100px\">Название</td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.tube.name\" ng-disabled=\"model1.tube.isDisabled\"></td></tr><tr><td style=\"width:100px\">Вычислитель</td><td><ui-select ng-model=\"model1.device\" theme=\"bootstrap\" ng-disabled=\"model1.tube.isDisabled\" reset-search-input=\"false\" on-select=\"changeDevice($item)\" style=\"width: 300px\" title=\"Выберите тип\"><ui-select-match placeholder=\"Выберите тип...\">{{$select.selected.name}}</ui-select-match><ui-select-choices repeat=\"device in model1.devices track by $index\" refresh-delay=\"0\"><!--<img ng-src=\"{{meta[connection.type].img}}\" width=\"20\" />--><span ng-bind-html=\"device.name | highlight: $select.search\"></span></ui-select-choices></ui-select></td><!--<td>\r" +
    "\n" +
    "                                    <a href=\"#\" editable-select=\"model1.currentDevice\" e-ng-options=\"s as s.name for s in model1.devices\" buttons=\"no\">\r" +
    "\n" +
    "                                        {{model1.currentDevice.name}}\r" +
    "\n" +
    "                                    </a>\r" +
    "\n" +
    "                                </td>--></tr><tr ng-repeat=\"field in model1.device.fieldNames\"><td style=\"width:100px\">{{model1.device.fieldCaptions[$index]}}<!--<a href=\"#\" ng-if=\"field.descr !== ''\" popover=\"{{field.descr}}\" popover-trigger=\"focus\">\r" +
    "\n" +
    "                                        <img src=\"./img/information.png\" width=\"16\" />\r" +
    "\n" +
    "                                    </a>--></td><td><input type=\"text\" class=\"form-control\" ng-model=\"model1.tube[field]\" ng-disabled=\"model1.tube.isDisabled\"></td><!--<a href=\"#\" editable-text=\"model1.tube[field]\" buttons=\"no\">{{ model1.tube[field] || '?' }}</a>--></tr></table></form></div></div><div class=\"col-md-6\"><div style=\"overflow: auto; height: 75%\"><div ng-repeat=\"connection in model1.connections\"><ng-include src=\"'choose-connection-edit-template.html'\"></ng-include><hr style=\"height: 5px; background: black\"></div><hr><span><ui-select ng-model=\"model1.tube._child\" theme=\"bootstrap\" on-select=\"addOrCreateConnection(model1.tube)\" reset-search-input=\"true\" style=\"width: 300px\" title=\"Выберите соединение\"><ui-select-match placeholder=\"Выберите соединение...\">{{$select.selected._text}}</ui-select-match><ui-select-choices repeat=\"connection in model1._connections track by $index\" refresh=\"refreshConnections($select.search,model1,['CsdConnection','MatrixConnection','SimpleMatrixConnection','LanConnection','MatrixSwitch','ZigbeeConnection','HttpConnection'])\" refresh-delay=\"0\"><img ng-src=\"{{meta[connection.type].img}}\" width=\"20\"> <span ng-bind-html=\"connection._text | highlight: $select.search\"></span></ui-select-choices></ui-select></span><!--<h4><a href=\"#\" ng-click=\"addConnection('MatrixConnection')\"><img src=\"./img/add.png\" width=\"20\" /> Добавить Матрикс</a></h4>\r" +
    "\n" +
    "                    <h4><a href=\"#\" ng-click=\"addConnection('MatrixSwitch')\"><img src=\"./img/add.png\" width=\"20\" /> Добавить Матрикс свитч</a></h4>\r" +
    "\n" +
    "                    <h4><a href=\"#\" ng-click=\"addConnection('LanConnection')\"><img src=\"./img/add.png\" width=\"20\" /> Добавить соединение LAN</a></h4>\r" +
    "\n" +
    "                    <h4><a href=\"#\" ng-click=\"addConnection('CsdConnection')\"><img src=\"./img/add.png\" width=\"20\" /> Добавить модем CSD</a></h4>\r" +
    "\n" +
    "                    <h4><a href=\"#\" ng-click=\"addConnection('HttpConnection')\"><img src=\"./img/add.png\" width=\"20\" /> Добавить соединение по интернету</a></h4>\r" +
    "\n" +
    "                    <h4><a href=\"#\" ng-click=\"addConnection('ComConnection')\"><img src=\"./img/add.png\" width=\"20\" /> Добавить последовательное соединение</a></h4>--></div></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-3 col-md-6\" style=\"text-align: left\"><button class=\"btn btn-default\" ng-click=\"model.reset()\">Сброс</button> <button class=\"btn btn-primary\" ng-click=\"model.delete()\">{{trashButton}}</button></div><div class=\"col-xs-9 col-md-6\"><!--<span ng-if=\"model.editedCounter>0\">\r" +
    "\n" +
    "                <span class=\"red\" ng-bind='\"Непринятые изменения: \" + model.editedCounter'></span>\r" +
    "\n" +
    "                <button class=\"btn btn-primary\" ng-click=\"save()\">Сохранить</button>\r" +
    "\n" +
    "            </span>\r" +
    "\n" +
    "            <span ng-if=\"model.editedCounter==0\">\r" +
    "\n" +
    "                <button class=\"btn btn-primary\" ng-click=\"save()\" disabled=\"disabled\">Сохранить</button>\r" +
    "\n" +
    "            </span>--><button class=\"btn btn-primary\" ng-click=\"save()\">Сохранить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div><script type=\"text/ng-template\" id=\"csd-connection-edit.html\"><h4>\r" +
    "\n" +
    "        <span>\r" +
    "\n" +
    "            <img src=\"../img/phone.png\" width=\"20\">\r" +
    "\n" +
    "            Модем\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"$parent.deleteConnection(connection)\">×</button>\r" +
    "\n" +
    "    </h4>\r" +
    "\n" +
    "    <div class=\"checkbox\">\r" +
    "\n" +
    "        <label>\r" +
    "\n" +
    "            <input type=\"checkbox\" ng-model=\"connection.isDisabled\" /> Отключен\r" +
    "\n" +
    "        </label>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <table class=\"table table-hover\" ng-disabled=\"connection.isDisabled\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Телефон</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"connection.phone\" ng-disabled=\"connection.isDisabled\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Ожидание при наборе номера (сек.)</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"connection.callTimeout\" min=\"30\" max=\"180\" ng-disabled=\"connection.isDisabled\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">AT команды</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <textarea ng-model=\"connection.commands\" class=\"form-control\" ng-disabled=\"connection.isDisabled\"></textarea>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Окна (часы опроса, через запятую)</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"connection.windows\" ng-disabled=\"connection.isDisabled\" ng-list />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Оборудование</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <select ng-model=\"connection.dev\" class=\"form-control\" ng-disabled=\"connection.isDisabled\">\r" +
    "\n" +
    "                    <option value=\"modem\">Модем</option>\r" +
    "\n" +
    "                    <option value=\"stel\">Стел</option>\r" +
    "\n" +
    "                </select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Опрос через:</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <ui-select ng-model=\"connection._child\"\r" +
    "\n" +
    "                           theme=\"bootstrap\"\r" +
    "\n" +
    "                           ng-disabled=\"connection.isDisabled\"\r" +
    "\n" +
    "                           on-select=\"$parent.addOrCreateConnection(connection)\"\r" +
    "\n" +
    "                           reset-search-input=\"true\"\r" +
    "\n" +
    "                           style=\"width: 300px;\"\r" +
    "\n" +
    "                           title=\"Выберите порт\">\r" +
    "\n" +
    "                    <ui-select-match placeholder=\"Выберите модемный пул...\">{{$select.selected._text}}</ui-select-match>\r" +
    "\n" +
    "                    <ui-select-choices repeat=\"port in connection._connections track by $index\"\r" +
    "\n" +
    "                                       refresh=\"refreshConnections($select.search,connection,['CsdPort','Modem'])\"\r" +
    "\n" +
    "                                       refresh-delay=\"0\">\r" +
    "\n" +
    "                        <span ng-include=\"connection-icon.html\" onload=\"con = port\"></span>\r" +
    "\n" +
    "                        <span ng-bind-html=\"port._text | highlight: $select.search\"></span>\r" +
    "\n" +
    "                    </ui-select-choices>\r" +
    "\n" +
    "                </ui-select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table></script><script type=\"text/ng-template\" id=\"matrix-connection-edit.html\"><h4>\r" +
    "\n" +
    "        <span>\r" +
    "\n" +
    "            <img src=\"../img/fastrack.png\" width=\"20\">\r" +
    "\n" +
    "            Контроллер матрикс\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"$parent.deleteConnection(connection)\">×</button>\r" +
    "\n" +
    "    </h4>\r" +
    "\n" +
    "    <div class=\"checkbox\">\r" +
    "\n" +
    "        <label>\r" +
    "\n" +
    "            <input type=\"checkbox\" ng-model=\"connection.isDisabled\" /> Отключен\r" +
    "\n" +
    "        </label>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>IMEI</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection.imei\" class=\"form-control\" ng-disabled=\"connection.isDisabled\"></input>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Телефон</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"connection.phone\" ng-disabled=\"connection.isDisabled\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>Порт на контролере</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection._relation.port\" class=\"form-control\" ng-disabled=\"connection.isDisabled\"></input>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Порт матрикс</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <ui-select ng-model=\"connection._child\"\r" +
    "\n" +
    "                           theme=\"bootstrap\"\r" +
    "\n" +
    "                           ng-disabled=\"connection.isDisabled\"\r" +
    "\n" +
    "                           on-select=\"$parent.addOrCreateConnection(connection)\"\r" +
    "\n" +
    "                           reset-search-input=\"true\"\r" +
    "\n" +
    "                           style=\"width: 300px;\"\r" +
    "\n" +
    "                           title=\"Выберите порт\">\r" +
    "\n" +
    "                    <ui-select-match placeholder=\"Опрос через...\">{{$select.selected._text}}</ui-select-match>\r" +
    "\n" +
    "                    <ui-select-choices repeat=\"port in connection._connections track by $index\"\r" +
    "\n" +
    "                                       refresh=\"refreshConnections($select.search,connection,['MatrixPort'])\"\r" +
    "\n" +
    "                                       refresh-delay=\"0\">\r" +
    "\n" +
    "                        <span ng-bind-html=\"port._text | highlight: $select.search\"></span>\r" +
    "\n" +
    "                    </ui-select-choices>\r" +
    "\n" +
    "                </ui-select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table></script><script type=\"text/ng-template\" id=\"matrix-switch-connection-edit.html\"><h4>\r" +
    "\n" +
    "        <span>\r" +
    "\n" +
    "            <img src=\"../img/fastrack.png\" width=\"20\">\r" +
    "\n" +
    "            Свитч матрикс\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"$parent.deleteConnection(connection)\">×</button>\r" +
    "\n" +
    "    </h4>\r" +
    "\n" +
    "    <div class=\"checkbox\">\r" +
    "\n" +
    "        <label>\r" +
    "\n" +
    "            <input type=\"checkbox\" ng-model=\"connection.isDisabled\" /> Отключен\r" +
    "\n" +
    "        </label>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>Название</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection.name\" class=\"form-control\" ng-disabled=\"connection.isDisabled\"></input>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>Порт на свиче</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection._relation.port\" class=\"form-control\" ng-disabled=\"connection.isDisabled\"></input>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Опрос через:</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <ui-select ng-model=\"connection._child\"\r" +
    "\n" +
    "                           theme=\"bootstrap\"\r" +
    "\n" +
    "                           ng-disabled=\"connection.isDisabled\"\r" +
    "\n" +
    "                           on-select=\"$parent.addOrCreateConnection(connection)\"\r" +
    "\n" +
    "                           reset-search-input=\"true\"\r" +
    "\n" +
    "                           style=\"width: 300px;\"\r" +
    "\n" +
    "                           title=\"Выберите порт\">\r" +
    "\n" +
    "                    <ui-select-match placeholder=\"Выберите соединение\">{{$select.selected._text}}</ui-select-match>\r" +
    "\n" +
    "                    <ui-select-choices repeat=\"port in connection._connections track by $index\"\r" +
    "\n" +
    "                                       refresh=\"refreshConnections($select.search,connection,['MatrixConnection'])\"\r" +
    "\n" +
    "                                       refresh-delay=\"0\">\r" +
    "\n" +
    "                        <span ng-bind-html=\"port._text | highlight: $select.search\"></span>\r" +
    "\n" +
    "                    </ui-select-choices>\r" +
    "\n" +
    "                </ui-select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table>\r" +
    "\n" +
    "    <div ng-if=\"connection._child\" ng-repeat=\"connection in [connection._child]\"><ng-include src=\"'choose-connection-edit-template.html'\"></ng-include></div></script><script type=\"text/ng-template\" id=\"lan-connection-edit.html\"><h4>\r" +
    "\n" +
    "        <span>\r" +
    "\n" +
    "            <img src=\"../img/network_adapter.png\" width=\"20\">\r" +
    "\n" +
    "            LAN-соединение\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"$parent.deleteConnection(connection)\">×</button>\r" +
    "\n" +
    "    </h4>\r" +
    "\n" +
    "    <div class=\"checkbox\">\r" +
    "\n" +
    "        <label>\r" +
    "\n" +
    "            <input type=\"checkbox\" ng-model=\"connection.isDisabled\" /> Отключен\r" +
    "\n" +
    "        </label>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>IP</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection.host\" class=\"form-control\" ng-disabled=\"connection.isDisabled\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>Порт</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection.port\" class=\"form-control\" ng-disabled=\"connection.isDisabled\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Порт LAN</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <ui-select ng-model=\"connection._child\"\r" +
    "\n" +
    "                           theme=\"bootstrap\"\r" +
    "\n" +
    "                           ng-disabled=\"connection.isDisabled\"\r" +
    "\n" +
    "                           on-select=\"$parent.addOrCreateConnection(connection)\"\r" +
    "\n" +
    "                           reset-search-input=\"true\"\r" +
    "\n" +
    "                           style=\"width: 300px;\"\r" +
    "\n" +
    "                           title=\"Выберите порт\">\r" +
    "\n" +
    "                    <ui-select-match placeholder=\"Опрос через...\">{{$select.selected._text}}</ui-select-match>\r" +
    "\n" +
    "                    <ui-select-choices repeat=\"port in connection._connections track by $index\"\r" +
    "\n" +
    "                                       refresh=\"refreshConnections($select.search,connection,['LanPort'])\"\r" +
    "\n" +
    "                                       refresh-delay=\"0\">\r" +
    "\n" +
    "                        <span ng-bind-html=\"port._text | highlight: $select.search\"></span>\r" +
    "\n" +
    "                    </ui-select-choices>\r" +
    "\n" +
    "                </ui-select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table></script><script type=\"text/ng-template\" id=\"http-connection-edit.html\"><h4>\r" +
    "\n" +
    "        <span>\r" +
    "\n" +
    "            <img src=\"../img/globe_network.png\" width=\"20\">\r" +
    "\n" +
    "            Соединение через Интернет\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"$parent.deleteConnection(connection)\">×</button>\r" +
    "\n" +
    "    </h4>\r" +
    "\n" +
    "    <div class=\"checkbox\">\r" +
    "\n" +
    "        <label>\r" +
    "\n" +
    "            <input type=\"checkbox\" ng-model=\"connection.isDisabled\" /> Отключен\r" +
    "\n" +
    "        </label>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Порт HTTP</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <ui-select ng-model=\"connection._child\"\r" +
    "\n" +
    "                           theme=\"bootstrap\"\r" +
    "\n" +
    "                           ng-disabled=\"connection.isDisabled\"\r" +
    "\n" +
    "                           on-select=\"$parent.addOrCreateConnection(connection)\"\r" +
    "\n" +
    "                           reset-search-input=\"true\"\r" +
    "\n" +
    "                           style=\"width: 300px;\"\r" +
    "\n" +
    "                           title=\"Выберите порт\">\r" +
    "\n" +
    "                    <ui-select-match placeholder=\"Опрос через...\">{{$select.selected._text}}</ui-select-match>\r" +
    "\n" +
    "                    <ui-select-choices repeat=\"port in connection._connections track by $index\"\r" +
    "\n" +
    "                                       refresh=\"refreshConnections($select.search,connection,['HttpPort'])\"\r" +
    "\n" +
    "                                       refresh-delay=\"0\">\r" +
    "\n" +
    "                        <span ng-bind-html=\"port._text | highlight: $select.search\"></span>\r" +
    "\n" +
    "                    </ui-select-choices>\r" +
    "\n" +
    "                </ui-select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table></script><script type=\"text/ng-template\" id=\"zigbee-connection-edit.html\"><h4>\r" +
    "\n" +
    "        <span>\r" +
    "\n" +
    "            <img src=\"../img/network_wireless.png\" width=\"20\">\r" +
    "\n" +
    "            Беспроводное соединение\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"$parent.deleteConnection(connection)\">×</button>\r" +
    "\n" +
    "    </h4>\r" +
    "\n" +
    "    <div class=\"checkbox\">\r" +
    "\n" +
    "        <label>\r" +
    "\n" +
    "            <input type=\"checkbox\" ng-model=\"connection.isDisabled\" /> Отключен\r" +
    "\n" +
    "        </label>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\" readonly>MAC</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" ng-model=\"connection.mac\" class=\"form-control\" ng-disabled=\"connection.isDisabled\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Вид</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <select class=\"form-control\" ng-model=\"connection.kind\" ng-disabled=\"connection.isDisabled\">\r" +
    "\n" +
    "                    <option value=\"C\">Координатор</option>\r" +
    "\n" +
    "                    <option value=\"R\">Роутер</option>\r" +
    "\n" +
    "                    <option value=\"ED\">Конечное устройство</option>\r" +
    "\n" +
    "                </select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td style=\"width:100px\">Порт беспроводного соединения</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <ui-select ng-model=\"connection._child\"\r" +
    "\n" +
    "                           theme=\"bootstrap\"\r" +
    "\n" +
    "                           ng-disabled=\"connection.isDisabled\"\r" +
    "\n" +
    "                           on-select=\"$parent.addOrCreateConnection(connection)\"\r" +
    "\n" +
    "                           reset-search-input=\"true\"\r" +
    "\n" +
    "                           style=\"width: 300px;\"\r" +
    "\n" +
    "                           title=\"Выберите порт\">\r" +
    "\n" +
    "                    <ui-select-match placeholder=\"Опрос через...\">{{$select.selected._text}}</ui-select-match>\r" +
    "\n" +
    "                    <ui-select-choices repeat=\"port in connection._connections track by $index\"\r" +
    "\n" +
    "                                       refresh=\"refreshConnections($select.search,connection,['ZigbeePort'])\"\r" +
    "\n" +
    "                                       refresh-delay=\"0\">\r" +
    "\n" +
    "                        <span ng-bind-html=\"port._text | highlight: $select.search\"></span>\r" +
    "\n" +
    "                    </ui-select-choices>\r" +
    "\n" +
    "                </ui-select>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table></script><script type=\"text/ng-template\" id=\"choose-connection-edit-template.html\"><div nf-if=\"connection\" ng-switch=\"connection.type\">\r" +
    "\n" +
    "        <div ng-switch-when=\"CsdConnection\">\r" +
    "\n" +
    "            <ng-include src=\"'csd-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-switch-when=\"MatrixConnection\">\r" +
    "\n" +
    "            <ng-include src=\"'matrix-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-switch-when=\"SimpleMatrixConnection\">\r" +
    "\n" +
    "            <ng-include src=\"'matrix-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-switch-when=\"MatrixSwitch\">\r" +
    "\n" +
    "            <ng-include src=\"'matrix-switch-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-switch-when=\"LanConnection\">\r" +
    "\n" +
    "            <ng-include src=\"'lan-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-switch-when=\"HttpConnection\">\r" +
    "\n" +
    "            <ng-include src=\"'http-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "        <div ng-switch-when=\"ZigbeeConnection\">\r" +
    "\n" +
    "            <ng-include src=\"'zigbee-connection-edit.html'\"></ng-include>\r" +
    "\n" +
    "        </div>\r" +
    "\n" +
    "    </div></script><script type=\"text/ng-template\" id=\"connection-icon.html\"><span nf-if=\"con\" ng-switch=\"con.type\">\r" +
    "\n" +
    "        <span ng-switch-when=\"CsdConnection\">\r" +
    "\n" +
    "            <img src=\"../img/phone.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-switch-when=\"MatrixConnection\">\r" +
    "\n" +
    "            <img src=\"../img/fastrack.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-switch-when=\"SimpleMatrixConnection\">\r" +
    "\n" +
    "            <img src=\"../img/fastrack.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-switch-when=\"MatrixSwitch\">\r" +
    "\n" +
    "            <img src=\"../img/fastrack.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-switch-when=\"LanConnection\">\r" +
    "\n" +
    "            <img src=\"../img/network_adapter.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-switch-when=\"HttpConnection\">\r" +
    "\n" +
    "            <img src=\"../img/globe_network.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-switch-when=\"ZigbeeConnection\">\r" +
    "\n" +
    "            <img src=\"../img/network_wireless.png\" width=\"20\">\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "    </span></script><script type=\"text/ng-template\" id=\"disable-confirm.html\"><div class=\"modal-header\">\r" +
    "\n" +
    "        <h3 class=\"modal-title\">Причина отключения</h3>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <div class=\"modal-body\">\r" +
    "\n" +
    "        <div fs-datetime=\"\" ng-model=\"model.date\" ng-disabled=\"false\"></div>\r" +
    "\n" +
    "        <textarea style=\"margin-top:5px\" ng-model=\"model.reason\" class=\"form-control\"></textarea>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "    <div class=\"modal-footer\">\r" +
    "\n" +
    "        <button class=\"btn btn-primary\" type=\"button\" ng-click=\"ok()\">OK</button>\r" +
    "\n" +
    "        <button class=\"btn btn-warning\" type=\"button\" ng-click=\"cancel()\">Отмена</button>\r" +
    "\n" +
    "    </div></script>"
  );


  $templateCache.put('tpls/row.html',
    "<div mx-ctx-init action=\"doSomething(arg)\" arg=\"row.entity\" context-menu data-target=\"row-menu-{{::rowRenderIndex}}\"><!--{{rowRenderIndex}}--><div ng-repeat=\"col in colContainer.renderedColumns track by col.colDef.name\" class=\"ui-grid-cell\" ui-grid-cell></div></div><div class=\"dropdown position-fixed\" id=\"row-menu-{{::rowRenderIndex}}\"><ul class=\"dropdown-menu\" role=\"menu\"><mx-menu-item data-name=\"poll\" data-arg=\"[row.entity.id]\"></mx-menu-item><mx-menu-item data-name=\"list-item-edit\" data-arg=\"[row.entity.id]\"></mx-menu-item><mx-menu-item data-name=\"rights\" data-arg=\"[row.entity.Area[0].id]\"></mx-menu-item><li><a class=\"pointer\" role=\"menuitem\" tabindex=\"-1\">дабл клик 2</a></li><mx-menu-item data-name=\"foo1\" data-arg=\"bye\"></mx-menu-item><li><a class=\"pointer\" role=\"menuitem\" tabindex=\"-1\"><span><img src=\"./img/house_add.png\" width=\"20\"></span><span>дабл клик</span></a></li><li role=\"presentation\" class=\"divider\"></li><li><a class=\"pointer\" role=\"menuitem\" tabindex=\"-1\">дабл клик 2</a></li></ul></div>"
  );


  $templateCache.put('tpls/select-cell.html',
    "<input type=\"checkbox\" ng-model=\"data.selected\" ng-click=\"$parent.$parent.rowSelected(data.id, data.selected)\">"
  );


  $templateCache.put('tpls/task-cell.html',
    "<div class=\"ui-grid-cell-contents\"><div ng-if=\"row.entity[col.field]===1\"><img src=\"./img/sheduled_task.png\" width=\"20\" title=\"{{row.entity['taskName']}}\"></div></div>"
  );


  $templateCache.put('tpls/test-mini.html',
    "<div ng-controller=\"TestCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div class=\"modal-preview-head\">Тест <span class=\"badge\" tooltip=\"{{model.names.join('; ')}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\" ng-bind=\"model.names.length\"></span></div><!-- body --><div class=\"modal-preview-body alert alert-info\">Кол-во: <span ng-bind=\"model.counter\"></span></div></a></div>"
  );


  $templateCache.put('tpls/test-modal.html',
    "<div class=\"modal-header\" style=\"padding-bottom: 0px\"><h3 class=\"modal-title\"><img src=\"/img/action_log.png\" width=\"32\"> Тест <span class=\"badge\" tooltip=\"{{model.ids | json}}\" tooltip-append-to-body=\"true\" tooltip-placement=\"bottom\">{{model.ids.length}}</span></h3></div><div class=\"modal-body\"><h1 ng-bind=\"model.counter\"></h1><button ng-click=\"model.decrement()\" class=\"btn btn-default\"><img src=\"./img/16/toggle.png\"></button> <button ng-click=\"model.increment()\" class=\"btn btn-default\"><img src=\"./img/16/toggle_expand.png\"></button></div><div class=\"modal-footer\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div>"
  );


  $templateCache.put('tpls/test-show-selected.html',
    "<div class=\"modal-header\"><h3 class=\"modal-title\">Выделенные <span class=\"badge\">{{model.data.length}}</span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><accordion close-others=\"true\"><accordion-group ng-repeat=\"obj in model.data\"><accordion-heading>{{obj.Area[0].name}}: {{obj.name}}</accordion-heading><div style=\"overflow: scroll; width: 100%; height: 75%\"><pre ng-bind=\"obj | json\"></pre></div><!--<ul ng-repeat=\"prop in obj\">\r" +
    "\n" +
    "                <li>\r" +
    "\n" +
    "                    <pre ng-bind=\"prop | json\"></pre>\r" +
    "\n" +
    "                </li>\r" +
    "\n" +
    "            </ul>--></accordion-group></accordion></div><div class=\"modal-footer\"><button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div>"
  );


  $templateCache.put('tpls/users-list.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/user_bender.png\" width=\"32\"> <span ng-if=\"!model.selected\">Пользователи <span class=\"smallergrey\">пользователь или группа не выбраны</span></span> <span ng-if=\"model.selected\">Пользователь: <span class=\"smallergrey\">{{model.selected.name}}</span></span></h3></div><div class=\"modal-body\" style=\"padding:5px\"><div ng-if=\"!model.isLoaded\"><div style=\"display: table; height: 55%; width: 100%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle; text-align: center\"><img ng-src=\"/img/loader.gif\" width=\"32\"></div></div></div><div ng-if=\"model.isLoaded\"><div class=\"row\" style=\"margin:5px\"><div class=\"col-md-6\"><input type=\"text\" class=\"form-control\" ng-model=\"filter\" style=\"margin:5px\"><div ui-tree id=\"tree-root\" data-drag-enabled=\"false\" class=\"well well-sm pre-scrollable\" style=\"max-height:550px;width:100%\"><ol ui-tree-nodes ng-model=\"model.users\"><li ng-repeat=\"node in model.users\" ui-tree-node data-nodrag data-collapsed ng-include=\"'users-nodes-renderer.html'\"></li></ol></div></div><div class=\"col-md-6\"><form name=\"editForm\"><div ng-if=\"model.selected\"><span style=\"display:none\">{{model.setForm(editForm)}}{{model.selected._dirty=editForm.$dirty}}</span><div style=\"overflow: auto; height: 55%\" ng-switch=\"model.selected.type\"><div ng-switch-when=\"User\"><ng-include src=\"'user-editor-tpl.html'\"></ng-include></div><div ng-switch-when=\"Group\"><ng-include src=\"'group-editor-tpl.html'\"></ng-include></div></div></div></form><div ng-if=\"!model.selected\"><div style=\"display: table; height: 55%; overflow: hidden\"><div style=\"display: table-cell; vertical-align: middle\"><h3>Выберите пользователя или группу</h3></div></div></div></div></div></div></div><div class=\"modal-footer\"><div class=\"row\"><div class=\"col-xs-9 col-md-9\"><button class=\"btn btn-primary\" ng-click=\"model.save()\">Сохранить</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div></div></div><script type=\"text/ng-template\" id=\"user-editor-tpl.html\"><h4>Человек<span ng-if=\"userForm.$dirty\">*</span></h4>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td>Логин</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"model.selected.login\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td>Пароль</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <div class=\"input-group\">\r" +
    "\n" +
    "                    <input type=\"password\" class=\"form-control\" ng-model=\"model.selected._password\" />\r" +
    "\n" +
    "                    <span class=\"input-group-btn\">\r" +
    "\n" +
    "                        <button class=\"btn btn-default\" id=\"basic-addon2\" ng-click=\"model.applyPass(model.selected)\">Принять</button>\r" +
    "\n" +
    "                    </span>\r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td>Имя</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"model.selected.name\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td>Фамилия</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"model.selected.surname\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td>Отчество</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"model.selected.patronymic\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td colspan=\"2\">\r" +
    "\n" +
    "                <div class=\"checkbox\">\r" +
    "\n" +
    "                    <label>\r" +
    "\n" +
    "                        <input type=\"checkbox\" ng-model=\"model.selected.isAdmin\" /> Администратор\r" +
    "\n" +
    "                    </label>\r" +
    "\n" +
    "                </div>\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "    </table></script><script type=\"text/ng-template\" id=\"group-editor-tpl.html\"><h4>Группа</h4>\r" +
    "\n" +
    "    <table class=\"table table-hover\">\r" +
    "\n" +
    "        <tr>\r" +
    "\n" +
    "            <td>Название</td>\r" +
    "\n" +
    "            <td>\r" +
    "\n" +
    "                <input type=\"text\" class=\"form-control\" ng-model=\"model.selected.name\" />\r" +
    "\n" +
    "            </td>\r" +
    "\n" +
    "        </tr>\r" +
    "\n" +
    "        <!--<tr>\r" +
    "\n" +
    "            <td style=\"width: 100px\">Вид</td>\r" +
    "\n" +
    "            <td></td>\r" +
    "\n" +
    "        </tr>-->\r" +
    "\n" +
    "    </table>\r" +
    "\n" +
    "    <button class=\"btn btn-primary\" ng-click=\"model.addUser(model.selected)\">Добавить пользователя</button>\r" +
    "\n" +
    "    <button class=\"btn btn-primary\" ng-click=\"model.addGroup(model.selected)\">Добавить подгруппу</button></script><script type=\"text/ng-template\" id=\"users-nodes-renderer.html\"><div ui-tree-handle class=\"tree-node tree-node-content\" style=\"cursor:pointer\">\r" +
    "\n" +
    "        <a ng-if=\"node._children && node._children.length > 0\" data-nodrag ng-click=\"toggle(this)\">\r" +
    "\n" +
    "            <img ng-if=\"!collapsed\" src=\"/img/16/toggle.png\" />\r" +
    "\n" +
    "            <img ng-if=\"collapsed\" src=\"/img/16/toggle_expand.png\" />\r" +
    "\n" +
    "        </a>\r" +
    "\n" +
    "        <a ng-if=\"!(node._children && node._children.length > 0)\" data-nodrag>\r" +
    "\n" +
    "            <img src=\"/img/16/toggle.png\" />\r" +
    "\n" +
    "        </a>\r" +
    "\n" +
    "        <span ng-if=\"node.type==='User'\" ng-click=\"model.selectNode(node)\">\r" +
    "\n" +
    "            <img src=\"/img/user.png\" /> {{node.login}}<small ng-if=\"node._dirty\">*</small>\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "        <span ng-if=\"node.type==='Group'\" ng-click=\"model.selectNode(node)\">\r" +
    "\n" +
    "            <img src=\"/img/group.png\" /> {{node.name}}<small ng-if=\"node._dirty\">*</small>\r" +
    "\n" +
    "        </span>\r" +
    "\n" +
    "    </div>\r" +
    "\n" +
    "\r" +
    "\n" +
    "    <ol ui-tree-nodes=\"\" ng-model=\"node._children\" ng-class=\"{hidden: collapsed}\">\r" +
    "\n" +
    "        <li ng-repeat=\"node in node._children\" ui-tree-node data-nodrag data-collapsed ng-include=\"'users-nodes-renderer.html'\">\r" +
    "\n" +
    "        </li>\r" +
    "\n" +
    "    </ol></script>"
  );


  $templateCache.put('tpls/vserial-mini.html',
    "<div ng-controller=\"VSerialCtrl\"><a ng-click=\"model.modalOpen()\" href=\"#\"><!-- head --><div class=\"modal-preview-head\"><img src=\"/img/port.png\" width=\"20\"> Виртуальный COM-порт<br><div ng-if=\"model.server.state=='disconnected'\"><div><!--<b class=\"red\">Объект:</b>--><span ng-if=\"model.server.currentName != ''\"><!--<img src=\"/img/house.png\" width=\"20\" />-->{{model.server.currentName}}</span> <span ng-if=\"model.server.currentName == ''\" class=\"red\">???</span></div></div><div ng-if=\"model.server.state != 'disconnected'\"><div><!--<b class=\"darkgreen\">Объект:</b>--><span ng-if=\"model.server.targetName != ''\"><!--<img src=\"/img/house.png\" width=\"20\" />-->{{model.server.targetName}}</span> <span ng-if=\"model.server.targetName == ''\" class=\"red\">???</span></div></div></div><!-- body --><div class=\"modal-preview-body alert alert-info\"><span ng-class=\"{'red': model.vcom.state == 'disconnected', 'darkgreen': model.vcom.state == 'connected', 'blue': model.vcom.state != 'disconnected' && model.vcom.state != 'connected'}\">Сервер COM-порта<span ng-if=\"model.serial.state=='opened'\">: {{model.serial.target}}</span></span><br><span ng-class=\"{'red' : !model.server.isReceiving, 'green' : model.server.isReceiving}\">&lArr;</span>&nbsp; <span ng-class=\"{'red' : !model.serial.isTransmitting, 'green' : model.serial.isTransmitting}\">&lArr;</span>&nbsp; <span ng-class=\"{'red' : !model.server.isTransmitting, 'green' : model.server.isTransmitting}\">&rArr;</span>&nbsp; <span ng-class=\"{'red' : !model.serial.isReceiving, 'green' : model.serial.isReceiving}\">&rArr;</span><br><span ng-class=\"{'red': model.server.state == 'disconnected', 'darkgreen': model.server.state == 'connected', 'blue': model.server.state != 'disconnected' && model.server.state != 'connected'}\">Сервер опроса</span></div></a></div>"
  );


  $templateCache.put('tpls/vserial-modal.html',
    "<div class=\"modal-header\"><button type=\"button\" class=\"close\" data-dismiss=\"modal\" ng-click=\"model.close()\" data-target=\"#report-window\" aria-hidden=\"true\">×</button><h3 class=\"modal-title\"><img src=\"/img/port.png\" width=\"32\"> <span>Виртуальный COM-порт</span></h3></div><div class=\"modal-body\"><div class=\"row\"><div ng-class=\"{'col-md-5' : !model.accordion.open, 'col-md-6' : model.accordion.open}\"><div class=\"panel panel-default\"><div class=\"panel-heading\">Сервер COM-порта</div><div class=\"panel-body\"><!--<span ng-class=\"{'red' : !model.serial.isReceiving, 'green' : model.serial.isReceiving}\">--&gt;</span><br />\r" +
    "\n" +
    "                    <span ng-class=\"{'red' : !model.serial.isTransmitting, 'green' : model.serial.isTransmitting}\">&lt;--</span><br />--><div ng-if=\"model.vcom.state == 'disconnected'\"><div class=\"red\"><b>Адрес:</b> {{model.vcom.host}}:{{model.vcom.port}}</div><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.vcom.connect()\"><img src=\"/img/connect.png\" width=\"20\"> Подключиться</button></div><div ng-if=\"model.vcom.state == 'connected'\"><div class=\"darkgreen\"><b>Адрес:</b> {{model.vcom.host}}:{{model.vcom.port}}</div><div><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.vcom.disconnect()\"><img src=\"/img/disconnect.png\" width=\"20\"> Отключиться</button></div><div><br><div ng-if=\"model.serial.state=='closed'\"><div><b class=\"red\">Выберите COM-порт:</b><select ng-model=\"model.serial.current\" ng-options=\"port for port in model.serial.ports\" ng-disabled=\"model.serial.ports.length == 0\"></select><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.serial.status()\"><img src=\"/img/arrow_refresh_small.png\" width=\"20\"></button></div><div><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.serial.open()\" ng-disabled=\"model.serial.current.length == 0\">Открыть</button></div></div><div ng-if=\"model.serial.state=='opened'\"><div><b class=\"darkgreen\">Выбран COM-порт:</b> {{model.serial.target}} <button type=\"button\" class=\"btn btn-default\" ng-click=\"model.serial.status()\"><img src=\"/img/arrow_refresh_small.png\" width=\"20\"></button></div><div><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.serial.close()\">Закрыть</button></div></div><div ng-if=\"model.serial.state!='closed' && model.serial.state!='opened'\"><div><b class=\"blue\">Ожидание COM-порта:</b> <span ng-if=\"model.serial.target != ''\">{{model.serial.target}}</span> <span ng-if=\"model.serial.target == ''\" class=\"red\">???</span> <button type=\"button\" class=\"btn btn-default\" ng-click=\"model.serial.status()\"><img src=\"/img/arrow_refresh_small.png\" width=\"20\"></button></div><div><button type=\"button\" class=\"btn btn-default\" disabled>Ожидание...</button></div></div></div></div><div ng-if=\"model.vcom.state != 'disconnected' && model.vcom.state != 'connected'\"><div class=\"blue\"><b>Адрес:</b> {{model.vcom.host}}:{{model.vcom.port}}</div><button type=\"button\" class=\"btn btn-default\" disabled><img src=\"/img/loader.gif\" width=\"20\"> Ожидание...</button></div></div><!--<button type=\"button\" class=\"btn btn-default\" ng-click=\"model.vcom.turn()\"\r" +
    "\n" +
    "                        ng-bind-html=\"(model.vcom.state == 'disconnected'? 'Подключиться' : (model.vcom.state == 'connected'? 'Отключиться' : '<img src=\\'/img/loader.gif\\' width=\\'20\\'> Ожидание...'))\"></button>--><!--<button type=\"button\" class=\"btn btn-default\" ng-click=\"model.vcom.disconnect()\">Закрыть соединение</button>--></div></div><div ng-if=\"!model.accordion.open\" class=\"hidden-sm col-md-1\"><span ng-class=\"{'red' : !model.serial.isReceiving, 'green' : model.serial.isReceiving}\">&rArr;</span><br><span ng-class=\"{'red' : !model.serial.isTransmitting, 'green' : model.serial.isTransmitting}\">&lArr;</span></div><div ng-if=\"!model.accordion.open\" class=\"hidden-sm col-md-1\" style=\"text-align: right\"><span ng-class=\"{'red' : !model.server.isTransmitting, 'green' : model.server.isTransmitting}\">&rArr;</span><br><span ng-class=\"{'red' : !model.server.isReceiving, 'green' : model.server.isReceiving}\">&lArr;</span></div><div ng-class=\"{'col-md-5' : !model.accordion.open, 'col-md-6' : model.accordion.open}\"><div class=\"panel panel-default\"><div class=\"panel-heading\">Сервер опроса</div><div class=\"panel-body\"><div ng-if=\"model.server.state=='disconnected'\"><div><b class=\"red\">Объект:</b> <span ng-if=\"model.server.currentName != ''\"><img src=\"/img/house.png\" width=\"20\"> {{model.server.currentName}}</span> <span ng-if=\"model.server.currentName == ''\" class=\"red\">сначала выберите объект из списка</span></div><div><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.server.connect()\"><img src=\"/img/control_play_blue.png\" width=\"20\"> Начать опрос через виртуальный порт</button></div></div><div ng-if=\"model.server.state == 'connected'\"><div><b class=\"darkgreen\">Объект:</b> <span ng-if=\"model.server.targetName != ''\"><img src=\"/img/house.png\" width=\"20\"> {{model.server.targetName}}</span> <span ng-if=\"model.server.targetName == ''\" class=\"red\">???</span></div><div><button type=\"button\" class=\"btn btn-default\" ng-click=\"model.server.disconnect()\"><img src=\"/img/control_stop_blue.png\" width=\"20\"> Закончить опрос</button></div><div ng-if=\"model.server.targetStatus\" ng-class=\"{'darkgreen' : model.server.targetStatus.code == 20, 'red' : model.server.targetStatus.state != 20}\"><br><b>Статус:</b> <img ng-if=\"model.server.targetStatusImg\" ng-src=\"/img/{{model.server.targetStatusImg}}\" width=\"20\"> {{model.server.targetStatusTitle}}</div></div><div ng-if=\"model.server.state != 'connected' && model.server.state != 'disconnected'\"><div><b class=\"blue\">Объект:</b> <span ng-if=\"model.server.targetName != ''\"><img src=\"/img/house.png\" width=\"20\"> {{model.server.targetName}}</span> <span ng-if=\"model.server.targetName == ''\" class=\"red\">???</span></div><div><button type=\"button\" class=\"btn btn-default\" disabled><img src=\"/img/loader.gif\" width=\"20\"> Ожидание...</button></div></div></div><!--<b>Объект:</b> {{(model.server.state=='disconnected'? model.server.currentName : model.server.targetName)}} <br />--><!--<button type=\"button\" class=\"btn btn-default\" ng-click=\"model.server.turn()\" ng-bind-html=\"(model.server.state == 'disconnected'? 'Подключиться' : (model.vcom.state == 'connected'? 'Отключиться' : '<img src=\\'/img/loader.gif\\' width=\\'20\\'> Ожидание...'))\"></button>--><!--<button type=\"button\" class=\"btn btn-default\" ng-click=\"model.server.turn()\">\r" +
    "\n" +
    "                    <img src=\"/img/connect.png\" ng-if=\"model.server.state == 'disconnected'\" width=\"20\" />\r" +
    "\n" +
    "                    <img src=\"/img/disconnect.png\" ng-if=\"model.server.state == 'connected'\" width=\"20\" />\r" +
    "\n" +
    "                    <img src=\"/img/loader.gif\" ng-if=\"model.server.state != 'disconnected' && model.server.state != 'connected'\" width=\"20\" />\r" +
    "\n" +
    "                    <span ng-bind=\"(model.server.state == 'disconnected'? 'Подключиться' : (model.server.state == 'connected'? 'Отключиться' : 'Ожидание...'))\"></span>\r" +
    "\n" +
    "                </button>--><!--<span ng-class=\"{'red' : !model.server.isTransmitting, 'green' : model.server.isTransmitting}\">--&gt;</span><br />\r" +
    "\n" +
    "                <span ng-class=\"{'red' : !model.server.isReceiving, 'green' : model.server.isReceiving}\">&lt;--</span><br />\r" +
    "\n" +
    "                <b>Объект:</b> {{model.server.currentName}}<br />\r" +
    "\n" +
    "                state: {{model.server.state}} <br />\r" +
    "\n" +
    "                target: {{model.server.targetName}} <br />\r" +
    "\n" +
    "                <button type=\"button\" class=\"btn btn-default\" ng-click=\"model.server.connect()\" ng-disabled=\"model.server.state != 'disconnected'\">Открыть соединение</button>\r" +
    "\n" +
    "                <button type=\"button\" class=\"btn btn-default\" ng-click=\"model.server.disconnect()\" ng-disabled=\"model.server.state != 'connected'\">Закрыть соединение</button>--></div></div></div><div><accordion close-others=\"true\"><accordion-group is-open=\"model.accordion.open\"><accordion-heading>Журнал обмена данными <i class=\"pull-right glyphicon\" ng-class=\"{'glyphicon-chevron-down': model.accordion.open, 'glyphicon-chevron-right': !model.accordion.open}\"></i></accordion-heading><div>Сообщений: <span ng-bind=\"model.accordion.log.length\"></span> <button class=\"btn btn-default\" ng-click=\"model.accordion.log.length = 0\">Очистить</button></div><div ng-repeat=\"msg in model.accordion.log\">{{msg.text}}</div></accordion-group></accordion></div></div><div class=\"modal-footer\"><button class=\"btn btn-primary\" ng-click=\"model.modal.dismiss()\">Скрыть</button> <button class=\"btn btn-warning\" ng-click=\"model.close()\">Закрыть</button></div>"
  );

}]);
