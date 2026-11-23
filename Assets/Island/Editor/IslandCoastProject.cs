using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IslandCoastProject
{
    const string RecipeDir="Assets/Island/Resources/Island/Furniture/";
    const string ArtDir="Assets/Island/Art/";
    public static void Upgrade()
    {
        Define("04_Table","table","潮風のテーブル",4,2,2);
        Define("05_Bookshelf","bookshelf","旅人の本棚",6,2,3);
        Define("06_Brazier","brazier","星見の火鉢",2,6,4);
        Define("07_Birdhouse","birdhouse","野鳥の巣箱",4,1,4);
        Define("08_Arch","arch","花のアーチ",6,5,5);
        Define("09_Daybed","daybed","浜辺の長椅子",7,3,6);
        var scene=EditorSceneManager.OpenScene(IslandProject.ScenePath);
        var spots=UnityEngine.Object.FindObjectsByType<IslandSpot>(FindObjectsSortMode.None);
        if (!spots.Any(s=>s.kind==IslandSpot.Kind.TidePool))
        {
            var group=new GameObject("浜辺の観察地点");
            Vector2[] centers={new Vector2(-7,-5.7f),new Vector2(1,7.7f),new Vector2(10.3f,-1.5f)};
            var teal=Paint("Sea Enamel",new Color32(35,130,137,255));
            var ivory=Paint("Sailcloth",new Color32(250,225,162,255));
            var timber=Paint("Driftwood",new Color32(135,86,54,255));
            for(int i=0;i<3;i++)
            {
                var station=new GameObject(IslandGame.ShoreNames[i]);
                station.transform.SetParent(group.transform);
                station.transform.position=new Vector3(centers[i].x,0,centers[i].y);
                var s=station.AddComponent<IslandSpot>();
                s.id="shore-"+i;s.kind=IslandSpot.Kind.TidePool;s.shoreIndex=i;s.reach=2;s.blockingRadius=.22f;
                Piece(station.transform,PrimitiveType.Cylinder,new Vector3(0,.52f,0),new Vector3(.115f,.55f,.115f),timber);
                Piece(station.transform,PrimitiveType.Cube,new Vector3(.195f,.91f,0),new Vector3(.48f,.31f,.045f),teal);
                Piece(station.transform,PrimitiveType.Cube,new Vector3(.197f,.91f,-.026f),new Vector3(.23f,.047f,.013f),ivory);
                Piece(station.transform,PrimitiveType.Sphere,new Vector3(-.2f,.13f,-.12f),new Vector3(.51f,.21f,.44f),ivory);
            }
            EditorSceneManager.SaveScene(scene);
        }
        if(UnityEngine.Object.FindObjectsByType<IslandLandmark>(FindObjectsSortMode.None).Length==0)
        {
            BuildLandmarks();
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
    }
    static void Define(string file,string id,string label,int wood,int stone,int chapter)
    {
        string path=RecipeDir+file+".asset";
        if(File.Exists(path))return;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ArtDir+"Prefabs/"+id+".prefab");
        if(!prefab)throw new InvalidOperationException("Missing furniture prefab: "+id);
        var r=ScriptableObject.CreateInstance<FurnitureRecipe>();r.id=id;r.displayName=label;
        r.woodCost=wood;r.stoneCost=stone;r.unlockAfterChapters=chapter;r.prefab=prefab;
        AssetDatabase.CreateAsset(r,path);
    }
    static Material Paint(string name, Color color)
    {
        string path=ArtDir+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material)return material;
        material=new Material(Shader.Find("Standard")){color=color};
        material.SetFloat("_Glossiness",.16f);AssetDatabase.CreateAsset(material,path);return material;
    }
    static void Piece(Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
    {
        var p=GameObject.CreatePrimitive(type);p.transform.SetParent(parent,false);
        p.transform.localPosition=pos;p.transform.localScale=scale;p.GetComponent<Renderer>().sharedMaterial=mat;
        UnityEngine.Object.DestroyImmediate(p.GetComponent<Collider>());
    }
    static void BuildLandmarks()
    {
        var red=Paint("Signal Red",new Color32(181,69,55,255));
        var white=Paint("Sailcloth",new Color32(250,225,162,255));
        var blue=Paint("Sea Enamel",new Color32(35,130,137,255));
        var wood=Paint("Driftwood",new Color32(135,86,54,255));
        var light=Paint("Warm Lamp",new Color32(255,197,86,255));
        light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(.75f,.39f,.1f));EditorUtility.SetDirty(light);
        Transform Landmark(string name,int level,float x,float z)
        {
            var obj=new GameObject(name);obj.transform.position=new Vector3(x,0,z);
            var landmark=obj.AddComponent<IslandLandmark>();landmark.chapter=level;
            var form=new GameObject("外観").transform;form.SetParent(obj.transform,false);landmark.structure=form;
            return form;
        }
        var jetty=Landmark("第二章・船着場の荷物",2,-8.45f,-5.63f);
        for(int i=0;i<3;i++)
        {
            float x=i==1?.49f:0, y=i==2?.49f:0;
            Piece(jetty,PrimitiveType.Cube,new Vector3(x,y+.225f,0),new Vector3(.47f,.44f,.45f),wood);
            foreach(float dx in new[]{-.14f,.14f})Piece(jetty,PrimitiveType.Cube,new Vector3(x+dx,y+.23f,0),new Vector3(.036f,.455f,.465f),white);
        }
        var pennants=Landmark("第五章・港の大漁旗",5,-8.2f,-6.45f);
        pennants.rotation=Quaternion.Euler(0,28,0);
        foreach(float x in new[]{-.9f,.9f})Piece(pennants,PrimitiveType.Cylinder,new Vector3(x,1.03f,0),new Vector3(.08f,1.03f,.08f),wood);
        Piece(pennants,PrimitiveType.Cube,new Vector3(0,1.9f,0),new Vector3(1.85f,.035f,.03f),white);
        for(int i=0;i<5;i++)Piece(pennants,PrimitiveType.Cube,new Vector3(-.65f+i*.32f,1.75f,0),new Vector3(.24f,.29f,.035f),i%2==0?red:blue);
        var beacon=Landmark("第八章・東岬の灯台",8,9.9f,4.5f);
        void Ring(float y,float r,float h,Material m)=>Piece(beacon,PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(r,h/2,r),m);
        Ring(.16f,1.35f,.32f,wood);Ring(.67f,1.03f,.84f,white);Ring(1.26f,.96f,.4f,red);
        Ring(1.82f,.90f,.72f,white);Ring(2.22f,1.10f,.13f,blue);Ring(2.51f,.68f,.49f,light);
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Piece(beacon,PrimitiveType.Cylinder,new Vector3(.38f*Mathf.Cos(a),2.53f,.38f*Mathf.Sin(a)),new Vector3(.053f,.28f,.053f),blue);}
        Ring(2.83f,1.04f,.12f,red);Ring(2.93f,.73f,.087f,red);Ring(3.02f,.43f,.1f,red);
        var rotor=new GameObject("回転灯").transform;rotor.SetParent(beacon,false);rotor.localPosition=Vector3.up*2.52f;
        Piece(rotor,PrimitiveType.Cube,new Vector3(0,0,.30f),new Vector3(.22f,.33f,.13f),white);
        beacon.GetComponentInParent<IslandLandmark>().lantern=rotor;
    }
    public static void Release()
    {
        Upgrade();
        var state=new IslandState();
        for(int i=0;i<IslandGame.SpecimenNames.Length;i++) if(!state.RecordSpecimen(i)||state.RecordSpecimen(i))throw new Exception("Specimen registration "+i);
        if(state.SpecimenCount!=IslandGame.SpecimenNames.Length)throw new Exception("Gallery");
        IslandCampaignCheck.Run();
        IslandExhibitionCheck.Run();
        CreateReviewSave();
        IslandProject.BuildWindows();
    }
    static void CreateReviewSave()
    {
        string path=Environment.GetEnvironmentVariable("NAGI_REVIEW_OUTPUT");
        if(string.IsNullOrEmpty(path))return;
        var s=new IslandState();
        var recipes=Resources.LoadAll<FurnitureRecipe>("Island/Furniture").ToDictionary(r=>r.id);
        void Craft(string id){if(!s.TryCraft(recipes[id]))throw new Exception("Review craft "+id);}
        void Mail(){if(!s.TryDeliverOrder())throw new Exception("Review order "+s.CurrentOrder.title);}
        void Exhibit(string id,string slot,int rotation){Craft(id);if(!s.TryPlace(slot,id,rotation))throw new Exception(slot);}
        s.TryGather("preview-wood",true,98,1,1);s.TryGather("preview-stone",false,75,1,1);
        for(int i=0;i<4;i++){s.TryPlant(1,10+i*2);s.TryHarvest(11+i*2);}
        Mail();Craft("bench");Mail();s.TryUpgrade(IslandTool.Gardening);s.TryUpgrade(IslandTool.Gathering);
        Craft("table");Mail();
        Exhibit("bench","plot-1-1",0);Exhibit("table","plot-1-2",0);
        Craft("bookshelf");Mail();Craft("birdhouse");Mail();Exhibit("lantern","plot-2-1",0);
        Craft("brazier");Mail();Exhibit("arch","plot-1-3",0);Craft("daybed");Mail();Mail();
        Exhibit("birdhouse","plot-1-4",0);Exhibit("planter","plot-2-2",0);
        Exhibit("daybed","plot-2-3",0);Exhibit("brazier","plot-2-4",0);
        s.TryPlant(1,101);
        for(int i=0;i<IslandGame.SpecimenNames.Length;i++)s.RecordSpecimen(i);
        if(!IslandSave.Write(Path.Combine(path,"review-save.json"),s))throw new Exception("Preview save");
    }
}
