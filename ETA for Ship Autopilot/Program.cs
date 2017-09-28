using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        // This is the tag that my program will look for on your LCDs.
        // The default value is "[ETA]".
        public const String TAG_STR = "[ETA]";

        // This is how long my program will wait, in seconds, until the next time it updates:
        // The default value is 0.25f, or 1/4 of a second.
        public const double THROTTLE = 0.25f;

        /***********************************************************
         * DO NOT CHANGE ANYTHING BELOW THIS LINE UNLESS YOU KNOW
         * WHAT YOU ARE DOING! YOU COULD BREAK THINGS!
         ***********************************************************/
        private List<IMyTextPanel> gridTextPanels;
        private List<IMyRemoteControl> gridRemoteControls;

        private double dt;

        public Program() {
            gridTextPanels = new List<IMyTextPanel>();
            gridRemoteControls = new List<IMyRemoteControl>();
            dt = THROTTLE;
        }

        public void Main() {
            try {
                // Throttle script updates, as this script is supposed to be a tie-in, not usually running entirely on it's own.
                if(dt < THROTTLE) {
                    dt += Runtime.TimeSinceLastRun.TotalSeconds;
                    return;
                }
                dt = 0;

                GridTerminalSystem.GetBlocksOfType(gridRemoteControls, remoteControl => remoteControl.IsAutoPilotEnabled);
                GridTerminalSystem.GetBlocksOfType(gridTextPanels, textPanel => textPanel.Enabled && textPanel.CustomName.Contains(TAG_STR));

                if(gridTextPanels.Count == 0) {
                    Echo("No LCDs found to update. Check to make sure your LCDs have \"" + TAG_STR + "\"!");
                }

                AutopilotETA CurrentETA = new AutopilotETA(this);
                CurrentETA.CalculateETA(gridRemoteControls, false);

                int count = 0;
                foreach(IMyTextPanel panel in gridTextPanels) {
                    panel.ShowPublicTextOnScreen();

                    if(gridRemoteControls.Count() != 0) {
                        if(CurrentETA.IsDestinationSet && !CurrentETA.IsTimeInfinite) {
                            panel.WritePublicText(String.Format("ETA: {0:g}", CurrentETA.EstimatedTime));
                        } else if(CurrentETA.IsTimeInfinite) {
                            panel.WritePublicText("ETA: Stopped");
                        } else {
                            panel.WritePublicText("ETA: No destination found.");
                        }
                    } else {
                        // Just double-check the user *has* a remote control on the grid:
                        GridTerminalSystem.GetBlocksOfType(gridRemoteControls);
                        if(gridRemoteControls.Count() == 0) {
                            panel.WritePublicText("ETA: No Remote Controls on this ship!");
                        } else {
                            panel.WritePublicText("ETA: Autopilot disabled.");
                        }
                    }
                    count++;
                }
                Echo("ETA: " + CurrentETA.ETAStatus);
                Echo(String.Format("Last run: {0:F4}ms", Runtime.LastRunTimeMs));
                Echo("Updated " + count + " panel(s) in " + Runtime.CurrentInstructionCount + " instructions.");
            } catch {
                GridTerminalSystem.GetBlocksOfType(gridTextPanels, textPanel => textPanel.Enabled && textPanel.CustomName.Contains(TAG_STR));
                if(gridTextPanels.Count == 0) { throw; }
                foreach(IMyTextPanel panel in gridTextPanels) {
                    panel.ShowPublicTextOnScreen();
                    panel.WritePublicText("A catastropic error has occured. Check the programmable block for more info.");
                    throw;
                }
            }
        }
    }
}