using System.Collections.Generic;
using EF_Split_Projector.Helpers;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class ObjectMergerTests
    {
        public class DestRootObject
        {
            public string StringField;
            public DestChildObject DestChild;

            public List<int> Integers;
            public List<DestChildObject> DestChildren;

            public List<List<DestChildObject>> NestedChildren;
        }

        public class DestChildObject
        {
            public int IntegerField;
            public string StringField;
        }

        public class SourceObject
        {
            public string SourceString;
            public int SourceChildInteger;
            public string SourceChildString;
        }

        [Test]
        public void MergesNestedComplexObject()
        {
            var dest0 = new DestRootObject
                {
                    DestChild = new DestChildObject
                        {
                            IntegerField = 42
                        }
                };
            var dest1 = new DestRootObject
                {
                    StringField = "ExpectedString",
                    DestChild = new DestChildObject
                        {
                            StringField = "ExpectedChildString"
                        }
                };

            var merger = ObjectMerger.CreateMerger<SourceObject, DestRootObject>(s => new DestRootObject
                {
                    StringField = s.SourceString,

                    DestChild = new DestChildObject
                        {
                            StringField = s.SourceChildString
                        }
                });

            var result = (DestRootObject)merger.Merge(dest0, dest1);

            Assert.AreEqual("ExpectedString", result.StringField);
            Assert.AreEqual(42, result.DestChild.IntegerField);
            Assert.AreEqual("ExpectedChildString", result.DestChild.StringField);
        }

        [Test]
        public void MergesNestedListOfPrimitiveObjects()
        {
            var dest0 = new DestRootObject { };
            var dest1 = new DestRootObject
                {
                    Integers = new List<int> { 0, 1, 2, 3, 4 }
                };

            var merger = ObjectMerger.CreateMerger<SourceObject, DestRootObject>(s => new DestRootObject
                {
                    Integers = new List<int> { }
                });

            var result = (DestRootObject)merger.Merge(dest0, dest1);

            for(var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, result.Integers[i]);
            }
        }

        [Test]
        public void MergesNestedListOfComplexObjects()
        {
            var dest0 = new DestRootObject
                {
                    DestChildren = new List<DestChildObject>
                        {
                            new DestChildObject { IntegerField = 42 },
                            new DestChildObject { IntegerField = 84 }
                        }
                };

            var dest1 = new DestRootObject
                {
                    DestChildren = new List<DestChildObject>
                        {
                            new DestChildObject { StringField = "FortyTwo" },
                            new DestChildObject { StringField = "EightyFour" }
                        }
                };

            var merger = ObjectMerger.CreateMerger<SourceObject, DestRootObject>(s => new DestRootObject
                {
                    DestChildren = new List<DestChildObject>
                        {
                            new DestChildObject { StringField = s.SourceChildString }
                        }
                });

            var result = (DestRootObject)merger.Merge(dest0, dest1);

            Assert.AreEqual(42, result.DestChildren[0].IntegerField);
            Assert.AreEqual(84, result.DestChildren[1].IntegerField);
            Assert.AreEqual("FortyTwo", result.DestChildren[0].StringField);
            Assert.AreEqual("EightyFour", result.DestChildren[1].StringField);
        }

        [Test]
        public void MergesNestedListsOfListOfComplexObjects()
        {
            var dest0 = new DestRootObject
                {
                    NestedChildren = new List<List<DestChildObject>>
                        {
                            new List<DestChildObject> { new DestChildObject { IntegerField = 42 } },
                            new List<DestChildObject> { new DestChildObject { IntegerField =  84 } }
                        }
                };

            var dest1 = new DestRootObject
                {
                    NestedChildren = new List<List<DestChildObject>>
                        {
                            new List<DestChildObject> { new DestChildObject { StringField = "FortyTwo" } },
                            new List<DestChildObject> { new DestChildObject { StringField =  "EightyFour" } }
                        }
                };

            var merger = ObjectMerger.CreateMerger<SourceObject, DestRootObject>(s => new DestRootObject
                {
                    NestedChildren = new List<List<DestChildObject>>
                        {
                            new List<DestChildObject> { new DestChildObject { StringField = "string" } }
                        }
                });

            var result = (DestRootObject)merger.Merge(dest0, dest1);

            Assert.AreEqual(42, result.NestedChildren[0][0].IntegerField);
            Assert.AreEqual(84, result.NestedChildren[1][0].IntegerField);
            Assert.AreEqual("FortyTwo", result.NestedChildren[0][0].StringField);
            Assert.AreEqual("EightyFour", result.NestedChildren[1][0].StringField);
        }
    }
}