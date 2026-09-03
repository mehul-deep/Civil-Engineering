using System;

namespace WaterTankTool_WFA.Solver_Equation
{
    public class FoundationEquations
    {
        public class AnchorBoltEquations
        {
            // 1) Bolt cross-sectional area
            public double CrossSectionalArea(double nominalBoltDiameter)
            {
                return Math.Round((Math.PI * Math.Pow(nominalBoltDiameter, 2)) / 4.0, 5);
            }

            // 2) Hole area
            public double HoleArea(double boltHoleDiameter)
            {
                return Math.Round((Math.PI * Math.Pow(boltHoleDiameter, 2)) / 4.0, 5);
            }

            // 3) Bolt angular spacing around full circle
            public double BoltAngularSpacing(int totalNoOfAnchorBolts)
            {
                if (totalNoOfAnchorBolts <= 0) return 0;
                return Math.Round(360.0 / totalNoOfAnchorBolts, 5);
            }

            // 4) Angle of each bolt
            public double BoltAngle(double startAngleFirstBoltDeg, int boltNumber, int totalNoOfAnchorBolts)
            {
                return Math.Round(
                    startAngleFirstBoltDeg + ((boltNumber - 1) * BoltAngularSpacing(totalNoOfAnchorBolts)),
                    5);
            }

            // 5) Coordinates of each anchor bolt
            public double BoltXCoordinate(double boltCircleRadius, double boltAngleDeg)
            {
                double angleRad = DegreeToRadian(boltAngleDeg);
                return Math.Round(boltCircleRadius * Math.Cos(angleRad), 5);
            }

            public double BoltYCoordinate(double boltCircleRadius, double boltAngleDeg)
            {
                double angleRad = DegreeToRadian(boltAngleDeg);
                return Math.Round(boltCircleRadius * Math.Sin(angleRad), 5);
            }

            // 6) Arc spacing between adjacent bolts
            public double ArcSpacing(double boltCircleRadius, int totalNoOfAnchorBolts)
            {
                if (totalNoOfAnchorBolts <= 0) return 0;

                double thetaRad = (2.0 * Math.PI) / totalNoOfAnchorBolts;
                return Math.Round(boltCircleRadius * thetaRad, 5);
            }

            // 7) Chord spacing between adjacent bolts
            public double ChordSpacing(double boltCircleRadius, int totalNoOfAnchorBolts)
            {
                if (totalNoOfAnchorBolts <= 0) return 0;

                return Math.Round(
                    2.0 * boltCircleRadius * Math.Sin(Math.PI / totalNoOfAnchorBolts),
                    5);
            }

            // 8) Number of bolts per segment
            public double BoltsPerSegment(int totalNoOfAnchorBolts, int noOfSegments)
            {
                if (noOfSegments <= 0) return 0;
                return Math.Round((double)totalNoOfAnchorBolts / noOfSegments, 5);
            }

            public bool BoltsPerSegmentIsInteger(int totalNoOfAnchorBolts, int noOfSegments)
            {
                if (noOfSegments <= 0) return false;
                return totalNoOfAnchorBolts % noOfSegments == 0;
            }

            // 9) Bolt hole clear edge distance check
            // e_clear = e - dh/2
            public double ClearEdgeDistance(double edgeDistance, double boltHoleDiameter)
            {
                return Math.Round(edgeDistance - (boltHoleDiameter / 2.0), 5);
            }

            // 10) Tension demand per bolt
            // Tu = Mu / (0.67 * D)
            public double TotalTensionDemandFromMoment(double overturningMoment, double boltCircleDiameter)
            {
                if (boltCircleDiameter <= 0) return 0;
                double leverArm = 0.67 * boltCircleDiameter;
                return Math.Round(overturningMoment / leverArm, 5);
            }

            // Tb = 2 * Tu / Nb (Circular anchor group assumption)
            public double TensionDemandPerBolt_CircularGroup(double totalTensionDemand, int totalNoOfAnchorBolts)
            {
                if (totalNoOfAnchorBolts <= 0) return 0;
                return Math.Round((2.0 * totalTensionDemand) / totalNoOfAnchorBolts, 5);
            }

            // Tb = Tu / Nb
            public double TensionDemandPerBolt_Equal(double totalTensionDemand, int totalNoOfAnchorBolts)
            {
                if (totalNoOfAnchorBolts <= 0) return 0;
                return Math.Round(totalTensionDemand / totalNoOfAnchorBolts, 5);
            }

            // Tb = Tu / Neffi
            public double TensionDemandPerBolt_Effective(double totalTensionDemand, int effectiveNoOfBoltsInTension)
            {
                if (effectiveNoOfBoltsInTension <= 0) return 0;
                return Math.Round(totalTensionDemand / effectiveNoOfBoltsInTension, 5);
            }

