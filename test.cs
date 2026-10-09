using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;
using WaterTankTool_WFA.Tanks;
using System.Reflection;

class Program
{
    static void Main()
    {
        string dbPath = @""C:\Users\Mehul.Deep\Desktop\top temp\Nikhil-1\Nikhil\Maguire Water Tank Project\Maguire_WaterTankDesign-Tool\src\WaterTankTool_WFA\WaterTankTool_WFA\bin\Debug\net8.0-windows\project_data.db"";
        if (!File.Exists(dbPath)) {
            Console.WriteLine(""DB NOT FOUND!"");
            return;
        }

        var options = new DbContextOptionsBuilder<WaterTankDbContext>()
            .UseSqlite($""Data Source={dbPath}"")
            .Options;

        using var db = new WaterTankDbContext($""Data Source={dbPath}"");

        var tankProps = db.TankProperties.FirstOrDefault();
        var misc = db.DeadLoadEntity.FirstOrDefault();

        Console.WriteLine($""TotalWeight: {tankProps?.TotalWeight}"");
        Console.WriteLine($""WeightOfWater: {tankProps?.WeightOfWater}"");
        Console.WriteLine($""WeightOfSteel: {tankProps?.WeightOfSteel}"");
        Console.WriteLine($""MiscLoad: {misc?.Miscellaneous_Load}"");

        var segments = db.SegmentProperties.ToList();
        var multileg = new Multileg_Cylinders();
        // we can't easily instantiate multileg because it requires AppState and DB, 
        // let's just print segments
        foreach(var s in segments) {
            Console.WriteLine($""Seg: {s.SegmentType}, d={s.Diameter}, h={s.HeightFinal - s.HeightInitial}, t={s.Thickness}"");
        }
    }
}
