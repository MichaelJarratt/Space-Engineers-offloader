# Space Engineers :: offloader
This script was designed to run on transport vehicles made to carry cargo between two places, but it can be adapted to pulling items into a bases containers without too much    modification.  
  
The scipt listens for the designated connector connecting with another grid, then pulls all the items (indescriminately) out of connected containers and into containers of the grid running the script until it is full or the connected grid is empty.  
The scipt was designed with Ores in mind, which does matter. For example on a small grid this would not work with certain items, such as steel plates, as only ores and *some* other components can pass through small conveyors.

# How to use this:
If you still think this script will suite you needs, you may simply copy and paste the code insode of *offloader.sln* excluding the namespace and the class into the programmable block.
<copy code that is after the second *{* and before the second to last *}*>
