
//namespace WebApi.Code;

//public static class DbInitializer
//{
//    public static void Initialize(SqliteDbContext context)
//    {
//        // Make sure the database is created
//        context.Database.EnsureCreated();

//        // Check if there are any data already
//        //if (context.MyEntities.Any())
//        //{
//        //    return; // DB has been seeded
//        //}

//        //var myEntities = new MyEntity[]
//        //{
//        //    new MyEntity { Name = "Entity1" },
//        //    new MyEntity { Name = "Entity2" }
//        //};

//        //foreach (MyEntity e in myEntities)
//        //{
//        //    context.MyEntities.Add(e);
//        //}

//        context.SaveChanges();
//    }
//}