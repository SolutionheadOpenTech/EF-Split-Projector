using Solutionhead.EntityParser;
using Solutionhead.TestFoundations;
using Solutionhead.TestFoundations.Utilities;

namespace Tests.TestContext
{
    public class TestHelper : DbContextIntegrationTestHelper<TestDatabase>
    {
        public override TestDatabase ResetContext()
        {
            DisposeOfContext();
            Context = new TestDatabase();

            var entityParser = new Solutionhead.EntityParser.EntityParser(Context);
            ForeignKeyConstrainer = new EntityObjectGraphForeignKeyConstrainer(entityParser.Entities);

            ObjectInstantiator = new ObjectInstantiator();

            return Context;
        }

        protected override void DropAndRecreateContext()
        {
            if(Context != null && Context.Database.Exists())
            {
                Context.Database.Delete();
            }

            ResetContext();
        }

        public void Reset()
        {
            Context.Database.ExecuteSqlCommand(@"
                EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
                EXEC sp_MSForEachTable 'DELETE FROM ?'
                EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'
                EXEC sp_MSForEachTable @command1 =
                'IF EXISTS (SELECT * from syscolumns where id = Object_ID(''?'') and colstat & 1 = 1)
                BEGIN
                    DBCC CHECKIDENT(''?'', RESEED, 0)
                END'");
            ResetContext();
            ObjectInstantiator = new ObjectInstantiator();
        }
    }
}