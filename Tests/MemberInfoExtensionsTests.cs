using System.Linq;
using NUnit.Framework;
using EF_Split_Projector.Helpers.Extensions;

namespace Tests
{
    [TestFixture]
    public class MemberInfoExtensionsTests
    {
        public interface ITest
        {
            string StringProperty { get; }
            int IntProperty { get; }
        }

        public interface ITest<T>
        {
            T GenericProperty { get; }
        }

        public class Test : ITest, ITest<int>
        {
            public string StringProperty { get; private set; }
            public int IntProperty { get; private set; }
            public int GenericProperty { get; private set; }
        }

        [Test]
        public void Returns_true_if_MemberInfo_is_a_property_implementing_an_interface_MemberInfo()
        {
            var implementation = typeof(Test).GetMember("StringProperty").Single();
            var interfaceProperty = typeof(ITest).GetMember("StringProperty").Single();
            Assert.IsTrue(implementation.IsOrImplements(interfaceProperty));
        }

        [Test]
        public void Returns_false_if_MemberInfo_is_a_property_that_does_not_implement_interface_MemberInfo()
        {
            var implementation = typeof(Test).GetMember("StringProperty").Single();
            var interfaceProperty = typeof(ITest).GetMember("IntProperty").Single();
            Assert.IsFalse(implementation.IsOrImplements(interfaceProperty));
        }

        [Test]
        public void Returns_true_if_MemberInfo_is_a_property_which_implements_generic_MemberInfo()
        {
            var implementation = typeof(Test).GetMember("GenericProperty").Single();
            var interfaceProperty = typeof(ITest<int>).GetMember("GenericProperty").Single();
            Assert.IsTrue(implementation.IsOrImplements(interfaceProperty));
        }

        [Test]
        public void Returns_true_if_MemberInfo_is_a_property_which_implements_generic_definition_MemberInfo()
        {
            var implementation = typeof(Test).GetMember("GenericProperty").Single();
            var interfaceProperty = typeof(ITest<>).GetMember("GenericProperty").Single();
            Assert.IsTrue(implementation.IsOrImplements(interfaceProperty));
        }
    }
}