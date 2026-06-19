using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaterTankTool_WFA.Constants;
using WaterTankTool_WFA.Entity;

namespace WaterTankTool_WFA.Solver_Equation
{

    public class Equations
    {
        //Constants
        private const double rw = 62.4; // Weight density of water (pcf)
        private const double rs = 490;  // Weight density of steel (pcf)
        private const double rc = 144;  // Weight density of concrete (pcf)


    }

    public class Segment_Cylinder_Equations
    {
        UnitsConverter unitsConverter = new UnitsConverter();

        Material_Property_Data Material_Property_Data;

        private WaterTankDbContext _context;

        private WindLoadEntity Qwind;

        public Segment_Cylinder_Equations() {
            _context = WaterTankDbContext.GetInstance();
            Qwind = _context.WindLoadEntity.FirstOrDefault() ?? new WindLoadEntity { Exposure = "C", Cf = 1.0, G = 1.0, Q = 0.0 };


        }

        public double ProjectedArea(double heightInitial,double heightfinal, double diameter)
        {
            var height = heightfinal - heightInitial;
            var result = (height * diameter );
            return Math.Round(result,5);
        }

        public double Centroid(double heightInitial,double heightFinal)
        {
            var height = heightFinal - heightInitial;
            var result = heightInitial + (height / 2);
            return result;
        }

        public double weightOfPedestal(double heightInitial,double heightFinal,double Diameter,double t)
        {
            var thickness = unitsConverter.inch_TO_Ft(t);

            var height = heightFinal - heightInitial;

            var outerVolume = (Math.PI / 4) * (Math.Pow(Diameter, 2) * height);

            var innerVolume = (Math.PI / 4) * Math.Pow((Diameter - (2*thickness)),2) * height;

            var segmentVolume = outerVolume - innerVolume;

            var weight = (segmentVolume * ConstantsClass.rs ) / 1000;

            return weight;
        }

        public double kzi(double heightinitial)
        {
            //this need to be changed according to the Exposure class selected
            var zg = WindLoadExposure_C.Zg;
            var a = WindLoadExposure_C.Alpha;
            var result = 2.01 * Math.Pow((heightinitial / zg), 2 / a);
            return result;
        }

        public double kzf(double heightfinal)
        {
            //this need to be changed according to the Exposure class selected
            var zg = WindLoadExposure_C.Zg;
            var a = WindLoadExposure_C.Alpha;
            var result = 2.01 * Math.Pow((heightfinal / zg), 2 / a);
            return result;
        }

        public double qzi(double heightInitial)
        {
            double result = 0;
            if (Qwind != null)
            {

                var value1 = Qwind.Q * kzi(heightInitial)  ;
                //var value2 = 30 * Qwind.Cf;
                result = value1;
            }

            return result;
        }

        public double qzf(double heightFinal)
        {
            double result = 0;
            if (Qwind != null)
            {

                var value1 = Qwind.Q * kzf(heightFinal);
                //var value2 = 30 * Qwind.Cf;
                result = value1;
            }

            return result;
        }

        public double F(double heightInitial,double heightFinal,double diameter)
        {
            var height = heightFinal - heightInitial;

            var result1 = 30 * Qwind.Cf * (ProjectedArea(heightInitial, heightFinal, diameter) / 1000);

            var result2 = ((qzi(heightInitial) + qzf(heightFinal)) / 2) * Qwind.Cf * Qwind.G * (ProjectedArea(heightInitial, heightFinal, diameter) / 1000);

            return Math.Max(result1,result2);
        }
        public double L(double heightInitial,double heightFinal)
        {
            var h = heightFinal - heightInitial;

            if (h <= 0) return heightInitial; // Safe fallback

            double denominator = (qzi(heightInitial) * h) + (0.5 * (qzf(heightFinal) - qzi(heightInitial)) * h);

            if (Math.Abs(denominator) < 0.0001) return heightInitial + (h / 2.0); // Assume uniform wind if Q is zero

            double numerator = (qzi(heightInitial) * Math.Pow(h, 2) / 2) + (0.5 * (qzf(heightFinal) - qzi(heightInitial)) * 2*(Math.Pow( h, 2)) / 3);

            double F = heightInitial + (numerator / denominator);

            return F;

        }


        public double Mbase(double heightInitial, double heightFinal, double diameter)
        {
            var result = L(heightInitial, heightFinal) * F(heightInitial, heightFinal, diameter);
            return result;
        }
    }


