# Repo for development of my modded Rust server

- Console connect URL: 78.108.218.16:28015
- Name in modded server list : GRUBHUB [Warring Factions]
- SServer Discord (limited beta): https://discord.gg/DMBbNEUPs5

## What you need to know before going in
- Dynamic tides - burning things makes it worst, planting hemp and letting it "die" (where it'll turn into a tree) makes it better.
- Dynamic faction scores - you start out very slightly pacifist. Killing scientists increases bandit score slightly and decreases scientist score, and vice versa. Killing animal raises score with both. Positive score with one aligns with that faction, with both aligns pacifist, with neither aligns Wild. Wild is the animal faction - they won't autoaggro on wild players.
- While in a faction:
  - Hitting a faction NPC with a lit torch will put them in "squad mode", where they stay in your orbit. Do it again to release them.
- Each Bandit or Scientist is also a shop, with their own pricing - it's a bit above their heads
- Players also have faction score - shooting an aligned player could push you out of alignment and have your faction turn on you
- Look at an NPC in your faction, then drop an item to have him drop his current item and take yours. You can use this to swap out weapons.
- Look at an animal while wild and drop a chair or sled to tie it to the animal
- While in a seat, animals that are squadded will run in the direction you're looking, while staying within a radius
  - Hold SHIFT to go faster, hold CTRL to go slower
  - Hold Right-Click to lock direction, allowing drivebys
  - This allows you to lead your animals into battle against the other factions
  - You don't need to be sitting on the animal to do this - sit in a friend's sled while they drive from the chair, and drive your own squad as a forward team!
- Backpack follows in death, so use it to store things in an unlootable way. Limited to 4 rows.
- Traps trigger on NPCs
- Deploy things anywhere if deployment is otherwise valid by using the middle click button instead of the left click.
  - Wire tool now also works like this, including on electronics at monuments - you can hack puzzles with a gas generator
- Raw meat as well as berries will rot if not stored either in a fridge or alongside salt
  - You get salt by purifying fresh water - when you open the intake, you get the salt automatically, as if scraping it off the inside.
  - Rot happens randomly on a clock, when a stack is picked the whole stack rots
  - If meat is stored alongside salt and gets picked for rotting, it turns to jerky instead
  - If berries are targeted the same way, they just stay fresh berries
  - Preserve berries by combining them with salt in an oven - campfire or bbq
  - Because salt is a reskinned gunpowder, selling it is... complicated. Best to just trade in persone
  - Eat everything you scavenge when you start. As you start to stabilize your food needs, start working on salt production or rush fridge
  - A communal fridge at the faction town might be a good way to assure shared food security
- Create orders both in-game using notes in sky lanterns and using C&C on the website! [http://nullzer0.42web.io/](http://nullzer0.42web.io/)
  - Uses the following pattern (calls 5 for 300 seconds / 5 minutes)
```
help!
send:5
timeout:300
```

## So you're trying to build this thing
Proper instructions incoming.


## [Prefab/Plugin Credits](Credits.md)
## Updates

### 23/8/2022 Is that the finish line?
- The command revamp is mostly functional - no more AI Information zones, everything is a grid now. Fireworks and lanterns are now inert. Just need to make a different page for each faction and a bit of tweaking in the server code. Grid based, webpage driven, NPCs holding monuments and stores gives faction currency for spawning npcs - basically play rust in the browser as a weird mashup of chess, battleship, and settlers of catan
- Every NPC is now also a shop, with their own pricing - this is still a work in progress but eh it's a start
- deployment plays nice with factions now
- Otherwise, just missing map-agnostic faction towns by preference/priority in a way that should hopefully allow each faction to at least have A base on the map.

### 10/7/22 The overhaul
- Found a way to implement TRUE deploy-anywhere. This opens up the use of procmaps but required a rewrite that has affected the faction system considerably. Adaptation continues
- Abstracted out the BasicAIState replacement, actual state replacement and not just 10000 hooks, as well as created a  way to manipulate State-Event mapping. This allows complete AI overhaul, both of animals and humanNPC
- With the new deployment method and the new AI management, the faction system needed a full rewrite. Work is ongoing, but most of the faction controls are back for HumanNPC. Animals are still very broken faction-control-wise and invisible to humannpcs, but the work continues.
- Progress has been very much behind the curtain since the last update and in a lot of ways is a step back - but the sun is bright on the horizon, friends.

### 19/3/22 Map and Faction Update
Updates like this are probably not gonna be regular, but i wanted to share the map mostly (pictures at the bottom). That said, I should recap the whole faction/ai revamp

- There are 4 factions - Unalgined, Scientist, Bandit, Wild
- Boars, bears, wolves, and stags (once i replace the "deer" references) are wild, sharks too, chickens and shopkeepers are unaligned
- This is all bandit/scientst but not wild
  - All scientists are Scientist, bandit guards are Bandit
  - 4 hardcoded monuments - bandit camp, junkyard (both bandit), miltunnel and compound (both scientist). These have hardcoded spawners.
  - Vending machines check the nearest defined zone, scrap sold to vending machines go to that faction's bank
  - using the faction bank, define zones (like the gas station) and set up spawn points (sky lanterns set up zones and move paths using notes written in a declarative language I need to document still, and spawns are as simple as placing skull spikes). On a timer, the faction will pick a spawn point created this way and deduct the spawn cost from the faction bank if available to spawn a faction member there.
  - through these mechanics, areas of the map, the profits related to them, and the npc control of that space, are defined and redefined dynamically by players
  - to flip a zone, track down the enemy spawn points and pick them up, then place one of your own. Hardcoded zones cannot be flipped and their spawners are immutable. They can only be held for as long as you can hold them against the constant respawn.
  - spawn points can be set anywhere - but for them to take over the zone, the zone must be cleared of enemy spawns before it's placed
  - Build pretty much anywhere except in sewers and miltunnel (hardocded limitation by facepunch)
  - use fireworks (color and type scheme define the command) to command all or part of your faction npcs to coordinate defense or large scale raids
  - expectation is most player building will be at faction towns and semi-cooperative if not full commune - large-scale NPC raids on faction towns can be organized
  - taking out the torch will show you the nearest defined zone and who controls it
  - if you look at an aligned npc and drop an item, they will drop their current item and pick it up. That said, the ammo they spawn with is limited, so take that into consideration. Planning to make it possible to load them with different ammo types too. So you can upgrade your faction that way.
  - All NPCs spawn in a wander mode where they'll kinda dilly-dally their way slowly away from the spawn, bit like animals are, so you could run into an enemy npc pretty much anywhere with a scale of likelihood the closer you get to their areas - however command fireworks lock spawned npcs to AIInformationZone paths. Thinking of going back to a zoneless way of sorta magnetically pulling their wander towards the goal so it's not limited to paths predefined
- This affects wild as well
  - shooting bandit improves your relation to scientist and viceversa, damaging your faction score with the shot faction by 2x
  - Buying from vending machines improves your relation to a faction without damaging your relation to another
  - if both factions hate you, you become wild - animals won't attack a wild aligned player, but both warring factions will
  - if both factions like you, you become unaligned - neither will shoot at you, but neither will care if you get shot either (shooting unaligned things has no faction score effect)
  - if you kill wild things, both factions go up by a very tiny bit
  - icon in the top right to tell you which you are now
  - smacking an aligned npc with a torch will put them in squad mode - they'll patrol in your radius when close, and chase you when over a set distance. including animals. Smack them a second time to release them.
- This only affects wild
  - If you "drop" a chair or sled while looking at a wild animal (not chickens) the chair or sled will weld to the animal, letting you ride them
  - both can be attached, for up to 3 seats
  - while sitting, all animals in your squad go into a sort of "follow eyesight" mode - they'll move forward at a speed dependent on whether you're holding shift or ctrl or neither, in the direction you're facing. If they go too far they come back towards you. Including the animal you're riding, which is how you control it
  - you can control your own squad from a sled on a wolf driven by another player, to act as a scouting force.
  - As long as the animals are in passive, they follow orders, but being wild animals, i they go into attack/chase/flee(A/C/F), they will follow their instincts and you are just along for the ride. This includes the animal you're driving - but only affects the animals in that state, so you keep some control of the pack
  - While zoomed, animals go into "run forward" mode, allowing drive-by tactics and crashing the sides of your pack into their line (where they go A/C/F and split off from the pack)
  - that said, if your ride falls into A/C/F, since you can't turn 180 the rest of the pack usually follows


Superdense map is coming along nicely. Right now using the red brick building as a placeholder while I figure out how to fill out some other rowhouses, then I'll swap them out. Lots of floaters still, not shown are the water treatment plant, harbor, radtown, miltunnels, dome, or subway network. MLRS are around too, to help with attempts to flip zones.

![image](https://user-images.githubusercontent.com/33932119/159136547-4d6fae05-c631-49a5-a1cb-dd554a71fb43.png)
![image](https://user-images.githubusercontent.com/33932119/159136554-dd0533cd-ec34-414a-9e4f-65fc116ee9a0.png)
![image](https://user-images.githubusercontent.com/33932119/159136566-a7e183a8-76a9-4e9b-884d-5590d2d5b92d.png)
![image](https://user-images.githubusercontent.com/33932119/159136574-ea270c5a-9f1d-4f9c-abe1-4152d2ab18d8.png)
![image](https://user-images.githubusercontent.com/33932119/159136581-efafc846-f7fc-4ad0-97ae-f84e6140f96b.png)
![image](https://user-images.githubusercontent.com/33932119/159136591-4fbb2461-cfbb-4865-bb3b-fc2fbc548656.png)
And one of the wolf all kitted up:
![image](https://user-images.githubusercontent.com/33932119/159137438-c86abe98-79d1-46c8-a7f0-60ddd4190efb.png)
