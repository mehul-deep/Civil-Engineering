using System;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

class Program {
    static void Main() {
        string dbPath = @""C:\Users\Mehul.Deep\Desktop\top temp\Nikhil-1\Nikhil\Maguire Water Tank Project\Maguire_WaterTankDesign-Tool\src\WaterTankTool_WFA\WaterTankTool_WFA\bin\Debug\net8.0-windows\project_data.db"";
        var options = new DbContextOptionsBuilder<WaterTankDbContext>().UseSqlite($""Data Source={dbPath}"").Options;
        using var db = new WaterTankDbContext($""Data Source={dbPath}"");

        var tp = db.TankProperties.FirstOrDefault();
        var misc = db.DeadLoadEntity.FirstOrDefault();
        var segments = db.SegmentProperties.ToList();

        string outStr = $""TotalWeight: {tp?.TotalWeight}\nWater: {tp?.WeightOfWater}\nSteel: {tp?.WeightOfSteel}\nMisc: {misc?.Miscellaneous_Load}\n"";
        
        File.WriteAllText(""C:\\Users\\Mehul.Deep\\Desktop\\top temp\\Nikhil-1\\dump.txt"", outStr);
    }
}