    //Equations for Conical Segments
    public class Segment_Conical_Equations
    {
        UnitsConverter unitsConverter = new UnitsConverter();

        private WaterTankDbContext _context;

        private WindLoadEntity Qwind;

        public Segment_Conical_Equations()
        {
            _context = WaterTankDbContext.GetInstance();
            Qwind = _context.WindLoadEntity.FirstOrDefault() ?? new WindLoadEntity { Exposure = "C", Cf = 1.0, G = 1.0, Q = 0.0 };

        }

        public double ProjectedArea(double heightInitial, double heightfinal, double diameter)
        {
            var height = heightfinal - heightInitial;
            var result = (height * diameter );
            return Math.Round(result,5);
        }

        public double Centroid(double heightInitial, double heightFinal,double diameterTop, double diameterBottom)
        {
            var r1 = diameterBottom / 2;
            var r2 = diameterTop / 2; 

            var height = heightFinal - heightInitial;

            var num = Math.Pow(r1, 2) + (2 * r1 * r2) + (3 * Math.Pow(r2, 2));
            var deno = Math.Pow(r1, 2) + (r1 * r2) + Math.Pow(r2, 2);

            var result = heightInitial+((height/4) * (num/deno));


            return result;
        }

        public double weight(double hi, double hf, double di,double df, double t)
        {
            //var thickness = unitsConverter.inch_TO_Ft(t);
            //di = Top Diameter ------ df = Bottom Diameter

            var d = df - di;
            var h = hf-hi;

            var S1 = Math.Round(((Math.PI / 4) * Math.Pow(di,2)),4);
            var S2 = Math.Round(((Math.PI / 4) * Math.Pow(df,2)),4);
            var S3 = Math.Round(((Math.PI / 4) * Math.Pow((di - (2*t/12)),2)),4);
            var S4 = Math.Round(((Math.PI / 4) * Math.Pow((df - (2*t/12)), 2)),4);

            var weight = ((h / 3) * (S1 + S2 - S3 - S4 + Math.Sqrt(S1 * S2) - Math.Sqrt(S3*S4))) * (ConstantsClass.rs/1000);


            return weight;
        }


        public double kzi(double heightinitial)
        {
            double res = 0;
            if(heightinitial <= 15 && Qwind.Exposure == "C")
            {
                res = 0.85;
            }
            else if (heightinitial <= 15 && Qwind.Exposure == "D")
            {
                res = 1.03;
            }
            else if( heightinitial > 15)
            {
                if(Qwind.Exposure == "C")
                {
                    var zg = WindLoadExposure_C.Zg;
                    var a = WindLoadExposure_C.Alpha;
                    res = 2.01 * Math.Pow((heightinitial / zg), 2 / a);
                }
                else if(Qwind.Exposure == "D")
                {
                    var zg = WindLoadExposure_D.Zg;
                    var a = WindLoadExposure_D.Alpha;
                    res = 2.01 * Math.Pow((heightinitial / zg), 2 / a);
                }

            }
            return res;

        }

        public double kzf(double heightfinal)
        {

            double res = 0;
            if (heightfinal <= 15 && Qwind.Exposure == "C")
            {
                res = 0.85;
            }
            else if (heightfinal <= 15 && Qwind.Exposure == "D")
            {
                res = 1.03;
            }
            else if (heightfinal > 15)
            {
                if (Qwind.Exposure == "C")
                {
                    var zg = WindLoadExposure_C.Zg;
                    var a = WindLoadExposure_C.Alpha;
                    res = 2.01 * Math.Pow((heightfinal / zg), 2 / a);
                }
                else if (Qwind.Exposure == "D")
                {
                    var zg = WindLoadExposure_D.Zg;
                    var a = WindLoadExposure_D.Alpha;
                    res = 2.01 * Math.Pow((heightfinal / zg), 2 / a);
                }

            }
            return res;

        }

        public double qzi(double heightInitial)
        {
            double result = 0;
            if (Qwind != null)
            {

                var value1 = Qwind.Q * kzi(heightInitial) * Qwind.G;
                //var value2 = 30 * Qwind.Cf;
                result = value1;
            }

            return Math.Round(result,5);
        }

        public double qzf(double heightFinal)
        {
            double result = 0;
            if (Qwind != null)
            {

                var value1 = Qwind.Q * kzf(heightFinal) * Qwind.G;
                //var value2 = 30 * Qwind.Cf;
                result = value1;
            }

            return Math.Round(result,5);
        }

