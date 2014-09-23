using Solutionhead.TestFoundations;

namespace Tests.TestContext
{
    public class TestHelper : DbContextIntegrationTestHelper<TestDatabase>
    {
        public override TestDatabase ResetContext()
        {
            DisposeOfContext();
            Context = new TestDatabase();
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