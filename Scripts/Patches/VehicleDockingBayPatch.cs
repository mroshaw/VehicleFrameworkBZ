using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VehicleFramework.MiscComponents;

namespace VehicleFramework.Patches
{
    [HarmonyPatch(typeof(VehicleDockingBay))]
    public static class VehicleDockingBayPatch
    {
        public static List<VehicleDockingBay> dockingBays = new List<VehicleDockingBay>();

        public static bool IsThisDockable(VehicleDockingBay bay, GameObject nearby)
        {
            return
                !IsThisASubmarineWithStandingPilot(nearby)
                && IsThisVehicleSmallEnough(bay, nearby);
        }
        public static bool IsThisASubmarineWithStandingPilot(GameObject nearby)
        {
            VehicleTypes.Submarine mv = UWE.Utils.GetComponentInHierarchy<VehicleTypes.Submarine>(nearby.gameObject);
            if(mv == null)
            {
                return false;
            }    
            if(mv.IsUnderCommand && !mv.IsPlayerPiloting())
            {
                return true;
            }
            return false;
        }
        public static bool IsThisVehicleSmallEnough(VehicleDockingBay bay, GameObject nearby)
        {
            ModVehicle mv = UWE.Utils.GetComponentInHierarchy<ModVehicle>(nearby.gameObject);
            if (mv == null) return true;
            if (!mv.CanMoonpoolDock || mv.docked) return false;
            DockingBayBounds bounds = bay.gameObject.GetComponent<DockingBayBounds>();
            if (bounds == null) return false;
            return bounds.IsVehicleSmallEnough(bay, mv);
        }

        [HarmonyPrefix]
        [HarmonyPatch("IDockingBay.AllowedToDock")]
        private static bool AllowedToDockPrefix(VehicleDockingBay __instance, Dockable dockable,
            Dockable ___interpolatingDockable, bool ___fullyConstructed, ref bool __result)
        {
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle == null)
            {
                return true;
            }
            // The Seatruck Dock expansion has a different pose/cinematic contract.
            __result = !__instance.MoonpoolExpansionEnabled() && ___fullyConstructed
                && !dockable.isDocked && !dockable.GetRecentlyUndocked()
                && ___interpolatingDockable == null && __instance.GetDockedObject() == null
                && IsThisDockable(__instance, vehicle.gameObject);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateDocking")]
        private static bool UpdateDockingPrefix(VehicleDockingBay __instance)
        {
            Dockable dockable = __instance.interpolatingDockable != null
                ? __instance.interpolatingDockable : __instance.GetDockedObject();
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle == null)
            {
                return true;
            }
            float fraction = __instance.interpolationTime > 0f
                ? Mathf.Clamp01((Time.time - __instance.timeDockingStarted) / __instance.interpolationTime) : 1f;
            UpdateDockedPositionPrefix(__instance, dockable, fraction);
            if (fraction == 1f)
            {
                Player.allowSaving = true;
                if (__instance.interpolatingDockable != null)
                {
                    __instance.Dock(dockable);
                    __instance.interpolatingDockable = null;
                    // Custom vehicles supply their own player exit; no Seamoth cinematic.
                    if (__instance.dockPlayer)
                    {
                        Player.main.SetCurrentSub(__instance.GetSubRoot());
                    }
                    dockable.OnDockingComplete();
                }
            }
            bool nearby = (__instance.nearbyDockable != null || __instance.interpolatingDockable != null)
                && __instance.powerConsumer.IsPowered();
            SafeAnimator.SetBool(__instance.animator, "vehicle_nearby", nearby);
            SafeAnimator.SetBool(__instance.animator, "exosuit_docked", false);
            vehicle.AnimateMoonPoolArms(__instance);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleDockingBay.Start))]
        public static void VehicleDockingBayStartPostfix(VehicleDockingBay __instance)
        {
            if (!dockingBays.Contains(__instance))
            {
                dockingBays.Add(__instance);
            }
            __instance.gameObject.EnsureComponent<DockingBayBounds>()
                .WithX(8.6f).WithY(8.6f).WithZ(12f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleDockingBay.OnDestroy))]
        public static void VehicleDockingBayOnDestroyPostfix(VehicleDockingBay __instance) => dockingBays.Remove(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleDockingBay.Dock))]
        private static void DockPostfix(VehicleDockingBay __instance, Dockable dockable) => NotifyDocked(__instance, dockable);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleDockingBay.SetVehicleDocked))]
        private static void SetVehicleDockedPostfix(VehicleDockingBay __instance, Dockable dockable) => NotifyDocked(__instance, dockable);

        private static void NotifyDocked(VehicleDockingBay bay, Dockable dockable)
        {
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle != null && !vehicle.IsDocked)
            {
                if (vehicle.IsUnderCommand)
                {
                    Player.main.SetCurrentSub(bay.GetSubRoot());
                    Player.main.ToNormalMode(false);
                }
                // Verified BZ BaseMoonpool/Launchbay_cinematic/DockBottom/playerSpawn.
                Transform playerSpawn = bay.transform.Find("playerSpawn");
                vehicle.OnVehicleDocked(playerSpawn != null ? playerSpawn.position : Vector3.zero);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateDockedPosition")]
        private static bool UpdateDockedPositionPrefix(VehicleDockingBay __instance, Dockable dockable, float interpfraction)
        {
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle == null)
            {
                return true;
            }
            Vector3 position = __instance.dockingEndPos.position - vehicle.GetDifferenceFromCenter();
            dockable.transform.position = Vector3.Lerp(__instance.startPosition, position, interpfraction);
            dockable.transform.rotation = Quaternion.Lerp(__instance.startRotation,
                __instance.dockingEndPos.rotation, interpfraction);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleDockingBay.LateUpdate))]
        public static void LateUpdatePostfix(VehicleDockingBay __instance)
        {
            Dockable dockable = __instance.GetDockedObject();
            ModVehicle vehicle = dockable != null ? dockable.vehicle as ModVehicle : null;
            if (vehicle != null)
            {
                vehicle.AnimateMoonPoolArms(__instance);
            }
        }
    }
}
