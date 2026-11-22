using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed class IslandPhoto : MonoBehaviour
{
    public IslandGame game;
    public static string ReviewDirectory()
    {
        var args=Environment.GetCommandLineArgs();
        int n=Array.IndexOf(args,"-nagiReviewDir");
        return n>=0 && n+1<args.Length ? Path.GetFullPath(args[n+1]) : null;
    }
    IEnumerator Start()
    {
        string dir=ReviewDirectory();
        if(dir==null)yield break;
        Application.runInBackground=true;
        yield return new WaitForSecondsRealtime(.8f);
        yield return Capture(Path.Combine(dir,"Title.png"));
        game.ClosePanel();
        yield return new WaitForSecondsRealtime(.5f);
        yield return Capture(Path.Combine(dir,"World.png"));
        game.Open("orders");
        yield return new WaitForSecondsRealtime(.2f);
        yield return Capture(Path.Combine(dir,"Journal.png"));
        if(!File.Exists(Path.Combine(dir,"World.png"))) {Application.Quit(1);yield break;}
        Debug.Log("NAGI_RELEASE_SCREEN_CHECK_OK");Application.Quit();
    }
    bool busy;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F12) && !busy)
            StartCoroutine(Capture(Path.Combine(Application.persistentDataPath,"Photos",DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".png")));
    }
    IEnumerator Capture(string path)
    {
        busy=true;
        yield return new WaitForEndOfFrame();
        var shot=ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,shot.EncodeToPNG());
        }
        finally {Destroy(shot);busy=false;}
    }
}
