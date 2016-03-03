using System;
using System.Collections.Generic;
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
    }
}