using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterTankTool_WFA.Entity
{
    public class DrywellEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public double LowerDiameter { get; set; } // LDIA
        public double LowerHeight { get; set; } // LHT
        public double LowerThickness { get; set; } // LTHK
        
        public double UpperDiameter { get; set; } // UDIA
        public double UpperHeight { get; set; } // UHT
        public double UpperThickness { get; set; } // UTHK
        
        public double StiffenerOutsideRadius { get; set; } // STFOR
        public double StiffenerInsideRadius { get; set; } // STFIR
        public double StiffenerThickness { get; set; } // STFTHK
        public int StiffenerQuantity { get; set; } // STFQ
    }
}