            // 11) Shear demand per bolt
            // Vb = Vu / Nb
            public double ShearDemandPerBolt(double totalShearDemand, int totalNoOfAnchorBolts)
            {
                if (totalNoOfAnchorBolts <= 0) return 0;
                return Math.Round(totalShearDemand / totalNoOfAnchorBolts, 5);
            }

            // 12) Tensile capacity of one bolt
            // Pnt = Ab * Fnt
            public double TensileCapacityBasic(double nominalBoltDiameter, double Fnt)
            {
                double Ab = CrossSectionalArea(nominalBoltDiameter);
                return Math.Round(Ab * Fnt, 5);
            }

            // φPnt = φ * Ab * Fnt
            public double TensileDesignStrength(double nominalBoltDiameter, double Fnt, double phi)
            {
                double Ab = CrossSectionalArea(nominalBoltDiameter);
                return Math.Round(phi * Ab * Fnt, 5);
            }

            // φPnt = φ * Ab * Fu
            public double TensileDesignStrengthUltimate(double nominalBoltDiameter, double Fu, double phi)
            {
                double Ab = CrossSectionalArea(nominalBoltDiameter);
                return Math.Round(phi * Ab * Fu, 5);
            }

            // 13) Shear capacity of one bolt
            // Fnv = 0.6 * Fu
            // φVn = φ * Ab * Fnv
            public double ShearDesignStrengthUltimate(double nominalBoltDiameter, double Fu, double phi)
            {
                double Ab = CrossSectionalArea(nominalBoltDiameter);
                double Fnv = 0.6 * Fu;
                return Math.Round(phi * Ab * Fnv, 5);
            }

            // Vn = Ab * Fnv
            public double ShearCapacityBasic(double nominalBoltDiameter, double Fnv)
            {
                double Ab = CrossSectionalArea(nominalBoltDiameter);
                return Math.Round(Ab * Fnv, 5);
            }

            // Optional design strength
            public double ShearDesignStrength(double nominalBoltDiameter, double Fnv, double phi)
            {
                double Ab = CrossSectionalArea(nominalBoltDiameter);
                return Math.Round(phi * Ab * Fnv, 5);
            }

            // 14) Interaction check
            public double InteractionCheck(double tensionDemandPerBolt, double tensileStrength,
                                           double shearDemandPerBolt, double shearStrength)
            {
                if (tensileStrength <= 0 || shearStrength <= 0) return 0;
                return Math.Round(
                    (tensionDemandPerBolt / tensileStrength) + (shearDemandPerBolt / shearStrength),
                    5);
            }

            // 15) Concrete Breakout Capacity (ACI 318)
            // Nb = kc * sqrt(f'c) * hef^1.5
            public double ConcreteBreakoutStrength(double kc, double fcPrime, double hef)
            {
                return Math.Round(kc * Math.Sqrt(fcPrime) * Math.Pow(hef, 1.5), 5);
            }

            // 16) Concrete Breakout Utilization
            public double ConcreteBreakoutUtilization(double tensionDemandPerBolt, double breakoutCapacity, double phi)
            {
                if (breakoutCapacity <= 0) return 0;
                return Math.Round(tensionDemandPerBolt / (phi * breakoutCapacity), 5);
            }

            public bool InteractionPass(double interactionValue)
            {
                return interactionValue <= 1.0;
            }

            private double DegreeToRadian(double degree)
            {
                return degree * Math.PI / 180.0;
            }
        }

        public class BasePlateEquations
        {
            // Gross area, Ag
            // Ag = (theta / 360) * pi * (Ro^2 - Ri^2)
            public double GrossArea(double outsideRadius, double insideRadius, double segmentAngleDeg)
            {
                double area = (segmentAngleDeg / 360.0) * Math.PI *
                              (Math.Pow(outsideRadius, 2) - Math.Pow(insideRadius, 2));

                return Math.Round(area, 5);
            }

            // Net area, An
            // An = Ag - Nh * ((pi * dh^2)/4) * (1/144)
            public double NetArea(double outsideRadius, double insideRadius, double segmentAngleDeg,
                                  int noOfBoltHoles, double boltHoleDiameterIn)
            {
                double Ag = GrossArea(outsideRadius, insideRadius, segmentAngleDeg);
                double oneHoleAreaFt2 = ((Math.PI * Math.Pow(boltHoleDiameterIn, 2)) / 4.0) * (1.0 / 144.0);

                double An = Ag - (noOfBoltHoles * oneHoleAreaFt2);

                return Math.Round(An, 5);
            }

            // Volume, V = Ag * (t/12)
            public double Volume(double outsideRadius, double insideRadius, double segmentAngleDeg, double thicknessIn)
            {
                double Ag = GrossArea(outsideRadius, insideRadius, segmentAngleDeg);
                double volume = Ag * (thicknessIn / 12.0);

                return Math.Round(volume, 5);
            }

