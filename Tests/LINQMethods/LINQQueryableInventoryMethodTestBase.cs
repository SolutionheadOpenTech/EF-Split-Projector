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
    public abstract class LINQMethodTestBase<TSource, TProjectResult> : IntegratedTestsBase
    {
        protected virtual int TestRecords { get { return 3; } }

        protected abstract void Process(IQueryable<TSource> source, Expression<Func<TSource, TProjectResult>> select);

        protected abstract IQueryable<TSource> Source { get; }

        protected abstract Expression<Func<TSource, TProjectResult>> Projector { get; }
            
        [Test]
        public void SplitResultsAreAsExpected()
        {
            for(var i = 0; i < TestRecords; ++i )
            {
                TestHelper.CreateObjectGraphAndInsertIntoDatabase<Inventory>();
            }

            Process(Source, Projector);
        }
    }

    public abstract class LINQSingularMethodTestBase<TResult> : LINQMethodTestBase<Inventory, IntegratedTestsBase.InventorySelect>
    {
        protected abstract TResult GetResult(IQueryable<InventorySelect> source);

        protected override IQueryable<Inventory> Source { get { return TestHelper.Context.Inventory; } }

        protected override Expression<Func<Inventory, InventorySelect>> Projector { get { return SelectInventory() ; } }

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

    public abstract class LINQQueryableMethodTestBase<TSource, TSelect, TReturn> : LINQMethodTestBase<TSource, TSelect>
    {
        protected abstract IQueryable<TReturn> GetQuery(IQueryable<TSelect> source);

        protected sealed override void Process(IQueryable<TSource> source, Expression<Func<TSource, TSelect>> select)
        {
            List<TReturn> expectedResults = null;
            try
            {
                var query = GetQuery(source.Select(@select));
                Console.WriteLine("Regular Query:");
                Console.WriteLine(query.ToString());
                Console.WriteLine();

                expectedResults = query.ToList();
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

            var splitQuery = GetQuery(source.AutoSplitSelect(@select));
            Console.WriteLine(((SplitQueryableBase)splitQuery).CommandString);
            var splitResults = splitQuery.ToList();

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expectedResults, splitResults));
        }
    }

    public abstract class LINQQueryableInventoryMethodTestBase<TResult> : LINQQueryableMethodTestBase<Inventory, IntegratedTestsBase.InventorySelect, TResult>
    {
        protected override IQueryable<Inventory> Source { get { return TestHelper.Context.Inventory; } }

        protected override Expression<Func<Inventory, InventorySelect>> Projector { get { return SelectInventory(); } }
    }
}