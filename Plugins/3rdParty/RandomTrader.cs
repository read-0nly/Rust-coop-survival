using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Add an item to the vending machine listing so they dont show x.
 */

/* 1.0.1 
 * Optimized the code in some areas.
 * Fixed an issue with RandomTrader not initializing properly.
 * 
 */

namespace Oxide.Plugins
{
    [Info("Random Trader", "imthenewguy", "1.0.0")]
    [Description("Spawns a random vending machine along the road with a random selection of items.")]
    class RandomTrader : RustPlugin
    {
        #region Config       

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Currency type [scrap, economics, serverrewards]")]
            public string currency = "scrap";

            [JsonProperty("Trader Information")]
            public SpawnInfo trader_info = new SpawnInfo();

            [JsonProperty("Shop Information - each entry requires its own unique CopyPaste fileName.")]
            public Dictionary<string, ShopInfo> shop_info = new Dictionary<string, ShopInfo>(StringComparer.InvariantCultureIgnoreCase);

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        public class SpawnInfo
        {
            [JsonProperty("How many traders can spawn at a time?")]
            public int max_spawned = 1;

            [JsonProperty("Minimum time that a trader will stay for [seconds]?")]
            public float min_lifetime = 300f;

            [JsonProperty("Maximum time that a trader will stay for [seconds]?")]
            public float max_lifetime = 600f;

            [JsonProperty("Minmum time between attempts to spawn a new store [seconds]")]
            public float spawn_min_delay = 1800f;

            [JsonProperty("Maximum time between attempts to spawn a new store [seconds]")]
            public float spawn_max_delay = 3600f;

            [JsonProperty("Distance from the side of the road to spawn the trader")]
            public float trader_distance = 15f;

            [JsonProperty("Height adjustment of spawned prefabs")]
            public float prefab_height_adjustment = 0.5f;
        }

        public class ShopInfo
        {
            [JsonProperty("Unique CopyPaste file name for this list.")]
            public string file_name;

            [JsonProperty("The name of the shop.")]
            public string shop_name;

            [JsonProperty("Limit of purchases per player [0 = no limit]")]
            public int shop_purchase_limit = 0;

            [JsonProperty("How many items from the list should we randomly pick? [0 = all]")]
            public int shop_items_picked = 16;            

            [JsonProperty("List of items that you wish to list in this store.")]
            public List<ItemInfo> items = new List<ItemInfo>();
        }

        public class ItemInfo
        {
            public string name;
            public ulong skin;
            public int max_quantity;
            public int min_quantity;
            public bool use_random_quantity;
            public string shortname;
            public KeyValuePair<string, string> img_url;

            public int cost_min;
            public int cost_max;

            public string store_display_name;

            [JsonConstructor]
            public ItemInfo()
            {

            }

            public ItemInfo(string shortname, ulong skin, int min_quantity, int max_quantity, int cost_min, int cost_max, bool use_random_quantity = true, string name = null, KeyValuePair<string, string> img_url = new KeyValuePair<string, string>(), string store_display_name = null)
            {
                this.shortname = shortname;
                this.skin = skin;
                this.min_quantity = min_quantity;
                this.max_quantity = max_quantity;
                this.cost_min = cost_min;
                this.cost_max = cost_max;
                this.use_random_quantity = use_random_quantity;
                this.name = name;
                this.img_url = img_url;
                this.store_display_name = store_display_name;
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            Config_Item_Defs.AddRange(ItemManager.GetItemDefinitions());
            config.shop_info.Add("items", new ShopInfo()
            {
                file_name = "RoadsideVending",
                shop_name = "Random Item Trader",
                items = GetRandomItems()
            });
            config.shop_info.Add("weapons", new ShopInfo()
            {
                file_name = "WeaponVending",
                shop_name = "Random Weapon Trader",
                items = GetRandomItemsOfType(ItemCategory.Weapon)
            });
            config.shop_info.Add("clothing", new ShopInfo()
            {
                file_name = "ClothingVending",
                shop_name = "Random Clothing Trader",
                items = GetRandomItemsOfType(ItemCategory.Attire)
            });
            config.shop_info.Add("tools", new ShopInfo()
            {
                file_name = "ToolVending",
                shop_name = "Random Tool Trader",
                items = GetRandomItemsOfType(ItemCategory.Tool)
            });
            Config_Item_Defs.Clear();
        }

        List<ItemDefinition> Config_Item_Defs = new List<ItemDefinition>();

        List<ItemInfo> GetRandomItemsOfType(ItemCategory catagory)
        {
            var result = new List<ItemInfo>();
            if (Config_Item_Defs.Count == 0) Config_Item_Defs = ItemManager.GetItemDefinitions();
            var itemDefs = Config_Item_Defs.Where(x => x.category == catagory);

            foreach (var item in itemDefs)
            {
                if ((item.category == ItemCategory.Weapon))
                {
                    if (item.isHoldable && (item.occupySlots == ItemSlot.None || item.occupySlots == 0)) result.Add(new ItemInfo(item.shortname, 0, 1, 5, 100, 1000));
                }
                else if ((item.category == ItemCategory.Tool))
                {
                    if (item.isHoldable && (item.occupySlots == ItemSlot.None || item.occupySlots == 0)) result.Add(new ItemInfo(item.shortname, 0, 1, 3, 25, 300));
                }
                else if (item.category == ItemCategory.Attire)
                {
                    if (item.isWearable && ((item.Blueprint != null && item.Blueprint.userCraftable) || item.condition.repairable)) result.Add(new ItemInfo(item.shortname, 0, 1, 5, 50, 500));
                }
            }
            return result;
        }

