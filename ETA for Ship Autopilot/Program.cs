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
        private TimeSpan CurrentETA;
        private double dt;

        public Program() {
            gridTextPanels = new List<IMyTextPanel>();
            gridRemoteControls = new List<IMyRemoteControl>();
            CurrentETA = new TimeSpan();
            dt = THROTTLE;
        }

        public bool GetETA(List<IMyRemoteControl> remoteControls, bool isComplexVector, out TimeSpan ETA) {
            // Init:
            Vector3D controlDestination;
            Vector3D currentPosition;

            // really there should only be one remote control :/
            foreach(IMyRemoteControl control in remoteControls) {
                try {
                    currentPosition = control.GetPosition();
                    GetControlDestinationCoordinates(control, out controlDestination);

                    if(!isComplexVector) {
                        // calculate linear ETA
                        try {
                            double distance = Vector3D.Distance(currentPosition, controlDestination);
                            double shipVelocity = control.GetShipSpeed();

                            ETA = TimeSpan.FromSeconds(distance / shipVelocity);
                            return true;
                        } catch {
                            ETA = new TimeSpan(-1);
                            return false;
                        }
                    } else {
                        throw new Exception("Not implemented.");
#pragma warning disable CS0162 // Unreachable code detected
                        ETA = new TimeSpan();
#pragma warning restore CS0162 // Unreachable code detected
                        return true;
                    }

                } catch(Exception e) {
                    Echo(e.Message);
                    Echo(e.StackTrace);
                    throw;
                }
            }
            // failure case:
            ETA = new TimeSpan();
            return false;
        }

        public bool GetControlDestinationCoordinates(IMyRemoteControl control, out Vector3D destination) {
            // get current waypoint from the remote control's text:
            int start; int end;
            string coordinates;

            start = control.DetailedInfo.IndexOf("{");
            end = control.DetailedInfo.IndexOf("}");

            if(start == -1 || end == -1) {
                destination = new Vector3D(0, 0, 0);
                return false;
            }

            coordinates = control.DetailedInfo.Substring(start, start - end);
            if(coordinates == "") {
                destination = new Vector3D(0, 0, 0);
                return false;
            }

            string[] values = coordinates.Split(' ');
            if(values.Length != 3) {
                destination = new Vector3D(0, 0, 0);
                return false;
            }


            var coord = new double[3];
            for(int i = 0; i < 3; i++) {
                if(!Double.TryParse(values[i].Substring(2), out coord[i])) {
                    throw new FormatException("Could not parse " + values[i] + " as double.");
                }
            }
            destination = new Vector3D(coord[0], coord[1], coord[2]);
            return true;
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

                foreach(IMyTextPanel panel in gridTextPanels) {
                    panel.ShowPublicTextOnScreen();

                    if(gridRemoteControls.Count() != 0) {

                        if(GetETA(gridRemoteControls, false, out CurrentETA)) {
                            // ETA is found
                            panel.WritePublicText(String.Format("ETA: {0:g}", CurrentETA));
                        } else if(GetETA(gridRemoteControls, false, out CurrentETA) && CurrentETA.Ticks == -1) {
                            // ETA is infinite
                            panel.WritePublicText("ETA: Infinity");
                        } else {
                            // No ETA found: autopilot off or no RCs.
                            panel.WritePublicText("Autopilot off.");
                        }
                    } else {
                        GridTerminalSystem.GetBlocksOfType(gridRemoteControls);
                        if(gridRemoteControls.Count() == 0) {
                            panel.WritePublicText("No remote controls on this ship!");
                        } else {
                            panel.WritePublicText("Autopilot is turned off.");
                        }
                    }
                }

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