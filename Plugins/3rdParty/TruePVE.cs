﻿using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Plugins.TruePVEExtensionMethods;
using Rust;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TruePVE", "nivex", "2.0.8")]
    [Description("Improvement of the default Rust PVE behavior")]
    // Thanks to the original author, ignignokt84.
    class TruePVE : RustPlugin
    {
        #region Variables
        private static TruePVE Instance;
        private const uint WALL_LOW_JPIPE = 310235277;
        // config/data container
        private Configuration config = new Configuration();

        [PluginReference]
        Plugin ZoneManager, LiteZones, Clans, Friends, AbandonedBases, RaidableBases;

        // usage information string with formatting
        public string usageString;
        // valid commands
        private enum Command { def, sched, trace, usage, enable, sleepers };

        // default values array

        // flags for RuleSets

        private class RuleFlags : FlagsAttribute
        {
            public const long None = 0;
            public const long AdminsHurtSleepers = 1L << 1;
            public const long AnimalsIgnoreSleepers = 1L << 2;
            public const long AuthorizedDamage = 1L << 3;
            public const long AuthorizedDamageRequiresOwnership = 1L << 4;
            public const long CupboardOwnership = 1L << 5;
            public const long FriendlyFire = 1L << 6;
            public const long HeliDamageLocked = 1L << 7;
            public const long HumanNPCDamage = 1L << 8;
            public const long LockedBoxesImmortal = 1L << 9;
            public const long LockedDoorsImmortal = 1L << 10;
            public const long NoPlayerDamageToCar = 1L << 11;
            public const long NoPlayerDamageToMini = 1L << 12;
            public const long NoPlayerDamageToScrap = 1L << 13;
            public const long NoHeliDamage = 1L << 14;
            public const long NoHeliDamagePlayer = 1L << 15;
            public const long NoHeliDamageQuarry = 1L << 16;
            public const long NoHeliDamageRidableHorses = 1L << 17;
            public const long NoHeliDamageSleepers = 1L << 18;
            public const long NpcsCanHurtAnything = 1L << 19;
            public const long PlayerSamSitesIgnorePlayers = 1L << 20;
            public const long ProtectedSleepers = 1L << 21;
            public const long TrapsIgnorePlayers = 1L << 22;
            public const long TrapsIgnoreScientist = 1L << 23;
            public const long TurretsIgnorePlayers = 1L << 24;
            public const long TurretsIgnoreScientist = 1L << 25;
            public const long TwigDamage = 1L << 26;
            public const long TwigDamageRequiresOwnership = 1L << 27;
            public const long VehiclesTakeCollisionDamageWithoutDriver = 1L << 28;
            public const long SamSitesIgnoreMLRS = 1L << 29;
            public const long SelfDamage = 1L << 30;
            public const long StaticSamSitesIgnorePlayers = 1L << 31;
            public const long StaticTurretsIgnorePlayers = 1L << 32;
            public const long SuicideBlocked = 1L << 33;

            public static long Get(string value)
            {
                switch (value)
                {
                    case "None": default: return RuleFlags.None;
                    case "AdminsHurtSleepers": return RuleFlags.AdminsHurtSleepers;
                    case "AnimalsIgnoreSleepers": return RuleFlags.AnimalsIgnoreSleepers;
                    case "AuthorizedDamage": return RuleFlags.AuthorizedDamage;
                    case "AuthorizedDamageRequiresOwnership": return RuleFlags.AuthorizedDamageRequiresOwnership;
                    case "CupboardOwnership": return RuleFlags.CupboardOwnership;
                    case "FriendlyFire": return RuleFlags.FriendlyFire;
                    case "HeliDamageLocked": return RuleFlags.HeliDamageLocked;
                    case "HumanNPCDamage": return RuleFlags.HumanNPCDamage;
                    case "LockedBoxesImmortal": return RuleFlags.LockedBoxesImmortal;
                    case "LockedDoorsImmortal": return RuleFlags.LockedDoorsImmortal;
                    case "NoPlayerDamageToCar": return RuleFlags.NoPlayerDamageToCar;
                    case "NoPlayerDamageToMini": return RuleFlags.NoPlayerDamageToMini;
                    case "NoPlayerDamageToScrap": return RuleFlags.NoPlayerDamageToScrap;
                    case "NoHeliDamage": return RuleFlags.NoHeliDamage;
                    case "NoHeliDamagePlayer": return RuleFlags.NoHeliDamagePlayer;
                    case "NoHeliDamageQuarry": return RuleFlags.NoHeliDamageQuarry;
                    case "NoHeliDamageRidableHorses": return RuleFlags.NoHeliDamageRidableHorses;
                    case "NoHeliDamageSleepers": return RuleFlags.NoHeliDamageSleepers;
                    case "NpcsCanHurtAnything": return RuleFlags.NpcsCanHurtAnything;
                    case "PlayerSamSitesIgnorePlayers": return RuleFlags.PlayerSamSitesIgnorePlayers;
                    case "ProtectedSleepers": return RuleFlags.ProtectedSleepers;
                    case "TrapsIgnorePlayers": return RuleFlags.TrapsIgnorePlayers;
                    case "TrapsIgnoreScientist": return RuleFlags.TrapsIgnoreScientist;
                    case "TurretsIgnorePlayers": return RuleFlags.TurretsIgnorePlayers;
                    case "TurretsIgnoreScientist": return RuleFlags.TurretsIgnoreScientist;
                    case "TwigDamage": return RuleFlags.TwigDamage;
                    case "TwigDamageRequiresOwnership": return RuleFlags.TwigDamageRequiresOwnership;
                    case "VehiclesTakeCollisionDamageWithoutDriver": return RuleFlags.VehiclesTakeCollisionDamageWithoutDriver;
                    case "SamSitesIgnoreMLRS": return RuleFlags.SamSitesIgnoreMLRS;
                    case "SelfDamage": return RuleFlags.SelfDamage;
                    case "StaticSamSitesIgnorePlayers": return RuleFlags.StaticSamSitesIgnorePlayers;
                    case "StaticTurretsIgnorePlayers": return RuleFlags.StaticTurretsIgnorePlayers;
                    case "SuicideBlocked": return RuleFlags.SuicideBlocked;
                }
            }
        }

        private Timer scheduleUpdateTimer;                              // timer to check for schedule updates        
        private RuleSet currentRuleSet;                                 // current ruleset        
        private string currentBroadcastMessage;                         // current broadcast message        
        private bool useZones;                                          // internal useZones flag        
        private const string Any = "any";                               // constant "any" string for rules        
        private const string AllZones = "allzones";                     // constant "allzones" string for mappings        
        private const string PermCanMap = "truepve.canmap";             // permission for mapping command
        private bool animalsIgnoreSleepers;                             // toggle flag to protect sleepers        
        private bool trace = false;                                     // trace flag        
        private const string traceFile = "ruletrace";                   // tracefile name        
        private const float traceTimeout = 300f;                        // auto-disable trace after 300s (5m)        
        private Timer traceTimer;                                       // trace timeout timer
        private bool tpveEnabled = true;                                // toggle flag for damage handling        
        private List<DamageType> damageTypes = new List<DamageType>
        {
            DamageType.Arrow,
            DamageType.Blunt,
            DamageType.Bullet,
            DamageType.Explosion,
            DamageType.Heat,
            DamageType.Generic,
            DamageType.Slash,
            DamageType.Stab,
        };
        #endregion

        #region Loading/Unloading

        private void Unload()
        {
            Instance = null; 
            scheduleUpdateTimer?.Destroy();
        }

        private void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Name == "ZoneManager")
                ZoneManager = plugin;
            if (plugin.Name == "LiteZones")
                LiteZones = plugin;
            if (ZoneManager != null || LiteZones != null)
                useZones = config?.options.useZones ?? true;
        }

        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "ZoneManager")
                ZoneManager = null;
            if (plugin.Name == "LiteZones")
                LiteZones = null;
            if (ZoneManager == null && LiteZones == null)
                useZones = false;
            traceTimer?.Destroy();
        }

        private void Init()
        {
            Unsubscribe(nameof(OnEntityMarkHostile));
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(OnEntityEnter));
            Unsubscribe(nameof(OnTurretTarget));
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnPlayerConnected));
            Unsubscribe(nameof(OnSamSiteTarget));
            Unsubscribe(nameof(OnTrapTrigger));
            Unsubscribe(nameof(OnNpcTarget)); 
            Instance = this;
            // register console commands automagically
            foreach (Command command in Enum.GetValues(typeof(Command)))
            {
                AddCovalenceCommand($"tpve.{command}", nameof(CommandDelegator));
            }
            // register chat commands
            AddCovalenceCommand("tpve_prod", nameof(CommandDelegator));
            AddCovalenceCommand("tpve_enable", nameof(CommandDelegator));
            AddCovalenceCommand("tpve", nameof(CommandDelegator));
            permission.RegisterPermission(PermCanMap, this);
            // build usage string for console (without sizing)
            usageString = WrapColor("orange", GetMessage("Header_Usage")) + $" - {Version}{Environment.NewLine}" +
                          WrapColor("cyan", $"tpve.{Command.def}") + $" - {GetMessage("Cmd_Usage_def")}{Environment.NewLine}" +
                          WrapColor("cyan", $"tpve.{Command.trace}") + $" - {GetMessage("Cmd_Usage_trace")}{Environment.NewLine}" +
                          WrapColor("cyan", $"tpve.{Command.sched} [enable|disable]") + $" - {GetMessage("Cmd_Usage_sched")}{Environment.NewLine}" +
                          WrapColor("cyan", $"/tpve_prod") + $" - {GetMessage("Cmd_Usage_prod")}{Environment.NewLine}" +
                          WrapColor("cyan", $"/tpve map") + $" - {GetMessage("Cmd_Usage_map")}";
        }

        private void OnServerInitialized(bool isStartup)
        {
            // check for server pve setting
            if (ConVar.Server.pve) WarnPve();
            // load configuration
            config.Init();
            currentRuleSet = config.GetDefaultRuleSet();
            if (currentRuleSet == null)
                PrintWarning(GetMessage("Warning_NoRuleSet"), config.defaultRuleSet);
            useZones = config.options.useZones && (LiteZones != null || ZoneManager != null);
            if (useZones && config.mappings.Count == 1 && config.mappings.FirstOrDefault().Key.Equals(config.defaultRuleSet))
                useZones = false;
            if (config.schedule.enabled)
                TimerLoop(true);
            if (config.ruleSets.Exists(ruleSet => ruleSet.HasFlag(RuleFlags.AnimalsIgnoreSleepers))) Subscribe(nameof(OnNpcTarget));
            if (currentRuleSet == null) return;
            if (config.ruleSets.Exists(ruleSet => (ruleSet._flags & (RuleFlags.StaticTurretsIgnorePlayers | RuleFlags.TrapsIgnorePlayers | RuleFlags.TrapsIgnoreScientist | RuleFlags.TurretsIgnorePlayers | RuleFlags.TurretsIgnoreScientist)) != 0))
            {
                Subscribe(nameof(OnEntityEnter));
                Subscribe(nameof(OnTurretTarget));
            }
            if (config.ruleSets.Exists(ruleSet => (ruleSet._flags & (RuleFlags.SamSitesIgnoreMLRS | RuleFlags.PlayerSamSitesIgnorePlayers | RuleFlags.StaticSamSitesIgnorePlayers)) != 0))
            {
                Subscribe(nameof(OnSamSiteTarget));
            }
            if (config.ruleSets.Exists(ruleSet => (ruleSet._flags & (RuleFlags.TrapsIgnorePlayers | RuleFlags.TrapsIgnoreScientist)) != 0))
            {
                Subscribe(nameof(OnTrapTrigger));
            }
            if (config.schedule.enabled && config.schedule.broadcast && currentBroadcastMessage != null)
            {
                Subscribe(nameof(OnPlayerConnected));
            }
            if (config.options.disableBaseOvenSplash)
            {
                BaseNetworkable.serverEntities.OfType<BaseOven>().ForEach(oven => oven.disabledBySplash = false);
            }
            if (config.options.disableHostility)
            {
                Subscribe(nameof(OnEntityMarkHostile));
            }
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(OnEntityTakeDamage));
        }
        #endregion

        #region Command Handling
        // delegation method for commands
        private void CommandDelegator(IPlayer user, string command, string[] args)
        {
            // return if user doesn't have access to run console command
            if (!user.IsServer && !(user.Object as BasePlayer).IsAdmin) return;

            if (args.Length > 0 && args[0] == "map" && user.HasPermission(PermCanMap))
            {
                CommandMap(user, command, args);
                return;
            }

            if (command == "tpve_prod")
            {
                HandleProd(user);
                return;
            }

            if (command == "tpve_enable")
            {
                tpveEnabled = !tpveEnabled;
                Message(user, "Enable", tpveEnabled);
                return;
            }

            if (command == "tpve" && args.Length != 0) command = args[0];
            else command = command.Replace("tpve.", string.Empty);

            TruePVE.Command @enum;
            if (!Enum.TryParse(command, out @enum))
            {
                user.Reply($"Invalid argument: {command}");
                return;
            }

            switch (@enum)
            {
                case Command.sleepers:
                    HandleSleepers(user);
                    return;
                case Command.def:
                    HandleDef(user);
                    return;
                case Command.sched:
                    HandleScheduleSet(user, args);
                    return;
                case Command.trace:
                    trace = !trace;
                    if (!trace)
                    {
                        tracePlayer = null;
                        traceEntity = null;
                    }
                    else tracePlayer = user.Object as BasePlayer;
                    Message(user, "Notify_TraceToggle", new object[] { trace ? "on" : "off" });
                    if (trace)
                    {
                        traceTimer = timer.In(traceTimeout, () => trace = false);
                    }
                    else traceTimer?.Destroy();
                    return;
                case Command.enable:
                    tpveEnabled = !tpveEnabled;
                    Message(user, "Enable", tpveEnabled);
                    return;
                case Command.usage:
                default:
                    ShowUsage(user);
                    return;
            }
        }

        private void HandleSleepers(IPlayer user)
        {
            if (animalsIgnoreSleepers)
            {
                animalsIgnoreSleepers = false;
                if (!config.ruleSets.Exists(ruleSet => ruleSet.HasFlag(RuleFlags.AnimalsIgnoreSleepers))) Unsubscribe(nameof(OnNpcTarget));
                user.Reply("Sleepers are no longer protected from animals.");
            }
            else
            {
                animalsIgnoreSleepers = true;
                Subscribe(nameof(OnNpcTarget));
                user.Reply("Sleepers are now protected from animals.");
            }
        }
                
        // handle setting defaults
        private void HandleDef(IPlayer user)
        {
            config.options = new ConfigurationOptions();
            Message(user, "Notify_DefConfigLoad");
            LoadDefaultData();
            Message(user, "Notify_DefDataLoad");
            SaveConfig();
        }

        // handle prod command (raycast to determine what player is looking at)
        private void HandleProd(IPlayer user)
        {
            var player = user.Object as BasePlayer;
            if (player == null || !player.IsAdmin)
            {
                Message(user, "Error_NoPermission");
                return;
            }

            object entity;
            if (!GetRaycastTarget(player, out entity) || entity.IsNull())
            {
                SendReply(player, WrapSize(12, WrapColor("red", GetMessage("Error_NoEntityFound", player.UserIDString))));
                return;
            }
            Message(player, "Notify_ProdResult", new object[] { entity.GetType(), (entity as BaseEntity).ShortPrefabName });
        }
                
        private void CommandMap(IPlayer user, string command, string[] args)
        {
            // assume args[0] is the command (beyond /tpve)
            if (args.Length > 0) command = args[0];

            // shift arguments
            args = args.Length > 1 ? args.Skip(1) : new string[0];

            if (command != "map")
            {
                Message(user, "Error_InvalidCommand");
            }
            else if (args.Length == 0)
            {
                Message(user, "Error_InvalidParamForCmd", command);
            }
            else
            {
                string from = args[0]; // mapping name
                string to = args.Length == 2 ? args[1] : null; // target ruleSet/exclude, otherwise delete mapping
                if (to != null)
                {
                    if (to != "exclude" && !config.ruleSets.Exists(r => r.name == to))
                    {
                        // target ruleset must exist, or be "exclude"
                        Message(user, "Error_InvalidMapping", from, to);
                        return;
                    }
                    if (config.HasMapping(from))
                    {
                        string old = config.mappings[from];
                        Message(user, "Notify_MappingUpdated", from, old, to); // update existing mapping
                    }
                    else Message(user, "Notify_MappingCreated", from, to); // add new mapping
                    config.mappings[from] = to;
                    SaveConfig();
                }
                else
                {
                    if (config.HasMapping(from))
                    {
                        Message(user, "Notify_MappingDeleted", from, config.mappings[from]);
                        config.mappings.Remove(from); // remove mapping
                        SaveConfig();
                    }
                    else Message(user, "Error_NoMappingToDelete", from);
                }
            }
        }

        // handles schedule enable/disable
        private void HandleScheduleSet(IPlayer user, string[] args)
        {
            if (args.Length == 0)
            {
                Message(user, "Error_InvalidParamForCmd");
                return;
            }
            if (!config.schedule.valid)
            {
                Message(user, "Notify_InvalidSchedule");
            }
            else if (args[0] == "enable")
            {
                if (config.schedule.enabled) return;
                config.schedule.enabled = true;
                TimerLoop();
                Message(user, "Notify_SchedSetEnabled");
            }
            else if (args[0] == "disable")
            {
                if (!config.schedule.enabled) return;
                config.schedule.enabled = false;
                if (scheduleUpdateTimer != null)
                    scheduleUpdateTimer.Destroy();
                Message(user, "Notify_SchedSetDisabled");
            }
            else
            {
                Message(user, "Error_InvalidParameter", args[0]);
            }
        }
        #endregion

        #region Configuration/Data

        // load config
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                CheckData();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Puts("{0}", ex);
                LoadDefaultConfig();
                return;
            }

            // check config version, update version to current version
            if (config.configVersion != Version.ToString())
            {
                config.configVersion = Version.ToString();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration
            {
                configVersion = Version.ToString(),
                options = new ConfigurationOptions()
            };
            LoadDefaultData();
        }

        // save data
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        // check rulesets and groups
        private void CheckData()
        {
            if (config.ruleSets.IsNullOrEmpty() || config.groups.IsNullOrEmpty())
            {
                LoadDefaultData();
            }
            if (config.schedule == null)
            {
                config.schedule = new Schedule();
            }
            CheckMappings();
        }

        // rebuild mappings
        private bool CheckMappings()
        {
            bool dirty = false;
            foreach (RuleSet rs in config.ruleSets)
            {
                if (!config.mappings.ContainsValue(rs.name))
                {
                    config.mappings[rs.name] = rs.name;
                    dirty = true;
                }
            }
            return dirty;
        }

        // load default data to mappings, rulesets, and groups
        private bool LoadDefaultData()
        {
            config.mappings.Clear();
            config.ruleSets.Clear();
            config.groups.Clear();
            config.schedule = new Schedule();
            config.defaultRuleSet = "default";

            // build groups first
            config.groups.Add(new EntityGroup("barricades")
            {
                members = "Barricade, icewall, GraveYardFence", // "barricade.cover.wood, door_barricade_a, door_barricade_a_large, door_barricade_b, door_barricade_dbl_a, door_barricade_dbl_a_large, door_barricade_dbl_b, door_barricade_dbl_b_large",
                exclusions = "barricade.concrete, barricade.sandbags, barricade.metal, barricade.stone, barricade.wood, barricade.woodwire"
            });

            config.groups.Add(new EntityGroup("dispensers")
            {
                members = "BaseCorpse, HelicopterDebris, PlayerCorpse, NPCPlayerCorpse, HorseCorpse, SkyLantern"
            });

            config.groups.Add(new EntityGroup("fire")
            {
                members = "FireBall, FlameExplosive, FlameThrower, BaseOven, FlameTurret, rocket_heli_napalm, napalm, oilfireball2"
            });

            config.groups.Add(new EntityGroup("guards")
            {
                members = "bandit_guard, scientistpeacekeeper, sentry.scientist.static"
            });

            config.groups.Add(new EntityGroup("heli")
            {
                members = "BaseHelicopter"
            });

            config.groups.Add(new EntityGroup("highwalls")
            {
                members = "SimpleBuildingBlock, wall.external.high.ice, gates.external.high.stone, gates.external.high.wood"
            });

            config.groups.Add(new EntityGroup("ridablehorses")
            {
                members = "RidableHorse"
            });

            config.groups.Add(new EntityGroup("cars")
            {
                members = "BasicCar, ModularCar, BaseModularVehicle, BaseVehicleModule, VehicleModuleEngine, VehicleModuleSeating, VehicleModuleStorage, VehicleModuleTaxi, ModularCarSeat"
            });

            config.groups.Add(new EntityGroup("mini")
            {
                members = "minicopter.entity"
            });

            config.groups.Add(new EntityGroup("scrapheli")
            {
                members = "ScrapTransportHelicopter"
            });

            config.groups.Add(new EntityGroup("ch47")
            {
                members = "ch47.entity"
            });

            config.groups.Add(new EntityGroup("npcs")
            {
                members = "ch47scientists.entity, BradleyAPC, CustomScientistNPC, CustomScientistNpc, ScarecrowNPC, HumanNPC, NPCPlayer, ScientistNPC, TunnelDweller, SimpleShark, UnderwaterDweller, Zombie, ZombieNPC"
            });

            config.groups.Add(new EntityGroup("players")
            {
                members = "BasePlayer, FrankensteinPet"
            });

            config.groups.Add(new EntityGroup("resources")
            {
                members = "ResourceEntity, TreeEntity, OreResourceEntity, LootContainer",
                exclusions = "hobobarrel.deployed"
            });

            config.groups.Add(new EntityGroup("snowmobiles")
            {
                members = "snowmobile, tomahasnowmobile"
            });

            config.groups.Add(new EntityGroup("traps")
            {
                members = "AutoTurret, BearTrap, FlameTurret, Landmine, GunTrap, ReactiveTarget, TeslaCoil, spikes.floor"
            });

            config.groups.Add(new EntityGroup("junkyard")
            {
                members = "magnetcrane.entity, carshredder.entity"
            });

            // create default ruleset
            RuleSet defaultRuleSet = new RuleSet(config.defaultRuleSet)
            {
                _flags = RuleFlags.HumanNPCDamage | RuleFlags.LockedBoxesImmortal | RuleFlags.LockedDoorsImmortal | RuleFlags.PlayerSamSitesIgnorePlayers | RuleFlags.TrapsIgnorePlayers | RuleFlags.TurretsIgnorePlayers,
                flags = "HumanNPCDamage, LockedBoxesImmortal, LockedDoorsImmortal, PlayerSamSitesIgnorePlayers, TrapsIgnorePlayers, TurretsIgnorePlayers"
            };

            // create rules and add to ruleset
            defaultRuleSet.AddRule("anything can hurt dispensers");
            defaultRuleSet.AddRule("anything can hurt resources");
            defaultRuleSet.AddRule("anything can hurt barricades");
            defaultRuleSet.AddRule("anything can hurt traps");
            defaultRuleSet.AddRule("anything can hurt heli");
            defaultRuleSet.AddRule("anything can hurt npcs");
            defaultRuleSet.AddRule("anything can hurt players");
            defaultRuleSet.AddRule("nothing can hurt ch47");
            defaultRuleSet.AddRule("nothing can hurt cars");
            defaultRuleSet.AddRule("nothing can hurt mini");
            defaultRuleSet.AddRule("nothing can hurt snowmobiles");
            //defaultRuleSet.AddRule("nothing can hurt guards");
            defaultRuleSet.AddRule("nothing can hurt ridablehorses");
            defaultRuleSet.AddRule("cars cannot hurt anything");
            defaultRuleSet.AddRule("mini cannot hurt anything");
            defaultRuleSet.AddRule("ch47 cannot hurt anything");
            defaultRuleSet.AddRule("scrapheli cannot hurt anything");
            defaultRuleSet.AddRule("players cannot hurt players");
            defaultRuleSet.AddRule("players cannot hurt traps");
            defaultRuleSet.AddRule("guards cannot hurt players");
            defaultRuleSet.AddRule("fire cannot hurt players");
            defaultRuleSet.AddRule("traps cannot hurt players");
            defaultRuleSet.AddRule("highwalls cannot hurt players");
            defaultRuleSet.AddRule("barricades cannot hurt players");
            defaultRuleSet.AddRule("mini cannot hurt mini");
            defaultRuleSet.AddRule("npcs can hurt players");
            defaultRuleSet.AddRule("junkyard cannot hurt anything");
            defaultRuleSet.AddRule("junkyard can hurt cars");

            config.ruleSets.Add(defaultRuleSet); // add ruleset to rulesets list

            config.mappings[config.defaultRuleSet] = config.defaultRuleSet; // create mapping for ruleset

            return true;
        }

        private bool ResetRules(string key)
        {
            if (string.IsNullOrEmpty(key) || config == null)
            {
                return false;
            }

            string old = config.defaultRuleSet;

            config.defaultRuleSet = key;
            currentRuleSet = config.GetDefaultRuleSet();

            if (currentRuleSet == null)
            {
                config.defaultRuleSet = old;
                currentRuleSet = config.GetDefaultRuleSet();
                return false;
            }

            return true;
        }
        #endregion

        #region Trace
        private BaseEntity traceEntity;
        private BasePlayer tracePlayer;

        private void Trace(string message, int indentation = 0)
        {
            if (config.options.PlayerConsole || config.options.ServerConsole)
            {
                if (tracePlayer.IsReallyConnected() && traceEntity != null)
                {
                    if (config.options.MaxTraceDistance == 0 || tracePlayer.Distance(traceEntity) <= config.options.MaxTraceDistance)
                    {
                        if (config.options.PlayerConsole)
                        {
                            tracePlayer.ConsoleMessage(message);
                        }

                        if (config.options.ServerConsole)
                        {
                            Puts(message);
                        }

                        _tsb.AppendLine(string.Empty.PadLeft(indentation, ' ') + message);
                    }
                }
            }
            else _tsb.AppendLine(string.Empty.PadLeft(indentation, ' ') + message);
        }

        private void LogTrace()
        {
            var text = _tsb.ToString();
            traceEntity = null;
            try
            {
                LogToFile(traceFile, text, this);
            }
            catch (IOException)
            {
                timer.Once(1f, () => LogToFile(traceFile, text, this));
                return;
            }
            _tsb.Length = 0;
        }
        private StringBuilder _tsb = new StringBuilder();
        #endregion Trace

        #region Hooks/Handler Procedures
        private void OnPlayerConnected(BasePlayer player)
        {
            SendReply(player, GetMessage("Prefix") + currentBroadcastMessage);
        }

        private string CurrentRuleSetName() => currentRuleSet.name;
        private bool IsEnabled() => tpveEnabled;

        // handle damage - if another mod must override TruePVE damages or take priority,
        // set handleDamage to false and reference HandleDamage from the other mod(s)
        private object OnEntityTakeDamage(ResourceEntity entity, HitInfo hitInfo)
        {
            // if default global is not enabled, return true (allow all damage)
            if (hitInfo == null || currentRuleSet == null || currentRuleSet.IsEmpty() || !currentRuleSet.enabled)
            {
                return null;
            }

            // get entity and initiator locations (zones)
            List<string> entityLocations = GetLocationKeys(entity);
            List<string> initiatorLocations = GetLocationKeys(hitInfo.Initiator);
            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, trace))
            {
                if (trace) Trace("Exclusion found; allow and return", 1);
                return null;
            }

            if (trace) Trace("No exclusion found - looking up RuleSet...", 1);
            // process location rules
            RuleSet ruleSet = GetRuleSet(entityLocations, initiatorLocations);

            return EvaluateRules(entity, hitInfo, ruleSet) ? (object)null : true;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity.IsNull() || hitInfo == null || AllowKillingSleepers(entity, hitInfo))
            {
                return null;
            }

            object extCanTakeDamage = Interface.CallHook("CanEntityTakeDamage", new object[] { entity, hitInfo });

            if (extCanTakeDamage is bool)
            {
                if ((bool)extCanTakeDamage)
                {
                    return null;
                }

                CancelHit(hitInfo);
                return true;
            }

            if (!config.options.handleDamage)
            {
                return null;
            }

            var majority = hitInfo.damageTypes.GetMajorityDamageType();

            if (majority == DamageType.Decay || majority == DamageType.Fall || majority == DamageType.Radiation)
            {
                return null;
            }

            if (!AllowDamage(entity, hitInfo))
            {
                if (trace) LogTrace();
                CancelHit(hitInfo);
                return true;
            }

            if (trace) LogTrace();
            return null;
        }

        private void CancelHit(HitInfo hitInfo)
        {
            hitInfo.damageTypes = new DamageTypeList();
            hitInfo.DidHit = false;
            hitInfo.DoHitEffects = false;
        }

        private bool AllowKillingSleepers(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity.ShortPrefabName != "player")
            {
                return false;
            }
            var victim = entity.ToPlayer();
            if (victim.IsSleeping())
            {
                if (config.AllowKillingSleepersAlly && hitInfo.Initiator is BasePlayer && IsAlly(victim.userID, hitInfo.InitiatorPlayer.userID))
                {
                    return true;
                }
                return config.AllowKillingSleepers;
            }
            return false;
        }

        // determines if an entity is "allowed" to take damage
        private bool AllowDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (trace)
            {
                traceEntity = entity;
                _tsb.Length = 0;
            }

            // if default global is not enabled or entity is npc, allow all damage
            if (currentRuleSet == null || currentRuleSet.IsEmpty() || !currentRuleSet.enabled || entity is BaseNpc || entity is BaseHelicopter)
            {
                return true;
            }

            // allow damage to door barricades and covers
            if (entity is Barricade && (entity.ShortPrefabName.Contains("door_barricade") || entity.ShortPrefabName.Contains("cover")))
            {
                return true;
            }
            
            // if entity is a barrel, trash can, or giftbox, allow damage (exclude water and hobo barrels)
            if (!entity.ShortPrefabName.Equals("waterbarrel") && entity.prefabID != 1748062128)
            {
                if (entity.ShortPrefabName.Contains("barrel") || entity.ShortPrefabName.Equals("loot_trash") || entity.ShortPrefabName.Equals("giftbox_loot"))
                {
                    return true;
                }
            }

            var weapon = hitInfo.Initiator ?? hitInfo.Weapon ?? hitInfo.WeaponPrefab;

            TrySetInitiator(hitInfo, weapon);

            if (trace)
            {
                // Sometimes the initiator is not the attacker (turrets)
                Trace("======================" + Environment.NewLine +
                  "==  STARTING TRACE  ==" + Environment.NewLine +
                  "==  " + DateTime.Now.ToString("HH:mm:ss.fffff") + "  ==" + Environment.NewLine +
                  "======================");
                Trace($"From: {weapon?.GetType().Name ?? "Unknown_Weapon"}, {weapon?.ShortPrefabName ?? "Unknown_Prefab"}", 1);
                Trace($"To: {entity.GetType().Name}, {entity.ShortPrefabName}", 1);
            }

            // get entity and initiator locations (zones)
            List<string> entityLocations = GetLocationKeys(entity);
            List<string> initiatorLocations = GetLocationKeys(weapon);
            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, trace))
            {
                if (trace) Trace("Exclusion found; allow and return", 1);
                return true;
            }

            if (trace) Trace("No exclusion found - looking up RuleSet...", 1);

            // process location rules
            RuleSet ruleSet = GetRuleSet(entityLocations, initiatorLocations);

            if (trace) Trace($"Using RuleSet \"{ruleSet.name}\"", 1);

            if (config.Firework && entity is BaseFirework)
            {
                return true;
            }

            if (weapon?.ShortPrefabName == "maincannonshell" || weapon is BradleyAPC)
            {
                if (trace) Trace("Initiator is BradleyAPC; evaluating RuleSet rules...", 1);
                return EvaluateRules(entity, weapon, ruleSet);
            }

            var attacker = hitInfo.Initiator as BasePlayer;

            if (!attacker.IsRealNull() && !attacker.userID.IsSteamId())
            {
                // check for sleeper protection - return false if sleeper protection is on (true)
                if (entity is BasePlayer && ruleSet.HasFlag(RuleFlags.ProtectedSleepers) && entity.ToPlayer().IsSleeping())
                {
                    if (trace) Trace("Target is sleeping player, with ProtectedSleepers flag set; block and return", 1);
                    return false;
                }

                if (ruleSet.HasFlag(RuleFlags.NpcsCanHurtAnything))
                {
                    if (trace) Trace("Initiator is NPC; flag set; allow damage and return", 1);
                    return true;
                }
            }

            if (ruleSet.HasFlag(RuleFlags.VehiclesTakeCollisionDamageWithoutDriver) && entity is BaseMountable && weapon == entity)
            {
                BaseVehicle vehicle = entity.HasParent() ? (entity as BaseMountable).VehicleParent() : entity as BaseVehicle;

                if (vehicle.IsValid())
                {
                    var player = vehicle.GetDriver();

                    if (trace) Trace($"Vehicle collision: { (player.IsNull() ? "No driver; allow and return" : "Has driver; continue checks") }", 1);

                    if (player.IsNull())
                    {
                        return true;
                    }
                }
            }

            // Check storage containers and doors for locks
            if (ruleSet.HasFlag(RuleFlags.LockedBoxesImmortal) && entity is StorageContainer || ruleSet.HasFlag(RuleFlags.LockedDoorsImmortal) && entity is Door)
            {
                if (!(entity is LootContainer))
                {
                    // check for lock
                    object hurt = CheckLock(ruleSet, entity, hitInfo);
                    if (trace) Trace($"Door/StorageContainer detected with immortal flag; lock check results: { (hurt == null ? "null (no lock or unlocked); continue checks" : (bool)hurt ? "allow and return" : "block and return") }", 1);
                    if (hurt is bool)
                    {
                        return (bool)hurt;
                    }
                }
            }

            // check heli and turret
            object heli = CheckHeliInitiator(ruleSet, hitInfo);
            
            if (heli is bool)
            {
                return HandleHelicopter(ruleSet, entity, heli);
            }
            
            // after heli check, return true if initiator is null
            if (hitInfo.Initiator.IsNull())
            {
                if (damageTypes.Exists(x => hitInfo.damageTypes.Get(x) > 0f))
                {
                    if (trace) Trace($"Initiator empty for player damage; block and return", 1);
                    return false;
                }
                if (weapon is MLRSRocket)
                {
                    if (trace) Trace($"Initiator empty for MLRS Rocket; block and return", 1);
                    return false;
                }
                if (trace) Trace($"Initiator empty; allow and return", 1);
                return true;
            }

            bool isVictim = entity is BasePlayer;
            BasePlayer victim = isVictim ? entity as BasePlayer : null;

            if (hitInfo.Initiator is BaseNpc)
            {
                // check for sleeper protection - return false if sleeper protection is on (true)
                if (isVictim && ruleSet.HasFlag(RuleFlags.ProtectedSleepers) && victim.IsSleeping())
                {
                    if (trace) Trace("Target is sleeping player, with ProtectedSleepers flag set; block and return", 1);
                    return false;
                }

                if (trace) Trace("Initiator is NPC; allow and return", 1);
                return true; // allow NPC damage to other entities if sleeper protection is off
            }

            if (hitInfo.Initiator is SamSite && (isVictim || entity is BaseMountable))
            {
                if (CheckExclusion(hitInfo.Initiator))
                {
                    if (trace) Trace($"Initiator is samsite, and target is player; exclusion found; allow and return", 1);
                    return true;
                }

                bool isAllowed = (hitInfo.Initiator as SamSite).staticRespawn ? !ruleSet.HasFlag(RuleFlags.StaticSamSitesIgnorePlayers) : !ruleSet.HasFlag(RuleFlags.PlayerSamSitesIgnorePlayers);

                if (trace) Trace($"Initiator is samsite, and target is player; {(isAllowed ? "flag not set; allow and return" : "flag set; block and return")}", 1);

                return isAllowed;
            }

            if (isVictim)
            {
                if (!ruleSet.HasFlag(RuleFlags.StaticTurretsIgnorePlayers) && hitInfo.Initiator is AutoTurret && !hitInfo.Initiator.OwnerID.IsSteamId())
                {
                    return true;
                }

                var damageType = hitInfo.damageTypes.GetMajorityDamageType();

                if ((damageType == DamageType.Slash || damageType == DamageType.Stab) && hitInfo.Initiator == null)
                {
                    var lastAttacker = entity.lastAttacker as BasePlayer;

                    if (lastAttacker == null || !lastAttacker.userID.IsSteamId())
                    {
                        if (trace) Trace("Initiator is hurt trigger; allow damage and return", 1);
                        return true;
                    }
                }

                if (entity == attacker && damageType == DamageType.Bullet && hitInfo.damageTypes.Total() < 0f)
                {
                    return true;
                }

                // handle suicide
                if (victim.userID.IsSteamId() && hitInfo.damageTypes?.Get(DamageType.Suicide) > 0)
                {
                    if (trace) Trace($"DamageType is suicide; blocked? {(ruleSet.HasFlag(RuleFlags.SuicideBlocked) ? "true; block and return" : "false; allow and return")}", 1);
                    if (ruleSet.HasFlag(RuleFlags.SuicideBlocked))
                    {
                        Message(victim, "Error_NoSuicide");
                        return false;
                    }
                    return true;
                }

                // allow players to hurt themselves
                if (ruleSet.HasFlag(RuleFlags.SelfDamage) && victim.userID.IsSteamId() && hitInfo.Initiator == entity)
                {
                    if (trace) Trace($"SelfDamage flag; player inflicted damage to self; allow and return", 1);
                    return true;
                }
            }
            
            if (hitInfo.Initiator is MiniCopter && entity is BuildingBlock)
            {
                if (trace) Trace("Initiator is minicopter, target is building; evaluate and return", 1);
                return EvaluateRules(entity, hitInfo, ruleSet);
            }

            if (ruleSet.HasFlag(RuleFlags.TwigDamage) && entity is BuildingBlock)
            {
                var block = entity as BuildingBlock;

                if (block.grade == BuildingGrade.Enum.Twigs)
                {
                    if (ruleSet.HasFlag(RuleFlags.TwigDamageRequiresOwnership) && attacker.IsValid() && attacker.userID.IsSteamId()) // Allow twig damage by owner or anyone authed if ruleset flag is set
                    {
                        if (IsAlly(entity.OwnerID, attacker.userID))
                        {
                            return true;
                        }
                        return attacker.IsBuildingAuthed(entity.transform.position, entity.transform.rotation, entity.bounds);
                    }
                    return true; // Allow twig damage by anyone if ruleset flag is set
                }
            }

            if (attacker.IsValid())
            {
                if (attacker.isMounted)
                {
                    if (!EvaluateRules(entity, attacker.GetMounted(), ruleSet, false))
                    {
                        return false;
                    }
                }

                if (ruleSet.HasFlag(RuleFlags.NoPlayerDamageToMini) && entity is MiniCopter && !(entity is ScrapTransportHelicopter))
                {
                    if (trace) Trace("Initiator is player and target is MiniCopter, with NoPlayerDamageToMini flag set; block and return", 1);
                    return false;
                }

                if (ruleSet.HasFlag(RuleFlags.NoPlayerDamageToScrap) && entity is ScrapTransportHelicopter)
                {
                    if (trace) Trace("Initiator is player and target is ScrapTransportHelicopter, with NoPlayerDamageToScrap flag set; block and return", 1);
                    return false;
                }

                if (ruleSet.HasFlag(RuleFlags.NoPlayerDamageToCar) && entity.name.Contains("modularcar"))
                {
                    if (trace) Trace("Initiator is player and target is ModularCar, with NoPlayerDamageToCar flag set; block and return", 1);
                    return false;
                }

                if (entity is AdvancedChristmasLights)
                {
                    if (entity.OwnerID == 0)
                    {
                        return attacker.CanBuild();
                    }
                }
                else if (entity is GrowableEntity)
                {
                    if (attacker.CanBuild())
                    {
                        return true;
                    }

                    var ge = entity as GrowableEntity;
                    var planter = ge.GetPlanter();

                    return planter.IsNull() || !planter.OwnerID.IsSteamId() || planter.OwnerID == attacker.userID;
                }

                if (isVictim)
                {
                    if (ruleSet.HasFlag(RuleFlags.FriendlyFire) && victim.userID.IsSteamId() && IsAlly(attacker.userID, victim.userID))
                    {
                        if (trace) Trace("Initiator and target are allied players, with FriendlyFire flag set; allow and return", 1);
                        return true;
                    }

                    // allow sleeper damage by admins if configured
                    if (ruleSet.HasFlag(RuleFlags.AdminsHurtSleepers) && attacker.IsAdmin && victim.IsSleeping())
                    {
                        if (trace) Trace("Initiator is admin player and target is sleeping player, with AdminsHurtSleepers flag set; allow and return", 1);
                        return true;
                    }

                    // allow Human NPC damage if configured
                    if (ruleSet.HasFlag(RuleFlags.HumanNPCDamage) && IsHumanNPC(attacker, victim))
                    {
                        if (trace) Trace("Initiator or target is HumanNPC, with HumanNPCDamage flag set; allow and return", 1);
                        return true;
                    }
                }
                else if (ruleSet.HasFlag(RuleFlags.AuthorizedDamage) && !isVictim && !entity.IsNpc)
                { // ignore checks if authorized damage enabled (except for players and npcs)
                    if (ruleSet.HasFlag(RuleFlags.AuthorizedDamageRequiresOwnership) && !IsAlly(entity.OwnerID, attacker.userID) && CanAuthorize(entity, attacker, ruleSet))
                    {
                        if (trace) Trace("Initiator is player who does not own non-player target; block and return", 1);
                        return false;
                    }

                    if (CheckAuthorized(entity, attacker, ruleSet))
                    {
                        if (entity is SamSite || entity.name.Contains("modular") || entity is BaseMountable)
                        {
                            if (trace) Trace($"Target is {entity.GetType().Name}; evaluate and return", 1);
                            return EvaluateRules(entity, hitInfo, ruleSet);
                        }
                        if (trace) Trace("Initiator is player with authorization over non-player target; allow and return", 1);
                        return true;
                    }
                }
            }

            if (trace) Trace("No match in pre-checks; evaluating RuleSet rules...", 1);
            return EvaluateRules(entity, hitInfo, ruleSet);
        }

        private void TrySetInitiator(HitInfo hitInfo, BaseEntity weapon)
        {
            if (weapon == null)
            {
                return;
            }
            
            if (!(hitInfo.Initiator is BasePlayer) && weapon.creatorEntity is BasePlayer)
            {
                hitInfo.Initiator = weapon.creatorEntity;
            }

            if (hitInfo.Initiator == null)
            {
                hitInfo.Initiator = weapon.GetParentEntity();
            }

            if (hitInfo.Initiator == null)
            {
                hitInfo.Initiator = weapon;
            }
        }

        private bool HandleHelicopter(RuleSet ruleSet, BaseCombatEntity entity, object heli)
        {
            if (entity is BasePlayer)
            {
                var victim = entity as BasePlayer;

                if (ruleSet.HasFlag(RuleFlags.NoHeliDamageSleepers))
                {
                    if (trace) Trace($"Initiator is heli, and target is player; flag check results: { (victim.IsSleeping() ? "victim is sleeping; block and return" : "victim is not sleeping; continue checks") }", 1);
                    if (victim.IsSleeping()) return false;
                }

                if (trace) Trace($"Initiator is heli, and target is player; flag check results: { (ruleSet.HasFlag(RuleFlags.NoHeliDamagePlayer) ? "flag set; block and return" : "flag not set; allow and return") }", 1);
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamagePlayer);
            }
            if (entity is MiningQuarry)
            {
                if (trace) Trace($"Initiator is heli, and target is quarry; flag check results: { (ruleSet.HasFlag(RuleFlags.NoHeliDamageQuarry) ? "flag set; block and return" : "flag not set; allow and return") }", 1);
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamageQuarry);
            }
            if (entity is RidableHorse)
            {
                if (trace) Trace($"Initiator is heli, and target is ridablehorse; flag check results: { (ruleSet.HasFlag(RuleFlags.NoHeliDamageRidableHorses) ? "flag set; block and return" : "flag not set; allow and return") }", 1);
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamageRidableHorses);
            }
            if (trace) Trace($"Initiator is heli, target is non-player; results: { ((bool)heli ? "allow and return" : "block and return") }", 1);
            //return EvaluateRules(entity, weapon, ruleSet);
            return (bool)heli;
        }

        public bool IsAlly(ulong playerId, ulong targetId)
        {
            RelationshipManager.PlayerTeam team;
            if (RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out team) && team.members.Contains(targetId))
            {
                return true;
            }

            if (Convert.ToBoolean(Clans?.Call("IsMemberOrAlly", playerId.ToString(), targetId.ToString())))
            {
                return true;
            }

            if (Convert.ToBoolean(Friends?.Call("AreFriends", playerId.ToString(), targetId.ToString())))
            {
                return true;
            }

            return false;
        }

        private bool CanAuthorize(BaseEntity entity, BasePlayer attacker, RuleSet ruleSet)
        {
            if (entity is BaseVehicle && !EvaluateRules(entity, attacker, ruleSet, false))
            {
                return false;
            }

            if (entity.OwnerID == 0)
            {
                return entity is MiniCopter || entity.prefabID == WALL_LOW_JPIPE && !entity.enableSaving;
            }

            return IsPlayerEntity(entity);
        }

        private HashSet<string> _deployables = new HashSet<string>();

        private bool IsPlayerEntity(BaseEntity entity)
        {
            if (_deployables.Count == 0)
            {
                foreach (var def in ItemManager.GetItemDefinitions())
                {
                    var imd = def.GetComponent<ItemModDeployable>();
                    if (imd == null) continue;
                    _deployables.Add(imd.entityPrefab.resourcePath);
                }
            }
            return entity.PrefabName.Contains("building") || entity.PrefabName.Contains("modular") || entity is BaseMountable || _deployables.Contains(entity.PrefabName);
        }

        // process rules to determine whether to allow damage
        private bool EvaluateRules(BaseEntity entity, BaseEntity attacker, RuleSet ruleSet, bool returnDefaultValue = true)
        {
            List<string> e0Groups = config.ResolveEntityGroups(attacker);
            List<string> e1Groups = config.ResolveEntityGroups(entity);

            if (trace)
            {
                Trace($"Initiator EntityGroup matches: { (e0Groups.IsNullOrEmpty() ? "none" : string.Join(", ", e0Groups.ToArray())) }", 2);
                Trace($"Target EntityGroup matches: { (e1Groups.IsNullOrEmpty() ? "none" : string.Join(", ", e1Groups.ToArray())) }", 2);
            }

            return ruleSet.Evaluate(e0Groups, e1Groups, attacker, returnDefaultValue);
        }

        private bool EvaluateRules(BaseEntity entity, HitInfo hitInfo, RuleSet ruleSet)
        {
            return EvaluateRules(entity, hitInfo.Initiator ?? hitInfo.WeaponPrefab, ruleSet);
        }

        // checks an entity to see if it has a lock
        private object CheckLock(RuleSet ruleSet, BaseEntity entity, HitInfo hitInfo)
        {
            var slot = entity.GetSlot(BaseEntity.Slot.Lock); // check for lock

            if (slot.IsNull() || !slot.IsLocked())
            {
                return null; // no lock or unlocked, continue checks
            }

            // if HeliDamageLocked flag is false or NoHeliDamage flag, all damage is cancelled from immortal flag
            if (!ruleSet.HasFlag(RuleFlags.HeliDamageLocked) || ruleSet.HasFlag(RuleFlags.NoHeliDamage))
            {
                return false;
            }

            object heli = CheckHeliInitiator(ruleSet, hitInfo);

            return Convert.ToBoolean(heli); // cancel damage except from heli
        }

        private object CheckHeliInitiator(RuleSet ruleSet, HitInfo hitInfo)
        {
            // Check for heli initiator
            if (hitInfo.Initiator is BaseHelicopter || (hitInfo.Initiator != null && (hitInfo.Initiator.ShortPrefabName.Equals("oilfireballsmall") || hitInfo.Initiator.ShortPrefabName.Equals("napalm"))))
            {
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamage);
            }
            else if (hitInfo.WeaponPrefab != null && (hitInfo.WeaponPrefab.ShortPrefabName.Equals("rocket_heli") || hitInfo.WeaponPrefab.ShortPrefabName.Equals("rocket_heli_napalm")))
            {
                return !ruleSet.HasFlag(RuleFlags.NoHeliDamage);
            }
            return null;
        }

        // checks if the player is authorized to damage the entity
        private bool CheckAuthorized(BaseEntity entity, BasePlayer player, RuleSet ruleSet)
        {
            if (!ruleSet.HasFlag(RuleFlags.CupboardOwnership))
            {
                if (entity.OwnerID == 0 || IsAlly(entity.OwnerID, player.userID))
                {
                    return true; // allow damage to entities that the player owns
                }

                return player.IsBuildingAuthed(entity.WorldSpaceBounds());
            }

            // treat entities outside of cupboard range as unowned, and entities inside cupboard range require authorization
            return player.CanBuild(entity.WorldSpaceBounds());
        }

        private bool IsFunTurret(bool isAutoTurret, BaseEntity entity)
        {
            if (!isAutoTurret)
            {
                return false;
            }

            var weapon = (entity as AutoTurret).GetAttachedWeapon()?.GetItem();

            if (weapon?.info?.shortname?.StartsWith("fun.") ?? false)
            {
                return true;
            }

            return false;
        }

        private object OnSamSiteTarget(SamSite ss, BaseEntity entity)
        {
            object extCanEntityBeTargeted = Interface.CallHook("CanEntityBeTargeted", new object[] { entity, ss });
            
            if (extCanEntityBeTargeted is bool)
            {
                if ((bool)extCanEntityBeTargeted)
                {
                    if (trace) Trace($"CanEntityBeTargeted allowed {entity.ShortPrefabName} to be targetted by SamSite", 1);
                    return null;
                }

                if (trace) Trace($"CanEntityBeTargeted blocked {entity.ShortPrefabName} from being targetted by SamSite", 1);
                ss.CancelInvoke(ss.WeaponTick);
                return true;
            }

            RuleSet ruleSet = GetRuleSet(entity, ss);

            if (ruleSet == null)
            {
                if (trace) Trace($"OnSamSiteTarget allowed {entity.ShortPrefabName} to be targetted; no ruleset found.", 1);
                return null;
            }

            if (ss.staticRespawn && ruleSet.HasFlag(RuleFlags.StaticSamSitesIgnorePlayers)) return SamSiteHelper(ss, entity);
            if (!ss.staticRespawn && ruleSet.HasFlag(RuleFlags.PlayerSamSitesIgnorePlayers)) return SamSiteHelper(ss, entity);
            if ((entity is MLRS || entity is MLRSRocket) && ruleSet.HasFlag(RuleFlags.SamSitesIgnoreMLRS)) return SamSiteHelper(ss, entity);

            return null;
        }

        private object OnEntityMarkHostile(BasePlayer player, float duration) => true;

        private void OnEntitySpawned(BaseOven oven)
        {
            if (config.options.disableBaseOvenSplash)
            {
                oven.disabledBySplash = false;
            }
        }

        private void OnEntitySpawned(MLRSRocket rocket)
        {
            if (rocket.IsNull()) return;
            var systems = FindEntitiesOfType<MLRS>(rocket.transform.position, 15f);
            if (systems.Count == 0 || CheckIsEventTerritory(systems[0].TrueHitPos)) return;
            var owner = systems[0].rocketOwnerRef.Get(true) as BasePlayer;
            if (owner.IsNull()) return;
            rocket.creatorEntity = owner;
            rocket.OwnerID = owner.userID;
        }

        private bool CheckIsEventTerritory(Vector3 position)
        {
            if (AbandonedBases.CanCall() && Convert.ToBoolean(AbandonedBases?.Call("EventTerritory", position))) return true;
            if (RaidableBases.CanCall() && Convert.ToBoolean(RaidableBases?.Call("EventTerritory", position))) return true;
            return false;
        }

        private static List<T> FindEntitiesOfType<T>(Vector3 a, float n, int m = -1) where T : BaseEntity
        {
            int hits = Physics.OverlapSphereNonAlloc(a, n, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            List<T> entities = new List<T>();
            for (int i = 0; i < hits; i++)
            {
                var entity = Vis.colBuffer[i]?.ToBaseEntity();
                if (entity is T) entities.Add(entity as T);
                Vis.colBuffer[i] = null;
            }
            return entities;
        }

        private object SamSiteHelper(SamSite ss, BaseEntity entity)
        {
            var entityLocations = GetLocationKeys(entity);
            var initiatorLocations = GetLocationKeys(ss);

            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, false))
            {
                if (trace) Trace($"OnSamSiteTarget allowed {entity.ShortPrefabName} to be targetted; exclusion of zone found.", 1);
                return null;
            }

            // check for exclusions in entity groups
            if (CheckExclusion(ss))
            {
                if (trace) Trace($"OnSamSiteTarget allowed {entity.ShortPrefabName} to be targetted; exclusion found in entity group.", 1);
                return null;
            }

            if (trace && entity is BasePlayer) Trace($"SamSitesIgnorePlayers blocked {entity.ShortPrefabName} from being targetted.", 1);
            else if (trace && entity is MLRS) Trace($"SamSitesIgnoreMLRS blocked {entity.ShortPrefabName} from being targetted.", 1);
            ss.CancelInvoke(ss.WeaponTick);
            return true;
        }

        // check if entity can be targeted
        private object OnEntityEnter(TargetTrigger trigger, BasePlayer target)
        {
            if (trigger == null || target == null)
            {
                return null;
            }

            var entity = trigger.GetComponentInParent<BaseEntity>();

            return OnEntityEnterInternal(entity, target);

        }

        private object OnEntityEnterInternal(BaseEntity entity, BasePlayer target)
        { 
            if (entity == null || entity is NPCAutoTurret || target == null)
            {
                return null;
            }

            object extCanEntityBeTargeted = Interface.CallHook("CanEntityBeTargeted", new object[] { target, entity });

            if (extCanEntityBeTargeted is bool)
            {
                if ((bool)extCanEntityBeTargeted)
                {
                    return null;
                }

                return true;
            }

            RuleSet ruleSet = GetRuleSet(target, entity);

            if (ruleSet == null)
            {
                return null;
            }

            var isAutoTurret = entity is AutoTurret;
            var isStatic = !entity.OwnerID.IsSteamId();

            if (target.IsNpc || !target.userID.IsSteamId())
            {
                if (isAutoTurret)
                {
                    var obj = ruleSet.HasFlag(RuleFlags.TurretsIgnoreScientist) && entity.OwnerID.IsSteamId() ? true : (object)null;
                    return obj;
                }
                else
                {
                    var obj = ruleSet.HasFlag(RuleFlags.TrapsIgnoreScientist) ? true : (object)null;
                    return obj;
                }
            }
            else if (isAutoTurret && ruleSet.HasFlag(isStatic ? RuleFlags.StaticTurretsIgnorePlayers : RuleFlags.TurretsIgnorePlayers) || !isAutoTurret && ruleSet.HasFlag(RuleFlags.TrapsIgnorePlayers))
            {
                if (IsFunTurret(isAutoTurret, entity))
                {
                    return null;
                }

                var entityLocations = GetLocationKeys(target);
                var initiatorLocations = GetLocationKeys(entity);

                // check for exclusion zones (zones with no rules mapped)
                if (CheckExclusion(entityLocations, initiatorLocations, trace))
                {
                    return null;
                }

                // check for exclusions in entity group
                if (CheckExclusion(target, entity) || CheckExclusion(entity))
                {
                    return null;
                }

                return true;
            }

            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BasePlayer target)
        {
            return OnEntityEnterInternal(turret, target);
        }

        // ignore players stepping on traps if configured
        private object OnTrapTrigger(BaseTrap trap, GameObject go)
        {
            var player = go.GetComponent<BasePlayer>();

            if (player.IsNull() || trap.IsNull())
            {
                return null;
            }

            object extCanEntityTrapTrigger = Interface.CallHook("CanEntityTrapTrigger", new object[] { trap, player });

            if (extCanEntityTrapTrigger is bool)
            {
                if ((bool)extCanEntityTrapTrigger)
                {
                    return null;
                }

                return true;
            }

            var entityLocations = GetLocationKeys(player);
            var initiatorLocations = GetLocationKeys(trap);
            RuleSet ruleSet = GetRuleSet(player, trap);

            if (ruleSet == null)
            {
                return null;
            }

            if ((player.IsNpc || !player.userID.IsSteamId()) && ruleSet.HasFlag(RuleFlags.TrapsIgnoreScientist))
            {
                return true;
            }
            else if (!player.IsNpc && player.userID.IsSteamId() && ruleSet.HasFlag(RuleFlags.TrapsIgnorePlayers))
            {
                // check for exclusion zones (zones with no rules mapped)
                if (CheckExclusion(entityLocations, initiatorLocations, false))
                {
                    return null;
                }

                if (CheckExclusion(trap))
                {
                    return null;
                }

                return true;
            }

            return null;
        }

        private object OnNpcTarget(BaseNpc npc, BasePlayer target)
        {
            if (!target.IsValid() || target.IsNpc || !target.userID.IsSteamId() || !target.IsSleeping())
            {
                return null;
            }

            RuleSet ruleSet = GetRuleSet(target, npc);

            if (ruleSet == null || !animalsIgnoreSleepers && !ruleSet.HasFlag(RuleFlags.AnimalsIgnoreSleepers))
            {
                return null;
            }

            var entityLocations = GetLocationKeys(target);
            var initiatorLocations = GetLocationKeys(npc);

            // check for exclusion zones (zones with no rules mapped)
            if (CheckExclusion(entityLocations, initiatorLocations, false))
            {
                return null;
            }

            return true;
        }

        // Check for exclusions in entity groups (attacker)
        private bool CheckExclusion(BaseEntity attacker)
        {
            string attackerName = attacker.GetType().Name;

            return config.groups.Exists(group => group.IsExclusion(attacker.ShortPrefabName) || group.IsExclusion(attackerName));
        }

        // Check for exclusions in entity groups (target, attacker)
        private bool CheckExclusion(BaseEntity target, BaseEntity attacker)
        {
            string targetName = target.GetType().Name;

            if (!config.groups.Exists(group => group.IsMember(target.ShortPrefabName) || group.IsExclusion(targetName)))
            {
                return false;
            }

            string attackerName = attacker.GetType().Name;

            return config.groups.Exists(group => group.IsExclusion(attacker.ShortPrefabName) || group.IsExclusion(attackerName));
        }

        private RuleSet GetRuleSet(List<string> vicLocations, List<string> atkLocations)
        {
            RuleSet ruleSet = currentRuleSet;

            if (atkLocations == null) atkLocations = vicLocations; // Allow TruePVE to be used on PVP servers that want to add PVE zones via Zone Manager (just do this inside of Zone Manager instead...)

            if (!vicLocations.IsNullOrEmpty() && !atkLocations.IsNullOrEmpty())
            {
                if (trace) Trace($"Beginning RuleSet lookup for [{ (vicLocations.Count == 0 ? "empty" : string.Join(", ", vicLocations.ToArray())) }] and [{ (atkLocations.Count == 0 ? "empty" : string.Join(", ", atkLocations.ToArray())) }]", 2);

                var locations = GetSharedLocations(vicLocations, atkLocations);

                if (trace) Trace($"Shared locations: { (locations.Count == 0 ? "none" : string.Join(", ", locations.ToArray())) }", 3);

                if (locations?.Count > 0)
                {
                    var names = locations.Select(s => config.mappings[s]).ToList();
                    var sets = config.ruleSets.Where(r => names.Contains(r.name)).ToList();

                    if (trace) Trace($"Found {names.Count} location names, with {sets.Count} mapped RuleSets", 3);

                    if (sets.Count == 0 && config.mappings.ContainsKey(AllZones) && config.ruleSets.Exists(r => r.name == config.mappings[AllZones]))
                    {
                        sets.Add(config.ruleSets.FirstOrDefault(r => r.name == config.mappings[AllZones]));
                        if (trace) Trace($"Found allzones mapped RuleSet", 3);
                    }

                    if (sets.Count > 1)
                    {
                        if (trace) Trace($"WARNING: Found multiple RuleSets: {string.Join(", ", sets.Select(s => s.name))}", 3);
                        PrintWarning(GetMessage("Warning_MultipleRuleSets"), string.Join(", ", sets.Select(s => s.name)));
                    }

                    ruleSet = sets.FirstOrDefault();
                    if (trace) Trace($"Found RuleSet: {ruleSet?.name ?? "null"}", 3);
                }
            }

            if (ruleSet == null)
            {
                ruleSet = currentRuleSet;
                if (trace) Trace($"No RuleSet found; assigned current global RuleSet: {ruleSet?.name ?? "null"}", 3);
            }

            return ruleSet;
        }

        private RuleSet GetRuleSet(BaseEntity e0, BaseEntity e1)
        {
            List<string> e0Locations = GetLocationKeys(e0);
            List<string> e1Locations = GetLocationKeys(e1);
            return GetRuleSet(e0Locations, e1Locations);
        }

        // get locations shared between the two passed location lists
        private List<string> GetSharedLocations(List<string> e0Locations, List<string> e1Locations)
        {
            //return System.Linq.Enumerable.Intersect(e0Locations, e1Locations).Where(s => config.HasMapping(s)).ToList();
            return e0Locations.Intersect(e1Locations).Where(s => config.HasMapping(s)).ToList();
        }

        // Check exclusion for given entity locations
        private bool CheckExclusion(List<string> e0Locations, List<string> e1Locations, bool trace)
        {
            if (e0Locations == null || e1Locations == null)
            {
                if (trace) Trace("No shared locations (empty location) - no exclusions", 3);
                return false;
            }
            if (trace) Trace($"Checking exclusions between [{ (e0Locations.Count == 0 ? "empty" : string.Join(", ", e0Locations.ToArray())) }] and [{ (e1Locations.Count == 0 ? "empty" : string.Join(", ", e1Locations.ToArray())) }]", 2);
            List<string> locations = GetSharedLocations(e0Locations, e1Locations);
            if (trace) Trace($"Shared locations: {(locations.Count == 0 ? "none" : string.Join(", ", locations.ToArray()))}", 3);
            if (!locations.IsNullOrEmpty())
            {
                foreach (string loc in locations)
                {
                    if (config.HasEmptyMapping(loc))
                    {
                        if (trace) Trace($"Found exclusion mapping for location: {loc}", 3);
                        return true;
                    }
                }
            }
            if (trace) Trace("No shared locations, or no matching exclusion mapping - no exclusions", 3);
            return false;
        }

        // add or update a mapping
        private bool AddOrUpdateMapping(string key, string ruleset)
        {
            if (string.IsNullOrEmpty(key) || config == null || ruleset == null || (ruleset != "exclude" && !config.ruleSets.Exists(r => r.name == ruleset)))
                return false;

            config.mappings[key] = ruleset;
            SaveConfig();

            return true;
        }

        // remove a mapping
        private bool RemoveMapping(string key)
        {
            if (config.mappings.Remove(key))
            {
                SaveConfig();
                return true;
            }
            return false;
        }
        #endregion

        #region Messaging
        private void Message(BasePlayer player, string key, params object[] args) => SendReply(player, BuildMessage(player, key, args));

        private void Message(IPlayer user, string key, params object[] args) => user.Reply(RemoveFormatting(BuildMessage(user.Object as BasePlayer, key, args)));

        // build message string
        private string BuildMessage(BasePlayer player, string key, params object[] args)
        {
            string message = GetMessage(key, player?.UserIDString);
            if (args.Length > 0) message = string.Format(message, args);
            string type = key.Split('_')[0];
            if (player != null)
            {
                string size = GetMessage("Format_" + type + "Size");
                string color = GetMessage("Format_" + type + "Color");
                return WrapSize(size, WrapColor(color, message));
            }
            else
            {
                string color = GetMessage("Format_" + type + "Color");
                return WrapColor(color, message);
            }
        }

        // prints the value of an Option
        private void PrintValue(ConsoleSystem.Arg arg, string text, bool value)
        {
            SendReply(arg, WrapSize(GetMessage("Format_NotifySize"), WrapColor(GetMessage("Format_NotifyColor"), text + ": ") + value));
        }

        // wrap string in <size> tag, handles parsing size string to integer
        private string WrapSize(string size, string input)
        {
            int i;
            if (int.TryParse(size, out i))
                return WrapSize(i, input);
            return input;
        }

        // wrap a string in a <size> tag with the passed size
        private string WrapSize(int size, string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return "<size=" + size + ">" + input + "</size>";
        }

        // wrap a string in a <color> tag with the passed color
        private string WrapColor(string color, string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(color))
                return input;
            return "<color=" + color + ">" + input + "</color>";
        }

        // show usage information
        private void ShowUsage(IPlayer user) => user.Reply(RemoveFormatting(usageString));

        public string RemoveFormatting(string source) => source.Contains(">") ? Regex.Replace(source, "<.*?>", string.Empty) : source;

        // warn that the server is set to PVE mode
        private void WarnPve() => PrintWarning(GetMessage("Warning_PveMode"));
        #endregion

        #region Helper Procedures

        // is player a HumanNPC
        private bool IsHumanNPC(BasePlayer attacker, BasePlayer victim)
        {
            if (attacker.name.Contains("ZombieNPC") || victim.name.Contains("ZombieNPC")) return true;
            
            return attacker.IsNpc || !attacker.userID.IsSteamId() || victim.IsNpc || !victim.userID.IsSteamId();
        }

        // get location keys from ZoneManager (zone IDs) or LiteZones (zone names)
        private List<string> GetLocationKeys(BaseEntity entity)
        {
            if (!useZones || entity.IsNull()) return null;
            List<string> locations = new List<string>();
            string zname;
            if (ZoneManager.CanCall())
            {
                List<string> zmloc = new List<string>();
                if (ZoneManager.Version >= new VersionNumber(3, 0, 1))
                {
                    if (entity is BasePlayer)
                    {
                        // BasePlayer fix from chadomat
                        string[] zmlocplr = (string[])ZoneManager.Call("GetPlayerZoneIDs", new object[] { entity as BasePlayer });
                        foreach (string s in zmlocplr)
                        {
                            zmloc.Add(s);
                        }
                    }
                    else if (entity.IsValid())
                    {
                        string[] zmlocent = (string[])ZoneManager.Call("GetEntityZoneIDs", new object[] { entity });
                        foreach (string s in zmlocent)
                        {
                            zmloc.Add(s);
                        }
                    }
                }
                else if (ZoneManager.Version < new VersionNumber(3, 0, 0))
                {
                    if (entity is BasePlayer)
                    {
                        string[] zmlocplr = (string[])ZoneManager.Call("GetPlayerZoneIDs", new object[] { entity as BasePlayer });
                        foreach (string s in zmlocplr)
                        {
                            zmloc.Add(s);
                        }
                    }
                    else if (entity.IsValid())
                    {
                        zmloc = (List<string>)ZoneManager.Call("GetEntityZones", new object[] { entity });
                    }
                }
                else // Skip ZM version 3.0.0
                {
                    zmloc = null;
                }

                if (zmloc != null && zmloc.Count > 0)
                {
                    // Add names into list of ID numbers
                    foreach (string s in zmloc)
                    {
                        locations.Add(s);
                        zname = (string)ZoneManager.Call("GetZoneName", s);
                        if (zname != null) locations.Add(zname);
                        /*if (trace)
                        {
                            string message = $"Found zone {zname}: {s}";
                            if (!_foundMessages.Contains(message))
                            {
                                _foundMessages.Add(message);
                                Puts(message);
                                timer.Once(1f, () => _foundMessages.Remove(message));
                            }
                        }*/
                    }
                }
            }
            if (LiteZones.CanCall())
            {
                List<string> lzloc = (List<string>)LiteZones?.Call("GetEntityZones", new object[] { entity });
                if (lzloc != null && lzloc.Count > 0)
                {
                    locations.AddRange(lzloc);
                }
            }
            return locations;
        }
        private List<string> _foundMessages = new List<string>();

        // handle raycast from player (for prodding)
        private bool GetRaycastTarget(BasePlayer player, out object closestEntity)
        {
            closestEntity = false;

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f))
            {
                closestEntity = hit.GetEntity();
                return true;
            }
            return false;
        }

        // loop to update current ruleset
        private void TimerLoop(bool firstRun = false)
        {
            string ruleSetName;
            config.schedule.ClockUpdate(out ruleSetName, out currentBroadcastMessage);
            if (currentRuleSet.name != ruleSetName || firstRun)
            {
                currentRuleSet = config.ruleSets.FirstOrDefault(r => r.name == ruleSetName);
                if (currentRuleSet == null)
                    currentRuleSet = new RuleSet(ruleSetName); // create empty ruleset to hold name
                if (config.schedule.broadcast && currentBroadcastMessage != null)
                {
                    Server.Broadcast(currentBroadcastMessage, GetMessage("Prefix"));
                    Puts(RemoveFormatting(GetMessage("Prefix") + " Schedule Broadcast: " + currentBroadcastMessage));
                }
            }

            if (config.schedule.enabled)
                scheduleUpdateTimer = timer.Once(config.schedule.useRealtime ? 30f : 3f, () => TimerLoop());
        }

        #endregion

        #region Subclasses
        // configuration and data storage container

        private class ConfigurationOptions
        {
            [JsonProperty(PropertyName = "handleDamage")] // (true) enable TruePVE damage handling hooks
            public bool handleDamage { get; set; } = true;

            [JsonProperty(PropertyName = "useZones")] // (true) use ZoneManager/LiteZones for zone-specific damage behavior (requires modification of ZoneManager.cs)
            public bool useZones { get; set; } = true;

            [JsonProperty(PropertyName = "Trace To Player Console")]
            public bool PlayerConsole { get; set; }

            [JsonProperty(PropertyName = "Trace To Server Console")]
            public bool ServerConsole { get; set; }

            [JsonProperty(PropertyName = "Maximum Distance From Player To Trace")]
            public float MaxTraceDistance { get; set; }

            [JsonProperty(PropertyName = "Prevent Water From Extinguishing BaseOven")]
            public bool disableBaseOvenSplash { get; set; }

            [JsonProperty(PropertyName = "Prevent Players From Being Marked Hostile")]
            public bool disableHostility { get; set; }
        }

        private class Configuration
        {
            [JsonProperty(PropertyName = "Config Version")]
            public string configVersion = null;
            [JsonProperty(PropertyName = "Default RuleSet")]
            public string defaultRuleSet = "default";
            [JsonProperty(PropertyName = "Configuration Options")]
            public ConfigurationOptions options = new ConfigurationOptions();
            [JsonProperty(PropertyName = "Mappings")]
            public Dictionary<string, string> mappings = new Dictionary<string, string>();
            [JsonProperty(PropertyName = "Schedule")]
            public Schedule schedule = new Schedule();
            [JsonProperty(PropertyName = "RuleSets")]
            public List<RuleSet> ruleSets = new List<RuleSet>();
            [JsonProperty(PropertyName = "Entity Groups")]
            public List<EntityGroup> groups = new List<EntityGroup>(); 
            [JsonProperty(PropertyName = "Allow Killing Sleepers")]
            public bool AllowKillingSleepers;
            [JsonProperty(PropertyName = "Allow Killing Sleepers (Ally Only)")]
            public bool AllowKillingSleepersAlly;
            [JsonProperty(PropertyName = "Ignore Firework Damage")]
            public bool Firework = true;
            Dictionary<uint, List<string>> groupCache = new Dictionary<uint, List<string>>();

            public void Init()
            {
                schedule.Init();
                foreach (RuleSet rs in ruleSets)
                    rs.Build();
                ruleSets.Remove(null);
            }

            public List<string> ResolveEntityGroups(BaseEntity entity)
            {
                if (!entity.IsNull())
                {
                    if (!entity.net.IsNull())
                    {
                        List<string> groupList;
                        if (!groupCache.TryGetValue(entity.net.ID, out groupList))
                        {
                            groupList = groups.Where(g => g.Contains(entity)).Select(g => g.name).ToList();
                            groupCache[entity.net.ID] = groupList;
                        }
                        return groupList;
                    }

                    return groups.Where(g => g.Contains(entity)).Select(g => g.name).ToList();
                }

                return null;
            }

            public bool HasMapping(string key)
            {
                return mappings.ContainsKey(key) || mappings.ContainsKey(AllZones);
            }

            public bool HasEmptyMapping(string key)
            {
                if (mappings.ContainsKey(AllZones) && mappings[AllZones].Equals("exclude")) return true; // exlude all zones
                if (!mappings.ContainsKey(key)) return false;
                if (mappings[key].Equals("exclude")) return true;
                RuleSet r = ruleSets.FirstOrDefault(rs => rs.name.Equals(mappings[key]));
                if (r == null) return true;
                return r.IsEmpty();
            }

            public RuleSet GetDefaultRuleSet()
            {
                try
                {
                    return ruleSets.Single(r => r.name == defaultRuleSet);
                }
                catch (Exception)
                {
                    Interface.Oxide.LogWarning($"Warning - duplicate ruleset found for default RuleSet: '{defaultRuleSet}'");
                    return ruleSets.FirstOrDefault(r => r.name == defaultRuleSet);
                }
            }
        }

        private class RuleSet
        {
            public string name;
            public bool enabled = true;
            public bool defaultAllowDamage = false;
            public string flags = string.Empty;
            [JsonIgnore]
            public long _flags = RuleFlags.None;
            [JsonIgnore]
            public bool Changed;

            public HashSet<string> rules = new HashSet<string>();
            HashSet<Rule> parsedRules = new HashSet<Rule>();

            public RuleSet() { }
            public RuleSet(string name) { this.name = name; }

            // evaluate the passed lists of entity groups against rules
            public bool Evaluate(List<string> eg1, List<string> eg2, BaseEntity attacker, bool returnDefaultValue = true)
            {
                if (Instance.trace) Instance.Trace("Evaluating Rules...", 3);
                if (parsedRules.IsNullOrEmpty())
                {
                    if (Instance.trace) Instance.Trace($"No rules found; returning default value: {defaultAllowDamage}", 4);
                    return defaultAllowDamage;
                }
                bool? res;
                if (Instance.trace) Instance.Trace("Checking direct initiator->target rules...", 4);
                // check all direct links
                bool resValue = defaultAllowDamage;
                bool resFound = false;

                if (eg1 != null && eg1.Count > 0 && eg2 != null && eg2.Count > 0)
                {
                    foreach (string s1 in eg1)
                    {
                        foreach (string s2 in eg2)
                        {
                            if ((res = Evaluate(s1, s2)).HasValue)
                            {
                                resValue = res.Value;
                                resFound = true;
                                break;
                            }
                        }
                    }
                }

                if (!resFound && eg1 != null && eg1.Count > 0)
                {
                    if (Instance.trace) Instance.Trace("No direct match rules found; continuing...", 4);

                    foreach (string s1 in eg1)
                    {// check group -> any
                        if ((res = Evaluate(s1, Any)).HasValue)
                        {
                            resValue = res.Value;
                            resFound = true;
                            break;
                        }
                    }
                }

                if (!resFound && eg2 != null && eg2.Count > 0)
                {
                    if (Instance.trace) Instance.Trace("No matching initiator->any rules found; continuing...", 4);

                    foreach (string s2 in eg2)
                    {// check any -> group
                        if ((res = Evaluate(Any, s2)).HasValue)
                        {
                            resValue = res.Value;
                            resFound = true;
                            break;
                        }
                    }
                }

                if (resFound)
                {
                    /*if (attacker.IsValid() && Instance.data.groups.Any(group => group.IsExclusion(attacker.GetType().Name) || group.IsExclusion(attacker.ShortPrefabName)))
                    {
                        if (Instance.trace) Instance.Trace($"Exclusion found; allow damage? {!resValue}", 6);
                        return !resValue;
                    }*/

                    return resValue;
                }

                if (returnDefaultValue)
                {
                    if (Instance.trace) Instance.Trace($"No matching any->target rules found; returning default value: {defaultAllowDamage}", 4);
                    return defaultAllowDamage;
                }

                return true;
            }

            // evaluate two entity groups against rules
            public bool? Evaluate(string eg1, string eg2)
            {
                if (eg1 == null || eg2 == null || parsedRules.IsNullOrEmpty()) return null;
                if (Instance.trace) Instance.Trace($"Evaluating \"{eg1}->{eg2}\"...", 5);
                Rule rule = parsedRules.FirstOrDefault(r => r.valid && r.key.Equals(eg1 + "->" + eg2));
                if (rule != null)
                {
                    if (Instance.trace) Instance.Trace($"Match found; allow damage? {rule.hurt}", 6);
                    return rule.hurt;
                }
                if (Instance.trace) Instance.Trace($"No match found", 6);
                return null;
            }

            // build rule strings to rules
            public void Build()
            {
                foreach (string ruleText in rules)
                    parsedRules.Add(new Rule(ruleText));
                parsedRules.Remove(null);
                ValidateRules();
                if (flags.Length == 0)
                {
                    _flags |= RuleFlags.None;
                    return;
                }
                foreach (string _value in flags.Split(','))
                {
                    string value = _value.Trim();                    
                    long flag = RuleFlags.Get(value);
                    if (flag == RuleFlags.None)
                    {
                        if (value == "SamSitesIgnorePlayers")
                        {
                            ConvertSamSiteFlag();
                        }
                        else if (value == "TrapsIgnoreScientists")
                        {
                            ConvertTrapsIgnoreScientists();
                        }
                        else if (value == "TurretsIgnoreScientists")
                        {
                            ConvertTurretsIgnoreScientists();
                        }
                        else
                        {
                            Instance.Puts("WARNING - invalid flag: '{0}' (does this flag still exist?)", value);
                        }
                    }
                    else if (!HasFlag(flag))
                    {
                        _flags |= flag;
                    }
                }
                if (Changed)
                {
                    Instance.SaveConfig();
                    Changed = false;
                }
            }

            private void ConvertSamSiteFlag()
            {
                flags = flags.Replace("SamSitesIgnorePlayers", "PlayerSamSitesIgnorePlayers, StaticSamSitesIgnorePlayers");                
                if (!HasFlag(RuleFlags.PlayerSamSitesIgnorePlayers))
                {
                    _flags |= RuleFlags.PlayerSamSitesIgnorePlayers;
                }
                if (!HasFlag(RuleFlags.StaticSamSitesIgnorePlayers))
                {
                    _flags |= RuleFlags.StaticSamSitesIgnorePlayers;
                }
                Changed = true;
            }

            private void ConvertTrapsIgnoreScientists()
            {
                flags = flags.Replace("TrapsIgnoreScientists", "TrapsIgnoreScientist");
                if (!HasFlag(RuleFlags.TrapsIgnoreScientist))
                {
                    _flags |= RuleFlags.TrapsIgnoreScientist;
                }
                Changed = true;
            }

            private void ConvertTurretsIgnoreScientists()
            {
                flags = flags.Replace("TurretsIgnoreScientists", "TurretsIgnoreScientist");
                if (!HasFlag(RuleFlags.TurretsIgnoreScientist))
                {
                    _flags |= RuleFlags.TurretsIgnoreScientist;
                }
                Changed = true;
            }

            public void ValidateRules()
            {
                foreach (Rule rule in parsedRules)
                    if (!rule.valid)
                        Interface.Oxide.LogWarning($"Warning - invalid rule: {rule.ruleText}");
            }

            // add a rule
            public void AddRule(string ruleText)
            {
                rules.Add(ruleText);
                parsedRules.Add(new Rule(ruleText));
            }

            public bool HasAnyFlag(long flags) { return (_flags | flags) != RuleFlags.None; }
            public bool HasFlag(long flag) { return (_flags & flag) == flag; }
            public bool IsEmpty() { return rules.IsNullOrEmpty() && _flags == RuleFlags.None; }
        }

        private class Rule
        {
            public string ruleText;
            [JsonIgnore]
            public string key;
            [JsonIgnore]
            public bool hurt;
            [JsonIgnore]
            public bool valid;

            public Rule() { }
            public Rule(string ruleText)
            {
                this.ruleText = ruleText;
                valid = RuleTranslator.Translate(this);
            }

            public override int GetHashCode() { return key.GetHashCode(); }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (obj == this) return true;
                if (obj is Rule)
                    return key.Equals((obj as Rule).key);
                return false;
            }
        }

        // helper class to translate rule text to rules
        private class RuleTranslator
        {
            static readonly Regex regex = new Regex(@"\s+");
            static readonly List<string> synonyms = new List<string>() { "anything", "nothing", "all", "any", "none", "everything" };
            public static bool Translate(Rule rule)
            {
                if (string.IsNullOrEmpty(rule.ruleText)) return false;
                string str = rule.ruleText;
                string[] splitStr = regex.Split(str);
                // first and last words should be ruleset names
                string rs0 = splitStr[0];
                string rs1 = splitStr[splitStr.Length - 1];
                string[] mid = splitStr.Skip(1).Take(splitStr.Length - 2).ToArray();
                if (mid == null || mid.Length == 0) return false;

                bool canHurt = true;
                foreach (string s in mid)
                    if (s.Equals("cannot") || s.Equals("can't"))
                        canHurt = false;

                // rs0 and rs1 shouldn't ever be "nothing" simultaneously
                if (rs0.Equals("nothing") || rs1.Equals("nothing") || rs0.Equals("none") || rs1.Equals("none")) canHurt = !canHurt;

                if (synonyms.Contains(rs0)) rs0 = Any;
                if (synonyms.Contains(rs1)) rs1 = Any;

                rule.key = rs0 + "->" + rs1;
                rule.hurt = canHurt;
                return true;
            }
        }

        // container for mapping entities
        private class EntityGroup
        {
            private List<string> memberList { get; set; } = new List<string>();
            private List<string> exclusionList { get; set; } = new List<string>();
            public string name { get; set; }

            public string members
            {
                get
                {
                    if (memberList.Count == 0) return string.Empty;
                    return string.Join(", ", memberList.ToArray());
                }
                set
                {
                    if (string.IsNullOrEmpty(value)) return;
                    memberList = value.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            public string exclusions
            {
                get
                {
                    if (exclusionList.Count == 0) return string.Empty;
                    return string.Join(", ", exclusionList.ToArray());
                }
                set
                {
                    if (string.IsNullOrEmpty(value)) return;
                    exclusionList = value.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            public EntityGroup()
            {

            }

            public EntityGroup(string name)
            {
                this.name = name;
            }

            public bool IsMember(string value)
            {
                foreach (var member in memberList)
                {
                    if (member.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsExclusion(string value)
            {
                foreach (var exclusion in exclusionList)
                {
                    if (exclusion.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool Contains(BaseEntity entity)
            {
                if (entity.IsNull()) return false;
                return (memberList.Contains(entity.GetType().Name) || memberList.Contains(entity.ShortPrefabName)) && !(exclusionList.Contains(entity.GetType().Name) || exclusionList.Contains(entity.ShortPrefabName));
            }
        }

        // scheduler
        private class Schedule
        {
            public bool enabled = false;
            public bool useRealtime = false;
            public bool broadcast = false;
            public List<string> entries = new List<string>();
            List<ScheduleEntry> parsedEntries = new List<ScheduleEntry>();
            [JsonIgnore]
            public bool valid = false;

            public void Init()
            {
                foreach (string str in entries)
                    parsedEntries.Add(new ScheduleEntry(str));
                // schedule not valid if entries are empty, there are less than 2 entries, or there are less than 2 rulesets defined
                if (parsedEntries.IsNullOrEmpty() || parsedEntries.Sum(e => e.valid ? 1 : 0) < 2 || parsedEntries.Select(e => e.ruleSet).Distinct().Count < 2)
                    enabled = false;
                else
                    valid = true;
            }

            // returns delta between current time and next schedule entry
            public void ClockUpdate(out string ruleSetName, out string message)
            {
                TimeSpan time = useRealtime ? new TimeSpan((int)DateTime.Now.DayOfWeek, 0, 0, 0).Add(DateTime.Now.TimeOfDay) : TOD_Sky.Instance.Cycle.DateTime.TimeOfDay;
                try
                {
                    ScheduleEntry se = null;
                    // get the most recent schedule entry
                    if (parsedEntries.Where(t => !t.isDaily).Count > 0)
                        se = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && t.time <= time && ((useRealtime && !t.isDaily) || !useRealtime)).Max(t => t.time));
                    // if realtime, check for daily
                    if (useRealtime)
                    {
                        ScheduleEntry daily = null;
                        try
                        {
                            daily = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && t.time <= DateTime.Now.TimeOfDay && t.isDaily).Max(t => t.time));
                        }
                        catch (Exception)
                        { // no daily entries
                        }
                        if (daily != null && se == null)
                            se = daily;
                        if (daily != null && daily.time.Add(new TimeSpan((int)DateTime.Now.DayOfWeek, 0, 0, 0)) > se.time)
                            se = daily;
                    }
                    ruleSetName = se.ruleSet;
                    message = se.message;
                }
                catch (Exception)
                {
                    ScheduleEntry se = null;
                    // if time is earlier than all schedule entries, use max time
                    if (parsedEntries.Where(t => !t.isDaily).Count > 0)
                        se = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && ((useRealtime && !t.isDaily) || !useRealtime)).Max(t => t.time));
                    if (useRealtime)
                    {
                        ScheduleEntry daily = null;
                        try
                        {
                            daily = parsedEntries.FirstOrDefault(e => e.time == parsedEntries.Where(t => t.valid && t.isDaily).Max(t => t.time));
                        }
                        catch (Exception)
                        { // no daily entries
                        }
                        if (daily != null && se == null)
                            se = daily;
                        if (daily != null && daily.time.Add(new TimeSpan((int)DateTime.Now.DayOfWeek, 0, 0, 0)) > se.time)
                            se = daily;
                    }
                    ruleSetName = se?.ruleSet;
                    message = se?.message;
                }
            }
        }

        // helper class to translate schedule text to schedule entries
        private class ScheduleTranslator
        {
            static readonly Regex regex = new Regex(@"\s+");
            public static bool Translate(ScheduleEntry entry)
            {
                if (string.IsNullOrEmpty(entry.scheduleText)) return false;
                string str = entry.scheduleText;
                string[] splitStr = regex.Split(str, 3); // split into 3 parts
                // first word should be a timespan
                string ts = splitStr[0];
                // second word should be a ruleset name
                string rs = splitStr[1];
                // remaining should be message
                string message = splitStr.Length > 2 ? splitStr[2] : null;

                try
                {
                    if (ts.StartsWith("*."))
                    {
                        entry.isDaily = true;
                        ts = ts.Substring(2);
                    }
                    entry.time = TimeSpan.Parse(ts);
                    entry.ruleSet = rs;
                    entry.message = message;
                    return true;
                }
                catch
                { }

                return false;
            }
        }

        private class ScheduleEntry
        {
            public string ruleSet;
            public string message;
            public string scheduleText;
            public bool valid;
            public TimeSpan time { get; set; }
            [JsonIgnore]
            public bool isDaily = false;

            public ScheduleEntry() { }
            public ScheduleEntry(string scheduleText)
            {
                this.scheduleText = scheduleText;
                valid = ScheduleTranslator.Translate(this);
            }
        }
        #endregion

        #region Lang
        // load default messages to Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix", "<color=#FFA500>[ TruePVE ]</color>" },
                {"Enable", "TruePVE enable set to {0}" },

                {"Header_Usage", "---- TruePVE usage ----"},
                {"Cmd_Usage_def", "Loads default configuration and data"},
                {"Cmd_Usage_sched", "Enable or disable the schedule" },
                {"Cmd_Usage_prod", "Show the prefab name and type of the entity being looked at"},
                {"Cmd_Usage_map", "Create/remove a mapping entry" },
                {"Cmd_Usage_trace", "Toggle tracing on/off" },

                {"Warning_PveMode", "ConVar server.pve is TRUE!  TruePVE is designed for PVP mode, and may cause unexpected behavior in PVE mode."},
                {"Warning_NoRuleSet", "No RuleSet found for \"{0}\"" },
                {"Warning_DuplicateRuleSet", "Multiple RuleSets found for \"{0}\"" },

                {"Error_InvalidCommand", "Invalid command" },
                {"Error_InvalidParameter", "Invalid parameter: {0}"},
                {"Error_InvalidParamForCmd", "Invalid parameters for command \"{0}\""},
                {"Error_InvalidMapping", "Invalid mapping: {0} => {1}; Target must be a valid RuleSet or \"exclude\"" },
                {"Error_NoMappingToDelete", "Cannot delete mapping: \"{0}\" does not exist" },
                {"Error_NoPermission", "Cannot execute command: No permission"},
                {"Error_NoSuicide", "You are not allowed to commit suicide"},
                {"Error_NoEntityFound", "No entity found"},

                {"Notify_AvailOptions", "Available Options: {0}"},
                {"Notify_DefConfigLoad", "Loaded default configuration"},
                {"Notify_DefDataLoad", "Loaded default mapping data"},
                {"Notify_ProdResult", "Prod results: type={0}, prefab={1}"},
                {"Notify_SchedSetEnabled", "Schedule enabled" },
                {"Notify_SchedSetDisabled", "Schedule disabled" },
                {"Notify_InvalidSchedule", "Schedule is not valid" },
                {"Notify_MappingCreated", "Mapping created for \"{0}\" => \"{1}\"" },
                {"Notify_MappingUpdated", "Mapping for \"{0}\" changed from \"{1}\" to \"{2}\"" },
                {"Notify_MappingDeleted", "Mapping for \"{0}\" => \"{1}\" deleted" },
                {"Notify_TraceToggle", "Trace mode toggled {0}" },

                {"Format_EnableColor", "#00FFFF"}, // cyan
                {"Format_EnableSize", "12"},
                {"Format_NotifyColor", "#00FFFF"}, // cyan
                {"Format_NotifySize", "12"},
                {"Format_HeaderColor", "#FFA500"}, // orange
                {"Format_HeaderSize", "14"},
                {"Format_ErrorColor", "#FF0000"}, // red
                {"Format_ErrorSize", "12"},
            }, this);
        }

        // get message from Lang
        private string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
        #endregion
    }
}


