namespace SlimSql.MsSql


module Helpers =

    let p name value =
        ( name, value )


    let sql query parameters =
        {
            Query = query
            Parameters = parameters
        }
