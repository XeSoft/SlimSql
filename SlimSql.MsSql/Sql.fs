namespace SlimSql.MsSql

type SqlQuery =
    {
        Query : string
        Parameters : (string * obj) list
    }


type SqlConfig =
    {
        ConnectString : string
        CommandTimeout : int option
    }
    with
        static member create connectString =
            {
                ConnectString = connectString
                CommandTimeout = None
            }

        static member withTimeout seconds sqlConfig =
            { sqlConfig with CommandTimeout = Some seconds }


module Sql =

    module Helpers =

        let p name value =
            ( name, value )


        let sql query parameters =
            {
                Query = query
                Parameters = parameters
            }


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


    module Startup =

        open System


        type OptionHandler<'T>() =
            inherit SqlMapper.TypeHandler<option<'T>>()

            override __.SetValue(param, value) =
                let valueOrNull =
                    match value with
                    | Some x -> box x
                    | None -> null

                param.Value <- valueOrNull

            override __.Parse value =
                if isNull value || value = box DBNull.Value then
                    None
                else
                    Some (value :?> 'T)


        let registerOptionTypes () =
            SqlMapper.AddTypeHandler(new OptionHandler<bool>())
            SqlMapper.AddTypeHandler(new OptionHandler<int16>())
            SqlMapper.AddTypeHandler(new OptionHandler<int32>())
            SqlMapper.AddTypeHandler(new OptionHandler<int64>())
            SqlMapper.AddTypeHandler(new OptionHandler<string>())
            SqlMapper.AddTypeHandler(new OptionHandler<Guid>())
            SqlMapper.AddTypeHandler(new OptionHandler<DateTime>())
            SqlMapper.AddTypeHandler(new OptionHandler<DateTimeOffset>())
            SqlMapper.AddTypeHandler(new OptionHandler<TimeSpan>())
            SqlMapper.AddTypeHandler(new OptionHandler<single>())
            SqlMapper.AddTypeHandler(new OptionHandler<double>())
            SqlMapper.AddTypeHandler(new OptionHandler<decimal>())
            SqlMapper.AddTypeHandler(new OptionHandler<byte>())
            SqlMapper.AddTypeHandler(new OptionHandler<byte[]>())


    let private initResult =
        Startup.registerOptionTypes ()
            