        public double F(double heightInitial, double heightFinal, double diameter)
        {
            var height = heightFinal - heightInitial;

            var result1 = 30 * Qwind.Cf * (ProjectedArea(heightInitial, heightFinal, diameter) / 1000);

            var result2 = ((qzi(heightInitial) + qzf(heightFinal)) / 2) * Qwind.Cf * Qwind.G * (ProjectedArea(heightInitial, heightFinal, diameter) / 1000);

            return Math.Max(result1,result2);
        }
        public double L(double heightInitial, double heightFinal)
        {
            var h = heightFinal - heightInitial;

            if (h <= 0) return heightInitial; // Safe fallback

            double denominator = Math.Round(((qzi(heightInitial) * h) + (0.5 * (qzf(heightFinal) - qzi(heightInitial)) * h)),4);

            if (Math.Abs(denominator) < 0.0001) return heightInitial + (h / 2.0); // Assume uniform wind if Q is zero

            double numerator = Math.Round(((qzi(heightInitial) * Math.Pow(h, 2) / 2) + (0.5 * (qzf(heightFinal) - qzi(heightInitial)) * Math.Pow(2 * h, 2) / 3)), 4);

            double F = heightInitial + (numerator / denominator);

            return F;

        }

        public double Mbase(double heightInitial, double heightFinal, double diameter)
        {
            var result = L(heightInitial, heightFinal) * F(heightInitial, heightFinal, diameter);
            return result;
        }

    }

    public class Multileg_Cylinders
    {
        UnitsConverter unitsConverter = new UnitsConverter();

        private WaterTankDbContext _context;

        private WindLoadEntity Qwind;

        public Multileg_Cylinders()
        {
            _context = WaterTankDbContext.GetInstance();
            Qwind = _context.WindLoadEntity.FirstOrDefault() ?? new WindLoadEntity { Exposure = "C", Cf = 1.0, G = 1.0, Q = 0.0 };
        }


        public double ProjectedArea(double heightInitial, double heightfinal, double diameter)
        {
            var height = heightfinal - heightInitial;
            var result =  height * diameter * AppState.NoOfColumns;
            return result;
        }

        public double weightOfPedestal(double heightInitial, double heightFinal, double Diameter, double t,string segmentType)
        {
            var thickness = unitsConverter.inch_TO_Ft(t);

            var height = heightFinal - heightInitial;

            var outerVolume = (Math.PI / 4) * (Math.Pow(Diameter, 2) * height);

            var innerVolume = (Math.PI / 4) * Math.Pow((Diameter - (2 * thickness)), 2) * height;

            var segmentVolume = outerVolume - innerVolume;

            double add = 0;

            double weight = 0;

            if (heightInitial == 0 && segmentType == "Cylinder")
            {
                add = (AppState.crossBracing / AppState.NoOfSegment);
                weight = ((segmentVolume * ConstantsClass.rs * AppState.NoOfColumns) / 1000) + add;
            }
            else if(heightInitial > 0 && segmentType == "Cylinder")
            {
                add = ((AppState.struts / (AppState.NoOfSegment - 1)) + (AppState.crossBracing / AppState.NoOfSegment));
                weight = ((segmentVolume * ConstantsClass.rs * AppState.NoOfColumns) / 1000) + add;

            }

            else if(segmentType == "Riser")
            {
                add = 0;
                weight = ((segmentVolume * ConstantsClass.rs) / 1000) + add;

            }

              

            return weight;
        }

        public double Centroid(double heightInitial, double heightFinal)
        {
            var height = heightFinal - heightInitial;
            var result = heightInitial + (height / 2);
            return result;
        }

        public double weight(double hi, double hf, double di, double df, double t)
        {
            //var thickness = unitsConverter.inch_TO_Ft(t);
            //di = Top Diameter ------ df = Bottom Diameter

            var d = df - di;
            var h = hf - hi;

            var S1 = Math.Round(((Math.PI / 4) * Math.Pow(di, 2)), 4);
            var S2 = Math.Round(((Math.PI / 4) * Math.Pow(df, 2)), 4);
            var S3 = Math.Round(((Math.PI / 4) * Math.Pow((di - (2 * t / 12)), 2)), 4);
            var S4 = Math.Round(((Math.PI / 4) * Math.Pow((df - (2 * t / 12)), 2)), 4);

            var weight = ((h / 3) * (S1 + S2 - S3 - S4 + Math.Sqrt(S1 * S2) - Math.Sqrt(S3 * S4))) * (ConstantsClass.rs / 1000);


            return weight;
        }




