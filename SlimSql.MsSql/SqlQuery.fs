namespace SlimSql.MsSql

type SqlQuery =
    {
        Query : string
        Parameters : (string * obj) list
    }
