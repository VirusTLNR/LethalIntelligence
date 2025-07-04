## Changelog

### 0.4.8
- fixed item animations and held positions to use original player animations (instead of the old MaskedAIRevamp ones), positions and rotations, all items should now be supported in regards to masked holding them properly when they pick them up. Let me know if any item a masked holds seemed weird, and i will look into if it looks weird!

### 0.4.7
- fixed some more "CalculatePolygonPath" can only be called on an active agent that has been placed on a NavMesh" errors
- fixed a potentially vanilla bug (origin is unknown) which causes fire exit 1 (outside) to lead to fire exit 2/3 (inside), which then means when you leave via the same fire exit, you are not in the right location. (this bug is nothing to do with LI, i just needed to fix it myself as masked use fire exits, it will fix it for masked and players). Other fixes using vanilla methods will not interfere with this fix.

### 0.4.6
- fixed Mineshaft Elevator issues so masked can now leave and enter via the Main Entrance
- fixed an error where my entrances code ran too early so entrances were throwing errors due to them not being found leading to the game to freeze during level loading.
- fixed an audio bug when a masked uses an entrance (previously the audio sound would always play on the main entrance, now it plays on the entrance used, on both sides of the entrance (like when players use them))

### 0.4.5
- Improved masked's usage of stamina to prevent graphical issues due to wrong animations being used.
- fixed some (cant fix all as this shouldent be a thing) error spam when masked are spawned in orbit.
- fixed a compatibility issue with ButteryFixes (and perhaps also with Mimics and Lethal Things)
- fixed a lot of error spam relating to non-hosts setting destination on the masked NavmeshAgent
- fixed a null reference error when picking up walkies, also fixed an error when pathing to walkies
- fixed dropping of items on shipleave (as well as tweaked how item dropping works)

### 0.4.4 - Hotfix
- fixed masked killing client players
- added extra logic around masked chasing players who exit the facility
- improved entrance teleport checks to ignore moons with no interior (Company & Galentry at the moment, but should apply to any moon where the interior does not exist)
- reduced log spam (and so hopefully some lag) when masked cannot find an item to pick up and keep trying to by delaying their "re-attempt" until the next ingame hour (and so if it is not the next ingame hour, they should do something else)

### 0.4.3 - The 'Anti-MP' Patch
- prevented error spam when "apparatus" is null (usually on the mansion interior)
- prevented an error message at start of the day due to items being null
- instead of syncing "masked" position and rotation myself, ive switched to instead using agent position and rotation.
- disabled syncing of "in special animation" bool as it was causing issues, will fix in future.
- improved player detection, selection, following and losing track of players.
- made sure that masked are "collidable" (ie, so they can kill you) majority of the time. only real exceptions is while they are "using" the terminal/breaker/apparatus.

### 0.4.2 - Hotfix
- fixed an issue where a masked spotting a player at spawn would cause "-1" value for focus/activity there in breaking their logic.
- fixed an issue where a mod breaks mirage, which then breaks LI.
- hopefully improved FPS a bit by moving calculations from FixedUpdate to LateUpdate

### 0.4.1
- fixed all remaining networking issues in relation to the terminal (deceiving/cunning/insane), everything should be 100% synced now (including signal translator messages).
- fixed all remaining networking issues with sabotaging the apparatus
- fixed a position recognition issue at the start of the round leading to bad decisions
- cleaned up some code thats no longer needed as well as fixed an error spamming due to host only code running on the client when going to the breaker box.
- modified some distance checking code
- temporarily disabled Aggressive's ability to pick up weapons as Aggressive holding a weapon is unable to kill you currently. (will fix soon :))

### 0.4.0 - Hotfix
- fixed another thing that was making masked look upwards.
- improved entrance teleports checks to ignore Melanie's furniture "pocket room" entrance and mark it as invalid automatically. if melanie's "ship" fire exit is placed, it should be valid with mode 0, but will be invalid with mode 1/2 as i dont check individual sides of an entrance.
- added a lot of null reference checks to the entrance teleport checking + using processes.
- added a proper bepindependency for LethalNetworkAPI

### 0.3.9 - Hotfix
- improved validation of EntranceTeleports by being less strict on some constraints to be in line with previous existing code that works fine. (bringing the new automatic code and the old manual code, more in line with each other)
- added a new idle mode to prevent log spam when all entrances are unavailable, as well as to give a "fall back" mode to masked who have nothing to do, this currently makes masked walk from 5f to 80f away from the main entrance in an attempt to find players, if they find a player they will exit idle mode. They will not enter/exit the interior in idle mode.
- added a null reference check to prevent an exception regarding entrance teleports, this will now instead lead to "idle mode" if this occurs.
- improved logging when masked spawns and debug mode is turned off

### 0.3.8 - Hotfix
- fixed an issue with routing to the company spamming an error about the main entrance being null in 0.3.7

### 0.3.7
- automated detection of bad MainEntrances/FireExit's so the masked do not use them (previously I was trying to add these manually)
- prevented a null reference exception in regards to the NavMeshAgent being null

