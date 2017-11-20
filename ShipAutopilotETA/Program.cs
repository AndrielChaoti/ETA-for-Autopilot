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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // If you need to change any settings about the program, this is where you do it.
        // This is the tag that my program will look for on your LCDs.
        // The default value is "[ETA]".
        public const string TagString = "[ETA]";

        // This is how long my program will wait, in seconds, until the next time it updates:
        // The default value is 0.25f, or 1/4 of a second.
        public const double ThrottleTime = 0.25f;

        /***********************************************************
         * DO NOT CHANGE ANYTHING BELOW THIS LINE UNLESS YOU KNOW
         * WHAT YOU ARE DOING! YOU COULD BREAK THINGS!
         ***********************************************************/
        private List<IMyTextPanel> gridTextPanels;
        private List<IMyRemoteControl> gridRemoteControls;

        private double timeSinceLastRun;
        private long ticks = 0;
        private long lastTicks = 0;
        private int updates = 0;
        private bool debug = false;

        public Program()
        {
            // initalize some variables.
            gridTextPanels = new List<IMyTextPanel>();
            gridRemoteControls = new List<IMyRemoteControl>();

            // Set the timeSinceLastRun to be throttleTime so that we don't
            // have to wait for the first run
            timeSinceLastRun = ThrottleTime;
            updates = 6;

            Echo("ETA script compiled successfully.");
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;

        }

        public void UpdateGrid()
        {
            // Rescan the grid for our blocks
            gridTextPanels.Clear();
            gridRemoteControls.Clear();

            GridTerminalSystem.GetBlocksOfType(gridRemoteControls, remoteControl => remoteControl.IsAutoPilotEnabled);
            GridTerminalSystem.GetBlocksOfType(gridTextPanels, textPanel => textPanel.Enabled && textPanel.CustomName.Contains(TagString));
        }

        public void Main(string argument, UpdateType updateSource)
        {
            int count = 0;
            // handle command arguments:
            if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
            {
                if (argument.ToLower().Contains("debug"))
                {
                    debug = !debug;
                }
            }

            try
            {
                if ((updateSource & UpdateType.Update100) != 0)
                {
                    // We will only update our local copy of the grid every 6 long cycles (roughly 10s)
                    updates++;
                    lastTicks = ticks;
                    ticks = 0;

                    if (updates >= 6)
                    {
                        UpdateGrid();
                        updates = 0;
                        return;
                    }
                }

                if ((updateSource & UpdateType.Update1) != 0)
                {
                    ticks++;
                    timeSinceLastRun += Runtime.TimeSinceLastRun.TotalSeconds;

                    if (gridTextPanels.Count == 0)
                    {
                        Echo("No LCDs found to update. Check to make sure your LCDs have \"" + TagString + "\"!");
                    }

                    AutopilotETA CurrentETA = new AutopilotETA(this);
                    CurrentETA.CalculateETA(gridRemoteControls, false);

                    // throttle updating LCDs.
                    
                    if (timeSinceLastRun >= ThrottleTime)
                    {
                        timeSinceLastRun = 0;

                        foreach (IMyTextPanel panel in gridTextPanels)
                        {
                            panel.ShowPublicTextOnScreen();

                            if (gridRemoteControls.Count() != 0)
                            {
                                if (CurrentETA.IsDestinationSet && !CurrentETA.IsTimeInfinite)
                                {
                                    panel.WritePublicText(String.Format("ETA: {0:g}", CurrentETA.EstimatedTime));
                                }
                                else if (CurrentETA.IsTimeInfinite)
                                {
                                    panel.WritePublicText("ETA: Stopped");
                                }
                                else
                                {
                                    panel.WritePublicText("ETA: No destination found.");
                                }
                            }
                            else
                            {
                                // Just double-check the user *has* a remote control on the grid:
                                GridTerminalSystem.GetBlocksOfType(gridRemoteControls);
                                if (gridRemoteControls.Count() == 0)
                                {
                                    panel.WritePublicText("ETA: No Remote Controls on this ship!");
                                }
                                else
                                {
                                    panel.WritePublicText("ETA: Autopilot disabled.");
                                }
                            }
                            count++;
                        }
                    }

                    Echo("ETA: " + CurrentETA.ETAStatus);
                }
            }
            catch
            {
                GridTerminalSystem.GetBlocksOfType(gridTextPanels, textPanel => textPanel.Enabled && textPanel.CustomName.Contains(TagString));
                if (gridTextPanels.Count == 0) { throw; }
                foreach (IMyTextPanel panel in gridTextPanels)
                {
                    panel.ShowPublicTextOnScreen();
                    panel.WritePublicText("A catastropic error has occured. Check the programmable block for more info.");
                    throw;
                }
            }

            if (debug)
            {
                Echo("Debug Mode ----");
                Echo("Updated " + count + " panel(s) in " + Runtime.CurrentInstructionCount + " instructions.");
                Echo("updateSource = " + updateSource.ToString());
                Echo("updates = " + updates.ToString());
                Echo("lastTicks = " + lastTicks.ToString());
                Echo("timeSinceLastRun = " + timeSinceLastRun.ToString());
                Echo(String.Format("Last run: {0:F4}ms", Runtime.LastRunTimeMs));
            }
        }
    }
}