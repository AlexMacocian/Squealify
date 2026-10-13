; Unshipped analyzer release
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SQF001 | Squealify | Error | Table has no primary key
SQF002 | Squealify | Error | Invalid [AutoIncrement]
SQF003 | Squealify | Error | Unknown column
SQF004 | Squealify | Error | Foreign key column count mismatch
SQF010 | Squealify | Error | Invalid [SqlQuery] method
SQF011 | Squealify | Error | Invalid [SqlQuery] container
SQF012 | Squealify | Error | [SqlQuery] class does not derive from SqlQueryContext
SQF013 | Squealify | Error | Unsupported [SqlQuery] parameter
SQF014 | Squealify | Error | Unknown SQL parameter
SQF015 | Squealify | Error | Unused [SqlQuery] parameter
SQF016 | Squealify | Error | Unsupported [SqlQuery] return type
