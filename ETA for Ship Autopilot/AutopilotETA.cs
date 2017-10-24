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
    partial class Program {
        public class AutopilotETA {

            private MyGridProgram Program;

            public bool IsDestinationSet { get; internal set; }
            public bool IsTimeInfinite { get; internal set; }
            public string ETAStatus { get; internal set; }
            public TimeSpan EstimatedTime { get; internal set; }

            public AutopilotETA(MyGridProgram me) {
                Program = me;
            }

            public void CalculateETA(List<IMyRemoteControl> remoteControls, bool complexSearch) {
                // Init:
                Vector3D controllerDestination;
                Vector3D currentPosition;

                TimeSpan calculatedTime;

                foreach (IMyRemoteControl controller in remoteControls) {
                    try {
                        currentPosition = controller.GetPosition();
                        bool success = ParseControllerDestination(controller, out controllerDestination);
                        // could not find a destination
                        if (!success) {
                            IsTimeInfinite = false;
                            IsDestinationSet = false;
                            ETAStatus = "nodestination";
                            return;
                        }

                        if(complexSearch) {
                            // nop
                        } else {
                            // calculate basic ETA, making assumptions along the way:
                            try {
                                IsDestinationSet = true;
                                double distance = Vector3D.Distance(currentPosition, controllerDestination);
                                double shipVelocity = controller.GetShipSpeed();

                                if (shipVelocity == 0) {
                                    IsTimeInfinite = true;
                                    ETAStatus = "notmoving";
                                    return;
                                }

                                calculatedTime = TimeSpan.FromSeconds(distance / shipVelocity);
                                IsTimeInfinite = false;
                                EstimatedTime = calculatedTime;
                                ETAStatus = calculatedTime.ToString();
                                return;
                            } catch {
                                ETAStatus = "error";
                                return;
                            }
                        }
                    } catch (Exception e) {
                        ETAStatus = "error";
                        Program.Echo(e.Message);
                        throw;
                    }
                }
                ETAStatus = "none";
            }

            // Will return false if could not parse destination, otherwise true and a valid Vector3D representing the destination waypoint that was found.
            private bool ParseControllerDestination(IMyRemoteControl remoteControl, out Vector3D destination) {
                // Initialize:
                int start; int end;
                string rcCoordinates;
                start = remoteControl.DetailedInfo.IndexOf("{");
                end = remoteControl.DetailedInfo.IndexOf("}");

                if (start == -1 || end == -1) {
                    destination = new Vector3D();
                    return false;
                }

                rcCoordinates = remoteControl.DetailedInfo.Substring(start + 1, (end - start) - 1);

                if (rcCoordinates == "") {
                    destination = new Vector3D();
                    return false;
                }

                string[] coordinateValues = rcCoordinates.Split(' ');
                if (coordinateValues.Length != 3) {
                    destination = new Vector3D();
                    return false;
                }
                var numericCoordinate = new double[3];
                for (int i = 0; i < 3; i++) {
                    if (!Double.TryParse(coordinateValues[i].Substring(2), out numericCoordinate[i])) {
                        throw new FormatException("Could not parse " + coordinateValues[i].Substring(2) + " as double.");
                    }
                }

                destination = new Vector3D(numericCoordinate[0], numericCoordinate[1], numericCoordinate[2]);
                return true;
            }
        }
    }
}