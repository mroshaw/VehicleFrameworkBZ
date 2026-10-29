using UnityEngine;

namespace VehicleFramework.VehicleChildComponents
{
    // BZ compatibility: bridge Vehicle's single collision object to the serialized framework collision-model array.
    internal sealed class VehicleCollisionGroup : MonoBehaviour
    {
        [SerializeField] private GameObject[] models = System.Array.Empty<GameObject>();
        private bool suppressed;

        internal void Configure(GameObject[] collisionModels)
        {
            models = collisionModels;
        }

        private void OnEnable()
        {
            if (suppressed)
            {
                SetModelsActive(true);
                suppressed = false;
            }
        }

        private void OnDisable()
        {
            // Only the explicit Vehicle.docked toggle should change authored models.
            if (!gameObject.activeSelf)
            {
                suppressed = true;
                SetModelsActive(false);
            }
        }

        private void SetModelsActive(bool active)
        {
            if (models is null)
            {
                return;
            }
            foreach (GameObject model in models)
            {
                if (model != null)
                {
                    model.SetActive(active);
                }
            }
        }
    }
}