            // Weight per segment, Wbp = Ag * (t/12) * (rs/1000)
            public double WeightPerSegment(double outsideRadius, double insideRadius, double segmentAngleDeg,
                                           double thicknessIn, double steelUnitWeight)
            {
                double Ag = GrossArea(outsideRadius, insideRadius, segmentAngleDeg);
                double weight = Ag * (thicknessIn / 12.0) * (steelUnitWeight / 1000.0);

                return Math.Round(weight, 5);
            }

            // Total weight, Wbp,total = n * Wbp
            public double TotalWeight(double weightPerSegment, int noOfSegments)
            {
                double totalWeight = noOfSegments * weightPerSegment;
                return Math.Round(totalWeight, 5);
            }

            // Outer arc length, Lo = Ro * θr
            public double OuterArcLength(double outsideRadius, double segmentAngleDeg)
            {
                double thetaRad = ThetaRadians(segmentAngleDeg);
                double length = outsideRadius * thetaRad;

                return Math.Round(length, 5);
            }

            // Inner arc length, Li = Ri * θr
            public double InnerArcLength(double insideRadius, double segmentAngleDeg)
            {
                double thetaRad = ThetaRadians(segmentAngleDeg);
                double length = insideRadius * thetaRad;

                return Math.Round(length, 5);
            }

            // θr = θ(π/180)
            public double ThetaRadians(double segmentAngleDeg)
            {
                return Math.Round(segmentAngleDeg * (Math.PI / 180.0), 5);
            }

            // Radial width, b = Ro - Ri
            public double RadialWidth(double outsideRadius, double insideRadius)
            {
                double width = outsideRadius - insideRadius;
                return Math.Round(width, 5);
            }

            // Centroid radius
            public double CentroidRadius(double outsideRadius, double insideRadius, double segmentAngleDeg)
            {
                double theta_s = segmentAngleDeg * (Math.PI / 180.0);

                if (theta_s == 0 || outsideRadius == insideRadius)
                    return 0;

                double term1 = (4.0 * Math.Sin(theta_s / 2.0)) / (3.0 * theta_s);
                double term2 = (Math.Pow(outsideRadius, 3) - Math.Pow(insideRadius, 3)) /
                               (Math.Pow(outsideRadius, 2) - Math.Pow(insideRadius, 2));

                double rBar = term1 * term2;

                return Math.Round(rBar, 5);
            }

            // β = α + θ/2
            public double CentroidAngle(double startAngleDeg, double segmentAngleDeg)
            {
                double beta = startAngleDeg + (segmentAngleDeg / 2.0);
                return Math.Round(beta, 5);
            }

            // xc = r̄ cosβ
            public double CentroidX(double outsideRadius, double insideRadius,
                                    double segmentAngleDeg, double startAngleDeg)
            {
                double rBar = CentroidRadius(outsideRadius, insideRadius, segmentAngleDeg);
                double betaDeg = CentroidAngle(startAngleDeg, segmentAngleDeg);
                double betaRad = betaDeg * (Math.PI / 180.0);

                double x = rBar * Math.Cos(betaRad);
                return Math.Round(x, 5);
            }

            // yc = r̄ sinβ
            public double CentroidY(double outsideRadius, double insideRadius,
                                    double segmentAngleDeg, double startAngleDeg)
            {
                double rBar = CentroidRadius(outsideRadius, insideRadius, segmentAngleDeg);
                double betaDeg = CentroidAngle(startAngleDeg, segmentAngleDeg);
                double betaRad = betaDeg * (Math.PI / 180.0);

                double y = rBar * Math.Sin(betaRad);
                return Math.Round(y, 5);
            }

            // --- Structural Design Equations ---

            // 1. Geometry Inputs Derived from These
            public double OuterRadius(double coneDiameterFt, double widthOfRingWallFt)
            {
                return Math.Round((coneDiameterFt / 2.0) + (widthOfRingWallFt / 4.0), 5);
            }

            public double InnerRadius(double coneDiameterFt, double widthOfRingWallFt)
            {
                return Math.Round((coneDiameterFt / 2.0) - (widthOfRingWallFt / 4.0), 5);
            }

            // 2.0. Design Against Moment
            // 2.1. Determine Factored Load and Moment
            // Convert kip-ft to kip-in
            public double ConvertMomentToKipIn(double momentKipFt)
            {
                return Math.Round(momentKipFt * 12.0, 5);
            }

            // 2.2. Determine Circumferential Moment per Unit Strip (kip-in/in)
            public double CircumferentialMomentPerUnitStrip(double momentKipIn, double coneDiameterFt)
            {
                if (coneDiameterFt <= 0) return 0;
                double mStripFt = momentKipIn / (Math.PI * coneDiameterFt); // kip-in/ft
                return Math.Round(mStripFt / 12.0, 5); // kip-in/in
            }

