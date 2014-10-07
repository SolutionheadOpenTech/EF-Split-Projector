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
    }
}