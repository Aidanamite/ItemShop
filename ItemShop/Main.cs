using HarmonyLib;
using SRML;
using SRML.Console;
using SRML.SR;
using SRML.SR.SaveSystem;
using SRML.SR.SaveSystem.Data;
using SRML.SR.Patches;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MonomiPark.SlimeRancher.Regions;
using System.Linq;
using UnityEngine.UI;
using Console = SRML.Console.Console;
using Object = UnityEngine.Object;
using System.Collections;
using SRML.Config.Attributes;

namespace ItemShop
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        internal static Vector3 shopSpawnPos = new Vector3(79.4f, 12.3f, -100);
        internal static Vector3 shopSpawnRot = Vector3.up * 82;
        internal static CorralUI uiPrefab;
        internal static bool sceneStarted = false;
        internal static CompoundDataPiece pendingData;

        public override void PreLoad()
        {
            uiPrefab = Resources.FindObjectsOfTypeAll<CorralUI>().First((x) => !x.name.EndsWith("(Clone)"));
            HarmonyInstance.PatchAll();
            SRCallbacks.OnMainMenuLoaded += (x) => sceneStarted = false;
            SaveRegistry.RegisterWorldDataLoadDelegate(ReadData);
            SaveRegistry.RegisterWorldDataSaveDelegate(WriteData);
        }
        public override void PostLoad()
        {
            GameContext.Instance.MessageDirector.RegisterBundlesListener(b =>
            {
                var dict = b.GetBundle("ui").bundle.dict;
                var lang = b.GetCultureLang();
                if (lang == MessageDirector.Lang.EN)
                    dict["b.food"] = "Food";
                else if (lang == MessageDirector.Lang.DE)
                    dict["b.food"] = "Nahrungs";
                else if (lang == MessageDirector.Lang.ES)
                    dict["b.food"] = "Comida";
                else if (lang == MessageDirector.Lang.FR)
                    dict["b.food"] = "Aliment";
                else if (lang == MessageDirector.Lang.RU)
                    dict["b.food"] = "Еды";
                else if (lang == MessageDirector.Lang.SV)
                    dict["b.food"] = "Mat";
                else if (lang == MessageDirector.Lang.ZH)
                    dict["b.food"] = "食物";
                else if (lang == MessageDirector.Lang.JA)
                    dict["b.food"] = "エサ";
                else if (lang == MessageDirector.Lang.PT)
                    dict["b.food"] = "Comida";
                else if (lang == MessageDirector.Lang.KO)
                    dict["b.food"] = "음식";
                dict["t.item_shop"] = "Item Shop";
                dict["b.plorts"] = dict["m.drone.target.name.category_plorts"];
                dict = b.GetBundle("pedia").bundle.dict;
                dict["no_desc"] = "No description available";
            });
        }
        public static void WriteData(CompoundDataPiece data)
        {
            CompoundDataPiece storage;
            if (data.HasPiece("activators"))
            {
                storage = data.GetCompoundPiece("activators");
                storage.DataList.Clear();
            }
            else
            {
                storage = new CompoundDataPiece("activators");
                data.AddPiece(storage);
            }
            int i = 0;
            foreach (var p in Resources.FindObjectsOfTypeAll<ShopUIActivator>())
            {
                var d = new CompoundDataPiece(i++.ToString());
                d.SetValue("pos", p.transform.position);
                d.SetValue("last", p.lastSpawn);
                var s = new CompoundDataPiece("cue");
                var j = 0;
                foreach (var q in p.spawnCue)
                    s.SetValue(j++.ToString(), q);
                d.AddPiece(s);
                storage.AddPiece(d);
            }
        }
        public static void ReadData(CompoundDataPiece data)
        {
            if (!sceneStarted) { 
                pendingData = data;
                return;
            }
            if (!data.HasPiece("activators"))
                return;
            var storage = data.GetCompoundPiece("activators");
            var uis = Resources.FindObjectsOfTypeAll<ShopUIActivator>();
            foreach (CompoundDataPiece p in storage.DataList)
            {
                var pos = p.GetValue<Vector3>("pos");
                var ui = uis.FirstOrDefault((x) => (x.transform.position - pos).sqrMagnitude < 0.2f);
                if (!ui)
                    continue;
                ui.lastSpawn = p.GetValue<double>("last");
                foreach (var q in p.GetCompoundPiece("cue").DataList)
                    ui.spawnCue.Add(q.GetValue<Identifiable.Id>());
            }
        }
        public static void Log(string message) => Console.Log($"[{modName}]: " + message);
        public static void LogError(string message) => Console.LogError($"[{modName}]: " + message);
        public static void LogWarning(string message) => Console.LogWarning($"[{modName}]: " + message);
        public static void LogSuccess(string message) => Console.LogSuccess($"[{modName}]: " + message);
    }

    [ConfigFile("settings")]
    public static class Config
    {
        public static double SpawnDelay = 0.1;
        public static float ScaleTime = 0.5f;
        public static float LaunchForce = 50;
    }

    static class ExtentionMethods
    {
        public static List<Y> GetAll<X, Y>(this IEnumerable<X> os, Func<X, IEnumerable<Y>> collector, bool ignoreDuplicates = true)
        {
            var l = new List<Y>();
            foreach (var o in os)
            {
                var c = collector(o);
                if (c != null)
                {
                    if (ignoreDuplicates)
                        l.AddRangeUnique(c);
                    else
                        l.AddRange(c);
                }
            }
            return l;
        }
        public static List<Y> GetAll<X, Y>(this IEnumerable<X> os, Func<X, Y> collector, bool ignoreDuplicates = true)
        {
            var l = new List<Y>();
            foreach (var o in os)
            {
                var c = collector(o);
                if (c != null)
                {
                    if (ignoreDuplicates)
                        l.AddUnique(c);
                    else
                        l.Add(c);
                }
            }
            return l;
        }

        public static void AddRangeUnique<X>(this List<X> c, IEnumerable<X> collection)
        {
            foreach (var i in collection)
                c.AddUnique(i);
        }

        public static void AddUnique<X>(this List<X> c, X i)
        {
            if (!c.Contains(i))
                c.Add(i);
        }
        public static IEnumerator ScaleTo(this Transform t, Vector3 target, float time)
        {
            var start = t.localScale;
            var passed = 0f;
            while (passed < time)
            {
                passed += Time.deltaTime;
                t.localScale = Vector3.Lerp(start, target, passed / time);
                yield return null;
            }
            yield break;
        }
    }

    public class IdentifiableData
    {
        public Identifiable.Id Id; public string NameKey; public string DescKey; public Sprite Icon; public PediaDirector.Id? PediaId; public string TabKey; public int Cost;
    }

    public class ShopUIActivator : UIActivator
    {
        public Transform ejector;
        PurchaseUI active;
        public List<Identifiable.Id> spawnCue = new List<Identifiable.Id>();
        internal double lastSpawn;
        double spawnDelay => Config.SpawnDelay * TimeDirector.SECS_PER_DAY / SceneContext.Instance.TimeDirector.secsPerGameDay;

        public override GameObject Activate() => CreateUI();

        public GameObject CreateUI()
        {
            bool flag = Input.GetKey(KeyCode.LeftShift);
            int amt = flag ? 10 : 1;
            var list = new List<PurchaseUI.Purchasable>();
            var dictionary = new Dictionary<string, List<PurchaseUI.Purchasable>>();
            foreach (var u in SceneContext.Instance.PediaDirector.pediaModel.unlocked)
            {
                var s = SceneContext.Instance.PediaDirector.identDict.FirstOrDefault((x) => x.Value == u).Key;
                if (s == Identifiable.Id.NONE) continue;
                AddEntry( GetIdentifiableData(s),list,dictionary);
                if (Identifiable.IsSlime(s) && Enum.TryParse(s.ToString().Replace("_SLIME","_PLORT"),out s))
                    AddEntry(GetIdentifiableData(s), list, dictionary);
            }
            GameObject gameObject = null;
            gameObject = GameContext.Instance.UITemplates.CreatePurchaseUI(SceneContext.Instance.ExchangeDirector.GetSpecRewardIcon(ExchangeDirector.NonIdentReward.NEWBUCKS_LARGE), MessageUtil.Qualify("ui", "t.item_shop"), list.ToArray(), false, () => Destroy(gameObject), false);
            var categories = new List<PurchaseUI.Category>();
            foreach (var p in dictionary)
                categories.Add(new PurchaseUI.Category(p.Key, p.Value.ToArray()));
            gameObject.GetComponent<PurchaseUI>().SetCategories(categories);
            gameObject.GetComponent<PurchaseUI>().SetPurchaseMsgs("b.purchase", "b.sold_out");
            active = gameObject.GetComponent<PurchaseUI>();
            return gameObject;
        }
        public void AddEntry(IdentifiableData data, List<PurchaseUI.Purchasable> list, Dictionary<string, List<PurchaseUI.Purchasable>> dictionary)
        {
            if (data.Cost == 0) return;
            var p = new PurchaseUI.Purchasable(
                data.NameKey, data.Icon, data.Icon, data.DescKey, data.Cost, data.PediaId, () => {
                    if (SceneContext.Instance.PlayerState.GetCurrency() > data.Cost) {
                        SceneContext.Instance.PlayerState.SpendCurrency(data.Cost);
                        active.PlayPurchaseFX();
                        spawnCue.Add(data.Id);
                        return;
                    }
                    active.PlayErrorCue();
                    active.Error("e.insuf_coins", false);
                }, () => true, () => true
            );
            list.Add(p);
            if (dictionary.ContainsKey(data.TabKey))
                dictionary[data.TabKey].Add(p);
            else
                dictionary.Add(data.TabKey, new List<PurchaseUI.Purchasable> { p });
        }

        void Update()
        {
            if (spawnCue.Count == 0)
            {
                lastSpawn = SceneContext.Instance.TimeDirector.WorldTime();
                return;
            }
            while (lastSpawn < SceneContext.Instance.TimeDirector.WorldTime() - spawnDelay)
            {
                lastSpawn += spawnDelay;
                var prefab = GameContext.Instance.LookupDirector.GetPrefab(spawnCue[0]);
                if (prefab)
                {
                    var obj = SRBehaviour.InstantiateActor(prefab,RegionRegistry.RegionSetId.HOME,true);
                    obj.transform.position = ejector.position + ejector.forward * 0.2f;
                    obj.transform.rotation = ejector.rotation;
                    if (Config.LaunchForce > 0)
                        obj.GetComponent<Rigidbody>()?.AddForce(ejector.forward * Config.LaunchForce);
                    if (Config.ScaleTime > 0)
                    {
                        var scale = obj.transform.localScale;
                        obj.transform.localScale = Vector3.one * 0.01f;
                        obj.GetComponent<Identifiable>().StartCoroutine(obj.transform.ScaleTo(scale, Config.ScaleTime));
                    }
                }
                spawnCue.RemoveAt(0);
            }
        }

        static List<IdentifiableData> dataCache = new List<IdentifiableData>();
        public static IdentifiableData GetIdentifiableData(Identifiable.Id Id) => GetIdentifiableData(Id, SceneContext.Instance.PediaDirector.GetPediaId(Id));
        public static IdentifiableData GetIdentifiableData(Identifiable.Id Id, PediaDirector.Id? Pedia)
        {
            var data = dataCache.Find((x) => x.Id == Id);
            if (data != null)
                return data;
            data = new IdentifiableData();
            data.Id = Id;
            data.PediaId = Pedia;
            var PIdStr = data.PediaId == null ? null : data.PediaId.Value.ToString().ToLowerInvariant();
            var IIdStr = Id.ToString().ToLowerInvariant();
            data.Icon = GameContext.Instance.LookupDirector.GetIcon(Id);
            if (GameContext.Instance.MessageDirector.Get("actor", "l." + IIdStr) != null)
                data.NameKey = MessageUtil.Qualify("actor", "l." + IIdStr);
            else if (GameContext.Instance.MessageDirector.Get("pedia", "t." + IIdStr) != null)
                data.NameKey = MessageUtil.Qualify("pedia", "t." + IIdStr);
            else if (data.PediaId != null && GameContext.Instance.MessageDirector.Get("pedia", "t." + PIdStr) != null)
                data.NameKey = MessageUtil.Qualify("pedia", "t." + PIdStr);
            if (PIdStr != null)
                data.DescKey = "m.intro." + PIdStr;
            else
                data.DescKey = "no_desc";
            if (Identifiable.IsSlime(Id))
                data.TabKey = "slimes";
            else if (Identifiable.IsFood(Id) || Identifiable.IsChick(Id))
                data.TabKey = "food";
            else if (Identifiable.IsEcho(Id) || Identifiable.IsEchoNote(Id) || Identifiable.IsOrnament(Id))
                data.TabKey = "decorations";
            else if (Identifiable.IsPlort(Id))
                data.TabKey = "plorts";
            else if (Identifiable.IsCraft(Id))
                data.TabKey = "resources";
            else
                data.TabKey = "other";
            if (SceneContext.Instance.ExchangeDirector.valueDict.TryGetValue(Id, out var value))
                data.Cost = (int)(value * (Identifiable.IsSlime(Id) ? 40 : 10));
            else if (SceneContext.Instance.EconomyDirector.currValueMap.TryGetValue(Id, out var value2))
                data.Cost = (int)(value2.baseValue * 10);
            dataCache.Add(data);
            return data;
        }
    }

    [HarmonyPatch(typeof(SceneContext),"Start")]
    class Patch_SceneStart
    {
        static void Postfix(SceneContext __instance)
        {
            if (Levels.IsLevel(Levels.WORLD))
            {
                var home = Resources.FindObjectsOfTypeAll<Region>().First((x) => x.name == "cellRanch_Home").transform;
                Renderer techBack = null;
                Renderer techScreen = null;
                Renderer techButton = null;
                Renderer techChute = null;
                foreach (var r in Resources.FindObjectsOfTypeAll<MeshRenderer>())
                {
                    if (!techBack && r.name == "techBackingSquare")
                        techBack = r;
                    if (!techScreen && r.name == "techDisplay1x1" && r.transform.Find("MarketIconUI"))
                        techScreen = r;
                    if (!techButton && r.name == "techActivator")
                        techButton = r;
                    if (!techChute && r.name == "techChute")
                        techChute = r;
                    if (techBack && techScreen && techButton && techChute)
                        break;
                }
                var shop = new GameObject("ItemShop", typeof(MeshRenderer), typeof(MeshFilter), typeof(BoxCollider)).transform;
                shop.gameObject.SetActive(false);
                shop.SetParent(home, false);
                shop.position = Main.shopSpawnPos;
                shop.rotation = Quaternion.Euler(Main.shopSpawnRot);
                shop.GetComponent<MeshRenderer>().material = techBack.material;
                shop.GetComponent<MeshFilter>().mesh = techBack.GetComponent<MeshFilter>().mesh;
                var boxCol = shop.GetComponent<BoxCollider>();
                boxCol.center = new Vector3(0, 2.25f, -1.25f);
                boxCol.size = new Vector3(2.5f, 4.5f, 2.5f);

                var screen = new GameObject(techScreen.name, typeof(MeshRenderer), typeof(MeshFilter)).transform;
                screen.SetParent(shop, false);
                screen.GetComponent<MeshRenderer>().material = techScreen.material;
                screen.GetComponent<MeshFilter>().mesh = techScreen.GetComponent<MeshFilter>().mesh;
                Object.Instantiate(techScreen.transform.Find("MarketIconUI"), screen).GetComponentInChildren<Image>().sprite = SceneContext.Instance.ExchangeDirector.GetSpecRewardIcon(ExchangeDirector.NonIdentReward.NEWBUCKS_LARGE);

                var chute = new GameObject(techChute.name, typeof(MeshRenderer), typeof(MeshFilter)).transform;
                chute.SetParent(shop, false);
                chute.localPosition = new Vector3(-1.25f, 2.4f, -1.3f);
                chute.localRotation = Quaternion.Euler(0,-90,0);
                chute.GetComponent<MeshRenderer>().material = techChute.material;
                chute.GetComponent<MeshFilter>().mesh = techChute.GetComponent<MeshFilter>().mesh;

                var button = new GameObject(techButton.name, typeof(MeshRenderer), typeof(MeshFilter), typeof(CapsuleCollider)).transform;
                button.SetParent(shop, false);
                button.localPosition = Vector3.forward * 0.3f;
                button.GetComponent<MeshRenderer>().material = techButton.material;
                button.GetComponent<MeshFilter>().mesh = techButton.GetComponent<MeshFilter>().mesh;
                var capCol = button.GetComponent<CapsuleCollider>();
                capCol.center = Vector3.up * 0.75f;
                capCol.radius = 0.2f;
                capCol.height = 1.5f;

                var inter = new GameObject("triggerActivate", typeof(SphereCollider)).transform;
                inter.SetParent(button, false);
                inter.localPosition += Vector3.up * 1.5f;
                var sphCol = inter.GetComponent<SphereCollider>();
                sphCol.radius = 0.3f;
                inter.gameObject.AddComponent<ShopUIActivator>().ejector = chute;

                shop.gameObject.SetActive(true);
                if (!Main.sceneStarted) {
                    Main.sceneStarted = true;
                    if (Main.pendingData != null)
                        Main.ReadData(Main.pendingData);
                    Main.pendingData = null;
                }
            }
        }
    }
}