            // 2.3. Determine the maximum design bearing stress
            public double MaximumDesignBearingStress(double fcPrimeKsi, double x2WidthOfRingWall, double x1WidthOfBasePlate, double phi = 0.90)
            {
                if (x1WidthOfBasePlate <= 0) return 0;
                double ratio = x2WidthOfRingWall / x1WidthOfBasePlate;
                // ACI 318 limits sqrt(A2/A1) to 2.0. Also, if X1 > X2, ratio shouldn't be less than 1.0.
                if (ratio < 1.0) ratio = 1.0;
                if (ratio > 4.0) ratio = 4.0; // max sqrt is 2.0

                double sqrtRatio = Math.Sqrt(ratio);

                double fp = 0.85 * phi * fcPrimeKsi * sqrtRatio;
                double maxFp = 1.7 * fcPrimeKsi;
                return Math.Round(Math.Min(fp, maxFp), 5);
            }

            // 3. Determine Equivalent Eccentricity
            // Pstrip = P / (pi * Dcone * 12)
            // e = Mstrip / Pstrip
            public double EquivalentEccentricity(double mStripKipInPerIn, double totalVerticalLoadKips, double coneDiameterFt)
            {
                if (coneDiameterFt <= 0) return 0;
                double pStrip = totalVerticalLoadKips / (Math.PI * coneDiameterFt * 12.0);
                if (pStrip == 0) return 0;
                return Math.Round(mStripKipInPerIn / pStrip, 5);
            }

            // 3.1 Check Bearing Condition
            // N/6
            public double BearingConditionLimit(double designStripN)
            {
                return Math.Round(designStripN / 6.0, 5);
            }

            // 3.2 Determine Critical Section
            // m = B / 2
            public double CriticalSection(double designStripB)
            {
                return Math.Round(designStripB / 2.0, 5);
            }

            // 3.3 & 3.4 Determine Strip Plastic Moment
            // Approximate factored strip moment Mplu = Mstrip
            public double StripPlasticMoment(double mStrip)
            {
                return mStrip;
            }

            // 3.5 Determine Required Thickness
            public double RequiredThickness(double mPlu, double fyKsi, double phi = 0.90)
            {
                if (fyKsi <= 0 || phi <= 0) return 0;
                return Math.Round(Math.Sqrt((4.0 * mPlu) / (phi * fyKsi)), 5);
            }

            // 5.0 Local Plate Stability / Buckling Check
            // B / tp
            public double CompactnessRatio(double designStripB, double tp)
            {
                if (tp <= 0) return 0;
                return Math.Round(designStripB / tp, 5);
            }

            // Utilization Ratios
            public double Utilization(double demand, double capacity)
            {
                if (capacity <= 0) return 0;
                return Math.Round(demand / capacity, 5);
            }
        }

        public class RingWallEquations
        {
            private double R(double value)
            {
                return Math.Round(value, 5);
            }

            // ==============================
            // 3.1 Geometry Calculation
            // ==============================

            // 1) Rin = Rcl - B/2
            public double InnerRadiusOfFooting(double radiusToFoundationCenterline, double footingBaseWidth)
            {
                return R(radiusToFoundationCenterline - (footingBaseWidth / 2.0));
            }

            // 2) Rout = Rcl + B/2
            public double OuterRadiusOfFooting(double radiusToFoundationCenterline, double footingBaseWidth)
            {
                return R(radiusToFoundationCenterline + (footingBaseWidth / 2.0));
            }

            // 3) A = π(Rout² - Rin²)
            public double FootingPlanArea(double outerRadius, double innerRadius)
            {
                return R(Math.PI * (Math.Pow(outerRadius, 2) - Math.Pow(innerRadius, 2)));
            }

            // 4) Rrw = Rcl + trw/2
            public double RingWallCenterlineRadius(double radiusToFoundationCenterline, double ringWallThickness)
            {
                return R(radiusToFoundationCenterline + (ringWallThickness / 2.0));
            }

            // ==============================
            // 3.2 Service Load Calculation
            // ==============================

            // 5) Ww = Vwater / 7.48052 * γw / 1000
            public double WaterWeight(double waterVolumeGallons, double unitWeightWater)
            {
                return R((waterVolumeGallons / 7.48052) * (unitWeightWater / 1000.0));
            }

            // 8) Wf = A * tedge * γc / 1000
            public double FootingSelfWeight(double footingPlanArea, double footingEdgeThickness, double concreteUnitWeight)
            {
                return R(footingPlanArea * footingEdgeThickness * (concreteUnitWeight / 1000.0));
            }

            // 9) Vs = Ww + Wtank + Wsup + Wf
            public double TotalServiceVerticalLoad(double waterWeight, double tankDeadLoad, double superimposedLoad, double footingSelfWeight)
            {
                return R(waterWeight + tankDeadLoad + superimposedLoad + footingSelfWeight);
            }

