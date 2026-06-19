using System;
using System.Collections.Generic;
using System.Linq;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;
using WaterTankTool_WFA.Tanks;

namespace WaterTankTool_WFA.Services
{
    public class LoadService
    {
        private readonly WaterTankDbContext _context;
        private const double Multiplier = 1.667;

        public LoadService()
        {
            _context = WaterTankDbContext.GetInstance();
        }

        public (double Pu, double Mu, double Vu) GetGoverningLoads()
        {
            double Pu = 0;
            double Mu = 0;
            double Vu = 0;

            try
            {
                var tankProperties = _context.TankProperties.FirstOrDefault();
                var deadLoad = _context.DeadLoadEntity.FirstOrDefault();
                var segments = _context.SegmentProperties.ToList();
                var windLoad = _context.WindLoadEntity.FirstOrDefault();
                var seismicLoad = _context.SeismicLoadEntity.FirstOrDefault();

                if (tankProperties == null || segments.Count == 0)
                    return (0, 0, 0);

                // 1. Calculate P (Dead + Water + Segments)
                double totalWeight = double.Parse(tankProperties.TotalWeight);
                double segmentWeight = 0;

                var cylinderEquations = new Segment_Cylinder_Equations();
                var conicalEquations = new Segment_Conical_Equations();
                var multileg = new Multileg_Cylinders();

                if (AppState.CurrentTankType == TankType.MultiColumn)
                {
                    foreach (var s in segments)
                    {
                        if (s.SegmentType == "Cylinder" || s.SegmentType == "Riser")
                            segmentWeight += multileg.weightOfPedestal(s.HeightInitial, s.HeightFinal, s.Diameter, s.Thickness, s.SegmentType);
                    }
                }
                else
                {
                    foreach (var s in segments)
                    {
                        if (s.SegmentType == "Cylinder")
                            segmentWeight += cylinderEquations.weightOfPedestal(s.HeightInitial, s.HeightFinal, s.Diameter, s.Thickness);
                        else if (s.SegmentType == "Base")
                            segmentWeight += conicalEquations.weight(s.HeightInitial, s.HeightFinal, (double)s.DiameterInitial, (double)s.DiameterFinal, s.Thickness);
                    }
                }

                Pu = totalWeight + segmentWeight + (deadLoad?.Miscellaneous_Load ?? 0);

                // 2. Calculate M and V (Wind vs Seismic)
                double windM = 0;
                double windV = 0;
                double seismicM = 0;
                double seismicV = seismicLoad?.V ?? 0;

                // Wind Calculation
                var tankSegment = segments.FirstOrDefault(x => x.SegmentType == "Tanks");
                if (tankSegment != null)
                {
                    double fTank = multileg.F_Tank(tankSegment.HeightInitial, tankSegment.HeightFinal, tankSegment.Diameter, double.Parse(tankProperties.ProjectedArea));
                    windV += fTank;
                    windM += fTank * (double.Parse(tankProperties.Centroid) + tankSegment.HeightInitial);
                }

                foreach (var s in segments)
                {
                    double f = 0;
                    double m = 0;

                    if (AppState.CurrentTankType == TankType.MultiColumn)
                    {
                        if (s.SegmentType == "Cylinder" || s.SegmentType == "Riser")
                        {
                            f = multileg.F(s.HeightInitial, s.HeightFinal, s.Diameter, s.SegmentType);
                            m = multileg.Mbase(s.HeightInitial, s.HeightFinal, s.Diameter, s.SegmentType);
                        }
                    }
                    else
                    {
                        if (s.SegmentType == "Cylinder")
                        {
                            f = cylinderEquations.F(s.HeightInitial, s.HeightFinal, s.Diameter);
                            m = cylinderEquations.Mbase(s.HeightInitial, s.HeightFinal, s.Diameter);
                        }
                        else if (s.SegmentType == "Base")
                        {
                            var avgDia = ((double)s.DiameterFinal + (double)s.DiameterInitial) / 2;
                            f = conicalEquations.F(s.HeightInitial, s.HeightFinal, avgDia);
                            m = conicalEquations.Mbase(s.HeightInitial, s.HeightFinal, avgDia);
                        }
                    }
                    windV += f;
                    windM += m;
                }

                // Seismic Moment (simplified logic from Load_Combinations)
                if (seismicLoad != null)
                {
                    double comXweight = double.Parse(tankProperties.TotalWeight) * (double.Parse(tankProperties.Centroid) + segments[0].HeightInitial);
                    // (Note: This is a simplification; a full centroid calculation would be better, but we follow the existing pattern)
                    seismicM = (comXweight / Pu) * seismicLoad.V;
                }

                Mu = Math.Max(windM, seismicM);
                Vu = Math.Max(windV, seismicV);

                // Apply Multiplier
                Pu *= Multiplier;
                Mu *= Multiplier;
                Vu *= Multiplier;

                return (Math.Round(Pu, 4), Math.Round(Mu, 4), Math.Round(Vu, 4));
            }
            catch
            {
                return (0, 0, 0);
            }
        }
    }
}
