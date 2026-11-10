using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WaterTankTool_WFA.Entity;
using WaterTankTool_WFA.Solver_Equation;

namespace WaterTankTool_WFA.Foundation_Properties
{
    public partial class RingWallProperties : Form
    {
        private readonly FoundationEquations.RingWallEquations _eq;
        private readonly WaterTankDbContext _context;

        public RingWallProperties()
        {
            InitializeComponent();
            _context = WaterTankDbContext.GetInstance();
            _eq = new FoundationEquations.RingWallEquations();
            this.Load += RingWallProperties_Load;
            MakeOutputTextBoxesReadOnly();
        }

        private void RingWallProperties_Load(object? sender, EventArgs e)
        {
            if (AppState.CurrentTankType == TankType.MultiColumn)
            {
                SetupMultiColumnUI();
                DisplayMultiColumnCalculatedData();
            }
            else
            {
                SetupSingleColumnUI();
                DisplaySingleColumnCalculatedData();
            }
        }

        private void SetupMultiColumnUI()
        {
            this.Text = "Multi-Leg Pedestal Design Results";
            this.Size = new Size(880, 560);
            tabControl1.Visible = false;

            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            this.Controls.Add(pnl);

            int yLeft = 14;
            int yRight = 14;
            int col1 = 15;
            int col2 = 445;

            _txtNumLegs = AddResultRow(pnl, "Number of Legs:", "", ref yLeft, false, col1);
            _txtPedClass = AddResultRow(pnl, "Pedestal Class (lu/b_min):", "", ref yLeft, false, col1);
            _txtSlender = AddResultRow(pnl, "Slenderness (klu/r):", "", ref yLeft, false, col1);
            _txtPuTotal = AddResultRow(pnl, "Axial Pu (kip):", "", ref yLeft, false, col1);
            _txtPgravity = AddResultRow(pnl, "Gravity Axial Load/Leg (kip):", "", ref yLeft, false, col1);
            _txtMuTotal = AddResultRow(pnl, "Moment Mu (k-ft):", "", ref yLeft, false, col1);
            _txtTleg = AddResultRow(pnl, "Overturning Tension/Leg (kip):", "", ref yLeft, false, col1);
            _txtPcompTotal = AddResultRow(pnl, "Max Compression Load (kip):", "", ref yLeft, false, col1);
            _txtBearingCap = AddResultRow(pnl, "Bearing Capacity φPn (kip):", "", ref yLeft, false, col1);
            _txtBearingDC = AddResultRow(pnl, "Bearing D/C Ratio:", "", ref yLeft, false, col1);
            _txtAsMin = AddResultRow(pnl, "Min Reinforcement As,min (in2):", "", ref yLeft, false, col1);
            _txtAsProv = AddResultRow(pnl, "Provided As (in2):", "", ref yLeft, false, col1);
            _txtPhiPn = AddResultRow(pnl, "Axial Compression φPn (kip):", "", ref yLeft, false, col1);
            _txtAxialDC = AddResultRow(pnl, "Axial D/C Ratio:", "", ref yLeft, false, col1);

            _txtPhiUsed = AddResultRow(pnl, "Transverse Reinf. Factor φ:", "", ref yRight, false, col2);
            _txtUpliftCap = AddResultRow(pnl, "Tensile Capacity φTn (kip):", "", ref yRight, false, col2);
            _txtUpliftDC = AddResultRow(pnl, "Uplift D/C Ratio:", "", ref yRight, false, col2);
            _txtPM_CbAb = AddResultRow(pnl, "Balanced cb / ab (in):", "", ref yRight, false, col2);
            _txtPM_Asb = AddResultRow(pnl, "Balanced Steel Asb (in2):", "", ref yRight, false, col2);
            _txtPM_Mnb = AddResultRow(pnl, "Balanced Moment Mnb (kip-ft):", "", ref yRight, false, col2);
            _txtPM_PhiMn = AddResultRow(pnl, "Design Moment φMnb (kip-ft):", "", ref yRight, false, col2);
            _txtPM_Mu = AddResultRow(pnl, "Pedestal Moment Mu,ped (k-ft):", "", ref yRight, false, col2);
            _txtPM_DC = AddResultRow(pnl, "P-M D/C Ratio:", "", ref yRight, false, col2);
            _txtVuTotal = AddResultRow(pnl, "Shear Vu (kip):", "", ref yRight, false, col2);
            _txtShearPed = AddResultRow(pnl, "Shear Vu,ped (kip):", "", ref yRight, false, col2);
            _txtTieSpacing = AddResultRow(pnl, "Tie Spacing s_max (in):", "", ref yRight, false, col2);
            _txtFootingLd = AddResultRow(pnl, "Development Length ld (in):", "", ref yRight, false, col2);
            _txtFootingLdProv = AddResultRow(pnl, "Provided Dev. Length (in):", "", ref yRight, false, col2);
            _txtFootingDC = AddResultRow(pnl, "Development D/C Ratio:", "", ref yRight, false, col2);
        }

        private TextBox _txtNumLegs = null!, _txtPedClass = null!, _txtSlender = null!, _txtPgravity = null!, _txtTleg = null!, _txtPcompTotal = null!;
        private TextBox _txtPuTotal = null!, _txtMuTotal = null!, _txtVuTotal = null!, _txtPhiUsed = null!;
        private TextBox _txtBearingCap = null!, _txtBearingDC = null!, _txtAsMin = null!, _txtAsProv = null!;
        private TextBox _txtPhiPn = null!, _txtAxialDC = null!;
        private TextBox _txtUpliftCap = null!, _txtUpliftDC = null!;
        private TextBox _txtPM_CbAb = null!, _txtPM_Asb = null!, _txtPM_Mnb = null!, _txtPM_Mu = null!, _txtPM_PhiMn = null!, _txtPM_DC = null!;
        private TextBox _txtShearPed = null!, _txtTieSpacing = null!;
        private TextBox _txtFootingLd = null!, _txtFootingLdProv = null!, _txtFootingDC = null!;

