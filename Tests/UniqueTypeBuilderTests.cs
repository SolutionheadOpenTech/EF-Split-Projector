using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EF_Split_Projector.Helpers;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class UniqueTypeBuilderTests
    {
        [Test]
        public void Returns_same_type_for_given_members_list()
        {
            var members = new Dictionary<string, Type>
                {
                    { "someint", typeof(int) },
                    { "someOtherint", typeof(int) },
                    { "someString", typeof(int) },
                };
            var members2 = new Dictionary<string, Type>
                {
                    { "someString", typeof(int) },
                    { "someOtherint", typeof(int) },
                    { "someint", typeof(int) },
                };

            var expected = UniqueTypeBuilder.GetUniqueType(members, null);
            var result = UniqueTypeBuilder.GetUniqueType(members2, null);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Does_not_produce_exception_when_running_on_multiple_threads()
        {
            var tasks = new List<Task>();
            for(var i = 0; i < 1000; ++i)
            {
                tasks.Add(new Task(ConstructType));
            }
            tasks.ForEach(t => t.Start());

            try
            {
                tasks.ForEach(t => t.Wait());
            }
            catch(AggregateException ex)
            {
                throw ex.InnerExceptions[0];
            }
        }

        private static void ConstructType()
        {
            UniqueTypeBuilder.GetUniqueType(new Dictionary<string, Type>
                {
                    {"someint", typeof(int)},
                    {"someOtherint", typeof(int)},
                    {"someString", typeof(int)}
                }, null);
        }
    }
}