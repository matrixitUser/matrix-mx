if object_id('RowsCache','U') is null 
create table RowsCache( 
	[id] uniqueidentifier not null primary key,
	[state] int,
	[description] nvarchar(255),
	[name] nvarchar(255),
	[city] nvarchar(255),
	[phone] nvarchar(255),
	[imei] nvarchar(255)
)
