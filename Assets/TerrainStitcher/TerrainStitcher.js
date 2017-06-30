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

import System;

class TerrainStitcher {
	static var smoothModTable : Double[] = null;
	static var smoothDYTable : Double[] = null;
	static var memoizationTablesFilled = false;
	
	static var prevSize = Vector3(0, 0, 0);
	static var prevHeight = 0;
	static var prevWidth = 0;
	static var prevNumSamples = 0;

	static function stitch(terrain1: Terrain, terrain2 : Terrain, numSamples : int) {
		stitch(terrain1, terrain2, numSamples, 0);
	}
	
	// When stitching two terrains together the edges meet in the middle, however to make the stitch
	// look more natural we want to also raise/lower a number of cells near the edge. We gradually
	// lower the amount by which we change these cells.
	static function smoothDY(current, max) {
		var x = 1.0 - parseFloat(current) / (max - 1);
		
		/*return 0.05*((x*(x-0.3)*(x-0.5)*(x-0.7)*(x-0.85)*(x-1.0)) / -0.0025770937499999995) +
			0.2*((x*(x-0.15)*(x-0.5)*(x-0.7)*(x-0.85)*(x-1.0)) / 0.0013859999999999999) +
			0.5*((x*(x-0.15)*(x-0.3)*(x-0.7)*(x-0.85)*(x-1.0)) / -0.0012249999999999995) +
			0.8*((x*(x-0.15)*(x-0.3)*(x-0.5)*(x-0.85)*(x-1.0)) / 0.0013859999999999999) +
			0.95*((x*(x-0.15)*(x-0.3)*(x-0.5)*(x-0.7)*(x-1.0)) / -0.0025770937500000004) +
			(x*(x-0.15)*(x-0.3)*(x-0.5)*(x-0.7)*(x-0.85)) / 0.013387500000000004;*/
		
		/*Polynomial (Lagrange) interpolated from (x, y):
		0 0
		0.5 0.5
		1 1
		.30  .20
		.70 .80
		.15  .05
		.85 .95
		*/
		var result = (236706659320000.0*Math.Pow(x,6)+99115929736349168000.0*Math.Pow(x,5)-247790818523389713100.0*Math.Pow(x,4)
			+80036273392876468420.0*Math.Pow(x,3)+127736693875550215551.0*Math.Pow(x,2)-713647159857677493.0*x)/58384668816317760000.0;
		smoothDYTable[current] = result;
		return result;
	}
	
	// When one terrain needs to stay the same height in the corners we want to gradually transition
	// from not modifying a tile (at the corners) to modifying tiles for 50% (the other 50% is done
	// by the other terrain to meet in the middle)
	static function smoothMod(current, max) {
		var x : Double = parseFloat(current) / (max - 1);
		//return 0.5 * Mathf.Cos(-Mathf.PI * x + 0.5 * Mathf.PI);

		/*Polynomial (Lagrange) interpolated from (x, y):
		0.0,0.0
		0.01,1.0E-4
		0.05,0.0050
		0.1,0.04
		0.5,0.5
		0.9,0.04
		0.95,0.0050
		0.99,1.0E-4
		1.0,0.0
		*/
		var result = 1.0E-4*((1*(x-0.0)*(x-0.05)*(x-0.1)*(x-0.5)*(x-0.9)*(x-0.95)*(x-0.99)*(x-1.0)) / -1.4317846804800003E-5) +
			0.0050*((1*(x-0.0)*(x-0.01)*(x-0.1)*(x-0.5)*(x-0.9)*(x-0.95)*(x-0.99)*(x-1.0)) / 3.074152499999999E-5) +
			0.04*((1*(x-0.0)*(x-0.01)*(x-0.05)*(x-0.5)*(x-0.9)*(x-0.95)*(x-0.99)*(x-1.0)) / -9.804240000000002E-5) +
			0.5*((1*(x-0.0)*(x-0.01)*(x-0.05)*(x-0.1)*(x-0.9)*(x-0.95)*(x-0.99)*(x-1.0)) / 0.0019448099999999997) +
			0.04*((1*(x-0.0)*(x-0.01)*(x-0.05)*(x-0.1)*(x-0.5)*(x-0.95)*(x-0.99)*(x-1.0)) / -9.804239999999985E-5) +
			0.0050*((1*(x-0.0)*(x-0.01)*(x-0.05)*(x-0.1)*(x-0.5)*(x-0.9)*(x-0.99)*(x-1.0)) / 3.0741525000000005E-5) +
			1.0E-4*((1*(x-0.0)*(x-0.01)*(x-0.05)*(x-0.1)*(x-0.5)*(x-0.9)*(x-0.95)*(x-1.0)) / -1.4317846804800018E-5);
		smoothModTable[current] = result;
		return result;
	}
	
