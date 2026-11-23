using UnityEngine;

/// <summary>Illustrated front cover and the seaside specimen notebook.</summary>
public sealed class IslandFolio : MonoBehaviour
{
    public IslandGame game;
    Texture2D cover, specimens, passage, shade;
    Font sans, serif;
    GUIStyle textStyle;
    int page;
    static readonly Color Ink = new Color32(31,58,52,255);
    static readonly Color Paper = new Color32(249,243,222,255);
    static readonly Color Sea = new Color32(42,107,96,255);
    static readonly Color Gold = new Color32(231,178,94,255);
    void Awake()
    {
        cover = Resources.Load<Texture2D>("Island/Media/Harbor");
        specimens = Resources.Load<Texture2D>("Island/Media/Specimens");
        passage = Resources.Load<Texture2D>("Island/Media/PassageSpecimens");
        sans = BrowserTools.ReadableFont(new[]{"Meiryo","Yu Gothic","Arial"},24);
        serif = BrowserTools.ReadableFont(new[]{"Yu Mincho","MS Mincho","Meiryo"},48);
        shade = new Texture2D(128,1,TextureFormat.RGBA32,false);
        for (int x = 0; x < 128; x++) shade.SetPixel(x,0,new Color(.025f,.12f,.12f,
            .86f * (1-Mathf.SmoothStep(0,1,x/115f))));
        shade.wrapMode = TextureWrapMode.Clamp; shade.Apply();
    }
    void OnDestroy(){Destroy(shade);Destroy(sans);Destroy(serif);}
    static void Fill(Rect r, Color c) { GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,c,0,0); }
    void Type(Rect r,string msg,int size,Color color,bool book=false,TextAnchor align=TextAnchor.MiddleLeft)
    {
        if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label){padding = new RectOffset(), wordWrap = true};
        textStyle.font = book ? serif : sans; textStyle.fontSize = size; textStyle.alignment = align;
        textStyle.normal.textColor = color; GUI.Label(r,msg,textStyle);
    }
    bool Button(Rect r,string msg,bool main=false)
    {
        bool hot=r.Contains(Event.current.mousePosition);
        Fill(r,main ? (hot ? Gold : Paper) : (hot ? Sea : Ink));
        Type(r,msg,16,main ? Ink : Paper,false,TextAnchor.MiddleCenter);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
    void OnGUI()
    {
        if (!game || (game.Panel!="title" && game.Panel!="tide" && game.Panel!="album")) return;
        var old = GUI.matrix; int depth = GUI.depth; GUI.depth = -2;
        float scale=Mathf.Min(Screen.width/960f, Screen.height/580f);
        float w=Screen.width/scale,h=Screen.height/scale;
        GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        if(game.Panel=="title") Cover(w,h); else Notebook(w,h);
        GUI.depth=depth;GUI.matrix=old;
    }
    void Cover(float w,float h)
    {
        var screen=new Rect(0,0,w,h);
        if(cover) GUI.DrawTexture(screen,cover,ScaleMode.ScaleAndCrop);
        GUI.DrawTexture(screen,shade);
        float x=w*.077f, y=h*.3f;
        Fill(new Rect(x,y-17,55,3),Gold);
        Type(new Rect(x,y,w*.8f,24),"凪島だより　／　N A G I    I S L A N D",16,Paper);
        var titleBox = new Rect(x-3,y+30,w*.84f,97);
        int titleSize = Mathf.Clamp(Mathf.FloorToInt(titleBox.width/IslandGame.TitleName.Length),38,58);
        Type(new Rect(titleBox.x+1.5f,titleBox.y+2,titleBox.width,titleBox.height),IslandGame.TitleName,titleSize,new Color(.05f,.10f,.10f,.82f),true);
        Type(titleBox,IslandGame.TitleName,titleSize,Paper,true);
        Type(new Rect(x,y+134,495,65),"木や岩を調べて、木材3つと石2つを集めよう。\n港に届けて船着き場を直し、自分の島を育てよう。",17,Paper);
        if(Button(new Rect(x,y+219,208,47),"島へ出かける　›",true)) game.ClosePanel();
        if(Button(new Rect(x+216,y+219,116,47),game.MusicOn?"音楽　ON":"音楽　OFF")) game.ToggleMusic();
        if(Button(new Rect(x,y+279,108,31),"ゲーム終了")) game.QuitGame();
    }
    void SampleIcon(Rect r,int index)
    {
        Texture2D plate=index<9?specimens:passage; index%=9;
        if(plate) GUI.DrawTextureWithTexCoords(r,plate,
            new Rect(index%3/3f,(2-index/3)/3f,1f/3,1f/3));
    }
    void Notebook(float w,float h)
    {
        Fill(new Rect(0,0,w,h),new Color(.03f,.12f,.13f,.67f));
        float x=(w-700)/2,y=(h-490)/2;
        Fill(new Rect(x+5,y+7,700,490),new Color(0,0,0,.22f));
        Fill(new Rect(x,y,700,490),Paper); Fill(new Rect(x,y,7,490),Sea);
        bool album=game.Panel=="album";
        Type(new Rect(x+31,y+17,400,21),album?"N A T U R A L I S T ’ S   J O U R N A L":"T I D E   S T U D Y",11,Sea);
        Type(new Rect(x+30,y+40,540,42),album?"浜辺図鑑":IslandGame.ShoreNames[game.Shore],29,Ink,true);
        if(Button(new Rect(x+585,y+25,88,33),"島へ戻る")) game.ClosePanel();
        Fill(new Rect(x+30,y+90,640,1),Sea);
        if(album)
        {
            int first=page/9*9;
            for(int i=first;i<first+9;i++)
            {
                Rect tile=new Rect(x+31+i%3*94,y+111+(i-first)/3*94,87,87);
                Fill(tile,i==page ? Gold : new Color(.83f,.86f,.76f));
                Rect face=new Rect(tile.x+3,tile.y+3,81,81);
                if(game.State.HasSpecimen(i))SampleIcon(face,i);
                else Type(face,"？",27,Sea,true,TextAnchor.MiddleCenter);
                if(GUI.Button(tile,GUIContent.none,GUIStyle.none))page=i;
            }
            if(Button(new Rect(x+32,y+388,271,30),first==0?"白砂の離島を見る　›":"‹　凪島を見る"))page=first==0?9:0;
            bool known=game.State.HasSpecimen(page);
            Rect pic=new Rect(x+399,y+99,178,178);
            if(known) SampleIcon(pic,page); else Type(pic,"？",64,Sea,true,TextAnchor.MiddleCenter);
            Type(new Rect(x+325,y+275,340,40),known?IslandGame.SpecimenNames[page]:"まだ見ぬ漂着物",25,Ink,true);
            Type(new Rect(x+325,y+318,330,32),IslandGame.ShoreNames[page/3]+"　／　"+new[]{"左","中央","右"}[page%3]+"の波",15,Sea);
            Type(new Rect(x+325,y+354,330,70),known?IslandGame.SpecimenNotes[page]:"浜辺の青い目印を訪ね、引き波のタイミングを合わせましょう。",16,Ink);
            Type(new Rect(x+32,y+413,270,42),$"採集の記録　{game.State.SpecimenCount} / {IslandGame.SpecimenNames.Length}",19,Sea);
        }
        else
        {
            int sample=game.LastSample;
            if(sample>=0) SampleIcon(new Rect(x+39,y+110,140,140),sample);
            Type(new Rect(x+(sample>=0?205:32),y+111,460,45),sample>=0?IslandGame.SpecimenNames[sample]:"引き波を待つ",25,Ink,true);
            Type(new Rect(x+(sample>=0?205:32),y+162,450,75),game.SampleMessage,17,Ink);
            float left=x+36,top=y+275,span=628;
            Fill(new Rect(left,top,span,30),new Color(.8f,.84f,.75f));
            for(int i=0;i<3;i++)
            {
                float mid=left+span*(i+.5f)/3f, wide=(.115f-.022f*i)*span*2;
                Fill(new Rect(mid-wide/2,top,wide,30),i==0?Sea:i==1?Gold:new Color(.68f,.44f,.49f));
                Type(new Rect(mid-48,top+35,96,25),game.State.HasSpecimen(game.Shore*3+i)?"記録済み":"未発見",13,Sea,false,TextAnchor.MiddleCenter);
            }
            Fill(new Rect(left+span*game.TideLevel-2,top-8,4,46),Ink);
            if(Button(new Rect(x+35,y+391,215,49),"E ／ Space　拾う")) game.TakeSample();
            if(Button(new Rect(x+474,y+391,190,49),"浜辺図鑑を開く"))game.Open("album");
        }
    }
}
