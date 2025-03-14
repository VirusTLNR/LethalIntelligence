# LethalIntelligence

## Information

A mod aiming to tweak all AI in the game to have improved flavour and perhaps be more intelligent

This mod is built upon Piggy's "MaskedAiRevamp v0.2.1" (https://thunderstore.io/c/lethal-company/p/Piggy/MaskedAIRevamp/). (github located here.. https://github.com/Piggy3590/MaskedAIRevamp)

Serious FPS drops may occur, even on high end PCs, do report them to me if they occur and I will look into them.

*Also... There may be tons of bugs.* (please do report them to me, either via github or the Lethal Company Modding Community on discord!)

## Note re:DebugMode
There is now a setting in config for debug mode, while this is experimental, this will be turned on by default, if you are getting performance issues (low fps i mean) then feel free to turn it off, or just increase the delay between reports. It shouldent be causing an issue, but does give me vital information for fixing your issues. (if you were using this mod before v0.1.5 this will not be on by default, and if you are using this since before v0.1.8 then the delay will be 100, the lower the better though.)

if this is on.. OR Imperium is installed.. debug mode will be turned on, this will provide more logs (potentially more spammy), if debug mode is on, regular "status" reports will be logged for every masked, so if you are having issues, feel free to turn this mode on, replicate the issue, then send me the log.

## Mod Features
<details>
  <summary>Masked Behaviour (spoiler?):</summary>

* Aggressive
    * If you have a dropped shotgun, pick it up and shoot people. (currently disabled - fixing soon)
    * If there is a player with a shotgun, attack with a shovel type item. (currently disabled - fixing soon)
    * will almost always target a detected player. (also has a player focus where they 100% focus on chasing you til you are dead)

* Stealthy
    * Will mimic players
    * will hide from players
    * very unlikely to target players
    * Can pick up and use WalkieTalkies

* Cunning
    * Stealing items in the area around the ship and hiding them in bushes (max 5 items)
    * Call a fake dropship using the terminal
    * Tampers with the breaker box to turn off the lights, will keep turning the lights off while they are alive and the lights are turned on.

* Deceiving
    * Uses terminal codes to make you think someone is in the ship and help/hinder you.
    * will tend to ignore (not attack) you in favour of making you beleive they are a player.
    * Can pick up and use WalkieTalkies

* Insane
    * Uses signal translator to make you think someone is in the ship and help/hinder you.
    * can "sabotage" the apparatus (after 2pm only)
    * will make the ship take off after it has completed sabotaging the apparatus.. fair warning will occur as long as you own a signal translator.
    * will tend to target players more than most other personalities
    * Can pick up and use WalkieTalkies

 </details>

## Known Issues
- big fps drops if more than 10 masked spawn at once (vanilla limits you to 2? 4? 10 shouldent be spawning that often! if a mod spawns 10+ at once and you want to use it with LI, please let me know, ill try to disable my mod when that situation is occuring)
- pathing to objects behind locked doors/walls will cause "stuck motion" (as does any route to which the masked cannot reach the destination) - working on a solution to this
- if you despawn a masked (using a mod to remove them from the game) while it is on the terminal, the terminal is unusable and the lobby must be restarted - not planning to fix as shouldent occur in an actual game. (if you kill them before you despawn them, this wont happen).
- problematic interiors (usually due to navmesh issues): PeachesCastle,

If you have any of the above issues, or any other issue, dont hesitate to send me the full log file after your session has ended (either on discord, or as an issue on github)

## Mods
100% Compatible:
+ 'MaskedEnemyOverhaulFork' mod by Coppertiel
+ 'Skinwalkers' by RedbugRedfern
+ 'Mirage' by qwbarch
+ 'Wendigos_Voice_Cloning' by Tim_Shaw
+ 'SignalTranslatorUpgrade' by Fredolx
+ 'AlwaysHearActiveWalkies' by Suskitech
+ 'Remnants' by KawaiiBone
+ '2StoryShip' by MelanieMelanicious
+ 'Wider Ship Mod' by mborsch
+ 'ProblematicPilotry' by windblownleaves

Not 100% Compatible (and how to get the best compatibility with these mods.. feel free to suggest other things that need adding to this list as i dont use every mod!):
+ General Improvements - Disable all settings related to the masked, if you turn some on and get no issues, do let me know which ones so i can add them here as "fine". The "map dot" should be a 100% turn off as its part of this mod too.
+ TooManyEmotes - Turn off "stop and stare duration override"? on, or off, one or the other, this may make masked look weird!
+ Welcome to Ooblaterra - the ghost masked on this moon are seemingly causing errors.. i d k why yet.

## Thanks to...
- Piggy for the original Masked AI Revamp to which this mod is built on and inspired me to do more, and for the permission to use your code as a base point.
- TestAccount666 for the signal translator code from AutomaticSignals.
- MattyMatty for the LobbyCompatibility softdependency class as well as helping resolve the bad masked<->terminal positioning due to hardcoded positioning previously.
- Kite (on discord) for the Masked joining/leaving terminal fixes.
- WhiteSpike (on discord) for help and suggestions regarding the breaker box.
- Tim_Shaw for help on trying to make their mod compatible and integrated.
- XuXiaolan for help/advice/tips with spawning items as well as many other things (soon to be too many things to mention).
- Szumi57 for inspiration on how to fix EntranceTeleports.
- qwbarch for all the help on integrating Mirage and LethalIntelligence!
- Fandovec03 for suggesting LOS as a fix to "distance related issues" I was having.
- darmuh for help with "synced audios" for the terminal audio improvements
- IAmBatby for suggesting how I could improve checking the EntranceTeleports
- Buttery Stancakes for helping me fix compatibility with ButteryFixes as well as providing insight into how to fix Mineshaft Elevator issues.