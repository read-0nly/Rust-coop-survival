
using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Rust.Ai;
using System;
using UnityEngine; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;

namespace Oxide.Plugins
{
	[Info("Food Waste", "obsol", "0.0.1")]
	[Description("Food wastes over time, with fridges and salt offering preservation methods.")]
/*======================================================================================================================= 
* 
//One Entity Enter - if nobuild zone, leave

/*
void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
{
    Puts("OnEntityEnter works!");
}
*=======================================================================================================================*/


	public class FoodWaste : CovalencePlugin
	{

		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) =>
            _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Food Waste]</color>", 0, Array.Empty<object>());
		float saltStep = 0.5f;
		string saltIcon = "https://i.imgur.com/Uuzl712.png";
		int wasteTickThreshold = 200;
		int wasteTick = 0;
		private List<ItemContainer> containers = new List<ItemContainer>();
		
		private Dictionary<WaterPurifier,float> purifierSaltStorage = new Dictionary<WaterPurifier,float>();
		
		private void OnServerInitialized()
        {
			foreach(BoxStorage sc in GameObject.FindObjectsOfType<BoxStorage>()){
				if(!containers.Contains(sc.inventory)){containers.Add(sc.inventory);}
			}
            timer.Every(10f, () => {
				wasteTick++;
				if(containers.Count>0 && (wasteTick %(containers.Count < wasteTickThreshold? wasteTickThreshold-containers.Count : 0))==0){
					//*
					ItemContainer ic = containers[UnityEngine.Random.Range(0,containers.Count)];
					/*/
					foreach(ItemContainer ic in containers){
						//*/
						List<Item> foods = new List<Item>();
						Item saltPile = null;
							//Puts(ic.entityOwner.ToString());
							if(ic.entityOwner.ToString().Contains("fridge")){
								return;
							}
						//Item it = ic.itemList[Random.Range(0,ic.itemList.Count)];
						foreach(Item it in ic.itemList){
							//Puts(it.info.shortname);
							//Puts(it.name);
							if(it.info.shortname.Contains(".raw") || it.info.shortname=="bearmeat" || it.info.shortname=="meat.boar"){
								foods.Add(it);	
							}
							if(it.info.shortname.Contains(".berry") && !it.info.shortname.Contains("clone")&& !it.info.shortname.Contains("seed") ){
								foods.Add(it);	
							}
							else if (it.name != null && it.name.ToLower()=="salt"){
								saltPile = it;
							}
						}					
						Item itemTarget = null;
						//Puts(foods.Count.ToString());
						if(foods.Count>0){
							itemTarget = foods[UnityEngine.Random.Range(0,foods.Count)];
						}
						if(itemTarget==null){return;}
						if(itemTarget.info.shortname.Contains(".raw") || itemTarget.info.shortname=="bearmeat" || itemTarget.info.shortname=="meat.boar"){
							if(saltPile==null){
								ic.Remove(itemTarget);
								ic.Insert(ItemManager.CreateByItemID(-751151717, itemTarget.amount, 0));
							}
							else{
								saltPile.amount--;
								if(saltPile.amount<1){									
									ic.Remove(saltPile);//-1848736516
								}
								else{
									saltPile.MarkDirty();
								}
								
								ic.Remove(itemTarget);
								Item jerky = ItemManager.CreateByItemID(-1848736516, itemTarget.amount, 0);
								jerky.name="Jerky";
								ic.Insert(jerky);
							}
						}
						else if(itemTarget.info.shortname.Contains(".berry")){
							if(saltPile==null){
								ic.Remove(itemTarget);
								ic.Insert(ItemManager.CreateByItemID(352130972, itemTarget.amount, 0));
							}
							else{
								saltPile.amount--;
								if(saltPile.amount<1){									
									ic.Remove(saltPile);//-1848736516
								}
								else{
									saltPile.MarkDirty();
								}
							}
							
						}
						wasteTick = 0;
					//}/////////////////////////////////////////
					
				}
				
			});
            timer.Every(120f, () => {				
				foreach(StorageContainer sc in GameObject.FindObjectsOfType<StorageContainer>()){
					if(!containers.Contains(sc.inventory)){containers.Add(sc.inventory);}
				}
				foreach(StorageContainer sc in GameObject.FindObjectsOfType<StorageContainer>()){
					if(!containers.Contains(sc.inventory)){containers.Add(sc.inventory);}
				}
			});
		}
		
		void OnWaterPurified(WaterPurifier waterPurifier, float timeCooked)
		{
			//Puts("OnWaterPurify works!");
			if(!purifierSaltStorage.ContainsKey(waterPurifier)){purifierSaltStorage[waterPurifier]=0.0f;}
			purifierSaltStorage[waterPurifier]+=saltStep;
			//Puts(purifierSaltStorage[waterPurifier].ToString());
			/*
			
            Item item = ItemManager.CreateByItemID(itemData.ItemID, itemData.Amount, itemData.Skin);
            item.condition = itemData.Condition;
            item.maxCondition = itemData.MaxCondition;
			
			
			*/
			//return null;
		}
		
		object OnOvenCook(BaseOven oven, Item item)
		{
			List<Item> salts = oven.inventory.FindItemsByItemID(-265876753);
			foreach(Item salt in salts){
				if(salt.name.ToLower() == "salt"){
					Puts(salt.name);
					foreach(Item fruit in oven.inventory.itemList){
						if(fruit.info.shortname.Contains(".berry") && !fruit.info.shortname.Contains("clone")&& !fruit.info.shortname.Contains("seed") && fruit.amount > 9){
							fruit.amount += -10;
							if(fruit.amount<1){
								oven.inventory.Remove(fruit);
							}
							salt.amount+=-5;
							if(salt.amount<1){									
								oven.inventory.Remove(salt);////-1848736516
							}
							string[] parsedName = fruit.info.shortname.Split('.');
							string color = parsedName[parsedName.Count() - 2];
							Item jam = ItemManager.CreateByItemID(-1941646328, 1, 0);
							jam.name=color+" Berry Jam";
							oven.inventory.Insert(jam);
						}
					}
				}
			}			
			return null;
		}
	
		bool? OnIngredientsCollect(ItemCrafter itemCrafter, ItemBlueprint blueprint, ItemCraftTask task, int amount, BasePlayer player)
		{
			//*
		    List<Item> collect = new List<Item>();
			
			foreach(ItemAmount ingredient in blueprint.ingredients){
					CollectIngredient(itemCrafter,ingredient.itemid, (int) ingredient.amount * amount, collect, player);				
			}
			bool dropout = false;
			
			foreach(ItemAmount ingredient in blueprint.ingredients){
					bool ingredientFound = false;
					foreach(Item item in collect){
						ingredientFound = ingredientFound || (item.info.itemid ==ingredient.itemid);
					}
					if(!ingredientFound){dropout=true;}
			}
			task.potentialOwners = new List<ulong>();
			foreach (Item obj in collect)
			{
			  obj.CollectedForCrafting(player);
			  if (!task.potentialOwners.Contains(player.userID))
				task.potentialOwners.Add(player.userID);
			}
			task.takenItems = collect;
			if(dropout){
				task.cancelled=true;
				if (((task.takenItems == null ? 0 : (task.takenItems.Count > 0 ? 1 : 0))) != 0)
				{
				  foreach (Item takenItem in task.takenItems)
				  {
					if (takenItem != null && takenItem.amount > 0)
					{
					  if (takenItem.IsBlueprint() && (UnityEngine.Object) takenItem.blueprintTargetDef == (UnityEngine.Object) task.blueprint.targetItem)
						takenItem.UseItem(task.numCrafted);
					  if (takenItem.amount > 0 && !takenItem.MoveToContainer(player.inventory.containerMain))
					  {
						takenItem.Drop(player.inventory.containerMain.dropPosition + UnityEngine.Random.value * Vector3.down + UnityEngine.Random.insideUnitSphere, player.inventory.containerMain.dropVelocity);
						player.Command("note.inv", (object) takenItem.info.itemid, (object) -takenItem.amount);
					  }
					}
				  }
				}
			}
			return false;/*/
			return null;
			//*/
		}
		
		  private void CollectIngredient(ItemCrafter ic, int item, int amount, List<Item> collect,BasePlayer player)
		  {
			foreach (ItemContainer container in ic.containers)
			{
			  amount -= Take(container, collect, item, amount, player);
			  if (amount <= 0)
				break;
			}
		  }
		  private int Take(ItemContainer ic, List<Item> collect, int itemid, int iAmount,BasePlayer player)
		  {
			int num1 = 0;
			if (iAmount == 0)
			  return num1;
			List<Item> list = Facepunch.Pool.GetList<Item>();
			foreach (Item obj in ic.itemList)
			{
			  if (obj.info.itemid == itemid && obj.name==null)
			  {
				int num2 = iAmount - num1;
				if (num2 > 0)
				{
				  if (obj.amount > num2)
				  {
					obj.MarkDirty();
					obj.amount -= num2;
					num1 += num2;
					Item byItemId = ItemManager.CreateByItemID(itemid);
					byItemId.amount = num2;
					byItemId.CollectedForCrafting(player);
					if (collect != null)
					{
					  collect.Add(byItemId);
					  break;
					}
					break;
				  }
				  if (obj.amount <= num2)
				  {
					num1 += obj.amount;
					list.Add(obj);
					collect?.Add(obj);
				  }
				  if (num1 == iAmount)
					break;
				}
			  }
			}
			foreach (Item obj in list)
			  obj.RemoveFromContainer();
			Facepunch.Pool.FreeList<Item>(ref list);
			return num1;
		  }
		//*/
		
		
		bool CanStackItem(Item item, Item targetItem)
		{
			if(item.name != targetItem.name || item.info.itemid != targetItem.info.itemid){return false;}
			return true;
		}
		
		
		/*
		
		    List<Item> collect = new List<Item>();
    foreach (ItemAmount ingredient in bp.ingredients)
      this.CollectIngredient(ingredient.itemid, (int) ingredient.amount * amount, collect);
    task.potentialOwners = new List<ulong>();
    foreach (Item obj in collect)
    {
      obj.CollectedForCrafting(player);
      if (!task.potentialOwners.Contains(player.userID))
        task.potentialOwners.Add(player.userID);
    }
    task.takenItems = collect;
	
	
	  public void CollectIngredient(int item, int amount, List<Item> collect)
  {
    foreach (ItemContainer container in this.containers)
    {
      amount -= container.Take(collect, item, amount);
      if (amount <= 0)
        break;
    }
  }
  
		*/
		
  
		
		void OnLootEntity(BasePlayer player, BaseEntity entity)
		{
			WaterPurifier wp = entity.GetComponent<WaterPurifier>();
			if(wp==null){return;}
			if(!purifierSaltStorage.ContainsKey(wp)){return;}
			int saltAmount = (int)purifierSaltStorage[wp];
            Item item = ItemManager.CreateByItemID(-265876753, saltAmount, 2572257907);
			item.name = "Salt";
			item.MarkDirty();
			purifierSaltStorage[wp]-=saltAmount;	
			player.inventory.GiveItem(item);
		}
		Item OnItemSplit(Item item, int amount)
		{
			if(item.name != null){
				item.amount -= amount;
				Item byItemId = ItemManager.CreateByItemID(item.info.itemid);
				byItemId.amount = amount;
				byItemId.skin = item.skin;
				byItemId.name = item.name;
				item.MarkDirty();
				byItemId.MarkDirty();
				return byItemId;
			}
			else{				
				/**/return null;//*
			}
			/**/
		}
	}
}