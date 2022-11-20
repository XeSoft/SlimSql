namespace SlimSql.Postgres

module Sql =

    open Npgsql
    open Dapper
    open SlimSql.Postgres.Integration.Dapper


    // Why?
    // 1. Dapper type handlers are global, requiring global one-time configuration.
    // 2. Module `do` will not run the Dapper initialization for some reason.
    // 3. Requiring user-facing initialization for this is not desirable.
    module private ThreadSafe =
        // double-check lock pattern
        let mutable private __uninitialized = true
        let private __locker = obj()
        // after slower initial call, fast path is used
        let initialize () =
            if __uninitialized then // fast path: no lock, maybe stale value
                lock __locker (fun () -> // synchronize thread access
                    if __uninitialized then // check up-to-date value
                        Init.addOptionTypes ()
                        __uninitialized <- false
                )


    let read<'T> (config: SqlConfig) op =
        ThreadSafe.initialize ()
        task {
            try
                let cmd = CommandDefinition.fromSqlOperation config op
                use connection = new NpgsqlConnection(config.ConnectString)
                let! items = connection.QueryAsync<'T>(cmd)
                return Ok (List.ofSeq items)
            with ex ->
                return Error ex
        }
        |> Async.AwaitTask


    let readFirst<'T> (config: SqlConfig) op =
        ThreadSafe.initialize ()
        task {
            try
                let cmd = CommandDefinition.fromSqlOperation config op
                use connection = new NpgsqlConnection(config.ConnectString)
                let! items = connection.QueryAsync<'T>(cmd)
                return Ok (Seq.tryHead items)
            with ex ->
                return Error ex
        }
        |> Async.AwaitTask


    let readSingle<'T> (config: SqlConfig) op =
        ThreadSafe.initialize ()
        task {
            try
                let cmd = CommandDefinition.fromSqlOperation config op
                use connection = new NpgsqlConnection(config.ConnectString)
                let! item = connection.QuerySingleAsync<'T>(cmd)
                return Ok item
            with ex ->
                return Error ex
        }


    /// make sure to dispose the returned GridReader
    let multiRead (config: SqlConfig) op =
        ThreadSafe.initialize ()
        task {
            let cmd = CommandDefinition.fromSqlOperation config op
            let connection = new NpgsqlConnection(config.ConnectString)
            return! connection.QueryMultipleAsync(cmd)
        }
        |> Async.AwaitTask


    let multiResult x =
        async {
            try
                let! x_ = x
                return Ok x_
            with ex ->
                return Error ex
        }


    let write (config: SqlConfig) op =
        ThreadSafe.initialize ()
        task {
            try
                let cmd = CommandDefinition.fromSqlOperation config op
                use connection = new NpgsqlConnection(config.ConnectString)
                let! _ = connection.ExecuteAsync(cmd)
                return Ok ()
            with ex ->
                return Error ex
        }
        |> Async.AwaitTask


    /// Returns the count of rows affected by the operation.
    /// 
    /// Note: if the op contains multiple SQL statements,
    /// the returned count is only for the last executed statement.
    let writeWithCount (config: SqlConfig) op =
        ThreadSafe.initialize ()
        task {
            try
                let cmd = CommandDefinition.fromSqlOperation config op
                use connection = new NpgsqlConnection(config.ConnectString)
                let! affectedCount = connection.ExecuteAsync(cmd)
                return Ok affectedCount
            with ex ->
                return Error ex
        }
        |> Async.AwaitTask


