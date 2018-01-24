namespace SlimSql.MsSql

module Sql =

    open System.Data.SqlClient
    open Dapper


    let query<'T> sqlConfig sqlQuery =
        async {
            use connection = new SqlConnection(sqlConfig.ConnectString)
            do! connection.OpenAsync () |> Async.AwaitTask
            let! result =
                connection.QueryAsync<'T>
                    ( sqlQuery.Query
                    , dict sqlQuery.Parameters
                    , commandTimeout = Option.toNullable sqlConfig.CommandTimeout
                    )
                    |> Async.AwaitTask
            return Array.ofSeq result
        }


    let writeBatch sqlConfig sqlQueries =
        async {
            use connection = new SqlConnection(sqlConfig.ConnectString)
            do! connection.OpenAsync () |> Async.AwaitTask
            use transaction = connection.BeginTransaction ()
            for sqlQuery in sqlQueries do
                do!
                    connection.ExecuteAsync
                        ( sqlQuery.Query
                        , dict sqlQuery.Parameters
                        , transaction
                        , commandTimeout = Option.toNullable sqlConfig.CommandTimeout
                        )
                        |> Async.AwaitTask
                        |> Async.Ignore
            transaction.Commit ()
        }


    let private initResult =
        Startup.registerOptionTypes ()
            
