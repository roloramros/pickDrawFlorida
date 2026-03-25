// AnalisisGuardadoRepository.cs - CORREGIDO
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace FloridaLotteryApp.Data;

public class FolderInfo
{
    public string? Folder { get; set; }
    public string? Label { get; set; }
    public List<long> Ids { get; set; } = new();
    public string DisplayName => $"{Folder} - {Label}";
}

public static class AnalisisGuardadoRepository
{
    public static List<string> GetTiposAnalisisUnicos()
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT tipo_analisis
            FROM saved_analisis
            WHERE tipo_analisis IS NOT NULL AND tipo_analisis != ''
            ORDER BY tipo_analisis;
            """;
        
        using var reader = cmd.ExecuteReader();
        var tipos = new List<string>();
        
        while (reader.Read())
        {
            var tipo = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(tipo))
            {
                tipos.Add(tipo);
            }
        }
        
        return tipos;
    }

    public static List<FolderInfo> GetFoldersByTipoAnalisis(string tipoAnalisis)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT 
                rowid,
                folder,
                label
            FROM saved_analisis
            WHERE tipo_analisis = $tipo
              AND folder IS NOT NULL 
              AND folder != ''
            ORDER BY folder, rowid DESC;
            """;

        cmd.Parameters.AddWithValue("$tipo", tipoAnalisis);

        using var reader = cmd.ExecuteReader();
        var folderDict = new Dictionary<string, FolderInfo>();
        
        while (reader.Read())
        {
            var folder = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (string.IsNullOrWhiteSpace(folder)) continue;
            
            var id = reader.GetInt64(0);
            var label = reader.IsDBNull(2) ? null : reader.GetString(2);
            
            if (!folderDict.ContainsKey(folder))
            {
                folderDict[folder] = new FolderInfo
                {
                    Folder = folder,
                    Label = label,
                    Ids = new List<long> { id }
                };
            }
            else
            {
                folderDict[folder].Ids.Add(id);
                if (string.IsNullOrWhiteSpace(folderDict[folder].Label) && !string.IsNullOrWhiteSpace(label))
                {
                    folderDict[folder].Label = label;
                }
            }
        }
        
        return folderDict.Values.ToList();
    }

    public static bool DeleteByFolder(string tipoAnalisis, string folder)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM saved_analisis
            WHERE tipo_analisis = $tipo AND folder = $folder;
            """;
        
        cmd.Parameters.AddWithValue("$tipo", tipoAnalisis);
        cmd.Parameters.AddWithValue("$folder", folder);
        
        return cmd.ExecuteNonQuery() > 0;
    }

    public static bool Delete(long id)
    {
        using var conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM saved_analisis
            WHERE rowid = $id;
            """;
        
        cmd.Parameters.AddWithValue("$id", id);
        
        return cmd.ExecuteNonQuery() > 0;
    }
}