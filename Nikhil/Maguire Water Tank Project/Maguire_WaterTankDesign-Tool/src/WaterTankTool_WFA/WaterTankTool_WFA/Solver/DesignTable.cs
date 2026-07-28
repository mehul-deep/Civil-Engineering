using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Solver
{
    public partial class DesignTable : Form
    {

        private readonly WaterTankDbContext _context;


        public string waterWeight;
        public string snowWeight;
        public string selfWeight;
        public List<tabelData2> tabelData2s = new List<tabelData2>();
        public event EventHandler<List<tabelData2>> Table2Loaded;

        public DesignTable()
        {

            InitializeComponent();

            _context = WaterTankDbContext.GetInstance();


            if (_context.SnowLoadEntity.FirstOrDefault() == null)
            {
                ShowError("Please add Snow Load first!");   // shows once
                return;                                     // skip the rest
            }

            try
            {
                LoadTable2();
            }
            catch (Exception ex)
            {
                ShowError($"Unexpected error while initialising Solver Output: {ex.Message}");
            }
        }

        private void ShowError(string msg, string title = "Error")
             => MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        private static int SegmentTypeOrder(string t)
            => t == "Tanks" ? 0
             : t == "Cylinder" ? 1
             : t == "Riser" ? 2
             : 3; // Base/others last
        private void LoadTable2()
        {
            // 1) fetch & sort
            List<SegmentProperties> segmentList = _context?.SegmentProperties?.ToList() ?? new();
            segmentList = segmentList
                .OrderBy(s => SegmentTypeOrder(s.SegmentType))
                .ThenByDescending(s => s.HeightFinal)
                .ToList();
            if (segmentList.Count == 0)
                return;

            // 2) first pass: compute each segment’s geometry + r, Fl, Cc
            var temp = new List<(
                SegmentProperties seg,
                double radius,
                double thickness,
                double rt,
                double a,
                double i,
                double co,
                double Fl,
                double Cc,
                double r
            )>();

            foreach (var seg in segmentList)
            {
                if (seg.SegmentType == "Tanks")
                {
                    // all zero for "Tanks"
                    temp.Add((seg, 0, 0, 0, 0, 0, 0, 0, 0, 0));
                    continue;
                }

                double dFinal = seg.DiameterFinal ?? seg.Diameter;
                double radius = 12 * dFinal / 2.0;
                double rt = Math.Round((12 * dFinal / 2.0) / seg.Thickness, 4);

                double iVal = Math.Round((Math.PI / 64) *
                              (Math.Pow(12 * dFinal, 4) -
                               Math.Pow(12 * dFinal - 2 * seg.Thickness, 4)), 4);

                double aVal = Math.Round((Math.PI / 4) *
                              (Math.Pow(12 * dFinal, 2) -
                               Math.Pow(12 * dFinal - 2 * seg.Thickness, 2)), 4);

                double co = Math.Round(1022 / (195 + rt), 4);
                double r = Math.Round(Math.Sqrt(iVal / aVal), 4);

                // allowable-stress state
                double rtcValue = AppState.Rtc;
                double fyVal = double.Parse(AppState.Fy);

                double Fl = rt < rtcValue
                    ? Math.Min(
                          Math.Round((233 * fyVal) / (2 * (166 + rt)), 4),
                          Math.Round(fyVal / 2, 4)
                      )
                    : Math.Round((co * 29_000_000) / (2 * rt), 4);

                double Cc = Math.Round(Math.Sqrt((Math.Pow(Math.PI, 2) * 29_000_000) / Fl), 4);

                temp.Add((seg, radius, seg.Thickness, rt, aVal, iVal, co, Fl, Cc, r));
            }

            // 3) second pass: prefix sum of r to get prefix-average, numerator is HeightFinal only
            tabelData2s = new List<tabelData2>(temp.Count);
            double runningRsum = 0.0;
            int revCount = 0;
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                var cur = temp[i];

                // If it's a tank row, force zeros and do NOT affect the rolling average
                if (cur.seg.SegmentType == "Tanks")
                {
                    tabelData2s.Add(new tabelData2
                    {
                        Segment = cur.seg.SegmentName,
                        Radius = 0,
                        Thickness = 0,
                        Rt = 0,
                        A = 0,
                        I = 0,
                        r = 0,
                        Co = 0,
                        Fl = 0,
                        KLr = 0,
                        Cc = 0,
                        Kf = 0,
                        Fa = 0,
                        Fb = 0
                    });
                    continue; // <-- skip KLr/Kf/Fa/Fb math
                }

                revCount++;
                // accumulate r of this segment
                runningRsum += temp[i].r;

                // average of the last revCount r’s
                double avgR = runningRsum / revCount;

                // height for *this* segment only
                double hf = temp[i].seg.HeightFinal;
                double hi = temp[i].seg.HeightInitial;

                double height = hf - hi;
                var kValue = 0;
                double klr = 0;
                if (AppState.CurrentTankType == TankType.SingleColumn)
                {
                    kValue = 2;
                    klr = Math.Round((kValue * 12 * hf) / avgR, 4);

                }
                else
                {
                    kValue = 1;
                    klr = Math.Round((kValue * 12 * (height / temp[i].r)), 4);

                }
         

                // Kf, Fa, Fb as you already do...
                double Cc = temp[i].Cc;
                double Fl = temp[i].Fl;
                double Kf = klr <= 25
                    ? 1
                    : klr <= Cc
                        ? Math.Round(1 - 0.5 * Math.Pow(klr / Cc, 2), 4)
                        : Math.Round(0.5 * Math.Pow(Cc / klr, 2), 4);

                double Fa = Math.Round((Fl / 1000) * Kf, 4);
                double Fb = Math.Round(Fl / 1000, 4);

                tabelData2s.Add(new tabelData2
                {
                    Segment = temp[i].seg.SegmentName,
                    Radius = Math.Round(temp[i].radius, 4),
                    Thickness = Math.Round(temp[i].thickness, 4),
                    Rt = temp[i].rt,
                    A = temp[i].a,
                    I = temp[i].i,
                    r = temp[i].r,
                    Co = temp[i].co,
                    Fl = Math.Round(Fl / 1000, 4),
                    KLr = klr,
                    Cc = Cc,
                    Kf = Kf,
                    Fa = Fa,
                    Fb = Fb
                });
            }

            // built the list from bottom→top, so reverse it to restore the original order:
            tabelData2s.Reverse();

            // Bind
            dataGridView1.DataSource = tabelData2s;
        }

        public IReadOnlyList<tabelData2> TableData2Results
        {
            get { return tabelData2s; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AllowableStress allowable = new AllowableStress();
            var res = allowable.ShowDialog();

            if(res == DialogResult.OK || res == DialogResult.Cancel)
            {
                LoadTable2();
            }
        }

    }
    public class tabelData2
    {
        public string Segment { get; set; }
        public double Radius { get; set; }
        public double Thickness { get; set; }
        public double Rt { get; set; }
        public double A { get; set; }
        public double I { get; set; }
        public double r { get; set; }
        public double Co { get; set; }
        public double Fl { get; set; }
        public double KLr { get; set; }
        public double Cc { get; set; }
        public double Kf { get; set; }
        public double Fa { get; set; }
        public double Fb { get; set; }
    }

}