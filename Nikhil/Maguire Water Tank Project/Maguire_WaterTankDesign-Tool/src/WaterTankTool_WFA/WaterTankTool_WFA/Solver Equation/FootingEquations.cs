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
    }
}
