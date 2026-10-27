using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaftAppleGames.VehicleFrameworkBZ.VehicleChildComponents
{
    public class ForeverBattery : EnergyMixin
    {
        // use fixed update because EnergyMixin has none
        public void FixedUpdate()
        {
            AddEnergy(capacity - charge);
        }
    }
}
