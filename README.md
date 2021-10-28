# Repo for development of my modded Rust server

Roadmap:
- ~Remove downsides to eating human meat~ < Done but in a lazy way - it would be better to modify the prefab defaults
- ~add chance of cannibal status~ < Done
- ~Cannibalism lowers hunger.max by A LOT but boosts max health. You can have more health than normal - as long as you KEEP EATING~ < Done but hackish and lazy
- ~Cannibalism should grant nightvision~
- ~Base hunger and thirst reduced, driving more focus on scavenging~
- ~On death, some sort of log function tugs max health towards hunger/max point. So if you die hungry, you respawn with lower max health. Drive more scavenging~
- Reduce gather rates considerably, increase loot tables especially of basic things like tools as well as farming, salt, ammo.
- 5 different towns with their own themes, scattered points of interest between
- ~No build restrictions in structures - building entirely new map with custom monuments to serve the purpose~
- ~No decay protection - so you need to maintain what you wanna keep manually.~
- ~Decay fiddling so that most things last 24 hours unattended~ < needs balance but works
- Hidden shops scattered with their own inventories and prices. No map pins
- ~More car availability~
- ~Remove crafting of high-tier things like AKs, most ammo crafting, or change costs to make it unviable.~
  - ~Guns should be there as a wildcard but should be hard to acquire. No one should be armed to the teeth.~ 
    - ~Ideally, they mostly get used in desperation facing off cannibals (or by those cannibals desperate for a meal)~
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
- ~Variable water level~
- ~forest management~
- ~Food rot and preservation~
-

# Disease plugin plan
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

# Houseboat plugin plan
 - Look at building plan code - how does it spawn a building?
 - Spawn floor frame at boat location, with boat as parent
 - instantiate building instance on floor frame, building part with 100% stability
 - Collision testing - if no effect, gonna need to add rigidbodies to the parts
 - Can entities be labelled "dirty" to force an update? Can the ocean?
 - Failing that - thin terrain trigger right under water level, rise with water. <Start here

Current issues:
- Map triggers are fucked, deadzones in the sewers
- Building in sewer parts don't work at all. Caves neither. 
  - Terrain triggers are stuffed in the map file - this makes it so even deleting them server side doesn't allow clients to build. Need a way to break up prefabs into sub-references, or dynamically add sewer parts on initialize and remove them from that - since it's after the map it should load the changes, but only on connect
- ~"Deployables" break for a bunch of reasons. Statics seem uninteractable. This one might require I take the time to deploy and wire those sorts of things myself at wipe, ugh. ~ I didn't have the rustedit dll