        public double kzi(double heightinitial)
        {
            double res = 0;
            if (heightinitial <= 15 && Qwind.Exposure == "C")
            {
                res = 0.85;
            }
            else if (heightinitial <= 15 && Qwind.Exposure == "D")
            {
                res = 1.03;
            }
            else if (heightinitial > 15)
            {
                if (Qwind.Exposure == "C")
                {
                    var zg = WindLoadExposure_C.Zg;
                    var a = WindLoadExposure_C.Alpha;
                    res = 2.01 * Math.Pow((heightinitial / zg), 2 / a);
                }
                else if (Qwind.Exposure == "D")
                {
                    var zg = WindLoadExposure_D.Zg;
                    var a = WindLoadExposure_D.Alpha;
                    res = 2.01 * Math.Pow((heightinitial / zg), 2 / a);
                }

            }
            return res;

        }

        public double kzf(double heightfinal)
        {

            double res = 0;
            if (heightfinal <= 15 && Qwind.Exposure == "C")
            {
                res = 0.85;
            }
            else if (heightfinal <= 15 && Qwind.Exposure == "D")
            {
                res = 1.03;
            }
            else if (heightfinal > 15)
            {
                if (Qwind.Exposure == "C")
                {
                    var zg = WindLoadExposure_C.Zg;
                    var a = WindLoadExposure_C.Alpha;
                    res = 2.01 * Math.Pow((heightfinal / zg), 2 / a);
                }
                else if (Qwind.Exposure == "D")
                {
                    var zg = WindLoadExposure_D.Zg;
                    var a = WindLoadExposure_D.Alpha;
                    res = 2.01 * Math.Pow((heightfinal / zg), 2 / a);
                }

            }
            return res;

        }

        public double qzi(double heightInitial)
        {
            double result = 0;
            if (Qwind != null)
            {

                var value1 = Qwind.Q * kzi(heightInitial) * Qwind.G;
                //var value2 = 30 * Qwind.Cf;
                result = value1;
            }

            return result;
        }

        public double qzf(double heightFinal)
        {
            double result = 0;
            if (Qwind != null)
            {

                var value1 = Qwind.Q * kzf(heightFinal) * Qwind.G;
                //var value2 = 30 * Qwind.Cf;
                result = value1;
            }

            return result;
        }


        public double F(double heightInitial, double heightFinal, double diameter,string segmentType)
        {
            Segment_Cylinder_Equations c1 = new Segment_Cylinder_Equations();
            double projectArea = 0;
            if(segmentType == "Riser")
            {
                projectArea = c1.ProjectedArea(heightInitial, heightFinal, diameter);
            }
            else
            {
                projectArea = ProjectedArea(heightInitial, heightFinal, diameter);
            }
            var height = heightFinal - heightInitial;
            var result1 = 30 * Qwind.Cf * (projectArea / 1000);

            var result2 = ((qzi(heightInitial) + qzf(heightFinal)) / 2) * Qwind.Cf * Qwind.G * (projectArea / 1000);


            return Math.Max(result1, result2);
        }

        public double F_Tank(double heightInitial, double heightFinal, double diameter,double projectedArea)
        {
            var height = heightFinal - heightInitial;
            var result1 = 30 * Qwind.Cf * (projectedArea / 1000);

            var result2 = ((qzi(heightInitial) + qzf(heightFinal)) / 2) * Qwind.Cf * Qwind.G * (projectedArea / 1000);


            return Math.Max(result1, result2);
        }

        public double L_Tank(double heightInitial, double centroid)
        {
            var result = heightInitial + centroid;
            return result;

        }

        public double L(double heightInitial, double heightFinal)
        {
            var h = heightFinal - heightInitial;

            if (h <= 0) return heightInitial; // Safe fallback

            double denominator = (qzi(heightInitial) * h) + (0.5 * (qzf(heightFinal) - qzi(heightInitial)) * h);

            if (Math.Abs(denominator) < 0.0001) return heightInitial + (h / 2.0); // Assume uniform wind if Q is zero

            double numerator = (qzi(heightInitial) * Math.Pow(h, 2) / 2) + (0.5 * (qzf(heightFinal) - qzi(heightInitial)) * 2 * (Math.Pow(h, 2)) / 3);

            double F = heightInitial + (numerator / denominator);

            return F;

        }


        public double Mbase(double heightInitial, double heightFinal, double diameter,string segmentType)
        {
            var result = L(heightInitial, heightFinal) * F(heightInitial, heightFinal, diameter, segmentType);
            return result;
        }
    }
}
