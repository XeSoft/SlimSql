namespace SlimSql.Postgres

module Sql =

    open Npgsql
    open SlimSql.Postgres.Integration.Dapper
    open System.Threading.Tasks


    // Why?
    // 1. Dapper type handlers are global, requiring global one-time configuration.
    // 2. F# does not provide a way to define a static constructor for a module.
    // 3. Module `do` only runs for host projects, not libraries.
    // 4. Requiring user-facing initialization for F# types is not desirable.
    // Note: This pattern is essentially a user-defined static constructor.
    module private InitOnce =
        // double-check lock pattern
        let mutable private __uninitialized = true
        let private __locker = obj()
        // after slower initial call, fast path is used
        let inline initialize () =
            if __uninitialized then // fast path: no lock, maybe stale value
                lock __locker (fun () -> // synchronize thread access
                    if __uninitialized then // check up-to-date value
                        Init.addOptionTypes ()
                        __uninitialized <- false
                )

    module Internal =

        let inline write cfg op =
            task {
                use batch =  new NpgsqlBatch()
                NpgsqlBatch.apply op.Statements batch
                use connection = new NpgsqlConnection(cfg.ConnectString)
                batch.Connection <- connection
                do! connection.OpenAsync(cfg.Cancel)
                for opt in cfg.RunOpts do
                    match opt with
                    | RunOpt.Prepare ->
                        do! batch.PrepareAsync(cfg.Cancel)
                return! batch.ExecuteNonQueryAsync(cfg.Cancel)
            }

        let inline read cfg op (f: NpgsqlDataReader -> Task<'T>) =
            task {
                use batch =  new NpgsqlBatch()
                NpgsqlBatch.apply op.Statements batch
                use connection = new NpgsqlConnection(cfg.ConnectString)
                batch.Connection <- connection
                do! connection.OpenAsync(cfg.Cancel)
                for opt in cfg.RunOpts do
                    match opt with
                    | RunOpt.Prepare ->
                        do! batch.PrepareAsync(cfg.Cancel)
                use! reader = batch.ExecuteReaderAsync(cfg.Cancel)
                return! f reader
            }

        let inline toSingleOp op =
            match op.Statements with
            | [] -> op
            | stmt :: _ -> { Statements = [stmt] }


    module Write =

        type AffectedRowCount = int

        let exec (sqlConfig: SqlConfig) (op: SqlOperation) =
            InitOnce.initialize ()
            task {
                let! _affected = Internal.write sqlConfig op
                return ()
            }

        let execCount (sqlConfig: SqlConfig) (op: SqlOperation)
            : Task<AffectedRowCount> =
            InitOnce.initialize ()
            Internal.write sqlConfig op


    module Read =

        let array<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp (NpgsqlDataReader.array<'T> sqlConfig.Cancel)

        let list<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp (NpgsqlDataReader.list<'T> sqlConfig.Cancel)

        let tryFirst<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp (NpgsqlDataReader.tryFirst<'T> sqlConfig.Cancel)

        let first<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp (NpgsqlDataReader.first<'T> sqlConfig.Cancel)

        let scalar<'T> (sqlConfig: SqlConfig) (op: SqlOperation) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp (NpgsqlDataReader.scalar<'T> sqlConfig.Cancel)

        let fold<'state, 'item> (sqlConfig: SqlConfig) (op: SqlOperation)
            (initial: 'state)
            (apply: 'state -> 'item -> 'state) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp
                (NpgsqlDataReader.fold<'state, 'item> sqlConfig.Cancel initial apply)

        let foldWhile<'state, 'item> (sqlConfig: SqlConfig) (op: SqlOperation)
            (canContinue: 'state -> bool)
            (initial: 'state)
            (apply: 'state -> 'item -> 'state) =
            InitOnce.initialize ()
            let singleOp = Internal.toSingleOp op
            Internal.read sqlConfig singleOp
                (NpgsqlDataReader.foldWhile<'state, 'item> sqlConfig.Cancel canContinue initial apply)

        /// Use NpgsqlDataReader to process multiple query results or more advanced queries.
        /// Some extensions are provided. E.g. NpgsqlDataReader.array
        let multi<'T> (sqlConfig: SqlConfig) (op: SqlOperation)
            (readFn: NpgsqlDataReader -> Task<'T>) =
            InitOnce.initialize ()
            Internal.read sqlConfig op readFn



