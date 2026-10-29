using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VehicleFramework.UpgradeTypes
{
    public abstract class SelectableUpgrade : ModVehicleUpgrade
    {
        public override string Description => "This is a selectable upgrade module.";
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public virtual float Cooldown => 0;
        public virtual float EnergyCost => 0;
        public virtual void OnSelected(SelectableActionParams param)
        {
            // BZ compatibility: BZ Vehicle lacks the inherited SN subName field; retain subName.name for framework vehicles and use vehicleName for native BZ vehicles.
            Logger.DebugLog("Selecting " + ClassId + " on ModVehicle: " + (param.vehicle is ModVehicle mv ? mv.subName.name : param.vehicle.vehicleName) + " in slotID: " + param.slotID.ToString());
        }
    }
}
