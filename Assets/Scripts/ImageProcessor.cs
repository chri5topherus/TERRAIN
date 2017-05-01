/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2014 Intel Corporation. All Rights Reserved.

*******************************************************************************/

using UnityEngine.UI;
using UnityEngine;
using System.Collections;

public class ImageProcessor: MonoBehaviour {

	private Texture2D colorTexture2D; //Color Texture
	private Texture2D depthTexture2D;
	private PXCMImage colorImage = null;//PXCMImage for color 
	private PXCMImage depthImage = null;

	private PXCMSenseManager psm; //SenseManager Instance
	private pxcmStatus sts; //Check error status

	private int height = 480;
	private int width = 640;

	private bool firstTime = true;

	private Mesh mesh;
	int numPoints;

	Vector3[] points;
	Color[] pointsDepths;
	int[] indecies;
	Color[] colors;
	Color[] colors2;




	public Image colorIMG;
	public Image depthIMG;

	/// <summary>
	/// Use this for initialization
	/// Unity function called on the frame when a script is enabled 
	/// just before any of the Update methods is called the first time.
	/// </summary>
	void Start () {

		numPoints = 20000;

		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		points = new Vector3[numPoints];
		pointsDepths = new Color[height*width];
		indecies = new int[numPoints];
		colors2 = new Color[numPoints];


		/* Initialize a PXCMSenseManager instance */
		psm = PXCMSenseManager.CreateInstance();
		if (psm == null)
		{
			Debug.LogError("SenseManager Initialization Failed");
			return;
		}

		/* Enable the color stream of size 640x480 */
		psm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, width, height);
		psm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, width, height);

		/* Initialize the execution pipeline */
		sts = psm.Init();
		if (sts != pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.LogError("PXCMSenseManager.Init Failed");
			OnDisable();
			return;
		}

	}


	private void generatePointCloud(Texture2D depthTexture) {

		pointsDepths = depthTexture.GetPixels ();
		//Debug.Log (pointsDepths.ToString());

		int index = 0;

		for(int i = 0 ; i < height ; i=i+2)  {
			
			for(int j = 0 ; j < width ; j=j+2)  {

				if (pointsDepths [i * width + j].r > 0.2f) {
					//Debug.Log (pointsDepths [i * width + j].r);

					if (index < numPoints) {
						points [index] = new Vector3 (j, i, pointsDepths [i * width + j].r * 100);
						indecies [index] = index;
						colors2 [index] = Color.red;
					}
					index++;
				}
				//Debug.Log (pointsDepths[i*width/10 + j].r);
				//
			}
		}

		Debug.Log (index);

		//clean rest of vertices
		for (int i = index; i < numPoints; i++) {
			indecies [i] = 0;
		}

		mesh.vertices = points;
		mesh.colors = colors2;
		mesh.SetIndices(indecies, MeshTopology.Points,0);






	}

	/// <summary>
	/// Update is called every frame, if the MonoBehaviour is enabled.
	/// </summary>
	void Update()
	{



		if (Input.GetKeyDown (KeyCode.Space)) {
			firstTime = true;
			Debug.Log ("button pressed");

		}


		/* Make sure PXCMSenseManager Instance is Initialized */
		if (psm == null) return;

		/* Wait until any frame data is available true(aligned) false(unaligned) */
		if (psm.AcquireFrame(true) != pxcmStatus.PXCM_STATUS_NO_ERROR) 
			return;


		/* Retrieve a sample from the camera */
		PXCMCapture.Sample sample = psm.QuerySample();
		if (sample != null)
		{

			depthImage = sample.depth;
			if (depthImage != null)
			{       
				if (depthTexture2D == null)
				{
					/* If not allocated, allocate a Texture2D */
					depthTexture2D = new Texture2D(depthImage.info.width, depthImage.info.height, TextureFormat.ARGB32, false);

					/* Associate the Texture2D with a gameObject */
					depthIMG.sprite = Sprite.Create (depthTexture2D, new Rect(0,0,width,height), new Vector2(0,0)); 





				}

				if(firstTime){
					generatePointCloud (depthTexture2D);
					//firstTime = false;
				}

				/* Retrieve the image data in Texture2D */
				PXCMImage.ImageData depthImageData;
				depthImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out depthImageData);
				depthImageData.ToTexture2D(0, depthTexture2D); // converts RSSDK image data to Unity Texture2D

				depthImage.ReleaseAccess(depthImageData);

				/* Apply the texture to the GameObject to display on */
				depthTexture2D.Apply();
			}


			colorImage = sample.color;
			if (colorImage != null)
			{
				if (colorTexture2D == null)
				{
					/* If not allocated, allocate a Texture2D */
					colorTexture2D = new Texture2D(colorImage.info.width, colorImage.info.height, TextureFormat.ARGB32, false);

					/* Associate the Texture2D with a gameObject */
					//colorPlane.GetComponent<Renderer>().material.mainTexture = colorTexture2D;
					colorIMG.sprite = Sprite.Create (colorTexture2D, new Rect(0,0,width,height), new Vector2(0,0)); 

					//colorPlane.renderer.material.mainTextureScale = new Vector2(-1f, 1f);

					//testIMG.sprite = Sprite.Create (colorTexture2D, new Rect(0,0,200,400), new Vector2 (0, 0));
				}






				// Retrieve the image data in Texture2D
				PXCMImage.ImageData colorImageData;
				colorImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out colorImageData);
				colorImageData.ToTexture2D(0, colorTexture2D);
				//colorTexture2D.SetPixel (0, 0, Color.cyan);


				//to filter red from the image 
				//red value has to be bigger then two times g and b value 
				//fade out corner

		
				colors = colorTexture2D.GetPixels ();
				float treshMin = 1.5F;
				float treshMax = 2.5F;

				float colorDif01;
				float colorDif02;

				for (int i = 0; i < colors.Length; i++) {

					colorDif01 = colors [i].g / colors [i].r;
					colorDif02 = colors [i].g / colors [i].b;

					colorDif01 = (colorDif01 + colorDif02) / 2F;

					if (colorDif01 > treshMin)
						colors [i] = new Color(colors [i].r, colors [i].g, colors [i].b, remapNumber(treshMax,treshMin,colorDif01));

					/*
					if (colorDif01 > tresh) {
						colors [i] = Color.cyan;
					}
*/


					/*
					
					//all other ignored
					if (colors [i].g * tresh < colors [i].r && colors [i].b * tresh < colors [i].r) {

						colors [i].r / colors [i].g



						colorTMP01 = (colorTMP01 + colorTMP02) / 2F;

						colors [i] = new Color(0F,0F,0F);

					}


					if (colors [i].g * tresh < colors [i].r && colors [i].b * tresh < colors [i].r)
						colors [i] = Color.cyan;
					else if ((colors [i].g * 1.5F < colors [i].r && colors [i].b * 1.5F < colors [i].r))
						colors [i] = Color.green;

					*/
				}

				colorTexture2D.SetPixels (colors);


				//for (int i = 0; i < colorTexture2D.height; i++) {
				//	for (int j = 0; j < colorTexture2D.width; j++) {
				//		colorTexture2D.SetPixel (j, i, Color.cyan);
				//	}
				//}

				colorImage.ReleaseAccess(colorImageData);

				/* Apply the texture to the GameObject to display on */
				colorTexture2D.Apply();
			}
		}

		/* Realease the frame to process the next frame */
		psm.ReleaseFrame();

	}

	/// <summary>
	/// Unity function that is called when the behaviour becomes disabled () or inactive.
	/// Used for clean-up in the end
	/// </summary>
	void OnDisable()
	{
		//faceAnalyzer.Dispose();
		if (psm == null) return;
		psm.Dispose();
	}

	private float remapNumber(float min, float max, float num) {
		return ((num - min) / (max - min));
	}

}