	// Get the height (Y) difference that needs to be applied at this point to match the terrains' height
	// X: along the edge
	// Y: into the terrain
	static function getYModDynamic(domTerrain : int, terrainToMod : int, dY : double, curCellX : int, maxCellsX : int,
		curCellY : int, maxCellsY : int) {
		var	yMod = dY / 2.0;
		
		if (terrainToMod == 1) {
			if (domTerrain == 1)
				yMod = dY * smoothMod(curCellX, maxCellsX);
			else if (domTerrain == 2)
				yMod = dY * (1.0 - smoothMod(curCellX, maxCellsX));
		}
		else if (terrainToMod == 2) {
			if (domTerrain == 1)
				yMod = dY * (1.0 - smoothMod(curCellX, maxCellsX));
			else if (domTerrain == 2)
				yMod = dY * smoothMod(curCellX, maxCellsX);
		}
		else {
			Debug.LogError("terrainToMod must be either 1 or 2! (found: " + terrainToMod + ")");
		}

		return parseFloat(yMod * smoothDY(curCellY, maxCellsY));
	}
	
	// Get the height (Y) difference that needs to be applied at this point to match the terrains' height
	// X: along the edge
	// Y: into the terrain
	static function getYModMemoized(domTerrain : int, terrainToMod : int, dY : double, curCellX : int, maxCellsX : int,
		curCellY : int, maxCellsY : int) {
		var	yMod = dY / 2.0;
		
		if (terrainToMod == 1) {
			if (domTerrain == 1)
				yMod = dY * smoothModTable[curCellX];
			else if (domTerrain == 2)
				yMod = dY * (1.0 - smoothModTable[curCellX]);
		}
		else if (terrainToMod == 2) {
			if (domTerrain == 1)
				yMod = dY * (1.0 - smoothModTable[curCellX]);
			else if (domTerrain == 2)
				yMod = dY * smoothModTable[curCellX];
		}
		else {
			Debug.LogError("terrainToMod must be either 1 or 2! (found: " + terrainToMod + ")");
		}

		return parseFloat(yMod * smoothDYTable[curCellY]);
	}
	
