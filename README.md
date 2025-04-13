download the posgres sql and pgadmin
create the migration using following command 
Note -: if you see initial migrations already in the migration folder then do not create skip #1 and run #2 command.
        now if you are creating you own database entities then only you need to run both #1 and #2 command
  1.  add-migration InitialMigrationIdentity -context CoilIdentityDbContext
  2.  update-database -context CoilIdentityDbContext
  3.  add-migration InitialMigrationApplication -context CoilApplicationDbContext
  4.  update-database -context CoilApplicationDbContext

above commands lets you create identity and application migrations and create the database.

Now run you application and swaggare will show you application and identity api endpoint
![image](https://github.com/user-attachments/assets/0f0d629d-0d70-4207-81e3-0cb0e5a94df3)
