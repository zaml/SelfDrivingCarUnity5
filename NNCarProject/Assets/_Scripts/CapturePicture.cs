using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityStandardAssets.CrossPlatformInput;

public class CapturePicture : MonoBehaviour
{

	int entries = 0;

	System.Text.StringBuilder csv = new System.Text.StringBuilder();

	// time that a new feature will be captured.
	private int nextUpdate = 2;

	// 4k = 3840 x 2160   1080p = 1920 x 1080
	public int captureWidth = 1920;
	public int captureHeight = 1080;

	public Camera cam;

	// optional game object to hide during screenshots (usually your scene canvas hud)
	public GameObject hideGameObject;

	// optimize for many screenshots will not destroy any objects so future screenshots will be fast
	public bool optimizeForManyScreenshots = true;

	// configure with raw, jpg, png, or ppm (simple raw format)
	public enum Format
	{
		RAW,
		JPG,
		PNG,
		PPM}

	;

	public Format format = Format.PPM;

	// folder to write output (defaults to data path)
	public string folder;

	// private vars for screenshot
	private Rect rect;
	private RenderTexture renderTexture;
	private Texture2D screenShot;

	void Update ()
	{
		// pass the input to the car!
		float h = CrossPlatformInputManager.GetAxis ("Horizontal");
		float v = CrossPlatformInputManager.GetAxis ("Vertical");

		// work with a single decimal for better movement labeling.
		h = (float)Math.Round ((double)h, 1);


		// execute code every [nextUpdate] amount of seconds
		if (Time.time >= nextUpdate) {
			nextUpdate = Mathf.FloorToInt (Time.time) + 1;

			// capture neural network feature from user driving.
			CaptureFeature (h, v);
		}

	}

	void CaptureFeature (float steering, float speed)
	{

		// check keyboard 'k' for one time screenshot capture and holding down 'v' for continious screenshots
		//captureScreenshot |= Input.GetKeyDown("k");
		//captureVideo = Input.GetKey("v");


		// create screenshot objects if needed
		if (renderTexture == null) {
			// creates off-screen render texture that can rendered into
			rect = new Rect (0, 0, captureWidth, captureHeight);
			renderTexture = new RenderTexture (captureWidth, captureHeight, 24);
			screenShot = new Texture2D (captureWidth, captureHeight, TextureFormat.RGB24, false);
		}

		// get main camera and manually render scene into rt
		cam.targetTexture = renderTexture;
		cam.Render ();

		// read pixels will read from the currently active render texture so make our offscreen 
		// render texture active and then read the pixels
		RenderTexture.active = renderTexture;
		screenShot.ReadPixels (rect, 0, 0);

		// reset active camera texture and render texture
		//cam.targetTexture = null;
		RenderTexture.active = null;

		// get our unique filename
		//string filename = uniqueFilename((int) rect.width, (int) rect.height);

		// pull in our file header/data bytes for the specified image format (has to be done from main thread)
		//byte[] fileHeader = null;
		byte[] fileData = null;

		if (format == Format.RAW) {
			fileData = screenShot.GetRawTextureData ();
		} else if (format == Format.PNG) {
			fileData = screenShot.EncodeToPNG ();
		} else if (format == Format.JPG) {
			fileData = screenShot.EncodeToJPG ();
		}
				
	    // --------------- Feature Selection -----------------------
		Texture2D t = new Texture2D (captureWidth,captureHeight);
		t.LoadImage (fileData);
		var c = t.GetPixels();
		var c_size = captureHeight * captureWidth;

		// gs is the grayscale array used for the NN features.
		// position 0 of the array is the label (steering direction)
		float[] gs = new float[c_size+1];
		gs [0] = steering; // Label Setup
		for (int i = 0; i < c_size; i++) {
			gs [i+1] = c [i].grayscale;
		}

		// add gs to feature set (training set)
		csv.AppendLine(string.Join(",", Array.ConvertAll(gs, x => x.ToString())));

		// save after 10 iterations (for now...)

		entries++;

		if (entries == 10) {
			File.WriteAllText("NNCar.csv", csv.ToString());
		}


		// --------------- End --------------------------------------


		// create new thread to save the image to file (only operation that can be done in background)
		/*
				new System.Threading.Thread(() =>
					{
						// create file and write optional header with image bytes
						var f = System.IO.File.Create(filename);
						if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
						f.Write(fileData, 0, fileData.Length);
						f.Close();
						Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));
					}).Start();
				*/

		// cleanup if needed
		if (optimizeForManyScreenshots == false) {
			Destroy (renderTexture);
			renderTexture = null;
			screenShot = null;
		}

	}

}
