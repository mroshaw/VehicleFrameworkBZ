using System;
using System.Collections;
using UnityEngine;
using UWE;
using Nautilus.Utility;

namespace VehicleFramework.Assets
{
    internal static class BelowZeroVehicleAssets
    {
        private static CoroutineTask<GameObject>? prawnRequest;
        private static CoroutineTask<GameObject>? seatruckRequest;
        private static bool isLoading;
        private static bool isLoaded;

        internal static GameObject Prawn { get; private set; } = null!;
        internal static GameObject Seatruck { get; private set; } = null!;
        internal static EnergyMixin BatteryTemplate { get; private set; } = null!;
        internal static StorageContainer StorageTemplate { get; private set; } = null!;
        internal static VFXConstructing ConstructionTemplate { get; private set; } = null!;
        internal static LightShadowQuality LightQuality { get; private set; } = null!;
        internal static SeaTruckLights LightSounds { get; private set; } = null!;
        internal static WaterClipProxy WaterClipTemplate { get; private set; } = null!;
        internal static Material GlassMaterial { get; private set; } = null!;
        internal static SkinnedMeshRenderer UpgradeModel { get; private set; } = null!;
        internal static FMODAsset SmallImpactSound { get; private set; } = null!;
        internal static FMODAsset MediumImpactSound { get; private set; } = null!;
        internal static FMODAsset FastImpactSound { get; private set; } = null!;

        internal static IEnumerator EnsureLoaded()
        {
            while (isLoading)
            {
                yield return null;
            }
            if (isLoaded && BatteryTemplate != null && GlassMaterial != null)
            {
                yield break;
            }
            isLoading = true;
            try
            {
                // Keep the requests/results for the lifetime of the framework. These are
                // game-owned prefab assets, not live donor vehicles to activate or mutate.
                prawnRequest = CraftData.GetPrefabForTechTypeAsync(TechType.Exosuit);
                yield return prawnRequest;
                Prawn = prawnRequest.GetResult();
                seatruckRequest = CraftData.GetPrefabForTechTypeAsync(TechType.SeaTruck);
                yield return seatruckRequest;
                Seatruck = seatruckRequest.GetResult();
                if (Prawn == null || Seatruck == null)
                {
                    throw new InvalidOperationException("Below Zero vehicle donor prefabs could not be loaded.");
                }
                BatteryTemplate = GetRequiredComponent<EnergyMixin>(Prawn, "");
                StorageTemplate = GetRequiredComponent<StorageContainer>(Prawn, "exosuit_01/root/geoChildren/upgrade_geoHldr/Storage");
                ConstructionTemplate = GetRequiredComponent<VFXConstructing>(Prawn, "");
                LightQuality = GetRequiredComponent<LightShadowQuality>(Prawn, "lights_parent/light_left");
                LightSounds = GetRequiredComponent<SeaTruckLights>(Seatruck, "");
                WaterClipTemplate = GetRequiredComponent<WaterClipProxy>(Seatruck, "WaterClipProxy");
                UpgradeModel = GetRequiredComponent<SkinnedMeshRenderer>(Seatruck, "model/seatruck_anim/engine_console_key_02_geo");
                Renderer glass = GetRequiredComponent<Renderer>(Seatruck, "model/seatruck_anim/Seatruck_cabin_interior_glass_geo");
                GlassMaterial = CreateGlassMaterial(glass.sharedMaterial);
                SmallImpactSound = AudioUtils.GetFmodAsset("event:/sub/common/fishsplat");
                MediumImpactSound = AudioUtils.GetFmodAsset("event:/sub/seamoth/impact_solid_medium");
                FastImpactSound = AudioUtils.GetFmodAsset("event:/sub/seamoth/impact_solid_hard");
                isLoaded = true;
            }
            finally
            {
                isLoading = false;
            }
        }

        private static Material CreateGlassMaterial(Material template)
        {
            // Keep BZ's shader, but retain the Seamoth interior glass appearance used by
            // the original framework. Seatruck glass is nearly clear and uses WBOIT blending.
            Material material = new Material(template);
            material.name = "VehicleFramework Glass";
            material.DisableKeyword("WBOIT");
            material.EnableKeyword("MARMO_ALPHA");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetFloat("_Mode", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlend2", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend2", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_AddSrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_AddDstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_AddSrcBlend2", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_AddDstBlend2", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetColor("_Color", new Color(0.088343434f, 0.15590787f, 0.31617647f, 0.466f));
            material.SetFloat("_Fresnel", 0f);
            material.SetFloat("_SpecInt", 1f);
            material.SetFloat("_Shininess", 5.98f);
            return material;
        }

        private static T GetRequiredComponent<T>(GameObject prefab, string path) where T : Component
        {
            Transform target = path.Length == 0 ? prefab.transform : prefab.transform.Find(path);
            T component = target != null ? target.GetComponent<T>() : null;
            if (component == null)
            {
                throw new InvalidOperationException($"BZ donor {prefab.name}/{path} is missing {typeof(T).Name}.");
            }
            return component;
        }
    }
}