        // Single-Column Results Controls (Steps 1 - 14 of Ringwallsinglelegdesign.pdf)
        private TextBox _scStep1Loads = null!, _scStep2Diams = null!, _scStep2Radii = null!, _scStep2Dims = null!, _scStep2VolWt = null!;
        private TextBox _scStep3Reactions = null!, _scStep3NoSw = null!, _scStep3Sw = null!, _scStep3WithSw = null!;
        private TextBox _scStep4Area = null!, _scStep4Stress = null!;
        private TextBox _scStep5VertBars = null!, _scStep5VertRatio = null!;
        private TextBox _scStep6HoopBars = null!, _scStep6HoopMin = null!;

        private TextBox _scStep7SoilPress = null!, _scStep7Unfact = null!, _scStep7Fact = null!;
        private TextBox _scStep8Ecc = null!;
        private TextBox _scStep9Geom = null!, _scStep9Klr = null!;
        private TextBox _scStep10Pn = null!, _scStep10PhiPn = null!, _scStep10Dcr = null!;
        private TextBox _scStep11HoopSoil = null!;
        private TextBox _scStep12Params = null!, _scStep12Shear = null!;
        private TextBox _scStep13Iter = null!, _scStep13Cap = null!, _scStep13Dcr = null!;

        private DataGridView _dgvStep14PM = null!;
        private TextBox _scStep14Summary = null!;

        private void SetupSingleColumnUI()
        {
            this.Text = "Single-Leg Ring Wall Foundation Design Results";
            this.Size = new Size(1355, 565);
            this.CenterToScreen();
            tabControl1.Visible = false;

            Panel pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            this.Controls.Add(pnl);

            int yL = 12;
            int yM = 12;
            int col1 = 12;
            int col2 = 462;
            int col3 = 915;

            // Column 1: Geometry, Line Loads, Axial Stress & Reinforcement
            _scStep1Loads = AddScResultRow(pnl, "Loads Pu / Mu / Vu:", ref yL, col1);
            _scStep2Diams = AddScResultRow(pnl, "Diameters D_CL / Do / Di (ft):", ref yL, col1);
            _scStep2Radii = AddScResultRow(pnl, "Radii R_CL / Ro / Ri (ft):", ref yL, col1);
            _scStep2Dims = AddScResultRow(pnl, "Wall tw / Hw / Min Edge L:", ref yL, col1);
            _scStep2VolWt = AddScResultRow(pnl, "Area Aw / Vol Vw / Wt Ww:", ref yL, col1);
            _scStep3Reactions = AddScResultRow(pnl, "Reactions w0 / wM (k/ft):", ref yL, col1);
            _scStep3NoSw = AddScResultRow(pnl, "Line Load (No-SW) wmax / wmin:", ref yL, col1);
            _scStep3Sw = AddScResultRow(pnl, "Perim C / w_sw / w_sw,u:", ref yL, col1);
            _scStep3WithSw = AddScResultRow(pnl, "Line Load w_max / w_min:", ref yL, col1);
            _scStep4Area = AddScResultRow(pnl, "Strip Gross Area Ag (1-ft):", ref yL, col1);
            _scStep4Stress = AddScResultRow(pnl, "Axial Stress σmax (SW / No-SW):", ref yL, col1);
            _scStep5VertBars = AddScResultRow(pnl, "Vert Bars / Spacing sv / As,v:", ref yL, col1);
            _scStep5VertRatio = AddScResultRow(pnl, "Vert Ratio ρv vs As,min:", ref yL, col1);
            _scStep6HoopBars = AddScResultRow(pnl, "Hoop Bars / Spacing sh / ρh:", ref yL, col1);
            _scStep6HoopMin = AddScResultRow(pnl, "Hoop Ash,min / Req sh,max:", ref yL, col1);

            // Column 2: Soil Pressure, Eccentricity, Slenderness k=1 & k=2, Hoop Force, Shear & Combined P-M
            _scStep7SoilPress = AddScResultRow(pnl, "Soil pa(z) / Base pa,max / y:", ref yM, col2);
            _scStep7Unfact = AddScResultRow(pnl, "Unfactored Soil Fa / Ma:", ref yM, col2);
            _scStep7Fact = AddScResultRow(pnl, "Factored (1.6H) Fa,u / Ma,u:", ref yM, col2);
            _scStep8Ecc = AddScResultRow(pnl, "Eccentricity e vs e_lim (tw/6):", ref yM, col2);
            _scStep9Geom = AddScResultRow(pnl, "Slenderness λh (lc/h) / r (in):", ref yM, col2);
            _scStep9Klr = AddScResultRow(pnl, "Ratio klc/r (k=1.0 / k=2.0):", ref yM, col2);
            _scStep10Pn = AddScResultRow(pnl, "Nominal Pn (k=1.0 / k=2.0):", ref yM, col2);
            _scStep10PhiPn = AddScResultRow(pnl, "Design φPn (k=1.0 / k=2.0):", ref yM, col2);
            _scStep10Dcr = AddScResultRow(pnl, "Axial DCR (k=1.0 / k=2.0):", ref yM, col2);
            _scStep11HoopSoil = AddScResultRow(pnl, "Soil Hoop Nθ,max / Stress σθ:", ref yM, col2);
            _scStep12Params = AddScResultRow(pnl, "Shear d / ρw / λs / Nu/6Ag:", ref yM, col2);
            _scStep12Shear = AddScResultRow(pnl, "Out-of-Plane Vc / φVc / DCRv:", ref yM, col2);
            _scStep13Iter = AddScResultRow(pnl, "Iterated NA c / a / Strain εt:", ref yM, col2);
            _scStep13Cap = AddScResultRow(pnl, "Strip Pn / Mn / Design φMn:", ref yM, col2);
            _scStep13Dcr = AddScResultRow(pnl, "Combined Axial-Flex DCR_M:", ref yM, col2);

            // Column 3: ACI Axial-Flexural (P-M) Interaction Table
            GroupBox grpPM = new GroupBox
            {
                Text = "ACI Axial-Flexural (P-M) Interaction Check (1-ft Strip)",
                Location = new Point(col3, 10),
                Size = new Size(402, 496)
            };
            pnl.Controls.Add(grpPM);

            _dgvStep14PM = new DataGridView
            {
                Location = new Point(10, 24),
                Size = new Size(382, 345),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _dgvStep14PM.Columns.Add("colC", "c (in)");
            _dgvStep14PM.Columns.Add("colPn", "Pn (kip)");
            _dgvStep14PM.Columns.Add("colMn", "Mn (kip-ft)");
            _dgvStep14PM.Columns.Add("colPhiPn", "φPn (kip)");
            _dgvStep14PM.Columns.Add("colPhiMn", "φMn (kip-ft)");
            foreach (DataGridViewColumn col in _dgvStep14PM.Columns)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            grpPM.Controls.Add(_dgvStep14PM);

            Label lblPmSum = new Label
            {
                Text = "Governing P-M Interaction Demand vs. Capacity:",
                Location = new Point(10, 378),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            grpPM.Controls.Add(lblPmSum);

            _scStep14Summary = new TextBox
            {
                Location = new Point(10, 400),
                Size = new Size(382, 84),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.LightGreen,
                Font = new Font("Consolas", 9.0F, FontStyle.Bold)
            };
            grpPM.Controls.Add(_scStep14Summary);
        }

        private TextBox AddScResultRow(Panel pnl, string labelText, ref int y, int xOffset)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(xOffset, y + 3), AutoSize = true };
            TextBox txt = new TextBox
            {
                Location = new Point(xOffset + 202, y),
                Size = new Size(236, 23),
                ReadOnly = true,
                BackColor = Color.White
            };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            txt.BringToFront();
            y += 33;
            return txt;
        }

