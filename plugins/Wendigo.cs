using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine; 

namespace Oxide.Plugins
{
	[Info("Wendigo", "obsol", "0.0.1")]
	[Description("Variant of Cannibal by BuzZ[PHOQUE], removes poison from human meat, with a chance to be made a cannibal when eaten. Cannibals cannot eat plants, can eat meat at a reduced benefit, and have nightvision. Depends on NightVision by Clearshot")]

/*======================================================================================================================= 
*
    OnItemUse
		if cannibal && cannibaltime > 24
			FlipWendigo
		If human meat
			if not cannibal
				Remove poison, see if the rest still works
				if roll random
					FlipWendigo
			if cannibal
				get huge hunger boost
		If not human meat
			if cannibal
				if "meat", get some hunger and thirst, no healing
				else, poison
				
	FlipWendigo
		ToggleMyCannibalState(BasePlayer player, string command, string[] args)
		private void NightVisionCommand(IPlayer player, "nv", string[] args)
	

*=======================================================================================================================*/


	public class Wendigo : RustPlugin
	{
		[PluginReference]
        Plugin NightVision;
        class StoredData
        {
            public Dictionary<ulong, string> Wendis = new Dictionary<ulong, string>();
            public Dictionary<ulong, int> WendiTime = new Dictionary<ulong, int>();
            public StoredData()
            {
            }
        }

		int defaultHealth = 100;
		int defaultCals = 100;
		int defaultWater = 100;
		int wendiHealth = 150;
		int wendiCals = 50;
		int wendiWater = 200;
		int baseTime=8;
		
