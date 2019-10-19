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
        IMySensorBlock airlockSensor;
        IMyAirVent airVent;
        IMyLightingBlock airlockLamp;
       

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;

            string outerDoorName = "AirlockOuterDoor";
            string innerDoorName = "AirlockInnerDoor";
            string sensorName = "AirlockSensor";
            string innerSensorName = "AirlockInnerSensor";
            string outerSensorName = "AirlockOuterSensor";
            string airVentName = "AirlockVent";
            string airlockLampName = "AirlockLamp";

            outerDoor = GridTerminalSystem.GetBlockWithName(outerDoorName) as IMyDoor;
            innerDoor = GridTerminalSystem.GetBlockWithName(innerDoorName) as IMyDoor;
            airlockSensor = GridTerminalSystem.GetBlockWithName(sensorName) as IMySensorBlock;
            innerSensor = GridTerminalSystem.GetBlockWithName(innerSensorName) as IMySensorBlock;
            outerSensor = GridTerminalSystem.GetBlockWithName(outerSensorName) as IMySensorBlock;
            airVent = GridTerminalSystem.GetBlockWithName(airVentName) as IMyAirVent;
            airlockLamp = GridTerminalSystem.GetBlockWithName(airlockLampName) as IMyLightingBlock;

            // Check that all mandatory entities are accounted for.
            if (outerDoor == null ||
                innerDoor == null ||
                airlockSensor == null ||
                innerSensor == null ||
                outerSensor == null ||
                airVent == null)
            {
                Echo("Airlock not found!");
            }
            else
            {
                airlockLamp.Color = Color.Red;
                outerDoor.CloseDoor();
                innerDoor.OpenDoor();
                while(outerDoor.Status != DoorStatus.Closed)
                {
                    // Wait for the doors to close.
                }
                // Disable manual opening/closing of doors.
                innerDoor.Enabled = false;
                outerDoor.Enabled = false;
                if (!airVent.CanPressurize)
                {
                    Echo("Airlock cannot pressurize!");
                }
                else
                {
                    airVent.Depressurize = false;
                    while (airVent.Status != VentStatus.Pressurizing)
                    {
                        // Wait to presssurize.
                    }
                    airlockLamp.Color = Color.Green;
                    Echo("Airlock Ready!");
                }
            }
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {

        }
    }
}