        private void DisplaySingleColumnCalculatedData()
        {
            var entity = _context.Set<RingWallEntity>().FirstOrDefault();
            var anchorBolt = _context.AnchorBoltEntity?.FirstOrDefault();
            var basePlate = _context.BasePlateEntity?.FirstOrDefault();
            var loadService = new Services.LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

            bool isUpdatedSchema = entity != null && entity.ScQa.HasValue;

            double pVal = (isUpdatedSchema && entity!.ScPu.HasValue && entity.ScPu.Value > 0)
                ? entity.ScPu.Value
                : (puGov > 0 ? puGov : (basePlate?.Pu ?? 1653.556));
            double muVal = (isUpdatedSchema && entity!.ScMu.HasValue && entity.ScMu.Value > 0)
                ? entity.ScMu.Value
                : (muGov > 0 ? muGov : (basePlate?.OverturningMoment ?? anchorBolt?.Mu ?? 2903.3789));
            double vuVal = (isUpdatedSchema && entity!.ScVu.HasValue && entity.ScVu.Value > 0)
                ? entity.ScVu.Value
                : (vuGov > 0 ? vuGov : ((anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : 53.43));

            double coneDiamFt = 21.0;
            if (basePlate != null && basePlate.Dbp > 0)
            {
                coneDiamFt = basePlate.Dbp;
            }
            else
            {
                var bottomSeg = _context.SegmentProperties?.OrderBy(s => s.HeightInitial).FirstOrDefault();
                if (bottomSeg != null && (bottomSeg.DiameterFinal ?? bottomSeg.Diameter) > 0)
                    coneDiamFt = bottomSeg.DiameterFinal ?? bottomSeg.Diameter;
                else if (anchorBolt != null && anchorBolt.Rb > 0)
                    coneDiamFt = Math.Round(2.0 * anchorBolt.Rb - 0.5, 2);
            }

            double dcl = (isUpdatedSchema && entity!.ScDcl.HasValue && entity.ScDcl.Value > 0)
                ? entity.ScDcl.Value
                : coneDiamFt;
            double tw = (isUpdatedSchema && entity!.Trw > 0) ? entity.Trw : 2.0;
            double hw = (isUpdatedSchema && entity!.Hw.HasValue && entity.Hw.Value > 0) ? entity.Hw.Value : 9.0;
            double coverIn = (isUpdatedSchema && entity!.Cc > 0) ? entity.Cc : 2.0;

            double fcPsi = (isUpdatedSchema && entity!.ScFcPrime.HasValue && entity.ScFcPrime.Value > 0)
                ? (entity.ScFcPrime.Value > 100 ? entity.ScFcPrime.Value : entity.ScFcPrime.Value * 1000.0)
                : 4000.0;
            double fcKsi = fcPsi / 1000.0;
            double fyKsi = (isUpdatedSchema && entity!.ScFy.HasValue && entity.ScFy.Value > 0) ? entity.ScFy.Value : 60.0;
            double gammaC = (isUpdatedSchema && entity!.ScGammaC.HasValue && entity.ScGammaC.Value > 0) ? entity.ScGammaC.Value : 150.0;

            double gammaSoil = (isUpdatedSchema && entity!.ScGammaSoil.HasValue && entity.ScGammaSoil.Value > 0) ? entity.ScGammaSoil.Value : 125.0;
            double ka = (isUpdatedSchema && entity!.ScKa.HasValue && entity.ScKa.Value > 0) ? entity.ScKa.Value : 0.30;

            string vertBarLbl = (isUpdatedSchema && !string.IsNullOrWhiteSpace(entity!.VertBarLabel)) ? entity.VertBarLabel! : "#4";
            double vertBarArea = (isUpdatedSchema && entity!.VertBarArea.HasValue && entity.VertBarArea.Value > 0) ? entity.VertBarArea.Value : 0.20;
            int vertCount = (isUpdatedSchema && entity!.VertBarCountPerFace.HasValue && entity.VertBarCountPerFace.Value > 0)
                ? entity.VertBarCountPerFace.Value
                : (int)Math.Round(Math.PI * dcl);

            string hoopBarLbl = (isUpdatedSchema && !string.IsNullOrWhiteSpace(entity!.HoopBarLabel)) ? entity.HoopBarLabel! : "#4";
            double hoopBarArea = (isUpdatedSchema && entity!.HoopBarArea.HasValue && entity.HoopBarArea.Value > 0) ? entity.HoopBarArea.Value : 0.20;
            int hoopCount = (isUpdatedSchema && entity!.HoopBarCountPerFace.HasValue && entity.HoopBarCountPerFace.Value > 0) ? entity.HoopBarCountPerFace.Value : 8;

            // --- Step 2: Geometry of Ring Wall ---
            double doFt = _eq.OuterDiameterFt(dcl, tw);
            double diFt = _eq.InnerDiameterFt(dcl, tw);
            double roFt = _eq.OuterRadiusFromDiamFt(doFt);
            double riFt = _eq.InnerRadiusFromDiamFt(diFt);
            double rclFt = _eq.CenterlineRadiusFromDiamFt(dcl);
            double awFt2 = _eq.RingWallCrossSectionalAreaFt2(doFt, diFt);
            double vwFt3 = _eq.RingWallVolumeFt3(awFt2, hw);
            double wwKips = _eq.RingWallWeightKips(vwFt3, gammaC);
            double minEdgeL = _eq.MinimumInsideEdgeDistanceIn(dcl);

            // --- Step 3: Circumferential Vertical Load Distribution ---
            double w0 = _eq.AverageAxialLineReactionKipPerFt(pVal, rclFt);
            double wM = _eq.MomentInducedLineReactionKipPerFt(muVal, rclFt);
            double wMaxNoSw = _eq.MaxVerticalLineLoadNoSwKipPerFt(w0, wM);
            double wMinNoSw = _eq.MinVerticalLineLoadNoSwKipPerFt(w0, wM);

            double perimC = _eq.CenterlineCircumferenceFt(rclFt);
            double wSw = _eq.UnfactoredSelfWeightPerFt(tw, hw, gammaC);
            double wSwU = _eq.FactoredSelfWeightPerFt(wSw, 1.2);
            double wMax = _eq.MaxVerticalLineLoadWithSwKipPerFt(w0, wSwU, wM);
            double wMin = _eq.MinVerticalLineLoadWithSwKipPerFt(w0, wSwU, wM);

            // --- Step 4: Ring-Wall Axial Compression Check ---
            double agIn2 = _eq.GrossAreaPerFootStripIn2(tw);
            double sigmaMaxSw = _eq.AxialCompressiveStressPsi(wMax, agIn2);
            double sigmaMaxNoSw = _eq.AxialCompressiveStressPsi(wMaxNoSw, agIn2);

            // --- Step 5: Vertical Reinforcement Check ---
            double svIn = _eq.VerticalBarSpacingIn(dcl, vertCount);
            double asvIn2 = _eq.VerticalSteelAreaPerFtStripIn2(vertBarArea, svIn);
            double rhoV = _eq.VerticalReinforcementRatio(asvIn2, agIn2);
            double asvMinIn2 = _eq.MinimumVerticalSteelAreaIn2(agIn2, 0.0012);
            bool vertPass = asvIn2 >= asvMinIn2;

            // --- Step 6: Horizontal Hoop Reinforcement Check ---
            double shIn = _eq.HorizontalTributarySpacingIn(hw, hoopCount);
            double rhoH = _eq.HorizontalReinforcementRatio(hoopBarArea, tw, shIn);
            double ashMinIn2 = _eq.MinimumHorizontalSteelInTributaryStripIn2(tw, shIn, 0.0020);
            double shMaxReqIn = _eq.RequiredMaxHorizontalSpacingIn(hoopBarArea, tw, 0.0020);

            // --- Step 7: Lateral Soil Pressure Acting on Ringwall ---
            double paSlope = _eq.ActiveEarthPressureSlopePsfPerFt(ka, gammaSoil);
            double paMaxPsf = _eq.MaxActiveEarthPressurePsf(ka, gammaSoil, hw);
            double faKipFt = _eq.UnfactoredLateralSoilForceKipPerFt(ka, gammaSoil, hw);
            double ySoilFt = _eq.SoilResultantHeightFt(hw);
            double maKipFt = _eq.UnfactoredSoilCantileverMomentKipFtPerFt(faKipFt, hw);
            double fauKipFt = _eq.FactoredLateralSoilForceKipPerFt(faKipFt, 1.6);
            double mauKipFt = _eq.FactoredSoilCantileverMomentKipFtPerFt(maKipFt, 1.6);

            // --- Step 8: Load Eccentricity Check ---
            double eLimIn = _eq.MiddleThirdEccentricityLimitIn(tw);
            double eFt = _eq.AppliedLoadEccentricityFt(mauKipFt, wMax);
            double eIn = _eq.AppliedLoadEccentricityIn(mauKipFt, wMax);
            bool eccPass = eIn <= eLimIn;

            // --- Step 9 & 10: Slenderness & Axial Compression Strength (Both k = 1.0 and k = 2.0) ---
            double lambdaH = _eq.GeometricHeightToThicknessRatio(hw, tw);
            double rIn = _eq.WeakAxisRadiusOfGyrationIn(tw);
            double klr1 = _eq.SlendernessRatioKlr(1.0, hw, rIn);
            double klr2 = _eq.SlendernessRatioKlr(2.0, hw, rIn);

            double pn1 = _eq.NominalWallAxialStrengthKips(fcKsi, agIn2, 1.0, hw, tw);
            double phiPn1 = _eq.DesignWallAxialStrengthKips(pn1, 0.65);
            double dcrAxial1 = _eq.WallAxialDcr(wMax, phiPn1);

            double pn2 = _eq.NominalWallAxialStrengthKips(fcKsi, agIn2, 2.0, hw, tw);
            double phiPn2 = _eq.DesignWallAxialStrengthKips(pn2, 0.65);
            double dcrAxial2 = _eq.WallAxialDcr(wMax, phiPn2);
            bool axialPass = dcrAxial1 <= 1.0 && dcrAxial2 <= 1.0;

            // --- Step 11: Circumferential Hoop Force Due to Soil Pressure ---
            double nThetaMax = _eq.MaxCircumferentialHoopForceKipPerFt(paMaxPsf, rclFt);
            double sigmaThetaKsf = _eq.CircumferentialCompressiveStressKsf(nThetaMax, tw);
            double sigmaThetaPsi = _eq.CircumferentialCompressiveStressPsi(sigmaThetaKsf);

            // --- Step 12: Ringwall Out-of-Plane Shear Strength ---
            double dbVertIn = _eq.BarDiameterFromLabelIn(vertBarLbl);
            double dIn = _eq.WallEffectiveDepthIn(tw, coverIn, dbVertIn);
            double dPrimeIn = coverIn + (dbVertIn / 2.0);
            double bwIn = 12.0;
            double rhoW = _eq.WallShearReinforcementRatio(asvIn2, bwIn, dIn);
            double lambdaS = _eq.WallSizeEffectFactorSqrt(dIn);
            double nuOver6Ag = _eq.AxialCompressionShearTermPsi(wMax, agIn2);
            double vcKips = _eq.NominalOutOfPlaneShearKips(fcPsi, lambdaS, rhoW, nuOver6Ag, bwIn, dIn, 1.0);
            double phiVcKips = _eq.DesignOutOfPlaneShearKips(vcKips, 0.75);
            double dcrShear = _eq.OutOfPlaneShearDcr(fauKipFt, phiVcKips);
            bool shearPass = dcrShear <= 1.0;

            // --- Step 13: Combined Axial-Flexural Strength (Iterated Neutral Axis c) ---
            double hIn = tw * 12.0;
            double asFace = asvIn2 / 2.0;
            var step13 = _eq.SolveCombinedAxialFlexuralStrength(wMax, mauKipFt, bwIn, hIn, dPrimeIn, dIn, asFace, fcKsi, fyKsi, 29000.0);
            bool pmPass = step13.dcrM <= 1.0;

            // --- Populate Column 1 ---
            _scStep1Loads.Text = $"{pVal:F2} k / {muVal:F2} k-ft / {vuVal:F2} k";
            _scStep2Diams.Text = $"{dcl:F2} / {doFt:F2} / {diFt:F2}";
            _scStep2Radii.Text = $"{rclFt:F2} / {roFt:F2} / {riFt:F2}";
            _scStep2Dims.Text = $"{tw:F2} ft ({hIn:F0}\") / {hw:F2} ft / {minEdgeL:F0}\"";
            _scStep2Dims.BackColor = tw >= 1.0 ? Color.LightGreen : Color.LightCoral;
            _scStep2VolWt.Text = $"{awFt2:F2} ft² / {vwFt3:F2} ft³ / {wwKips:F2} k";
            _scStep3Reactions.Text = $"w0 = {w0:F2} / wM = {wM:F2}";
            _scStep3NoSw.Text = $"{wMaxNoSw:F2} / {wMinNoSw:F2} kip/ft";
            _scStep3Sw.Text = $"{perimC:F3} ft / {wSw:F2} / {wSwU:F2} k/ft";
            _scStep3WithSw.Text = $"{wMax:F2} / {wMin:F2} kip/ft";
            _scStep3WithSw.BackColor = wMin >= 0 ? Color.LightGreen : Color.LemonChiffon;
            _scStep4Area.Text = $"{tw:F2} ft² = {agIn2:F0} in²";
            _scStep4Stress.Text = $"{sigmaMaxSw:F1} psi / {sigmaMaxNoSw:F1} psi";
            _scStep5VertBars.Text = $"({vertCount}) {vertBarLbl} EF / {svIn:F1}\" / {asvIn2:F2} in²";
            _scStep5VertRatio.Text = $"{rhoV:F6} ({rhoV * 100.0:F3}%) > {asvMinIn2:F4} ({(vertPass ? "PASS" : "FAIL")})";
            _scStep5VertRatio.BackColor = vertPass ? Color.LightGreen : Color.LightCoral;
            _scStep6HoopBars.Text = $"({hoopCount}) {hoopBarLbl} EF / {shIn:F1}\" / {rhoH:F6}";
            _scStep6HoopMin.Text = $"Ash,min={ashMinIn2:F3} in² / sh,max={shMaxReqIn:F2}\" (PASS)";
            _scStep6HoopMin.BackColor = Color.LightGreen;

            // --- Populate Column 2 ---
            _scStep7SoilPress.Text = $"{paSlope:F1}z psf / {paMaxPsf:F1} psf / y={ySoilFt:F1}'";
            _scStep7Unfact.Text = $"Fa = {faKipFt:F3} k/ft / Ma = {maKipFt:F3} k-ft/ft";
            _scStep7Fact.Text = $"Fa,u = {fauKipFt:F3} k/ft / Ma,u = {mauKipFt:F3} k-ft/ft";
            _scStep8Ecc.Text = $"{eFt:F4}' = {eIn:F3}\" < {eLimIn:F1}\" ({(eccPass ? "PASS" : "FAIL")})";
            _scStep8Ecc.BackColor = eccPass ? Color.LightGreen : Color.LightCoral;
            _scStep9Geom.Text = $"λh = {lambdaH:F2} / r = {rIn:F3} in";
            _scStep9Klr.Text = $"{klr1:F2} (k=1.0) / {klr2:F2} (k=2.0)";
            _scStep10Pn.Text = $"{pn1:F2} k (k=1) / {pn2:F2} k (k=2)";
            _scStep10PhiPn.Text = $"{phiPn1:F2} k (k=1) / {phiPn2:F2} k (k=2)";
            _scStep10Dcr.Text = $"{dcrAxial1 * 100.0:F2}% (k=1) / {dcrAxial2 * 100.0:F2}% (k=2) {(axialPass ? "PASS" : "FAIL")}";
            _scStep10Dcr.BackColor = axialPass ? Color.LightGreen : Color.LightCoral;
            _scStep11HoopSoil.Text = $"{nThetaMax:F3} k/ft / {sigmaThetaKsf:F3} ksf ({sigmaThetaPsi:F1} psi)";
            _scStep12Params.Text = $"d={dIn:F2}\" / ρw={rhoW:F6} / λs={lambdaS:F3} / {nuOver6Ag:F2}";
            _scStep12Shear.Text = $"{vcKips:F2} k / {phiVcKips:F2} k / {dcrShear * 100.0:F1}% ({(shearPass ? "PASS" : "FAIL")})";
            _scStep12Shear.BackColor = shearPass ? Color.LightGreen : Color.LightCoral;
            _scStep13Iter.Text = $"c={step13.cIn:F2}\" / a={step13.aIn:F2}\" / εt={step13.epsT:F4} (φ={step13.phi:F2})";
            _scStep13Cap.Text = $"Pn={step13.pnKips:F2} k / Mn={step13.mnKipFt:F2} / φMn={step13.phiMnKipFt:F2} k-ft";
            _scStep13Dcr.Text = $"{step13.dcrM * 100.0:F2}% ({step13.dcrM:F3}) ({(pmPass ? "PASS" : "FAIL")})";
            _scStep13Dcr.BackColor = pmPass ? Color.LightGreen : Color.LightCoral;

            // --- Populate Column 3 (Step 14 P-M Interaction Table) ---
            _dgvStep14PM.Rows.Clear();
            double[] cSteps = new double[] { 2, 4, 6, 8, 10, 12, 16, 20, 24 };
            foreach (double cStep in cSteps)
            {
                double cActual = Math.Abs(hIn - 24.0) < 0.01 ? cStep : Math.Round(cStep * (hIn / 24.0), 2);
                var rowPM = _eq.EvaluateStripPMAtNeutralAxis(cActual, bwIn, hIn, dPrimeIn, dIn, asFace, fcKsi, fyKsi, 29000.0);
                _dgvStep14PM.Rows.Add(
                    cActual.ToString("0.##", CultureInfo.InvariantCulture),
                    rowPM.pnKips.ToString("F2", CultureInfo.InvariantCulture),
                    rowPM.mnKipFt.ToString("F2", CultureInfo.InvariantCulture),
                    rowPM.phiPnKips.ToString("F2", CultureInfo.InvariantCulture),
                    rowPM.phiMnKipFt.ToString("F2", CultureInfo.InvariantCulture)
                );
            }

            _scStep14Summary.Text =
                $"Demand  (Pu, Mu)   = ({wMax:F2} kip, {mauKipFt:F3} kip-ft)\r\n" +
                $"Capacity (φPn, φMn) = ({wMax:F2} kip, {step13.phiMnKipFt:F2} kip-ft)\r\n" +
                $"DCR_P-M = {mauKipFt:F3} / {step13.phiMnKipFt:F2} = {step13.dcrM:F3} ({step13.dcrM * 100.0:F1}%) ... {(pmPass ? "PASS" : "FAIL")}";
            _scStep14Summary.BackColor = pmPass ? Color.LightGreen : Color.LightCoral;
        }

        private TextBox AddResultRow(Panel pnl, string labelText, string initialValue, ref int y, bool isHeader = false, int xOffset = 20)
        {
            Label lbl = new Label { Text = labelText, Location = new Point(xOffset, y), AutoSize = true };
            if (isHeader)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                pnl.Controls.Add(lbl);
                y += 30;
                return null!;
            }

            TextBox txt = new TextBox
            {
                Text = initialValue,
                Location = new Point(xOffset + 205, y - 3),
                Size = new Size(125, 23),
                ReadOnly = true,
                BackColor = Color.White
            };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(txt);
            txt.BringToFront();
            y += 31;
            return txt;
        }