            // 10) qallow = input / 1000
            public double AllowableSoilBearingPressure(double allowableSoilBearing)
            {
                return R(allowableSoilBearing / 1000.0);
            }

            // 11) qs = Vs / A
            public double ServiceBearingPressure(double totalServiceVerticalLoad, double footingPlanArea)
            {
                if (footingPlanArea <= 0) return 0;
                return R(totalServiceVerticalLoad / footingPlanArea);
            }

            // 12) Ub = qs / qallow
            public double BearingUtilizationRatio(double serviceBearingPressure, double allowableSoilBearingPressure)
            {
                if (allowableSoilBearingPressure <= 0) return 0;
                return R(serviceBearingPressure / allowableSoilBearingPressure);
            }

            public bool BearingPass(double bearingUtilizationRatio)
            {
                return bearingUtilizationRatio <= 1.0;
            }

            // ==============================
            // 3.3 Flexural Design
            // ==============================

            // 13) d = tedge * 12 - cc
            public double EffectiveDepth(double footingEdgeThicknessFt, double concreteCoverIn)
            {
                return R((footingEdgeThicknessFt * 12.0) - concreteCoverIn);
            }

            // 14) b = 12 in
            public double StripWidth()
            {
                return 12.0;
            }

            // 18) Beta one factor
            public double BetaOneFactor(double concreteStrength)
            {
                if (concreteStrength <= 4000)
                    return 0.85;

                double betaOne = 0.85 - (0.05 * ((concreteStrength - 4000.0) / 1000.0));
                return R(Math.Max(0.65, betaOne));
            }

            // 19) εty = fy / Es
            public double YieldStrain(double steelYieldStrength, double steelModulus)
            {
                if (steelModulus <= 0) return 0;
                return R(steelYieldStrength / steelModulus);
            }

            // 20) a = As * fy / (0.85 * fc' * b)
            public double CompressionBlockDepth(double steelArea, double steelYieldStrength, double concreteStrength, double stripWidth)
            {
                double denominator = 0.85 * concreteStrength * stripWidth;
                if (denominator <= 0) return 0;

                return R((steelArea * steelYieldStrength) / denominator);
            }

            // 21) c = a / β1
            public double NeutralAxisDepth(double compressionBlockDepth, double betaOne)
            {
                if (betaOne <= 0) return 0;
                return R(compressionBlockDepth / betaOne);
            }

            // 22) εt = 0.003 * (d - c) / c
            public double NetTensileStrain(double effectiveDepth, double neutralAxisDepth)
            {
                if (neutralAxisDepth <= 0) return 0;
                return R(0.003 * ((effectiveDepth - neutralAxisDepth) / neutralAxisDepth));
            }

            // 23) phi
            public double StrengthReductionFactor(double netTensileStrain, double yieldStrain)
            {
                if (netTensileStrain >= yieldStrain + 0.003)
                    return 0.90;

                if (netTensileStrain <= yieldStrain)
                    return 0.65;
                
                return R(0.65 + 0.25 * ((netTensileStrain - yieldStrain) / 0.003));
            }

            // 24) Mn = As * fy * (d - a/2)
            public double NominalMomentCapacity(double steelArea, double steelYieldStrength, double effectiveDepth, double compressionBlockDepth)
            {
                return R(steelArea * steelYieldStrength * (effectiveDepth - (compressionBlockDepth / 2.0)));
            }

            // 25) phiMn = phi * Mn / 1000
            public double DesignMomentStrength(double phi, double nominalMomentCapacity)
            {
                return R((phi * nominalMomentCapacity) / 1000.0);
            }

            // 26) Uf = Mu / phiMn
            public double FlexuralUtilizationRatio(double factoredMoment, double designMomentStrength)
            {
                if (designMomentStrength <= 0) return 0;
                return R(factoredMoment / designMomentStrength);
            }

            public bool FlexurePass(double flexuralUtilizationRatio)
            {
                return flexuralUtilizationRatio <= 1.0;
            }

            // 27) Mnreq = Mu * 1000 / 0.9
            public double RequiredNominalMomentForSteelDesign(double factoredMoment)
            {
                return R((factoredMoment * 1000.0) / 0.9);
            }

            // 28) Asreq
            public double RequiredSteelArea(double steelYieldStrength, double effectiveDepth, double concreteStrength,
                                            double stripWidth, double requiredNominalMoment)
            {
                double k = Math.Pow(steelYieldStrength, 2) / (2.0 * 0.85 * concreteStrength * stripWidth);
                double term = Math.Pow(steelYieldStrength * effectiveDepth, 2) - (4.0 * k * requiredNominalMoment);

                if (k <= 0 || term < 0) return 0;

                double asReq = ((steelYieldStrength * effectiveDepth) - Math.Sqrt(term)) / (2.0 * k);
                return R(asReq);
            }

