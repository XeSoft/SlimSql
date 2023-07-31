namespace SlimSql.Postgres

[<AutoOpen>]
module Types =

    open Npgsql
    open System.Threading

    [<NoComparison; NoEquality; Struct>]
    type SqlStatement =
        {
            Text : string
            Params : NpgsqlParameter list
        }

    [<NoComparison; NoEquality; Struct>]
    type SqlOperation =
        {
            Statements : SqlStatement list
        }

    [<RequireQualifiedAccess; Struct>]
    type RunOpt =
        /// Prepare statements before running them.
        | Prepare

    type SqlConfig =
        {
            ConnectString: string
            Cancel: CancellationToken
            Transaction: NpgsqlTransaction option
            RunOpts: RunOpt list
        }
