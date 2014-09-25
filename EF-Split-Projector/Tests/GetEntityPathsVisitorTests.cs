using System;
using System.Linq;
using EF_Split_Projector.Helpers;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class GetEntityPathsVisitorTests : IntegratedTestsBase
    {
        [Test]
        public void Test()
        {
            var originalQuery = TestHelper.Context.Inventory.Select(SelectInventory());
            var paths = GetEntityPathsVisitor.GetDistinctEntityPaths(originalQuery.GetObjectContext(), originalQuery.Expression);
            foreach(var path in paths)
            {
                Console.WriteLine("[RootNode]");
                DisplayPaths(path);
            }
        }

        private void DisplayPaths(GetEntityPathsVisitor.EntityPathNode path)
        {
            Console.WriteLine(path);
            foreach(var child in path.Paths)
            {
                DisplayPaths(child);
            }
        }
    }
}