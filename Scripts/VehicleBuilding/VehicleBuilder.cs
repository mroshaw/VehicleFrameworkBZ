using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleFramework.Admin;
using VehicleFramework.Assets;
using VehicleFramework.Engines;
using VehicleFramework.VehicleChildComponents;
using VehicleFramework.VehicleTypes;
using static ConsoleInput;

namespace VehicleFramework.VehicleBuilding
{
    public struct VehicleEntry
    {
        public VehicleEntry(ModVehicle inputMv, int id, PingType pt_in, Sprite sprite, TechType tt=(TechType)0)
        {
            mv = inputMv ?? throw SessionManager.Fatal("Vehicle Entry cannot take a null mod vehicle");
            unique_id = id;
            pt = pt_in;
            name = mv.name;
            techType = tt;
            ping_sprite = sprite;
        }
        public ModVehicle mv;
        public string name;
        public int unique_id;
        public PingType pt;
        public Sprite ping_sprite;
        public TechType techType;
    }

    // You must not pass null ModVehicles to VehicleBuilder.
    internal static class VehicleBuilder
    {
        private static int numVehicleTypes = 0;
        internal static List<ModVehicle> prefabs = new();
        internal static IEnumerator Prefabricate(ModVehicle mv, PingType pingType, bool verbose)
        {
            VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "Prefabricating the " + mv.gameObject.name);
            yield return BelowZeroVehicleAssets.EnsureLoaded();
            if(!Instrument(mv, pingType))
            {
                Logger.Error("Failed to instrument the vehicle: " + mv.gameObject.name);
                Logger.LoopMainMenuError($"Failed prefabrication. Not registered. See log.", mv.gameObject.name);
                yield break;
            }
            prefabs.Add(mv);
            VehicleEntry ve = new(mv, numVehicleTypes, pingType, mv.PingSprite ?? Assets.StaticAssets.DefaultPingSprite);
            numVehicleTypes++;
            VehicleEntry naiveVE = new(ve.mv, ve.unique_id, ve.pt, ve.ping_sprite, TechType.None);
            VehicleManager.vehicleTypes.Add(naiveVE); // must add/remove this vehicle entry so that we can call VFConfig.Setup.
            VFConfig.Setup(mv); // must call VFConfig.Setup so that VNI.PatchCraftable can access the per-vehicle config
            VehicleNautilusInterface.PatchCraftable(ref ve, verbose);
            VehicleManager.vehicleTypes.Remove(naiveVE); // must remove this vehicle entry bc PatchCraftable adds a more complete one (with tech type)
            mv.gameObject.SetActive(true);
        }
        private static string GetBuilderErrorPrefix(ModVehicle mv, string issueBlockName)
        {
            return $"[VehicleBuilder] Error while building vehicle '{mv?.HullName ?? "NULL MODVEHICLE"}' regarding {issueBlockName}:";
        }
        #region setup_funcs
        internal static bool SetupInnateStorages(ModVehicle mv)
        {
            // Wow, look at this:
            // This Nautilus line might be super nice if it works for us
            // allow it to be opened as a storage container:
            //PrefabUtils.AddStorageContainer(obj, "StorageRoot", "TallLocker", 3, 8, true);
            int iter = 0;
            try
            {
                if (mv.InnateStorages != null)
                {
                    foreach (VehicleStorage vs in mv.InnateStorages)
                    {
                        vs.Container.SetActive(false);
                        InnateStorageContainer.Create(vs, mv, iter++);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"{GetBuilderErrorPrefix(mv, "Innate Storage")} Probably for some InnateStorage innate, we have innate.Container==null. Check your ModVehicle.InnateStorages override.");
                Logger.Error(e.ToString());
                return false;
            }
            return true;
        }
        internal static bool SetupModularStorages(ModVehicle mv)
        {
            // Wow, look at this:
            // This Nautilus line might be super nice if it works for us
            // allow it to be opened as a storage container:
            //PrefabUtils.AddStorageContainer(obj, "StorageRoot", "TallLocker", 3, 8, true);
            int iter = 0;
            try
            {
                if (mv.ModularStorages != null)
                {
                    foreach (VehicleStorage vs in mv.ModularStorages)
                    {
                        vs.Container.SetActive(false);

                        if (BelowZeroVehicleAssets.Prawn == null)
                        {
                            throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
                        }

                        FMODAsset storageCloseSound = BelowZeroVehicleAssets.StorageTemplate.closeSound;
                        FMODAsset storageOpenSound = BelowZeroVehicleAssets.StorageTemplate.openSound;
                        var inp = vs.Container.EnsureComponent<ModularStorageInput>();
                        inp.mv = mv;
                        inp.slotID = iter;
                        iter++;
                        inp.model = vs.Container;
                        if (vs.Container.GetComponentInChildren<Collider>() == null)
                        {
                            inp.collider = vs.Container.EnsureComponent<BoxCollider>();
                        }
                        inp.openSound = storageOpenSound;
                        inp.closeSound = storageCloseSound;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"{GetBuilderErrorPrefix(mv, "Modular Storage")} Probably for some VehicleStorage modular, we have modular.Container==null. Check your ModVehicle.ModularStorages override.");
                Logger.Error(e.ToString());
                return false;
            }
            return true;
        }
        private static bool ValidateConsole(ModVehicle mv, VehicleUpgrades vu)
        {
            if (vu.Interface == null)
            {
                Logger.Error($"{GetBuilderErrorPrefix(mv, "Upgrades Console")} The VehicleUpgrade.Interface was null.");
                return false;
            }
            if (vu.Interface.GetComponentInChildren<Collider>() == null)
            {
                Logger.Error($"{GetBuilderErrorPrefix(mv, "Upgrades Console")} There is no Collider on the VehicleUpgrade.Interface.");
                return false;
            }
            return true;
        }
        private static void InnerSetupConsole(ModVehicle mv, VehicleUpgrades vu)
        {
            VehicleUpgradeConsoleInput vuci = vu.Interface.EnsureComponent<VehicleUpgradeConsoleInput>();
            vuci.flap = vu.Flap?.transform ?? vu.Interface.transform;
            vuci.anglesOpened = vu.AnglesOpened ?? Vector3.zero;
            vuci.anglesClosed = vu.AnglesClosed ?? Vector3.zero;
            vuci.collider = vuci.GetComponentInChildren<Collider>();
            mv.upgradesInput = vuci;
            var up = vu.Interface.EnsureComponent<UpgradeProxy>();
            up.proxies = vu.ModuleProxies ?? new();
            SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vu.Interface.transform);
            vu.Interface.EnsureComponent<SaveLoad.VFUpgradesIdentifier>();
        }
        internal static bool SetupUpgradesConsole(ModVehicle mv)
        {
            if (mv == null) return false;
            List<VehicleUpgrades>? ups = mv.Upgrades;
            if (ups == null || ups.Count == 0)
            {
                VehicleUpgradeConsoleInput vuci = mv.VehicleModel.EnsureComponent<VehicleUpgradeConsoleInput>();
                vuci.enabled = false;
                BoxCollider innerCollider = mv.VehicleModel.AddComponent<BoxCollider>();
                vuci.collider = innerCollider;
                innerCollider.size = Vector3.zero;
                mv.upgradesInput = vuci;
                return true;
            }
            if (ups.Select(x => ValidateConsole(mv, x)).Where(x => x == false).Any())
            {
                Logger.Error($"{GetBuilderErrorPrefix(mv, "Upgrades Console")} Failed to Validate the Upgrade Console.");
                return false;
            }
            ups.ForEach(x => InnerSetupConsole(mv, x));
            return true;
        }
        internal static void SetupBoundingBox(ModVehicle mv)
        {
            if (mv.BoundingBoxCollider == null)
            {
                mv.BoundingBoxCollider = mv.VehicleModel.GetComponent<BoxCollider>();
            }
            if (mv.BoundingBoxCollider == null)
            {
                mv.BoundingBoxCollider = mv.VehicleModel.AddComponent<BoxCollider>();
                if(mv.BoundingBoxCollider == null)
                {
                    throw Admin.SessionManager.Fatal($"Failed to SetupBoundingBox for ModVehicle: {mv.HullName}. Check the BoundingBoxCollider override.");
                }
                mv.BoundingBoxCollider.size = new(6, 8, 12);
                Logger.Warn($"The {mv.HullName} has been given a default BoundingBox of size 6x8x12.");
            }
            mv.BoundingBoxCollider.enabled = false;
        }
        internal static bool SetupObjects(ModVehicle mv)
        {
            SetupBoundingBox(mv);
            bool result = SetupInnateStorages(mv) &&
                SetupModularStorages(mv) &&
                SetupUpgradesConsole(mv);
            return result;
        }
        internal static void SetupObjects(Submarine mv)
        {
            PilotingTrigger.Create(mv);
            mv.Hatches?.ForEach(x => VehicleChildComponents.VehicleHatch.Create(x, mv));
            ControlPanel.ControlPanel.Create(mv);
        }
        internal static void SetupObjects(Submersible mv)
        {
            PilotingTrigger.Create(mv);
            mv.Hatches?.ForEach(x => VehicleChildComponents.VehicleHatch.Create(x, mv));
        }
        internal static void SetupEnergyInterface(ModVehicle mv)
        {
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            EnergyMixin batteryTemplate = BelowZeroVehicleAssets.BatteryTemplate;
            var eInterf = mv.gameObject.EnsureComponent<EnergyInterface>();
            List<EnergyMixin> energyMixins = new();
            if (mv.Batteries == null || mv.Batteries.Count == 0)
            {
                // Configure energy mixin for this battery slot
                var energyMixin = mv.gameObject.AddComponent<ForeverBattery>();
                energyMixin.storageRoot = mv.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                energyMixin.defaultBattery = batteryTemplate.defaultBattery;
                energyMixin.compatibleBatteries = new List<TechType>(batteryTemplate.compatibleBatteries);
                energyMixin.soundPowerUp = batteryTemplate.soundPowerUp;
                energyMixin.soundPowerDown = batteryTemplate.soundPowerDown;
                energyMixin.soundBatteryAdd = batteryTemplate.soundBatteryAdd;
                energyMixin.soundBatteryRemove = batteryTemplate.soundBatteryRemove;
                if (energyMixin.controlledObjects is null)
                {
                    energyMixin.controlledObjects = Array.Empty<GameObject>();
                }
                energyMixin.batteryModels = Array.Empty<EnergyMixin.BatteryModels>();
                energyMixin.controlledObjects = Array.Empty<GameObject>();
                energyMixins.Add(energyMixin);
                eInterf.sources = energyMixins.ToArray();
                mv.energyInterface = eInterf;
                return;
            }
            foreach (VehicleBattery vb in mv.Batteries)
            {
                // Configure energy mixin for this battery slot
                var energyMixin = vb.BatterySlot.EnsureComponent<EnergyMixin>();
                energyMixin.storageRoot = mv.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                energyMixin.defaultBattery = batteryTemplate.defaultBattery;
                energyMixin.compatibleBatteries = new List<TechType>(batteryTemplate.compatibleBatteries);
                energyMixin.soundPowerUp = batteryTemplate.soundPowerUp;
                energyMixin.soundPowerDown = batteryTemplate.soundPowerDown;
                energyMixin.soundBatteryAdd = batteryTemplate.soundBatteryAdd;
                energyMixin.soundBatteryRemove = batteryTemplate.soundBatteryRemove;
                if (energyMixin.controlledObjects is null)
                {
                    energyMixin.controlledObjects = Array.Empty<GameObject>();
                }
                energyMixin.batteryModels = Array.Empty<EnergyMixin.BatteryModels>();
                energyMixins.Add(energyMixin);
                var tmp = vb.BatterySlot.EnsureComponent<VehicleBatteryInput>();
                tmp.mixin = energyMixin;
                tmp.tooltip = "VFVehicleBattery";
                BatteryProxy.Create(vb, energyMixin);
                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.BatterySlot.transform);
                vb.BatterySlot.EnsureComponent<SaveLoad.VFBatteryIdentifier>();
                eInterf.sources = energyMixins.ToArray();
                mv.energyInterface = eInterf;
            }
        }
        internal static void SetupLightSounds(ModVehicle mv)
        {
            if (mv.LightsOnSound == null)
            {
                mv.LightsOnSound = mv.gameObject.AddComponent<FMOD_CustomEmitter>();
                mv.LightsOnSound.asset = BelowZeroVehicleAssets.LightSounds.onSound;
            }
            if (mv.LightsOffSound == null)
            {
                mv.LightsOffSound = mv.gameObject.AddComponent<FMOD_CustomEmitter>();
                mv.LightsOffSound.asset = BelowZeroVehicleAssets.LightSounds.offSound;
            }
        }
        internal static void SetupLights(ModVehicle mv, List<VehicleFloodLight>? lights)
        {
            if (lights == null || lights.Count == 0)
            {
                return;
            }
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            GameObject prawnHeadLight = BelowZeroVehicleAssets.Prawn.transform.Find("lights_parent/light_left").gameObject;
            foreach (VehicleFloodLight pc in lights)
            {

                var thisLight = pc.Light.EnsureComponent<Light>();
                LightShadowQuality quality = pc.Light.EnsureComponent<LightShadowQuality>();
                quality.qualityPairs = BelowZeroVehicleAssets.LightQuality.qualityPairs;
                quality.light = thisLight;
                thisLight.type = LightType.Spot;
                thisLight.spotAngle = pc.Angle;
                thisLight.innerSpotAngle = pc.Angle * .75f;
                thisLight.color = pc.Color;
                thisLight.intensity = pc.Intensity;
                thisLight.range = pc.Range;
                thisLight.shadows = LightShadows.Hard;
                thisLight.gameObject.SetActive(false);

                var RLS = mv.gameObject.AddComponent<RegistredLightSource>();
                RLS.hostLight = thisLight;
            }
        }
        internal static void SetupVolumetricLights(ModVehicle mv)
        {
            // BZ's stock Prawn/Seatruck headlights do not have the SN cone effect.
            // Preserve explicitly authored effects without instantiating a legacy Seamoth.
            mv.volumetricLights.Clear();
            foreach (VFXVolumetricLight light in mv.GetComponentsInChildren<VFXVolumetricLight>(true))
            {
                if (light.volumGO != null)
                {
                    mv.volumetricLights.Add(light.volumGO);
                }
            }
        }
        internal static void SetupLiveMixin(ModVehicle mv)
        {
            var liveMixin = mv.gameObject.EnsureComponent<LiveMixin>();
            var lmData = ScriptableObject.CreateInstance<LiveMixinData>();
            lmData.canResurrect = true;
            lmData.broadcastKillOnDeath = true;
            lmData.destroyOnDeath = false;
            // NEWNEW
            // What's going to happen when a vdehicle dies now?
            //lmData.explodeOnDestroy = true;
            lmData.weldable = true;
            lmData.minDamageForSound = 20f;
            /*
             * Other Max Health Values
             * Seamoth: 200
             * Prawn: 600
             * Odyssey: 667
             * Atrama: 1000
             * Abyss: 1250
             * Cyclops: 1500
             */
            lmData.maxHealth = mv.MaxHealth;
            liveMixin.health = mv.MaxHealth;
            liveMixin.data = lmData;
            mv.liveMixin = liveMixin;
        }
        internal static void SetupRigidbody(ModVehicle mv)
        {
            var rb = mv.gameObject.EnsureComponent<Rigidbody>();
            /* 
             * For reference,
             * Cyclop: 12000
             * Abyss: 5000
             * Atrama: 4250
             * Odyssey: 3500
             * Prawn: 1250
             * Seamoth: 800
             */
            rb.mass = mv.Mass;
            rb.drag = 10f;
            rb.angularDrag = 10f;
            rb.useGravity = false;
            mv.useRigidbody = rb;
        }
        internal static void SetupEngine(ModVehicle mv)
        {
            mv.stabilizeRoll = true;
            mv.controlSheme = Admin.EnumHelper.GetScheme();
            mv.mainAnimator = mv.gameObject.EnsureComponent<Animator>();
            switch (mv)
            {
                case Submarine:
                    mv.VFEngine ??= mv.gameObject.AddComponent<OdysseyEngine>();
                    break;
                case Submersible:
                    mv.VFEngine ??= mv.gameObject.AddComponent<CricketEngine>();
                    break;
                case Drone:
                    mv.VFEngine ??= mv.gameObject.AddComponent<CricketEngine>();
                    break;
            }
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            mv.splashSound = BelowZeroVehicleAssets.Prawn.GetComponent<Exosuit>().splashSound;
            //atrama.vehicle.bubbles = CopyComponent<ParticleSystem>(seamoth.GetComponent<SeaMoth>().bubbles, atrama.vehicle.gameObject);
        }
        internal static void SetupWorldForces(ModVehicle mv)
        {
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            mv.worldForces = CopyComponent<WorldForces>(BelowZeroVehicleAssets.Prawn.GetComponent<WorldForces>(), mv.gameObject)
                ?? throw Admin.SessionManager.Fatal($"Failed to copy WorldForces from {BelowZeroVehicleAssets.Prawn.name} to {mv.gameObject.name}!");
            mv.worldForces.useRigidbody = mv.useRigidbody;
            mv.worldForces.underwaterGravity = 0f;
            mv.worldForces.aboveWaterGravity = 9.8f;
            mv.worldForces.waterDepth = 0f;
            mv.worldForces.handleDrag = true;
            mv.worldForces.underwaterDrag = 1f;
        }
        internal static void SetupHudPing(ModVehicle mv, PingType pingType)
        {
            var temp = mv.gameObject.EnsureComponent<PingInstance>();
            temp.origin = mv.transform;
            temp.pingType = pingType;
            temp.SetLabel("Vehicle");
            VFPingManager.mvPings.Add(temp);
        }
        internal static void SetupCrushDamage(ModVehicle mv)
        {
            var ce = mv.gameObject.AddComponent<FMOD_CustomEmitter>();
            ce.restartOnPlay = true;
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            foreach (var thisCE in BelowZeroVehicleAssets.Prawn.GetComponentsInChildren<FMOD_CustomEmitter>())
            {
                if (thisCE.name == "crushDamageSound")
                {
                    ce.asset = thisCE.asset;
                }
            }
            /* For reference,
             * Prawn dies from max health in 3:00 minutes.
             * Seamoth in 0:30
             * Cyclops in 3:45
             * So ModVehicles can die in 3:00 as well
             */
            mv.crushDamage = mv.gameObject.EnsureComponent<CrushDamage>();
            mv.crushDamage.soundOnDamage = ce;
            mv.crushDamage.kBaseCrushDepth = mv.BaseCrushDepth;
            mv.crushDamage.damagePerCrush = mv.CrushDamage;
            mv.crushDamage.crushPeriod = mv.CrushPeriod;
            mv.crushDamage.vehicle = mv;
            mv.crushDamage.liveMixin = mv.liveMixin;
            // TODO: this is of type VoiceNotification
            mv.crushDamage.crushDepthUpdate = null;
        }
        internal static void SetupWaterClipping(ModVehicle mv)
        {
            if (mv.WaterClipProxies == null)
            {
                return;
            }
            // Enable water clipping for proper interaction with the surface of the ocean
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            WaterClipProxy waterClipTemplate = BelowZeroVehicleAssets.WaterClipTemplate;
            foreach (GameObject proxy in mv.WaterClipProxies)
            {
                WaterClipProxy waterClip = proxy.AddComponent<WaterClipProxy>();
                waterClip.shape = WaterClipProxy.Shape.Box;
                // Retain the authored box shape while reusing BZ's clip material and layer.
                waterClip.clipMaterial = waterClipTemplate.clipMaterial;
                //"""You need to do this. By default the layer is 0. This makes it displace everything in the default rendering layer. We only want to displace water.""" -Lee23
                waterClip.gameObject.layer = waterClipTemplate.gameObject.layer;
            }
        }
        internal static void SetupSubName(ModVehicle mv)
        {
            var subname = mv.gameObject.EnsureComponent<SubName>();
            subname.pingInstance = mv.GetComponent<PingInstance>();
            subname.colorsInitialized = 0;
            subname.hullName = mv.StorageRootObject.AddComponent<TMPro.TextMeshProUGUI>(); // DO NOT push a TMPro.TextMeshProUGUI on the root vehicle object!!!

            mv.subName = subname;
            mv.SetName(mv.vehicleDefaultName);
        }
        internal static void SetupCollisionSound(ModVehicle mv)
        {
            CollisionSound sound = mv.gameObject.EnsureComponent<CollisionSound>();
            sound.hitSoundSmall = BelowZeroVehicleAssets.SmallImpactSound;
            sound.hitSoundSlow = BelowZeroVehicleAssets.Prawn.GetComponent<SoundOnDamage>().sound;
            sound.hitSoundMedium = BelowZeroVehicleAssets.MediumImpactSound;
            sound.hitSoundFast = BelowZeroVehicleAssets.FastImpactSound;
        }
        internal static void SetupOutOfBoundsWarp(ModVehicle mv)
        {
            mv.gameObject.EnsureComponent<OutOfBoundsWarp>();
        }
        internal static void SetupConstructionObstacle(ModVehicle mv)
        {
            var co = mv.gameObject.EnsureComponent<ConstructionObstacle>();
            co.reason = $"{mv.HullName} is in the way.";
        }
        internal static void SetupSoundOnDamage(ModVehicle mv)
        {
            // TODO: we could have unique sounds for each damage type
            // TODO: this might not work, might need to put it in a VehicleStatusListener
            var sod = mv.gameObject.EnsureComponent<SoundOnDamage>();
            sod.damageType = DamageType.Normal;
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            sod.sound = BelowZeroVehicleAssets.Prawn.GetComponent<SoundOnDamage>().sound;
        }
        internal static void SetupDealDamageOnImpact(ModVehicle mv)
        {
            var ddoi = mv.gameObject.EnsureComponent<DealDamageOnImpact>();
            // NEWNEW
            // ddoi.damageTerrain = true;
            ddoi.speedMinimumForSelfDamage = 4;
            ddoi.speedMinimumForDamage = 2;
            ddoi.affectsEcosystem = true;
            ddoi.minimumMassForDamage = 5;
            ddoi.mirroredSelfDamage = true;
            ddoi.mirroredSelfDamageFraction = 0.5f;
            ddoi.capMirrorDamage = -1;
            ddoi.minDamageInterval = 0;
            ddoi.timeLastDamage = 0;
            ddoi.timeLastDamagedSelf = 0;
            ddoi.prevPosition = Vector3.zero;
            ddoi.prevPosition = Vector3.zero;
            ddoi.allowDamageToPlayer = false;
        }
        internal static void SetupDamageComponents(ModVehicle mv)
        {
            // add vfxvehicledamages... or not

            // add temperaturedamage
            var tempdamg = mv.gameObject.EnsureComponent<TemperatureDamage>();
            if (BelowZeroVehicleAssets.Prawn == null)
            {
                throw Admin.SessionManager.Fatal("BelowZeroVehicleAssets.Prawn is null! Did you forget to call BelowZeroVehicleAssets.EnsureLoaded()?");
            }
            tempdamg.liveMixin = mv.liveMixin;
            tempdamg.baseDamagePerSecond = 2.0f; // 10 times what the seamoth takes, since the Atrama 
            // the following configurations are the same values the seamoth takes
            tempdamg.minDamageTemperature = 70f;
            tempdamg.onlyLavaDamage = false;
            tempdamg.timeDamageStarted = -1000;
            tempdamg.timeLastDamage = 0;
            tempdamg.player = null;

            // add ecotarget
            var et = mv.gameObject.EnsureComponent<EcoTarget>();
            et.type = EcoTargetType.Shark; // same as seamoth (lol)
            et.nextUpdateTime = 0f;

            // add creatureutils
            var cr = mv.gameObject.EnsureComponent<CreatureUtils>();
            cr.setupEcoTarget = true;
            cr.setupEcoBehaviours = false;
            cr.addedComponents = new Component[1];
            cr.addedComponents[0] = et as Component;
        }
        internal static void SetupLavaLarvaAttachPoints(ModVehicle mv)
        {
            if (mv.LavaLarvaAttachPoints == null || mv.LavaLarvaAttachPoints.Count == 0)
            {
                return;
            }
            GameObject attachParent = new("AttachedLavaLarvae");
            attachParent.transform.SetParent(mv.transform);
            attachParent.AddComponent<EcoTarget>().SetTargetType(EcoTargetType.HeatSource);
            var lavaLarvaTarget = attachParent.AddComponent<LavaLarvaTarget>();
            lavaLarvaTarget.energyInterface = mv.energyInterface;
            lavaLarvaTarget.larvaePrefabRoot = attachParent.transform;
            lavaLarvaTarget.liveMixin = mv.liveMixin;
            lavaLarvaTarget.primiryPointsCount = mv.LavaLarvaAttachPoints.Count;
            lavaLarvaTarget.vehicle = mv;
            lavaLarvaTarget.subControl = null;
            List<LavaLarvaAttachPoint> llapList = new();
            foreach (var llap in mv.LavaLarvaAttachPoints)
            {
                GameObject llapGO = new();
                llapGO.transform.SetParent(attachParent.transform);
                var thisLlap = llapGO.AddComponent<LavaLarvaAttachPoint>();
                thisLlap.Clear();
                llapList.Add(thisLlap);
                llapGO.transform.localPosition = attachParent.transform.InverseTransformPoint(llap.position);
                llapGO.transform.localEulerAngles = attachParent.transform.InverseTransformDirection(llap.eulerAngles);
            }
            lavaLarvaTarget.attachPoints = llapList.ToArray();
        }
        internal static void SetupSubRoot(Submarine mv, PowerRelay powerRelay)
        {
            var subroot = mv.gameObject.EnsureComponent<SubRoot>();
            subroot.rb = mv.useRigidbody;
            subroot.worldForces = mv.worldForces;
            subroot.modulesRoot = mv.modulesRoot.transform;
            subroot.powerRelay = powerRelay;
            (mv.RespawnPoint ?? mv.gameObject).EnsureComponent<RespawnPoint>();
        }
        internal static void SetupCameraController(ModVehicle mv)
        {
            if (mv.Cameras == null || mv.Cameras.Count == 0)
            {
                return;
            }
            var camController = mv.gameObject.EnsureComponent<VehicleRootComponents.MVCameraController>();
            mv.Cameras.ForEach(x => camController.AddCamera(x.camera, x.name));
        }
        internal static void SetupDenyBuildingTags(ModVehicle mv)
        {
            mv.DenyBuildingColliders?.ForEach(x => x.tag = Builder.denyBuildingTag);
        }
        #endregion
        internal static bool Instrument(ModVehicle mv, PingType pingType)
        {
            SetupSubName(mv); // Prepare mv.HullName for use in error messages
            mv.StorageRootObject.EnsureComponent<ChildObjectIdentifier>();
            mv.modulesRoot = mv.ModulesRootObject.EnsureComponent<ChildObjectIdentifier>();

            if (!SetupObjects(mv as ModVehicle))
            {
                Logger.Error("Failed to SetupObjects for ModVehicle.");
                return false;
            }

            switch (mv)
            {
                case Submarine sub:
                    SetupObjects(sub);
                    break;
                case Submersible subbie:
                    SetupObjects(subbie);
                    break;
                default:
                    break;
            }

            mv.enabled = false;
            SetupEnergyInterface(mv);
            mv.enabled = true;
            SetupLights(mv, mv.HeadLights);
            SetupLightSounds(mv);
            SetupLiveMixin(mv);
            SetupRigidbody(mv);
            if (mv.disableOnDock is null)
            {
                mv.disableOnDock = System.Array.Empty<GameObject>();
            }
            if (mv.disableDockedColliders is null)
            {
                mv.disableDockedColliders = System.Array.Empty<Collider>();
            }
            Dockable dockable = mv.gameObject.EnsureComponent<Dockable>();
            dockable.vehicle = mv;
            dockable.rb = mv.useRigidbody;
            dockable.energyInterface = mv.energyInterface;
            SetupWorldForces(mv);
            SetupHudPing(mv, pingType);
            SetupCrushDamage(mv); // must be after liveMixin
            SetupWaterClipping(mv);
            SetupCollisionSound(mv);
            SetupOutOfBoundsWarp(mv);
            SetupConstructionObstacle(mv);
            SetupSoundOnDamage(mv);
            SetupDealDamageOnImpact(mv);
            SetupDamageComponents(mv);
            SetupLavaLarvaAttachPoints(mv);
            SetupDenyBuildingTags(mv);
            GameObject collisionGroup = new GameObject("VehicleCollisionGroup");
            collisionGroup.transform.SetParent(mv.transform, false);
            collisionGroup.AddComponent<VehicleCollisionGroup>().Configure(mv.CollisionModel);
            mv.collisionModel = collisionGroup;

            switch (mv)
            {
                case Submarine sub:
                    SetupEngine(sub);
                    SetupLights(sub, sub.FloodLights);
                    PowerRelay powerRelay = mv.gameObject.AddComponent<PowerRelay>(); // See PowerRelayPatcher. Allows Submarines to recharge batteries.
                    SetupSubRoot(sub, powerRelay); // depends on SetupWorldForces
                    break;
                case Submersible subbie:
                    SetupEngine(subbie);
                    break;
                case Drone drone:
                    SetupEngine(drone);
                    break;
                default:
                    break;
            }

            // ApplyShaders should happen last
            Shader shader = Shader.Find(Admin.Utils.marmosetUberName);
            ApplyShaders(mv, shader);

            return true;
        }
        internal static void ApplyGlassMaterial(ModVehicle mv)
        {
            if (mv.CanopyWindows == null || mv.CanopyWindows.Count == 0)
            {
                return;
            }
            foreach (Renderer renderer in mv.GetComponentsInChildren<Renderer>(true))
            {
                if (mv.CanopyWindows.Contains(renderer.gameObject))
                {
                    renderer.sharedMaterial = BelowZeroVehicleAssets.GlassMaterial;
                }
            }
        }
        internal static void ApplyShaders(ModVehicle mv, Shader shader)
        {
            if (mv.AutoApplyShaders)
            {
                ForceApplyShaders(mv, shader);
                ApplyGlassMaterial(mv);
            }
        }
        internal static void ForceApplyShaders(ModVehicle mv, Shader shader)
        {
            if (shader == null)
            {
                Logger.Warn("Tried to apply a null Shader.");
                return;
            }
            bool IsValidTarget(Renderer rend)
            {
                if (rend.gameObject.GetComponent<SkipMarmo>())
                {
                    return false;
                }
                if (mv.CanopyWindows != null && mv.CanopyWindows.Contains(rend.gameObject))
                {
                    return false;
                }
                return true;
            }
            // Add the [marmoset] shader to all renderers
            foreach (var material in mv.gameObject.GetComponentsInChildren<Renderer>(true).Where(IsValidTarget).SelectMany(x => x.materials))
            {
                // give it the marmo shader, no matter what
                material.shader = shader;
            }
        }
        public static T? CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.EnsureComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        public static string GetPingTypeString(CachedEnumString<PingType> _, PingType inputType)
        {
            foreach(VehicleEntry ve in VehicleManager.vehicleTypes)
            {
                if (ve.pt == inputType)
                {
                    return ve.name;
                }
            }
            foreach (var pair in Assets.SpriteHelper.PingSprites)
            {
                if (pair.Item2 == inputType)
                {
                    return pair.Item1;
                }
            }
            return PingManager.sCachedPingTypeStrings.Get(inputType);
        }
        public static Sprite GetPingTypeSprite(SpriteManager.Group _, string name, Sprite? defaultSprite)
        {
            foreach (VehicleEntry ve in VehicleManager.vehicleTypes)
            {
                if (ve.name == name)
                {
                    return ve.ping_sprite;
                }
            }
            foreach(var pair in Assets.SpriteHelper.PingSprites)
            {
                if(pair.Item1 == name)
                {
                    return pair.Item3;
                }
            }
            return SpriteManager.Get(SpriteManager.Group.Pings, name, defaultSprite);
        }