        private void DisplayMultiColumnCalculatedData()
        {
            AppState.Load();
            var entity = _context.Set<RingWallEntity>().FirstOrDefault();
            var anchorBolt = _context.AnchorBoltEntity.FirstOrDefault();
            var basePlate = _context.BasePlateEntity.FirstOrDefault();
            var loadService = new Services.LoadService();
            var (puGov, muGov, vuGov) = loadService.GetGoverningLoads();

            var eq = new FoundationEquations.MultiColumnPedestalEquations();

            int numLegs = AppState.NoOfColumns > 1
                ? AppState.NoOfColumns
                : ((anchorBolt?.Ns.HasValue == true && anchorBolt.Ns.Value > 0) ? anchorBolt.Ns.Value : 4);

            double puTotal = (anchorBolt?.Pu.HasValue == true && anchorBolt.Pu.Value > 0) ? anchorBolt.Pu.Value : puGov;
            if (puTotal <= 0) puTotal = 1637.59;

            double muTotal = (basePlate?.OverturningMoment.HasValue == true && basePlate.OverturningMoment.Value > 0)
                ? basePlate.OverturningMoment.Value
                : ((anchorBolt?.Mu.HasValue == true && anchorBolt.Mu.Value > 0) ? anchorBolt.Mu.Value : muGov);
            if (muTotal <= 0) muTotal = 5666.30;

            double vuTotal = (anchorBolt != null && anchorBolt.Vu > 0) ? anchorBolt.Vu : vuGov;
            if (vuTotal <= 0) vuTotal = 57.22;

            _txtNumLegs.Text = numLegs.ToString(CultureInfo.InvariantCulture);
            _txtPuTotal.Text = puTotal.ToString("F2", CultureInfo.InvariantCulture);
            _txtMuTotal.Text = muTotal.ToString("F2", CultureInfo.InvariantCulture);
            _txtVuTotal.Text = vuTotal.ToString("F3", CultureInfo.InvariantCulture);

            double autoB = (anchorBolt?.PedestalSize.HasValue == true && anchorBolt.PedestalSize.Value > 0) ? anchorBolt.PedestalSize.Value : 39.0;
            double autoL = (anchorBolt?.PedestalLength.HasValue == true && anchorBolt.PedestalLength.Value > 0) ? anchorBolt.PedestalLength.Value : autoB;

            double B = (entity?.PedestalSizeB.HasValue == true && entity.PedestalSizeB.Value > 0) ? entity.PedestalSizeB.Value : autoB;
            double L = (entity?.PedestalSizeL.HasValue == true && entity.PedestalSizeL.Value > 0) ? entity.PedestalSizeL.Value : autoL;
            double bMin = Math.Min(B, L);
            double Hp = (entity?.Hp.HasValue == true && entity.Hp.Value > 0) ? entity.Hp.Value : 7.417; // ft

            double basePlateArea = 706.86;
            if (basePlate != null && basePlate.Ro > 0)
            {
                basePlateArea = Math.PI * Math.Pow(basePlate.Ro, 2) / 4.0;
            }

            double fcPrime = (entity?.FcPrime.HasValue == true && entity.FcPrime.Value > 0) ? entity.FcPrime.Value : 4.0;
            double fy = (entity?.Fy.HasValue == true && entity.Fy.Value > 0) ? entity.Fy.Value : 60.0;
            double phiComp = (entity?.PhiCompression.HasValue == true && entity.PhiCompression.Value > 0) ? entity.PhiCompression.Value : 0.65;
            bool isSpiral = Math.Abs(phiComp - 0.75) < 0.01;
            double maxAxialFactor = isSpiral ? 0.85 : 0.80;
            _txtPhiUsed.Text = isSpiral ? "0.75 (Spirals)" : "0.65 (Tied)";

            double gammaC = (entity?.GammaC.HasValue == true && entity.GammaC.Value > 0) ? entity.GammaC.Value : 150.0;
            double AsProv = (entity?.PedestalAsProv.HasValue == true && entity.PedestalAsProv.Value > 0) ? entity.PedestalAsProv.Value : 7.92;

            // Step 1A: Pedestal Classification (lu / b_min <= 3.0)
            double lu_b = (Hp * 12.0) / bMin;
            _txtPedClass.Text = lu_b.ToString("F2", CultureInfo.InvariantCulture) + " <= 3.0";
            _txtPedClass.BackColor = eq.IsPedestal(Hp * 12.0, bMin) ? Color.LightGreen : Color.LightCoral;

            // Step 1B: Structural Slenderness (k * lu / r <= 22)
            double r = eq.RadiusOfGyrationSquare(bMin);
            double klu_r = (2.0 * (Hp * 12.0)) / r;
            _txtSlender.Text = klu_r.ToString("F2", CultureInfo.InvariantCulture) + " <= 22";
            _txtSlender.BackColor = eq.NeglectSlenderness(2.0, Hp * 12.0, r) ? Color.LightGreen : Color.LightCoral;

            // Step 2: Gravity Axial Load per Leg & Pedestal Self-Weight
            double pGrav = eq.GravityAxialLoadPerLeg(puTotal, numLegs);
            _txtPgravity.Text = pGrav.ToString("F2", CultureInfo.InvariantCulture);

            // Overturning Tension per Leg (from Anchor Bolt geometry)
            double coneDiamFt = (anchorBolt?.Dcone.HasValue == true && anchorBolt.Dcone.Value > 0) ? anchorBolt.Dcone.Value : 41.0469;
            double coneRadius = coneDiamFt * 12.0 / 2.0;
            var anchorEq = new FoundationEquations.MultiColumnAnchorBoltEquations();
            double totalTension = anchorEq.TotalOverturningTension(muTotal, coneRadius);
            double tLeg = anchorEq.TensionPerLeg(totalTension, anchorEq.TensionLegs(numLegs));
            if (tLeg < 0) tLeg = 0;
            _txtTleg.Text = tLeg.ToString("F2", CultureInfo.InvariantCulture);

            double wPed = eq.PedestalSelfWeight(B, L, Hp, gammaC);
            double wPedFactored = eq.FactoredPedestalSelfWeight(wPed);
            double pCompTotal = pGrav + wPedFactored;
            _txtPcompTotal.Text = pCompTotal.ToString("F2", CultureInfo.InvariantCulture);

            // Step 4: Pedestal Concrete Bearing Check
            double pedestalArea = B * L;
            double enhancement = eq.BearingEnhancementFactor(basePlateArea, pedestalArea);
            double bearingCap = eq.PedestalBearingCapacity(fcPrime, basePlateArea, enhancement, 0.65);
            _txtBearingCap.Text = bearingCap.ToString("F2", CultureInfo.InvariantCulture);

            double bearingDC = eq.DemandCapacityRatio(pCompTotal, bearingCap);
            SetRatioBox(_txtBearingDC, bearingDC);

            // Step 5: Pedestal Longitudinal Reinforcement
            double asMin = eq.MinimumReinforcementArea(pedestalArea);
            _txtAsMin.Text = asMin.ToString("F3", CultureInfo.InvariantCulture);
            _txtAsProv.Text = AsProv.ToString("F2", CultureInfo.InvariantCulture);
            _txtAsProv.BackColor = AsProv >= asMin ? Color.LightGreen : Color.LightCoral;

            // Step 6: Axial Compression Capacity
            double po = eq.NominalConcentricCompressionStrength(fcPrime, pedestalArea, AsProv, fy);
            double phiPn = eq.DesignAxialCompressionCapacity(po, phiComp, maxAxialFactor);
            _txtPhiPn.Text = phiPn.ToString("F2", CultureInfo.InvariantCulture);

            double axialDC = eq.DemandCapacityRatio(pCompTotal, phiPn);
            SetRatioBox(_txtAxialDC, axialDC);

            // Step 7: Pedestal Tensile Capacity Check
            double phiTn = eq.DesignTensileCapacity(AsProv, fy, 0.90);
            _txtUpliftCap.Text = phiTn.ToString("F2", CultureInfo.InvariantCulture);

            double upliftDC = eq.DemandCapacityRatio(tLeg, phiTn);
            SetRatioBox(_txtUpliftDC, upliftDC);

            // Step 9: Horizontal Shear per Pedestal & Tie Spacing (computed before Step 8 moment demand)
            double vuPed = eq.HorizontalShearPerPedestal(vuTotal, numLegs);
            _txtShearPed.Text = vuPed.ToString("F3", CultureInfo.InvariantCulture);

            double tieSpacing = eq.RequiredTieSpacing(1.0, bMin);
            _txtTieSpacing.Text = tieSpacing.ToString("F1", CultureInfo.InvariantCulture);

            // Step 8: Pedestal P-M Balanced Section Check (ACI 318 / Professor's Method)
            double cbVal, abVal, asbVal, mnbVal;
            double phiMnb = eq.BalancedSectionMomentCapacity(B, L, 3.5, fcPrime, fy, phiComp, out cbVal, out abVal, out asbVal, out mnbVal);
            _txtPM_CbAb.Text = $"{cbVal.ToString("F2", CultureInfo.InvariantCulture)} / {abVal.ToString("F2", CultureInfo.InvariantCulture)}";
            _txtPM_Asb.Text = asbVal.ToString("F2", CultureInfo.InvariantCulture);
            _txtPM_Mnb.Text = mnbVal.ToString("F1", CultureInfo.InvariantCulture);
            _txtPM_PhiMn.Text = phiMnb.ToString("F1", CultureInfo.InvariantCulture);

            double pedMu = Math.Round(vuPed * Hp, 2); // Shear at top of pedestal * clear height Hp
            _txtPM_Mu.Text = pedMu.ToString("F2", CultureInfo.InvariantCulture);

            double pmDC = eq.DemandCapacityRatio(pedMu, phiMnb);
            SetRatioBox(_txtPM_DC, pmDC);

            // Step 10: Pedestal-to-Footing Development Length
            double ld = eq.TensionDevelopmentLength(fy, fcPrime, 1.0);
            _txtFootingLd.Text = ld.ToString("F2", CultureInfo.InvariantCulture);

            double lProv = entity?.FootingAsProv ?? 30.0;
            if (Math.Abs(lProv - 5.28) < 0.01) lProv = 30.0;
            _txtFootingLdProv.Text = lProv.ToString("F2", CultureInfo.InvariantCulture);

            double footingDC = eq.DemandCapacityRatio(ld, lProv);
            SetRatioBox(_txtFootingDC, footingDC);
        }

