* Rescue Prisoner
* Siege
  * Switch to an AI that's moving to a siege weapon;
  * Switch to an AI that's using a siege weapon;
  * Use a siege weapon and switch to free camera;
  * On Bannerlord v1.5.6, issue use and stop-using orders for a siege weapon, both directly and with Shift to queue them. Verify that the weapon name appears in the order descriptions without errors, including in free camera after the player is knocked out.
  * With formation locking enabled, queue a Face Direction order followed by another order for several formations, including one whose troops are all operating siege weapons or carrying ladders. Verify that this formation receives the facing order without locked movement, no repeated exception appears, and the queue continues. Repeat with only formations whose troops are all detached selected.
  * In a debugger, force an exception during a queued order's execution, including after its pending state is created. Verify that only the failing formation drops that order and its matching pending state, other formations still execute it, and later queued orders remain available without retrying the same failure every tick.
* Naval battle
  * In free camera, select all ships and keep the order UI open while switching to player mode. Verify that the player-controlled ship is deselected and ship orders remain available for the AI-controlled ships.
