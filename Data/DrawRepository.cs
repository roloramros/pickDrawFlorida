using Microsoft.Data.Sqlite;
using System.Linq;

namespace FloridaLotteryApp.Data
{

public record Pick3Hit(DateTime Date, string DrawTime, string Number, int? Fireball);
public record Hit(DateTime Date, string Game, string DrawTime, string Number, int? Fireball);
public record AnalysisHit(DateTime Date, string DrawTime, string Pick3, string Pick4);
public record ComboHit(DateTime Date, string DrawTime, string Pick3, string Pick4, int? Pick3Fireball, int? Pick4Fireball);
public record FilteredCodificacion(DateTime Date, string DrawTime, string FullNumber, string Pick3, string Pick4, string NextPick3);

public static class DrawRepository
{
    public static DateTime? GetLastPick3Date()
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(draw_date) FROM pick3_draws;";
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return null;
        return DateTime.Parse(result.ToString()!);
    }

    public static List<Pick3Hit> FindThirdAnalysisMatches(string guidePick3, string resultPick3, string excludedPick3)
    {
        // Validar entradas
        if (string.IsNullOrWhiteSpace(guidePick3) || guidePick3.Length != 3 ||
            string.IsNullOrWhiteSpace(resultPick3) || resultPick3.Length != 3)
        {
            return new List<Pick3Hit>();
        }

        // 1. Identificar dígitos repetidos entre Guía y Resultado
        var guideDigits = guidePick3.Select(c => c.ToString()).ToList();
        var resultDigits = resultPick3.Select(c => c.ToString()).ToList();
        
        var repeatedDigits = guideDigits
            .Where(d => resultDigits.Contains(d))
            .Distinct()
            .ToList();

        // 2. Si hay 3 dígitos repetidos, retornar vacío
        if (repeatedDigits.Count == 3)
        {
            return new List<Pick3Hit>();
        }

        // 3. Determinar patrón y dígitos prohibidos
        var prohibitedDigits = new HashSet<string>();
        
        // Añadir todos los dígitos de Guía y Resultado
        foreach (var d in guideDigits) prohibitedDigits.Add(d);
        foreach (var d in resultDigits) prohibitedDigits.Add(d);

        // Construir el patrón SQL
        var patternConditions = new List<string>();
        var parameters = new Dictionary<string, object>();
        int paramIndex = 0;

        for (int i = 0; i < 3; i++)
        {
            var digit = resultDigits[i];
            
            if (repeatedDigits.Contains(digit))
            {
                // Posición fija: debe ser exactamente este dígito
                patternConditions.Add($"n{i + 1} = @p{paramIndex}");
                parameters.Add($"@p{paramIndex}", int.Parse(digit));
                paramIndex++;
            }
            else
            {
                // Posición libre: no puede ser dígito prohibido
                var freeConditions = new List<string>();
                freeConditions.Add($"n{i + 1} NOT IN ({string.Join(",", prohibitedDigits.Select(d => int.Parse(d)))})");
                
                // No puede repetir otros dígitos fijos del patrón
                for (int j = 0; j < i; j++)
                {
                    if (repeatedDigits.Contains(resultDigits[j]))
                    {
                        freeConditions.Add($"n{i + 1} != n{j + 1}");
                    }
                }
                
                patternConditions.Add($"({string.Join(" AND ", freeConditions)})");
            }
        }

        // 4. Condición para no repetir dígitos dentro del Pick3
        patternConditions.Add("n1 != n2 AND n1 != n3 AND n2 != n3");

        // 5. Excluir el Pick3 Resultado original
        patternConditions.Add($"NOT (n1 = {int.Parse(resultDigits[0])} AND n2 = {int.Parse(resultDigits[1])} AND n3 = {int.Parse(resultDigits[2])})");

        // 6. Construir consulta SQL
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        
        var whereClause = string.Join(" AND ", patternConditions);
        
        cmd.CommandText = $"""
            SELECT draw_date, draw_time, number, fireball
            FROM pick3_draws
            WHERE {whereClause}
            ORDER BY draw_date DESC, draw_time DESC;
        """;

        // Añadir parámetros
        foreach (var param in parameters)
        {
            cmd.Parameters.AddWithValue(param.Key, param.Value);
        }

        // 7. Ejecutar consulta
        using var r = cmd.ExecuteReader();
        var results = new List<Pick3Hit>();

        while (r.Read())
        {
            var date = DateTime.Parse(r.GetString(0));
            var drawTime = r.GetString(1);
            var number = r.GetString(2);
            int? fireball = r.IsDBNull(3) ? null : r.GetInt32(3);
            
            results.Add(new Pick3Hit(date, drawTime, number, fireball));
        }

        return results;
    }
    

