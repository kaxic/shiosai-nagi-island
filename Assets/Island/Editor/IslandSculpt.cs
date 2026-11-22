using UnityEngine;
using static IslandMesh;

// Measurements in metres; local zero is the centre of the footprint at ground level.
internal sealed class IslandSculpt
{
    readonly IslandMesh mesh;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    const string Timber="92603F", Endgrain="CC9562", DarkWood="694B40", Honey="D4A364";
    const string Iron="4D6770", Canvas="FBDE9F", Leaf="5C9860";

    internal IslandSculpt(IslandMesh mesh) { this.mesh=mesh; }

    void Box(float x,float y,float z,float w,float h,float d,string color,float pitch=0,float yaw=0,float roll=0)
    {
        mesh.Box(V(x,y,z),V(w,h,d),Paint(color),Quaternion.Euler(pitch,yaw,roll));
    }
    void Blob(float x,float y,float z,float w,float h,float d,string color,int seed,int sides=8)
    {
        mesh.Blob(V(x,y,z),V(w,h,d),Paint(color),seed,sides);
    }
    void Pole(Vector3 a,Vector3 b,float r,float s,string color,int sides=9)
    {
        mesh.Pole(a,b,r,s,Paint(color),sides);
    }
    void Disc(float x,float y,float z,float r,float top,float height,string color,int sides=12)
    {
        Pole(V(x,y,z),V(x,y+height,z),r,top,color,sides);
    }
    void Beam(Vector3 a,Vector3 b,float w,float d,string color)
    {
        mesh.Box((a+b)*.5f,V(w,Vector3.Distance(a,b),d),Paint(color),Quaternion.FromToRotation(Vector3.up,b-a));
    }

    internal void Build(string kind)
    {
        switch(kind)
        {
            case "cabin": Cabin(); break;
            case "tree": Tree(); break;
            case "stump": Stump(); break;
            case "rock": Rocks(false); break;
            case "pebbles": Rocks(true); break;
            case "workbench": Workbench(); break;
            case "farm": Farm(); break;
            case "sprout": Crops(false); break;
            case "ripe": Crops(true); break;
            case "bench": Bench(); break;
            case "lantern": Lantern(); break;
            case "planter": Planter(); break;
            case "table": Table(); break;
            case "bookshelf": Bookshelf(); break;
            case "brazier": Brazier(); break;
            case "arch": Arch(); break;
            case "birdhouse": Birdhouse(); break;
            case "daybed": Daybed(); break;
            case "player": Player(); break;
            default: throw new System.ArgumentException("Unknown island model: "+kind);
        }
    }