	/*
	* terrain1, terrain2: 	The terrains to stitch.
	* numSamples:			The number of samples to smooth [1, terrain resolution].
	* domTerrain:			The height of this terrain will not be altered at the corners [0, 1, 2].
	*						(0 = no dominating terrain)
	*						
	* Returns -1 iff the terrains' dimensions and/or resolution don't match, 0 otherwise.
	*/
	static function stitch(terrain1 : Terrain, terrain2 : Terrain, numSamples : int, domTerrain : int) {
		// Create Undo point
		// (must explicitly add the terrainData members as they are seen as separate from the Terrain)
		var terrainObjs = new Array(EditorUtility.CollectDeepHierarchy([terrain1, terrain2]));
		// (must explicitly add the terrainData members as they are seen as separate from the Terrain)
		terrainObjs = terrainObjs.concat(new Array(EditorUtility.CollectDeepHierarchy([terrain1.terrainData, terrain2.terrainData])));
		Undo.RegisterUndo(terrainObjs.ToBuiltin(UnityEngine.Object), "Stitch Terrains");

		// Find out direction in which they oppose eachother to apply correct translations	
		var dX = terrain2.transform.position.x - terrain1.transform.position.x;
		var dZ = terrain2.transform.position.z - terrain1.transform.position.z;
		
		var height = terrain1.terrainData.heightmapHeight;
		var width = terrain1.terrainData.heightmapWidth;
		
		// Assert that both terrains are equal in height/width & resolution
		if (height != terrain2.terrainData.heightmapHeight ||
			width != terrain2.terrainData.heightmapWidth ||
			terrain1.terrainData.size != terrain2.terrainData.size) {
			return -1;
		}
		
		// Initialize memoization parts as this is the first call for possibly multiple terrains with the same
		// dimensions and numSamples
		if (height != prevHeight || width != prevWidth || terrain1.terrainData.size != prevSize ||
				numSamples != prevNumSamples) {
			prevHeight = height;
			prevWidth = width;
			prevSize = terrain1.terrainData.size;
			prevNumSamples = numSamples;
			
			memoizationTablesFilled = false;
			smoothModTable = new Double[height];
			smoothDYTable = new Double[numSamples];			
		}
		
		if (memoizationTablesFilled)
			var getYMod = getYModMemoized;
		else 
			getYMod = getYModDynamic;
		
		var heights1 = terrain1.terrainData.GetHeights(0, 0, width, height);
		var heights2 = terrain2.terrainData.GetHeights(0, 0, width, height);
				
		// Find out which axis to stitch (depends on how the 2 terrains are positioned)
		// Stitch along Z axis
		if (Mathf.Abs(dX) > Mathf.Abs(dZ)) {
			var xDir = terrain2.transform.position.x > terrain1.transform.position.x ? 1 : -1;
			
			// Align terrains
			terrain2.transform.position = terrain1.transform.position;
			terrain2.transform.position.x = terrain1.transform.position.x + terrain1.terrainData.size.x * xDir;
			
			/* Stitch */
			// Loop over each sample point on the Z axis
			for (z = 0; z < height; z++) {
				// Get height difference between sample point on the two terrains ([0, 1.0])
				var dY = 0.0f;
				if (xDir == 1)
					dY = heights2[z, 0] - heights1[z, width - 1];
				else
					dY = heights2[z, width - 1] - heights1[z, 0];
						
				// Move a configured number of samples towards the point in the middle of the height difference
				// the amount to move them is reduced the farther away they are from the edge
				for (i = 0; i < numSamples; i++) {
					if (xDir == 1) {
						heights1[z, width - 1 - i] = heights1[z, width - 1 - i] + getYMod(domTerrain, 1, dY, z, height, i, numSamples);
						heights2[z, i] = heights2[z, i] - getYMod(domTerrain, 2, dY, z, height, i, numSamples);
					}
					else {
						heights1[z, i] = heights1[z, i] + getYMod(domTerrain, 1, dY, z, height, i, numSamples);
						heights2[z, width - 1 - i] = heights2[z, width - 1 - i] - getYMod(domTerrain, 2, dY, z, height, i, numSamples);
					}
				}
			}		
		}
		// Stitch along X axis
		else {
			var zDir = terrain2.transform.position.z > terrain1.transform.position.z ? 1 : -1;
			
			// Align terrains
			terrain2.transform.position = terrain1.transform.position;
			terrain2.transform.position.z = terrain1.transform.position.z + terrain1.terrainData.size.z * zDir;
			
			/* Stitch */
			// Loop over each sample point on the X axis
			for (x = 0; x < height; x++) {
				// Get height difference between sample point on the two terrains ([0, 1.0])
				dY = 0.0;
				if (zDir == 1)
					dY = heights2[0, x] - heights1[width - 1, x];
				else
					dY = heights2[width - 1, x] - heights1[0, x];
				
				// Move a configured number of samples towards the point in the middle of the height difference
				// the amount to move them is reduced the farther away they are from the edge
				for (i = 0; i < numSamples; i++) {
					if (zDir == 1) {
						heights1[width - 1 - i, x] = heights1[width - 1 - i, x] + getYMod(domTerrain, 1, dY, x, height, i, numSamples);
						heights2[i, x] = heights2[i, x] - getYMod(domTerrain, 2, dY, x, height, i, numSamples);
					}
					else {
						heights1[i, x] = heights1[i, x] + getYMod(domTerrain, 1, dY, x, height, i, numSamples);
						heights2[width - 1 - i, x] = heights2[width - 1 - i, x] - getYMod(domTerrain, 2, dY, x, height, i, numSamples);
					}
				}
			}
		}
		
		memoizationTablesFilled = true;
			
		// Modify terrain heights
		terrain1.terrainData.SetHeights(0, 0, heights1);
		terrain2.terrainData.SetHeights(0, 0, heights2);
		
		terrain1.Flush();
		terrain2.Flush();
		
		return 0;
	}
}