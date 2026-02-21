using Microsoft.Data.Sqlite;

namespace FloridaLotteryApp.Data;

public static class Db
{
    public static SqliteConnection Open()
    {
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = "florida_pick3_pick4_fixed.sqlite",
            Mode = SqliteOpenMode.ReadWrite
        }.ToString();

        var conn = new SqliteConnection(cs);
        conn.Open();
        return conn;
    }
}
