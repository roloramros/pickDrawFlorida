using Microsoft.Data.Sqlite;

namespace FloridaLotteryApp.Data;

public sealed class PlotOption2SavedRecord
{
    public long Id { get; set; }

    public string? Label { get; set; }

    public string G1Date { get; set; } = string.Empty;
    public string G1Time { get; set; } = string.Empty;
    public string G2Date { get; set; } = string.Empty;
    public string G2Time { get; set; } = string.Empty;
    public string G3Date { get; set; } = string.Empty;
    public string G3Time { get; set; } = string.Empty;
    public string G4Date { get; set; } = string.Empty;
    public string G4Time { get; set; } = string.Empty;

    public string R1Date { get; set; } = string.Empty;
    public string R1Time { get; set; } = string.Empty;
    public string R2Date { get; set; } = string.Empty;
    public string R2Time { get; set; } = string.Empty;
    public string R3Date { get; set; } = string.Empty;
    public string R3Time { get; set; } = string.Empty;
    public string R4Date { get; set; } = string.Empty;
    public string R4Time { get; set; } = string.Empty;
}

public static class PlotOption2SavedRepository
{
    public static List<PlotOption2SavedRecord> GetAll()
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                rowid,
                label,
                g1_date, g1_time,
                g2_date, g2_time,
                g3_date, g3_time,
                g4_date, g4_time,
                r1_date, r1_time,
                r2_date, r2_time,
                r3_date, r3_time,
                r4_date, r4_time
            FROM plot_option2_saved
            ORDER BY rowid DESC;
            """;

        using var reader = cmd.ExecuteReader();
        var results = new List<PlotOption2SavedRecord>();
        while (reader.Read())
        {
            results.Add(new PlotOption2SavedRecord
            {
                Id = reader.GetInt64(0),
                Label = reader.IsDBNull(1) ? null : reader.GetString(1),
                G1Date = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                G1Time = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                G2Date = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                G2Time = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                G3Date = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                G3Time = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                G4Date = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                G4Time = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                R1Date = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                R1Time = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                R2Date = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                R2Time = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                R3Date = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                R3Time = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                R4Date = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                R4Time = reader.IsDBNull(17) ? string.Empty : reader.GetString(17)
            });
        }

        return results;
    }

    public static long Insert(PlotOption2SavedRecord record)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO plot_option2_saved (
                label,
                g1_date, g1_time,
                g2_date, g2_time,
                g3_date, g3_time,
                g4_date, g4_time,
                r1_date, r1_time,
                r2_date, r2_time,
                r3_date, r3_time,
                r4_date, r4_time
            )
            VALUES (
                $label,
                $g1_date, $g1_time,
                $g2_date, $g2_time,
                $g3_date, $g3_time,
                $g4_date, $g4_time,
                $r1_date, $r1_time,
                $r2_date, $r2_time,
                $r3_date, $r3_time,
                $r4_date, $r4_time
            );
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$label", (object?)record.Label ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$g1_date", record.G1Date);
        cmd.Parameters.AddWithValue("$g1_time", record.G1Time);
        cmd.Parameters.AddWithValue("$g2_date", record.G2Date);
        cmd.Parameters.AddWithValue("$g2_time", record.G2Time);
        cmd.Parameters.AddWithValue("$g3_date", record.G3Date);
        cmd.Parameters.AddWithValue("$g3_time", record.G3Time);
        cmd.Parameters.AddWithValue("$g4_date", record.G4Date);
        cmd.Parameters.AddWithValue("$g4_time", record.G4Time);
        cmd.Parameters.AddWithValue("$r1_date", record.R1Date);
        cmd.Parameters.AddWithValue("$r1_time", record.R1Time);
        cmd.Parameters.AddWithValue("$r2_date", record.R2Date);
        cmd.Parameters.AddWithValue("$r2_time", record.R2Time);
        cmd.Parameters.AddWithValue("$r3_date", record.R3Date);
        cmd.Parameters.AddWithValue("$r3_time", record.R3Time);
        cmd.Parameters.AddWithValue("$r4_date", record.R4Date);
        cmd.Parameters.AddWithValue("$r4_time", record.R4Time);

        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    public static bool Delete(long id)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM plot_option2_saved
            WHERE rowid = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }
}
