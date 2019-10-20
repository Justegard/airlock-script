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
        IMyDoor outerDoor;
        IMyDoor innerDoor;

        IMySensorBlock innerSensor;
        IMySensorBlock outerSensor;
        IMyAirVent airVent;
        IMyLightingBlock airlockLamp;

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

        AirlockState currentState;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            currentState = AirlockState.init;

            string outerDoorName = "AirlockOuterDoor";
            string innerDoorName = "AirlockInnerDoor";
            string innerSensorName = "AirlockInnerSensor";
            string outerSensorName = "AirlockOuterSensor";
            string airVentName = "AirlockVent";
            string airlockLampName = "AirlockLamp";

            outerDoor = GridTerminalSystem.GetBlockWithName(outerDoorName) as IMyDoor;
            innerDoor = GridTerminalSystem.GetBlockWithName(innerDoorName) as IMyDoor;
            innerSensor = GridTerminalSystem.GetBlockWithName(innerSensorName) as IMySensorBlock;
            outerSensor = GridTerminalSystem.GetBlockWithName(outerSensorName) as IMySensorBlock;
            airVent = GridTerminalSystem.GetBlockWithName(airVentName) as IMyAirVent;
            airlockLamp = GridTerminalSystem.GetBlockWithName(airlockLampName) as IMyLightingBlock;

            // Check that all mandatory entities are accounted for.
            if (outerDoor == null ||
                innerDoor == null ||
                innerSensor == null ||
                outerSensor == null ||
                airVent == null)
            {
                Echo("Airlock not found!");
            }

            innerSensor.PlayProximitySound = false;
            innerSensor.LeftExtend = 0;
            innerSensor.RightExtend = 0;
            innerSensor.TopExtend = 0;
            innerSensor.BottomExtend = 3;
            innerSensor.FrontExtend = 2.5f;
            innerSensor.BackExtend = 3;

            outerSensor.PlayProximitySound = false;
            outerSensor.LeftExtend = 0;
            outerSensor.RightExtend = 0;
            outerSensor.TopExtend = 0;
            outerSensor.BottomExtend = 3;
            outerSensor.FrontExtend = 2.5f;
            innerSensor.BackExtend = 3;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (AirlockReady())
            {
                switch (currentState)
                {
                    case AirlockState.done:
                        innerDoor.Enabled = false;
                        outerDoor.Enabled = false;
                        airlockLamp.Color = Color.Green;
                        currentState = AirlockState.idle;
                        break;
                    case AirlockState.openingOuter:
                        outerDoor.Enabled = true;
                        outerDoor.OpenDoor();
                        currentState = AirlockState.done;
                        break;
                    case AirlockState.closingOuter:
                        outerDoor.Enabled = true;
                        outerDoor.CloseDoor();
                        currentState = AirlockState.pressurizing;
                        break;
                    case AirlockState.openingInner:
                        innerDoor.Enabled = true;
                        innerDoor.OpenDoor();
                        currentState = AirlockState.done;
                        break;
                    case AirlockState.closingInner:
                        innerDoor.Enabled = true;
                        innerDoor.CloseDoor();
                        currentState = AirlockState.depressurizing;
                        break;
                    case AirlockState.pressurizing:
                        innerDoor.Enabled = false;
                        outerDoor.Enabled = false;
                        airVent.Depressurize = false;
                        currentState = AirlockState.openingInner;
                        break;
                    case AirlockState.depressurizing:
                        innerDoor.Enabled = false;
                        outerDoor.Enabled = false;
                        airVent.Depressurize = true;
                        currentState = AirlockState.openingOuter;
                        break;
                    case AirlockState.init:
                        airlockLamp.Color = Color.Red;
                        innerDoor.Enabled = true;
                        outerDoor.Enabled = true;
                        innerDoor.OpenDoor();
                        outerDoor.CloseDoor();
                        currentState = AirlockState.done;
                        break;
                    case AirlockState.idle:
                        if (innerSensor.IsActive)
                        {
                            if (outerDoor.Status == DoorStatus.Open)
                            {
                                airlockLamp.Color = Color.Red;
                                currentState = AirlockState.closingOuter;
                            }
                        }
                        else if (outerSensor.IsActive)
                        {
                            if (innerDoor.Status == DoorStatus.Open)
                            {
                                airlockLamp.Color = Color.Red;
                                currentState = AirlockState.closingInner;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if the airlock is ready.
        /// </summary>
        /// <returns>Tru if ready, false if busy.</returns>
        private bool AirlockReady()
        {
            if ((outerDoor.Status == DoorStatus.Opening) ||
                (outerDoor.Status == DoorStatus.Closing) ||
                (innerDoor.Status == DoorStatus.Opening) ||
                (innerDoor.Status == DoorStatus.Closing))
            {
                return false;
            }
            return true;
        }
    }
}
