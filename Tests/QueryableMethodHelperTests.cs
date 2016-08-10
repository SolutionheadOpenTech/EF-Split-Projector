using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EF_Split_Projector.Helpers;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class QueryableMethodHelperTests
    {
        [Test]
        public void Returns_expected_MethodInfo()
        {
            var expected = GetMethodInfo<TestClass>(q => q.FirstOrDefault());
            var result = QueryableMethodHelper<TestClass>.GetMethod("FirstOrDefault");
            Assert.AreEqual(expected, result);

            expected = GetMethodInfo<TestClass>(q => q.FirstOrDefault(t => t.Integer > 0));
            result = QueryableMethodHelper<TestClass>.GetMethod("FirstOrDefault", typeof(Expression<Func<TestClass, bool>>));
            Assert.AreEqual(expected, result);
        }

        private class TestClass
        {
            public int Integer { get; set; }
        }

        private static MethodInfo GetMethodInfo<TElement>(Expression<Action<IQueryable<TElement>>> method)
        {
            return (method.Body as MethodCallExpression).Method;
        }
    }
}