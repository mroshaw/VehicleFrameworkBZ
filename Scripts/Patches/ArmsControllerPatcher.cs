using HarmonyLib;
using DaftAppleGames.VehicleFrameworkBZ.Extensions;

// PURPOSE: Allow custom handling of the player's body during ModVehicle piloting
// VALUE: Moderate-low. Convenient for developers. Maybe better to do it in a per-vehicle way

namespace DaftAppleGames.VehicleFrameworkBZ.Patches
{
    [HarmonyPatch(typeof(ArmsController))]
    public class ArmsControllerPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ArmsController.Update))]
        public static void ArmsControllerUpdatePostfix()
        {
            Player.main.GetModVehicle()?.HandlePilotingAnimations();
        }
    }
}
