using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VehicleFramework.Assets;

namespace VehicleFramework.VehicleChildComponents
{
    public class UpgradeProxy : MonoBehaviour
    {
        public List<Transform> proxies = new List<Transform>();
        public List<VehicleUpgradeConsoleInput.Slot> slots = new List<VehicleUpgradeConsoleInput.Slot>();

        /// <summary>
        /// Begins initializing the proxy visuals after the creator assigns its references.
        /// </summary>
        public void Awake() => Admin.SessionManager.StartCoroutine(GetSeamothBitsASAP());

        /// <summary>
        /// Loads the proxy visuals from the Below Zero donor assets.
        /// </summary>
        public IEnumerator GetSeamothBitsASAP()
        {
            yield return null; // AddComponent invokes Awake before Create finishes assigning references.
            yield return BelowZeroVehicleAssets.EnsureLoaded();
            if (this == null)
            {
                yield break;
            }
            SkinnedMeshRenderer template = BelowZeroVehicleAssets.UpgradeModel;
            slots.Clear();
            for (int index = 0; index < proxies.Count; index++)
            {
                foreach (Transform child in proxies[index])
                {
                    Destroy(child.gameObject);
                }
                // Use the key's rest mesh, not its donor-owned skin/bone transforms.
                GameObject model = new GameObject("UpgradeModule");
                model.transform.SetParent(proxies[index], false);
                model.AddComponent<MeshFilter>().sharedMesh = template.sharedMesh;
                model.AddComponent<MeshRenderer>().sharedMaterials = template.sharedMaterials;
                slots.Add(new VehicleUpgradeConsoleInput.Slot
                {
                    id = VehicleBuilding.ModuleBuilder.ModVehicleModulePrefix + index,
                    model = model
                });
            }
            GetComponentInChildren<VehicleUpgradeConsoleInput>().slots = slots.ToArray();
        }
    }
}
