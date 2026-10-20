using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaterTankTool_WFA
{
    public static class AppState
    {
        // ====== Your state (with defaults) ======
        public static int NoOfColumns { get; set; } = 1;
        public static TankType CurrentTankType { get; set; } = TankType.None;
        public static string Fy { get; set; } = "36000";
        public static int Rtc { get; set; } = 334;
        public static int NoOfSegment { get; set; } = 1;
        public static double struts { get; set; } = 0;
        public static double crossBracing { get; set; } = 0;

        // ====== Persistence ======
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            // Store enums as strings so renumbering won't break old files
            Converters = { new JsonStringEnumConverter() }
        };

        public static string CurrentProjectDir { get; set; } = string.Empty;

        private static string ConfigDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaterTankTool_WFA");

        private static string ConfigPath => Path.Combine(ConfigDir, "appstate.json");

        public static void Load()
        {
            try
            {
                string pathToRead = ConfigPath;
                if (!string.IsNullOrWhiteSpace(CurrentProjectDir))
                {
                    string projectStatePath = Path.Combine(CurrentProjectDir, "appstate.json");
                    if (File.Exists(projectStatePath))
                    {
                        pathToRead = projectStatePath;
                    }
                }

                if (!File.Exists(pathToRead))
                {
                    Directory.CreateDirectory(ConfigDir);
                    return;
                }

                var json = File.ReadAllText(pathToRead);
                var dto = JsonSerializer.Deserialize<AppStateDto>(json, JsonOpts);
                if (dto == null) return;

                NoOfColumns = dto.NoOfColumns;
                if (dto.CurrentTankType != TankType.None)
                    CurrentTankType = dto.CurrentTankType;
                Fy = dto.Fy ?? Fy;
                Rtc = dto.Rtc;
                NoOfSegment = dto.NoOfSegment;
                struts = dto.struts;
                crossBracing = dto.crossBracing;
            }
            catch
            {
                // Corrupt or unreadable file → ignore and keep defaults
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);

                var dto = new AppStateDto
                {
                    NoOfColumns = NoOfColumns,
                    CurrentTankType = CurrentTankType,
                    Fy = Fy,
                    Rtc = Rtc,
                    NoOfSegment = NoOfSegment,
                    struts = struts,
                    crossBracing = crossBracing
                };

                var json = JsonSerializer.Serialize(dto, JsonOpts);
                File.WriteAllText(ConfigPath, json);

                if (!string.IsNullOrWhiteSpace(CurrentProjectDir) && Directory.Exists(CurrentProjectDir))
                {
                    File.WriteAllText(Path.Combine(CurrentProjectDir, "appstate.json"), json);
                }
            }
            catch
            {
                // Swallow errors (e.g., disk locked). You can log if you have logging.
            }
        }

        // DTO used only for persistence
        private class AppStateDto
        {
            public int NoOfColumns { get; set; }
            public TankType CurrentTankType { get; set; }
            public string Fy { get; set; }
            public int Rtc { get; set; }
            public int NoOfSegment { get; set; }
            public double struts { get; set; }
            public double crossBracing { get; set; }
        }
    }
}
