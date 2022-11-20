namespace SlimSql.Postgres.Integration.Dapper

open Dapper
open System

open SlimSql.Postgres.Types


module IDynamicParameters =

    open Npgsql
    open SlimSql.Postgres.Integration.Npgsql

    // oddly, dapper does not accept a list of DbParameters
    let fromSqlParams (parms: SqlParam list) : Dapper.SqlMapper.IDynamicParameters =
        { new Dapper.SqlMapper.IDynamicParameters with
            member __.AddParameters(command: System.Data.IDbCommand, _: SqlMapper.Identity): unit =
                match command with
                | :? NpgsqlCommand as cmd ->
                    parms
                    |> List.iter (
                        NpgsqlParameter.fromSqlParam
                        >> cmd.Parameters.Add
                        >> ignore
                    )
                | _ ->
                    raise (System.NotImplementedException("Expected NpgsqlCommand"))
        }


module CommandDefinition =

    let fromSqlOperation (config: SqlConfig) (op: SqlOperation) =
        let parms = IDynamicParameters.fromSqlParams op.Parameters
        CommandDefinition(op.Statement, parms, Option.toObj config.Transaction, Option.toNullable config.CommandTimeoutSeconds)


module Init =

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


