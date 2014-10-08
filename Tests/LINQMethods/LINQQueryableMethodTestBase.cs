using System;
using System.Collections.Generic;
using System.Linq;
using EF_Split_Projector;
using NUnit.Framework;
using Tests.Helpers;
using Tests.TestContext.DataModels;

namespace Tests.LINQMethods
{
    public abstract class LINQMethodTestBase : IntegratedTestsBase
    {
        protected abstract void Process(IQueryable<InventorySelect> source);

        [Test]
        public void SplitResultsAreAsExpected()
        {
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

            var sourceQuery = TestHelper.Context.Inventory.Select(SelectInventory());

            Process(sourceQuery);
        }
    }

    public abstract class LINQSingularMethodTestBase<TResult> : LINQMethodTestBase
    {
        protected abstract TResult GetResult(IQueryable<InventorySelect> source);

        protected sealed override void Process(IQueryable<InventorySelect> source)
        {
            var expectedResult = default(TResult);
            try
            {
                expectedResult = GetResult(source);
            }
            catch(Exception ex)
            {
                if(ex is NotSupportedException)
                {
                    Assert.Pass("Method not supported in LINQ to Entities: {0}", ex.Message);
                }
                else
                {
                    throw;
                }
            }

            var splitQuery = source.AsSplitQueryable();
            var splitResult = GetResult(splitQuery);

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expectedResult, splitResult));
        }
    }

    public abstract class LINQQueryableMethodTestBase<TResult> : LINQMethodTestBase
    {
        protected abstract IQueryable<TResult> GetQuery(IQueryable<InventorySelect> source);

        protected sealed override void Process(IQueryable<InventorySelect> source)
        {
            var expected = GetQuery(source);
            List<TResult> expectedResults = null;
            try
            {
                expectedResults = expected.ToList();
            }
            catch(Exception ex)
            {
                if(ex is NotSupportedException)
                {
                    Assert.Pass("Method not supported in LINQ to Entities: {0}", ex.Message);
                }
                else
                {
                    throw;
                }
            }

            var splitQuery = expected.AsSplitQueryable();
            var splitResults = splitQuery.ToList();

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expectedResults, splitResults));
        }
    }
}