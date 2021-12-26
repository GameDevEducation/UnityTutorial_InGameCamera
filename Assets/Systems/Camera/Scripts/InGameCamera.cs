using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

public class InGameCamera : MonoBehaviour
{
    [SerializeField] RawImage LiveDisplayImage;
    [SerializeField] RawImage LastTakenImage;
    [SerializeField] Camera LinkedCamera;
    [SerializeField] RenderTexture CameraRT;
    [SerializeField] int NumShotsAvailable = 8;
    [SerializeField] TextMeshProUGUI ShotsRemainingDisplay;

    [SerializeField] float ShowLastImageFor = 2f;
    Texture2D LastImage;

    public List<Texture2D> CapturedImages = new List<Texture2D>();

    float LastImageShowTimeRemaining = -1f;
    int NumShotsRemaining;

    // Start is called before the first frame update
    void Start()
    {
        NumShotsRemaining = NumShotsAvailable;

        ShotsRemainingDisplay.text = $"{(NumShotsAvailable - NumShotsRemaining)}/{NumShotsAvailable}";
    }

    // Update is called once per frame
    void Update()
    {
        // update the last image show time
        if (LastImageShowTimeRemaining > 0)
        {
            LastImageShowTimeRemaining -= Time.deltaTime;

            // return to live display
            if (LastImageShowTimeRemaining <= 0f)
            {
                LiveDisplayImage.gameObject.SetActive(true);
                LastTakenImage.gameObject.SetActive(false);
            }
        }
    }

    public void DeleteLastPicture()
    {
        // no images taken?
        if (NumShotsRemaining == NumShotsAvailable)
            return;

        // restore the available shot
        NumShotsRemaining++;

        // remove the captured image
        CapturedImages.RemoveAt(CapturedImages.Count - 1);
        LastTakenImage.texture = null;

        // return to live display
        LastImageShowTimeRemaining = 0f;
        LiveDisplayImage.gameObject.SetActive(true);
        LastTakenImage.gameObject.SetActive(false);

        ShotsRemainingDisplay.text = $"{(NumShotsAvailable - NumShotsRemaining)}/{NumShotsAvailable}";
    }

    public void TakePicture()
    {
        // no more shots left?
        if (NumShotsRemaining <= 0)
        {
            return;
        }

        //// transfer the texture - this is GPU side only
        //Graphics.CopyTexture(CameraRT, LastImage);

        AsyncGPUReadback.Request(CameraRT, 0, (AsyncGPUReadbackRequest action) =>
        {
            LastImage = new Texture2D(CameraRT.width, CameraRT.height,
                                      CameraRT.graphicsFormat,
                                      UnityEngine.Experimental.Rendering.TextureCreationFlags.None);

            LastImage.SetPixelData(action.GetData<byte>(), 0);
            LastImage.Apply();

            CapturedImages.Add(LastImage);

            // update the last taken image display
            LastTakenImage.texture = LastImage;

            // save the image to file
            var currentTime = System.DateTime.Now;
            string fileName = $"Shot_{currentTime:yyyyMMdd_HHmmss}_{(1 + NumShotsAvailable - NumShotsRemaining)}.jpeg";
            fileName = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            System.IO.File.WriteAllBytes(fileName, LastImage.EncodeToJPG());

            // show the last image
            LiveDisplayImage.gameObject.SetActive(false);
            LastTakenImage.gameObject.SetActive(true);

            // show the image for a time
            LastImageShowTimeRemaining = ShowLastImageFor;
        });

        NumShotsRemaining--;
        ShotsRemainingDisplay.text = $"{(NumShotsAvailable - NumShotsRemaining)}/{NumShotsAvailable}";
    }
}
