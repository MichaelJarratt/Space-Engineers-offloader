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
        //##configuration##
        String meName = "Cargo Rover OrePull"; //name of the programmable block executing the scipt
        String debugPanelName = "Cargo Rover Offloader Debug Panel"; //name of the LCD that this script writes debug info to (screen must be set to display text/images)

        bool pullFromTop = true; //set to false if you don't want to pull items out of a connector
        String connectorInName = "Cargo Rover Connector Roof"; //name of connector that miners will unload to
        
        bool pushFromRear = true; //set to false if you don't want to push items out of rear connector
        String connectorOutName = "Cargo Rover Connector Rear"; //name of connectors to offload inventory into base
        List<String> pushInto = new List<String>() //names of containers to push items into
        {   //leave empty to allow any container
            //names of container to put items into e.g. "small cargo container 1"
            "Large Cargo Container"
        };
        //##!configuration##

        IMyShipConnector connectorIn; //the connector an offloading miner will connect to
        IMyShipConnector connectorOut; //the connector the transporter will connect to to offload items
        List<IMyInventory> inventories = new List<IMyInventory>(); //list of conveyor-connected inventories on the grid
        List<IMyInventory> connectedInventories = new List<IMyInventory>();
        IMyProgrammableBlock me; //reference to self
        IMyTextPanel debug; //debug output

        public Program()
        {
            Echo("all is cool and good");
            me = GridTerminalSystem.GetBlockWithName(meName) as IMyProgrammableBlock; //programmable block running script
            connectorIn = GridTerminalSystem.GetBlockWithName(connectorInName) as IMyShipConnector;
            connectorOut = GridTerminalSystem.GetBlockWithName(connectorOutName) as IMyShipConnector;
            debug = GridTerminalSystem.GetBlockWithName(debugPanelName) as IMyTextPanel; //debug output
            debug.WriteText(""); //Clear debug
            populateInventoriesList(); //populates list with local inventories

            Runtime.UpdateFrequency = UpdateFrequency.Update100; //program runs every 100 ticks
            //debugging stuff
            debug.WriteText("" + inventories.Count);
        }

        public void Main()
        {
            if (pullFromTop) //is the pull from top feature is enabled
            {
                if (connectorIn.Status == MyShipConnectorStatus.Connected) //if docking port is connected to something
                {
                    populateConnectedInventriesList(); //repopulate it as it updates the contents
                    debug.WriteText("ship connected. Inventories: " + connectedInventories.Count);
                    tryMoveItemsFromDockedToLocal(); //attemps to move items from the connected ships inventories to the rover
                }
                else //docking port is not connected
                {
                    if (connectedInventories.Count != 0) //have only just disconnected
                    {
                        connectedInventories.Clear(); //empty list as it is no longer valid
                    }
                    debug.WriteText("No ship connected");
                }
            }

            if(pushFromRear) //if the push from rear feature is enabled
            { 
                if(connectorOut.Status == MyShipConnectorStatus.Connected) //if rear connector is connected to something
                {
                    if(pushInto.Count == 0) //if any inventory can be pushed into
                    {
                        populateConnectedInventriesList(); //will probably cause conflicts if there is something attatched to the docking connector too, oh well
                    }
                    else
                    {
                        connectedInventories = getSpecificInventories(pushInto); //populates inventories list with containers in "pushInto"
                    }
                    tryMoveItemsFromLocalToConnected(); //move items from local inventories to the connected ones specified in "pushInto"
                }
                else //rear connector not connected to anything
                {
                    if (connectedInventories.Count != 0) //have only just disconnected
                    {
                        connectedInventories.Clear(); //empty list as it is no longer valid
                    }
                }
            }

        }

        /* attemps to move items from the connected grids inventories to the rover
         * will iterate over each accessable inventory in the connected grid
         * for each inventory that is not empty, it will get a list of the items inside
         * for each item, it will iterate over every accessable local inventory (on the grid this script is running on)
         * for each local inventory that isn't full, try to move the whole stack in (will move as much as possible of the whole stack can't be taken)
         */
        public void tryMoveItemsFromDockedToLocal()
        {
            foreach(IMyInventory connectedInv in connectedInventories) //for each connected inv
            {
                if(connectedInv.ItemCount != 0) //if the inventory has items in it
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>(); //holds all items from the inventory
                    connectedInv.GetItems(items); //gets all items in the inventory
                    foreach (MyInventoryItem item in items) //iterate over each item in the inventory, typically just an ore + stone
                    {
                        foreach (IMyInventory localInv in inventories) //for each local inventory
                        {
                            if(!localInv.IsFull) //if local inventory can accept more
                            {
                                connectedInv.TransferItemTo(localInv, item); //transfer as much as possible (nothing if inventory is full)
                            }
                        }
                    }
                }
            }
        }
        public void tryMoveItemsFromLocalToConnected()
        {
            foreach(IMyInventory localInv in inventories) //for each local inv
            {
                if(localInv.ItemCount != 0) //if the inventory has items in it
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>(); //hold items from the localInv
                    localInv.GetItems(items); //gets all items in the inventory
                    foreach(MyInventoryItem item in items) //iterate over each item in the inventory
                    {
                        foreach(IMyInventory connectedInv in connectedInventories) //for each connected inventory
                        {
                            if(!connectedInv.IsFull) //if connectedInv can accept more
                            {
                                localInv.TransferItemTo(connectedInv, item); //transfer as much as possible (nothing if inventory is full)
                            }

                        }
                    }
                }
            }
        }

        //returns list of accessable non-local inventories belonging to blocks in "containers"
        public List<IMyInventory> getSpecificInventories(List<String> containers)
        {
            List<IMyTerminalBlock> blocksWithInventory = getBlocksWithInventory(ThisGrid.nonLocal, containers); //get connected blocks with inventories with names in "containers"
            return getAccessableInventories(blocksWithInventory); //returns list of inventories that belong to these blocks which can be accessed by the local grid
        }

        //populates connectedInventories list
        public void populateConnectedInventriesList()
        {
            List<IMyTerminalBlock> blocksWithInventory = getBlocksWithInventory(ThisGrid.nonLocal); //get connected blocks with inventories
            connectedInventories = getAccessableInventories(blocksWithInventory); //populates "inventories" field with the accessable inventories of blocks
        }

        //initialisation

        //populates inventories list
        public void populateInventoriesList()
        {
            List<IMyTerminalBlock> blocksWithInventory = getBlocksWithInventory(ThisGrid.local); //get local blocks with inventories
            inventories = getAccessableInventories(blocksWithInventory); //populates "inventories" field with the accessable inventories of blocks
        }

        //returns a list of blocks with inventories, gets local inventories if ThisGrid.local, or connected inventories if ThisGrid.nonLocal
        public List<IMyTerminalBlock> getBlocksWithInventory(ThisGrid grid)
        {
            List<IMyTerminalBlock> blocksWithInventory = new List<IMyTerminalBlock>(); //will hold the returned blocks
            if (grid == ThisGrid.local) //get blocks from this grid
            {
                GridTerminalSystem.GetBlocksOfType<IMyEntity>(blocksWithInventory, block => block.InventoryCount > 0 && block.CubeGrid == me.CubeGrid);
            }
            else if(grid == ThisGrid.nonLocal) //get blocks from connected grid
            {
                GridTerminalSystem.GetBlocksOfType<IMyEntity>(blocksWithInventory, block => block.InventoryCount > 0 && block.CubeGrid != me.CubeGrid);
            }
            return blocksWithInventory;
        }
        //same as above, but only gets containers in the "containers" list
        public List<IMyTerminalBlock> getBlocksWithInventory(ThisGrid grid, List<String> containers)
        {
            List<IMyTerminalBlock> blocksWithInventory = new List<IMyTerminalBlock>(); //will hold the returned blocks
            if (grid == ThisGrid.local) //get blocks from this grid
            {
                GridTerminalSystem.GetBlocksOfType<IMyEntity>(blocksWithInventory, block => block.InventoryCount > 0 && block.CubeGrid == me.CubeGrid && containers.Contains(block.CustomName));
            }
            else if (grid == ThisGrid.nonLocal) //get blocks from connected grid
            {
                GridTerminalSystem.GetBlocksOfType<IMyEntity>(blocksWithInventory, block => block.InventoryCount > 0 && block.CubeGrid != me.CubeGrid && containers.Contains(block.CustomName));
            }
            return blocksWithInventory;
        }

        //sets the inventories field with every inventory connected to the grid
        public List<IMyInventory> getAccessableInventories(List<IMyTerminalBlock> blocks)
        {
            IMyShipConnector compareTo; //the connector which will be used to check if inventory is accessible via conveyors (incase one of the in/out connectors are not used)
            if(connectorIn == null) //not using the pull from connected inventory feature
            {
                compareTo = connectorOut; //if neither are used, the script will crash, but given there is no functionality at all if both features are disabled, it doesn't matter
            }
            else //if pull feature is enabled
            {
                compareTo = connectorIn;
            }
            List<IMyInventory> accessibleInvs = new List<IMyInventory>(); //holds accessible inventory objects
            int invCount; //number of inventories the block has
            foreach (IMyTerminalBlock invBlock in blocks) //iterate over blocks
            {
                invCount = invBlock.InventoryCount; //gets number of inventories a block has (one for most types)
                for (int i = 0; i < invCount; i++) //for each inventory a block has
                {
               
                    IMyInventory inventory = invBlock.GetInventory(i); //get inventory object
                    if (compareTo.GetInventory().IsConnectedTo(inventory)) //if the inventory is accessable by the connector
                    {
                        accessibleInvs.Add(inventory); //add inventory object to list
                    }
                }
            }
            return accessibleInvs;
        }

        //enum representing getting object from the local grid or connected grid
        public enum ThisGrid
        {
            local,
            nonLocal
        }
        //!initialisation


    }
}
