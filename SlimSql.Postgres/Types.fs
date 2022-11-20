namespace SlimSql.Postgres

[<AutoOpen>]
module Types =

    open Npgsql
    open NpgsqlTypes


    let DbNullObj = box System.DBNull.Value


    type SqlParam =
        {
            Name : string
            Value : obj
            Type : NpgsqlDbType option
        }


    type SqlOperation =
        {
            Statement : string
            Parameters : SqlParam list
        }


    type SqlConfig =
        {
            ConnectString : string
            Transaction : NpgsqlTransaction option
            CommandTimeoutSeconds : int option
        }


    let p name value =
        {
            Name = name
            Value = value
            Type = None
        }


    let pTyped name value type_ =
        {
            Name = name
            Value = value
            Type = Some type_
        }


    let sql statement parameters =
        {
            Statement = statement
            Parameters = parameters
        }
