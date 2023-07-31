namespace SlimSql.Postgres.Utils

module PgIdentifier =

    module Internal =

        open System.Text.RegularExpressions

(*
    From Postgres docs: https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS

        SQL identifiers and key words must begin with a letter (a-z, 
        but also letters with diacritical marks and non-Latin letters) 
        or an underscore (_). Subsequent characters in an identifier or 
        key word can be letters, underscores, digits (0-9), or dollar 
        signs ($). Note that dollar signs are not allowed in identifiers 
        according to the letter of the SQL standard, so their use might 
        render applications less portable. The SQL standard will not define 
        a key word that contains digits or starts or ends with an underscore, 
        so identifiers of this form are safe against possible conflict with 
        future extensions of the standard.

        The system uses no more than NAMEDATALEN-1 bytes of an identifier; 
        longer names can be written in commands, but they will be truncated. 
        By default, NAMEDATALEN is 64 so the maximum identifier length is 
        63 bytes. If this limit is problematic, it can be raised by changing 
        the NAMEDATALEN constant in src/include/pg_config_manual.h.

        Key words and unquoted identifiers are case insensitive.
*)

        // ^...$ string must only consist of ...
        // ^ starts with
        // [...] a single character which can be one of ...
        // [\p{L}_] a letter (any language) or underscore
        // [...]* then zero or more characters which can be ...
        // [\p{L}_\d$]* a letter or underscore or decimal digit or dollar sign
        // $ end of string
        let identifierRegex = Regex("""^[\p{L}_][\p{L}_\d$]*$""", RegexOptions.Compiled)

    let inline isValid (s: string) =
        Internal.identifierRegex.IsMatch(s)

    let inline isInvalid (s: string) =
        not (Internal.identifierRegex.IsMatch(s))


module SysQuery =

    let getTableNames =
        """
        SELECT c.relname AS TableName
        FROM pg_catalog.pg_class c
        LEFT JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('r','')
        AND n.nspname <> 'pg_catalog'
        AND n.nspname <> 'information_schema'
        AND n.nspname !~ '^pg_toast'
        AND pg_catalog.pg_table_is_visible(c.oid)
        ORDER BY 1
        """

