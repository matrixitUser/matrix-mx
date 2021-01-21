create table "tmp12"(id uuid);
insert into "tmp12"(id)values {0};
select t.id from "RowsCache" r right join "tmp12" t on r.id=t.id where r.id is null;
drop table "tmp12";