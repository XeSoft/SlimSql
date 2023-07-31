namespace SlimSql.Postgres

[<AutoOpen>]
module Stmt =

    open Npgsql
    open NpgsqlTypes
    open SlimSql.Postgres.Types


    /// A positional parameter. Query placeholders are: $1, $2, etc.
    let inline v (value: 'T) =
        let param = NpgsqlParameter<'T>()
        param.TypedValue <- value
        param

    /// A positional parameter with the database type specified.
    let inline vTyped (value: 'T) (type_: NpgsqlDbType) =
        let param = NpgsqlParameter<'T>()
        param.TypedValue <- value
        param.NpgsqlDbType <- type_
        param

    /// A named parameter. Query placeholder example: @ParamName
    /// 
    /// Note: Queries with named parameters are transformed into
    /// positional parameter queries before being sent to the server.
    /// For best performance, use positional parameters.
    let inline p (name: string) (value: 'T) =
        let param = NpgsqlParameter<'T>()
        param.TypedValue <- value
        param.ParameterName <- name
        param

    /// A named parameter with the database type specified.
    /// 
    /// Note: Queries with named parameters are transformed into
    /// positional parameter queries before being sent to the server.
    /// For best performance, use positional parameters.
    let inline pTyped (name: string) (value: 'T) (type_: NpgsqlDbType) =
        let param = NpgsqlParameter<'T>()
        param.TypedValue <- value
        param.NpgsqlDbType <- type_
        param.ParameterName <- name
        param

    /// Create a SqlOperation from a statement and its parameters.
    let sql (statement: string) (parameters: NpgsqlParameter list) =
        let stmt =
            { Text = statement
              Params = parameters }
        {Statements = [stmt]}

