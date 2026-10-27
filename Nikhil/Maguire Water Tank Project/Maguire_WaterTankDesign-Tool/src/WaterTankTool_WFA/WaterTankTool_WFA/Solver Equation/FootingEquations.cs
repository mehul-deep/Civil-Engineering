using System;

namespace WaterTankTool_WFA.Solver_Equation
{
    public class FootingEquations
    {
        // Step 1: Footing Load
        public double FootingGravityLoad(double puTotal, int numLegs)
        {
            if (numLegs <= 0) return puTotal;
            return puTotal / numLegs;
        }

        public double PedestalSelfWeight(double bpInches, double lpInches, double hpFeet, double gammaCPcf = 150.0)
        {
            return (bpInches / 12.0) * (lpInches / 12.0) * hpFeet * (gammaCPcf / 1000.0);
        }

        public double FootingDesignLoad(double pLeg, double factoredPedestalWeight)
        {
            return pLeg + factoredPedestalWeight;
        }

        public double CompressionSideLoad(double pLeg, double tLeg, double pedestalWeight)
        {
            return pLeg + tLeg + pedestalWeight;
        }

        // Step 2 & 3: Soil Bearing
        public double RequiredFootingArea(double pTotal, double qAllow)
        {
            if (qAllow <= 0) return 0;
            return pTotal / qAllow;
        }

        public double AppliedSoilPressure(double pTotal, double areaProvided)
        {
            if (areaProvided <= 0) return 0;
            return pTotal / areaProvided;
        }

        // Sliding Check
        public double SlidingFactorOfSafety(double mu, double pTotal, double vLeg)
        {
            if (vLeg <= 0) return 999;
            return (mu * pTotal) / vLeg;
        }

        // Step 4 & 5: Effective Depth & Two-Way Punching Shear Check
        public double EffectiveDepth(double hInches, double coverInches, double barDiameter)
        {
            return hInches - coverInches - (barDiameter / 2.0);
        }

        public double CriticalPunchingPerimeter(double bpInches, double lpInches, double d)
        {
            return 2.0 * (bpInches + d) + 2.0 * (lpInches + d);
        }

        public double AreaInsideCriticalPerimeter(double bpInches, double lpInches, double d)
        {
            return ((bpInches + d) * (lpInches + d)) / 144.0; // ft2
        }

        public double FactoredPunchingShear(double pTotal, double qU, double areaInsidePerimeter)
        {
            double pSoil = qU * areaInsidePerimeter;
            return Math.Max(0, pTotal - pSoil);
        }

        public double DesignPunchingShearCapacity(double fcPrimePsi, double bpInches, double lpInches, double boInches, double dInches, double alphaS = 40.0, double phi = 0.75, double lambda = 1.0)
        {
            double minSide = Math.Max(1.0, Math.Min(bpInches, lpInches));
            double maxSide = Math.Max(bpInches, lpInches);
            double beta = maxSide / minSide;

            double sqrtFc = Math.Sqrt(fcPrimePsi);
            double vc1 = 4.0 * lambda * sqrtFc * boInches * dInches / 1000.0;
            double vc2 = (2.0 + 4.0 / beta) * lambda * sqrtFc * boInches * dInches / 1000.0;
            double vc3 = (2.0 + (alphaS * dInches) / Math.Max(1.0, boInches)) * lambda * sqrtFc * boInches * dInches / 1000.0;

            double vcMin = Math.Min(vc1, Math.Min(vc2, vc3));
            return phi * vcMin; // kips
        }

        public double DemandCapacityRatio(double demand, double capacity)
        {
            if (capacity <= 0) return 999;
            return demand / capacity;
        }

        // Step 7: One-Way Shear (per 1-ft-wide strip, ACI 318)
        public double GoverningCantileverProjection(double bFootFt, double lFootFt, double bpInches, double lpInches)
        {
            double projB = (bFootFt - (bpInches / 12.0)) / 2.0;
            double projL = (lFootFt - (lpInches / 12.0)) / 2.0;
            return Math.Max(projB, projL); // ft
        }

        public double OneWayShearForcePerFt(double quKsf, double cantileverFt, double dInches)
        {
            double dFeet = dInches / 12.0;
            return Math.Max(0, quKsf * (cantileverFt - dFeet) * 1.0); // kips per 1-ft strip
        }

        public double DesignOneWayShearCapacityPerFt(double fcPrimePsi, double dInches, double phi = 0.75, double lambda = 1.0)
        {
            double bwInches = 12.0; // 1-ft-wide strip
            double Vc = (2.0 * lambda * Math.Sqrt(fcPrimePsi) * bwInches * dInches) / 1000.0; // kips/ft
            return phi * Vc; // kips/ft
        }

        // Step 8: Flexure and Reinforcement (per 1-ft-wide strip)
        public double FlexureMomentPerFt(double quKsf, double cantileverFt)
        {
            return (quKsf * cantileverFt * cantileverFt) / 2.0; // kip-ft/ft
        }

        public double RequiredFlexuralSteelPerFt(double muKipInPerFt, double fcPrimeKsi, double fyKsi, double dInches, double phi = 0.90)
        {
            if (muKipInPerFt <= 0 || fcPrimeKsi <= 0 || fyKsi <= 0 || dInches <= 0) return 0;
            double b = 12.0;
            double k = 0.85 * fcPrimeKsi * b;
            double rad = (dInches * dInches) - (2.0 * muKipInPerFt) / (phi * k);
            if (rad < 0) rad = 0;
            return (k / fyKsi) * (dInches - Math.Sqrt(rad)); // in2/ft
        }

        public double MinimumReinforcementPerFt(double hInches)
        {
            return 0.0018 * 12.0 * hInches; // in2/ft
        }

        public double DesignFlexuralCapacityKipInPerFt(double asProvIn2PerFt, double fcPrimeKsi, double fyKsi, double dInches, double phi = 0.90)
        {
            if (asProvIn2PerFt <= 0 || fcPrimeKsi <= 0 || fyKsi <= 0) return 0;
            double b = 12.0;
            double a = (asProvIn2PerFt * fyKsi) / (0.85 * fcPrimeKsi * b);
            double Mn = asProvIn2PerFt * fyKsi * (dInches - a / 2.0); // kip-in/ft
            return phi * Mn; // kip-in/ft
        }

        // Step 9: Pedestal/Footing Bearing
        public double PedestalBearingCapacity(double fcPrimeKsi, double bpInches, double lpInches, double phi = 0.65)
        {
            double A1 = bpInches * lpInches;
            return phi * 0.85 * fcPrimeKsi * A1; // kips
        }

        // Step 10: Dowel Reinforcement
        public double DowelMinimumArea(double bpInches, double lpInches)
        {
            double Ag = bpInches * lpInches;
            return 0.005 * Ag; // in2
        }

        // Step 11: Compression Development Length
        public double DevelopmentLength(double fyPsi, double fcPrimePsi, double dbInches)
        {
            double ldc = (fyPsi * 1.0) / (50.0 * 1.0 * Math.Sqrt(fcPrimePsi)) * dbInches;
            double minLdc = Math.Max(0.0003 * fyPsi * dbInches, 8.0);
            return Math.Max(ldc, minLdc); // inches
        }
    }
}
