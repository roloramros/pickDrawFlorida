using FloridaLotteryApp.Data;

namespace FloridaLotteryApp.Data;

public static class TestRepository
{
    public static int CountPick3()
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pick3_draws;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
