using System.Collections.Generic;
using System.Collections;
using System.Linq;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine;
using VehicleFramework.VehicleChildComponents;

// PURPOSE: generally ensures ModVehicles behave like normal Vehicles
// VALUE: Very high.

namespace VehicleFramework.Patches
{
    [HarmonyPatch(typeof(Vehicle))]
    public class VehiclePatcher
    {
        /*
         * This collection of patches generally ensures our ModVehicles behave like normal Vehicles.
         * Each will be commented if necessary
         */

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.Undock))]
        private static bool UndockPrefix(Vehicle __instance, Player player, float yUndockedPosition, ref IEnumerator __result)
        {
            ModVehicle vehicle = __instance as ModVehicle;
            if (vehicle == null)
            {
                return true;
            }
            __result = vehicle.Undock(player, yUndockedPosition);
            return false;
        }



        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.OnHandClick))]
        public static bool OnHandClickPrefix(Vehicle __instance)
        {
            if (VehicleTypes.Drone.MountedDrone != null || (__instance as ModVehicle) != null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.OnHandHover))]
        public static bool OnHandHoverPrefix(Vehicle __instance)
        {
            ModVehicle? mv = __instance as ModVehicle;
            if (mv == null)
            {
                if (VehicleTypes.Drone.MountedDrone != null)
                {
                    return false;
                }
            }
            else
            {
                if (mv.IsScuttled)
                {
                    float now = mv.GetComponent<Sealed>().openedAmount;
                    float max = mv.GetComponent<Sealed>().maxOpenedAmount;
                    string percent = Mathf.CeilToInt(now * 100f / max).ToString();
                    HandReticle.main.SetText(HandReticle.TextType.Hand, $"{Language.main.Get("VFDeconstructionHint")}: {percent}", true, GameInput.Button.None);
                }
                else if (mv.IsUnderCommand)
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, "", true, GameInput.Button.None);
                }
                else
                {
                    HandReticle.main.SetText(HandReticle.TextType.Hand, mv.HullName, true, GameInput.Button.None);
                }
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.ApplyPhysicsMove))]
        private static bool ApplyPhysicsMovePrefix(Vehicle __instance)
        {
            return __instance is not ModVehicle;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.LazyInitialize))]
        public static bool LazyInitializePrefix(Vehicle __instance, ref EnergyInterface ___energyInterface)
        {
            ModVehicle? mv = __instance as ModVehicle;
            if (mv == null)
            {
                return true;
            }

            ___energyInterface = __instance.gameObject.GetComponent<EnergyInterface>();
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.GetAllStorages))]
        public static void GetAllStoragesPostfix(Vehicle __instance, ref List<IItemsContainer> containers)
        {
            ModVehicle? mv = __instance as ModVehicle;
            if (mv == null || mv.InnateStorages == null)
            {
                return;
            }
            foreach (var tmp in mv.InnateStorages)
            {
                containers.Add(tmp.Container.GetComponent<InnateStorageContainer>().Container);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.IsPowered))]
        public static void IsPoweredPostfix(Vehicle __instance, ref bool __result)
        {
            ModVehicle? mv = __instance as ModVehicle;
            if (mv == null)
            {
                return;
            }
            if (!mv.IsPoweredOn)
            {
                __result = false;
                return;
            }
            PowerRelay powerRelay = mv.GetComponent<PowerRelay>();
            if (powerRelay != null && powerRelay.IsPowered())
            {
                __result = true;
                return;
            }
        }

        [HarmonyPatch(nameof(Vehicle.Update))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /* This is basically a prefix for Vehicle.Update,
             * but we choose to transpile instead,
             * so that our code may be considered "core."
             * That is, it will be skipped if any other Prefix returns false.
             * This is desirable to be as "alike" normal Vehicles as possible;
             * in particular, this ensures compatibility with FreeLook
             * We must control our ModVehicle rotation within the core Vehicle.Update code.
             */
            List<CodeInstruction> codes = new(instructions);
            List<CodeInstruction> newCodes = new(codes.Count + 2);
            CodeInstruction myNOP = new(OpCodes.Nop);
            for (int i = 0; i < codes.Count + 2; i++)
            {
                newCodes.Add(myNOP);
            }
            // push reference to vehicle
            // Call a static function which takes a vehicle and ControlsRotation if it's a ModVehicle
            newCodes[0] = new(OpCodes.Ldarg_0);
            newCodes[1] = CodeInstruction.Call(typeof(ModVehicle), nameof(ModVehicle.MaybeControlRotation));
            for (int i = 0; i < codes.Count; i++)
            {
                newCodes[i + 2] = codes[i];
            }
            return newCodes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vehicle.GetPilotingMode))]
        public static bool GetPilotingModePrefix(Vehicle __instance, ref bool __result)
        {
            VehicleTypes.Drone? mv = __instance as VehicleTypes.Drone;
            if (mv == null)
            {
                return true;
            }
            __result = Player.main.GetMode() == Player.Mode.LockedPiloting && mv.IsUnderCommand;
            return false;
        }



        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.Awake))]
        public static void VehicleAwakeHarmonyPostfix(Vehicle __instance)
        {
            Admin.GameObjectManager<Vehicle>.Register(__instance);
        }


    }
}
