using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector;
using NUnit.Framework;
using Tests.Helpers;
using Tests.TestContext.DataModels;

namespace Tests.LINQMethods
{
    public abstract class LINQMethodTestBase : IntegratedTestsBase
    {
        protected virtual int TestRecords { get { return 3; } }

        protected abstract void Process(IQueryable<Inventory> source, Expression<Func<Inventory, InventorySelect>> select);

        [Test]
        public void SplitResultsAreAsExpected()
        {
            for(var i = 0; i < TestRecords; ++i )
            {
                TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            }

            Process(TestHelper.Context.Inventory, SelectInventory());
        }
    }

    public abstract class LINQSingularMethodTestBase<TResult> : LINQMethodTestBase
    {
        protected abstract TResult GetResult(IQueryable<InventorySelect> source);

        protected sealed override void Process(IQueryable<Inventory> source, Expression<Func<Inventory, InventorySelect>> select)
        {
            var expectedResult = default(TResult);
            try
            {
                expectedResult = GetResult(source.Select(select));
            }
            catch(Exception ex)
            {
                if(ex is NotSupportedException)
                {
                    Assert.Pass("Method not supported in LINQ to Entities: {0}", ex.Message);
                }
                else
                {
                    Assert.Fail("Unexpected exception in LINQ to Entities - test setup might be incorrect. Exception: {0}", ex.Message);
                }
            }

            var splitQuery = source.AutoSplitSelect(select);
            var splitResult = GetResult(splitQuery);

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expectedResult, splitResult));
        }
    }

    public abstract class LINQQueryableMethodTestBase<TResult> : LINQMethodTestBase
    {
        protected abstract IQueryable<TResult> GetQuery(IQueryable<InventorySelect> source);

        protected sealed override void Process(IQueryable<Inventory> source, Expression<Func<Inventory, InventorySelect>> select)
        {
            List<TResult> expectedResults = null;
            try
            {
                expectedResults = GetQuery(source.Select(@select)).ToList();
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

            var splitResults = GetQuery(source.AutoSplitSelect(@select)).ToList();

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expectedResults, splitResults));
        }
    }
}