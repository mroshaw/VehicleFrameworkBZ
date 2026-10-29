using Nautilus.Options;
using Nautilus.Options.Attributes;
using System.Reflection;
using UnityEngine;

namespace VehicleFramework
{
    [Menu("Vehicle Framework")]
    internal class VehicleFrameworkNautilusConfig : Nautilus.Json.ConfigFile
    {
        [Button(LabelLanguageId = "VFConfigManagerLabel", TooltipLanguageId = "VFConfigManagerTooltip")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Config will totally die if this is marked static.")]
        public void PrintConfigManagerInfo(ButtonClickedEventArgs _)
        {
            ErrorMessage.AddMessage(Language.main.Get("VFConfigManagerPrintInfo"));
        }
        // BZ compatibility: BZ Nautilus has no SN GameInputHandler; expose the same shortcuts through its keybind settings.

        [Keybind(LabelLanguageId = "OptionVFNextCameraButton")]
        public KeyCode NextCameraKey { get; set; } = KeyCode.F;

        [Keybind(LabelLanguageId = "OptionVFPrevCameraButton")]
        public KeyCode PreviousCameraKey { get; set; } = KeyCode.T;

        [Keybind(LabelLanguageId = "OptionVFExitCameraButton")]
        public KeyCode ExitCameraKey { get; set; } = KeyCode.V;

        [Keybind(LabelLanguageId = "OptionVFHeadlightsButton")]
        public KeyCode HeadlightsKey { get; set; } = KeyCode.Mouse0;

        [Keybind(LabelLanguageId = "OptionVFMagBootsButton")]
        public KeyCode MagnetBootsKey { get; set; } = KeyCode.G;

        #region accessibility
        [Toggle(LabelLanguageId = "VFFlashingLightsLabel", TooltipLanguageId = "VFFlashingLightsTooltip")]
        public bool IsFlashingLights = false;

        [Toggle(LabelLanguageId = "VFSubtitlesLabel", TooltipLanguageId = "VFSubtitlesTooltip")]
        public bool IsSubtitles = true;

        [Toggle(LabelLanguageId = "VFFahrenheit", TooltipLanguageId = "VFFahrenheitTooltip")]
        public bool IsFahrenheit = false;

        [Toggle(LabelLanguageId = "VFDebugLogLabel", TooltipLanguageId = "VFDebugLogTooltip")]
        public bool IsDebugLogging = false;

        [Toggle(LabelLanguageId = "VFCompatCheckToggleLabel", TooltipLanguageId = "VFCompatCheckToggleTooltip")]
        public bool IsCompatChecking = true;
        #endregion

        #region cheats
        [Button(LabelLanguageId = "VFDroneStationCheatLabel", TooltipLanguageId = "VFDroneStationCheatTooltip")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Config will totally die if this is marked static.")]
        public void UnlockDroneStation(ButtonClickedEventArgs _)
        {
            if(Player.main == null)
            {
                ErrorMessage.AddWarning(Language.main.Get("VFDroneStationError"));
            }
            else
            {
                DevConsole.SendConsoleCommand("unlock dronestation");
            }
        }
        #endregion
    }
}