        private bool ConfigChanged;
        const string wendigo = "wendigo.player"; 
        int experiencetime = 10;    
        private StoredData storedData;
		private EnvSync _envSync;
        private DateTime _sunnyDayDate = new DateTime(2024, 1, 25);
		private void OnServerInitialized()
        {
            _envSync = BaseNetworkable.serverEntities.OfType<EnvSync>().FirstOrDefault();

            timer.Every(5f, () => {
                if (!_envSync.limitNetworking)
                    _envSync.limitNetworking = true;

                List<Connection> subscribers = _envSync.net.group.subscribers;
                if (subscribers != null && subscribers.Count > 0)
                {
                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        Connection connection = subscribers[i];
                        global::BasePlayer basePlayer = connection.player as global::BasePlayer;

                        if (!(basePlayer == null)) {
                            //NVPlayerData nvPlayerData = GetNVPlayerData(basePlayer);
                            if (storedData.Wendis.ContainsKey(basePlayer.userID) == false) continue;

                            if (Net.sv.write.Start())
                            {
                                connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
                                BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
                                {
                                    forConnection = connection,
                                    forDisk = false
                                };
                                Net.sv.write.PacketID(Message.Type.Entities);
                                Net.sv.write.UInt32(connection.validate.entityUpdates);
                                using (saveInfo.msg = Facepunch.Pool.Get<ProtoBuf.Entity>())
                                {
                                    _envSync.Save(saveInfo);
									saveInfo.msg.environment.dateTime = _sunnyDayDate.AddHours(storedData.WendiTime[basePlayer.userID]).ToBinary();
									saveInfo.msg.environment.fog = 0;
									saveInfo.msg.environment.rain = 0;
									saveInfo.msg.environment.clouds = 0;
                                    if (saveInfo.msg.baseEntity == null)
                                    {
                                        //LogError(this + ": ToStream - no BaseEntity!?");
                                    }
                                    if (saveInfo.msg.baseNetworkable == null)
                                    {
                                        //LogError(this + ": ToStream - no baseNetworkable!?");
                                    }
                                    saveInfo.msg.ToProto(Net.sv.write);
                                    _envSync.PostSave(saveInfo);
                                    Net.sv.write.Send(new SendInfo(connection));
                                }
                            }
                        }
                    }
                }
            });
        }
        public List<string> NotForWendigo = new List<string>()
        {

            "Apple","berries", "Cactus","Beans","Candy",
            "Chocholate","Corn","Granola","Hemp","Mushroom","Pickle","Pumpkin"

        };
        void Init()
        {
            LoadVariables();
            permission.RegisterPermission(wendigo, this); 
        }

        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("WendisOnServer");
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("WendisOnServer", storedData);
        }
		
		void LoadDefaultMessages()
		{
		}

		protected override void LoadDefaultConfig()
        {
            LoadVariables();
        }


        private void LoadVariables()
        {
            experiencetime = Convert.ToInt32(GetConfig("Minimum time to play as cannibal, before being able to toggle it off", "in real time minutes", "1440"));                // WINNER NAME COLOR

            if (!ConfigChanged) return;
            SaveConfig();
            ConfigChanged = false;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                ConfigChanged = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                ConfigChanged = true;
            }
            return value;
        }

        void WhenAWendigoEatsHuman(BasePlayer Eater)
        {
            Eater.metabolism.poison.value = 0;
            Eater.metabolism.bleeding.value = 0;
            Eater.metabolism.calories.value = Eater.metabolism.calories.value + 50;
            Eater.health = Eater.health + 10;
			Eater._maxHealth += 1;						
            Eater.metabolism.hydration.value = Eater.metabolism.hydration.value + 50;
			Eater.metabolism.comfort.value += 10;	
			storedData.WendiTime[Eater.userID]= (storedData.WendiTime[Eater.userID] + 1)% 24;
			//NightVision.CallHook("UnlockPlayerTime", player);	
			//Interface.uMod.CallHook("LockPlayerTime", Eater, storedData.WendiTime[Eater.userID], 0, 0);	
        }
        void WhenAHumanEatsHuman(BasePlayer Eater)
        {
			Eater.metabolism.poison.value = Eater.metabolism.poison.value + 2;
			Eater.metabolism.bleeding.value = 0;
			Eater.metabolism.calories.value = Eater.metabolism.calories.value + 15;
			Eater.health = Eater.health + 2;
			Eater.metabolism.hydration.value = Eater.metabolism.hydration.value + 10;
			Eater.metabolism.comfort.value = 0;
			if(Oxide.Core.Random.Range(0, 100)<90){
				setWendigo(Eater,true);
			};
        }
        void WhenAWendigoEatsMeat(BasePlayer Eater, string ItemToEat)
        {
            Eater.metabolism.poison.value = Eater.metabolism.poison.value + 2;
            Eater.metabolism.calories.value = Eater.metabolism.calories.value +10;
            Eater.metabolism.hydration.value = Eater.metabolism.hydration.value +5;
			//NightVision.CallHook("UnlockPlayerTime", player);	
			//Interface.uMod.CallHook("LockPlayerTime", Eater, storedData.WendiTime[Eater.userID], 0, 1);	
        }
        void WhenAWendigoEatsPlant(BasePlayer Eater, string ItemToEat)
        {
            Eater.metabolism.poison.value = Eater.metabolism.poison.value + 5;
            Eater.metabolism.calories.value = Eater.metabolism.calories.value - 40;
            Eater.health = Eater.health - 40;
            Eater.metabolism.hydration.value = Eater.metabolism.hydration.value - 30;
			//NightVision.CallHook("UnlockPlayerTime", player);	
			//Interface.uMod.CallHook("LockPlayerTime", Eater, storedData.WendiTime[Eater.userID], 1, 1);	
        }
        void OnItemUse(Item item, int amountToUse)
        {
			string ItemToEat = item.info.shortname.ToString();
            if (ItemToEat == null){return;}
            ItemContainer Container = item.GetRootContainer();
            if (Container == null){return;}
            BasePlayer Eater = Container.GetOwnerPlayer();
            if (Eater == null){return;}
			//if cannibal && cannibaltime > 24
			//FlipWendigo
			if (storedData.Wendis.ContainsKey(Eater.userID) == true)
			{   
				string since;
                storedData.Wendis.TryGetValue(Eater.userID, out since);
                System.DateTime Since = DateTime.Parse(since); // as System.DateTime;
                TimeSpan fenetre = new TimeSpan();
                fenetre = DateTime.Now - Since;
                int minuten = Convert.ToInt32(fenetre.TotalMinutes);
                if (experiencetime == null){experiencetime = 1440;}
				if(minuten > experiencetime){
					setWendigo(Eater,false);
				}
			}			
            if (ItemToEat.Contains("human") == true)
            {
				//Human on human
				if(!(storedData.Wendis.ContainsKey(Eater.userID))){
					WhenAHumanEatsHuman(Eater);
				}else{
					//Wendigo on human
					WhenAWendigoEatsHuman(Eater);
				}
			}
			else{
				if(storedData.Wendis.ContainsKey(Eater.userID)){
					bool isForbidden = false;
					foreach (string miam in NotForWendigo)
					{	
						//wendigo on plant
						if (ItemToEat.Contains(miam.ToLower()) == true)
						{
							WhenAWendigoEatsPlant(Eater, ItemToEat);
							isForbidden = true;
						}
					}
					if(!isForbidden){
						WhenAWendigoEatsMeat(Eater, ItemToEat);
					}
				}
			}
        }

        private void OnPlayerRespawned(BasePlayer player)
        {			
			if (storedData.Wendis.ContainsKey(player.userID)){
				player._maxHealth = wendiHealth;						
				player.metabolism.hydration.max = wendiWater;
				player.metabolism.calories.max = wendiCals;
			}else{
				player._maxHealth = defaultHealth;						
				player.metabolism.hydration.max = defaultWater;
				player.metabolism.calories.max = defaultCals;
			}            
        }

		void setWendigo(BasePlayer player, bool wending)
		{
			if(wending){
				if (! storedData.Wendis.ContainsKey(player.userID)){
					storedData.Wendis.Add(player.userID, System.DateTime.Now.ToString());
					if (! storedData.WendiTime.ContainsKey(player.userID)){
						storedData.WendiTime.Add(player.userID, baseTime);	
					}
					//NightVision?.CallHook("LockPlayerTime", player, storedData.WendiTime[player.userID], 0, 0);	
					player._maxHealth = wendiHealth;						
					player.metabolism.hydration.max = wendiWater;
					player.metabolism.calories.max = wendiCals;					
                    player.health = System.Math.Min(player.health, wendiHealth);
                    player.metabolism.hydration.value = System.Math.Min(player.metabolism.hydration.value, wendiWater);
                    player.metabolism.calories.value = System.Math.Min(player.metabolism.calories.value, wendiCals);
				}
			}else{				
				if (storedData.Wendis.ContainsKey(player.userID)){
					storedData.Wendis.Remove(player.userID);
					//NightVision?.CallHook("UnlockPlayerTime", player);	
					player._maxHealth = defaultHealth;						
					player.metabolism.hydration.max = defaultWater;
					player.metabolism.calories.max = defaultCals;						
                    player.health = System.Math.Min(player.health, defaultHealth);
                    player.metabolism.hydration.value = System.Math.Min(player.metabolism.hydration.value, defaultWater);
                    player.metabolism.calories.value = System.Math.Min(player.metabolism.calories.value, defaultCals);			
				}
			}
		}
    }
}