        private void SetRatioBox(TextBox textBox, double ratio)
        {
            textBox.Text = ratio.ToString("0.#####", CultureInfo.InvariantCulture);
            textBox.BackColor = ratio <= 1.0 ? Color.LightGreen : Color.LightCoral;
        }

        public RingWallProperties(double trw, double B, double Rcl, double tedge, double cc,
            double vWater, double gammaW, double wTank, double wSup, double gammaC,
            double qAllowInput, double fc, double fy, double lambda, double Es, double As,
            double Mu, double Abar, double sProv, double Vu, double b0, double Vup,
            double Bu, double A1, double A2, bool bearingEnhancementPermitted,
            string columnLocation = "Interior", double phiShear = 0.75, double phiBearing = 0.65)
        {
            InitializeComponent();
            _eq = new FoundationEquations.RingWallEquations();
            MakeOutputTextBoxesReadOnly();
        }

        private void MakeOutputTextBoxesReadOnly()
        {
            foreach (Control control in GetAllControls(this))
            {
                if (control is TextBox tb)
                {
                    tb.ReadOnly = true;
                    tb.TabStop = false;
                    tb.BackColor = Color.White;
                }
            }
        }

        private Control[] GetAllControls(Control parent)
        {
            var controls = new System.Collections.Generic.List<Control>();
            foreach (Control control in parent.Controls)
            {
                controls.Add(control);
                if (control.HasChildren) controls.AddRange(GetAllControls(control));
            }
            return controls.ToArray();
        }
    }
}
