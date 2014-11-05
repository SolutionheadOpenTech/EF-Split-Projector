using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EF_Split_Projector.Helpers.Visitors;

namespace EF_Split_Projector
{
    public static class ProjectorMerge
    {
        /// <summary>
        /// <para>Will merge a projector that results in a base type with a projector that results in a derived type such that</para>
        /// <para>given CustomerResult : PersonResult, then</para>
        /// <para>customer => new PersonResult { Name = customer.Name }</para>
        /// <para>merged with:</para>
        /// <para>customer => new CustomerResult { CustomerId = customer.Id }</para>
        /// <para>will result in:</para>
        /// <para>customer => new CustomerResult { Name = customer.Name, CustomerId = customer.Id }</para>
        /// </summary>
        /// <typeparam name="TSource">The input type for the projectors.</typeparam>
        /// <typeparam name="TDestBase">The return type for the baseProjector.</typeparam>
        /// <typeparam name="TDestDerived">The return type for the derivedProjector. Note that this must either be the same as TDestBase or else derive from it.</typeparam>
        /// <param name="baseProjector">The base projector.</param>
        /// <param name="derivedProjector">The derived projector that will be based on the base projector.</param>
        /// <returns>A new expression tree representing the merged projection between baseProjector and derivedProjector.</returns>
        public static Expression<Func<TSource, TDestDerived>> Merge<TSource, TDestBase, TDestDerived>(this Expression<Func<TSource, TDestBase>> baseProjector, Expression<Func<TSource, TDestDerived>> derivedProjector)
            where TDestBase : new()
            where TDestDerived : TDestBase, new()
        {
            if(baseProjector == null) { throw new ArgumentNullException("baseProjector"); }
            if(derivedProjector == null) { throw new ArgumentNullException("baseProjector"); }

            var mutatedBase = MutateProjectorReturnVisitor.Mutate<TSource, TDestBase, TDestDerived>(baseProjector);
            return new[] { mutatedBase, derivedProjector }.Merge();
            //return MergeProjectorsVisitor.Merge(mutatedBase, derivedProjector);
        }

        /// <summary>
        /// <para>Will merge a projector that results in a base type with a projector that results in a derived type when both projectors have differing but related input types such that</para>
        /// <para>given CustomerResult : PersonResult, then</para>
        /// <para>person => new PersonResult { Name = person.Name }</para>
        /// <para>merged with:</para>
        /// <para>customer => new CustomerResult { CustomerId = customer.Id }</para>
        /// <para>defining:</para>
        /// <para>customer => customer.person</para>
        /// <para>will result in:</para>
        /// <para>customer => new CustomerResult { Name = customer.person.Name, CustomerId = customer.Id }</para>
        /// </summary>
        /// <typeparam name="TBaseSource">The input type for the base projector.</typeparam>
        /// <typeparam name="TDerivedSource">The input type for the derived projector. Note that TNewSource does not have to derive from TOldSource, only be able to somehow result in it.</typeparam>
        /// <typeparam name="TBaseResult">The return type for the base projector.</typeparam>
        /// <typeparam name="TDerivedResult">The return type for the derivedProjector. Note that this must either be the same as TDestBase or else derive from it.</typeparam>
        /// <param name="baseProjector">The base projector.</param>
        /// <param name="derivedProjector">The derived projector that will be based on the base projector.</param>
        /// <param name="oldSourceSelector">An expression defining how to get from TNewSource to TOldSource.</param>
        /// <returns>A new expression tree representing the merged projection between baseProjector with a new input type and derivedProjector.</returns>
        public static Expression<Func<TDerivedSource, TDerivedResult>> Merge<TBaseSource, TDerivedSource, TBaseResult, TDerivedResult>(
            this Expression<Func<TBaseSource, TBaseResult>> baseProjector,
            Expression<Func<TDerivedSource, TDerivedResult>> derivedProjector,
            Expression<Func<TDerivedSource, TBaseSource>> oldSourceSelector)
            where TBaseResult : new()
            where TDerivedResult : TBaseResult, new()
        {
            if(baseProjector == null) { throw new ArgumentNullException("baseProjector"); }
            if(derivedProjector == null) { throw new ArgumentNullException("baseProjector"); }
            if(oldSourceSelector == null) { throw new ArgumentNullException("oldSourceSelector"); }

            var mutatedSource = MutateProjectorSingleParameterVisitor.Mutate(baseProjector, oldSourceSelector);
            return mutatedSource.Merge(derivedProjector);
        }

        public static Expression<Func<TSource, TDest>> Merge<TSource, TDest>(this IEnumerable<Expression<Func<TSource, TDest>>> projectors)
        {
            return projectors == null ? null : MergeOnProjectorVisitor.Merge(projectors.ToArray());
        }

        public static Expression<Func<TSource, TDest>> Merge<TSource, TDest>(params Expression<Func<TSource, TDest>>[] projectors)
        {
            return MergeOnProjectorVisitor.Merge(projectors);
        }
    }
}
