/*
 *   R e a d m e
 *   -----------
 *   Estimated Travel Time Version 0.5 by AndrielChaoti (https://github.com/AndrielChaoti/ETA-for-Autopilot)
 *
 *  Using this script is super simple. Just make sure your ship has a Remote Control attached to it.
 *  Load up this script into a new programmable block, click "Check Code", then "Remember & Exit".
 *  Set up a timer block (Or use an existing timer block: this script works best when tied in with
 *  MMaster's Automatic LCDs 2 [http://steamcommunity.com/sharedfiles/filedetails/?id=822950976])
 *  RUNNING THE SCRIPT:
 *      - Click "Remember & Exit", this script will call on itself to keep running in the
 *          background without a timer block!
 *      - Add "[ETA]" to your desired LCD, without the quotation marks.
 *  Normally the script will only show usefil information if it finds a remote control with autopilot
 *  turned on, otherwise it will just display "Autopilot off".
 *  If you run into any errors, any tagged LCDs should print out a message telling you to check the
 *  programmable block. If this is not the case, check there anyway, it will have a -lot- more useful
 *  information!
 * 
 */