namespace SlimSql.Postgres

module SqlOperation =

    let merge ops =
        { Statements =
            ops
            |> Seq.map (fun x -> x.Statements)
            |> List.concat }
