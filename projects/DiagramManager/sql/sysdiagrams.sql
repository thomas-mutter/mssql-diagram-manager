create table [dbo].[sysdiagrams](
	[name] [sysname] not null,
	[principal_id] [int] not null,
	[diagram_id] [int] identity(1,1) not null,
	[version] [int] null,
	[definition] [varbinary](max) null,
primary key clustered 
(
	[diagram_id] asc
)with (pad_index = off, statistics_norecompute = off, ignore_dup_key = off, allow_row_locks = on, allow_page_locks = on, optimize_for_sequential_key = off) on [PRIMARY],
 constraint [UK_principal_name] unique nonclustered 
(
	[principal_id] asc,
	[name] asc
)with (pad_index = off, statistics_norecompute = off, ignore_dup_key = off, allow_row_locks = on, allow_page_locks = on, optimize_for_sequential_key = off) on [PRIMARY]
) on [PRIMARY] textimage_on [PRIMARY]

exec sys.sp_addextendedproperty @name=N'microsoft_database_tools_support', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'sysdiagrams'