    void Cabin()
    {
        Box(0,.18f,0,3.3f,.36f,2.42f,"7C8277");
        for(int i=0;i<8;i++)
            Box(-1.43f+i*.41f,.22f,-1.22f,.38f,.25f,.1f,i%2==0?"A7A596":"C0B69D");
        Box(0,1.36f,0,3.15f,1.99f,2.28f,"F2D9A4");
        for(int i=0;i<6;i++)
        {
            float y=.46f+i*.31f;
            Box(0,y,-1.153f,3.14f,.027f,.022f,"DDC294");
            Box(1.582f,y,0,.023f,.024f,2.28f,"D8BD90");
        }
        foreach(float x in new[]{-1.56f,1.56f})
        foreach(float z in new[]{-1.13f,1.13f}) Box(x,1.34f,z,.13f,2.14f,.13f,Timber);
        Box(0,.40f,-1.18f,3.24f,.14f,.13f,Timber);
        Box(0,2.30f,-1.18f,3.35f,.16f,.16f,DarkWood);
        // South-facing porch with a blue split curtain and framed sash window.
        Box(.61f,1.15f,-1.19f,.91f,1.6f,.13f,DarkWood);
        for(int i=0;i<5;i++) Box(.29f+i*.16f,1.14f,-1.274f,.15f,1.49f,.055f,i%2==0?Timber:"A4754C");
        Blob(.85f,1.04f,-1.32f,.045f,.045f,.033f,"F1C469",141);
        Box(.61f,1.99f,-1.32f,1.12f,.075f,.10f,DarkWood);
        for(int i=0;i<3;i++)
        {
            Box(.31f+i*.30f,1.76f,-1.34f,.285f,.40f,.05f,"426D7B",0,0,i-1);
            Box(.31f+i*.30f,1.64f,-1.373f,.28f,.033f,.014f,"A7D3C3");
        }
        Blob(.61f,1.79f,-1.377f,.080f,.082f,.008f,"EFDCA2",44);
        Box(-.80f,1.40f,-1.204f,.89f,.93f,.15f,DarkWood);
        Box(-.80f,1.41f,-1.29f,.70f,.73f,.045f,"719A9B");
        Box(-.80f,1.41f,-1.325f,.05f,.76f,.04f,Canvas);
        Box(-.80f,1.41f,-1.325f,.73f,.05f,.04f,Canvas);
        Box(-.80f,.90f,-1.26f,1.06f,.10f,.29f,Timber);
        Box(1.602f,1.45f,.18f,.14f,.86f,.78f,DarkWood);
        Box(1.68f,1.46f,.18f,.03f,.66f,.58f,"789D99");
        Box(1.70f,1.46f,.18f,.04f,.68f,.045f,Canvas);
        Box(1.70f,1.46f,.18f,.04f,.045f,.60f,Canvas);
        Box(.54f,.26f,-1.44f,1.37f,.18f,.64f,Honey);
        Box(.54f,.125f,-1.78f,1.16f,.18f,.37f,Endgrain);
        // Tiled gable with alternating courses, ridge caps, and projecting eaves.
        float halfRoof=1.48f, rise=1.08f, peak=3.40f;
        float slope=Mathf.Atan2(rise,halfRoof)*Mathf.Rad2Deg;
        for(int side=-1;side<=1;side+=2)
        {
            float z=side*halfRoof*.5f;
            Box(0,peak-rise*.5f,z,3.76f,.15f,1.85f,DarkWood,side*slope);
            for(int row=0;row<5;row++)
            {
                float fromPeak=(row+.49f)/5f;
                float offset=row%2==0?0:.5f;
                for(int col=-1;col<9;col++)
                {
                    float left=Mathf.Max(0,col+offset), right=Mathf.Min(9,col+1+offset);
                    if(right<=left) continue;
                    float x=-1.86f+(left+right)*.5f*(3.72f/9);
                    string[] tiles={"BE7050","D48659","D68D63","CD7955","C57B52","E39A69"};
                    string tone=tiles[(row*7+(col+2)*13+(side+1))%tiles.Length];
                    mesh.surface=Surface.Roof;
                    Box(x,peak-rise*fromPeak+.085f,side*halfRoof*fromPeak,
                        (right-left)*(3.72f/9)-.012f,.09f,.39f,tone,side*slope);
                    mesh.surface=Surface.Auto;
                }
            }
            Box(0,peak-rise-.02f,side*halfRoof,3.85f,.13f,.13f,Timber);
            foreach(float x in new[]{-1.59f,1.59f})
            {
                var a=V(x,peak,0); var b=V(x,peak-rise,side*halfRoof);
                mesh.Face(a,b,V(x,peak-rise,0),Paint("DAB480"),Vector3.right*x);
                Beam(a,b,.10f,.11f,Timber);
            }
        }
        for(int i=0;i<9;i++) Pole(V(-1.95f+i*.43f,peak+.10f,0),V(-1.50f+i*.43f,peak+.10f,0),.115f,.12f,"C07851",8);
        Box(-1.04f,3.20f,.35f,.39f,.85f,.40f,"A68877");
        for(int i=0;i<3;i++) Box(-1.04f,3.13f+i*.145f,.35f,.42f,.03f,.43f,"C5A88C");
        Box(-1.04f,3.65f,.35f,.53f,.13f,.53f,"896E66");
        Disc(-1.04f,3.70f,.35f,.148f,.14f,.012f,"554F50");
        // Water barrel with metal hoops.
        Disc(-1.58f,.02f,-1.26f,.27f,.31f,.66f,Timber);
        foreach(float y in new[]{.14f,.54f}) Disc(-1.58f,y,-1.26f,.307f,.308f,.045f,Iron);
        Disc(-1.58f,.68f,-1.26f,.292f,.292f,.025f,Endgrain);
    }

