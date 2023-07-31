namespace SlimSql.Postgres.AsyncResult

module Sql =

    open Npgsql
    open SlimSql.Postgres.Types
    open SlimSql.Postgres.Sql
    open System.Threading.Tasks

    module Internal =

        let inline tryEx ([<InlineIfLambda>]f) cfg op =
            task {
                try
                    let! value = f cfg op
                    return Ok value
                with ex ->
                    return Error ex
            }
            |> Async.AwaitTask

    open Internal

    module Write =

        let inline exec (sqlConfig: SqlConfig) (op: SqlOperation) =
            tryEx Write.exec sqlConfig op

        let inline execCount (sqlConfig: SqlConfig) (op: SqlOperation)
            : Async<Result<Write.AffectedRowCount, exn>> =
            tryEx Write.execCount sqlConfig op

    module Read =

        let inline array<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            tryEx Read.array<'T> sqlConfig op

        let inline list<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            tryEx Read.list<'T> sqlConfig op

        let inline tryFirst<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            tryEx Read.tryFirst<'T> sqlConfig op

        let inline first<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            tryEx Read.first<'T> sqlConfig op

        let inline scalar<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            tryEx Read.scalar<'T> sqlConfig op

        let inline fold<'state, 'item> (sqlConfig: SqlConfig) (op: SqlOperation)
            (initial: 'state)
            (apply: 'state -> 'item -> 'state) =
            tryEx (fun cfg op_ ->
                Read.fold<'state, 'item> cfg op_ initial apply) sqlConfig op

        let inline foldWhile<'state, 'item> (sqlConfig: SqlConfig) (op: SqlOperation)
            (canContinue: 'state -> bool)
            (initial: 'state)
            (apply: 'state -> 'item -> 'state) =
            tryEx (fun cfg op_ ->
                Read.foldWhile<'state, 'item> cfg op_ canContinue initial apply) sqlConfig op

        /// Use NpgsqlDataReader to process multiple query results.
        /// Some extensions are provided. E.g. NpgsqlDataReader.array
        let inline multi<'T> (sqlConfig: SqlConfig) (op: SqlOperation)
            (readFn: NpgsqlDataReader -> Task<'T>) =
            tryEx (fun cfg op_ ->
                Read.multi<'T> cfg op_ readFn) sqlConfig op

