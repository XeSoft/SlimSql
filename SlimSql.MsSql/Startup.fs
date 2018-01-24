namespace SlimSql.MsSql

(*

    This code is unfortunately necessary to
    teach Dapper how to handle option types.

*)

module Startup =

    open System
    open Dapper


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

