# Slim SQL

A SQL library for F# with an elegant, minimal API.


# Overview

## Usage

It doesn't try to hide SQL from you, but gets out of your way while you use SQL. Here is the basic usage.

```fsharp
open SlimSql.Postgres

type Course =
    { CourseId: int
      CourseName: string }

let listCourses offset limit =
    // Usage: sql <query> <parameters>
    sql
        // the query
        """
        SELECT CourseId, CourseName
        FROM Course
        ORDER BY CourseName
        LIMIT @Limit
        OFFSET @Offset
        ;
        """
        // the parameters
        [
            // Usage: p <name> <value>
            p "@Offset" offset
            p "@Limit" limit
        ]

let sqlConfig = SqlConfig.create myConnectionString

let query = listCourses 0 100
Sql.read<Course> sqlConfig query

// returns: Async<Result<Course list, exn>>
```


Multiple statements can be merged into one.

```fsharp
module Course =
    let deactivate courseId =
        sql
            """UPDATE Course SET IsActive = false WHERE CourseId = @CourseId;"""
            [ p "@CourseId" courseId ]

module Registration =
    let removeAllForCourse courseId =
        sql
            """DELETE FROM Registration WHERE CourseId = @CourseId;"""
            [ p "@CourseId" courseId ]


let deactivate sqlConfig courseId =
    SqlOperation.merge [
        Course.deactivate courseId
        Registration.removeAllForCourse courseId
    ]
    |> SqlOperation.wrapInTransaction
    |> Sql.write sqlConfig

    // returns Async<Result<unit, exn>>
```


Multiple reads in one database round-trip, with a little code organization thrown in.

```fsharp
open System
open SlimSql.Postgres

module Student =

    type Detail =
        { StudentId: Guid
          Name: string }

    let getDetail studentId =
        sql
            """SELECT StudentId, Name FROM Student WHERE StudentId = @StudentId;"""
            [ p "@StudentId" studentId ]

module Registration =

    type ForStudent =
        { RegistrationId: Guid
          CourseId: Guid
          CourseName: string
          StatusName: string }

    let listForStudent studentId offset limit =
        sql
            """
            SELECT r.RegistrationId, r.CourseId, c.Name AS CourseName, c.StatusName
            FROM Registration r
            JOIN Course c ON c.CourseId = r.CourseId
            WHERE r.StudentId = @StudentId
            ORDER BY c.Name, r.RegistrationId -- incl RegId for consistent sort
            LIMIT @Limit
            OFFSET @Offset
            """
            [
                p "@StudentId" studentId
                p "@Limit" limit
                p "@Offset" offset
            ]


type StudentOverview =
    { Detail: Student.Detail
      Registrations: Registration.ForStudent list }

type StudentOverviewRequest =
    { StudentId: Guid
      Offset: int
      Limit: int }

let overview sqlConfig { StudentId = studentId
                         Offset = offset
                         Limit = limit } =
    async {
        let op =
            SqlOperation.merge [
                Student.getDetail studentId
                Registration.listForStudent studentId offset limit
            ]
        use! multi = Sql.multiRead sqlConfig op
        match! GridReader.readFirst<Student.Detail> multi with
        | None -> return None
        | Some detail ->
            let! registrations = GridReader.read<Registration.ForStudent> multi
            return Some { Detail = detail; Registrations = registrations }
    }
    |> Sql.multiResult

    // returns Async<Result<StudentOverview option, exn>>
```

This uses Npgsql and Dapper under the covers.

In case the database type cannot be automatically inferred by Npgsql, you can provide the parameter type with `pTyped`:

```fsharp
let create entityId json =
    sql
        """INSERT INTO Entity (EntityId, Data) VALUES (@EntityId, @Data);"""
        [
            p "@EntityId" entityId
            pTyped "@Data" json NpgsqlDbType.Jsonb
        ]
```

Option types are supported. Here, Notes may be null so they are represented as an `option` type.

```fsharp
// Notes may be null in the database
module Course =
    type Detail =
        { CourseId: Guid
          Name: string
          Notes: string option }

    let getDetail courseId =
        sql
            """SELECT CourseId, Name, Notes FROM Course WHERE CourseId = @CourseId;"""
            [ p "@CourseId" courseId ]


let detail sqlConfig courseId =
    let op = Course.getDetail courseId
    Sql.readFirst<Course.Detail> sqlConfig op

    // returns: Async<Result<Course.Detail option, exn>>
```
