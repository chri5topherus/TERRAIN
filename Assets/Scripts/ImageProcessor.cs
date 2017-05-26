using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using Intel.RealSense;

public class ImageProcessor: MonoBehaviour {

	public GameObject _pointCloud;
	private PXCMProjection projection;
	private Texture2D colorTexture2D; //Color Texture
	public Texture colorTexture;
	private Texture2D depthTexture2D;
	private PXCMImage colorImage = null; //PXCMImage for color 
	private PXCMImage depthImage = null;
	private bool smooth = false;

	private PXCMSenseManager psm; //SenseManager Instance
	private pxcmStatus sts; //Check error status

	private int height = 480;
	private int width = 640;

	//private bool firstTime = true;


	//POINT CLOUD MESH ATTRIBUTES
	public float dampValue;
	private Mesh mesh;
	int numPoints = 20000;
	Vector3[] points;
	float[] allZPositions;
	Color[] pointsDepths;
	Color[] pointsColors;
	int[] indecies;
	Color[] colors;
	Color[] colors2;


	public UnityEngine.UI.Image colorIMG;
	public UnityEngine.UI.Image depthIMG;


	//Device device {
	//	get;
	//}

	void Start () {

		//projection = device.CreateProjection ();

		mesh = new Mesh();
		_pointCloud.GetComponent<MeshFilter>().mesh = mesh;
		points = new Vector3[numPoints];
		allZPositions = new float[height*width];
		pointsDepths = new Color[height*width];
		pointsColors = new Color[height*width];
		indecies = new int[numPoints];
		colors2 = new Color[numPoints];




		// Initialize a PXCMSenseManager instance
		psm = PXCMSenseManager.CreateInstance();
		if (psm == null)
		{
			Debug.LogError("SenseManager Initialization Failed");
			return;
		}


		//enable the color stream of size 640x480
		psm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, width, height);
		psm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, width, height);


		//initialize the execution pipeline
		sts = psm.Init();
		if (sts != pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.LogError("PXCMSenseManager.Init Failed");
			OnDisable();
			return;
		}

		PXCMCapture.Device device = psm.captureManager.device;
		projection = device.CreateProjection ();

	}

	//called every frame
	private void generatePointCloud(Texture2D depthTexture, Texture2D colorTexture) {

		float newZ;

		//pointsLastFrame = points;

		pointsDepths = depthTexture.GetPixels ();
		pointsColors = colorTexture.GetPixels ();
		int index = 0;

		//track every second pixel
		for(int i = 0 ; i < height ; i=i+2)  {
			
			for(int j = 0 ; j < width ; j=j+2)  {

				//ignore rest of the depth image
				if (pointsDepths [i * width + j].r > 0.2f) {
					//Debug.Log (pointsDepths [i * width + j].r);

					if (index < numPoints) {
						newZ = pointsDepths [i * width + j].r * 100;		
						if (smooth) {
							if (allZPositions [i * width + j] > newZ) {
								if (allZPositions [i * width + j] - newZ < 10f)
									newZ = newZ + (allZPositions [i * width + j] - newZ) / dampValue * (dampValue - 1);
							} else {
								if(newZ - allZPositions [i * width + j] < 10f) 
									newZ = newZ - (newZ - allZPositions [i * width + j]) / dampValue * (dampValue - 1);
							}
							allZPositions [i * width + j] = newZ;
						}

						points [index] = new Vector3 (j, i, newZ );
						indecies [index] = index;
						colors2 [index] = pointsColors [i * width + j];
					}
					index++;
				}
			}
		}

		//OUTPUT number of drawn points
		Debug.Log (index);

		//clean up rest of vertices
		for (int i = index; i < numPoints; i++) {
			indecies [i] = 0;
		}

		//set values on mesh 
		mesh.vertices = points;
		mesh.colors = colors2;
		mesh.SetIndices(indecies, MeshTopology.Points,0);
	}


	void Update()
	{



		if (Input.GetKeyDown (KeyCode.Space)) {
			Debug.Log ("button pressed: smooth: " + smooth);
			smooth = !smooth;
		}


		//Make sure PXCMSenseManager Instance is Initialized
		if (psm == null) return;

		//Wait until any frame data is available true(aligned) false(unaligned)
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


				//Retrieve the image data in Texture2D
				PXCMImage.ImageData depthImageData;
				depthImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out depthImageData);
				depthImageData.ToTexture2D(0, depthTexture2D); // converts RSSDK image data to Unity Texture2D

				depthImage.ReleaseAccess(depthImageData);

				depthTexture2D.Apply();

			} //end of depth 


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


				//Intel.RealSense.Image img2 = new Intel.RealSense.Image (colorImage.instance, false);
				//Intel.RealSense.Image img3 = new Intel.RealSense.Image (depthImage.instance, false);
		
				//COLOR MAP
				//colorImage = projection.CreateColorImageMappedToDepth(depthImage, colorImage);

				//POINTCLOUD
				generatePointCloud (depthTexture2D, colorTexture2D);


				// Retrieve the image data in Texture2D
				PXCMImage.ImageData colorImageData;
				colorImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out colorImageData);
				colorImageData.ToTexture2D(0, colorTexture2D);
				//colorTexture2D.SetPixel (0, 0, Color.cyan);


				//to filter red from the image 
				//red value has to be bigger then two times g and b value 
				//fade out corner

				colorTexture = colorTexture2D;

				//set colors

				colors = colorTexture2D.GetPixels ();
				float treshMin = 1.5F;
				float treshMax = 2.5F;

				float colorDif01;
				float colorDif02;
				/*
				for (int i = 0; i < colors.Length; i++) {

					colorDif01 = colors [i].g / colors [i].r;
					colorDif02 = colors [i].g / colors [i].b;

					colorDif01 = (colorDif01 + colorDif02) / 2F;

					if (colorDif01 > treshMin)
						colors [i] = new Color(colors [i].r, colors [i].g, colors [i].b, remapNumber(treshMax,treshMin,colorDif01));

*/





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




				//}

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
		} //end of color 

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