        /*
        //https://github.com/Metious/MetiousSubnauticaMods/blob/master/CustomDataboxes/API/Databox.cs
        public static void VehicleDataboxPatch(CustomDataboxes.API.Databox databox)
        {
            string result = "";

            if (string.IsNullOrEmpty(databox.DataboxID))
                result += "Missing required Info 'DataboxID'\n";
            if (string.IsNullOrEmpty(databox.PrimaryDescription))
                result += "Missing required Info 'PrimaryDescription'\n";
            if (!string.IsNullOrEmpty(result))
            {
                string msg = "Unable to patch\n" + result;
                Logger.Log(msg);
                throw new InvalidOperationException(msg);
            }

            var dataBox = new CustomDataboxes.Databoxes.CustomDatabox(DataboxID)
            {
                PrimaryDescription = this.PrimaryDescription,
                SecondaryDescription = this.SecondaryDescription,
                TechTypeToUnlock = this.TechTypeToUnlock,
                BiomesToSpawn = BiomesToSpawnIn,
                coordinatedSpawns = CoordinatedSpawns,
                ModifyGameObject = this.ModifyGameObject
            };
            dataBox.Patch();

            TechType = dataBox.TechType;
        }
        public static void ScatterDataBoxes(List<VehicleCraftable> craftables)
        {
            List<Spawnable.SpawnLocation> spawnLocations = new
            {
                new Spawnable.SpawnLocation(Vector3.zero, Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(50,0,0), Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(100,0,0), Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(200,0,0), Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(400,0,0), Vector3.zero),
            };

            foreach (var craftable in craftables)
            {
                CustomDataboxes.API.Databox myDatabox = new CustomDataboxes.API.Databox()
                {
                    DataboxID = craftable.ClassID + "_databox",
                    PrimaryDescription = craftable.FriendlyName + "_databox",
                    SecondaryDescription = "wow so cool",
                    CoordinatedSpawns = spawnLocations,
                    TechTypeToUnlock = craftable.TechType
                };
                myDatabox.Patch();
            }
        }
        */
    }
}
