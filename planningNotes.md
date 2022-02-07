## The gist
- Faction system between bandit and scientist. you spawn marked pacifist, if you kill scientists bandits like you more and vice versa. killing lowers your score with that faction more than it raises it with the other.
- scientists run towards compound sometimes
- compound isn't a safezone but is owned by bandit faction. attacking bandit-aligned players also affects your faction score.
- build anywhere, no TC, no build privileges, no decay protection. Most deployables have no or little decay. twig no decay, wood normal decay, metal and up high decay, requires regular upkeep. also, no hammer pickup privileges, to prevent the easy grief. so think before you deploy.
- Food rots, get salt from purifier and stick it in the chest to preserve it. or use a fridge
- make jam by putting fruits and salt in "oven" (campfire/bbq/etc)
- gunpowder and most ammo crafting blocked, gunpowder can be made at mixing table though
- 11pm everyone gets booted, then time advances to 5am and water creeps up
- trees don't respawn, but hemp that moves to "dying" phase become small tree. plant your hemp seeds if you like wood.
- max hunger changes depending on how full/hungry you are, slowly, over time. Stay well-fed. think of it like fat.
- max thirst and health scale up over time to reward you for staying alive. think of it like babu become adult.
- the backpack is sacred. can't be looted, persists between respawns and wipes. cabin is temporary, backpack is forever.
- some places have doors you can stick door controllers to to shut - since housing isn't always an option consider this a "safe" option, though it needs power to keep the door shut and your power source is vulnerable.
- You lose all blueprints you've learned when you die - think of it as trauma caused by dying, or think of it as a roguelike mechanic

## Project relaunch 2/7
- Look into free maps video, replace all monuments with build-free clones
- remove building and crafting of anything past tier 1
- Allow hammer again. Finders keepers.
- find a way to weld a camper to a raft with barrels - houseboats
- sprinkle pre-determined "shelters", fridges
- transformers around the map can be powered by fuses in the power plant
- no text chat
- no safe zones, scietists spawn at tunnels and bandits spawn at bandit camp
- the goal for both is holding compound, some remain and patrol. Will mostly roam until event is triggered
- if compound is held for more than x seconds by faction, some faction members spawn inside
- when event starts, other side gets a tank
- bandit tank, 3/4 health
- camper submarine

## Roadmap:
- On death, some sort of log function tugs max health towards hunger/max point. So if you die hungry, you respawn with lower max health. Drive more scavenging
- Reduce gather rates considerably, increase loot tables especially of basic things like tools as well as farming, salt, ammo.
- Hidden shops scattered with their own inventories and prices. No map pins
- Permanent fog
- Custom plant breeding code, Custom animal breeding code, based off population in range on some event. 
  - Animals that have eaten drop a seed of something they ate that night
  - When an animal eats, if there are 1-3 others nearby, they breed
  - Animals have their own hunger system
    - Deer like corn and berries and hemp
    - Bears like corn and berries and pumpkin
    - Pigs like mushroom, corn, and pumpkin
- No chat, voicechat only
- Phones, CCTVs, some leftover computers for communication/surveillance
- Synced interserver backpacks - and with it, dedicated climate islands, to allow easy inter-server transition.
- my own disease plugin
- some sort of houseboat plugin, bonus if the houseboats actually boat


## Done:
- ~Variable water level~ < Night cycle kicks players otherwise they drown in midair
- ~forest management~ < spawn blocked, hemp grows into tree after passing mature, requires players to plant them first, only small trees
- ~Food rot and preservation~ < get salt from purifiers by opening panel after it's purified for a while, all containers containing food are in a pool, it picks one every couple of seconds and picks a single slot to rot out of the food items. rots the stack. either get salt, or spread your stacks to spread the RNG.
- ~More car availability~ < very done
- ~Remove crafting of high-tier things like AKs, most ammo crafting, or change costs to make it unviable.~ < Blocked gunpowder crafting except mixing table, blocked crafting most ammo. no nodes, only pickup, so harder to mass-produce anyways.
  - ~Guns should be there as a wildcard but should be hard to acquire. No one should be armed to the teeth.~ < different strategy - limit ammo, but in some ways make guns more available. bluffs viable.
    - ~Ideally, they mostly get used in desperation facing off cannibals (or by those cannibals desperate for a meal)~ < Cannibal broken
- 5 different towns with their own themes, scattered points of interest between < 1 so far
- ~No build restrictions in structures - building entirely new map with custom monuments to serve the purpose~ < Mostly true. Strategically false.
- ~No decay protection - so you need to maintain what you wanna keep manually.~ < Blocked TC, blocked hammer pickup to balance the hole from losing build perm control. twig has no decay, wood has reduced decay, metal and hq have high decay.
- ~Decay fiddling so that most things last 24 hours unattended~ < needs balance but works. Decided to be less severe about it
- ~Base hunger and thirst reduced, driving more focus on scavenging~ < Done

## FoodWaste Plugin details
- Get salt by opening a purifier that's been purifying salt water. It's a variant of gunpowder. I'll spare you the absolute fuckery it took to get it working.
- There's a clock that ticks regularly and picks containers based on the number of containers deployed
- It then picks a food stack in that container and "rots" it, depending on what it is
- This excludes fridges
- if there's salt in the chest, it consumes salt instead of rotting food. If the food it picked was meat, it turns it to jerky instead of rotting. Fruits/veggies are kept fresh.
- You can turn fruit to jam for long term preservation by combining fruit and salt in any oven variant including campfire. This follows pickle mechanics, but that's unintentional and will be fixed at some point.
- player inventory and backpack unaffected. I consider this a bug and it will be fixed, but it isn't at the moment.

