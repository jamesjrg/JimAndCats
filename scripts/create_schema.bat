echo starting schema creation
sqlcmd -S "(LocalDB)\v11.0" -d JimAndCats -i create_schema.sql
echo schema creation finished
