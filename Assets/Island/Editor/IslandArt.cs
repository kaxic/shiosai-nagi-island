using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static IslandMesh;

/// <summary>Editor-only creation of persistent, individually replaceable island meshes.</summary>
public static class IslandArt
{
    const string ArtFolder="Assets/Island/Art";
    const string MeshFolder=ArtFolder+"/Meshes";
    const string PaintPath=ArtFolder+"/Island Paint.mat";

    public static GameObject CreateModel(string kind,Transform parent)
    {
        return Model(kind,kind,parent);
    }

    public static void CreateLandscape(Transform parent)
    {
        Model("Island and lagoon","terrain",parent);
        Model("Paths and meadow","meadow",parent);
        Model("Landing pier","pier",parent);
        Model("Tide pools and foam","shoreline",parent);
        var lightObject = new GameObject("Late morning sun");
        lightObject.transform.SetParent(parent,false);
        lightObject.transform.rotation=Quaternion.Euler(53,-34,0);
        var sunlight=lightObject.AddComponent<Light>();
        sunlight.type=LightType.Directional;
        sunlight.color=new Color(1,.945f,.855f);
        sunlight.intensity=1.15f;
        sunlight.shadows=LightShadows.Soft;
        sunlight.shadowStrength=.72f;
        sunlight.shadowBias=.045f;
        sunlight.shadowNormalBias=.22f;
        RenderSettings.sun=sunlight;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=Paint("E0F0EA");
        RenderSettings.ambientEquatorColor=Paint("B7DAD3");
        RenderSettings.ambientGroundColor=Paint("C4AC8B");
        RenderSettings.fog=false;
    }

