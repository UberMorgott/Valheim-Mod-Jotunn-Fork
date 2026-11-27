using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Utility class for the BepInEx ConfigurationManager plugin, without requiring a hard dependency
    /// </summary>
    public static class ConfigManagerUtils
    {
        /// <summary>
        ///     The ConfigurationManager plugin instance if installed, otherwise null
        /// </summary>
        public static BaseUnityPlugin Plugin { get; private set; }

        private static Func<bool> getDisplayingWindow;
        private static Action<bool> setDisplayingWindow;
        private static MethodInfo buildSettingListMethodInfo;

        static ConfigManagerUtils()
        {
            if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out var configManagerInfo) && configManagerInfo.Instance)
            {
                BindPlugin(configManagerInfo.Instance);
            }
            else if (Chainloader.PluginInfos.TryGetValue("_shudnal.ConfigurationManager", out configManagerInfo) && configManagerInfo.Instance)
            {
                BindPlugin(configManagerInfo.Instance);
            }
            else if (Chainloader.PluginInfos.TryGetValue("lordrasmus.jotunnconfigmanager", out configManagerInfo) && configManagerInfo.Instance)
            {
                BindPlugin(configManagerInfo.Instance);
            }
        }

        private static void BindPlugin(BaseUnityPlugin plugin)
        {
            Plugin = plugin;
            var displayingWindowInfo = AccessTools.Property(Plugin.GetType(), "DisplayingWindow");
            if (displayingWindowInfo != null)
            {
                var getter = displayingWindowInfo.GetGetMethod(true);
                if (getter != null)
                {
                    getDisplayingWindow = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), Plugin, getter);
                }
                var setter = displayingWindowInfo.GetSetMethod(true);
                if (setter != null)
                {
                    setDisplayingWindow = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), Plugin, setter);
                }
            }
            buildSettingListMethodInfo = AccessTools.Method(Plugin.GetType(), "BuildSettingList");
        }

        /// <summary>
        ///     Is the config manager main window displayed on screen<br />
        ///     Safe to use even if ConfigurationManager is not installed.
        /// </summary>
        public static bool DisplayingWindow
        {
            get => Plugin && getDisplayingWindow != null && getDisplayingWindow();
            set => setDisplayingWindow?.Invoke(value);
        }

        /// <summary>
        ///     Rebuild the setting list. Use to update the config manager window if config settings were removed or added while it was open.<br />
        ///     Safe to call even if ConfigurationManager is not installed.
        /// </summary>
        public static void BuildSettingList()
        {
            if (Plugin)
            {
                buildSettingListMethodInfo.Invoke(Plugin, null);
            }
        }
    }
}
