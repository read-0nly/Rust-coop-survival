# Repo for development of my modded Rust server

## Start here
Just go read the wiki, it's better-formatted: https://github.com/read-0nly/Rust-coop-survival/wiki

## Behind the curtain
My main planning document / documentation - it's a damn mess: https://github.com/read-0nly/Rust-coop-survival/blob/main/planningNotes.md

## So you're trying to build this thing
Proper instructions incoming.

## Updates

### 3/19 Map and Faction Update
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