    static GameObject Model(string name,string assetName,Transform parent)
    {
        if(!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder(ArtFolder,"Meshes");
        string path=MeshFolder+"/"+assetName+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            mesh=Bake(assetName);
            AssetDatabase.CreateAsset(mesh,path);
        }
        var paint=AssetDatabase.LoadAssetAtPath<Material>(PaintPath);
        if(!paint)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(ArtFolder+"/IslandPaint.shader");
            if(!shader) throw new InvalidOperationException("IslandPaint.shader is missing");
            paint=new Material(shader){name="Island Paint"};
            AssignPaintTextures(paint);
            AssetDatabase.CreateAsset(paint,PaintPath);
        }
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=paint;
        AddContactWash(go,assetName);
        return go;
    }

    internal static void AddContactWash(GameObject model,string kind)
    {
        Vector2 size;
        switch(kind)
        {
            case "cabin": size=new Vector2(5.10f,4.55f); break;
            case "tree": size=new Vector2(2.40f,2.04f); break;
            case "rock": size=new Vector2(2.30f,1.73f); break;
            case "workbench": size=new Vector2(2.30f,1.24f); break;
            case "farm": size=new Vector2(3.28f,2.20f); break;
            case "bench": case "daybed": size=new Vector2(2.02f,1.16f); break;
            case "table": size=new Vector2(1.68f,1.50f); break;
            case "bookshelf": size=new Vector2(2.07f,1.05f); break;
            case "arch": size=new Vector2(2.09f,.98f); break;
            case "planter": size=new Vector2(1.43f,.98f); break;
            case "brazier": size=new Vector2(1.31f,1.20f); break;
            case "player": size=new Vector2(.76f,.67f); break;
            case "lantern": case "birdhouse": size=new Vector2(.98f,.85f); break;
            case "pebbles": size=new Vector2(1.32f,.71f); break;
            case "stump": size=new Vector2(1.13f,.96f); break;
            default: return;
        }
        const string child="Ground shadow";
        if(model.transform.Find(child)) return;
        const string materialPath=ArtFolder+"/Contact Wash.mat";
        var wash=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!wash)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(ArtFolder+"/ContactWash.shader");
            if(!shader) throw new InvalidOperationException("Missing contact wash shader");
            wash=new Material(shader){name="Contact Wash"};
            AssetDatabase.CreateAsset(wash,materialPath);
        }
        var sheet=GameObject.CreatePrimitive(PrimitiveType.Quad);
        sheet.name=child;
        UnityEngine.Object.DestroyImmediate(sheet.GetComponent<Collider>());
        sheet.transform.SetParent(model.transform,false);
        sheet.transform.localPosition=new Vector3(-size.x*.035f,.027f,size.y*.025f);
        sheet.transform.localRotation=Quaternion.Euler(90,0,0);
        sheet.transform.localScale=new Vector3(size.x,size.y,1);
        var renderer=sheet.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=wash;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;
        renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    }

    internal static void AssignPaintTextures(Material paint)
    {
        foreach(string name in new[]{"Meadow","Beach","Lagoon","Cedar","Canopy","CoastalStone"})
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder+"/Textures/"+name+".png");
            if(!texture) throw new InvalidOperationException("Missing paint texture: "+name);
            paint.SetTexture("_"+name,texture);
        }
    }

    // Also used when an illustrator re-bakes a shipped mesh while retaining its asset GUID.
    internal static Mesh Bake(string kind)
    {
        var sculpt=new IslandMesh();
        switch(kind)
        {
            case "terrain": Coast(sculpt); break;
            case "meadow": Meadow(sculpt); break;
            case "pier": Pier(sculpt); break;
            case "shoreline": Shore(sculpt); break;
            default: new IslandSculpt(sculpt).Build(kind); break;
        }
        return sculpt.Finish(kind);
    }

    static Vector3 Point(float angle,float r,float y)
    {
        float edge=1+.046f*Mathf.Sin(3*angle+.58f)+.034f*Mathf.Cos(5*angle+.7f)+.019f*Mathf.Sin(9*angle);
        return new Vector3(Mathf.Cos(angle)*13*r*edge,y,Mathf.Sin(angle)*10*r*edge);
    }

    static void Coast(IslandMesh mesh)
    {
        // The wide level plateau supports placement. Narrow changes in height are confined to the shore.
        const int slices=76;
        float[] radii={.19f,.39f,.58f,.75f,.855f,.94f,.97f,1.008f,1.044f,1.072f,1.103f,1.17f,1.30f,1.47f,1.68f,2.1f,3f,7f,16f};
        float[] levels={0,0,0,0,0,0,-.072f,-.244f,-.311f,-.367f,-.399f,-.399f,-.399f,-.399f,-.399f,-.399f,-.399f,-.399f,-.399f};
        string[] paints={"ABCA8C","ABCA8C","ABCA8C","ABCA8C","AFCA84","ABC783","9BBA71","EDCB95","F5D9A2","E6C693","C6E3C2","93DBCB","6ACFC6","51C3BD","42B6B6","37A9AF","339FA9","339FA9","339FA9"};
        var ring=new Vector3[radii.Length,slices];
        var random=new System.Random(2801);
        for(int k=0;k<radii.Length;k++)
        for(int j=0;j<slices;j++)
        {
            float angle=(j+(k%2)*.29f)*2*Mathf.PI/slices;
            float width=k>=9 && k<=15?.016f:.006f;
            float r=radii[k]+((float)random.NextDouble()-.5f)*width;
            if(k>=6 && k<=10) r+=.014f*Mathf.Sin(angle*4.0f+.72f)*(1-Mathf.Abs(k-8)/4f);
            ring[k,j]=Point(angle,r,levels[k]);
        }
        Color surface(Vector3 p,int k)
        {
            float noise=Mathf.PerlinNoise(22+p.x*.24f,37+p.z*.23f);
            if(k<6) return Color.Lerp(Paint("A0C084"),Paint("BDD28F"),noise);
            return Tone(Paint(paints[k]),.989f+.022f*noise);
        }
        for(int k=0;k<radii.Length;k++)
        for(int j=0;j<slices;j++)
        {
            int next=(j+1)%slices;
            mesh.surface=k<=6?Surface.Grass:k<=10?Surface.Sand:Surface.Sea;
            var c=ring[k,j]; var d=ring[k,next];
            Color cc=surface(c,k), cd=surface(d,k);
            if(k==0)
            {
                mesh.Face(Vector3.zero,c,d,surface(Vector3.zero,0),cc,cd,Vector3.up);
                continue;
            }
            var a=ring[k-1,j]; var b=ring[k-1,next];
            // Turf forms a small lip over the sand. Lagoon colours blend out into deeper water.
            int innerBand=k==7?7:k-1;
            var ca=surface(a,innerBand); var cb=surface(b,innerBand);
            mesh.Face(a,c,d,ca,cc,cd,Vector3.up);
            mesh.Face(a,d,b,ca,cd,cb,Vector3.up);
        }
    }

    static void Trail(IslandMesh mesh,Vector2[] knots,float width)
    {
        mesh.surface=Surface.Path;
        Vector3 left=Vector3.zero,right=Vector3.zero;
        for(int i=0;i<knots.Length;i++)
        {
            var previous=knots[Mathf.Max(0,i-1)];
            var next=knots[Mathf.Min(knots.Length-1,i+1)];
            var direction=(next-previous).normalized;
            float half=width*(.46f+.065f*Mathf.Sin(1.62f*i));
            var center=new Vector3(knots[i].x,.009f,knots[i].y);
            var cross=new Vector3(-direction.y,0,direction.x)*half;
            if(i>0)
            {
                var prevCenter=(left+right)*.5f;
                var mid=(center+prevCenter)*.5f+cross*.055f;
                var ground=Paint("D2C999");
                mesh.Face(left,right,mid,ground,Vector3.up);
                mesh.Face(right,center-cross,mid,Tone(ground,.997f),Vector3.up);
                mesh.Face(center-cross,center+cross,mid,ground,Vector3.up);
                mesh.Face(center+cross,left,mid,Tone(ground,1.004f),Vector3.up);
                Color faded=ground; faded.a=0;
                void fringe(Vector3 a,Vector3 b,Vector3 outerA,Vector3 outerB)
                {
                    mesh.Face(a,b,outerB,ground,ground,faded,Vector3.up);
                    mesh.Face(a,outerB,outerA,ground,faded,faded,Vector3.up);
                }
                Vector3 previousSide=(left-right).normalized*.21f;
                Vector3 side=cross.normalized*(.17f+.045f*Mathf.Sin(i));
                fringe(left,center+cross,left+previousSide,center+cross+side);
                fringe(right,center-cross,right-previousSide,center-cross-side);
            }
            left=center+cross;right=center-cross;
        }
    }

    static void Meadow(IslandMesh mesh)
    {
        Trail(mesh,new[]{new Vector2(-7.65f,-6.9f),new Vector2(-7,-6),new Vector2(-6.2f,-4.8f),
            new Vector2(-5.35f,-3.7f),new Vector2(-4.95f,-2.60f),new Vector2(-5.16f,-1.25f),
            new Vector2(-5.20f,-.1f),new Vector2(-4.95f,1.25f),new Vector2(-4.2f,2.37f)},1.06f);
        Trail(mesh,new[]{new Vector2(-5.3f,-.72f),new Vector2(-4.25f,-.63f),new Vector2(-3,-.38f),
            new Vector2(-1.55f,-.28f),new Vector2(-.33f,.13f),new Vector2(.9f,.84f),new Vector2(1.87f,1.47f),new Vector2(3.17f,2.08f)},.79f);

        var random=new System.Random(401);
        mesh.surface=Surface.Auto;
        float n()=>(float)random.NextDouble();
        for(int j=0;j<238;j++)
        {
            float angle=n()*Mathf.PI*2;
            float radius=.57f+n()*.358f;
            Vector3 cluster=Point(angle,radius,.018f);
            if(cluster.x < -4 && cluster.x > -8 && cluster.z < -.4f && cluster.z > -6.6f) continue;
            for(int k=0;k<3;k++)
            {
                float x=cluster.x+(n()-.5f)*.44f,z=cluster.z+(n()-.5f)*.4f;
                float size=.055f+.105f*n();
                var root=new Vector3(x,.018f,z);
                Color green=Paint(k%2==0?"91B67B":"A2BD78");
                Vector3 tip=root+new Vector3((n()-.5f)*.16f,size,(n()-.5f)*.15f);
                var side=new Vector3(size*.30f,0,-size*.18f);
                mesh.Face(root-side,tip,root+side,green,Vector3.forward*-1);
                mesh.Face(root-side,root+side,tip,green,Vector3.forward);
            }
            if(j%5==0)
            {
                for(int k=0;k<3;k++)
                {
                    var root=cluster+new Vector3((n()-.5f)*.42f,.065f+n()*.08f,(n()-.5f)*.42f);
                    float size=.047f+n()*.044f;
                    for(int petal=0;petal<5;petal++)
                    {
                        float a=petal*Mathf.PI*.4f;
                        Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)), side=Vector3.Cross(Vector3.up,dir);
                        mesh.Quad(root,root+(dir*.65f+side*.5f)*size,root+dir*size,root+(dir*.65f-side*.5f)*size,
                            Paint(j%3==0?"FFCA79":"FAE7B7"),Vector3.up);
                    }
                    mesh.Pole(root,root+Vector3.up*.015f,size*.28f,size*.25f,Paint("DE9F55"),5);
                }
            }
        }
    }

    static void Shore(IslandMesh mesh)
    {
        var random=new System.Random(108);
        float n()=>(float)random.NextDouble();
        mesh.surface=Surface.Foam;
        // Irregular, broken surf at the waterline follows the land silhouette.
        for(int i=0;i<153;i++)
        {
            if(n()<.24f) continue;
            float angle=i*Mathf.PI*2/153;
            float r=1.092f+.010f*Mathf.Sin(angle*5)+.009f*n();
            float length=.016f+n()*.024f, width=.004f+n()*.004f;
            var a=Point(angle,r,-.387f);
            var b=Point(angle+length*.48f,r+width,-.387f);
            var c=Point(angle+length,r,-.387f);
            var d=Point(angle+length*.45f,r-width*.3f,-.387f);
            mesh.Quad(a,b,c,d,Paint("E4EDCE"),Vector3.up);
        }
        string[] rocks={"8D9A95","98A5A0","A2A99B","AFAD98"};
        mesh.surface=Surface.Stone;
        for(int cluster=0;cluster<22;cluster++)
        {
            float angle=cluster*Mathf.PI*2/22+.12f*n();
            float radius=1.010f+.106f*n();
            var p=Point(angle,radius,-.33f);
            if(p.x< -6 && p.x>-11 && p.z<-5) continue;
            for(int j=0;j<2+(cluster%4);j++)
            {
                float scale=(j==0?.47f:.22f)*(.52f+n());
                var center=p+new Vector3((n()-.5f)*1.1f,scale*.24f,(n()-.5f)*.94f);
                mesh.Blob(center,new Vector3(scale*1.22f,scale*.76f,scale*.93f),Paint(rocks[(cluster+j)%rocks.Length]),cluster*37+j,6);
            }
        }
        // Short gouache strokes mark ripples, leaving generous unbroken areas of sea.
        mesh.surface=Surface.Foam;
        for(int group=0;group<172;group++)
        {
            float angle=n()*2*Mathf.PI, radius=1.14f+2.1f*n();
            var center=Point(angle,radius,-.387f);
            for(int k=0;k<2+(group%3);k++)
            {
                var p=center+new Vector3((n()-.5f)*1.2f,0,(n()-.5f)*.86f);
                float length=.23f+n()*.57f, width=.023f+n()*.026f;
                Vector3 front=new Vector3(length,0,.05f),side=new Vector3(.005f,0,width);
                mesh.Quad(p-front*.6f,p+side,p+front*.6f,p-side,Paint(radius<1.3f?"A8E0CA":"78CFC2"),Vector3.up);
            }
        }
    }

    static void Pier(IslandMesh mesh)
    {
        var rotation=Quaternion.Euler(0,34,0);
        var origin=new Vector3(-7.55f,0,-6.89f);
        Vector3 local(float x,float y,float z) => origin+rotation*new Vector3(x,y,z);
        void timber(float x,float y,float z,float w,float h,float d,string paint)
            =>mesh.Box(local(x,y,z),new Vector3(w,h,d),Paint(paint),rotation);
        string[] planks={"C29667","CC9B69","D4A775","BF9067"};
        foreach(float x in new[]{-.57f,.57f}) timber(x,.008f,-1.54f,.15f,.18f,4.15f,"896B50");
        for(int j=0;j<19;j++)
            timber(0,.131f,-3.56f+j*.213f,1.54f+.027f*Mathf.Sin(j*3),.110f,.20f,planks[j%4]);
        foreach(float x in new[]{-.75f,.75f})
        foreach(float z in new[]{-3.46f,-1.91f,-.35f})
        {
            mesh.Pole(local(x,-.68f,z),local(x,.48f,z),.115f,.097f,Paint("A57A55"),8);
            mesh.Pole(local(x,.33f,z),local(x,.39f,z),.107f,.105f,Paint("E2BD84"),8);
            mesh.Pole(local(x,.48f,z),local(x,.497f,z),.087f,.086f,Paint("D3A975"),8);
        }
        // Rope handrail on the outside edge.
        for(int span=0;span<2;span++)
        {
            float start=-3.46f+span*1.555f;
            for(int i=0;i<5;i++)
            {
                Vector3 point(int j){float t=j/5f;return local(-.75f,.36f-.16f*Mathf.Sin(Mathf.PI*t),start+t*1.555f);}
                mesh.Pole(point(i),point(i+1),.027f,.027f,Paint("D8BB83"),6);
            }
        }
        timber(-.33f,.39f,-3.15f,.47f,.39f,.42f,"C18D58");
        foreach(float z in new[]{-3.36f,-2.94f}) timber(-.33f,.39f,z,.48f,.068f,.033f,"E1AE73");
        var buoy=local(1.43f,-.26f,-2.97f);
        mesh.Blob(buoy,new Vector3(.22f,.25f,.22f),Paint("DD8166"),61);
        mesh.Pole(buoy+Vector3.down*.03f,buoy+Vector3.up*.04f,.21f,.215f,Paint("EDE2B8"),9);
        mesh.Pole(buoy+Vector3.up*.2f,buoy+Vector3.up*.34f,.037f,.029f,Paint("916C54"),7);
    }
}
