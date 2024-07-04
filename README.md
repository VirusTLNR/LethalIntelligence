# LethalIntelligence

## Information

A mod aiming to tweak all AI in the game to have improved flavour and perhaps be more intelligent

This mod is built upon Piggy's "MaskedAiRevamp v0.2.1" (https://thunderstore.io/c/lethal-company/p/Piggy/MaskedAIRevamp/).

As piggy wrote in the readme for v0.2.1 - this mod maybe very buggy, i hope to make it more stable over time!

All clients must have this mod installed for it to work!

There is compatibility with RugbugRedfern's SkinWalkers mod.

The current version is a **very very early** version, no optimization work has been done, so you may experience significant frame drops even with high PC specs!!!

*Also... There may be tons of bugs.* (please do report them to me, either via github or the Lethal Company Modding Community on discord!)

## Note re:DebugMode
There is now a setting in config for debug mode, for normal play, please leave this turned off.

if you turn this on.. OR Imperium is installed.. debug mode will be turned on, this will provide more logs (potentially more spammy), if debug mode is on, regular "status" reports will be logged for every masked. If you are having issues, feel free to turn this mode on, replicate the issue, then send me the log.

## Mod Features
<details>
  <summary>Masked Behaviour (spoiler?):</summary>

* Aggressive
    * If you have a dropped shotgun, pick it up and shoot people.
    * If there is a player with a shotgun, attack with a shovel type item.
    * will almost always target a detected player. (also has a player focus where they 100% focus on chasing you til you are dead)

* Stealthy
    * Will mimic players
    * will hide from players
    * very unlikely to target players

* Cunning
    * Stealing items in the area around the ship and hiding them in bushes (max 5 items)
    * Call a fake dropship using the terminal
    * Tampers with the breaker box to turn off the lights, will keep turning the lights off while they are alive and the lights are turned on.

* Deceiving
    * Uses terminal codes to make you think someone is in the ship and help/hinder you.
    * will tend to ignore you in favour of making you beleive they are a player.

* Insane
    * Uses signal translator to make you think someone is in the ship and help/hinder you.
    * can "sabotage" the apparatus (after 2pm only)
    * will make the ship take off after it has completed sabotaging the apparatus.. fair warning will occur as long as you own a signal translator.
    * will tend to target players more than most other personalities

 </details>

## Known Issues
- Personality selection sometimes bugs out in 0.1.2, potentially fixed in 0.1.3

## Mods
Recommended to install with this mod:

+ 'MaskedEnemyOverhaulFork' mod by Coppertiel (the original by HomelessGinger is bugged at time of writing this).
+ 'Skinwalkers' by RedbugRedfern. OR 'Mirage' by qwbarch OR 'Wendigos_Voice_Cloning' by Tim_Shaw (no integration for mirage as of yet, but it is planned when mirage v2 is out)
 

## Thanks to...

- Piggy for the original Masked AI Revamp to which this mod is built on and inspired me to do more, and for the permission to use your code as a base point.
- TestAccount666 for the signal translator code from AutomaticSignals.
- MattyMatty for the LobbyCompatibility softdependency class.
- Kite (on discord) for the Masked joining/leaving terminal fixes.
- WhiteSpike (on discord) for help and suggestions regarding the breaker box.
- Tim_Shaw for help on trying to make their mod compatible.
- XuXiaolan for help/advice/tips with spawning items as well as many other things (soon to be too many things to mention).
