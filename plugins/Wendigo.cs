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
            public Dictionary<ulong, Double> WendiTime = new Dictionary<ulong, Double>();
            public StoredData()
            {
            }
        }

		int defaultHealth = 100;
		int defaultCals = 50;
		int maxCals = 100;
		int defaultWater = 100;
		int wendiHealth = 100;
		int wendiCals = 50;
		int wendiMaxCals = 200;
		int wendiWater = 200;
		int baseTime=8;
		
        private bool ConfigChanged;
        const string wendigo = "wendigo.player"; 
        int experiencetime = 10;    
        private StoredData storedData;
		private EnvSync _envSync;
        private DateTime _sunnyDayDate = new DateTime(2024, 1, 25);
		
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
            Eater.metabolism.calories.value += (Eater.metabolism.calories.value+20<=Eater.metabolism.calories.max? 20:0);
			Eater.Heal(20);
			Eater._maxHealth += 1;					
            Eater.metabolism.hydration.value += (Eater.metabolism.hydration.value+50<=Eater.metabolism.hydration.max? 50:30);
			Eater.metabolism.comfort.value += 10;	
			storedData.WendiTime[Eater.userID]= (storedData.WendiTime[Eater.userID] + 0.1)% 24;
			//NightVision.CallHook("UnlockPlayerTime", player);	
			//Interface.uMod.CallHook("LockPlayerTime", Eater, storedData.WendiTime[Eater.userID], 0, 0);	
        }
        void WhenAHumanEatsHuman(BasePlayer Eater)
        {
			Eater.metabolism.poison.value = Eater.metabolism.poison.value + 2;
			Eater.metabolism.bleeding.value = 0;
			Eater.metabolism.calories.value = Eater.metabolism.calories.value + 5;
			Eater.metabolism.hydration.value = Eater.metabolism.hydration.value + 5;
			Eater.metabolism.comfort.value = 0;
			if(Oxide.Core.Random.Range(0, 100)<10){
				setWendigo(Eater,true);
			};
        }
        void WhenAWendigoEatsMeat(BasePlayer Eater, string ItemToEat)
        {
            Eater.metabolism.calories.value = Eater.metabolism.calories.value +5;
            Eater.metabolism.hydration.value = Eater.metabolism.hydration.value +5;
			storedData.WendiTime[Eater.userID]= (storedData.WendiTime[Eater.userID] + 0.5)% 24;
            Eater.metabolism.calories.max = ((Eater.metabolism.calories.max * 1.05f)<100?(Eater.metabolism.calories.max * 1.05f):100);
			//NightVision.CallHook("UnlockPlayerTime", player);	
			//Interface.uMod.CallHook("LockPlayerTime", Eater, storedData.WendiTime[Eater.userID], 0, 1);	
        }
        void WhenAWendigoEatsPlant(BasePlayer Eater, string ItemToEat)
        {
            Eater.metabolism.poison.value = Eater.metabolism.poison.value + 20;
            Eater.metabolism.calories.value = Eater.metabolism.calories.value - 20;
            Eater.metabolism.hydration.value = Eater.metabolism.hydration.value - 30;
			storedData.WendiTime[Eater.userID]= (storedData.WendiTime[Eater.userID] + 1)% 24;
			Eater._maxHealth += 4;		
			if(Oxide.Core.Random.Range(0, 100)<25){
				setWendigo(Eater,true);
			};			
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
			setDefaults(Eater);
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
            if (ItemToEat.Contains("human") == true && ItemToEat.Contains("cooked")==true)
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
					if(!isForbidden && ItemToEat.Contains("cooked")){
						WhenAWendigoEatsMeat(Eater, ItemToEat);
					}
				}
			}
        }
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
							if(storedData.Wendis.ContainsKey(basePlayer.userID)){
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
											return;
										}
										if (saveInfo.msg.baseNetworkable == null)
										{
											return;
										}
										saveInfo.msg.ToProto(Net.sv.write);
										_envSync.PostSave(saveInfo);
										Net.sv.write.Send(new SendInfo(connection));
									}
								}
							}
						
							if(basePlayer.metabolism.calories.value < 10 && basePlayer.metabolism.calories.max > 20){
								basePlayer.metabolism.calories.max+= -1.5f;									
							}else if (basePlayer.metabolism.calories.value<20 && basePlayer.metabolism.calories.max > 30){
								basePlayer.metabolism.calories.max+= -0.5f;
							}else if (basePlayer.metabolism.calories.max-basePlayer.metabolism.calories.value<10){
								if( storedData.Wendis.ContainsKey(basePlayer.userID)){
									basePlayer.metabolism.calories.max+= (basePlayer.metabolism.calories.max < wendiMaxCals?0.5f:0);
								}else{
									basePlayer.metabolism.calories.max+= (basePlayer.metabolism.calories.max < maxCals?0.5f:0);
								}
							}
                        }
                    }
                }
            });
        }

        private void OnPlayerRespawned(BasePlayer player)
        {			
          setDefaults(player);
        }
		
		void setDefaults(BasePlayer player){
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