### 0.3.6 - The Grand Re-Networking Patch #2 of 2
- TargetPlayer networking issues fixed
- item pickup networking issues fixed
- item dropping networking issues fixed
- fixed some bad logging since 0.3.4 (mostly the output of some logs was wrong due to variable changes not being correctly displayed in the logs)
- simplified entrance teleport coding.

### 0.3.5 - Hotfix
- fixed the change in 0.3.4 that made masked status reports spammy.

### 0.3.4 - The Grand Re-Networking Patch #1 of 2
- Positioning, Rotation and similar "basic" networking issues fixed.
- Breakerbox networking issues fixed
- Apparatus networking issues fixed
- Terminal networking issues fixed
- ShipLever networking issues fixed
- items seem to not get picked up by masked, I will fix issues with items in the next patch just ran out of time today and wanted to get something out the door.

### 0.3.3 - Hotfix (Please Note:- in this version and previous versions... networking was broken, i would reccommend only using for SOLO PLAY if for some reason you want an old version!)
- Fixed (hopefully) compatibility with the new version (1.14) of Mirage.

### 0.3.2
- (dev/debug) fixed and modified Visualisers for masked when using imperium
- fixed item dropping when masked die carrying an item
- added some null checks to masked using entrances passivly
- disabled vanilla "random turning" for masked so they hopefully look more "player" like in how they move. (this should also reduce instances of them "looking upwards".. maybe even prevents it)
- updated entranceteleport filters to mitigate against masked using "unusable" entrances due to missing navmesh/offnavmeshlinks from how moons/interiors are designed.
- prevented a softlock if you pull the ships lever while a masked is using the terminal
- fixed an error where no obj codes were found when deceiving was using the terminal

### 0.3.1 - Hotfix
- fixed issues with routing to the Apparatus
- fixed issues with routing to the BreakerBox
- fixed issues with routing to the MainEntrance and FireExits

### 0.3.0 - Hotfix
- fixed an issue with moons/interiors that dont use the actual door as the EntranceTeleport game object leading to masked standing at the doors.

### 0.2.9 - Hotfix
- changed Main Entrance/Fire Exit usage by the LI masked to be like vanilla masked usage (passive, used when the masked are at the door itself) so even if they bug out and stand at the entrance, they should use the entrance and change what they are doing to stop them lingering there too long.

### 0.2.8
- prevented softlocks relating to masked using the terminal (if you find yourself unable to use the terminal when no masked is there or similar, do let me know! shouldent happen though as i have tested all live game scenarios i think..)
- added audio cues for when masked is typing on the terminal keyboard, as well as an audio cue for cunning "purchasing items"
- improved walking to the terminal so it looks a bit less "snappy" when the masked turns.
- (for debugging only) modified some start variables which were previously wrong. (didnt affect anything luckily but nice to get them correct :))

### 0.2.7 - Hotfix
- fixed the issue with masked routing to and getting stuck at the main entrance
- 99% fixed masked positioning when using the terminal (may see some levitation with 2story ship and may see some sinking in the vanilla ship, this is down to the navmesh, not my mod), but at least masked should be at the terminal now. - please do send pictures of bad positions/levitating/sinking/etc though so I can maybe look into making a fix.

### 0.2.6
- temporarily fixed an issue with the main entrance when using the "LiminalPools" interior (v1.0.12) as this causes masked to bug out due to a missing NavMeshLink, once LiminalPools is updated, this issue should be resolved. Please note, this fix ignores the main entrance in this scenario.
- improved how masked "react" to arriving at their destination for some focus's (breakerbox/apparatus) to hopefully prevent them from bugging out due to bad configuration.
- lowered the proximity requirement for masked activities (breaker/apparatus) to potentially stop them bugging out when they cant reach close enough. (this may need revisiting as im not sure it has fixed it and is most likely the reason they bug out now)

### 0.2.5
- Potentially fixed routing problems which led to them standing at the main entrance (or in the ship) weirdly. (again x.x)
- fixed an issue with "randomitem" function throwing errors
- improved some logging for debugging purposes

### 0.2.4
- Fixed routing problems when Insane is doing "Sabotage/Escape" focus which led to them standing at the main entrance (or in the ship) weirdly.
- Fixed "findRandomItem" invalid cast exception
- removed some spammy debug logs
- (debug)improved some "masked goal" messages in the status reports
- force updated some distances that were causing delayed responses when masked performed certain actions.

### 0.2.3
- Improved the walkie talkie audio so that only the person holding the walkie hears the voice of the masked
- Added support for 'AlwaysHearActiveWalkies' mod when masked use walkies. (having "AlwaysHearActiveWalkies" installed makes the masked voice play out loud to all those around the walkie)
- Fixed Insane when they Sabotage the Apparatus and Escape. (they were seemingly running between main entrance and fire exits due to badly modified logic)

### 0.2.2
- Potentially fixed a null reference exception when the masked sabotage the apparatus (only seen once on Liminal Pools (i think) so far, hopefully never again :))
- potentially fixed masked standing at the main entrance and fire exits, as well as standing in the ship staring at the monitors (thanks vanilla code for not doing as expected)
- potentially fixed a null reference exception when a walkie is picked up
- added new Activity (findRandomItem)
- fixed some null reference errors in the masked status report
 
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
