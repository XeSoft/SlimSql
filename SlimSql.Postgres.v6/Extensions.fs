namespace SlimSql.Postgres

[<AutoOpen>]
module Extensions =

    open Npgsql
    open SlimSql.Postgres.Integration.Dapper
    open System.Threading

    type NpgsqlDataReader with

        member inline reader.array<'T>(?cancel: CancellationToken) =
            let cancel = defaultArg cancel CancellationToken.None
            NpgsqlDataReader.array<'T> cancel reader

        member inline reader.list<'T>(?cancel: CancellationToken) =
            let cancel = defaultArg cancel CancellationToken.None
            NpgsqlDataReader.list<'T> cancel reader

        member inline reader.tryFirst<'T>(?cancel: CancellationToken) =
            let cancel = defaultArg cancel CancellationToken.None
            NpgsqlDataReader.tryFirst<'T> cancel reader

        member inline reader.first<'T>(?cancel: CancellationToken) =
            let cancel = defaultArg cancel CancellationToken.None
            NpgsqlDataReader.first<'T> cancel reader

        member inline reader.scalar<'T>(?cancel: CancellationToken) =
            let cancel = defaultArg cancel CancellationToken.None
            NpgsqlDataReader.scalar<'T> cancel reader

        member inline reader.fold<'state, 'item> (initial: 'state, apply: 'state -> 'item -> 'state, ?cancel: CancellationToken) =
            let cancel = defaultArg cancel CancellationToken.None
            NpgsqlDataReader.fold cancel initial apply reader


