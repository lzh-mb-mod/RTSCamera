* Rescue Prisoner
* Command battle entry
  * Confirm that normal field and siege command battles still enter command mode. Repeat for the lord's hall troop selection, confirming the selection to start its battle.
  * Cancel each command battle selection, then enter a town or tournament and start an ordinary battle from the same encounter. Verify that command mode does not remain enabled and camera control works normally.
  * In a debugger, make the command battle action return without opening a mission, or throw before/after creating one. Verify that command mode is cleared when the action fails or opens no mission, and that unrelated troop-selection callbacks remain unchanged.
* Mission transitions
  * Draw order-position markers, leave the battle, and enter another battle. Verify that all marker styles and formation highlighting work in the new scene. In a debugger, repeat while skipping the previous battle's `Patch_OrderTroopPlacer.OnRemoveBehavior`; verify that no placer, entity or material from the previous battle is reused.
* Siege
  * Switch to an AI that's moving to a siege weapon;
  * Switch to an AI that's using a siege weapon;
  * Use a siege weapon and switch to free camera;
  * With formation locking enabled, queue a Face Direction order followed by another order for several formations, including one whose troops are all operating siege weapons or carrying ladders. Verify that this formation receives the facing order without locked movement, no repeated exception appears, and the queue continues. Repeat with only formations whose troops are all detached selected.
  * In a debugger, force an exception during a queued order's execution, including after its pending state is created. Verify that only the failing formation drops that order and its matching pending state, other formations still execute it, and later queued orders remain available without retrying the same failure every tick.
* Naval battle
  * In free camera, select all ships and keep the order UI open while switching to player mode. Verify that the player-controlled ship is deselected and ship orders remain available for the AI-controlled ships.