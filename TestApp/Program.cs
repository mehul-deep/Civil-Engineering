using System;
using WaterTankTool_WFA.Services;
using WaterTankTool_WFA.Entity;

var service = new LoadService();
var loads = service.GetGoverningLoads();
Console.WriteLine($"Pu: {loads.Pu}, Mu: {loads.Mu}, Vu: {loads.Vu}");
