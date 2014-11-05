using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class ExpressionMergeTests
    {
        private const string stringReplaceAddition = "Something";
        private const int intReplaceAddition = 10;

        [Test]
        public void Simple_case()
        {
            //Arrange
            const string expectedString0 = "String0";
            const string expectedString1 = "String1";
            const int expectedInt0 = 23;
            const int expectedInt1 = 42;

            var data = new List<TestDataModelChild>
                {
                    new TestDataModelChild
                        {
                            StringSource = expectedString0,
                            IntSource = expectedInt0
                        },
                    new TestDataModelChild
                        {
                            StringSource = expectedString1,
                            IntSource = expectedInt1
                        }
                };

            var projectString = ProjectString();
            var projectInt = ProjectInt();

            //Act
            var merge = projectString.Merge(projectInt);
            var results = data.AsQueryable().Select(merge).ToList();

            //Assert
            Assert.AreEqual(1, results.Count(r => r.StringResult == expectedString0 && r.IntResult == expectedInt0));
            Assert.AreEqual(1, results.Count(r => r.StringResult == expectedString1 && r.IntResult == expectedInt1));
        }

        [Test]
        public void Replaces_previous_MemberBindings_with_new_ones_if_they_exist()
        {
            //Arrange
            const string string0 = "String0";
            const string string1 = "String1";
            const int int0 = 23;
            const int int1 = 42;

            const string expectedString0 = string0 + stringReplaceAddition;
            const string expectedString1 = string1 + stringReplaceAddition;
            const int expectedInt0 = int0 + intReplaceAddition;
            const int expectedInt1 = int1 + intReplaceAddition;

            var data = new List<TestDataModelChild>
                {
                    new TestDataModelChild
                        {
                            StringSource = string0,
                            IntSource = int0
                        },
                    new TestDataModelChild
                        {
                            StringSource = string1,
                            IntSource = int1
                        }
                };

            var projectString = ProjectString();
            var projectInt = ProjectInt();

            //Act
            var merge = projectString.Merge(projectInt);
            merge = merge.Merge(ProjectStringReplace());
            merge = merge.Merge(ProjectIntReplace());
            var results = data.AsQueryable().Select(merge).ToList();

            //Assert
            Assert.AreEqual(1, results.Count(r => r.StringResult == expectedString0 && r.IntResult == expectedInt0));
            Assert.AreEqual(1, results.Count(r => r.StringResult == expectedString1 && r.IntResult == expectedInt1));
        }


        [Test]
        public void Nested_case()
        {
            //Arrange
            const string parentString = "This is the parent string.";
            const string childString = "Child string";
            const int childInt = 42;

            var data = new List<TestDataModelParent>
                {
                    new TestDataModelParent
                        {
                            ParentString = parentString,
                            TestDataModelChild = new TestDataModelChild
                                {
                                    StringSource = childString,
                                    IntSource = childInt
                                }
                        }
                };

            var projectParentString = ParentWithString();
            var projectParentChild = ParentWithChild();

            //Act
            var merge = projectParentString.Merge(projectParentChild);
            var results = data.AsQueryable().Select(merge).ToList();

            //Assert
            var result = results.Single();
            Assert.AreEqual(parentString, result.ParentStringResult);
            Assert.AreEqual(childString, result.TestResultModelChild.StringResult);
            Assert.AreEqual(childInt, result.TestResultModelChild.IntResult);
        }

        private Expression<Func<TestDataModelChild, TestResultModelChild>> ProjectString()
        {
            return d => new TestResultModelChild
            {
                StringResult = d.StringSource
            };
        }

        private Expression<Func<TestDataModelChild, TestResultModelChild>> ProjectInt()
        {
            return d => new TestResultModelChild
            {
                IntResult = d.IntSource
            };
        }

        private Expression<Func<TestDataModelChild, TestResultModelChild>> ProjectStringReplace()
        {
            return d => new TestResultModelChild
            {
                StringResult = d.StringSource + stringReplaceAddition
            };
        }

        private Expression<Func<TestDataModelChild, TestResultModelChild>> ProjectIntReplace()
        {
            return d => new TestResultModelChild
            {
                IntResult = d.IntSource + intReplaceAddition
            };
        }

        private Expression<Func<TestDataModelParent, TestResultModelParent>> ParentWithChild()
        {
            return d => new TestResultModelParent
            {
                TestResultModelChild = new TestResultModelChild
                {
                    StringResult = d.TestDataModelChild.StringSource,
                    IntResult = d.TestDataModelChild.IntSource
                }
            };
        }

        private Expression<Func<TestDataModelParent, TestResultModelParent>> ParentWithString()
        {
            return d => new TestResultModelParent
            {
                ParentStringResult = d.ParentString
            };
        }

        #region Test Classes

        private class TestDataModelChild
        {
            public string StringSource { get; set; }

            public int IntSource { get; set; }
        }

        private class TestDataModelParent
        {
            public string ParentString { get; set; }

            public TestDataModelChild TestDataModelChild { get; set; }
        }

        private class TestResultModelChild
        {
            public string StringResult { get; set; }

            public int IntResult { get; set; }
        }

        private class TestResultModelParent
        {
            public string ParentStringResult { get; set; }

            public TestResultModelChild TestResultModelChild { get; set; }
        }

        #endregion
    }
}
