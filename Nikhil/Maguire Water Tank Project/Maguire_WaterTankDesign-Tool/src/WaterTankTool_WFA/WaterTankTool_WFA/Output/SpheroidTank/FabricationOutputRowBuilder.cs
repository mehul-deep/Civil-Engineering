using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                row.Values[$"B{i}HT"] = (double)(s?.HeightFinal ?? 0) * 12;
                row.Values[$"B{i}THK"] = (double)(s?.Thickness ?? 0);
                row.Values[$"B{i}LR"] = (double)(s?.DiameterFinal ?? 0) * 6;
                row.Values[$"B{i}UR"] = (double)(s?.DiameterInitial ?? 0) *6;
                row.Values[$"B{i}DEG"] = 0;
                row.Values[$"B{i}Q"] = 0;
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

            row.Values["T1OR"] = 0;
            row.Values["T1LR"] = 0;
            row.Values["T1UR"] = 0;
            row.Values["TIHT"] = 0;
            row.Values["T1THK"] = 0;
            row.Values["T1DEG"] = 0;
            row.Values["T1Q"] = 0;
            row.Values["T2UR"] = 0;
            row.Values["T2LR"] = 0;
            row.Values["T2HT"] = 0;
            row.Values["T2THK"] = 0;
            row.Values["T2SEG"] = 0;
            row.Values["T2Q"] = 0;
            row.Values["T3UR"] = 0;
            row.Values["T3LR"] = 0;
            row.Values["T3HT"] = 0;
            row.Values["T3THK"] = 0;
            row.Values["T3SEG"] = 0;
            row.Values["T3Q"] = 0;
            row.Values["T4UR"] = 0;
            row.Values["T4LR"] = 0;
            row.Values["T4HT"] = 0;
            row.Values["T4THK"] = 0;
            row.Values["T4SEG"] = 0;
            row.Values["T4Q"] = 0;
            row.Values["T5UR"] = 0;
            row.Values["T5LR"] = 0;
            row.Values["T5HT"] = 0;
            row.Values["T5THK"] = 0;
            row.Values["T5SEG"] = 0;
            row.Values["T5Q"] = 0;
            row.Values["T6UR"] = 0;
            row.Values["T6LR"] = 0;
            row.Values["T6HT"] = 0;
            row.Values["T6THK"] = 0;
            row.Values["T6SEG"] = 0;
            row.Values["T6Q"] = 0;
            row.Values["KKTHK"] = 0;
            row.Values["KKLR"] = 0;
            row.Values["KKCHT"] = 0;
            row.Values["KKUSR"] = 0;
            row.Values["KKUER"] = 0;
            row.Values["KKER"] = 0;
            row.Values["KKSDEG"] = 0;
            row.Values["KKEDEG"] = 0;
            row.Values["KKSECR"] = 0;
            row.Values["BKTHK"] = 0;
            row.Values["BKR"] = 0;
            row.Values["BKQ"] = 0;
            row.Values["BKSDEG"] = 0;
            row.Values["BKEDEG"] = 0;
            row.Values["BKEDIM"] = 0;
            row.Values["BKDIA"] = 0;
            row.Values["TKTHK"] = 0;
            row.Values["TKR"] = 0;
            row.Values["TKQ"] = 0;
            row.Values["TKSDEG"] = 0;
            row.Values["TKEDEG"] = 0;
            row.Values["TKEDIM"] = 0;
            row.Values["TKDIA"] = 0;
            row.Values["RFTHK"] = 0;
            row.Values["RFR"] = 0;
            row.Values["RFQ"] = 0;
            row.Values["RFSDEG"] = 0;
            row.Values["RFEDEG"] = 0;
            row.Values["RFEDIM"] = 0;
            row.Values["RFDIA"] = 0;
            row.Values["RCLR"] = 0;
            row.Values["RCUR"] = 0;
            row.Values["RCHT"] = 0;
            row.Values["RCTHK"] = 0;
            row.Values["RCQ"] = 0;
            row.Values["RCBCRIR"] = 0;
            row.Values["RCBCROR"] = 0;
            row.Values["RCBCRTHK"] = 0;
            row.Values["RCBCRDEG"] = 0;
            row.Values["RCBCRQ"] = 0;
            row.Values["RCTCRIR"] = 0;
            row.Values["RCTCROR"] = 0;
            row.Values["RCTCRTHK"] = 0;
            row.Values["DWLDIA"] = 0;
            row.Values["DWLHT"] = 0;
            row.Values["DWLTHK"] = 0;
            row.Values["DWUDIA"] = 0;
            row.Values["DWUHT"] = 0;
            row.Values["DWUTHK"] = 0;
            row.Values["DWSTFOR"] = 0;
            row.Values["DWSTFIR"] = 0;
            row.Values["DWSTFTHK"] = 0;
            row.Values["DWSTFQ"] = 0;
            



            // ...continue later column by column...
            // ====== End filling fields ======

            return row;
        }



    }
}
