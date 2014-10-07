// Written by pwelter34 - https://github.com/loresoft/EntityFramework.Extended

using System.Collections.Generic;
using System.Data.Entity.Core.Objects;

namespace EF_Split_Projector.Extended.Future
{
    /// <summary>
    /// A interface encapsulating the execution of future queries.
    /// </summary>
    public interface IFutureRunner
    {
        /// <summary>
        /// Executes the future queries.
        /// </summary>
        /// <param name="context">The <see cref="ObjectContext"/> to run the queries against.</param>
        /// <param name="futureQueries">The future queries list.</param>
        void ExecuteFutureQueries(ObjectContext context, IList<IFutureQuery> futureQueries);
    }
}