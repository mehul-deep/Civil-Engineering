using System;
using System.IO;
using System.Linq;
using System.Text.Json;

class AppState {
    public static int NoOfColumns { get; set; }
    public static int NoOfSegment { get; set; }
    public static double crossBracing { get; set; }
    public static double struts { get; set; }
    
    public static void Load() {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ""WaterTankTool_WFA"", ""appstate.json"");
        if (File.Exists(path)) {
            var json = File.ReadAllText(path);
            using (JsonDocument doc = JsonDocument.Parse(json)) {
                var root = doc.RootElement;
                if (root.TryGetProperty(""NoOfColumns"", out JsonElement noOfCol)) {
                    NoOfColumns = noOfCol.GetInt32();
                }
                if (root.TryGetProperty(""NoOfSegment"", out JsonElement noOfSeg)) {
                    NoOfSegment = noOfSeg.GetInt32();
                }
                if (root.TryGetProperty(""crossBracing"", out JsonElement cb)) {
                    crossBracing = cb.GetDouble();
                }
                if (root.TryGetProperty(""struts"", out JsonElement st)) {
                    struts = st.GetDouble();
                }
            }
        }
    }
}

class Program {
    static void Main() {
        AppState.Load();
        Console.WriteLine(""NoOfColumns: "" + AppState.NoOfColumns);
        Console.WriteLine(""NoOfSegment: "" + AppState.NoOfSegment);
        Console.WriteLine(""crossBracing: "" + AppState.crossBracing);
        Console.WriteLine(""struts: "" + AppState.struts);
    }
}
