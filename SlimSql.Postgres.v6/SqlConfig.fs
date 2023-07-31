namespace SlimSql.Postgres

module SqlConfig =
    
    let create connectString =
        {
            ConnectString = connectString
            Cancel = System.Threading.CancellationToken.None
            Transaction = None
            RunOpts = []
        }

    let createWithCancel connectString token =
        {
            ConnectString = connectString
            Cancel = token
            Transaction = None
            RunOpts = []
        }


    let withCancellation token config =
        { config with
            Cancel = token
        }


    let withTransaction trans config =
        { config with
            Transaction = Some trans
        }


    let withOptions opts config =
        { config with
            RunOpts = opts
        }