namespace Oxide.Plugins.TruePVEExtensionMethods
{
    public static class ExtensionMethods
    {
        public static List<T> Distinct<T>(this IEnumerable<T> a) { var b = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (!b.Contains(c.Current)) { b.Add(c.Current); } } } return b; }
        public static bool Exists<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return true; } } } return false; }
        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> a, IEnumerable<T> b) { var d = new List<T>(); foreach (T item in b) { d.Add(item); } foreach (T e in a) { if (d.Remove(e)) { yield return e; } } }
        public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return c.Current; } } } return default(T); }
        public static T Single<T>(this IEnumerable<T> a, Func<T, bool> b) { var d = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b(c.Current)) { d.Add(c.Current); } } } if (d.Count > 1) throw new InvalidOperationException("single"); return d[0]; }
        public static IEnumerable<V> Select<T, V>(this IEnumerable<T> a, Func<T, V> b) { var c = new List<V>(); if (a == null) return c; using (var d = a.GetEnumerator()) { while (d.MoveNext()) { c.Add(b(d.Current)); } } return c; }
        public static string[] Skip(this string[] a, int b) { if (a.Length == 0) { return Array.Empty<string>(); } string[] c = new string[a.Length - b]; int n = 0; for (int i = 0; i < a.Length; i++) { if (i < b) continue; c[n] = a[i]; n++; } return c; }
        public static List<T> Take<T>(this IList<T> a, int b) { var c = new List<T>(); for (int i = 0; i < a.Count; i++) { if (c.Count == b) { break; } c.Add(a[i]); } return c; }
        public static List<T> ToList<T>(this IEnumerable<T> a) { var b = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { b.Add(c.Current); } } return b; }
        public static List<T> Where<T>(this IEnumerable<T> a, Func<T, bool> b) { var c = new List<T>(); using (var d = a.GetEnumerator()) { while (d.MoveNext()) { if (b(d.Current)) { c.Add(d.Current); } } } return c; }
        public static List<T> OfType<T>(this IEnumerable<BaseNetworkable> a) where T : BaseEntity { var b = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (c.Current is T) { b.Add(c.Current as T); } } } return b; }
        public static R Max<T, R>(this IList<T> a, Func<T, R> b) { R c = default(R); Comparer<R> @default = Comparer<R>.Default; for (int i = 0; i < a.Count; i++) { var d = b(a[i]); if (@default.Compare(d, c) > 0) { c = d; } } return c; }
        public static int Sum<T>(this IList<T> a, Func<T, int> b) { int c = 0; for (int i = 0; i < a.Count; i++) { var d = b(a[i]); if (!float.IsNaN(d)) { c += d; } } return c; }
        public static bool IsReallyConnected(this BasePlayer a) { return (object)a != null && (object)a.net != null && (object)a.net.connection != null; }
        public static bool IsNull<T>(this T a) where T : class { return (object)a == null; }
        public static bool CanCall(this Plugin a) { return a != null && a.IsLoaded; }
    }
}