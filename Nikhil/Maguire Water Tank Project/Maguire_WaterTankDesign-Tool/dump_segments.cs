using System;
using System.Data.SQLite;

class Program {
    static void Main() {
        string dbPath = @"C:\Users\Mehul.Deep\Desktop\Test with ahmad\Test with ahmad\project_data.db";
        string connString = $"Data Source={dbPath};Version=3;";
        using (var conn = new SQLiteConnection(connString)) {
            conn.Open();
            using (var cmd = new SQLiteCommand("SELECT * FROM SegmentProperties;", conn)) {
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        Console.WriteLine($"Type: {reader["SegmentType"]}, Name: {reader["SegmentName"]}, HI: {reader["HeightInitial"]}, HF: {reader["HeightFinal"]}, Dia: {reader["Diameter"]}, Thick: {reader["Thickness"]}");
                    }
                }
            }
        }
    }
}
