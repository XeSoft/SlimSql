namespace SlimSql.Postgres

module SqlConfig =
    
    let create connectString =
        {
            ConnectString = connectString
            Transaction = None
            CommandTimeoutSeconds = None
        }


    let withTransaction trans config =
        { config with
            Transaction = Some trans
        }


    let withTimeout seconds config =
        { config with
            CommandTimeoutSeconds = Some seconds
        }


