using System;
using System.Collections.Generic;

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

        // Step 6: Sliding Check
        public double SlidingFactorOfSafety(double mu, double pTotal, double vLeg)
        {
            if (vLeg <= 0) return 999;
            return (mu * pTotal) / vLeg;
        }

        // Step 7: Two-Way Punching Shear Check
        public double EffectiveDepth(double hInches, double coverInches, double barDiameter)
        {
            return hInches - coverInches - (barDiameter / 2.0);
        }

        public double CriticalPunchingPerimeter(double pedestalSize, double d)
        {
            double c_plus_d = pedestalSize + d;
            return 4.0 * c_plus_d;
        }

        public double AreaInsideCriticalPerimeter(double pedestalSize, double d)
        {
            double c_plus_d = pedestalSize + d;
            return (c_plus_d * c_plus_d) / 144.0; // ft2
        }

        public double FactoredPunchingShear(double pTotal, double qU, double areaInsidePerimeter)
        {
            double pSoil = qU * areaInsidePerimeter;
            return pTotal - pSoil;
        }

        public double PunchingShearStress(double vu, double bo, double d)
        {
            if (bo <= 0 || d <= 0) return 0;
            return (vu * 1000.0) / (bo * d); // psi
        }

        public double ConcretePunchingShearCapacity(double fcPrimePsi)
        {
            return 4.0 * Math.Sqrt(fcPrimePsi); // psi
        }
        
        // Step 8: Punching Shear Capacity in Force Form
        public double PunchingShearForceCapacity(double vcPsi, double boInches, double dInches)
        {
            return (vcPsi * boInches * dInches) / 1000.0; // kips
        }

        public double DemandCapacityRatio(double demand, double capacity)
        {
            if (capacity <= 0) return 999;
            return demand / capacity;
        }

        // Step 7: One-Way Shear
        public double OneWayShearCantilever(double L, double c)
        {
            return (L - c) / 2.0; // ft
        }

        public double OneWayShearForce(double qu, double B, double m, double dInches)
        {
            // qu in ksf, B in ft, m in ft, dInches in inches
            double dFeet = dInches / 12.0;
            return qu * B * (m - dFeet); // kips
        }

        public double OneWayShearCapacity(double fcPrimePsi, double As, double bwInches, double dInches)
        {
            // As in in2/ft, bwInches = 162 in, dInches = 26.5 in
            // Vc = 8 * (rho_w)^(1/3) * sqrt(f'c) * bw * d
            // rho_w = As / (12 * dInches)
            double rho_w = As / (12.0 * dInches);
            if (rho_w < 0.0018) rho_w = 0.0018;
            
            double Vc = 8.0 * Math.Pow(rho_w, 1.0 / 3.0) * Math.Sqrt(fcPrimePsi) * bwInches * dInches;
            return Vc / 1000.0; // kips
        }

        // Step 8: Flexure and Reinforcement
        public double FlexureMoment(double qu, double m)
        {
            // Mu per 1-ft strip
            return (qu * m * m) / 2.0; // kip-ft/ft
        }

        public double MinimumReinforcement(double hInches)
        {
            // Ag per 12-in strip = 12 * h
            return 0.0018 * 12.0 * hInches; // in2/ft
        }

        // Step 9: Pedestal/Footing Bearing
        public double PedestalBearingCapacity(double fcPrimePsi, double cInches)
        {
            double A1 = cInches * cInches;
            return 0.65 * 0.85 * (fcPrimePsi / 1000.0) * A1; // kips
        }

        // Step 10: Dowel Reinforcement
        public double DowelMinimumArea(double cInches)
        {
            double A1 = cInches * cInches;
            return 0.005 * A1; // in2
        }

        // Step 11: Development Length
        public double DevelopmentLength(double fyPsi, double fcPrimePsi, double dbInches)
        {
            // ldc = (fy * psi_r) / (50 * lambda * sqrt(fc)) * db
            // psi_r = 1.0, lambda = 1.0
            double ldc = (fyPsi * 1.0) / (50.0 * 1.0 * Math.Sqrt(fcPrimePsi)) * dbInches;
            double minLdc = 0.0003 * fyPsi * dbInches;
            if (minLdc < 8.0) minLdc = 8.0;

            return Math.Max(ldc, minLdc); // inches
        }
    }
}
