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

public class FaceTracking: MonoBehaviour {

    private Texture2D colorTexture2D; //Color Texture
    private PXCMImage colorImage = null;//PXCMImage for color 

    private FaceRenderer faceRenderer; //Rendererer for Visualization
    private int MaxPoints = 78;
    private PXCMSenseManager psm; //SenseManager Instance
    private pxcmStatus sts; //Check error status
    private PXCMFaceModule faceAnalyzer; //FaceModule Instance

	Color[] colors;

	public Image testIMG;


    /// <summary>
    /// Use this for initialization
    /// Unity function called on the frame when a script is enabled 
    /// just before any of the Update methods is called the first time.
    /// </summary>
	void Start () {

		colors = new Color[640 * 480];

		for (int i = 0; i < colors.Length; i++) {
			colors [i] = (Color.cyan);
		}

        /* Initialize a PXCMSenseManager instance */
        psm = PXCMSenseManager.CreateInstance();
        if (psm == null)
        {
            Debug.LogError("SenseManager Initialization Failed");
            return;
        }

        /* Enable the color stream of size 640x480 */
		psm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 640, 480);

        /* Initialize the execution pipeline */
        sts = psm.Init();
        if (sts != pxcmStatus.PXCM_STATUS_NO_ERROR)
        {
            Debug.LogError("PXCMSenseManager.Init Failed");
            OnDisable();
            return;
        }

	}



    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update()
    {

        /* Make sure PXCMSenseManager Instance is Initialized */
        if (psm == null) return;

        /* Wait until any frame data is available true(aligned) false(unaligned) */
        if (psm.AcquireFrame(true) != pxcmStatus.PXCM_STATUS_NO_ERROR) 
			return;


        /* Retrieve a sample from the camera */
        PXCMCapture.Sample sample = psm.QuerySample();
        if (sample != null)
        {
            colorImage = sample.color;
            if (colorImage != null)
            {
                if (colorTexture2D == null)
                {
                    /* If not allocated, allocate a Texture2D */
                    colorTexture2D = new Texture2D(colorImage.info.width, colorImage.info.height, TextureFormat.ARGB32, false);

                    /* Associate the Texture2D with a gameObject */
                    //colorPlane.GetComponent<Renderer>().material.mainTexture = colorTexture2D;
					testIMG.sprite = Sprite.Create (colorTexture2D, new Rect(0,0,640,480), new Vector2(0,0)); 

						             
						//colorPlane.renderer.material.mainTextureScale = new Vector2(-1f, 1f);

					//testIMG.sprite = Sprite.Create (colorTexture2D, new Rect(0,0,200,400), new Vector2 (0, 0));
                }

                /* Retrieve the image data in Texture2D */
               PXCMImage.ImageData colorImageData;
                colorImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out colorImageData);
                colorImageData.ToTexture2D(0, colorTexture2D);
				//colorTexture2D.SetPixel (0, 0, Color.cyan);

				colorTexture2D.SetPixels (colors);


			/*
				for (int i = 0; i < colorTexture2D.height; i++) {
					for (int j = 0; j < colorTexture2D.width; j++) {
						colorTexture2D.SetPixel (j, i, Color.cyan);
					}
				}
*/
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
}

