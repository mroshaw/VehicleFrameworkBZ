using HarmonyLib;

// PURPOSE: player is "normally grounded" while inside a Submarine
// VALUE: High.

namespace VehicleFramework.Patches
{
    [HarmonyPatch(typeof(PlayerController))]
    public class PlayerControllerPatcher
    {
        /*
         * This patch ensures the Player behaves as expected inside a ModVehicle.
         * That is, the player should always act as "normally grounded."
         * This patch prevents the player from doing any swim-related behaviors while inside a ModVehicle
         */
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerController.HandleUnderWaterState))]
        public static bool HandleUnderWaterStatePrefix(PlayerController __instance)
        {
            VehicleTypes.Submarine? mv = Player.main.GetVehicle() as VehicleTypes.Submarine;
            if (mv != null && !mv.IsPlayerControlling())
            {
                __instance.inVehicle = true;
                __instance.underWater = false;
                __instance.groundController.SetEnabled(false);
                __instance.underWaterController.SetEnabled(false);
                __instance.activeController = __instance.groundController;
                __instance.desiredControllerHeight = __instance.standheight - __instance.cameraOffset;
                __instance.activeController.SetControllerHeight(__instance.currentControllerHeight, __instance.cameraOffset);
                // BZ compatibility: BZ drives seated camera poses through its controller; preserve its native enabled/knockback guard during resizing.
                __instance.activeController.SetEnabled(__instance.enabled && !__instance.knockback);
                return false;
            }
            return true;
        }
    }
}
