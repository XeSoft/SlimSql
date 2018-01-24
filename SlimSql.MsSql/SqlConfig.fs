namespace SlimSql.MsSql

type SqlConfig =
    {
        ConnectString : string
        CommandTimeout : int option
    }
    with
        static member create connectString =
            {
                ConnectString = connectString
                CommandTimeout = None
            }

        static member withTimeout seconds sqlConfig =
            { sqlConfig with CommandTimeout = Some seconds }
