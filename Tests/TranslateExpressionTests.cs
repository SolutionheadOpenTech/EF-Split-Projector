using System;
using System.Collections.Generic;
using System.Linq;
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
            public IEnumerable<SourceItem> Items { get; set; }
        }

        public class NestedSource
        {
            public int SourceInt { get; set; }
            public Source Source { get; set; }
        }

        public class SourceItem
        {
            public int ItemInt { get; set; }
            public string ItemString { get; set; }
            public IEnumerable<SourceItemItem> Items { get; set; }
        }

        public class SourceItemItem
        {
            public int SourceItemItemInt { get; set; }
        }

        public class Dest
        {
            public string DestString { get; set; }
            public NestedDest NestedDest { get; set; }
            public NestedDest OtherNestedDest { get; set; }
            public IEnumerable<DestItem> Items { get; set; }
            public IEnumerable<DestItemItem> ItemItems { get; set; }
        }

        public class NestedDest
        {
            public int DestInt { get; set; }
            public Dest Dest { get; set; }
        }

        public class DestItem
        {
            public int DestInt { get; set; }
            public string DestString { get; set; }
        }

        public class DestItemItem
        {
            public int DestItemItemInt { get; set; }
        }

        [Test]
        public void Returns_expected_converted_expression()
        {
            var projector = SourceToDest();

            Expression<Func<Dest, object>> expression = d => d.OtherNestedDest.Dest.DestString;
            var sourceEquivalent = TranslateExpressionVisitor.TranslateFromProjectors(expression, projector);

            Assert.AreEqual("s => s.OtherNestedSource.Source.SourceString", sourceEquivalent.ToString());
        }

        //[Test]
        //public void Returns_expected_converted_expression_referencing_items()
        //{
        //    var projector = SourceToDestWithItems();

        //    Expression<Func<Dest, object>> expression = d => d.Items.Where(i => i.DestInt > 0);
        //    var sourceEquivalent = TranslateExpressionVisitor.TranslateFromProjectors(expression, projector);

        //    Console.WriteLine(expression.ToString());
        //    Console.WriteLine(sourceEquivalent.ToString());

        //    try
        //    {
        //        Assert.AreEqual("s => s.Items.Where(i => i.ItemInt > 0)", sourceEquivalent.ToString());
        //    }
        //    catch
        //    {
        //        Assert.AreEqual("s => s.Items.Where(i => (i.ItemInt > 0))", sourceEquivalent.ToString());
        //    }
        //}

        //[Test]
        //public void Returns_expected_converted_expression_referencing_items_projected_with_SelectMany()
        //{
        //    var projector = SourceToDestWithItems();

        //    Expression<Func<Dest, object>> expression = d => d.ItemItems.Any(m => m.DestItemItemInt > 0);
        //    var sourceEquivalent = TranslateExpressionVisitor.TranslateFromProjectors(expression, projector);

        //    Console.WriteLine(expression.ToString());
        //    Console.WriteLine(sourceEquivalent.ToString());

        //    try
        //    {
        //        Assert.AreEqual("s => Convert((s.Items.SelectMany(i => i.Items.Select(m => new DestItemItem() {DestItemItemInt = m.SourceItemItemInt})).Count() > 1))", sourceEquivalent.ToString());
        //    }
        //    catch
        //    {
        //        Assert.AreEqual("s => s.Items.Where(i => (i.ItemInt > 0))", sourceEquivalent.ToString());
        //    }
        //}

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

        private static Expression<Func<Source, Dest>> SourceToDestWithItems()
        {
            return s => new Dest
                {
                    Items = s.Items.Select(i => new DestItem
                        {
                            DestInt = i.ItemInt,
                            DestString = i.ItemString
                        }),
                    ItemItems = s.Items.SelectMany(i => i.Items.Select(m => new DestItemItem
                        {
                            DestItemItemInt = m.SourceItemItemInt
                        }))
                };
        }
    }
}