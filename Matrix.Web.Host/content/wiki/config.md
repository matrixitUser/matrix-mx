Конфигурация
============
Модули системы настраиваются через конфигурационные файлы, в формате XML.
Настройка сервера API
---------------------
### Настройки связи с СУБД ###
для настройки связи с СУБД Neo4j используется параметр neo4j-url,
настройка соединения с Redis - redis-host и redis-port.

	<appSettings>
	    <add key="binding" value="http://*:666" />
	    <add key="root-folder" value="../../content" />    	    
	    <add key="neo4j-url" value="http://neo4j:pass@172.16.0.36:7474/db/data" />    
	    <add key="redis-host" value="172.16.0.36" />
	    <add key="redis-port" value="6379" />
	</appSettings> 

настройка соединения с MS SQL Server находится в секции connectionStrings

	<connectionStrings>
    	<add name="Context" connectionString="data source=172.16.0.34;initial catalog=dev;user id=matrix;password=matrix" providerName="System.Data.SqlClient" />
 	</connectionStrings> 

### Настройка правил сохранения архивов ###

	<save-rules>
	    <rules>
	      <rule name="часы" format="DataRecordHour{0:MMyyyy}">
	        <!--на какие типы распространяется-->
	        <types>
	          <add name="Hour" />
	        </types>
	        <!--поля по которым создаются секции -->
	        <format-fields>
	          <add name="Date" />
	        </format-fields>
	        <!--уникальные поля, индексируются и перезаписываюся при повторах-->
	        <unique-fields>
	          <add name="Type" />
	          <add name="Date" />
	          <add name="ObjectId" />
	          <add name="S1" />
	        </unique-fields>
	      </rule>
	      <rule name="логи" format="DataRecordLog{0:MMyyyy}">
	        <types>
	          <add name="LogMessage" />
	        </types>
	        <format-fields>
	          <add name="Date" />
	        </format-fields>
	        <unique-fields>
	          <add name="Type" />
	          <add name="Date" />
	          <add name="ObjectId" />
	          <add name="S1" />
	        </unique-fields>
	      </rule>
	  </rules>
	</save-rules>