        // Adds 16 random definitions to the default config.
        List<ItemInfo> GetRandomItems()
        {
            var result = new List<ItemInfo>();
            if (Config_Item_Defs.Count == 0) Config_Item_Defs = ItemManager.GetItemDefinitions();
            var itemDefs = Config_Item_Defs;
            List<ItemDefinition> randomDefs = Pool.GetList<ItemDefinition>();
            List<ItemDefinition> uniqueDefs = Pool.GetList<ItemDefinition>();
            for (int i = 0; i < 128; i++)
            {
                uniqueDefs.AddRange(itemDefs.Where(x => !randomDefs.Contains(x)));
                randomDefs.Add(uniqueDefs.GetRandom());
                uniqueDefs.Clear();
            }
            Pool.FreeList(ref uniqueDefs);
            foreach (var def in randomDefs)
            {
                result.Add(new ItemInfo(def.shortname, 0, def.category == ItemCategory.Resources ? 100 : 1, def.category == ItemCategory.Resources ? 5000 : 10, def.category == ItemCategory.Resources ? 1 : 10, def.category == ItemCategory.Resources ? 10 : 1000));
            }
            Pool.FreeList(ref randomDefs);
            return result;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintToConsole("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintToConsole($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data

        PlayerEntity pcdData;
        private DynamicConfigFile PCDDATA;

        public string CurrencyTypeString;
        const string perm_admin = "randomtrader.admin";
        const string perm_use = "randomtrader.use";

        void Init()
        {
            permission.RegisterPermission(perm_admin, this);
            permission.RegisterPermission(perm_use, this);
            PCDDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);
            LoadData();
            foreach (var def in ItemManager.GetItemDefinitions())
            {
                if (!ItemDefs.ContainsKey(def.shortname)) ItemDefs.Add(def.shortname, def);
            }
            Puts($"ItemDefs length: {ItemDefs.Count}");
            CurrencyTypeString = config.currency.Equals("scrap", StringComparison.OrdinalIgnoreCase) ? "{0} scrap" : config.currency.Equals("economics", StringComparison.OrdinalIgnoreCase) ? "${0}" : "{0} points";            
        }

        Dictionary<string, ItemDefinition> ItemDefs = new Dictionary<string, ItemDefinition>();

        void Unload()
        {
            SaveData();
            foreach (var kvp in trade_entities)
            {
                ClearShop(kvp.Key);
            }

            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }

        void SaveData()
        {
            PCDDATA.WriteObject(pcdData);
        }

        void LoadData()
        {
            try
            {
                pcdData = Interface.Oxide.DataFileSystem.ReadObject<PlayerEntity>(this.Name);
            }
            catch
            {
                Puts("Couldn't load player data, creating new Playerfile");
                pcdData = new PlayerEntity();
            }
        }

        class PlayerEntity
        {
            public Dictionary<ulong, PCDInfo> pEntity = new Dictionary<ulong, PCDInfo>();

        }

        class PCDInfo
        {

        }


        #endregion;

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["SpawnUINotValid"] = "{0} is not a valid profile.",
                ["SpawnUIUsage"] = "Usage: /spawntrader <profile name>\n Valid profiles: {0}",
                ["despawntrader_NoTraders"] = "There are no traders spawned right now.",
                ["despawntrader_Usage"] = "Usage (case sensitive): /despawntrader <profile name>\nSpawned traders: {0}",
                ["despawntrader_confirmed"] = "Despawned: {0}.",
                ["rtremovestoredata_Usage"] = "This command will remove the desired shop data from the config.\nUsage: /rtremovestoredata <store name>\nValid stores: {0}",
                ["rtremovestoredata_Removed"] = "Removed data for: {0}",
                ["rtfindnewshops_added"] = "Added {0} new to the config.",
                ["rtfindnewshops_Failed"] = "Could not find any new shops.",
                ["rtfindnewshops_Failed"] = "Could not find any new shops.",
                ["CreateBuilding_Failed"] = "Attempted to spawn the {0} trader, but it is already spawned.",
                ["CreateBuilding_Failed_At_Limit"] = "Could not spawn the trader as we are already at the trader limit [{0}]",
                ["BuildingCreated_Success"] = "A random trader has appeared at <color=#FF0000>{0}</color> and will be there for <color=#ffff00>{1} seconds</color>.",
                ["ClearShop_Left"] = "The trader at <color=#FF0000>{0}</color> has left.",
                ["MenuDelayWait"] = "Please wait a moment before attempting to buy something else.",
                ["PurchasedMax"] = "You have purchased the maximum amount of items from this vendor.",
                ["NoStock"] = "This vendor has no stock left for {0}.",
                ["CannotAfford"] = "You cannot afford this item.",
                ["EconErrorCash"] = "You do not have enough cash to purchase this item.",
                ["SRNoPoints"] = "You do not have enough points to purchase this item.",
                ["SRPointError"] = "Error taking points.",
                ["UIName"] = "Name",
                ["UIStock"] = "Stock",
                ["UIPrice"] = "Price",
                ["UIPurchaseLimitDisplayed"] = "<color=#8f0303>Purchase limit: {0}</color>",
                ["UIBuy"] = "BUY"
            }, this);
        }

        #endregion

        #region hooks

        void ResetSpawnTimer()
        {
            if (SpawnTimer != null && !SpawnTimer.Destroyed) SpawnTimer.Destroy();
            var randomTime = UnityEngine.Random.Range(config.trader_info.spawn_min_delay, config.trader_info.spawn_max_delay);
            SpawnTimer = timer.Once(randomTime, () =>
            {
                try
                {
                    SpawnRandomTrader();
                }
                catch { }
                ResetSpawnTimer();
            });
        }

