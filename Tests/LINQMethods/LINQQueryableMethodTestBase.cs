using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        protected abstract void Process(IQueryable<Inventory> source, Expression<Func<Inventory, InventorySelect>> select);

        [Test]
        public void SplitResultsAreAsExpected()
        {
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();

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
                    throw;
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
            var expected = GetQuery(source.Select(select));
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

            var splitQuery = source.AutoSplitSelect(select);
            var splitResults = splitQuery.ToList();

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expectedResults, splitResults));
        }
    }
}