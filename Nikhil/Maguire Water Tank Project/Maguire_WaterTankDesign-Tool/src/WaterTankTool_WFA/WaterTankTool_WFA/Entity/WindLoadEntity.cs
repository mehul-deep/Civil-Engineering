using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterTankTool_WFA.Entity
{
    public class WindLoadEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Exposure { get; set; }
        public double Kzt { get; set; }
        public double Ke { get; set; }
        public double Kd { get; set; }
        public double G { get; set; }
        public double I { get; set; }
        public double V { get; set; }

        public double Zg { get; set; }
        public double alpha { get; set; }
        public double lambda { get; set; }
        public double Cf { get; set; }

        public double Q {  get; set; }
        public double? WindShear { get; set; }
        public double? WindMoment { get; set; }
    }
}
