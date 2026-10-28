using UnityEngine;

namespace VehicleFramework
{
    public partial class MainPatcher
    {
        internal KeyCode NextCameraKey => NautilusConfig.NextCameraKey;
        internal KeyCode PreviousCameraKey => NautilusConfig.PreviousCameraKey;
        internal KeyCode ExitCameraKey => NautilusConfig.ExitCameraKey;
        internal KeyCode HeadlightsKey => NautilusConfig.HeadlightsKey;
        internal KeyCode MagnetBootsKey => NautilusConfig.MagnetBootsKey;

        internal static bool GetKeyDown(KeyCode key)
        {
            return !GameInput.clearInput && !GameInput.scanningInput && Input.GetKeyDown(key);
        }
    }
}
