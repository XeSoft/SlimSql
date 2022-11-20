namespace SlimSql.Postgres.Integration.Npgsql

open Npgsql
open SlimSql.Postgres.Types

module NpgsqlParameter =

    open Microsoft.FSharp.Reflection
    open System


    let inline isOption (t : Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>>


    let fromSqlParam (p : SqlParam) : NpgsqlParameter =
        let value =
            if isNull p.Value then
                DbNullObj
            else
                let t = p.Value.GetType()
                if isOption t then
                    let _,fields = FSharpValue.GetUnionFields(p.Value, t)
                    fields.[0]
                else
                    p.Value

        match p.Type with
        | None ->
            NpgsqlParameter(p.Name, value)

        | Some t ->
            NpgsqlParameter(p.Name, t, Value=value)


