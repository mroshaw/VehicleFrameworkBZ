using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VehicleFramework.UpgradeTypes
{
    public abstract class SelectableChargeableUpgrade : ModVehicleUpgrade
    {
        public override string Description => "This is a selectable-chargeable upgrade module.";
        public override QuickSlotType QuickSlotType => QuickSlotType.SelectableChargeable;
        public virtual float MaxCharge => 0;
        public virtual float EnergyCost => 0;
        public virtual void OnSelected(SelectableChargeableActionParams param)
        {
            // BZ compatibility: BZ Vehicle lacks the inherited SN subName field; retain subName.name for framework vehicles and use vehicleName for native BZ vehicles.
            Logger.DebugLog("Selecting-Charging " + ClassId + " on ModVehicle: " + (param.vehicle is ModVehicle mv ? mv.subName.name : param.vehicle.vehicleName) + " in slotID: " + param.slotID.ToString());
        }
    }
}
