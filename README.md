[![Build status](https://ci.appveyor.com/api/projects/status/ax78f01y66q39jla)](https://ci.appveyor.com/project/vinneyk/ef-split-projector)
EF-Split-Projector
==================

EF Split Projector is an optimization utility for improving the performance of complex object graph selection with Entity Framework. The promise of an ORM such as Entity Framework is the abstraction of the database. Since the advent of LINQ to C#, hordes of developers have found more confort in this abstraction layer as opposed to the frail parameterized query and ADO Command of yore. For developers working with complex object models, however, it doesn't take long before you find that the abstraction comes with a price: ten's of thousands of lines generated complex T-SQL that can kill performance.

EF Split Projector puts an end to the compromise between projection and performance by spliting complex query operations into multiple chunks of SQL, which is more managable to the underlying database. To give an idea of the performance improvements we've seen, using plain EF we were ending up with some queries with >20k lines which, would take 20+ seconds to execute even when no results were returned--2+ minutes to return 100 records! These queries, when using the EF Split Projector utility can now execute, returning 100 records, in <1 second. 

(Specific examples and benchmarks to come.)
