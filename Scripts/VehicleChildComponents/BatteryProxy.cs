using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VehicleFramework.VehicleBuilding;
using VehicleFramework.Assets;

namespace VehicleFramework.VehicleChildComponents
{
    public class BatteryProxy : MonoBehaviour
    {
        public Transform? proxy = null;
        public EnergyMixin? mixin = null;

        internal static void Create(VehicleBattery vb, EnergyMixin energyMixin)
        {
            var model = vb.BatterySlot.gameObject.EnsureComponent<VehicleChildComponents.BatteryProxy>();
            model.proxy = vb.BatteryProxy;
            model.mixin = energyMixin;
        }

        // BZ compatibility: BZ replaces the Seamoth donor with Prawn; defer Awake initialization and create owned models in SN proxy units.
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
            if (proxy == null || mixin == null)
            {
                yield break;
            }
            yield return BelowZeroVehicleAssets.EnsureLoaded();
            if (this == null || proxy == null || mixin == null)
            {
                yield break;
            }
            foreach (Transform child in proxy)
            {
                Destroy(child.gameObject);
            }
            EnergyMixin.BatteryModels[] templates = BelowZeroVehicleAssets.BatteryTemplate.batteryModels;
            EnergyMixin.BatteryModels[] models = new EnergyMixin.BatteryModels[templates.Length];
            for (int index = 0; index < templates.Length; index++)
            {
                GameObject model = Instantiate(templates[index].model, proxy);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                // Existing vehicle proxies supply the 100x scale expected by the SN cell mesh.
                model.transform.localScale = Vector3.one * 0.01f;
                model.SetActive(false);
                models[index] = new EnergyMixin.BatteryModels
                {
                    model = model,
                    techType = templates[index].techType
                };
            }
            mixin.batteryModels = models;
            if (mixin.batterySlot != null)
            {
                mixin.NotifyHasBattery(mixin.batterySlot.storedItem);
            }
        }
    }
}
