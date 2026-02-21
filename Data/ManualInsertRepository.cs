using Microsoft.Data.Sqlite;

namespace FloridaLotteryApp.Data;

public static class ManualInsertRepository
{
    public static void InsertPair(
        DateTime date,
        string drawTime,
        string pick3Number,
        int? pick3Fireball,
        string pick4Number,
        int? pick4Fireball)
    {
        using var conn = Db.Open();
        using var tx = conn.BeginTransaction();

        InsertPick3(conn, tx, date, drawTime, pick3Number, pick3Fireball);
        InsertPick4(conn, tx, date, drawTime, pick4Number, pick4Fireball);

        tx.Commit();
    }

    public static bool UpdateLatestPair(
        DateTime date,
        string drawTime,
        string pick3Number,
        int? pick3Fireball,
        string pick4Number,
        int? pick4Fireball)
    {
        using var conn = Db.Open();
        var pick3RowId = GetLatestRowId(conn, "pick3", date, drawTime);
        var pick4RowId = GetLatestRowId(conn, "pick4", date, drawTime);

        if (pick3RowId == null || pick4RowId == null)
        {
            return false;
        }

        using var tx = conn.BeginTransaction();

        UpdatePick3(conn, tx, pick3RowId.Value, pick3Number, pick3Fireball);
        UpdatePick4(conn, tx, pick4RowId.Value, pick4Number, pick4Fireball);

        tx.Commit();
        return true;
    }

    public static bool DeleteLatestPair(DateTime date, string drawTime)
    {
        using var conn = Db.Open();
        var pick3RowId = GetLatestRowId(conn, "pick3", date, drawTime);
        var pick4RowId = GetLatestRowId(conn, "pick4", date, drawTime);

        if (pick3RowId == null && pick4RowId == null)
        {
            return false;
        }

        using var tx = conn.BeginTransaction();

        if (pick3RowId != null)
        {
            DeleteRow(conn, tx, "pick3", pick3RowId.Value);
        }

        if (pick4RowId != null)
        {
            DeleteRow(conn, tx, "pick4", pick4RowId.Value);
        }

        tx.Commit();
        return true;
    }

    private static long? GetLatestRowId(SqliteConnection conn, string game, DateTime date, string drawTime)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT rowid
            FROM {game}_draws
            WHERE draw_date = $d AND draw_time = $t
            ORDER BY rowid DESC
            LIMIT 1;
        """;
        cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$t", drawTime);

        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        return (long)result;
    }

    private static void InsertPick3(
        SqliteConnection conn,
        SqliteTransaction tx,
        DateTime date,
        string drawTime,
        string number,
        int? fireball)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO pick3_draws (draw_date, draw_time, number, n1, n2, n3, fireball)
            VALUES ($d, $t, $n, $n1, $n2, $n3, $fb);
        """;
        cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$t", drawTime);
        cmd.Parameters.AddWithValue("$n", number);
        cmd.Parameters.AddWithValue("$n1", number[0] - '0');
        cmd.Parameters.AddWithValue("$n2", number[1] - '0');
        cmd.Parameters.AddWithValue("$n3", number[2] - '0');
        cmd.Parameters.AddWithValue("$fb", (object?)fireball ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void InsertPick4(
        SqliteConnection conn,
        SqliteTransaction tx,
        DateTime date,
        string drawTime,
        string number,
        int? fireball)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO pick4_draws (draw_date, draw_time, number, n1, n2, n3, n4, fireball)
            VALUES ($d, $t, $n, $n1, $n2, $n3, $n4, $fb);
        """;
        cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$t", drawTime);
        cmd.Parameters.AddWithValue("$n", number);
        cmd.Parameters.AddWithValue("$n1", number[0] - '0');
        cmd.Parameters.AddWithValue("$n2", number[1] - '0');
        cmd.Parameters.AddWithValue("$n3", number[2] - '0');
        cmd.Parameters.AddWithValue("$n4", number[3] - '0');
        cmd.Parameters.AddWithValue("$fb", (object?)fireball ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void UpdatePick3(
        SqliteConnection conn,
        SqliteTransaction tx,
        long rowId,
        string number,
        int? fireball)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE pick3_draws
            SET number = $n, n1 = $n1, n2 = $n2, n3 = $n3, fireball = $fb
            WHERE rowid = $id;
        """;
        cmd.Parameters.AddWithValue("$id", rowId);
        cmd.Parameters.AddWithValue("$n", number);
        cmd.Parameters.AddWithValue("$n1", number[0] - '0');
        cmd.Parameters.AddWithValue("$n2", number[1] - '0');
        cmd.Parameters.AddWithValue("$n3", number[2] - '0');
        cmd.Parameters.AddWithValue("$fb", (object?)fireball ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void UpdatePick4(
        SqliteConnection conn,
        SqliteTransaction tx,
        long rowId,
        string number,
        int? fireball)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE pick4_draws
            SET number = $n, n1 = $n1, n2 = $n2, n3 = $n3, n4 = $n4, fireball = $fb
            WHERE rowid = $id;
        """;
        cmd.Parameters.AddWithValue("$id", rowId);
        cmd.Parameters.AddWithValue("$n", number);
        cmd.Parameters.AddWithValue("$n1", number[0] - '0');
        cmd.Parameters.AddWithValue("$n2", number[1] - '0');
        cmd.Parameters.AddWithValue("$n3", number[2] - '0');
        cmd.Parameters.AddWithValue("$n4", number[3] - '0');
        cmd.Parameters.AddWithValue("$fb", (object?)fireball ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void DeleteRow(SqliteConnection conn, SqliteTransaction tx, string game, long rowId)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"""
            DELETE FROM {game}_draws
            WHERE rowid = $id;
        """;
        cmd.Parameters.AddWithValue("$id", rowId);
        cmd.ExecuteNonQuery();
    }
}
