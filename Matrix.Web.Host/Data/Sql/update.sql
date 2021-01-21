if not exists(select * from RowsCache where id=@id)
	insert into RowsCache([id],[state],[description],[name],[city],[phone],[imei])
	values(@id,@state,@description,@name,@city,@phone,@imei)
else
	update RowsCache set [name]=isnull(@name,name),[state]=isnull(@state,[state]),[description]=isnull(@description,[description]),
	[city]=isnull(@city,city),[phone]=isnull(@phone,phone),[imei]=isnull(@imei,imei)
	where id=@id