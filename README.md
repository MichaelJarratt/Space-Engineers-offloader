# Space Engineers :: offloader
This script was designed to run on transport vehicles made to carry cargo between two places, but it can be adapted to pulling items into a bases containers without too much    modification.  
  
The scipt listens for the designated "docking connector" connecting with another grid, then pulls all the items (indescriminately) out of connected containers and into containers of the grid running the script until it is full or the connected grid is empty.  
The scipt was designed with Ores in mind, which does matter. For example on a small grid this would not work with certain items, such as steel plates, as only ores and *some* other components can pass through small conveyors.

There is also an optional feature to push items from the designated "rear connector" into the inventories of whatever it is connected to. If this is enabled then the names of blocks in the "pushInto" list will be used to get the inventories of those blocks and move items into them. If no block names are specified, then it will push items into any available inventory.

# How to use this:
If you still think this script will suite you needs, you may simply copy and paste the code insode of *offloader/Program.cs* excluding the namespace and the class into the programmable block.
<copy code that is after the second *{* and before the second to last *}*>
When in game, you can edit the values assigned in the section in between the *##configuarion##* and *##!configuration##* markers. here you can configure the push items feature and set the correct names for various blocks required to function.
