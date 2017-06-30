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

class TerrainStitcherWindow extends EditorWindow {
	var terrain1 : Terrain;
	var terrain2 : Terrain;
	var numSamplesAuto = 20;
	var numSamplesManual = 20;
	var dominatingTerrain = 0;
	
	var setAutoSamples = false;
	var setManualSamples = false;
	
	@MenuItem ("Terrain/Terrain Stitcher...")
	static function Init () {
		// Get existing open window or if none, make a new one:
		var window : TerrainStitcherWindow = EditorWindow.GetWindow(TerrainStitcherWindow);
    }
	
	function OnSelectionChange() {
		Repaint();
	}
	
	function findNeighbors(terrain : GameObject, gameObjects, originalPositions) {
		var neighbors = new Array();
		
		var thisPos = originalPositions[terrain];
		var width = terrain.GetComponent(Terrain).terrainData.size.x;
		
		for (var go : GameObject in gameObjects) {
			if (terrain == go)
				continue;
		
			var pos = originalPositions[go];
		
			var diffX = pos.x - thisPos.x;
			var diffZ = pos.z - thisPos.z;
			
			// A neighbor is positioned N, E, S or W of this terrain
			if (((diffZ >= width && diffZ < 1.5 * width) && (diffX > -0.5 * width && diffX < 0.5 * width)) || // N
				((diffZ > -0.5 * width && diffZ < 0.5 * width) && (diffX >= width && diffX < width * 1.5)) || // E
				((diffZ <= -width && diffZ > -1.5 * width) && (diffX > -0.5 * width && diffX < 0.5 * width)) || // S
				((diffZ > -0.5 * width && diffZ < 0.5 * width) && (diffX <= -width && diffX > -1.5 * width))) // W
				neighbors.Add(go);
		}
		
		return neighbors;
	}
	
	function stitchTerrains(terrains, numSamples) {
		var originalPositions = new Hashtable();
		for (var go : GameObject in terrains) {
			var pos = go.GetComponent(Terrain).transform.position;
			originalPositions.Add(go, pos);
		}
	
		var affectedTerrains = new ArrayList();
		var visitedTerrains = new ArrayList();
		var performedStitches = new Hashtable();
	
		do {
			for (var go : GameObject in terrains) {
				if (performedStitches[go] == null)
					performedStitches[go] = new ArrayList();
			
				var neighbors = findNeighbors(go, terrains, originalPositions);
				// If we have no neighbors we don't need stitching
				if (neighbors.length == 0) {
					visitedTerrains.Add(go);
					continue;
				}					
					
				// Terrain must be the first or already affected by another terrain to be allowed
				// to dominate another terrain
				if (!((affectedTerrains.Count == 0 || affectedTerrains.Contains(go)) &&
					!visitedTerrains.Contains(go)))
					continue;
			
				if (EditorUtility.DisplayCancelableProgressBar("Stitching terrains...", "", 
					parseFloat(visitedTerrains.Count) / terrains.length)) {
					EditorUtility.ClearProgressBar();
					return;
				}													
																																				
				for (var neighbor : GameObject in neighbors) {
					// No need to stitch if we are already stitched
					if (performedStitches[neighbor] != null && performedStitches[neighbor].Contains(go))
						continue;
		
					if (TerrainStitcher.stitch(go.GetComponent(Terrain), neighbor.GetComponent(Terrain), numSamples, 1) == -1) {
						EditorUtility.DisplayDialog("Terrain dimensions don't match!", "All terrains must have equal dimensions to"
							+ " allow stitching.", "OK");
						return;
					}

					performedStitches[go].Add(neighbor);					
					affectedTerrains.Add(neighbor);
				}
				
				visitedTerrains.Add(go);
			}
		} while (visitedTerrains.Count < terrains.length);
		
		EditorUtility.ClearProgressBar();
	}
	
	function OnGUI () {
		EditorGUILayout.BeginVertical();
	
		/* AUTO STITCHING */
		GUILayout.Label("Auto Stitching");
		
		// Every selected object must be a terrain for stitch button to stay enabled
		// and at least 2 terrains must be selected
		GUI.enabled = true;
		var selected = Selection.gameObjects;
		var numTerrains = 0;
		for (var go : GameObject in selected) {
			if (go.GetComponent(Terrain) == null) {
				GUI.enabled = false;
				break;
			} else {
				numTerrains++;
			}
		}
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Number of tiles to smooth: ");

		if (numTerrains < 2) {
			GUI.enabled = false;
			setAutoSamples = false;
			numSamplesAuto = EditorGUILayout.IntSlider(numSamplesAuto, 0, 0);
		} else {
			var width = selected[0].GetComponent(Terrain).terrainData.heightmapWidth;
			if (!setAutoSamples) {
				numSamplesAuto = width / 25 > 0 ? width / 25 : 1;
				setAutoSamples = true;
			}
			numSamplesAuto = EditorGUILayout.IntSlider(numSamplesAuto, 1, width);
		}
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Stitch Selected"))
			stitchTerrains(selected, numSamplesAuto);
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.Space();
		
		/* MANUAL STITCHING */
		GUI.enabled = true;
		GUILayout.Label("Manual Stitching");
		
		terrain1 = EditorGUILayout.ObjectField("Terrain 1", terrain1, Terrain, true);
		terrain2 = EditorGUILayout.ObjectField("Terrain 2", terrain2, Terrain, true);
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(GUIContent("Don't alter corners of terrain: ", "(0 = alter both terrains)"));
		dominatingTerrain = EditorGUILayout.IntSlider(dominatingTerrain, 0, 2);
		EditorGUILayout.EndHorizontal();
		

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Number of samples to smooth: ");

		if (terrain1 == null || terrain2 == null) {
			GUI.enabled = false;
			setManualSamples = false;
			numSamplesManual = EditorGUILayout.IntSlider(numSamplesManual, 0, 0);
		} else {
			width = terrain1.terrainData.heightmapWidth;
			if (!setManualSamples) {
				numSamplesManual = width / 25 > 0 ? width / 25 : 1;
				setManualSamples = true;
			}
			numSamplesManual = EditorGUILayout.IntSlider(numSamplesManual, 1, width);
		}
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Stitch")) {
			TerrainStitcher.stitch(terrain1, terrain2, numSamplesManual, dominatingTerrain);
		}
		EditorGUILayout.EndHorizontal();
				
		EditorGUILayout.EndVertical();
	}
}