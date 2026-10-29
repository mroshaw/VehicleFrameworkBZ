using HarmonyLib;
using UnityEngine;
using VehicleFramework.VehicleRootComponents;

namespace VehicleFramework.Patches
{
    [HarmonyPatch(typeof(MainCameraControl))]
    internal static class MainCameraControlPatches
    {
        // SN's camOffset is 0.06277394 m up and 0.15 m back; locked piloting adds
        // 0.18 m forward at pdaCamPivot. BZ no longer supplies that piloting offset.
        private static readonly Vector3 remoteCameraOffset = new Vector3(0.0f, 0.06277394f, 0.03f);

        /// <summary>
        /// Keeps a framework remote camera at its vehicle anchor instead of the player's animated head.
        /// </summary>
        [HarmonyPatch(nameof(MainCameraControl.OnLateUpdate))]
        [HarmonyPostfix]
        private static void OnLateUpdate_Postfix(MainCameraControl __instance)
        {
            Player player = Player.main;
            if (!player || __instance.transform.IsChildOf(player.transform))
            {
                return;
            }

            if (__instance.GetComponentInParent<MVCameraController>())
            {
                // BZ overwrites this child's world position from the player skeleton every frame.
                // VF has deliberately reparented camRoot to a vehicle camera, as it does in SN.
                __instance.cameraUPTransform.localPosition = remoteCameraOffset;
            }
        }
    }
}
