[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
[assembly: Squealify.SquealifyOptions(Synchronous = true, ContextBase = typeof(Squealify.Tests.Synchronous.CountingContext))]
