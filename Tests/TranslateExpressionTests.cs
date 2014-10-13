using System;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TranslateExpressionTests
    {
        public class Source
        {
            public string SourceString { get; set; }
            public NestedSource NestedSource { get; set; }
            public NestedSource OtherNestedSource { get; set; }
        }

        public class NestedSource
        {
            public int SourceInt { get; set; }
            public Source Source { get; set; }
        }

        public class Dest
        {
            public string DestString { get; set; }
            public NestedDest NestedDest { get; set; }
            public NestedDest OtherNestedDest { get; set; }
        }

        public class NestedDest
        {
            public int DestInt { get; set; }
            public Dest Dest { get; set; }
        }

        [Test]
        public void Returns_expected_converted_expression()
        {
            var sourceToDest = SourceToDest();

            Expression<Func<Dest, object>> expression = d => d.OtherNestedDest.Dest.DestString;
            var sourceEquivalent = TranslateExpressionVisitor.TranslateFromProjectors(expression, sourceToDest);

            Assert.AreEqual("s => s.OtherNestedSource.Source.SourceString", sourceEquivalent.ToString());
        }

        private static Expression<Func<Source, Dest>> SourceToDest()
        {
            return s => new Dest
                {
                    DestString = s.SourceString,
                    OtherNestedDest = new NestedDest
                        {
                            DestInt = s.OtherNestedSource.SourceInt,
                            Dest = new Dest
                                {
                                    DestString = s.OtherNestedSource.Source.SourceString
                                }
                        },
                    NestedDest = new NestedDest
                        {
                            DestInt = s.NestedSource.SourceInt
                        },
                };
        }
    }
}