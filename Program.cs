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
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyDoor> innerDoors = new List<IMyDoor>();
        List<IMyDoor> outerDoors = new List<IMyDoor>();
        List<IMyAirVent> airvents = new List<IMyAirVent>();
        List<IMySensorBlock> innerSensors = new List<IMySensorBlock>();
        List<IMySensorBlock> outerSensors = new List<IMySensorBlock>();
        List<IMyInteriorLight> lights = new List<IMyInteriorLight>();

        enum AirlockState
        {
            init,
            idle,
            done,
            openingInner,
            closingInner,
            openingOuter,
            closingOuter,
            pressurizing,
            depressurizing
        }

        List<AirlockState> currentState = new List<AirlockState>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            bool initOk = true;

            List<IMyBlockGroup> airlockGroups = new List<IMyBlockGroup>();
            List<IMyBlockGroup> allGroups = new List<IMyBlockGroup>();
            List<IMyDoor> doors = new List<IMyDoor>();
            List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<IMyAirVent> vents = new List<IMyAirVent>();
            List<IMyInteriorLight> lamps = new List<IMyInteriorLight>();

            GridTerminalSystem.GetBlockGroups(allGroups);

            foreach (IMyBlockGroup group in allGroups)
            {
                if (group.Name.Contains("[Airlock]"))
                {
                    airlockGroups.Add(group);
                }
            }
            foreach (IMyBlockGroup group in airlockGroups)
            {
                doors.Clear();
                sensors.Clear();
                vents.Clear();
                lamps.Clear();
                group.GetBlocksOfType(doors);
                group.GetBlocksOfType(sensors);
                group.GetBlocksOfType(vents);
                group.GetBlocksOfType(lamps);


                if (!(GetInnerDoor(doors) && GetOuterDoor(doors) && GetInnerSensor(sensors) && GetOuterSensor(sensors) && GetAirVent(vents)))
                {
                    Echo($"Error in group {group.Name}");
                    initOk = false;
                    break;
                }

                // Lights are optional.
                GetLight(lamps);

                currentState.Add(new AirlockState());
            }
            foreach (IMySensorBlock innerSensor in innerSensors)
            {
                innerSensor.PlayProximitySound = false;
                innerSensor.LeftExtend = 0;
                innerSensor.RightExtend = 0;
                innerSensor.TopExtend = 0;
                innerSensor.BottomExtend = 3;
                innerSensor.FrontExtend = 2.5f;
                innerSensor.BackExtend = 3;
            }
            foreach (IMySensorBlock outerSensor in outerSensors)
            {
                outerSensor.PlayProximitySound = false;
                outerSensor.LeftExtend = 0;
                outerSensor.RightExtend = 0;
                outerSensor.TopExtend = 0;
                outerSensor.BottomExtend = 3;
                outerSensor.FrontExtend = 2.5f;
                outerSensor.BackExtend = 3;
            }

            Echo($"innerDoors:{innerDoors.Count()}");
            Echo($"outerDoors:{outerDoors.Count()}");
            Echo($"airvents:{airvents.Count()}");
            Echo($"innerSensors:{innerSensors.Count()}");
            Echo($"outerSensors:{outerSensors.Count()}");
            Echo($"lights:{lights.Count()}");
            Echo($"states:{currentState.Count()}");
            if (initOk == true)
            {
                Echo("Airlock setup OK!");
            }
            else
            {
                Echo("Failed to initialize, script halted");
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }
        public void Save()
        {
        }
        public void Main(string argument, UpdateType updateSource)
        {
            for (int i=0; i < currentState.Count; i++)
            {
                if (AirlockReady(i))
                {
                    switch (currentState[i])
                    {
                        case AirlockState.done:
                            innerDoors[i].Enabled = false;
                            outerDoors[i].Enabled = false;
                            lights[i].Color = Color.Green;
                            currentState[i] = AirlockState.idle;
                            break;
                        case AirlockState.openingOuter:
                            outerDoors[i].Enabled = true;
                            outerDoors[i].OpenDoor();
                            currentState[i] = AirlockState.done;
                            break;
                        case AirlockState.closingOuter:
                            outerDoors[i].Enabled = true;
                            outerDoors[i].CloseDoor();
                            currentState[i] = AirlockState.pressurizing;
                            break;
                        case AirlockState.openingInner:
                            innerDoors[i].Enabled = true;
                            innerDoors[i].OpenDoor();
                            currentState[i] = AirlockState.done;
                            break;
                        case AirlockState.closingInner:
                            innerDoors[i].Enabled = true;
                            innerDoors[i].CloseDoor();
                            currentState[i] = AirlockState.depressurizing;
                            break;
                        case AirlockState.pressurizing:
                            innerDoors[i].Enabled = false;
                            outerDoors[i].Enabled = false;
                            airvents[i].Depressurize = false;
                            currentState[i] = AirlockState.openingInner;
                            break;
                        case AirlockState.depressurizing:
                            innerDoors[i].Enabled = false;
                            outerDoors[i].Enabled = false;
                            airvents[i].Depressurize = true;
                            currentState[i] = AirlockState.openingOuter;
                            break;
                        case AirlockState.init:
                            lights[i].Color = Color.Red;
                            innerDoors[i].Enabled = true;
                            outerDoors[i].Enabled = true;
                            innerDoors[i].OpenDoor();
                            outerDoors[i].CloseDoor();
                            currentState[i] = AirlockState.done;
                            break;
                        case AirlockState.idle:
                            if (innerSensors[i].IsActive)
                            {
                                if (outerDoors[i].Status == DoorStatus.Open)
                                {
                                    lights[i].Color = Color.Red;
                                    currentState[i] = AirlockState.closingOuter;
                                }
                            }
                            else if (outerSensors[i].IsActive)
                            {
                                if (innerDoors[i].Status == DoorStatus.Open)
                                {
                                    lights[i].Color = Color.Red;
                                    currentState[i] = AirlockState.closingInner;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the airlock is ready.
        /// </summary>
        /// <returns>True if ready, false if busy.</returns>
        private bool AirlockReady(int index)
        {
            if ((outerDoors[index].Status == DoorStatus.Opening) ||
                (outerDoors[index].Status == DoorStatus.Closing) ||
                (innerDoors[index].Status == DoorStatus.Opening) ||
                (innerDoors[index].Status == DoorStatus.Closing))
            {
                return false;
            }
            return true;
        }
        bool GetInnerDoor(List<IMyDoor> list)
        {
            string name = "InnerDoor";
            if (!list.Exists(x => x.CustomName.Contains(name)))
            {
                Echo($"{name} not found.");
                return false;
            }
            innerDoors.Add(list.Find(x => x.CustomName.Contains(name)));
            return true;
        }
        bool GetOuterDoor(List<IMyDoor> list)
        {
            string name = "OuterDoor";
            int i = list.FindIndex(x => x.CustomName.Contains(name));
            if (i < 0)
            {
                Echo($"{name} not found.");
                return false;
            } 
            outerDoors.Add(list[i]);
            return true;
        }
        bool GetInnerSensor(List<IMySensorBlock> list)
        {
            string name = "InnerSensor";
            if (!list.Exists(x => x.CustomName.Contains(name)))
            {
                Echo($"{name} not found.");
                return false;
            }
            innerSensors.Add(list.Find(x => x.CustomName.Contains(name)));
            return true;
        }
        bool GetOuterSensor(List<IMySensorBlock> list)
        {
            string name = "OuterSensor";
            if (!list.Exists(x => x.CustomName.Contains(name)))
            {
                Echo($"{name} not found.");
                return false;
            }
            outerSensors.Add(list.Find(x => x.CustomName.Contains(name)));
            return true;
        }
        bool GetLight(List<IMyInteriorLight> list)
        {
            string name = "Light";
            if (!list.Exists(x => x.CustomName.Contains(name)))
            {
                Echo($"{name} not found.");
                return false;
            }
            lights.Add(list.Find(x => x.CustomName.Contains(name)));
            return true;
        }
        bool GetAirVent(List<IMyAirVent> list)
        {
            string name = "AirVent";
            if (!list.Exists(x => x.CustomName.Contains(name)))
            {
                Echo($"{name} not found.");
                return false;
            }
            airvents.Add(list.Find(x => x.CustomName.Contains(name)));
            return true;
        }
    }
}