## SurvivalMods plugin details
- Hunger is capped low at spawn. So is thirst and max health.
- thirst and max health both scale the longer you live, up to their max - rewards longevity strategies
- hunger scales up if you're within 10 of your current max, and scales down if you're starving 
  - to the point that extended starvation means needing to slowly eat your hungermax back up by chasing your max with low-calorie, easy to digest foods
  - basically, not properly managing your hunger comes with steep penalties
  - think of it like a "fat" metric, and over-starvation requires nursing to get back to a healthy weight while being regularly well fed means surviving long periods without food
- The ocean slowly rises. This is why everyone needs to get kicked - otherwise the clientside doesn't update and they drown in open air.
- But yeah there's the 11pm kick. Downside, it sucks and leaves you vulnerable (like sleep, making finding good shelter for the night even more important!) but also it skips straight to 5pm after so you don't have to deal with as much night, at least.
- The idea is that as the water rises it turns into a bit of a waterworld server, where you dive to scavenge your old chests that the waves have swallowed
- Tree spawns are all removed at server initialize - as such, the trees only spawn once when the server is booted.
- planting hemp and letting it grow into the dead phase will instead make a small tree, so you can "manage" the forest by replanting trees, or have to deal with a lumber crisis
- living long enough will give you a higer-than-normal hp as well, kinda like an overshield for good behavior. 
- scientists created with npc spawner can be rallied to a "hotzone" - this will make them charge towards it when awake, intercepting players along the way. This allows waves on the compound
- You lose all blueprints you've learned when you die - think of it as trauma caused by dying, or think of it as a roguelike mechanic

## HumanNPC customizations
- This one needs explaining
- They will now not be hostile and mostly just stand around
- if they see a scientist of any kind, they'll attack them
- if a player or another humannpc is attacked, those in the area will rally against the attacker, acting as a pseudo-safezone
- the whole firing action is... not great. looking to somehow rework scientist AI instead
- with it, the compound is not a safezone, is not build restricted, has no turrets. 
  - You can try to overwhelm the guards if you like, become king of compound till the waves win
  - you can help them fight off wildlife and scientists that occasionally charge them
  - you can loot and chop up bodies for fun and profit

## Disease plugin plan
- Transmission vectors
  - Area (cough)
  - Touch (interaction with deployable)
- Effects
  - Hunger max decay
  - Hunger decay
  - Water decay
  - Max health cap
  - Occasional Wounded
- 5 families, defined by primary symptom (secondary symptoms will be weaker and few). Treating a secondary symptom will only stop that symptom for some time. Treating the primary removes disease.
- Timers to manage when diseased people/items time out
- Medicines
  - Add to salt system, medical salts (each treats a specific symptom), antibiotic soap (using while looking at deployable will disinfect it).
  - Mixing table required for it. Mix on leave panel.
- Recipe ideas 
  - lgf, sulfur, red berry seed < some synthesized thing
  - water, raw chicken, salt < Chicken soup
  - crude, sulfur, white berry < some synthesized thing
  - apple, apple, apple < 3 apples keep teh doctor away?
  - sulfur, salt, bones < Some synthesized thing
  - sulfur, salt, water < disinfectant
- Disease catch method depend on family
  - Wet too long
  - Cold too long
  - Bad food
  - Bleeding
  - Animal attack
- Strategies
  - identification and quarantine of the sick
  - distribution of medicine
  - disinfect everything
  - hazmat suit 


## Witchcraft plugin plan
- Create some extra items for the task
- some rituals need mixing table, some need an oven, some need specific conditions (a locked chest? a lootbox?). Basically use the fruit jam logic.
- Some effects are on the user (loot changes, damage resistance, gather changes) with timeouts
- some are environmental (attract animals, cause rain, lower the oceans, spawn trees)

## Houseboat plugin plan
 - Look at building plan code - how does it spawn a building?
 - Spawn floor frame at boat location, with boat as parent
 - instantiate building instance on floor frame, building part with 100% stability
 - Collision testing - if no effect, gonna need to add rigidbodies to the parts
 - Can entities be labelled "dirty" to force an update? Can the ocean?
 - Failing that - thin terrain trigger right under water level, rise with water. <Start here

## Cannibal, obsoleet spin < disabled, needs fixing
- ~Remove downsides to eating human meat~ < Done but in a lazy way - it would be better to modify the prefab defaults <borked by update reee
- ~add chance of cannibal status~ < Done <borked by update reee
- ~Cannibalism lowers hunger.max by A LOT but boosts max health. You can have more health than normal - as long as you KEEP EATING~ < Done but hackish and lazy<borked by update reee
- ~Cannibalism should grant nightvision~<borked by update reee

## Current issues:
- Building in sewer parts don't work at all. Caves neither. 
  - Terrain triggers are stuffed in the map file - this makes it so even deleting them server side doesn't allow clients to build. Need a way to break up prefabs into sub-references, or dynamically add sewer parts on initialize and remove them from that - since it's after the map it should load the changes, but only on connect
