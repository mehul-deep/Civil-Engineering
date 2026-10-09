using System;
using System.IO;
using System.Linq;

class Program {
    static void Main() {
        var dbPath = @"C:\Users\Mehul.Deep\Desktop\Test with ahmad\Test with ahmad\project_data.db";
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM SegmentProperties;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            Console.WriteLine($"Type: {reader["SegmentType"]}, HI: {reader["HeightInitial"]}, HF: {reader["HeightFinal"]}, Dia: {reader["Diameter"]}, Thick: {reader["Thickness"]}");
        }
    }
}
