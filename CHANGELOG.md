## Changelog

### 0.2.1 - Mirage Integration Patch (Hotfix 2)
- Fixed an issue where Mirage was considered a hard dependency and so would break the mod (and potentially the game) if mirage was disabled/not installed.

### 0.2.0 - Mirage Integration Patch (Hotfix)
- Fixed the NullReferenceException relating to PlayerAnimationEvents.UnlockArmsFromCamera when masked hold 1 handed items (generic 1 handers, including walkie talkies and flashlights too)

### 0.1.9 - Mirage Integration Patch
- Fixed Masked Picking up WalkieTalkies (previously they would not pick them up)
- Added integration with Mirage, masked will now play mirage audio through walkies to trick you (Stealthy/Deceiving/Insane only), audio will only play through walkies if someone has an active walkie far enough away from the masked, the starting rate is 50% chance, more far players with walkies will increase that chance)
- Fixed an issue regarding a conflict with "SignalTranslatorUpgrade" mod.
- Fixed a softlock from where a masked dies while using the terminal. 

### 0.1.8
- Fixed masked "double teleporting" at the main entrance leading to them getting stuck thinking they are outside when they are inside, and vica versa.
- Added auto removal of config entries that are no longer being used in the Lethal Intelligence config file. (for the future where I see many settings being added, then later removed :))
- Changed the default configuration of "debug mode delay" to 0 as that provides the most information, and I dont think status reports provide any lag. if you refresh your config and start getting lag after this update, then turn off debug mode, or increase the delay.

### 0.1.7
- Focus, Activity, and whether the masked is Running or not should now be synced from host to client, if you see different things from host to client, do let me know.
- Fixed a null reference exception relating to the breakerbox that was previously missed.

### 0.1.6 - Fixing Entrances/Exits + Implementing FireExit usage
- Masked can now use fire exits, 
- Masked getting stuck at the entrances/exits should now be fixed. If a masked has recently used an entrance/exit, they will now either.. do something else, or wait a short period and do some idle actions while they wait.

### 0.1.5 - Hotfix

- Potentially fixes the masked getting stuck at the main entrance on modded moons (i beleive due to a null reference exception, we shall see.)

### 0.1.4

- Fixed the bug where clients get an error saying the masked has no personality.

### 0.1.3 - the Masked "Insane" Escape Patch

- added MIT License (same as MaskedAIRevamp used)
- added the breakerbox powerboxdoor is now opened before it is used by a masked.
- added "sabotaging the apparatus" to Insane's focuses which then leads to another Focus (Escape), Apparatus focus can only happen after 2pm ingame time as it is the start of a "day ending" event.
- added "escape" focus to insane's focuses, this can only occur if the masked has completed the "Apparatus" focus.
- added variables to Imperium (v0.2.0) visualisers for the masked
- changed ai "update" timing from invoke (heavy reqs, 1 update per 0.1s) to "FixedUpdate" (light, but 1 update per 0.02s) in a bid to reduce fps losses even more.

### 0.1.2

- switched from FPS based timing to a fixed update timing of 0.1s and fixed all relevent timing issues related to this change.
- fixed some variables not updating since v0.1.1 leading to masked being unable to perform some tasks.
- fixed masked "sight" parameters to correct the sight they have so they are not seeing through the back of their skulls (well almost..)
- fixed a bug where a masked dying on the terminal prevented all other masked from using the terminal for the rest of the round

### 0.1.1

- attempted to improve the pathing to the breaker box
- prevented "Stealthy" from focusing on items as they have no logic for items.
- potentially fixed an issue with fps drops due to heavy calculations, hopefully this fix doesnt cause other issues :)

### 0.1.0

- fixed null reference exception regarding to MaskedAggressive Focus.Player when player escapes the masked.

### 0.0.9

- Added some basic background coding to help debug issues quicker while using Imperium. (for my benefit really).
- Potentially fixed an issue with item log spam due to errors with the list when a masked spawns (usually due to items being "removed" from the game completely i think))
- Made it so personalities can be turned on/off, if you turn off all personalities, the whole "Masked AI" functionality will automatically be disabled.

### 0.0.8 - Masked AI Revamp Coding Re-Write patch

- Rewritten a big chunk of the Masked AI Revamp original code, we have gone from "Personalities" only, to include "Focuses" and when there is no focus.. "Activities"
- Potentially added integration with Wendigos_Voice_Cloning by Tim_Shaw (please do test and let me know how it goes :))
- Removed some code branches which I could not fit into the new code logic branches of Personality/Focus/Activity.
- Fixed masked loving to linger at the main entrance, they should now "reposition" themselves between the MainEntrance, The ShipLocker and the BreakerBox.. I plan to add the "Apparatus" and the "FireExits" as other options in the future.
- Potentially fixed a bug where more than 1 masked will use the terminal.
- Masked now have a random chance to change focus to a nearby detected player (depends on the personality what the chance is, from something like 20% to near 100%)
- Fixed an issue where the mod would flag up as a virus on some Anti-Virus scanners due to having the word "Virus" in the AssemblyName.

### 0.0.7 - the Masked "Cunning" major fix patch

- Masked "Cunning" will now successfully steal 1 item from the ship and will no longer bring items to the ship, the stolen item will be hidden in a 'bush' on moons that have 'bushes'. may steal more items as well.
- Masked "Cunning" will tamper with the breaker box (turn it off), they may be able to turn it on if its already off.. then turn it off later.. but this doesnt 100% work yet.
- added a check to fix an issue with Masked<->Player collision that caused incompatibility with DramaMask
- Fixed the list of items which "Cunning" can use to take items from the ship
- Modified some animation selection logic to hopefully stop the masked using incorrect animations.

### 0.0.6

- Potentially fixed masked "aimbot" by stopping them look at you if they visually cant see you
- potentially fixed an issue causing masked to run slowly

### 0.0.5

- Fixing masked terminal interactions (hopefully) (joining/leaving mostly)
- Prevented a null exception related to masked picking up items

### 0.0.4

- Added more words to the selection of words for the insane to use.

### 0.0.3

- Fixed "SoftDependency" with BMX.LobbyCompatibility

### 0.0.2

- Added a new masked Personality (Insane) - may rename this later, but for now this is what it is called, the Insane will enter randomised signal translator messages to confuse/help/kill you. More functionality will come soon.
- Fixed the "Deceiving" Personality where in it seemed like it didnt actually do anything at the terminal, it will now enter terminal codes at the terminal for a period of time (a set amount of codes), there is a randomised time delay between codes.

### 0.0.1

- Initial release version number, there is no actual release, if you want this version, please head over to Piggy's MaskedAIRevamp and use version 0.2.1 -> https://thunderstore.io/c/lethal-company/p/Piggy/MaskedAIRevamp/.
