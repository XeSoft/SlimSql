# Slim SQL

A SQL library for F# with an elegant, minimal API.


# Overview

## Usage

It doesn't try to hide SQL from you, but just tries to get out of your way while you use SQL. Here's how you can define a query.

```fsharp
open SlimSql.MsSql.Helpers

let listCourses offset limit =
    sql
        """
        SELECT CourseId, CourseName
          FROM Course
        OFFSET @Offset ROWS
         FETCH
          NEXT @Limit ROWS ONLY
        ;
        """
        [
            p "Offset" offset
            p "Limit" limit
        ]
```

The helper functions in the above example are `sql` and `p`. Actually, `p` (short for parameter) is just a shortcut for making a tuple. Here's how you would run the query.

```fsharp
type Course =
    {
        CourseId : int
        CourseName : string
    }

open SlimSql.MsSql

let sqlConfig = SqlConfig.create connectStringFromSomewhere
let query = listCourses request.Offset request.Limit
let coursesAsync = Sql.query<Course> sqlConfig query

// coursesAsync is Async<Course array>
```

Here, `Sql.query<Course>` is a function which runs the query and converts each data row into a `Course` object. Like with most mappers, the property types and names have to match the returned columns. Additionally, the order of the returned fields from the SQL statement need to match the order of the record fields. 😒

Note that the query is created separately from the code which executes the query. This is particularly handy for queries which perform updates. I can setup multiple writes ahead of time, even from separate pieces of code, then perform them later in the same transaction.

```fsharp
let deactivateCourse courseId =
    sql "UPDATE ..." [ p "CourseId" courseId ]

let cancelCourseRegistrations courseId =
    sql "DELETE ..." [ p "CourseId" courseId ]

...

let patches =
    [
        deactivateCourse courseId
        cancelCourseRegistrations courseId
    ]

...

Sql.writeBatch sqlConfig patches

// returns Async<unit>
```

More documentation needed. See this [post](https://dev.to/kspeakman/dirt-simple-sql-queries-in-f-a37) for more info.