            // 29) Asprov = Abar * 12 / sprov
            public double ProvidedSteelArea(double barArea, double providedSpacing)
            {
                if (providedSpacing <= 0) return 0;
                return R((barArea * 12.0) / providedSpacing);
            }

            // 30) sreq = Abar * 12 / Asreq
            public double RequiredBarSpacing(double barArea, double requiredSteelArea)
            {
                if (requiredSteelArea <= 0) return 0;
                return R((barArea * 12.0) / requiredSteelArea);
            }

            // ==============================
            // 3.4 One-Way Shear Check
            // ==============================

            // 33) h = tedge * 12
            public double TotalMemberThickness(double footingEdgeThicknessFt)
            {
                return R(footingEdgeThicknessFt * 12.0);
            }

            // 34) λs = min(1, 2 / (1 + d/10))
            public double SizeEffectFactor(double effectiveDepth)
            {
                return R(Math.Min(1.0, 2.0 / (1.0 + (effectiveDepth / 10.0))));
            }

            // 35) ρw = As / bd
            public double ReinforcementRatio(double steelArea, double stripWidth, double effectiveDepth)
            {
                double denominator = stripWidth * effectiveDepth;
                if (denominator <= 0) return 0;

                return R(steelArea / denominator);
            }

            // 36) Ag = b * h
            public double GrossConcreteArea(double stripWidth, double totalMemberThickness)
            {
                return R(stripWidth * totalMemberThickness);
            }

            // 37) Placeholder: replace with exact Excel logic when available
            public double NominalOneWayShearStrength(double concreteStrength, double stripWidth, double effectiveDepth,
                                                     double lambda, double sizeEffectFactor)
            {
                double vn = 2.0 * lambda * sizeEffectFactor * Math.Sqrt(concreteStrength) * stripWidth * effectiveDepth;
                return R(vn);
            }

            // 38) phiVn = phi * Vn / 1000
            public double DesignOneWayShearStrength(double phi, double nominalShearStrength)
            {
                return R((phi * nominalShearStrength) / 1000.0);
            }

            // 39) Uv = Vu / phiVn
            public double OneWayShearUtilizationRatio(double shearDemand, double designShearStrength)
            {
                if (designShearStrength <= 0) return 0;
                return R(shearDemand / designShearStrength);
            }

            public bool OneWayShearPass(double utilizationRatio)
            {
                return utilizationRatio <= 1.0;
            }

            // ==============================
            // 3.5 Punching Shear Check
            // ==============================

            // 42) alpha_s
            public double ColumnLocationFactor(string columnLocation)
            {
                if (string.IsNullOrWhiteSpace(columnLocation))
                    return 40.0;

                switch (columnLocation.Trim().ToLower())
                {
                    case "interior":
                        return 40.0;
                    case "edge":
                        return 30.0;
                    case "corner":
                        return 20.0;
                    default:
                        return 40.0;
                }
            }

            // 43) vc1 = 4λ√fc'
            public double PunchingShearStressLimit1(double lambda, double concreteStrength)
            {
                return R(4.0 * lambda * Math.Sqrt(concreteStrength));
            }

            // vc2 = (2 + 4/beta)λ√fc'
            public double PunchingShearStressLimit2(double beta, double lambda, double concreteStrength)
            {
                if (beta <= 0) return 0;
                return R((2.0 + (4.0 / beta)) * lambda * Math.Sqrt(concreteStrength));
            }

            // vc3 = (2 + alpha_s*d/b0)λ√fc'
            public double PunchingShearStressLimit3(double alphaS, double effectiveDepth, double criticalPerimeter,
                                                    double lambda, double concreteStrength)
            {
                if (criticalPerimeter <= 0) return 0;
                return R((2.0 + ((alphaS * effectiveDepth) / criticalPerimeter)) * lambda * Math.Sqrt(concreteStrength));
            }

            // 44) vc = min(vc1, vc2, vc3)
            public double GoverningPunchingShearStress(double vc1, double vc2, double vc3)
            {
                return R(Math.Min(vc1, Math.Min(vc2, vc3)));
            }

            // 45) Vnp = vc * b0 * d
            public double NominalPunchingShearStrength(double governingShearStress, double criticalPerimeter, double effectiveDepth)
            {
                return R(governingShearStress * criticalPerimeter * effectiveDepth);
            }

            // 46) phiVnp = phi * Vnp / 1000
            public double DesignPunchingShearStrength(double phi, double nominalPunchingShearStrength)
            {
                return R((phi * nominalPunchingShearStrength) / 1000.0);
            }

            // 47) Up = Vup / phiVnp
            public double PunchingShearUtilizationRatio(double punchingShearDemand, double designPunchingShearStrength)
            {
                if (designPunchingShearStrength <= 0) return 0;
                return R(punchingShearDemand / designPunchingShearStrength);
            }