    void Tree()
    {
        Pole(V(0,0,0),V(-.09f,1.02f,.03f),.34f,.235f,Timber,8);
        Pole(V(-.09f,1.02f,.03f),V(.15f,2.17f,0),.235f,.13f,"A3734E",8);
        Pole(V(-.08f,.96f,0),V(-.64f,1.94f,-.14f),.17f,.089f,Timber,7);
        Pole(V(.02f,1.42f,0),V(.76f,2.04f,.20f),.15f,.07f,Timber,7);
        for(int j=0;j<5;j++)
        {
            float a=j*1.256f+.2f;
            Pole(V(Mathf.Cos(a)*.49f,.035f,Mathf.Sin(a)*.50f),V(0,.29f,0),.05f,.21f,Timber,7);
        }
        Blob(-.64f,2.00f,-.10f,.86f,.70f,.83f,"76AB65",9);
        Blob(.67f,2.17f,.16f,.80f,.72f,.80f,"79B268",12);
        Blob(.05f,2.62f,-.02f,1.05f,.91f,1.00f,"95C575",2,9);
        Blob(-.37f,2.35f,.55f,.82f,.67f,.70f,"80B36B",102);
        Blob(.53f,2.32f,-.63f,.66f,.58f,.69f,"87BA69",18);
        Blob(-.80f,2.58f,-.24f,.56f,.48f,.54f,"ADC977",29);
    }
    void Stump()
    {
        Disc(0,0,0,.34f,.28f,.35f,Timber,9);
        Disc(0,.35f,0,.26f,.26f,.018f,Endgrain,9);
        Disc(.02f,.37f,0,.155f,.15f,.006f,"AE7F54",10);
        Disc(.02f,.378f,0,.12f,.12f,.004f,"D7A675",10);
    }
    void Rocks(bool depleted)
    {
        if(!depleted)
        {
            Blob(-.18f,.35f,.05f,.70f,.62f,.61f,"899A9E",23,7);
            Blob(.37f,.27f,.10f,.50f,.44f,.47f,"9DA9A7",12,6);
            Blob(-.32f,.84f,.03f,.28f,.070f,.25f,"ADB68C",41,6);
        }
        Blob(-.52f,.11f,-.22f,.25f,.16f,.26f,"A7ADA2",51,6);
        Blob(.42f,.07f,-.34f,.21f,.12f,.14f,"A7AFAA",91,6);
        Blob(.09f,.045f,-.53f,.13f,.09f,.10f,"B9BAA5",26,5);
    }
    void Workbench()
    {
        foreach(float x in new[]{-.69f,.69f})
        foreach(float z in new[]{-.29f,.29f}) Box(x,.44f,z,.13f,.86f,.13f,Timber);
        Box(0,.26f,0,1.57f,.08f,.66f,"AB7B50");
        Box(0,.72f,-.36f,1.66f,.17f,.09f,Timber);
        for(int j=0;j<4;j++) Box(0,.88f,-.32f+j*.22f,1.83f,.13f,.212f,j%2==0?Honey:Endgrain);
        foreach(float x in new[]{-.78f,.78f}) Box(x,1.15f,.38f,.10f,.74f,.10f,Timber);
        Box(0,1.47f,.40f,1.68f,.19f,.07f,Endgrain);
        Box(-.46f,1.32f,.32f,.07f,.31f,.055f,Iron,0,0,-8);
        Box(-.46f,1.47f,.31f,.21f,.065f,.075f,Iron);
        Box(.24f,.97f,-.11f,.45f,.025f,.29f,Canvas,0,-11);
        Box(.22f,.99f,-.10f,.25f,.012f,.018f,Iron,0,-11);
        Box(-.39f,1.00f,-.08f,.45f,.07f,.065f,Timber,0,23);
        Box(-.57f,1.015f,-.005f,.15f,.13f,.25f,Iron,0,23);
        Box(.51f,.41f,.04f,.31f,.22f,.29f,"7998A0");
    }
    void Farm()
    {
        mesh.surface=Surface.Soil;
        Box(0,.059f,0,2.31f,.118f,1.57f,"936E52");
        mesh.surface=Surface.Auto;
        foreach(float x in new[]{-1.18f,1.18f}) Box(x,.11f,0,.072f,.17f,1.60f,Endgrain);
        foreach(float z in new[]{-.79f,.79f}) Box(0,.11f,z,2.42f,.17f,.065f,Endgrain);
        for(int j=0;j<3;j++)
        {
            Blob(0,.119f,(j-1)*.49f,1.10f,.084f,.178f,"AF835B",6+j,12);
        }
        foreach(float x in new[]{-1.18f,1.18f})
        foreach(float z in new[]{-.79f,.79f}) Disc(x,.02f,z,.076f,.07f,.20f,Timber,7);
        Box(1.05f,.40f,.64f,.054f,.57f,.056f,Timber,0,0,-6);
        Box(1.05f,.61f,.62f,.32f,.18f,.06f,Canvas,0,0,-6);
        Box(1.05f,.61f,.582f,.17f,.022f,.012f,"759065",0,0,-6);
    }
    void Crops(bool ripe)
    {
        for(int j=0;j<3;j++)
        for(int i=0;i<4;i++)
        {
            float x=-.78f+i*.50f+Mathf.Sin(i*3+j)*.05f, z=(j-1)*.49f;
            if(ripe)
            {
                Blob(x,.232f,z,.127f,.126f,.126f,(i+j)%4==0?"EDC8B2":"F4E2BC",51+i*9+j,7);
                Blob(x,.308f,z,.096f,.047f,.096f,(i+j)%4==0?"D6A9A0":"DBCE9E",53+i*9+j,7);
            }
            float y=ripe?.338f:.194f, scale=ripe?1.2f:.80f;
            FlowerLeaf(V(x,y,z),.06f*scale,.20f*scale,i*45+j*28,"619C62");
            FlowerLeaf(V(x,y,z),.058f*scale,.18f*scale,120+i*45+j*28,"82B363");
            FlowerLeaf(V(x,y,z),.058f*scale,.21f*scale,230+i*45+j*28,"73A95B");
        }
    }
    void FlowerLeaf(Vector3 basePoint,float width,float length,float angle,string tone)
    {
        Vector3 front=Quaternion.Euler(0,angle,0)*Vector3.forward;
        Vector3 right=Vector3.Cross(Vector3.up,front);
        Vector3 middle=basePoint+(front*.61f+Vector3.up*.62f)*length;
        Vector3 tip=basePoint+(front*.75f+Vector3.up)*length;
        Vector3 crease=middle+Vector3.up*width*.56f;
        Vector3 l=middle+right*width,r=middle-right*width;
        Color c=Paint(tone);
        mesh.Face(basePoint,l,crease,c,Vector3.up);
        mesh.Face(l,tip,crease,c,Vector3.up);
        mesh.Face(basePoint,crease,r,Tone(c,.91f),Vector3.up);
        mesh.Face(r,crease,tip,Tone(c,.91f),Vector3.up);
    }
    void Bench()
    {
        foreach(float x in new[]{-.58f,.58f})
        {
            foreach(float z in new[]{-.22f,.23f}) Box(x,.25f,z,.12f,.50f,.13f,"82674D");
            Box(x,.65f,.24f,.095f,.84f,.10f,Timber,-7);
        }
        for(int j=0;j<3;j++) Box(0,.51f,-.22f+j*.19f,1.60f,.095f,.175f,j%2==1?"E0AE76":Honey);
        Box(0,.82f,.27f,1.63f,.145f,.092f,Endgrain,-7);
        Box(0,1.02f,.295f,1.63f,.15f,.093f,Honey,-7);
        foreach(float x in new[]{-.58f,.58f}) Blob(x,.82f,.210f,.026f,.026f,.012f,Iron,7);
    }
    void Lantern()
    {
        Disc(0,0,0,.34f,.30f,.16f,"94998E",8);
        Disc(0,.16f,0,.20f,.16f,.15f,Endgrain,8);
        Box(0,.79f,0,.15f,1.19f,.16f,Timber);
        Box(.18f,1.36f,0,.51f,.10f,.12f,Timber);
        Pole(V(.36f,1.33f,0),V(.36f,1.19f,0),.022f,.022f,Iron);
        Disc(.36f,.77f,0,.19f,.20f,.38f,"FFD784",8);
        foreach(float y in new[]{.77f,1.13f}) Disc(.36f,y,0,.22f,.22f,.05f,Iron,8);
        foreach(float x in new[]{.24f,.48f})
        foreach(float z in new[]{-.13f,.13f}) Box(x,.96f,z,.026f,.34f,.026f,"9F7656");
        Disc(.36f,1.16f,0,.23f,.052f,.11f,"65817C",8);
    }
    void Planter()
    {
        Box(0,.22f,0,.91f,.42f,.60f,Timber);
        Box(0,.43f,0,.90f,.025f,.55f,"74564A");
        foreach(float z in new[]{-.30f,.30f})
        {
            Box(0,.39f,z,.97f,.12f,.075f,Endgrain);
            Box(0,.19f,z,.90f,.065f,.065f,Honey);
        }
        for(int i=0;i<7;i++)
        {
            float x=Mathf.Sin(i*3.72f)*.31f, z=Mathf.Cos(i*2.74f)*.17f,y=.65f+.13f*Mathf.Sin(i*4);
            Pole(V(x,.43f,z),V(x+.022f,y,z),.016f,.014f,Leaf,5);
            FlowerLeaf(V(x,.49f,z),.075f,.20f,54*i,"75A65D");
            FlowerLeaf(V(x,.48f,z),.06f,.15f,152+54*i,"87B873");
            Flower(V(x+.022f,y,z),.11f,i%2==0?"F5B266":"F8DDA2",i);
        }
    }
    internal void Flower(Vector3 center,float size,string petal,int seed)
    {
        for(int j=0;j<5;j++)
        {
            float a=j*Mathf.PI*.4f+seed;
            Vector3 p=center+V(Mathf.Sin(a)*size*.48f,.001f,Mathf.Cos(a)*size*.48f);
            mesh.Blob(p,V(size*.43f,size*.20f,size*.46f),Paint(petal),seed+j,5);
        }
        mesh.Blob(center+Vector3.up*size*.12f,V(size*.34f,size*.25f,size*.34f),Paint("E7A04A"),seed+5,5);
    }