    public static List<FilteredCodificacion> GetCodificacionesWithSingleCommonDigit(int maxResults = 1)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        // Obtener TODAS las codificaciones primero
        cmd.CommandText = """
            SELECT 
                draw_date,
                draw_time,
                number,
                n1, n2, n3,          -- Pick3: dígitos 1-3
                n4, n5, n6, n7,      -- Pick4: dígitos 4-7
                n8, n9, n10          -- NextPick3: dígitos 8-10
            FROM codificaciones
            ORDER BY draw_date DESC, draw_time DESC;
        """;

        using var r = cmd.ExecuteReader();
        var results = new List<FilteredCodificacion>();

        while (r.Read())
        {
            var date = DateTime.Parse(r.GetString(0));
            var drawTime = r.GetString(1);
            var fullNumber = r.GetString(2);

            // Obtener dígitos
            var pick3Digits = new[] { r.GetInt32(3), r.GetInt32(4), r.GetInt32(5) };
            var pick4Digits = new[] { r.GetInt32(6), r.GetInt32(7), r.GetInt32(8), r.GetInt32(9) };
            var nextPick3Digits = new[] { r.GetInt32(10), r.GetInt32(11), r.GetInt32(12) };

            // Convertir a strings
            var pick3 = string.Join("", pick3Digits);
            var pick4 = string.Join("", pick4Digits);
            var nextPick3 = string.Join("", nextPick3Digits);

            // REGLA 1: Pick3 debe tener 3 dígitos distintos
            if (pick3Digits.Distinct().Count() != 3)
                continue;

            // REGLA 2: Pick4 debe tener 4 dígitos distintos
            if (pick4Digits.Distinct().Count() != 4)
                continue;

            // REGLA 3: Pick3 y Pick4 deben tener EXACTAMENTE 1 dígito en común
            var commonDigits = pick3Digits.Intersect(pick4Digits).ToList();
            if (commonDigits.Count != 1)
                continue;

            // REGLA 4: NextPick3 debe tener 3 dígitos distintos (opcional, puedes quitar esta si no es necesaria)
            //if (nextPick3Digits.Distinct().Count() != 3)
                //continue;

            results.Add(new FilteredCodificacion(
                date, drawTime, fullNumber, pick3, pick4, nextPick3
            ));

            //if (results.Count >= maxResults)
            //{
              //  break;
            //}
        }