        Timer SpawnTimer;

        private Dictionary<string, string> loadOrder = new Dictionary<string, string>();

        void OnServerInitialized(bool initial)
        {
            if (ImageLibrary == null)
            {
                Puts("ImageLibrary is required to run this plugin.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (CopyPaste == null)
            {
                Puts("CopyPaste is required to run this plugin.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (config.currency.Equals("economics", StringComparison.OrdinalIgnoreCase) && (Economics == null))
            {
                Puts("Reset currency to scrap as Economics is not loaded.");
                config.currency = "scrap";
                SaveConfig();
            }
            else if (config.currency.Equals("serverrewards", StringComparison.OrdinalIgnoreCase) && (ServerRewards == null))
            {
                Puts("Reset currency to scrap as ServerRewards is not loaded.");
                config.currency = "scrap";
                SaveConfig();
            }

            DeploymentPoints = GetDeploymentPoints();

            foreach (var entry in config.shop_info)
            {
                foreach (var shopItem in entry.Value.items)
                {
                    if (!string.IsNullOrEmpty(shopItem.img_url.Key)) loadOrder.Add(shopItem.img_url.Key, shopItem.img_url.Value);
                }
            }

            Puts($"Loading {loadOrder.Count} images into ImageLibrary");
            ImageLibrary.Call("ImportImageList", this.Name, loadOrder, 0ul, true, new Action(ImagesReady));

            ResetSpawnTimer();

            timer.Once(30f, () =>
            {
                Puts($"Checking for new item lists.");
                List<string> current_stores = Pool.GetList<string>();
                current_stores.AddRange(config.shop_info.Keys);

                Interface.CallHook("RandomTraderReady", current_stores);
                Pool.FreeList(ref current_stores);
            });
        }

        private void ImagesReady()
        {
            loadOrder.Clear();
            loadOrder = null;
            Puts($"Loaded all images for {this.Name}.");
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity != null && entity.OwnerID == 69)
            {
                if (info != null && info.damageTypes != null) info.damageTypes.ScaleAll(0.0f);
                return true;
            }
            return null;
        }

        object CanPickupEntity(BasePlayer player, VendingMachine vm)
        {
            if (vending_keys.ContainsKey(vm)) return false;
            return null;
        }

        #endregion

        #region Vending

        //Used to identify which shop to send.
        Dictionary<VendingMachine, string> vending_keys = new Dictionary<VendingMachine, string>();

        object OnVendingShopOpen(VendingMachine machine, BasePlayer player)
        {
            string shop;
            if (!vending_keys.TryGetValue(machine, out shop) || !trade_entities.ContainsKey(shop)) return null;
            SendShop(player, shop);
            return true;
        }

        #endregion

        #region chat commands

        [ChatCommand("spawntrader")]
        void SpawnTrader(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, true)) return;
            if (args == null || args.Length == 0 || !config.shop_info.ContainsKey(args[0]))
            {
                PrintToChat(player, (args.Length > 0 ? string.Format(lang.GetMessage("SpawnUINotValid", this, player.UserIDString), args[0]) : null) + string.Format(lang.GetMessage("SpawnUIUsage", this, player.UserIDString), string.Join(", ", config.shop_info.Keys)));
                return;
            }
            CreateBuilding(args[0], player);
        }

        [ChatCommand("despawntrader")]
        void DespawnTrader(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, true)) return;
            if (args == null || args.Length == 0 || !trade_entities.ContainsKey(args[0]))
            {
                if (trade_entities.Count == 0) PrintToChat(player, lang.GetMessage("despawntrader_NoTraders", this, player.UserIDString));
                PrintToChat(player, string.Format(lang.GetMessage("despawntrader_Usage", this, player.UserIDString), string.Join(", ", trade_entities.Keys)));
                return;
            }
            ClearShop(args[0], true);
            PrintToChat(player, string.Format(lang.GetMessage("despawntrader_confirmed", this, player.UserIDString), args[0]));
        }

        // Clears the shop data.
        [ChatCommand("rtremovestoredata")]
        void RemoveStoreData(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, true)) return;
            string shop = args.Length > 0 ? config.shop_info.Keys.FirstOrDefault(x => x.Equals(args[0], StringComparison.OrdinalIgnoreCase)) : null;
            if (string.IsNullOrEmpty(shop))
            {
                PrintToChat(player, string.Format(lang.GetMessage("rtremovestoredata_Usage", this, player.UserIDString), string.Join(", ", config.shop_info.Keys)));
                return;
            }
            config.shop_info.Remove(shop);
            SaveConfig();
            PrintToChat(player, string.Format(lang.GetMessage("rtremovestoredata_Removed", this, player.UserIDString), shop));
        }

        [ChatCommand("rtfindnewshops")]
        void FindNewShops(BasePlayer player)
        {
            var old_shop_count = config.shop_info.Count;
            if (!HasPerm(player, true)) return;
            List<string> current_stores = Pool.GetList<string>();
            current_stores.AddRange(config.shop_info.Keys);
            Interface.CallHook("RandomTraderReady", current_stores);
            Pool.FreeList(ref current_stores);
            int newShops = 0;
            if ((newShops = config.shop_info.Count - old_shop_count) > 0) PrintToChat(player, string.Format(lang.GetMessage("rtfindnewshops_added", this, player.UserIDString), newShops));
            else PrintToChat(player, lang.GetMessage("rtfindnewshops_Failed", this, player.UserIDString));
        }

        #endregion

        #region Spawning traders

        void SpawnRandomTrader()
        {
            List<string> random_traders = Pool.GetList<string>();
            random_traders.AddRange(config.shop_info.Keys.Where(x => !trade_entities.ContainsKey(x)));
            if (random_traders.Count > 0) CreateBuilding(random_traders.GetRandom(), null);
            Pool.FreeList(ref random_traders);
        }

        void CreateBuilding(string name, BasePlayer player = null)
        {
            Puts($"Attempting to spawn {name} trader.");
            if (trade_entities.ContainsKey(name))
            {
                if (player != null) PrintToChat(player, string.Format(lang.GetMessage("CreateBuilding_Failed", this, player.UserIDString), name));
                else Puts(string.Format(lang.GetMessage("CreateBuilding_Failed", this, player.UserIDString), name));
                return;
            }
            if (trade_entities.Count >= config.trader_info.max_spawned)
            {
                if (player != null) PrintToChat(player, string.Format(lang.GetMessage("CreateBuilding_Failed_At_Limit", this, player.UserIDString), config.trader_info.max_spawned));
                else Puts(string.Format(lang.GetMessage("CreateBuilding_Failed_At_Limit", this, player.UserIDString), config.trader_info.max_spawned));
                return;
            }
            ShopInfo si;
            if (!config.shop_info.TryGetValue(name, out si)) return;
            var pos = DeploymentPoints.GetRandom();
            Puts($"Spawning {name} trader at {pos}.");
            CopyPaste.Call("TryPasteFromVector3", pos, 0f, si.file_name, new string[] { "auth", "false", "stability", "false" });
        }

        List<Vector3> GetDeploymentPoints()
        {
            var result = new List<Vector3>();

            List<PathList> paths = Pool.GetList<PathList>();
            paths.AddRange(TerrainMeta.Path.Roads);
            foreach (var path in paths)
            {
                if (path.Path?.Points.Length > 0)
                {
                    for (int i = 0; i < path.Path.Points.Length; i++)
                    {
                        if (i > 0)
                        {
                            float spacing = path.Width + config.trader_info.trader_distance;
                            var pos_right = AdjustForHeight(path.Path.Points[i] + Vector3.right * spacing);
                            if (IsValidTopology(pos_right)) result.Add(pos_right);

                            var pos_left = AdjustForHeight(path.Path.Points[i] + Vector3.left * spacing);
                            if (IsValidTopology(pos_left)) result.Add(pos_left);
                        }
                    }
                }
            }
            Pool.FreeList(ref paths);
            Puts($"Found {result.Count} valid points to spawn traders.");
            return result;
        }

        [ChatCommand("rtcheckpoints")]
        void CheckPointS(BasePlayer player)
        {
            if (!HasPerm(player, true)) return;
            var wasAdmin = player.IsAdmin;
            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }
            foreach (var point in DeploymentPoints)
            {
                player.SendConsoleCommand("ddraw.text", 20f, Color.red, point, "X");
            }
            if (!wasAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                player.SendNetworkUpdateImmediate();
            }
        }
        
        //Credit to Nivex for GetSpawnHeight snippet.
        private const int targetMask = Layers.Mask.World | Layers.Mask.Terrain | Layers.Mask.Default;
        public static float GetSpawnHeight(Vector3 target)
        {
            float y = TerrainMeta.HeightMap.GetHeight(target);
            float w = TerrainMeta.WaterMap.GetHeight(target);
            float p = TerrainMeta.HighestPoint.y + 250f;
            RaycastHit hit;

            if (Physics.Raycast(target.WithY(p), Vector3.down, out hit, ++p, targetMask, QueryTriggerInteraction.Ignore))
            {
                y = Mathf.Max(y, hit.point.y);
            }

            return Mathf.Max(y, w);
        }

        List<Vector3> DeploymentPoints;

        bool IsValidTopology(Vector3 pos)
        {
            if (TerrainMeta.TopologyMap.GetTopology(pos, TerrainTopology.ROAD)) return false;
            if (TerrainMeta.TopologyMap.GetTopology(pos, TerrainTopology.CLIFFSIDE)) return false;
            if (TerrainMeta.TopologyMap.GetTopology(pos, 524288)) return false;
            if (TerrainMeta.TopologyMap.GetTopology(pos, TerrainTopology.MONUMENT)) return false;
            return true;
        }

        Vector3 AdjustForHeight(Vector3 pos)
        {
            pos.y = GetSpawnHeight(pos) + config.trader_info.prefab_height_adjustment;
            return new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos), pos.z);
        }

        #endregion

        #region Instance handling

        [PluginReference]
        private Plugin CopyPaste, Economics, ServerRewards, ImageLibrary;

        //Name - TraderEntities
        Dictionary<string, TraderEntities> trade_entities = new Dictionary<string, TraderEntities>(StringComparer.InvariantCultureIgnoreCase);

        public class TraderEntities
        {
            public List<BaseEntity> pasted_entities = new List<BaseEntity>();
            public VendingMachineMapMarker vending_machine_marker;
            public MapMarkerGenericRadius vending_map_markers;
            public List<VendingMachine> machines = new List<VendingMachine>();
            public List<TradeInstance> shop = new List<TradeInstance>();
            public Dictionary<BasePlayer, int> purchases = new Dictionary<BasePlayer, int>();
            public Vector3 pos;
        }

        public class TradeInstance
        {
            public string shortname;
            public ulong skin;
            public int stock;
            public string name;
            public KeyValuePair<string, string> img_url;
            public int cost;
            public string store_display_name;

            public TradeInstance(string shortname, ulong skin, int stock, int cost, string name = null, KeyValuePair<string, string> img_url = new KeyValuePair<string, string>(), string store_display_name = null)
            {
                this.shortname = shortname;
                this.skin = skin;
                this.stock = stock;
                this.cost = cost;
                this.name = name;
                this.img_url = img_url;
                this.store_display_name = store_display_name;
            }
        }

        void RemovePastedEntities(string key)
        {
            TraderEntities te;
            if (!trade_entities.TryGetValue(key, out te)) return;
            Puts("Removing old paste");
            foreach (var entity in te.pasted_entities)
            {
                entity.KillMessage();
            }
            te.pasted_entities.Clear();
        }

        void RemovePastedEntities(TraderEntities te)
        {
            if (te == null) return;
            Puts("Removing old paste");
            foreach (var entity in te.pasted_entities)
            {
                entity.KillMessage();
            }
            te.pasted_entities.Clear();
        }

        void RemoveMapMarkers(VendingMachineMapMarker vending_machine_marker, MapMarkerGenericRadius vending_map_markers)
        {
            if (vending_machine_marker != null) vending_machine_marker.KillMessage();
            if (vending_map_markers != null) vending_map_markers.KillMessage();
        }

        VendingMachineMapMarker CreateVendingMachineMapMarker(Vector3 pos, ShopInfo shopInfo)
        {
            var Vmarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", pos) as VendingMachineMapMarker;
            Vmarker.markerShopName = shopInfo.shop_name;
            Vmarker.enabled = false;
            Vmarker.Spawn();
            return Vmarker;
        }

        MapMarkerGenericRadius CreateGenericMapMarker(Vector3 pos, ShopInfo shopInfo)
        {
            var marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", pos) as MapMarkerGenericRadius;
            marker.color2 = Color.magenta;
            marker.color1 = Color.blue;
            marker.alpha = 1f;
            marker.radius = 0.25f;
            marker.enabled = true;
            marker.Spawn();
            marker.SendUpdate();
            return marker;
        }

        void DestroyUI(BasePlayer player)
        {
            // Add UI here
            CuiHelper.DestroyUi(player, "RandomTraderShop");
        }

        void OnPasteFinished(List<BaseEntity> pastedEntities, string filename)
        {
            var kvp = config.shop_info.FirstOrDefault(x => x.Value.file_name == filename);
            if (string.IsNullOrEmpty(kvp.Key)) return;
            TraderEntities te;
            if (trade_entities.ContainsKey(kvp.Key))
            {
                ClearShop(kvp.Key, true);
            }
            trade_entities.Add(kvp.Key, te = new TraderEntities());

            // Setting owner id to 69 means we can identify it when it takes damage and prevent it.
            foreach (var entity in pastedEntities)
            {
                entity.OwnerID = 69;
                entity.SendNetworkUpdateImmediate();
            }
            te.pasted_entities.AddRange(pastedEntities);
            var pos = te.pasted_entities.First().transform.position;
            te.pos = pos;
            te.vending_machine_marker = CreateVendingMachineMapMarker(pos, kvp.Value);
            te.vending_map_markers = CreateGenericMapMarker(pos, kvp.Value);
            te.machines.AddRange(pastedEntities.OfType<VendingMachine>());
            foreach (var vendingMachine in te.machines)
            {
                vending_keys.Add(vendingMachine, kvp.Key);
            }

            // Setup shop
            te.shop.Clear();
            List<ItemInfo> random_items = Pool.GetList<ItemInfo>();
            List<ItemInfo> temp_list = Pool.GetList<ItemInfo>();
            for (int i = 0; i < kvp.Value.shop_items_picked; i++)
            {
                temp_list.AddRange(kvp.Value.items.Where(x => !random_items.Contains(x)));
                random_items.Add(temp_list.GetRandom());
                temp_list.Clear();
            }
            Pool.FreeList(ref temp_list);

            foreach (var item in random_items)
            {
                te.shop.Add(new TradeInstance(item.shortname, item.skin, item.use_random_quantity ? UnityEngine.Random.Range(item.min_quantity, item.max_quantity + 1) : item.max_quantity, UnityEngine.Random.Range(item.cost_min, item.cost_max + 1), item.name, item.img_url, item.store_display_name));
            }
            Pool.FreeList(ref random_items);

            var randomTime = UnityEngine.Random.Range(config.trader_info.min_lifetime, config.trader_info.max_lifetime);
            PrintToChat(string.Format(lang.GetMessage("BuildingCreated_Success", this), GetGrid(pos, false), Math.Round(randomTime, 0)));
            ClearActiveShopTimer(kvp.Key);
            Shop_Spawn_Timer.Add(kvp.Key, timer.Once(randomTime, () =>
            {
                ClearShop(kvp.Key, true);
            }));



            //var randomTime = UnityEngine.Random.Range(config.trader_info.min_lifetime, config.trader_info.max_lifetime);
            //string name = kvp.Key;
            //te._timer = timer.Once(randomTime, () =>
            //{
            //    ClearShop(name, true);
            //});

            //PrintToChat($"A random trader has appeared at <color=#FF0000>{GetGrid(pos, false)}</color> and will be there for <color=#ffff00>{Math.Round(randomTime, 0)} seconds</color>.");
        }

        void ClearActiveShopTimer(string shop)
        {
            if (!Shop_Spawn_Timer.ContainsKey(shop)) return;
            if (Shop_Spawn_Timer[shop] != null && !Shop_Spawn_Timer[shop].Destroyed) Shop_Spawn_Timer[shop].Destroy();
            Shop_Spawn_Timer.Remove(shop);
        }

        Dictionary<string, Timer> Shop_Spawn_Timer = new Dictionary<string, Timer>();

        void ClearShop(string name, bool remove_entry = false)
        {
            ClearActiveShopTimer(name);
            TraderEntities te;
            if (!trade_entities.TryGetValue(name, out te)) return;
            if (te.machines.Count > 0) PrintToChat(string.Format(lang.GetMessage("ClearShop_Left", this), GetGrid(te.machines.First().transform.position, false)));
            foreach (var machine in te.machines)
            {
                vending_keys.Remove(machine);
            }
            RemoveMapMarkers(te.vending_machine_marker, te.vending_map_markers);
            RemovePastedEntities(te);

            // Set a timer to respawn

            if (remove_entry) trade_entities.Remove(name);
        }

        #endregion

        #region UI

        bool HasPerm(BasePlayer player, bool require_admin = false)
        {
            if (!require_admin) return permission.UserHasPermission(player.UserIDString, perm_admin) || permission.UserHasPermission(player.UserIDString, perm_use);
            else return permission.UserHasPermission(player.UserIDString, perm_admin);
        }

        private void SendShop(BasePlayer player, string name, int firstIndex = 0)
        {
            if (!HasPerm(player) || !config.shop_info.ContainsKey(name)) return;
            TraderEntities te;
            if (!trade_entities.TryGetValue(name, out te))
            {
                if (firstIndex > 0) CuiHelper.DestroyUi(player, "RandomTraderShop");
                return;
            }

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.99" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.351 0.334", OffsetMax = "0.349 -0.326" }
            }, "Overlay", "RandomTraderShop");
            container.Add(new CuiElement
            {
                Name = "title",
                Parent = "RandomTraderShop",
                Components = {
                    new CuiTextComponent { Text = config.shop_info[name].shop_name?.ToUpper() ?? "RANDOM SHOP", Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-520.699 218.705", OffsetMax = "520.701 263.695" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-208.374 178.5", OffsetMax = "208.376 203.18" }
            }, "RandomTraderShop", "TitlesPanel");
            container.Add(new CuiElement
            {
                Name = "Name",
                Parent = "TitlesPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIName", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-138.916 -12.34", OffsetMax = "-32.126 12.34" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Stock",
                Parent = "TitlesPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIStock", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32.126 -12.34", OffsetMax = "17.251 12.34" }
                }
            });
            container.Add(new CuiElement
            {
                Name = "Price",
                Parent = "TitlesPanel",
                Components = {
                    new CuiTextComponent { Text = lang.GetMessage("UIPrice", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "52.938 -12.34", OffsetMax = "115.21 12.34" }
                }
            });
            var listing = 0;
            var itemIndex = firstIndex;
            foreach (var item in te.shop.Skip(firstIndex))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-208.375 {130.5 - (listing * 48)}", OffsetMax = $"208.375 {178.5 - (listing * 48)}" }
                }, "RandomTraderShop", $"Listing_{listing}");
                if (string.IsNullOrEmpty(item.img_url.Key))
                {                    
                    container.Add(new CuiElement
                    {
                        Name = "img",
                        Parent = $"Listing_{listing}",
                        Components = {
                        new CuiImageComponent { Color = "1 1 1 1", ItemId = ItemDefs[item.shortname].itemid, SkinId = item.skin },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-208.375 -24", OffsetMax = "-160.375 24" }
                    }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "img",
                        Parent = $"Listing_{listing}",
                        Components = {
                        new CuiRawImageComponent { Color = "1 1 1 1", Png = (string)ImageLibrary?.Call("GetImage", item.img_url.Key) },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-208.375 -24", OffsetMax = "-160.375 24" }
                    }
                    });
                }
                var display_name = item.store_display_name != null ? item.store_display_name : item.name != null ? item.name : !string.IsNullOrEmpty(ItemDefs[item.shortname].displayName.english) ? ItemDefs[item.shortname].displayName.english : item.shortname;
                container.Add(new CuiElement
                {
                    Name = "name",
                    Parent = $"Listing_{listing}",
                    Components = {
                    new CuiTextComponent { Text = display_name.TitleCase(), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-138.815 -24", OffsetMax = "-32.125 24" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "stock",
                    Parent = $"Listing_{listing}",
                    Components = {
                    new CuiTextComponent { Text = item.stock.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-32.128 -24", OffsetMax = "17.251 24" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "price",
                    Parent = $"Listing_{listing}",
                    Components = {
                    new CuiTextComponent { Text = string.Format(CurrencyTypeString, item.cost), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "52.939 -24", OffsetMax = "115.211 24" }
                }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "0.3301887 0.3301887 0.3301887 1", Command = $"randomtraderbuyitem {name} {itemIndex} {firstIndex}" },
                    Text = { Text = lang.GetMessage("UIBuy", this, player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "166.375 -12", OffsetMax = "208.375 12" }
                }, $"Listing_{listing}", "Buy_button");

                listing++;
                itemIndex++;

                if (listing >= 8) break;
            }

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "randomtraderclosemenu" },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "179.5 231.2", OffsetMax = "199.5 251.2" }
            }, "RandomTraderShop", "close_button");

            if (te.shop.Count > firstIndex + 8)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"randomtradersendpage {name} {firstIndex + 8}" },
                    Text = { Text = ">>", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "218.372 -31.591", OffsetMax = "265.488 31.591" }
                }, "RandomTraderShop", "next_button");
            }

            if (firstIndex > 0)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"randomtradersendpage {name} {firstIndex - 8}" },
                    Text = { Text = "<<", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-265.488 -31.591", OffsetMax = "-218.372 31.591" }
                }, "RandomTraderShop", "back_button");
            }

            if (config.shop_info[name].shop_purchase_limit > 0)
            {
                container.Add(new CuiElement
                {
                    Name = "PurchaseLimitWarning",
                    Parent = "RandomTraderShop",
                    Components = {
                    new CuiTextComponent { Text = string.Format(lang.GetMessage("UIPurchaseLimitDisplayed", this, player.UserIDString), config.shop_info[name].shop_purchase_limit), Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-520.683 203.178", OffsetMax = "520.717 218.702" }
                }
                });
            }

            CuiHelper.DestroyUi(player, "RandomTraderShop");
            CuiHelper.AddUi(player, container);
        }

        Dictionary<ulong, float> access_timer = new Dictionary<ulong, float>();

        [ConsoleCommand("randomtraderbuyitem")]
        void BuyItem(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (!access_timer.ContainsKey(player.userID)) access_timer.Add(player.userID, Time.time + 1f);
            else if (access_timer[player.userID] <= Time.time) access_timer[player.userID] = Time.time + 1f;
            else
            {
                PrintToChat(player, lang.GetMessage("MenuDelayWait", this, player.UserIDString));
                return;
            }

            // Args: {name} {index}
            var name = arg.Args[0];
            var index = Convert.ToInt32(arg.Args[1]);
            var pageIndex = Convert.ToInt32(arg.Args[2]);

            if (!trade_entities.ContainsKey(name) || Vector3.Distance(trade_entities[name].pos, player.transform.position) > 15f || !config.shop_info.ContainsKey(name))
            {
                Puts($"trade_entities contains: {trade_entities.ContainsKey(name)}\nDist: {Vector3.Distance(trade_entities[name].pos, player.transform.position)}\nconfig contais: {config.shop_info.ContainsKey(name)}");
                CuiHelper.DestroyUi(player, "RandomTraderShop");
                return;
            }

            if (config.shop_info[name].shop_purchase_limit > 0 && trade_entities[name].purchases.ContainsKey(player))
            {
                if (trade_entities[name].purchases[player] >= config.shop_info[name].shop_purchase_limit)
                {
                    PrintToChat(player, lang.GetMessage("PurchasedMax", this, player.UserIDString));
                    return;
                }
            }

            var itemInfo = trade_entities[name].shop.ElementAt(index);
            if (itemInfo == null) return;
            if (itemInfo.stock <= 0)
            {
                PrintToChat(player, string.Format(lang.GetMessage("NoStock", this, player.UserIDString), itemInfo.store_display_name ?? itemInfo.name ?? itemInfo.shortname));
                return;
            }
            //PrintToChat(player, $"Bought: {itemInfo.name ?? ItemDefs[itemInfo.shortname].displayName.english}[{itemInfo.shortname}] {itemInfo.cost} {itemInfo.stock}");

            if (config.currency.Equals("economics", StringComparison.OrdinalIgnoreCase) && TakeEcon(player, itemInfo.cost)) GiveItem(player, itemInfo, trade_entities[name]);
            else if (config.currency.Equals("serverrewards", StringComparison.OrdinalIgnoreCase) && TakeSRP(player, itemInfo.cost)) GiveItem(player, itemInfo, trade_entities[name]);
            else if (TakeScrap(player, itemInfo)) GiveItem(player, itemInfo, trade_entities[name]);

            SendShop(player, name, pageIndex);
        }

        [ConsoleCommand("randomtraderclosemenu")]
        void CloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "RandomTraderShop");
        }

        [ConsoleCommand("randomtradersendpage")]
        void SendPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            var name = arg.Args[0];
            var newIndex = Convert.ToInt32(arg.Args[1]);

            SendShop(player, name, newIndex);
        }

        bool TakeScrap(BasePlayer player, TradeInstance itemInfo)
        {
            List<Item> scrapStacks = Pool.GetList<Item>();
            if (CountScrap(player, scrapStacks, itemInfo.cost) < itemInfo.cost)
            {
                PrintToChat(player, lang.GetMessage("CannotAfford", this, player.UserIDString));
                Pool.FreeList(ref scrapStacks);
                return false;
            }
            var used = 0;
            foreach (var scrap in scrapStacks)
            {
                if (scrap.amount >= itemInfo.cost - used)
                {
                    scrap.UseItem(itemInfo.cost - used);
                    break;
                }
                else
                {
                    used += scrap.amount;
                    scrap.Remove();
                }
            }
            Pool.FreeList(ref scrapStacks);
            return true;
        }

        int CountScrap(BasePlayer player, List<Item> scrap, int requiredScrap)
        {
            if (scrap == null) return 0;
            var result = 0;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.shortname == "scrap")
                {
                    result += item.amount;
                    scrap.Add(item);
                }
                if (result >= requiredScrap) return result;
            }
            return result;
        }

        void GiveItem(BasePlayer player, TradeInstance itemInfo, TraderEntities te)
        {
            if (itemInfo == null || te == null) return;
            var item = ItemManager.CreateByName(itemInfo.shortname, 1, itemInfo.skin);
            if (itemInfo.name != null) item.name = itemInfo.name;

            player.GiveItem(item);

            if (!te.purchases.ContainsKey(player)) te.purchases.Add(player, 1);
            else te.purchases[player]++;

            itemInfo.stock--;
        }

        bool TakeEcon(BasePlayer player, int cost)
        {
            var playerBalance = Convert.ToDouble(Economics?.Call("Balance", player.userID));
            if (playerBalance < cost)
            {
                PrintToChat(player, lang.GetMessage("CannotAfford", this, player.UserIDString));
                return false;
            }
            if (!Convert.ToBoolean(Economics?.Call("Withdraw", player.userID, Convert.ToDouble(cost))))
            {
                PrintToChat(player, lang.GetMessage("EconErrorCash", this, player.UserIDString));
                return false;
            }

            return true;
        }

        bool TakeSRP(BasePlayer player, int cost)
        {
            var balance = Convert.ToInt32(ServerRewards.Call("CheckPoints", player.userID));
            if (balance < cost)
            {
                PrintToChat(player, lang.GetMessage("SRNoPoints", this, player.UserIDString));
                return false;
            }
            if (!Convert.ToBoolean(ServerRewards?.Call("TakePoints", player.userID, cost)))
            {
                PrintToChat(player, lang.GetMessage("SRPointError", this, player.UserIDString));
                return false;
            }

            return true;
        }

        #endregion

        #region Grid helpers

        private string GetGrid(Vector3 position, bool addVector) // Credit: Jake_Rich
        {
            var roundedPos = new Vector2(World.Size / 2 + position.x, World.Size / 2 - position.z);
            var grid = $"{NumberToLetter((int)(roundedPos.x / 146.3))}{(int)(roundedPos.y / 146.3)}";

            if (addVector)
            {
                grid += $" {position.ToString().Replace(",", "")}";
            }

            return grid;
        }

        private string NumberToLetter(int num) // Credit: Jake_Rich
        {
            var num2 = Mathf.FloorToInt((float)(num / 26));
            var num3 = num % 26;
            var text = string.Empty;
            if (num2 > 0)
            {
                for (var i = 0; i < num2; i++)
                {
                    text += Convert.ToChar(65 + i);
                }
            }

            return text + Convert.ToChar(65 + num3);
        }

        #endregion

        #region API

        /* API Requirements:
         * - Unique name/key to save it to the config with, so we don't duplicate it.
         * - Item info (shortname, amount, skin, name, cost min, cost max)
         * - File name
         * - shop name
         * - shop purchase limit
         * - how many items to pick in the shop
         * 
         * 
         */

        /*
         * *Add the loot purchasing
*Add econ/srp support
*Add a proper vanilla loot table
*Add option to limit purchase of items to 1?
*Add support to prevent purchasing of an item if the vending *machine no longer exists (check player distance if it does exist, incase it moved).

Lang

*Timer between purchases
Add API to be able to add purchase lists from other plugins

         * 
         * 
         */

        public class API_Info
        {
            public string fileName;
            public string shopName;
            public int purchase_limit;
            public int display_amount;
            public List<RTItemInfo> items = new List<RTItemInfo>();

            public class RTItemInfo
            {
                public string shortname;
                public ulong skin;
                public int min_quantity;
                public int max_quantity;
                public int min_cost;
                public int max_cost;
                public string name;
                public string store_display_name;
                public KeyValuePair<string, string> url;
                public RTItemInfo(string shortname, ulong skin, int min_quantity, int max_quantity, int min_cost, int max_cost, string name, string store_display_name, KeyValuePair<string, string> url)
                {
                    this.shortname = shortname;
                    this.skin = skin;
                    this.min_quantity = min_quantity;
                    this.max_quantity = max_quantity;
                    this.min_cost = min_cost;
                    this.max_cost = max_cost;
                    this.name = name;
                    this.store_display_name = store_display_name;
                    this.url = url;
                }
            }

            public API_Info(string fileName, string shopName, int purchase_limit, int display_amount)
            {
                this.fileName = fileName;
                this.shopName = shopName;
                this.purchase_limit = purchase_limit;
                this.display_amount = display_amount;
            }
        }

        // API ORDER
        // string fileName, string shopName, int purchaseLimit, int shopQuantityDisplayed

        // API ORDER FOR ITEMS
        // object items = shortname, skin, min_quantity, max_quantity, min_cost, max_cost, name

        [HookMethod("RTAddStore")]
        public void RTAddStore(string key, object[] hookObject, List<object[]> items)
        {
            if (config.shop_info.ContainsKey(key))
            {
                Puts($"Attempted to add new shop for {key}, but there is already an entry. Manually delete the entry before attempting to create another copy.");
                return;
            }

            if (hookObject.Length < 4) return;

            var data = new API_Info((string)hookObject[0], (string)hookObject[1], (int)hookObject[2], (int)hookObject[3]);
            foreach (var item in items)
            {
                if (item.Length < 8) continue;
                try
                {
                    data.items.Add(new API_Info.RTItemInfo((string)item[0], (ulong)item[1], (int)item[2], (int)item[3], (int)item[4], (int)item[5], (string)item[6], (string)item[7], (KeyValuePair<string, string>)item[8]));
                }
                catch
                {
                    Puts($"Failed to add an item entry for {key}.");
                }
            }
            ShopInfo si;
            config.shop_info.Add(key, si = new ShopInfo());
            si.file_name = data.fileName;
            si.shop_name = data.shopName;
            si.shop_purchase_limit = data.purchase_limit;
            si.shop_items_picked = data.display_amount;
            foreach (var item in data.items)
            {
                if (item == null) continue;
                var itemData = ConvertAPIItems(item);
                if (itemData != null) si.items.Add(itemData);
            }
            Puts($"Added new shop: {key} with {si.items.Count}x items.");
            SaveConfig();
        }

        ItemInfo ConvertAPIItems(API_Info.RTItemInfo data)
        {
            return new ItemInfo(data.shortname, data.skin, data.min_quantity, data.max_quantity, data.min_cost, data.max_cost, true, data.name, data.url, data.store_display_name?.TitleCase());
        }

        #endregion
    }
}
