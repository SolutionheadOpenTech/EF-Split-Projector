using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;
using NUnit.Framework;
using Solutionhead.TestFoundations.Utilities;
using Tests.Helpers;

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
            var source = GetSourceObject();
            var dest = projector.Compile().Invoke(source);

            Expression<Func<Dest, string>> expression = d => d.OtherNestedDest.Dest.DestString;
            var sourceEquivalent = (Expression<Func<Source, string>>)TranslateExpressionVisitor.TranslateFromProjector(expression, projector);

            Assert.AreEqual(expression.Compile().Invoke(dest), sourceEquivalent.Compile().Invoke(source));
        }

        [Test]
        public void Returns_expected_converted_expression_referencing_items()
        {
            var projector = SourceToDestWithItems();
            var source = GetSourceObject();
            var dest = projector.Compile().Invoke(source);

            Expression<Func<Dest, IEnumerable<DestItem>>> expression = d => d.Items.Where(i => i.DestInt > 0 && i.DestString != null);
            var sourceEquivalent = (Expression<Func<Source, IEnumerable<DestItem>>>)TranslateExpressionVisitor.TranslateFromProjector(expression, projector);

            Console.WriteLine(expression.ToString());
            Console.WriteLine(sourceEquivalent.ToString());

            Assert.IsTrue(EquivalentHelper.AreEquivalent(expression.Compile().Invoke(dest).Select(i => i.DestInt).ToList(), sourceEquivalent.Compile().Invoke(source).Select(i => i.DestInt).ToList()));
        }

        [Test]
        public void Returns_expected_converted_expression_referencing_items_projected_with_SelectMany()
        {
            var projector = SourceToDestWithItems();
            var source = GetSourceObject();
            var dest = projector.Compile().Invoke(source);

            Expression<Func<Dest, bool>> expression = d => d.ItemItems.Any(m => m.DestItemItemInt > 0);
            var sourceEquivalent = (Expression<Func<Source, bool>>)TranslateExpressionVisitor.TranslateFromProjector(expression, projector);

            Console.WriteLine(expression.ToString());
            Console.WriteLine(sourceEquivalent.ToString());

            Assert.AreEqual(expression.Compile().Invoke(dest), sourceEquivalent.Compile().Invoke(source));
        }

        private static Source GetSourceObject()
        {
            var objectInstantiator = new ObjectInstantiator { Fixture = { RepeatCount = 3 } };
            return objectInstantiator.InstantiateObject<Source>(s => s.OtherNestedSource.Source = s, s => s.Items = s.Items.ToList());
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