            public bool PunchingShearPass(double utilizationRatio)
            {
                return utilizationRatio <= 1.0;
            }

            // ==============================
            // 3.6 Concrete Bearing Check
            // ==============================

            // 48) Rn
            public double NominalConcreteBearingStrength(double concreteStrength, double loadedAreaA1,
                                                         double supportingAreaA2, bool enhancementPermitted)
            {
                if (loadedAreaA1 <= 0) return 0;

                if (enhancementPermitted)
                {
                    double rnEnhanced = 0.85 * concreteStrength * loadedAreaA1 * Math.Sqrt(supportingAreaA2 / loadedAreaA1);
                    double rnLimit = 2.0 * (0.85 * concreteStrength * loadedAreaA1);

                    return R(Math.Min(rnEnhanced, rnLimit));
                }

                return R(0.85 * concreteStrength * loadedAreaA1);
            }

            // 49) phiRn = phi * Rn / 1000
            public double DesignConcreteBearingStrength(double phi, double nominalBearingStrength)
            {
                return R((phi * nominalBearingStrength) / 1000.0);
            }

            // 50) Ubr = Bu / phiRn
            public double ConcreteBearingUtilizationRatio(double bearingDemand, double designBearingStrength)
            {
                if (designBearingStrength <= 0) return 0;
                return R(bearingDemand / designBearingStrength);
            }

            public bool ConcreteBearingPass(double utilizationRatio)
            {
                return utilizationRatio <= 1.0;
            }
        }

        public class MultiColumnAnchorBoltEquations
        {
            private double R(double value) => Math.Round(value, 5);

            // Step 1: Tension Side Legs (Assume half the columns are in tension)
            public int TensionLegs(int totalColumns)
            {
                return Math.Max(1, totalColumns / 2);
            }

            // Step 2: Leg Radius in inches (from Cone Diameter in feet)
            public double LegRadiusInches(double coneDiameterFt)
            {
                return R((coneDiameterFt * 12.0) / 2.0);
            }

            // Step 3: Tension Force From Overturning (T = Mu / (4/3 r))
            public double TotalOverturningTension(double momentKipFt, double radiusInches)
            {
                if (radiusInches <= 0) return 0;
                double momentKipIn = momentKipFt * 12.0;
                return R(momentKipIn / ((4.0 / 3.0) * radiusInches));
            }

            public double TensionPerLeg(double totalTension, int tensionLegs)
            {
                if (tensionLegs <= 0) return 0;
                return R(totalTension / tensionLegs);
            }

            // Step 4: Tension per Bolt
            // All bolts on the pedestal resist uplift
            public double TensionPerBolt(double tensionPerLeg, int totalBoltsPerLeg)
            {
                int tensionBolts = Math.Max(1, totalBoltsPerLeg);
                return R(tensionPerLeg / tensionBolts);
            }

            // Step 5: Required Steel Area
            public double RequiredSteelArea(double tensionPerBolt, double phi, double fy)
            {
                if (phi <= 0 || fy <= 0) return 0;
                return R(tensionPerBolt / (phi * fy));
            }

            public double BoltArea(double db)
            {
                // AISC/ACI Net Tensile Area lookup for standard threaded anchor bolts
                if (db == 1.5) return 1.77;
                if (db == 1.75) return 1.90;
                if (db == 2.0) return 2.50;
                if (db == 2.25) return 3.25;

                // Fallback to gross circular area for unknown/custom sizes
                return R(Math.PI * Math.Pow(db, 2) / 4.0);
            }

            // Step 7: Steel Tension Capacity
            public double SteelTensionCapacity(double phi, double area, double fy)
            {
                return R(phi * area * fy);
            }

            // Step 8: Steel Shear Capacity
            public double ShearPerLeg(double totalShear, int totalColumns)
            {
                if (totalColumns <= 0) return 0;
                return R(totalShear / totalColumns);
            }

            public double ShearPerBolt(double shearPerLeg, int totalBoltsPerLeg)
            {
                if (totalBoltsPerLeg <= 0) return 0;
                return R(shearPerLeg / totalBoltsPerLeg);
            }

            public double SteelShearCapacity(double phi, double area, double fy)
            {
                // PDF uses 0.6 * Fy
                return R(phi * 0.6 * fy * area);
            }

            // Step 9: Concrete Breakout Check
            // ACI approximate breakout
            public double ConcreteBreakoutStrength(double phi, double kc, double fcPrime, double hef)
            {
                double Ncb = phi * kc * Math.Sqrt(fcPrime) * Math.Pow(hef, 1.5);
                return R(Ncb / 1000.0); // Convert lb to kips
            }