    void Table()
    {
        // A breakfast for two, with indigo linen, a celadon pot and little teacups.
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;
            var foot=V(Mathf.Cos(a)*.48f,.055f,Mathf.Sin(a)*.48f);
            Beam(foot,V(0,.26f,0),.12f,.13f,Timber);
        }
        Disc(0,.10f,0,.16f,.105f,.52f,Timber,10);
        Disc(0,.61f,0,.57f,.67f,.09f,Timber,18);
        Disc(0,.70f,0,.687f,.684f,.075f,Honey,24);
        Box(-.10f,.781f,0,.31f,.018f,1.27f,"739BA0",0,9);
        Box(-.19f,.776f,-.63f,.31f,.10f,.02f,"6B9397",0,9);
        Disc(.14f,.78f,.08f,.108f,.144f,.14f,"94B3A4",12);
        Disc(.14f,.92f,.08f,.15f,.064f,.046f,"719D92",12);
        Disc(.14f,.966f,.08f,.035f,.032f,.033f,Iron);
        Pole(V(.245f,.838f,.08f),V(.345f,.912f,.08f),.058f,.032f,"94B3A4",8);
        foreach(float x in new[]{-.29f,.20f})
        {
            float z=x<0?.20f:-.30f;
            Disc(x,.783f,z,.079f,.094f,.084f,"EEDAB2",10);
            Disc(x,.867f,z,.071f,.071f,.004f,"8A7053",10);
        }
    }

    void Bookshelf()
    {
        Box(0,.84f,.20f,1.36f,1.6f,.085f,"78988B");
        foreach(float x in new[]{-.66f,.66f}) Box(x,.87f,0,.105f,1.70f,.52f,Timber);
        foreach(float y in new[]{.14f,.68f,1.20f,1.72f}) Box(0,y,0,1.48f,.10f,.57f,y>1.7f?Honey:Endgrain);
        string[] cloth={"B76553","6C9392","DFBB77","9DA568","6F8492","BD8774"};
        for(int row=0;row<3;row++)
        {
            int count=row==1?4:row==2?3:6;
            for(int j=0;j<count;j++)
            {
                float h=.28f+.10f*Mathf.Sin(row*4+j*6.7f);
                float x=-.53f+j*.142f, bottom=.20f+row*.523f;
                float lean=j==count-1?-10:0;
                Box(x,bottom+h*.5f,-.058f,.112f,h,.31f,cloth[(row*4+j)%cloth.Length],0,0,lean);
                Box(x,bottom+h*.71f,-.219f,.076f,.033f,.012f,Canvas,0,0,lean);
                Box(x,bottom+h*.20f,-.219f,.075f,.014f,.012f,"D7BBC0",0,0,lean);
            }
        }
        Disc(.38f,.735f,0,.127f,.147f,.15f,"AB785C",10);
        FlowerLeaf(V(.38f,.87f,0),.082f,.22f,35,"90AD71");
        FlowerLeaf(V(.38f,.87f,0),.09f,.23f,190,"6E9F73");
        Disc(.35f,1.256f,0,.11f,.167f,.16f,"CFAB7A",10);
        Disc(.35f,1.415f,0,.167f,.075f,.12f,"D5B586",10);
    }

    void Brazier()
    {
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;
            Pole(V(Mathf.Cos(a)*.43f,.045f,Mathf.Sin(a)*.43f),V(Mathf.Cos(a)*.27f,.40f,Mathf.Sin(a)*.27f),.075f,.057f,Iron,6);
        }
        Disc(0,.25f,0,.20f,.52f,.26f,"626E71",14);
        Disc(0,.505f,0,.54f,.53f,.052f,"8E8F82",14);
        Disc(0,.553f,0,.485f,.485f,.012f,"534B48",14);
        Pole(V(-.31f,.57f,-.15f),V(.30f,.64f,.14f),.085f,.074f,"80583F",7);
        Pole(V(-.24f,.61f,.19f),V(.24f,.64f,-.21f),.078f,.063f,"A57048",7);
        mesh.surface=Surface.Ember;
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI/3; float x=Mathf.Sin(a)*.17f,z=Mathf.Cos(a)*.14f,h=.26f+.035f*i;
            Pole(V(x,.61f,z),V(x+.024f,.61f+h*.33f,z),.075f,.098f,"DA7B46",7);
            Pole(V(x+.024f,.61f+h*.33f,z),V(x-.056f,.61f+h,z+.045f),.098f,.006f,"F0A454",7);
        }
        Pole(V(0,.61f,0),V(-.045f,1.15f,.025f),.126f,.004f,"FBD280",8);
        mesh.surface=Surface.Auto;
    }

    void Arch()
    {
        foreach(float x in new[]{-.68f,.68f})
        {
            Disc(x,.0f,0,.22f,.15f,.17f,"ACAA95",8);
            Box(x,.96f,0,.123f,1.73f,.16f,Timber);
            for(int i=0;i<4;i++)
                Beam(V(x-.09f,.43f+i*.29f,0),V(x+.10f,.67f+i*.29f,-.03f),.057f,.056f,"748A58");
        }
        Beam(V(-.82f,1.72f,0),V(-.38f,1.98f,0),.123f,.16f,Timber);
        Beam(V(-.38f,1.98f,0),V(.38f,1.98f,0),.123f,.16f,Timber);
        Beam(V(.38f,1.98f,0),V(.82f,1.72f,0),.123f,.16f,Timber);
        for(int j=0;j<17;j++)
        {
            float x=(j-8)*.093f, baseY=1.98f-.26f*Mathf.Pow(Mathf.Abs(x)/.80f,1.8f);
            float z=.06f*Mathf.Sin(j*2.27f);
            Blob(x,baseY,z,.118f,.111f,.124f,j%3==0?"99BA78":"76A06A",j+3,6);
            if(j%2==0)
            {
                int count=2+(j%4);
                for(int k=0;k<count;k++)
                    Blob(x+.027f*Mathf.Sin(k+j),baseY-.07f-k*.062f,z-.075f,.069f-k*.008f,.060f,.065f-k*.007f,
                        k==count-1?"D4C5CE":j%3==0?"AC91B4":"C0A6C4",217+j+k,6);
            }
        }
        for(int i=0;i<6;i++)
        {
            float x=i%2==0?-.68f:.68f;
            FlowerLeaf(V(x,.71f+i*.16f,-.055f),.064f,.167f,92*i,"81A86C");
        }
    }

    void Birdhouse()
    {
        Disc(0,0,0,.27f,.20f,.13f,"9B9D87",9);
        Box(0,.62f,0,.14f,1.12f,.14f,Timber);
        Beam(V(0,.86f,0),V(.23f,1.14f,.04f),.067f,.066f,Endgrain);
        Box(0,1.17f,0,.65f,.076f,.64f,Honey);
        Box(0,1.42f,.045f,.46f,.43f,.39f,Endgrain);
        mesh.Face(V(-.23f,1.63f,-.153f),V(.23f,1.63f,-.153f),V(0,1.85f,-.153f),Paint(Honey),Vector3.back);
        Box(-.145f,1.71f,.04f,.398f,.072f,.56f,"65959A",0,0,40);
        Box(.145f,1.71f,.04f,.398f,.072f,.56f,"80ABB0",0,0,-40);
        Pole(V(0,1.49f,-.153f),V(0,1.49f,-.163f),.077f,.075f,DarkWood,12);
        Pole(V(0,1.34f,-.155f),V(0,1.34f,-.32f),.023f,.022f,Timber,8);
        Blob(.20f,1.27f,-.22f,.085f,.080f,.123f,"CBA660",37);
        Blob(.20f,1.33f,-.29f,.065f,.057f,.062f,"668A8B",23);
        Pole(V(.20f,1.33f,-.334f),V(.20f,1.329f,-.384f),.028f,.001f,"E1AE63",5);
        Blob(.235f,1.34f,-.338f,.010f,.010f,.009f,DarkWood,27,5);
    }

    void Daybed()
    {
        foreach(float x in new[]{-.70f,.70f})
        foreach(float z in new[]{-.32f,.32f}) Box(x,.24f,z,.096f,.48f,.106f,Timber);
        Box(0,.42f,0,1.59f,.14f,.79f,Honey);
        mesh.surface=Surface.Cloth;
        Box(0,.535f,0,1.49f,.13f,.75f,"E5CF9B");
        Box(.13f,.61f,-.024f,.64f,.033f,.80f,"69989C");
        Box(.13f,.519f,-.422f,.64f,.20f,.028f,"6C9398");
        foreach(float x in new[]{-.12f,-.07f,.34f,.39f})
        {
            Box(x,.631f,-.024f,.018f,.009f,.80f,"E9DAAB");
            Box(x,.52f,-.439f,.018f,.196f,.009f,"E9DAAB");
        }
        Blob(-.51f,.675f,.053f,.22f,.145f,.29f,"C77F67",58,10);
        Blob(-.31f,.666f,.16f,.16f,.098f,.18f,"E5B276",78,9);
        mesh.surface=Surface.Auto;
        foreach(float x in new[]{-.77f,.77f})
        {
            Box(x,.58f,.25f,.070f,.35f,.073f,Timber);
            Box(x,.69f,0,.09f,.085f,.75f,Honey);
        }
    }

    void Player()
    {
        mesh.surface=Surface.Cloth;
        // +Z forward. The straw hat, red scarf and rolled blanket read from the overhead camera.
        foreach(float x in new[]{-.119f,.119f})
        {
            Box(x,.293f,0,.175f,.37f,.19f,"475F69");
            Blob(x,.110f,.043f,.123f,.112f,.168f,"79523E",73);
            Box(x,.19f,.01f,.19f,.064f,.20f,"C3A476");
        }
        Blob(0,.61f,0,.258f,.272f,.177f,"4E9290",62,9);
        Box(0,.459f,0,.41f,.071f,.27f,"806344");
        foreach(float x in new[]{-.252f,.252f})
        {
            Pole(V(x*.89f,.752f,0),V(x*1.14f,.572f,.017f),.099f,.083f,"6FA8A0",8);
            Blob(x*1.15f,.505f,.025f,.074f,.106f,.082f,"E4AC7D",17);
        }
        Disc(0,.740f,0,.125f,.123f,.10f,"CE916D",8);
        Disc(0,.754f,0,.158f,.172f,.07f,"C25E4A",8);
        Box(.092f,.704f,.167f,.098f,.168f,.045f,"D86B4F",0,0,25);
        Blob(0,.974f,.015f,.216f,.233f,.196f,"EBB78B",14,10);
        Blob(0,1.024f,-.047f,.226f,.202f,.170f,"644E43",40);
        foreach(float x in new[]{-.082f,.082f})
            Blob(x,.983f,.192f,.019f,.030f,.011f,"4A4640",41,5);
        Blob(0,.933f,.211f,.032f,.030f,.022f,"DE9E77",42,5);
        Disc(0,1.10f,0,.399f,.402f,.043f,"C29054",17);
        Disc(0,1.14f,0,.407f,.25f,.052f,"EDC689",17);
        Disc(0,1.18f,0,.243f,.207f,.130f,"E4B774",14);
        Disc(0,1.31f,0,.207f,.145f,.058f,"ECCA8D",14);
        Disc(0,1.185f,0,.245f,.237f,.046f,"9A6C48",14);
        Box(0,.625f,-.197f,.325f,.32f,.145f,"AC714B");
        Box(0,.716f,-.269f,.33f,.145f,.064f,"CA975C");
        foreach(float x in new[]{-.118f,.118f}) Box(x,.623f,-.31f,.033f,.20f,.027f,"745747");
        Pole(V(-.19f,.798f,-.211f),V(.19f,.798f,-.211f),.079f,.079f,"93A991",9);
    }
}
