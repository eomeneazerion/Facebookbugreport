using Facebook.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleUploadTest : MonoBehaviour
{
    [SerializeField] RawImage preview;
    [SerializeField] Text path;
    [SerializeField] Button takeSnapButton;
    [SerializeField] Button loginButton;

    // Start is called before the first frame update
    void Start()
    {
        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
        }

        loginButton.onClick.AddListener(Login);
        takeSnapButton.onClick.AddListener(TakeShot);
        takeSnapButton.gameObject.SetActive(false);
    }

    public void Login()
    {
        var perms = new List<string>() { "email", "user_friends", "gaming_user_picture" };
        FB.LogInWithReadPermissions(perms, AuthCallback);
    }

    public void TakeShot()
    {
        TakeScreenShotAndUploadImageToMediaLibrary("", (status, message, image) =>
        {
            Debug.LogError("Test ScreenShot " + message + " status: " + status);
            if (preview != null)
                preview.texture = image;
            path.text = status + " || " + message;
        },
        (status, message) =>
        {
            Debug.LogError("Test Upload " + message + " status: " + status);
        });
    }


    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            // Signal an app activation App Event
            FB.ActivateApp();
            // Continue with Facebook SDK
            // ...
        }
        else
        {
            Debug.Log("Failed to Initialize the Facebook SDK");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            // Pause the game - we will need to hide
         //   Time.timeScale = 0;
        }
        else
        {
            // Resume the game - we're getting focus again
          //  Time.timeScale = 1;
        }
    }
    public void AuthCallback(ILoginResult result)
    {
        if (result.Error != null)
        {
            Debug.LogError("[FBManager] LoginAuthCallabck error: " + result.Error);
 
        }
        else if (!FB.IsLoggedIn || result.Cancelled)
        {
            Debug.LogError("[FBManager] LoginAuthCallback - Cancelled");
        }
        else
        {
            takeSnapButton.gameObject.SetActive(true);
            path.text = "Logged in";
        }
    }

    public void TakeScreenShotAndUploadImageToMediaLibrary(string caption, Action<bool, string, Texture2D> onCompletedScreenShot, Action<bool, string> onCompletedUpload)
    {
        StartCoroutine(captureScreenshot((status, tex, image) =>
        {
            onCompletedScreenShot?.Invoke(status, tex, image);
            UploadImageToMediaLibrary(caption, tex, onCompletedUpload);
        }));
    }

    IEnumerator captureScreenshot(Action<bool, string, Texture2D> imagePath)
    {
        yield return new WaitForEndOfFrame();
        //about to save an image capture
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height);

        //Get Image from screen
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();

        Debug.Log(" screenImage.width" + screenImage.width + " texelSize" + screenImage.texelSize);
        //Convert to png
        byte[] imageBytes = screenImage.EncodeToPNG();

        string screenshotpath = Application.persistentDataPath + $"/gop3_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".jpg";
        System.IO.File.WriteAllBytes(screenshotpath, imageBytes);

        imagePath?.Invoke(screenImage != null, screenshotpath, screenImage);
    }

    public void UploadImageToMediaLibrary(string caption, string path, Action<bool, string> onCompleted = null)
    {
        void HandleResult(IResult result)
        {
            if (result == null)
            {
                onCompleted?.Invoke(false, "No response");
                return;
            }

            // Some platforms return the empty string instead of null.
            if (!string.IsNullOrEmpty(result.Error))
            {
                onCompleted?.Invoke(false, result.Error);
                // handle error case here.
            }
            else if (result.Cancelled)
            {
                onCompleted?.Invoke(false, "Cancelled");
                // a dialog was cancelled.
            }
            else if (!string.IsNullOrEmpty(result.RawResult))
            {
                onCompleted?.Invoke(true, "Success");
            }
            else
            {
                onCompleted?.Invoke(false, "Empty");
                // we got an empty response
            }
        }
        FBGamingServices.UploadImageToMediaLibrary(caption, new Uri(path), true, HandleResult);
    }
}
