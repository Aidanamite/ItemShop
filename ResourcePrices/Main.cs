using HarmonyLib;
using InControl;
using SRML;
using SRML.Config.Attributes;
using SRML.Console;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SECTR_AudioSystem;
using Console = SRML.Console.Console;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ResourcePrices
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{System.Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        public static Main instance;

        public Main() => instance = this;

        public override void PreLoad()
        {
            HarmonyInstance.PatchAll();
        }
        public static void Log(string message) => instance.ConsoleInstance.Log($"[{modName}]: " + message);
        public static void LogError(string message) => instance.ConsoleInstance.LogError($"[{modName}]: " + message);
        public static void LogWarning(string message) => instance.ConsoleInstance.LogWarning($"[{modName}]: " + message);
        public static void LogSuccess(string message) => instance.ConsoleInstance.LogSuccess($"[{modName}]: " + message);

        public static float FigureOutValue(Identifiable.Id id)
        {
            var min = GetRariety(id);
            if (min > 0)
                return (float)Math.Round(Config.BaseValue / Math.Pow(min, Config.CostFactor));
            return 100;
        }
        public static float GetRariety(Identifiable.Id id)
        {
            var min = 2f;
            foreach (var e in Gadget.EXTRACTOR_CLASS)
            {
                var ps = GameContext.Instance.LookupDirector.GetGadgetDefinition(e)?.prefab?.GetComponent<Extractor>()?.produces;
                if (ps != null)
                {

                    var s = new Dictionary<ZoneDirector.Zone, float>();
                    var w = new Dictionary<ZoneDirector.Zone, float>();
                    foreach (var p in ps)
                    {
                        var z = p.restrictZone ? p.zone : ZoneDirector.Zone.NONE;
                        s[z] = s.GetOrDefault(z) + p.weight;
                        if (p.id == id)
                            w[z] = p.weight;
                    }
                    foreach (var i in w)
                        if (i.Value > 0)
                        {
                            var cs = s[i.Key];
                            if (i.Key != ZoneDirector.Zone.NONE)
                                cs += s.GetOrDefault(ZoneDirector.Zone.NONE);
                            if (i.Value / cs < min)
                                min = i.Value / cs;
                        }
                }
            }
            if (min <= 1)
                return min;
            return -1;
        }
    }

    [ConfigFile("settings")]
    public static class Config
    {
        public static double CostFactor = 0.301;
        public static double BaseValue => SceneContext.Instance.ExchangeDirector.valueDict[Identifiable.Id.STRANGE_DIAMOND_CRAFT] * Math.Pow(Main.GetRariety(Identifiable.Id.STRANGE_DIAMOND_CRAFT),CostFactor);
    }

    [HarmonyPatch(typeof(ExchangeDirector),"Awake")]
    static class Patch_ExchangeDirector
    {
        static void Postfix(ExchangeDirector __instance)
        {
            foreach (var i in Identifiable.CRAFT_CLASS)
                if (!__instance.valueDict.ContainsKey(i))
                    __instance.valueDict[i] = Main.FigureOutValue(i);
        }
    }
}