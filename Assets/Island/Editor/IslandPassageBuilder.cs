using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

// Rebuilds only missing expedition assets. All geometry is editable in the resulting prefabs and scene.
public static class IslandPassageBuilder
{
    const string Base="Assets/Island/Art/";
    static readonly string[] ids={"parasol","parcelcart","shellsink","telescope","herbdryer","windchime","kiln","canoe","wayclock"};
    static readonly string[] names={"帆布のパラソル","港の手押し車","貝洗いの水盤","岬の望遠鏡","薬草の乾燥棚","硝子の風鈴棚","潮焼きの窯","小舟の花壇","渡し場の時計"};
    static readonly int[] costs={4,6,3,5,5,4,3,6,5}, stones={2,2,5,4,1,3,6,3,4};
    static Material cedar,enamel,cream,rose,stone,brass,moss,water,white;
    static Transform Group(Transform parent,string name,Vector3 pos)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;
    }
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static GameObject Piece(Transform parent,PrimitiveType shape,Vector3 pos,Vector3 size,Material mat,Vector3 euler,string name)
    {
        var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;
        o.transform.localScale=size;o.transform.localRotation=Quaternion.Euler(euler);o.GetComponent<MeshRenderer>().sharedMaterial=mat;
        Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static GameObject Box(Transform t,float x,float y,float z,float a,float b,float c,Material m,string n="部材")
        =>Piece(t,PrimitiveType.Cube,V(x,y,z),V(a,b,c),m,Vector3.zero,n);
    static GameObject Ball(Transform t,float x,float y,float z,float a,float b,float c,Material m,string n="飾り")
        =>Piece(t,PrimitiveType.Sphere,V(x,y,z),V(a,b,c),m,Vector3.zero,n);
    static void Rail(Transform t,Vector3 a,Vector3 b,float r,Material m,string n="支柱")
    {
        var o=Piece(t,PrimitiveType.Cylinder,(a+b)/2,V(r,(b-a).magnitude/2,r),m,Vector3.zero,n);
        o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void Disc(Transform t,float x,float y,float z,float wx,float depth,float wz,Material m,string n)
        =>Piece(t,PrimitiveType.Cylinder,V(x,y,z),V(wx,depth/2,wz),m,Vector3.zero,n);
    static Material Paint(string name,string texture,string rgb,float gloss=.18f)
    {
        string file=Base+"Passage/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(file);if(m)return m;
        m=new Material(Shader.Find("Standard"));ColorUtility.TryParseHtmlString("#"+rgb,out var color);m.color=color;
        m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"Textures/"+texture+".png");
        m.SetFloat("_Glossiness",gloss);AssetDatabase.CreateAsset(m,file);return m;
    }
    static void Flower(Transform p,float x,float y,float z,Material petal,float s)
    {
        Rail(p,V(x,y,z),V(x+.055f*s,y+.48f*s,z),.035f*s,moss);
        for(int j=0;j<5;j++){float a=j*1.2566f;Ball(p,x+(.055f+Mathf.Sin(a)*.10f)*s,y+.48f*s,z+Mathf.Cos(a)*.1f*s,.18f*s,.095f*s,.17f*s,petal);}
        Ball(p,x+.055f*s,y+.527f*s,z,.12f*s,.08f*s,.12f*s,brass);
    }
    static void Item(Transform t,int n)
    {
        if(n==0){
            Disc(t,0,.075f,0,.81f,.15f,.81f,stone,"傘台");Rail(t,V(0,.12f,0),V(0,1.93f,0),.105f,cedar);
            for(int j=0;j<12;j++){float a=j*30f*Mathf.Deg2Rad;var p=Group(t,"帆布の傘骨",Vector3.zero);p.localEulerAngles=V(0,j*30,0);
                var sail=Ball(p,0,1.67f,.43f,.52f,.145f,.94f,j%2==0?cream:enamel);sail.transform.localEulerAngles=V(13,0,0);
                Rail(t,V(0,1.79f,0),V(Mathf.Sin(a)*.87f,1.55f,Mathf.Cos(a)*.87f),.031f,brass);}
            Ball(t,0,1.85f,0,.14f,.14f,.14f,brass);
        }else if(n==1||n==7){
            float length=n==1?1.32f:1.67f;Box(t,0,.46f,0,length,.13f,.63f,cedar);
            foreach(int dir in new[]{-1,1}){var side=Box(t,0,.65f,dir*.34f,length,.40f,.11f,n==1?cedar:enamel);side.transform.localEulerAngles=V(dir*13,0,0);
                for(int j=0;j<4;j++)Ball(t,(j-1.5f)*.29f,.69f,dir*.409f,.052f,.052f,.052f,brass);}
            if(n==1){foreach(int z in new[]{-1,1}){Rail(t,V(-.35f,.34f,z*.40f),V(-.35f,.34f,z*.55f),.65f,stone,"車輪");
                Rail(t,V(.41f,.5f,z*.27f),V(1.0f,.73f,z*.27f),.076f,cedar);}Box(t,.38f,.24f,0,.10f,.4f,.52f,cedar);
                for(int j=0;j<3;j++)Ball(t,-.35f+j*.30f,.82f,0,.32f,.26f,.30f,j==1?rose:brass);}
            else{foreach(int x in new[]{-1,1}){Box(t,x*.46f,.23f,0,.20f,.39f,.70f,stone);Box(t,x*.77f,.62f,0,.15f,.32f,.60f,enamel);}
                for(int j=0;j<7;j++)Flower(t,(j-3)*.20f,.56f,(j%2-.5f)*.24f,j%2==0?rose:cream,.7f);}
        }else if(n==2||n==6){
            Disc(t,0,.12f,0,1.18f,.23f,.92f,stone,"礎石");
            if(n==2){Disc(t,0,.49f,0,.54f,.65f,.50f,enamel,"台座");Ball(t,0,.82f,0,1.34f,.39f,.88f,cream);
                Ball(t,0,1.00f,0,1.04f,.048f,.61f,water);Rail(t,V(-.47f,.84f,0),V(-.47f,1.29f,0),.074f,brass);Rail(t,V(-.47f,1.29f,0),V(-.18f,1.29f,0),.067f,brass);}
            else{Ball(t,0,.61f,0,1.30f,1.18f,1.06f,rose);Ball(t,0,.50f,-.46f,.67f,.59f,.16f,stone);
                for(int j=0;j<3;j++)Ball(t,(j-1)*.15f,.37f,-.55f,.17f,.29f,.055f,brass);
                Disc(t,0,1.09f,.16f,.40f,.53f,.40f,cream,"煙突");Disc(t,0,1.37f,.16f,.49f,.10f,.49f,rose,"煙突笠");}
        }else if(n==3||n==8){
            if(n==3){for(int j=0;j<3;j++){float a=j*2.0944f;Rail(t,V(Mathf.Sin(a)*.58f,.06f,Mathf.Cos(a)*.58f),V(0,1.05f,0),.11f,cedar);}
                Rail(t,V(-.43f,1.12f,0),V(.68f,1.51f,0),.26f,enamel,"鏡筒");Rail(t,V(.56f,1.47f,0),V(.74f,1.53f,0),.35f,brass);Rail(t,V(.73f,1.53f,0),V(.747f,1.54f,0),.28f,water);}
            else{Disc(t,0,.09f,0,.56f,.18f,.53f,stone,"台");Rail(t,V(0,.12f,0),V(0,1.72f,0),.14f,enamel);
                Rail(t,V(0,1.62f,-.12f),V(0,1.62f,.12f),.76f,brass);Rail(t,V(0,1.62f,-.145f),V(0,1.62f,-.155f),.67f,cream);
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Ball(t,.27f*Mathf.Sin(a),1.62f+.27f*Mathf.Cos(a),-.164f,.035f,.035f,.02f,stone);}
                Rail(t,V(0,1.62f,-.18f),V(.16f,1.71f,-.18f),.029f,stone);Rail(t,V(0,1.62f,-.181f),V(-.08f,1.82f,-.181f),.029f,stone);}
        }else{
            foreach(int d in new[]{-1,1}){Rail(t,V(d*.68f,.04f,0),V(d*.68f,1.54f,0),.11f,cedar);Box(t,d*.68f,.08f,0,.26f,.15f,.57f,stone);}
            Rail(t,V(-.82f,1.5f,0),V(.82f,1.5f,0),.12f,cedar);
            for(int j=0;j<5;j++){float x=(j-2)*.25f,h=1.15f-.08f*(j%3);
                Rail(t,V(x,1.49f,0),V(x,h,0),.022f,cream);
                if(n==4){for(int k=0;k<3;k++)Ball(t,x+(k-1)*.061f,h-.10f,0,.10f,.31f,.13f,j%2==0?moss:brass);}
                else{Ball(t,x,h,0,.23f,.25f,.23f,j%2==0?water:rose);Box(t,x,h-.27f,0,.085f,.30f,.025f,cream);}}
        }
    }
    static IslandSpot Spot(Transform parent,string name,string id,IslandSpot.Kind kind,float x,float z,float radius=.55f)
    {
        var p=Group(parent,name,V(x,0,z));var s=p.gameObject.AddComponent<IslandSpot>();s.id=id;s.kind=kind;s.reach=2.05f;s.blockingRadius=radius;return s;
    }
    static void Stratum(Transform t,string id,float rx,float rz,float height,Material mat)
    {
        var verts=new System.Collections.Generic.List<Vector3>{V(0,0,0)};var tris=new System.Collections.Generic.List<int>();
        var uv=new System.Collections.Generic.List<Vector2>{Vector2.zero};int steps=72;
        for(int row=0;row<2;row++)for(int i=0;i<steps;i++){float a=2*Mathf.PI*i/steps,r=1+.047f*Mathf.Sin(a*5+.6f)+.025f*Mathf.Cos(a*9);
            float x=Mathf.Sin(a)*rx*r,z=Mathf.Cos(a)*rz*r;verts.Add(V(x,-row*height,z));uv.Add(new Vector2(x*.35f,z*.35f));}
        for(int i=0;i<steps;i++){int a=1+i,b=1+(i+1)%steps;tris.AddRange(new[]{0,a,b,a,a+steps,b+steps,a,b+steps,b});}
        var mesh=new Mesh{name=id};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var path=Base+"Passage/"+id+".asset";if(!File.Exists(path))AssetDatabase.CreateAsset(mesh,path);else Object.DestroyImmediate(mesh);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void Map()
    {
        var t=Group(null,"白砂の離島",V(64,0,0));
        Box(t,0,-.73f,0,63,.13f,54,water,"浅瀬");
        Stratum(Group(t,"白砂の砂州",V(0,-.21f,0)),"PassageSand",12.7f,9.7f,.63f,white);
        Stratum(Group(t,"海草の台地",V(-.3f,-.035f,.25f)),"PassageMeadow",11.25f,8.36f,.42f,moss);
        var look=Group(t,"風を読む見晴らし台",V(1,0,5.4f));Box(look,0,.14f,0,3.4f,.26f,2.3f,stone);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Rail(look,V(x*1.32f,.2f,z*.86f),V(x*1.32f,2.37f,z*.86f),.15f,cedar);
        for(int j=-3;j<=3;j++){var l=Box(look,j*.48f,2.51f-Mathf.Abs(j)*.12f,0,.54f,.13f,2.38f,j%2==0?cream:enamel);l.transform.localEulerAngles=V(0,0,-Mathf.Sign(j)*13);}
        for(int j=0;j<7;j++)Box(t,-9.8f-j*.34f,.06f,-2.2f,.37f,.11f,1.44f,cedar,"小桟橋");
        int slot=0;for(int z=0;z<2;z++)for(int x=0;x<3;x++)Spot(t,"離島の展示広場","passage-slot-"+slot++,IslandSpot.Kind.FurnitureSlot,-3.2f+x*3.2f,-2.9f-z*2.5f,0);
        float[,] shores={{-8,-3},{1,7.6f},{9.5f,.8f}};
        for(int j=0;j<3;j++){var s=Spot(t,IslandGame.ShoreNames[3+j],"passage-shore-"+j,IslandSpot.Kind.TidePool,shores[j,0],shores[j,1],.2f);
            s.shoreIndex=3+j;Rail(s.transform,V(0,0,0),V(0,1.02f,0),.12f,cedar);Box(s.transform,0,.85f,0,.55f,.36f,.10f,enamel);Ball(s.transform,0,.88f,-.064f,.19f,.19f,.04f,cream);}
        for(int j=0;j<6;j++){bool tree=j<3;float[,] sites={{-7,2},{-4,4},{5,4},{6,-3},{5,1},{-5,-.15f}};
            var s=Spot(t,tree?"浜松":"石灰岩","passage-resource-"+j,tree?IslandSpot.Kind.Tree:IslandSpot.Kind.Stone,sites[j,0],sites[j,1],tree?.51f:.69f);
            s.regrowSeconds=tree?21:24;s.resourceYield=tree?4:3;var n=Group(s.transform,"通常",Vector3.zero);s.model=n.gameObject;
            if(tree){Rail(n,V(0,0,0),V(.12f,1.63f,0),.27f,cedar);for(int k=0;k<7;k++){float a=k*.898f;
                var leaf=Ball(n,Mathf.Sin(a)*.48f,1.66f+.11f*(k%3),Mathf.Cos(a)*.48f,.55f,.28f,1.14f,moss);leaf.transform.localEulerAngles=V(18,k*51,0);}}
            else{for(int k=0;k<3;k++)Ball(n,(k-1)*.33f,.25f+k*.07f,(k%2)*.29f,.79f,.58f,.84f,k%2==0?stone:cream);}
            var a2=Group(s.transform,"採集跡",Vector3.zero);s.alternate=a2.gameObject;Disc(a2,0,.08f,0,.42f,.15f,.39f,tree?cedar:stone,"跡");a2.gameObject.SetActive(false);}
        var bench=Spot(t,"船大工の作業台","passage-workbench",IslandSpot.Kind.Workbench,-2,1.8f);
        Box(bench.transform,0,.8f,0,1.65f,.15f,.82f,cedar);foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(bench.transform,x*.66f,.4f,z*.30f,.12f,.8f,.12f,enamel);
        Box(bench.transform,.19f,.94f,.08f,.45f,.12f,.14f,brass);for(int j=0;j<25;j++){float a=j*2.4f,r=1+j%3*.18f;float x=Mathf.Sin(a)*8.4f*r,z=Mathf.Cos(a)*5.4f*r;
            if((x< -5 || x>5)&&z>-.9f)Flower(t,x,0,z,j%2==0?cream:rose,.61f+j%3*.12f);}
    }
    static void AddCoastalWalk()
    {
        var land=GameObject.Find("白砂の離島").transform;
        if(land.Find("外洋の風景"))return;
        var t=Group(land,"外洋の風景",Vector3.zero);
        var meadow=Paint("海辺の芝地","Headland","B5C4B2");
        land.Find("海草の台地").GetComponent<MeshRenderer>().sharedMaterial=meadow;
        var pale=Paint("浜の石英","CoastalStone","A6B4AD");
        var shale=Paint("浜の粘板岩","CoastalStone","7C918E");
        for(int j=0;j<42;j++){
            float a=j*2.39996f,r=1+.043f*Mathf.Sin(a*5+.6f)+.026f*Mathf.Cos(a*9);
            float x=Mathf.Sin(a)*12.05f*r,z=Mathf.Cos(a)*9.13f*r;
            if(x< -8&&z<.3f&&z> -4.9f)continue;
            float k=.55f+(j%5)*.19f;
            var rock=Ball(t,x,-.15f,z,.96f*k,.64f*k,.77f*k,j%4==0?pale:shale,"波打ち際の岩");
            rock.transform.localEulerAngles=V(j%7,j*41,j%11);Ball(t,x+.36f*k,-.20f,z+.23f*k,.55f*k,.34f*k,.54f*k,stone);
            if(j%3==0)Ball(t,x-.42f,-.257f,z+.28f,.67f,.049f,.34f,cream,"小さな白波");
        }
        var boat=Group(t,"入江の渡し舟",V(-10.38f,-.28f,-4.2f));boat.localEulerAngles=V(0,-21,0);
        Ball(boat,0,.03f,0,1.19f,.51f,2.8f,enamel,"船体");Ball(boat,0,.24f,0,1.02f,.18f,2.39f,cedar,"甲板");
        foreach(int z in new[]{-1,1})Box(boat,0,.33f,z*.63f,.83f,.09f,.25f,cream,"腰板");
        Rail(boat,V(0,.27f,0),V(0,2.53f,0),.096f,cedar,"マスト");
        Rail(boat,V(0,.63f,-.04f),V(.08f,1.03f,-1.02f),.065f,cedar,"帆桁");
        for(int j=0;j<10;j++){float z=-(j+.5f)*.096f,h=1.61f-j*.141f,b=.73f+j*.031f;
            Box(boat,.07f,b+h*.5f,z,.055f,h,.105f,j==7||j==8?enamel:cream,"縫い合わせた帆");}
        for(int j=0;j<15;j++){float f=j/14f,x=Mathf.Lerp(-7.3f,-2.7f,f),z=-2.12f+1.56f*f+Mathf.Sin(f*4)*.36f;
            var step=Ball(t,x,.011f,z,.55f,.075f,.33f,j%3==0?stone:pale,"飛び石");step.transform.localEulerAngles=V(0,j*41,0);}
        for(int k=0;k<3;k++){var p=Group(t,"浜の松林",V(-5+k*1.1f,0,5.05f+(k%2)*.92f));p.localScale=Vector3.one*(.72f+k*.14f);
            Rail(p,V(0,0,0),V(.21f,1.76f,.05f),.21f,cedar);Ball(p,.25f,2.03f,.02f,1.37f,.71f,.95f,meadow);
            Ball(p,-.20f,1.67f,.07f,.83f,.47f,.81f,moss);}
        var rack=Group(t,"積荷",V(-8.67f,0,-2.74f));for(int j=0;j<3;j++){
            float x=(j%2)*.43f,y=j==2?.56f:.17f;Box(rack,x,y,0,.43f,.37f,.45f,cedar,"木箱");
            foreach(float dx in new[]{-.14f,.14f})Box(rack,x+dx,y,0,.044f,.382f,.461f,cream);}
        var mark=Group(t,"海上の航路標",V(9.65f,-.38f,7.04f));Disc(mark,0,.32f,0,.87f,.56f,.87f,rose,"浮標");
        Rail(mark,V(0,.57f,0),V(0,1.19f,0),.077f,brass);Ball(mark,0,1.25f,0,.27f,.24f,.27f,cream);
    }
    [MenuItem("Island/Tools/Install new passage assets")]
    public static void Install()
    {
        if(!AssetDatabase.IsValidFolder(Base+"Passage"))AssetDatabase.CreateFolder(Base.TrimEnd('/'),"Passage");
        cedar=Paint("潮木","Cedar","CEAA84");enamel=Paint("青磁","Glaze","67A6A3",.36f);cream=Paint("帆布","SailLinen","F6E5B6");
        rose=Paint("珊瑚色","Glaze","D98D7A");stone=Paint("石灰岩","CoastalStone","A9AD9F");brass=Paint("真鍮","Glaze","E5B85D",.43f);
        moss=Paint("海浜草","Meadow","9BB78D");water=Paint("瑠璃釉","Lagoon","70BBC5",.45f);white=Paint("白砂","Beach","EBDBB2");
        for(int i=0;i<ids.Length;i++){
            string pf=Base+"Prefabs/"+ids[i]+".prefab";if(!File.Exists(pf)){var p=new GameObject(names[i]);Item(p.transform,i);PrefabUtility.SaveAsPrefabAsset(p,pf);Object.DestroyImmediate(p);}
            string path="Assets/Island/Resources/Island/Furniture/"+(i+10)+"_"+ids[i]+".asset";
            if(!File.Exists(path)){var r=ScriptableObject.CreateInstance<FurnitureRecipe>();r.id=ids[i];r.displayName=names[i];r.woodCost=costs[i];r.stoneCost=stones[i];r.unlockAfterChapters=i/2;
                r.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(pf);AssetDatabase.CreateAsset(r,path);}}
        var scene=EditorSceneManager.OpenScene(IslandProject.ScenePath);
        if(GameObject.Find("白砂の離島")==null)Map();
        AddCoastalWalk();EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();Debug.Log("ISLAND_PASSAGE_INSTALLED");
    }
}