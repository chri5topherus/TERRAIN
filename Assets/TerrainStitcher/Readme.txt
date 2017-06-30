/*
 * Copyright (c) 2011 Reijer Hooijkaas.
 * All rights reserved.
 *
 * Contact: reijerh AT gmail.com
 * 
 * TERRAIN STITCHER v1.1
 *
 * TerrainStitcher is free for private, non-commercial use only, please
 * refer to License.txt for conditions that apply to commercial use.
 */

---------------------------------------------------------------------------

TERRAIN STITCHER IS FREE FOR PERSONAL USE, BUT DONATIONS ARE APPRECIATED
IF YOU FOUND IT PARTICULARLY USEFUL/TIMESAVING. PLEASE REFER TO LICENSE.TXT
FOR PAYMENT DETAILS.

---------------------------------------------------------------------------


TerrainStitcher is a Unity editor and API tool that stitches terrains together by adjusting their heightmaps along the borders that "connect" the terrains. This creates a seamless transition from one terrain to the other and makes manually adjusting terrain borders unnecessary. Terrains must have the same dimensions (x, y, z) and resolution for stitching to work! Undo is supported.

Usage:
Open the Terrain Stitcher window through the Terrain --> Terrain Stitcher menu button at Unity's topmost menu bar.

1. Automatic stitching
----------------------
This is what you'll probably want to use most of the time.

1.1 Simply layout the terrains in the editor so that the edges that you want to stitch together are fairly close.
1.2 Select the terrains in the editor.
1.3 Set the number of terrain tiles to smooth, the more terrain tiles you choose to smooth, the less noticable the stitching effect will be.
1.4 Click 'Stitch Selected'.

Using this method you can stitch as many terrains as you want.

2. Manual stitching
-------------------
This gives you slightly more control over the stitching process by specifying of which terrain the corners should remain unaltered, but only allows you to stitch two terrains together at the same time.

2.1 Drag the terrains you want to stitch to the terrain slots on the stitcher window.
2.2 (Optionally) Specify the terrain of which the corners must remain unaltered by the stitching process (select 0 to alter both terrains). This is useful if for example you want to stitch a terrain to already stitched terrains and want it to match their corner heights (this is always necessary when stitching together 4 terrains in a square).
2.3 Set the number of terrain tiles to smooth.
2.4 Click 'Stitch'.

3. API
------
Terrain Stitcher provides an API that can be used to stitch terrains together at runtime or in your own editor component. When stitching terrains at runtime, don't forget to call Terrain.SetNeighbors() after stitching to match LOD on neighboring terrains (prevents visible seems). The API consists of one function:

/*
* terrain1, terrain2: 	The terrains to stitch.
* numSamples:			The number of samples to smooth [1, terrain resolution].
* domTerrain:			The height of this terrain will not be altered at the corners [0, 1, 2].
*						(0 = no dominating terrain)
*						
* Returns -1 iff the terrains' dimensions and/or resolution don't match, 0 otherwise.
*/
TerrainStitcher.stitch(terrain1 : Terrain, terrain2 : Terrain, numSamples : int, domTerrain : int)