            // Step 10: Pullout Check
            // Np = 8 * Abrg * f'c
            public double PulloutStrength(double washerArea, double fcPrime)
            {
                double Np = 8.0 * washerArea * fcPrime;
                return R(Np / 1000.0); // Convert lb to kips
            }

            // Step 11: Pryout Check
            // Vcp = kcp * Ncb (kcp = 2.0 for hef >= 2.5 in, per ACI 318-19 Table 17.7.3.1)
            public double PryoutStrength(double ncbKips, double hef = 40.0)
            {
                double kcp = hef < 2.5 ? 1.0 : 2.0;
                return R(kcp * ncbKips);
            }

            // Step 12: Interaction Check
            public double InteractionRatio(double tu, double phiNn, double vu, double phiVn)
            {
                if (phiNn <= 0 || phiVn <= 0) return 0;
                return R(Math.Pow(tu / phiNn, 5.0 / 3.0) + Math.Pow(vu / phiVn, 5.0 / 3.0));
            }

            public bool InteractionPass(double interactionRatio)
            {
                return interactionRatio <= 1.0;
            }

            // Step 13: Edge Distance Check
            public double EdgeDistance(double pedestalWidth, double boltSpacing)
            {
                return R((pedestalWidth - boltSpacing) / 2.0);
            }

            public double MinimumEdgeDistance(double db)
            {
                return R(7.0 * db);
            }
        }

        public class MultiColumnBasePlateEquations
        {
            private double R(double value) => Math.Round(value, 5);

            // Step 1: Factored Moment per Pedestal (Mu,ped)
            public double FactoredMomentPerPedestal(double totalMuKipFt, int totalPedestals)
            {
                if (totalPedestals <= 0) return 0;
                return R(totalMuKipFt / totalPedestals);
            }

            public double ConvertMomentToKipIn(double momentKipFt)
            {
                return R(momentKipFt * 12.0);
            }

            // Step 2: Pedestal Area & Base Plate Area & Bearing Stress
            public double PedestalArea(double p, double l) => R(p * l);
            public double BasePlateArea(double d_o) => R(Math.PI * Math.Pow(d_o, 2) / 4.0);

            public double MaximumBearingStress(double fcPrimeKsi, double a2, double a1, double phiC = 0.65)
            {
                if (a1 <= 0 || a2 <= 0) return 0;
                double ratio = Math.Sqrt(a2 / a1);
                if (ratio > 2.0) ratio = 2.0; // ACI/AISC max limit sqrt(A2/A1) <= 2.0
                double fp = 0.85 * phiC * fcPrimeKsi * ratio;
                double limit = 1.7 * phiC * fcPrimeKsi;
                return R(Math.Min(fp, limit));
            }

            public double BearingStressLimit(double fcPrimeKsi, double phiC = 0.65)
            {
                return R(1.7 * phiC * fcPrimeKsi);
            }

            // Step 3: Check Bearing Capacity
            public double BearingCapacity(double fpKsi, double a1)
            {
                return R(fpKsi * a1);
            }

            // Step 4: Determine Equivalent Eccentricity
            // Applied load per pedestal in compression: Pu,ped = Pu,total / (totalPedestals / 2)
            public double AppliedLoadPerCompressionPedestal(double totalPuKips, int totalPedestals)
            {
                int compPedestals = Math.Max(1, totalPedestals / 2);
                return R(totalPuKips / compPedestals);
            }

            public double EquivalentEccentricity(double muPedKipIn, double puPedKips)
            {
                if (puPedKips <= 0) return 0;
                return R(muPedKipIn / puPedKips);
            }

            public double BearingConditionLimit(double d_o)
            {
                return R(d_o / 8.0); // D/8 for solid circular section limit
            }

            // Step 6: Determine Base Plate Thickness
            // 6.1 Column Bearing Area
            public double ColumnBearingArea(double pipeDiameterInches)
            {
                return R(Math.PI * Math.Pow(pipeDiameterInches, 2) / 4.0);
            }

            // 6.2 Bearing Pressure (q)
            public double BearingPressure(double puPedKips, double columnArea)
            {
                if (columnArea <= 0) return 0;
                return R(puPedKips / columnArea);
            }

            // 6.3 Plate Projection (m)
            public double PlateProjection(double outerDiameterDo, double pipeDiameterD)
            {
                return R((outerDiameterDo - pipeDiameterD) / 2.0);
            }

            // 6.4 Plastic Moment per 1-in Strip (Mplu)
            public double StripPlasticMoment(double qKsi, double mInches)
            {
                return R((qKsi * Math.Pow(mInches, 2)) / 2.0);
            }

            // 6.5 Required Thickness (tp)
            public double RequiredThickness(double mplu, double fyKsi, double phiB = 0.90)
            {
                if (phiB <= 0 || fyKsi <= 0 || mplu <= 0) return 0;
                return R(Math.Sqrt((4.0 * mplu) / (phiB * fyKsi)));
            }
        }
    }
}