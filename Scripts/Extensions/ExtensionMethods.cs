using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VehicleFramework.Patches;
using VehicleFramework.VehicleTypes;
using VehicleFramework.Admin;

namespace VehicleFramework.Extensions
{
    public static class ExtensionMethods
    {
        // BZ compatibility: BZ Vehicle lacks GetName; preserve custom hull names while supporting native vehicle names.
        /// <summary>
        /// Gets a display name for native and framework vehicles in Below Zero.
        /// </summary>
        public static string GetName(this Vehicle vehicle)
        {
            ModVehicle customVehicle = vehicle as ModVehicle;
            return customVehicle != null ? customVehicle.GetName() : vehicle.vehicleName;
        }

        public static ModVehicle? GetModVehicle(this Player player)
        {
            return 
                Drone.MountedDrone
                ?? player.GetVehicle() as ModVehicle
                ?? player.currentSub?.GetComponent<ModVehicle>();
        }
        public static List<string> GetCurrentUpgrades(this Vehicle vehicle)
        {
            return vehicle.modules.equipment.Select(x => x.Value).Where(x => x != null && x.item != null).Select(x=>x.item.name).ToList();
        }
        public static List<string> GetCurrentUpgrades(this SubRoot subroot)
        {
            IEnumerable<string> upgrades = new List<string>();
            foreach(UpgradeConsole upgradeConsole in subroot.GetComponentsInChildren<UpgradeConsole>(true))
            {
                IEnumerable<string> theseUpgrades = upgradeConsole.modules.equipment.Select(x => x.Value).Where(x => x != null && x.item != null).Select(x => x.item.name).Where(x => x != string.Empty);
                upgrades = upgrades.Concat(theseUpgrades);
            }
            return upgrades.ToList();
        }
        public static AudioSource Register(this AudioSource source)
        {
            return FreezeTimePatcher.Register(source);
        }
        // BZ compatibility: BZ docking state belongs to Dockable/IDockingBay; the SN bay fields and Cyclops collision path are unavailable.
        /// <summary>
        /// Releases a vehicle through Below Zero's dockable state transitions.
        /// </summary>
        public static void Undock(this Vehicle vehicle)
        {
            Dockable dockable = vehicle.GetComponent<Dockable>();
            if (dockable == null || !dockable.isDocked)
            {
                return;
            }
            foreach (VehicleDockingBay bay in Patches.VehicleDockingBayPatch.dockingBays)
            {
                if (bay == null || bay.GetDockedObject() != dockable)
                {
                    continue;
                }
                bay.OnUndockingStart();
                bay.SetVehicleUndocked();
                ((IDockingBay)bay).EjectDocked();
                dockable.OnUndockingComplete(null);
                return;
            }
        }

        public static bool IsPilotingCyclops(this Player player)
        {
            return player.IsInCyclops() && player.mode == Player.Mode.Piloting;
        }
        public static bool IsInCyclops(this Player player)
        {
            return player.currentSub != null && player.currentSub.name.ToLower().Contains("cyclops");
        }
        public static bool IsGameObjectAncestor(this Transform current, GameObject ancestor)
        {
            if (current == null || ancestor == null)
            {
                return false;
            }
            if (current.gameObject == ancestor)
            {
                return true;
            }
            return current.parent.IsGameObjectAncestor(ancestor);
        }
        public static TechType GetTechType(this Vehicle vehicle)
        {
            if (vehicle == null || vehicle.GetComponent<TechTag>() == null)
            {
                return TechType.None;
            }
            return vehicle.GetComponent<TechTag>().type;
        }
        public static float GetEngineVolume(this ModVehicle vehicle)
        {
            return VehicleConfig.GetConfig(vehicle).EngineVolume.Value;
        }
    }
}
