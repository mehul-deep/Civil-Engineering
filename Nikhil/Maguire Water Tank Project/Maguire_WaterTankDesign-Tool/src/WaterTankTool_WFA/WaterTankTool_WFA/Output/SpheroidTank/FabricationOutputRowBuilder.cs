using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaterTankTool_WFA.Entity;
namespace WaterTankTool_WFA.Output.SpheroidTank
{
    internal class FabricationOutputRowBuilder
    {

        WaterTankDbContext context;
        private WaterTank _waterTankForm;
        public FabricationOutputRowBuilder(WaterTank waterTank)
        {
            var _context = WaterTankDbContext.GetInstance();
            context = _context;
            _waterTankForm = waterTank;

        }

        public FabricationOutputRow BuildFromCurrentDesign()
        {

            var tankProperties = context.TankProperties?.FirstOrDefault();
            var baseSegments = context.SegmentProperties?
                .Where(s => s.SegmentType == "Base")
                .ToList();

            var columnSegments = context.SegmentProperties?
                .Where(s => s.SegmentType == "Cylinder")
                .ToList();

            var basePlate = context.BasePlateEntity?.FirstOrDefault();
            var anchorBolt = context.AnchorBoltEntity?.FirstOrDefault();

            int capacity = 0;

            if (tankProperties?.Capacity != null)
            {
                // Filter the string to keep only digits
                string numericPart = new string(tankProperties.Capacity.Where(char.IsDigit).ToArray());

                int.TryParse(numericPart, out capacity);
                capacity = capacity / 1000;
            }

            double baseConeDiameter = (double)(baseSegments?.FirstOrDefault()?.DiameterFinal ?? 0);
            double baseConeHeight = (double)(baseSegments?.LastOrDefault()?.HeightFinal ?? 0);

            var tankDesignation = $"{capacity}MG {baseConeDiameter} x {baseConeHeight}";

            var bpor = (baseConeDiameter * 12 / 2) + 7;
            var b1ur = (((double)(baseSegments?.FirstOrDefault()?.DiameterInitial ?? 0)) * 12)/2;

            var colHeight = ((double)(columnSegments?.LastOrDefault()?.HeightFinal ?? 0)) - ((double)(columnSegments?.FirstOrDefault()?.HeightInitial ?? 0));
            //var baseconeDiameter = segmentProperties.

            var row = new FabricationOutputRow();


            // ====== Start filling fields (example) ======
            row.Values["TANK DESIGNATION"] = tankDesignation;
            row.Values["PARAMETER REF."] = $"TANK-{tankProperties?.TankNumber ?? 1}";
            row.Values["MG"] = capacity;
            row.Values["BASCONE DIA"] = baseConeDiameter;
            row.Values["BASECONE HT"] = baseConeHeight;

            row.Values["DOOR TYPE"] = "STD";
            row.Values["DDEG"] = 0;

            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                int totalColumns = (anchorBolt != null && anchorBolt.Ns.HasValue && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : (AppState.NoOfColumns > 1 ? AppState.NoOfColumns : 4);
                
                double bpdia = 20.04;
                if (basePlate != null && basePlate.Dbp > 0) bpdia = basePlate.Dbp < 100 ? basePlate.Dbp * 12 : basePlate.Dbp;
                else if (anchorBolt != null && anchorBolt.Dcone.HasValue && anchorBolt.Dcone.Value > 0) bpdia = anchorBolt.Dcone.Value < 100 ? anchorBolt.Dcone.Value * 12 : anchorBolt.Dcone.Value;
                row.Values["BPDIA"] = bpdia; // Column pipe diameter (in)

                double bporVal = 30.0;
                if (basePlate != null && basePlate.Ro > 0) bporVal = basePlate.Ro;
                row.Values["BPOR"] = bporVal; // Plate Width B (in)

                double bpirVal = 30.0;
                if (basePlate != null && basePlate.Ri > 0) bpirVal = basePlate.Ri;
                row.Values["BPIR"] = bpirVal; // Plate Length N (in)

                row.Values["BPDEG"] = totalColumns > 0 ? 360.0 / totalColumns : 90.0; // Angular spacing between legs around tower

                double bpthk = 1.50;
                if (basePlate != null && basePlate.T > 0) bpthk = basePlate.T;
                else if (basePlate != null && basePlate.T_req.HasValue && basePlate.T_req.Value > 0) bpthk = basePlate.T_req.Value;
                row.Values["BPTHK"] = bpthk;

                row.Values["BPQ"] = totalColumns; // 1 base plate per pedestal/leg

                int boltsPerLeg = anchorBolt?.Nb ?? (basePlate?.Nb ?? 4);
                int totalTowerBolts = boltsPerLeg * totalColumns;
                row.Values["ABHQ"] = totalTowerBolts; // Total bolt holes across all tower pedestals
                row.Values["ABHSD"] = anchorBolt?.Ab ?? 0;
                row.Values["ABQ"] = totalTowerBolts; // Total anchor bolts across all legs

                row.Values["SC"] = 0;
                row.Values["SCWT"] = 0;
                row.Values["BPSQ"] = totalTowerBolts; // Shims match total anchor bolts
            }
            else
            {
                double bpdia = baseConeDiameter * 12;
                if (basePlate != null && basePlate.Dbp > 0)
                {
                    // If Dbp < 100, assume feet and convert to inches; otherwise assume inches
                    bpdia = basePlate.Dbp < 100 ? basePlate.Dbp * 12 : basePlate.Dbp;
                }
                row.Values["BPDIA"] = bpdia;

                double bporVal = bpor;
                if (basePlate != null && basePlate.Ro > 0) bporVal = basePlate.Ro;
                row.Values["BPOR"] = bporVal;

                double bpirVal = bporVal - 12;
                if (basePlate != null && basePlate.Ri > 0) bpirVal = basePlate.Ri;
                row.Values["BPIR"] = bpirVal;

                double bpdeg = 0;
                if (basePlate != null && basePlate.Theta > 0) bpdeg = basePlate.Theta;
                else if (basePlate != null && basePlate.N > 0) bpdeg = 360.0 / basePlate.N;
                row.Values["BPDEG"] = bpdeg;

                double bpthk = 0;
                if (basePlate != null && basePlate.T > 0) bpthk = basePlate.T;
                else if (basePlate != null && basePlate.T_req > 0) bpthk = basePlate.T_req.Value;
                row.Values["BPTHK"] = bpthk;

                int bpq = 0;
                if (basePlate != null && basePlate.N > 0) bpq = basePlate.N;
                row.Values["BPQ"] = bpq;

                int abhq = 0;
                if (anchorBolt != null && anchorBolt.Nb > 0) abhq = anchorBolt.Nb;
                else if (basePlate != null && basePlate.Nb != null && basePlate.Nb > 0) abhq = basePlate.Nb.Value;
                row.Values["ABHQ"] = abhq;

                double abhsd = 0;
                if (anchorBolt != null) abhsd = anchorBolt.Ab;
                row.Values["ABHSD"] = abhsd;

                int abq = 0;
                if (anchorBolt != null && anchorBolt.Nb > 0) abq = anchorBolt.Nb;
                else if (basePlate != null && basePlate.Nb != null && basePlate.Nb > 0) abq = basePlate.Nb.Value;
                row.Values["ABQ"] = abq;

                row.Values["SC"] = abq; // Default side chairs 1 per bolt if single standpipe
                row.Values["SCWT"] = 0;
                row.Values["BPSQ"] = abq; // Shims match total anchor bolts
            }


            for (int i = 1; i <= 5; i++)
            {
                // ElementAtOrDefault returns null if the index doesn't exist (e.g., base 4 when only 3 exist)
                var s = baseSegments.ElementAtOrDefault(i - 1);
                bool exists = (s != null);

                // B3, B4, B5 have an extra Outside Radius column in the company template
                if (i >= 3)
                    row.Values[$"B{i}OR"] = exists ? (object)((double)(s.DiameterFinal ?? 0) * 6) : "-";

                row.Values[$"B{i}LR"] = exists ? (object)((double)(s.DiameterFinal ?? 0) * 6) : "-";
                row.Values[$"B{i}UR"] = exists ? (object)((double)(s.DiameterInitial ?? 0) * 6) : "-";
                row.Values[$"B{i}HT"] = exists ? (object)(s.HeightFinal * 12) : "-";
                row.Values[$"B{i}THK"] = exists ? (object)(double)(s.Thickness) : "-";
                row.Values[$"B{i}DEG"] = exists ? (object)0 : "-";
                row.Values[$"B{i}Q"] = exists ? (object)0 : "-";
            }

            row.Values["CDIA"] = (((double)(columnSegments?.FirstOrDefault()?.Diameter ?? 0)) * 12);
            row.Values["CHT"] = colHeight;
            for (int i = 1; i <= 18; i++)
            {
                // ElementAtOrDefault returns null if the index doesn't exist (e.g., base 4 when only 3 exist)
                var s = columnSegments.ElementAtOrDefault(i - 1);

                row.Values[$"C{i}DIA"] = (double)(s?.Diameter ?? 0) * 12;
                row.Values[$"C{i}HT"] = (double)(s?.HeightFinal ?? 0);
                row.Values[$"C{i}THK"] = (double)(s?.Thickness ?? 0);

            }

            var transitions = context.TransitionsEntity?.ToList() ?? new List<TransitionsEntity>();
            var knuckles = context.KnuckleEntity?.ToList() ?? new List<KnuckleEntity>();
            var roofFinger = context.RoofFingerEntity?.FirstOrDefault();
            var reducerCone = context.ReducerConeEntity?.FirstOrDefault();
            var drywell = context.DrywellEntity?.FirstOrDefault();

            // T1 to T6
            for (int i = 1; i <= 6; i++)
            {
                var t = transitions.FirstOrDefault(x => x.TransitionNumber == i);
                if (t != null)
                {
                    if (i == 1)
                    {
                        row.Values["T1OR"] = t.OutsideRadius;
                        row.Values["T1LR"] = t.LowerRadius;
                        row.Values["T1UR"] = t.UpperRadius;
                        row.Values["TIHT"] = t.Height; // TIHT = company template typo for T1 Height (col DH)
                        row.Values["T1THK"] = t.Thickness;
                        row.Values["T1DEG"] = t.SegmentDegree;
                        row.Values["T1Q"] = t.Quantity;
                    }
                    else
                    {
                        row.Values[$"T{i}UR"] = t.UpperRadius;
                        row.Values[$"T{i}LR"] = t.LowerRadius;
                        row.Values[$"T{i}HT"] = t.Height;
                        row.Values[$"T{i}THK"] = t.Thickness;
                        row.Values[$"T{i}SEG"] = t.SegmentDegree;
                        row.Values[$"T{i}Q"] = t.Quantity;
                    }
                }
            }

            // Knuckle-Knuckle
            var kk = knuckles.FirstOrDefault(x => x.KnuckleType == "KnuckleKnuckle");
            if (kk != null)
            {
                row.Values["KKTHK"] = kk.Thickness;
                row.Values["KKLR"] = kk.LowerRadius;
                row.Values["KKCHT"] = kk.CenterHeight;
                row.Values["KKUSR"] = kk.UpperStartRadius;
                row.Values["KKUER"] = kk.UpperExtendRadius;
                row.Values["KKER"] = kk.ExtendRadius;
                row.Values["KKSDEG"] = kk.StartDegree;
                row.Values["KKEDEG"] = kk.EndDegree;
                row.Values["KKSECR"] = kk.SectionRadius;
            }

            // Bottom Knuckle
            var bk = knuckles.FirstOrDefault(x => x.KnuckleType == "BottomKnuckle");
            if (bk != null)
            {
                row.Values["BKTHK"] = bk.Thickness;
                row.Values["BKR"] = bk.LowerRadius;
                row.Values["BKQ"] = bk.Quantity;
                row.Values["BKSDEG"] = bk.StartDegree;
                row.Values["BKEDEG"] = bk.EndDegree;
                row.Values["BKEDIM"] = bk.ExtraDimension;
                row.Values["BKDIA"] = bk.Diameter;
            }

            // Top Knuckle
            var tk = knuckles.FirstOrDefault(x => x.KnuckleType == "TopKnuckle");
            if (tk != null)
            {
                row.Values["TKTHK"] = tk.Thickness;
                row.Values["TKR"] = tk.LowerRadius;
                row.Values["TKQ"] = tk.Quantity;
                row.Values["TKSDEG"] = tk.StartDegree;
                row.Values["TKEDEG"] = tk.EndDegree;
                row.Values["TKEDIM"] = tk.ExtraDimension;
                row.Values["TKDIA"] = tk.Diameter;
            }

            // Roof Fingers
            if (roofFinger != null)
            {
                row.Values["RFTHK"] = roofFinger.Thickness;
                row.Values["RFR"] = roofFinger.Radius;
                row.Values["RFQ"] = roofFinger.Quantity;
                row.Values["RFSDEG"] = roofFinger.SegmentDegree;
                row.Values["RFEDEG"] = roofFinger.EndDegree;
                row.Values["RFEDIM"] = roofFinger.ExtraDimension;
                row.Values["RFDIA"] = roofFinger.Diameter;
            }

            // Reducer Cone
            if (reducerCone != null)
            {
                row.Values["RCLR"] = reducerCone.LowerRadius;
                row.Values["RCUR"] = reducerCone.UpperRadius;
                row.Values["RCHT"] = reducerCone.Height;
                row.Values["RCTHK"] = reducerCone.Thickness;
                row.Values["RCQ"] = reducerCone.Quantity;
                row.Values["RCBCRIR"] = reducerCone.BottomRingInsideRadius;
                row.Values["RCBCROR"] = reducerCone.BottomRingOutsideRadius;
                row.Values["RCBCRTHK"] = reducerCone.BottomRingThickness;
                row.Values["RCBCRDEG"] = reducerCone.BottomRingDegree;
                row.Values["RCBCRQ"] = reducerCone.BottomRingQuantity;
                row.Values["RCTCRIR"] = reducerCone.TopRingInsideRadius;
                row.Values["RCTCROR"] = reducerCone.TopRingOutsideRadius;
                row.Values["RCTCRTHK"] = reducerCone.TopRingThickness;
            }

            // Drywell
            if (drywell != null)
            {
                row.Values["DWLDIA"] = drywell.LowerDiameter;
                row.Values["DWLHT"] = drywell.LowerHeight;
                row.Values["DWLTHK"] = drywell.LowerThickness;
                row.Values["DWUDIA"] = drywell.UpperDiameter;
                row.Values["DWUHT"] = drywell.UpperHeight;
                row.Values["DWUTHK"] = drywell.UpperThickness;
                row.Values["DWSTFOR"] = drywell.StiffenerOutsideRadius;
                row.Values["DWSTFIR"] = drywell.StiffenerInsideRadius;
                row.Values["DWSTFTHK"] = drywell.StiffenerThickness;
                row.Values["DWSTFQ"] = drywell.StiffenerQuantity;
            }




            // ...continue later column by column...
            // ====== End filling fields ======

            return row;
        }



    }
}
