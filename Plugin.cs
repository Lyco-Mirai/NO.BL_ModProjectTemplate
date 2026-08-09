using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MyCustomAddon
{
    public static class MyPluginInfo // Keep this name as-is, for namespace referance reasons...
    {
        // Mandetory
        public const string PLUGIN_GUID = "com.name.addon";
        public const string PLUGIN_NAME = "My Custom Addon";
        public const string PLUGIN_VERSION = "0.0.1";
        // Optional
        public const string PLUGIN_DESCRIPTION = "Some unique and interesting, detailing description...";
        public const string PLUGIN_GAME_VERSION = "0.34.1";
        public const string PLUGIN_AUTHOR_GROUP = "Blight Landsystems";
        public const string PLUGIN_PRIMARY_AUTHOR = "MLonsdale";
        public static readonly string[] PLUGIN_AUTHORS = new string[] { "MLonsdale" };
        public static readonly string[] PLUGIN_CONTRUBUTORS = new string[] { "MLonsdale" };
    }

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger;
        private Harmony harmony;
        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo((object)$"[{MyPluginInfo.PLUGIN_AUTHOR_GROUP}] '{MyPluginInfo.PLUGIN_NAME}' active.");

            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo((object)$"[{MyPluginInfo.PLUGIN_AUTHOR_GROUP}] Patches applied, awaiting Blueprinter load sequence.");
        }
    }
}
