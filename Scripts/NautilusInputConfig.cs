// BZ compatibility: BZ Nautilus lacks the SN input-registration API; shortcuts use Unity KeyCode bindings.
using UnityEngine;

namespace VehicleFramework
{
    public partial class MainPatcher
    {
        // BZ compatibility: BZ Nautilus lacks custom GameInput buttons; respect BZ input suppression when polling the configured keys.
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
