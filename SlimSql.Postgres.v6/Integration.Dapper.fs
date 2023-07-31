namespace SlimSql.Postgres.Integration.Dapper

open Dapper
open Npgsql
open System

module NpgsqlDataReader =

    module Const =
        let [<Literal>] InitialReadCapacity = 5

    // not using typed parser because of bug:
    // https://github.com/DapperLib/Dapper/issues/1822
    let getParser<'T> (reader: NpgsqlDataReader) =
        let parse = reader.GetRowParser(typeof<'T>)
        let f reader =
            parse.Invoke(reader) :?> 'T
        f

    let foldWhile<'state, 'item> cancel (canContinue: 'state -> bool) (initial: 'state) (apply: 'state -> 'item -> 'state) (reader: NpgsqlDataReader) =
        task {
            let parse = getParser<'item> reader
            let mutable state = initial
            let! _keepReading = reader.ReadAsync(cancel)
            let mutable keepReading = _keepReading
            while keepReading do
                let item = parse reader
                state <- apply state item
                let! _keepReading = reader.ReadAsync(cancel)
                keepReading <- _keepReading && canContinue state
            let! _ = reader.NextResultAsync(cancel)
            return state
        }

    let fold<'state, 'item> cancel (initial: 'state) (apply: 'state -> 'item -> 'state) (reader: NpgsqlDataReader) =
        task {
            let parse = getParser<'item> reader
            let mutable state = initial
            let! _keepReading = reader.ReadAsync(cancel)
            let mutable keepReading = _keepReading
            while keepReading do
                let item = parse reader
                state <- apply state item
                let! _keepReading = reader.ReadAsync(cancel)
                keepReading <- _keepReading
            let! _ = reader.NextResultAsync(cancel)
            return state
        }

    let resizeArray<'T> cancel (reader: NpgsqlDataReader) =
        task {
            let parse = getParser<'T> reader
            let items = ResizeArray<'T>(Const.InitialReadCapacity)
            let! _keepReading = reader.ReadAsync(cancel)
            let mutable keepReading = _keepReading
            while keepReading do
                let item = parse reader
                items.Add(item)
                let! _keepReading = reader.ReadAsync(cancel)
                keepReading <- _keepReading
            let! _ = reader.NextResultAsync(cancel)
            return items
        }

    let array<'T> cancel (reader: NpgsqlDataReader) =
        task {
            let! items = resizeArray<'T> cancel reader
            return items.ToArray()
        }

    let list<'T> cancel (reader: NpgsqlDataReader) =
        task {
            let! items = resizeArray<'T> cancel reader
            return List.ofSeq items
        }

    let tryFirst<'T> cancel (reader: NpgsqlDataReader) =
        task {
            let parse = getParser<'T> reader
            let mutable itemOpt = None
            let! wasRead = reader.ReadAsync(cancel)
            if wasRead then
                let item = parse reader
                itemOpt <- Some item
            let! _ = reader.NextResultAsync(cancel)
            return itemOpt
        }

    /// Use only when you know a row will always be returned.
    /// This will throw if used incorrectly.
    let first<'T> cancel (reader: NpgsqlDataReader) =
        task {
            let parse = getParser<'T> reader
            let! _ = reader.ReadAsync(cancel)
            let item = parse reader
            let! _ = reader.NextResultAsync(cancel)
            return item
        }

    /// Use only when you know a scalar will always be returned.
    /// This will throw if used incorrectly.
    let scalar<'T> cancel (reader: NpgsqlDataReader) =
        task {
            let! _ = reader.ReadAsync(cancel)
            let item = reader.GetFieldValue<'T>(0)
            let! _ = reader.NextResultAsync(cancel)
            return item
        }


module NpgsqlBatch =

    open SlimSql.Postgres.Types

    let apply (statements: SqlStatement list) (batch: NpgsqlBatch) =
        for stmt in statements do
            let cmd = NpgsqlBatchCommand(stmt.Text)
            for param in stmt.Params do
                cmd.Parameters.Add(param) |> ignore
            batch.BatchCommands.Add(cmd)


module Init =

    module Internal =

        let DbNullObj = box DBNull.Value

        type OptionHandler<'T>() =
            inherit SqlMapper.TypeHandler<Option<'T>>()

            override __.SetValue(param, value) =
                match value with
                | Some x ->
                    match param with
                    | :? NpgsqlParameter<'T> as p ->
                        p.TypedValue <- x
                    | _ ->
                        param.Value <- box x
                | None ->
                    param.Value <- null

            override __.Parse value =
                if value = DbNullObj || isNull value then
                    None
                else
                    Some (value :?> 'T)

        type VOptionHandler<'T>() =
            inherit SqlMapper.TypeHandler<ValueOption<'T>>()

            override __.SetValue(param, value) =
                match value with
                | ValueSome x ->
                    match param with
                    | :? NpgsqlParameter<'T> as p ->
                        p.TypedValue <- x
                    | _ ->
                        param.Value <- box x
                | ValueNone ->
                    param.Value <- null

            override __.Parse value =
                if value = DbNullObj || isNull value then
                    ValueNone
                else
                    ValueSome (value :?> 'T)

    open Internal


    let addOptionTypes () =
        SqlMapper.AddTypeHandler(OptionHandler<Boolean>())
        SqlMapper.AddTypeHandler(OptionHandler<Byte>())
        SqlMapper.AddTypeHandler(OptionHandler<Byte[]>())
        SqlMapper.AddTypeHandler(OptionHandler<Char>())
        SqlMapper.AddTypeHandler(OptionHandler<DateTime>())
        SqlMapper.AddTypeHandler(OptionHandler<DateTimeOffset>())
        SqlMapper.AddTypeHandler(OptionHandler<Decimal>())
        SqlMapper.AddTypeHandler(OptionHandler<Double>())
        SqlMapper.AddTypeHandler(OptionHandler<Guid>())
        SqlMapper.AddTypeHandler(OptionHandler<Int16>())
        SqlMapper.AddTypeHandler(OptionHandler<Int32>())
        SqlMapper.AddTypeHandler(OptionHandler<Int64>())
        SqlMapper.AddTypeHandler(OptionHandler<Single>())
        SqlMapper.AddTypeHandler(OptionHandler<String>())
        SqlMapper.AddTypeHandler(OptionHandler<TimeSpan>())
        SqlMapper.AddTypeHandler(OptionHandler<UInt16>())
        SqlMapper.AddTypeHandler(OptionHandler<UInt32>())
        SqlMapper.AddTypeHandler(OptionHandler<UInt64>())

        SqlMapper.AddTypeHandler(VOptionHandler<Boolean>())
        SqlMapper.AddTypeHandler(VOptionHandler<Byte>())
        SqlMapper.AddTypeHandler(VOptionHandler<Byte[]>())
        SqlMapper.AddTypeHandler(VOptionHandler<Char>())
        SqlMapper.AddTypeHandler(VOptionHandler<DateTime>())
        SqlMapper.AddTypeHandler(VOptionHandler<DateTimeOffset>())
        SqlMapper.AddTypeHandler(VOptionHandler<Decimal>())
        SqlMapper.AddTypeHandler(VOptionHandler<Double>())
        SqlMapper.AddTypeHandler(VOptionHandler<Guid>())
        SqlMapper.AddTypeHandler(VOptionHandler<Int16>())
        SqlMapper.AddTypeHandler(VOptionHandler<Int32>())
        SqlMapper.AddTypeHandler(VOptionHandler<Int64>())
        SqlMapper.AddTypeHandler(VOptionHandler<Single>())
        SqlMapper.AddTypeHandler(VOptionHandler<String>())
        SqlMapper.AddTypeHandler(VOptionHandler<TimeSpan>())
        SqlMapper.AddTypeHandler(VOptionHandler<UInt16>())
        SqlMapper.AddTypeHandler(VOptionHandler<UInt32>())
        SqlMapper.AddTypeHandler(VOptionHandler<UInt64>())


