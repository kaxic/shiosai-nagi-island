using UnityEngine;

// A small reusable pool of paper-cut flecks shared by picking, harvesting and decorating.
public sealed class IslandFeedback : MonoBehaviour
{
    readonly SpriteRenderer[] sprites=new SpriteRenderer[18];
    readonly Vector3[] velocities=new Vector3[18];
    readonly float[] remaining=new float[18];
    Sprite fleck; Texture2D ink; Camera view; int cursor;
    void Awake()
    {
        view=Camera.main;
        ink=new Texture2D(24,24,TextureFormat.RGBA32,false);
        var pixels=new Color32[24*24];
        for(int y=0;y<24;y++)for(int x=0;x<24;x++)
        {
            float dx=(x-11.5f)/11,dy=(y-11.5f)/11;
            float edge=1-Mathf.Abs(dx)*1.55f-Mathf.Abs(dy);
            pixels[y*24+x]=new Color(1,1,1,Mathf.Clamp01(edge*9));
        }
        ink.SetPixels32(pixels);ink.Apply();
        fleck=Sprite.Create(ink,new Rect(0,0,24,24),new Vector2(.5f,.5f),24);
        for(int i=0;i<sprites.Length;i++)
        {
            var go=new GameObject("紙片");go.transform.SetParent(transform,false);
            sprites[i]=go.AddComponent<SpriteRenderer>();sprites[i].sprite=fleck;sprites[i].enabled=false;
        }
    }
    public void Burst(Vector3 origin,Color color)
    {
        for(int j=0;j<9;j++)
        {
            int i=(cursor++)%sprites.Length;
            float a=j*2.39996f; velocities[i]=new Vector3(Mathf.Cos(a)*1.2f,1.2f+(j%4)*.32f,Mathf.Sin(a)*1.2f);
            remaining[i]=.56f+j%3*.06f;var s=sprites[i];s.enabled=true;s.color=Color.Lerp(color,Color.white,.12f*(j%4));
            s.transform.position=origin+Vector3.up*.38f;
            if(view)s.transform.rotation=view.transform.rotation*Quaternion.Euler(0,0,j*53);
            s.transform.localScale=Vector3.one*(.1f+j%3*.035f);
        }
    }
    void Update()
    {
        for(int i=0;i<sprites.Length;i++)
        {
            if(!sprites[i].enabled)continue;
            float dt=Time.deltaTime; remaining[i]-=dt;
            if(remaining[i]<=0){sprites[i].enabled=false;continue;}
            velocities[i]+=Vector3.down*(dt*5);
            sprites[i].transform.position+=velocities[i]*dt;
            sprites[i].transform.Rotate(0,0,80*dt);
            var c=sprites[i].color;c.a=Mathf.Clamp01(remaining[i]*5);sprites[i].color=c;
        }
    }
    void OnDestroy(){Destroy(fleck);Destroy(ink);}
}