        return results;
    }

    // NUEVO: devuelve number + fireball
    public static (string? Number, int? Fireball) GetPick3Result(DateTime date, string drawTime)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT number, fireball
            FROM pick3_draws
            WHERE draw_date = $d AND draw_time = $t
            ORDER BY rowid DESC
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$t", drawTime);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return (null, null);

        var number = r.IsDBNull(0) ? null : r.GetString(0);
        int? fb = r.IsDBNull(1) ? null : r.GetInt32(1);

        return (number, fb);
    }

    public static List<Pick3Hit> SearchPick3ByNumber(string number)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT draw_date, draw_time, number, fireball
            FROM pick3_draws
            WHERE number = $n
            ORDER BY draw_date DESC, draw_time DESC, rowid DESC;
        """;

        cmd.Parameters.AddWithValue("$n", number);

        using var r = cmd.ExecuteReader();
        var list = new List<Pick3Hit>();

        while (r.Read())
        {
            var d = DateTime.Parse(r.GetString(0));
            var t = r.GetString(1);
            var num = r.GetString(2);
            int? fb = r.IsDBNull(3) ? null : r.GetInt32(3);
            list.Add(new Pick3Hit(d, t, num, fb));
        }

        return list;
    }

    public static DateTime? GetLastDateOverall()
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT MAX(d) FROM (
              SELECT MAX(draw_date) AS d FROM pick3_draws
              UNION ALL
              SELECT MAX(draw_date) AS d FROM pick4_draws
            );
        """;

        var r = cmd.ExecuteScalar();
        if (r == null || r == DBNull.Value) return null;
        return DateTime.Parse(r.ToString()!);
    }

    public static (string? Number, int? Fireball) GetResult(string game, DateTime date, string drawTime)
    {
        // game: "pick3" o "pick4"
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = $"""
            SELECT number, fireball
            FROM {game}_draws
            WHERE draw_date = $d AND draw_time = $t
            ORDER BY rowid DESC
            LIMIT 1;
        """;

        cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$t", drawTime);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return (null, null);

        var num = r.IsDBNull(0) ? null : r.GetString(0);
        int? fb = r.IsDBNull(1) ? null : r.GetInt32(1);
        return (num, fb);
    }

    public static List<Hit> SearchByNumberBoth(string number)
    {
        var hits = new List<Hit>();
        if (number.Length == 3)
            hits.AddRange(SearchByNumberGame("pick3", number));
        else if (number.Length == 4)
            hits.AddRange(SearchByNumberGame("pick4", number));
        else
            return hits;

        return hits;
    }

    public static List<ComboHit> SearchPick3Pick4Combo(string pick3, string pick4)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT p3.draw_date,
                   p3.draw_time,
                   p3.number,
                   p4.number,
                   p3.fireball,
                   p4.fireball
            FROM pick3_draws p3
            INNER JOIN pick4_draws p4
                ON p3.draw_date = p4.draw_date
               AND p3.draw_time = p4.draw_time
            WHERE p3.number = $p3 AND p4.number = $p4
            ORDER BY p3.draw_date DESC, p3.draw_time DESC, p3.rowid DESC;
        """;

        cmd.Parameters.AddWithValue("$p3", pick3);
        cmd.Parameters.AddWithValue("$p4", pick4);

        using var r = cmd.ExecuteReader();
        var list = new List<ComboHit>();
        while (r.Read())
        {
            list.Add(new ComboHit(
                DateTime.Parse(r.GetString(0)),
                r.GetString(1),
                r.GetString(2),
                r.GetString(3),
                r.IsDBNull(4) ? null : r.GetInt32(4),
                r.IsDBNull(5) ? null : r.GetInt32(5)
            ));
        }

        return list;
    }

    private static List<Hit> SearchByNumberGame(string game, string number)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = $"""
            SELECT draw_date, draw_time, number, fireball
            FROM {game}_draws
            WHERE number = $n
            ORDER BY draw_date DESC, draw_time DESC, rowid DESC;
        """;
        cmd.Parameters.AddWithValue("$n", number);

        using var r = cmd.ExecuteReader();
        var list = new List<Hit>();
        while (r.Read())
        {
            list.Add(new Hit(
                DateTime.Parse(r.GetString(0)),
                game.ToUpper(),               // PICK3 / PICK4
                r.GetString(1),
                r.GetString(2),
                r.IsDBNull(3) ? null : r.GetInt32(3)
            ));
        }
        return list;
    }

    public static int CountDistinctDatesOverall()
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT COUNT(*) FROM (
              SELECT DISTINCT draw_date AS d FROM pick3_draws
              UNION
              SELECT DISTINCT draw_date AS d FROM pick4_draws
            );
        """;

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static List<DateTime> GetDistinctDatesOverall(int limit, int offset)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT d FROM (
              SELECT DISTINCT draw_date AS d FROM pick3_draws
              UNION
              SELECT DISTINCT draw_date AS d FROM pick4_draws
            )
            ORDER BY d DESC
            LIMIT $limit OFFSET $offset;
        """;

        cmd.Parameters.AddWithValue("$limit", limit);
        cmd.Parameters.AddWithValue("$offset", offset);

        using var r = cmd.ExecuteReader();
        var list = new List<DateTime>();
        while (r.Read())
            list.Add(DateTime.Parse(r.GetString(0)));

        return list;
    }

    public static List<AnalysisHit> FindPositionMatches(int p3Pos1Based, int p4Pos1Based)
    {
        // p3Pos1Based: 1..3
        // p4Pos1Based: 1..4

        string p3Col = p3Pos1Based switch { 1 => "p3.n1", 2 => "p3.n2", 3 => "p3.n3", _ => throw new ArgumentOutOfRangeException(nameof(p3Pos1Based)) };
        string p4Col = p4Pos1Based switch { 1 => "p4.n1", 2 => "p4.n2", 3 => "p4.n3", 4 => "p4.n4", _ => throw new ArgumentOutOfRangeException(nameof(p4Pos1Based)) };

        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        // Filtramos en SQL por la igualdad de dígito en esas posiciones para reducir candidatos.
        cmd.CommandText = $"""
            SELECT p3.draw_date, p3.draw_time, p3.number, p4.number,
                   p3.n1, p3.n2, p3.n3,
                   p4.n1, p4.n2, p4.n3, p4.n4
            FROM pick3_draws p3
            JOIN pick4_draws p4
              ON p3.draw_date = p4.draw_date
             AND p3.draw_time = p4.draw_time
            WHERE {p3Col} = {p4Col}
            ORDER BY p3.draw_date DESC, p3.draw_time DESC, p3.rowid DESC;
        """;

        using var r = cmd.ExecuteReader();
        var results = new List<AnalysisHit>();

        while (r.Read())
        {
            var date = DateTime.Parse(r.GetString(0));
            var drawTime = r.GetString(1);     // "M" o "E"
            var p3num = r.GetString(2);
            var p4num = r.GetString(3);

            int p3n1 = r.GetInt32(4);
            int p3n2 = r.GetInt32(5);
            int p3n3 = r.GetInt32(6);

            int p4n1 = r.GetInt32(7);
            int p4n2 = r.GetInt32(8);
            int p4n3 = r.GetInt32(9);
            int p4n4 = r.GetInt32(10);

            // Regla: dígitos independientes dentro de cada pick
            var p3 = new[] { p3n1, p3n2, p3n3 };
            if (p3.Distinct().Count() != 3) continue;

            var p4 = new[] { p4n1, p4n2, p4n3, p4n4 };
            if (p4.Distinct().Count() != 4) continue;

            // Regla: exactamente 1 dígito común entre Pick3 y Pick4
            var comunes = p3.Intersect(p4).ToList();
            if (comunes.Count != 1) continue;

            // Regla: el dígito común debe estar exactamente en las posiciones pedidas
            int expected = p3[p3Pos1Based - 1];
            if (expected != p4[p4Pos1Based - 1]) continue;       // ya debería por WHERE, pero lo dejamos seguro
            if (comunes[0] != expected) continue;

            results.Add(new AnalysisHit(date, drawTime, p3num, p4num));
        }

        return results;
    }

    public static List<AnalysisHit> FindPositionMatchesByPositionOnly(int p3Pos1Based, int p4Pos1Based)
    {
        // p3Pos1Based: 1..3
        // p4Pos1Based: 1..4

        string p3Col = p3Pos1Based switch { 1 => "p3.n1", 2 => "p3.n2", 3 => "p3.n3", _ => throw new ArgumentOutOfRangeException(nameof(p3Pos1Based)) };
        string p4Col = p4Pos1Based switch { 1 => "p4.n1", 2 => "p4.n2", 3 => "p4.n3", 4 => "p4.n4", _ => throw new ArgumentOutOfRangeException(nameof(p4Pos1Based)) };

        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        // Solo filtramos por posición, sin exigir que sea el único dígito común.
        cmd.CommandText = $"""
            SELECT p3.draw_date, p3.draw_time, p3.number, p4.number,
                   p3.n1, p3.n2, p3.n3,
                   p4.n1, p4.n2, p4.n3, p4.n4
            FROM pick3_draws p3
            JOIN pick4_draws p4
              ON p3.draw_date = p4.draw_date
             AND p3.draw_time = p4.draw_time
            WHERE {p3Col} = {p4Col}
            ORDER BY p3.draw_date DESC, p3.draw_time DESC, p3.rowid DESC;
        """;

        using var r = cmd.ExecuteReader();
        var results = new List<AnalysisHit>();

        while (r.Read())
        {
            var date = DateTime.Parse(r.GetString(0));
            var drawTime = r.GetString(1);     // "M" o "E"
            var p3num = r.GetString(2);
            var p4num = r.GetString(3);

            int p3n1 = r.GetInt32(4);
            int p3n2 = r.GetInt32(5);
            int p3n3 = r.GetInt32(6);

            int p4n1 = r.GetInt32(7);
            int p4n2 = r.GetInt32(8);
            int p4n3 = r.GetInt32(9);
            int p4n4 = r.GetInt32(10);

            var p3 = new[] { p3n1, p3n2, p3n3 };
            var p4 = new[] { p4n1, p4n2, p4n3, p4n4 };

            // Validamos que la coincidencia exista en las posiciones pedidas.
            if (p3[p3Pos1Based - 1] != p4[p4Pos1Based - 1]) continue;

            results.Add(new AnalysisHit(date, drawTime, p3num, p4num));
        }

        return results;
    }

    public static DateTime? GetNextPick3Date(DateTime date)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT MIN(draw_date)
            FROM pick3_draws
            WHERE draw_date > $d;
        """;

        cmd.Parameters.AddWithValue("$d", date.ToString("yyyy-MM-dd"));

        var r = cmd.ExecuteScalar();
        if (r == null || r == DBNull.Value) return null;
        return DateTime.Parse(r.ToString()!);
    }

    public static string? GetNextPick3Number(DateTime date, string drawTime)
        => GetNextPick3NumberInternal(date, drawTime);

    private static string? GetNextPick3NumberInternal(DateTime date, string drawTime)
    {
        // Si el hallazgo fue Mediodía (M), la próxima es Noche (E) del mismo día.
        if (drawTime == "M")
        {
            var nextSameDay = GetResult("pick3", date, "E").Number;
            return IsPick3Valid(nextSameDay) ? nextSameDay : null;
        }

        // Si el hallazgo fue Noche (E), la próxima es la primera tirada disponible del próximo día:
        // 1) intenta Mediodía (M)
        // 2) si no existe o es inválida, usa Noche (E)
        var nextDate = GetNextPick3Date(date);
        if (nextDate == null) return null;

        var nextMidday = GetResult("pick3", nextDate.Value, "M").Number;
        if (IsPick3Valid(nextMidday)) return nextMidday;

        var nextNight = GetResult("pick3", nextDate.Value, "E").Number;
        if (IsPick3Valid(nextNight)) return nextNight;

        return null;
    }

    private static bool IsPick3Valid(string? n)
    {
        if (string.IsNullOrWhiteSpace(n) || n.Length != 3) return false;
        if (!n.All(char.IsDigit)) return false;
        return true;
    }

    public static List<CodificacionEntry> FindCodificacionesWithRepeatPositions(int pos1Based, int pos2Based)
    {
        if (pos1Based < 1 || pos1Based > 7) throw new ArgumentOutOfRangeException(nameof(pos1Based));
        if (pos2Based < 1 || pos2Based > 7) throw new ArgumentOutOfRangeException(nameof(pos2Based));
        if (pos1Based == pos2Based) throw new ArgumentException("Las posiciones deben ser distintas.");

        string col1 = $"n{pos1Based}";
        string col2 = $"n{pos2Based}";

        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = $"""
            SELECT number, n1, n2, n3, n4, n5, n6, n7, n8, n9, n10
            FROM codificaciones
            WHERE {col1} = {col2}
            ORDER BY draw_date DESC, draw_time DESC;
        """;

        using var r = cmd.ExecuteReader();
        var results = new List<CodificacionEntry>();

        while (r.Read())
        {
            if (r.IsDBNull(1) || r.IsDBNull(2) || r.IsDBNull(3) || r.IsDBNull(4) || r.IsDBNull(5) ||
                r.IsDBNull(6) || r.IsDBNull(7) || r.IsDBNull(8) || r.IsDBNull(9) || r.IsDBNull(10))
            {
                continue;
            }

            var digits = new[]
            {
                r.GetInt32(1), r.GetInt32(2), r.GetInt32(3),
                r.GetInt32(4), r.GetInt32(5), r.GetInt32(6), r.GetInt32(7),
                r.GetInt32(8), r.GetInt32(9), r.GetInt32(10)
            };

            if (!HasSingleRepeatAtPositions(digits, pos1Based - 1, pos2Based - 1)) continue;

            var number = r.IsDBNull(0) ? "" : r.GetString(0);
            results.Add(new CodificacionEntry(number, digits));
        }

        return results;
    }

    private static bool HasSingleRepeatAtPositions(int[] digits, int pos1, int pos2)
    {
        if (digits.Length < 7) return false;

        int repeated = digits[pos1];
        if (repeated != digits[pos2]) return false;

        for (int i = 0; i < digits.Length; i++)
        {
            if (i == pos1 || i == pos2) continue;
            if (digits[i] == repeated) return false;
        }

        var distinctOthers = digits
            .Where((_, i) => i != pos1 && i != pos2)
            .Distinct()
            .Count();

        return distinctOthers == 5;
    }

}

public sealed record CodificacionEntry(string Number, int[] Digits);
}
