using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class IslandGame : MonoBehaviour
{
    [Header("島の設定")]
    public Transform player;
    public Transform playerVisual;
    public Camera islandCamera;
    public Vector2 walkableRadii = new Vector2(11.65f, 8.65f);
    [Min(1f)] public float walkSpeed = 4.3f;
    public Material markerMaterial;

    public IslandState State { get; private set; }
    public FurnitureRecipe[] Recipes { get; private set; }
    public string Prompt { get; private set; } = "";
    public string Notice => Time.unscaledTime < noticeUntil ? notice : "";
    public string Panel { get; private set; } = "";
    public bool IsPlacing => held != null;
    public int PlacedKinds => State.PlacedTypeCount;
    public bool HasWorkbenchInReach => workbenchInReach;
    public string PlacementName => held ? held.displayName : "";
    public bool PlacementValid { get; private set; }

    readonly Dictionary<string, FurnitureRecipe> catalog = new Dictionary<string, FurnitureRecipe>();
    readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
    readonly Dictionary<IslandSpot, LineRenderer> guides = new Dictionary<IslandSpot, LineRenderer>();
    IslandSpot[] spots;
    IslandSpot focus, destination;
    LineRenderer focusRing;
    GameObject ghost;
    FurnitureRecipe held;
    IslandSounds sounds;
    IslandFeedback feedback;
    int turns;
    bool workbenchInReach, dirtyPosition, goalAnnounced;
    string savePath, notice;
    float noticeUntil, nextPositionSave, walkPhase;
    long lastSecond = -1;
    Vector2 cameraHome;
    const float PlayerRadius = 0.27f;
    const float SlotRadius = 0.83f;
#if UNITY_EDITOR
    [NonSerialized] public static string EditorTestSavePath;
#endif
    static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    static readonly Color Gold = new Color(1f, 0.83f, 0.38f);
    static readonly Color Pale = new Color(0.88f, 0.99f, 0.84f, 0.89f);
    static readonly Color Good = new Color(0.34f, 1f, 0.67f);
    static readonly Color Bad = new Color(1f, 0.39f, 0.29f);

    void Awake()
    {
        Application.targetFrameRate = 60;
        savePath = Path.Combine(Application.persistentDataPath, "island-v1.json");
        string review = IslandPhoto.ReviewDirectory();
        if (!string.IsNullOrEmpty(review)) savePath = Path.Combine(review, "review-save.json");
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(EditorTestSavePath)) savePath = EditorTestSavePath;
#endif
        State = IslandSave.Load(savePath);
        Recipes = Resources.LoadAll<FurnitureRecipe>("Island/Furniture");
        Array.Sort(Recipes, (a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (var item in Recipes)
            if (item && !string.IsNullOrWhiteSpace(item.id) && item.prefab) catalog[item.id] = item;
        Recipes = Array.FindAll(Recipes, item => item && !string.IsNullOrWhiteSpace(item.id)
            && catalog.TryGetValue(item.id, out var found) && found == item);
        spots = FindObjectsByType<IslandSpot>(FindObjectsSortMode.None);
        sounds = gameObject.AddComponent<IslandSounds>();
        feedback = gameObject.AddComponent<IslandFeedback>();
        foreach (var spot in spots)
        {
            if (spot.kind != IslandSpot.Kind.FurnitureSlot) continue;
            guides.Add(spot, MakeOutline(spot.transform, true));
            RefreshFurniture(spot);
        }
        focusRing = MakeOutline(transform, false);
        Vector2 spawn = new Vector2(State.playerX, State.playerZ);
        if (!CanWalk(spawn)) spawn = FindOpenPoint();
        player.position = OnGround(spawn);
        player.rotation = Quaternion.Euler(0, State.playerYaw, 0);
        cameraHome = new Vector2(islandCamera.transform.position.x, islandCamera.transform.position.z);
        goalAnnounced = State.GoalReached;
        RefreshSpots(Now);
        RefreshFocus();
        InitializeShore();
        if (State.IsSaveReadOnly) Say("別バージョンの記録を保護中です。今回の変更は保存されません。", 8);
    }

    static Vector3 OnGround(Vector2 point) => new Vector3(point.x, 0, point.y);
    Vector2 PlayerPoint => new Vector2(player.position.x, player.position.z);

    Vector2 FindOpenPoint()
    {
        Vector2 start = new Vector2(0, -0.4f);
        if (CanWalk(start)) return start;
        for (int radius = 1; radius < 16; radius++)
            for (int step = 0; step < 16; step++)
            {
                float angle = step * Mathf.PI / 8;
                var p = start + radius * 0.6f * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (CanWalk(p)) return p;
            }
        return start;
    }

    bool CanWalk(Vector2 p)
    {
        if (float.IsNaN(p.x) || float.IsNaN(p.y)) return false;
        float x = p.x / Mathf.Max(1, walkableRadii.x);
        float z = p.y / Mathf.Max(1, walkableRadii.y);
        if (x*x + z*z > 1) return false;
        foreach (var site in spots)
        {
            float radius = site.kind == IslandSpot.Kind.FurnitureSlot
                ? (State.At(site.id) != null ? SlotRadius : 0) : site.blockingRadius;
            if (radius > 0 && Vector2.Distance(p, site.Center) < radius + PlayerRadius) return false;
        }
        return true;
    }

    void Update()
    {
        long now = Now;
        if (now != lastSecond) { RefreshSpots(now); lastSecond = now; }
        if (Panel == "title")
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ClosePanel();
            return;
        }
        if ((Panel == "" || Panel == "orders") && Input.GetKeyDown(KeyCode.J))
        { if (Panel == "orders") ClosePanel(); else Open("orders"); return; }
        if ((Panel == "" || Panel == "tools") && Input.GetKeyDown(KeyCode.U))
        { if (Panel == "tools") ClosePanel(); else Open("tools"); return; }
        if (Panel == "tide" && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))) TakeSample();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Panel.Length > 0) ClosePanel();
            else if (IsPlacing) CancelPlacement();
            else Open("pause");
            return;
        }
        if (Input.GetKeyDown(KeyCode.F1)) { if (Panel == "help") ClosePanel(); else Open("help"); return; }
        if (Input.GetKeyDown(KeyCode.Tab) && (Panel == "" || Panel == "bag"))
        {
            if (Panel == "bag") ClosePanel(); else Open("bag");
            return;
        }
        if (Panel.Length > 0) return;
        cameraZoom = Mathf.Clamp(cameraZoom - Input.mouseScrollDelta.y * 0.065f, 0.61f, 1.18f);
        MovePlayer();
        RefreshFocus();
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (IsPlacing) TurnPlacement();
            else RetrieveFocusedFurniture();
        }
        if (Input.GetKeyDown(KeyCode.E)) Interact();
        if (dirtyPosition && Time.unscaledTime >= nextPositionSave) Commit();
    }

    void LateUpdate()
    {
        float height = Mathf.Max(9.1f, 15.0f / Mathf.Max(0.5f, islandCamera.aspect));
        islandCamera.orthographicSize = Mathf.Lerp(islandCamera.orthographicSize, height * cameraZoom, 1 - Mathf.Exp(-7f * Time.deltaTime));
        Vector3 p = islandCamera.transform.position;
        float follow = Mathf.Clamp01(0.08f + (1 - cameraZoom) * 2.1f);
        p.x = cameraHome.x + player.position.x * follow;
        p.z = cameraHome.y + player.position.z * follow;
        islandCamera.transform.position = p;
    }

    void MovePlayer()
    {
        float dx = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                 - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float dz = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                 - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        Vector2 right = new Vector2(islandCamera.transform.right.x, islandCamera.transform.right.z).normalized;
        Vector2 forward = new Vector2(-right.y, right.x);
        Vector2 dir = (right * dx + forward * dz).normalized;
        if (dir.sqrMagnitude < 0.01f)
        {
            playerVisual.localPosition = Vector3.MoveTowards(playerVisual.localPosition, Vector3.zero, Time.deltaTime);
            return;
        }
        Vector2 old = PlayerPoint;
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector2 delta = dir * walkSpeed * (sprint ? 1.58f : 1f) * Mathf.Min(Time.deltaTime, 0.06f);
        Vector2 next = old + delta;
        if (!CanWalk(next))
        {
            next = old;
            if (CanWalk(old + new Vector2(delta.x, 0))) next.x += delta.x;
            if (CanWalk(next + new Vector2(0, delta.y))) next.y += delta.y;
        }
        player.position = OnGround(next);
        var target = Quaternion.LookRotation(OnGround(dir));
        player.rotation = Quaternion.RotateTowards(player.rotation, target, 850 * Time.deltaTime);
        walkPhase += (next-old).magnitude * 6.6f;
        playerVisual.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(walkPhase)) * 0.07f;
        dirtyPosition = true;
    }

    void RefreshFocus()
    {
        focus = null;
        workbenchInReach = false;
        float best = float.MaxValue;
        foreach (var site in spots)
        {
            float distance = Vector2.Distance(PlayerPoint, site.Center);
            if (distance > site.reach) continue;
            if (site.kind == IslandSpot.Kind.Workbench) workbenchInReach = true;
            if (site.kind == IslandSpot.Kind.FurnitureSlot && State.At(site.id) == null) continue;
            float edge = distance - site.blockingRadius;
            if (edge >= best) continue;
            best = edge;
            focus = site;
        }
        focusRing.enabled = focus && !IsPlacing && Panel == "";
        if (focusRing.enabled)
        {
            focusRing.transform.position = OnGround(focus.Center) + Vector3.up * 0.07f;
            float radius = Mathf.Max(0.85f, focus.blockingRadius + 0.18f);
            focusRing.transform.localScale = new Vector3(radius, 1, radius);
        }
        if (IsPlacing) RefreshPlacement();
        else Prompt = Describe(focus);
    }

    string Describe(IslandSpot site)
    {
        if (!site) return "木や岩に近づいて素材を集めよう";
        switch (site.kind)
        {
            case IslandSpot.Kind.Tree:
            case IslandSpot.Kind.Stone:
                int wait = State.ResourceWait(site.id, Now);
                if (wait > 0) return $"再び採集できるまで {wait} 秒";
                return site.kind == IslandSpot.Kind.Tree ? $"[E]  木材を集める  +{State.GatherYield(site.resourceYield)}" : $"[E]  石を集める  +{State.GatherYield(site.resourceYield)}";
            case IslandSpot.Kind.Workbench: return "[E]  作業台で家具を作る";
            case IslandSpot.Kind.TidePool: return "[E]  " + ShoreNames[site.shoreIndex] + "で浜を調べる";
            case IslandSpot.Kind.Cabin: return "[E]  島の暮らし方を読む";
            case IslandSpot.Kind.Farm:
                int growth = State.FarmWait(Now);
                return growth < 0 ? "[E]  畑にかぶの種をまく" : growth == 0 ? "[E]  かぶを収穫する" : $"かぶが育つまで {growth} 秒";
            case IslandSpot.Kind.FurnitureSlot:
                var item = State.At(site.id);
                return item != null && catalog.TryGetValue(item.recipeId, out var recipe)
                    ? $"[R]  {recipe.displayName}を家具袋に戻す" : "[R]  家具袋に戻す";
            default: return "";
        }
    }

    public void Interact()
    {
        if (Panel.Length > 0) return;
        RefreshFocus();
        if (IsPlacing) { Place(); return; }
        if (!focus) return;
        long now = Now;
        switch (focus.kind)
        {
            case IslandSpot.Kind.Tree:
            case IslandSpot.Kind.Stone:
                bool wood = focus.kind == IslandSpot.Kind.Tree;
                if (State.TryGather(focus.id, wood, focus.resourceYield, focus.regrowSeconds, now))
                    ActionDone($"{(wood ? "木材" : "石")}  +{State.GatherYield(focus.resourceYield)}", wood ? 0 : 1);
                break;
            case IslandSpot.Kind.Workbench: Open("craft"); break;
            case IslandSpot.Kind.Cabin: Open("help"); break;
            case IslandSpot.Kind.TidePool: StartTide(focus.shoreIndex); break;
            case IslandSpot.Kind.FurnitureSlot: RetrieveFocusedFurniture(); break;
            case IslandSpot.Kind.Farm:
                if (State.FarmWait(now) == 0 && State.TryHarvest(now)) ActionDone("かぶを収穫しました", 3);
                else if (State.TryPlant(focus.cropGrowSeconds, now)) ActionDone("かぶの種をまきました", 2);
                break;
        }
        RefreshSpots(now);
        RefreshFocus();
    }

    public void Open(string panel)
    {
        if (panel == "craft" && !HasWorkbenchInReach) { Say("家の前の作業台に近づこう"); return; }
        if (IsPlacing) CancelPlacement();
        Panel = panel;
        focusRing.enabled = false;
    }
    public void ClosePanel() { Panel = ""; RefreshFocus(); }

    public void Craft(FurnitureRecipe recipe)
    {
        if (Panel != "craft" || !HasWorkbenchInReach || !recipe) return;
        if (!State.CanMakeRecipe(recipe)) { Say($"第{recipe.unlockAfterChapters}章の依頼を届けると解放されます"); return; }
        if (!State.TryCraft(recipe)) { Say("素材が足りません"); return; }
        ActionDone($"{recipe.displayName}を家具袋に入れました", 2);
    }

    public void SelectFurniture(FurnitureRecipe recipe)
    {
        if (!recipe || State.Count(recipe.id) < 1) { Say("作業台で家具を作ろう"); return; }
        CancelPlacement();
        Panel = "";
        held = recipe;
        turns = 0;
        ghost = Instantiate(recipe.prefab);
        foreach (var collider in ghost.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var guide in guides.Values) guide.enabled = true;
        RefreshFocus();
    }

    void RefreshPlacement()
    {
        Vector2 aim = PlayerPoint + new Vector2(player.forward.x, player.forward.z) * 1.4f;
        destination = null;
        float best = float.MaxValue;
        foreach (var entry in guides)
        {
            Paint(entry.Value, Pale);
            if (Vector2.Distance(PlayerPoint, entry.Key.Center) > entry.Key.reach + 0.55f) continue;
            float distance = (entry.Key.Center - aim).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            destination = entry.Key;
        }
        PlacementValid = destination && State.At(destination.id) == null
            && Vector2.Distance(PlayerPoint, destination.Center) > SlotRadius + PlayerRadius + 0.06f;
        ghost.SetActive(destination && State.At(destination.id) == null);
        if (destination)
        {
            ghost.transform.SetPositionAndRotation(OnGround(destination.Center) + Vector3.up * 0.08f,
                destination.transform.rotation * Quaternion.Euler(0, 90 * turns, 0));
            Paint(guides[destination], PlacementValid ? Good : Bad);
        }
        Prompt = !destination ? "手前の広場へ移動し、置きたい区画を向こう"
            : PlacementValid ? "[E]  この区画に置く" : State.At(destination.id) != null
                ? "この区画には家具があります" : "少し下がると置けます";
    }

    public void TurnPlacement() { if (IsPlacing) { turns = (turns+1)%4; RefreshPlacement(); } }
    public void CancelPlacement()
    {
        held = null;
        destination = null;
        PlacementValid = false;
        if (ghost) { ghost.SetActive(false); Destroy(ghost); }
        foreach (var guide in guides.Values) guide.enabled = false;
        RefreshFocus();
    }

    void Place()
    {
        if (!PlacementValid || !destination) { Say(Prompt); return; }
        if (!State.TryPlace(destination.id, held.id, turns)) return;
        var site = destination;
        string name = held.displayName;
        RefreshFurniture(site);
        CancelPlacement();
        ActionDone(name + "を置きました", 2);
    }

    public void RetrieveFocusedFurniture()
    {
        if (Panel.Length > 0 || IsPlacing) return;
        RefreshFocus();
        var site = focus;
        if (!site || site.kind != IslandSpot.Kind.FurnitureSlot) return;
        if (!State.TryRemove(site.id)) return;
        RefreshFurniture(site);
        ActionDone("家具袋に戻しました", 0);
        RefreshFocus();
    }

    void RefreshFurniture(IslandSpot site)
    {
        if (models.TryGetValue(site.id, out var old) && old)
        { old.SetActive(false); Destroy(old); }
        models.Remove(site.id);
        var item = State.At(site.id);
        if (item == null || !catalog.TryGetValue(item.recipeId, out var recipe)) return;
        var model = Instantiate(recipe.prefab, site.transform);
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 90 * item.turns, 0));
        models[site.id] = model;
    }

    void RefreshSpots(long now)
    {
        foreach (var site in spots)
        {
            if (site.kind == IslandSpot.Kind.Tree || site.kind == IslandSpot.Kind.Stone)
            {
                bool ready = State.ResourceWait(site.id, now) == 0;
                if (site.model) site.model.SetActive(ready);
                if (site.alternate) site.alternate.SetActive(!ready);
            }
            else if (site.kind == IslandSpot.Kind.Farm)
            {
                int wait = State.FarmWait(now);
                if (site.alternate) site.alternate.SetActive(wait > 0);
                if (site.ripe) site.ripe.SetActive(wait == 0);
            }
        }
    }

    LineRenderer MakeOutline(Transform parent, bool plot)
    {
        var root = new GameObject(plot ? "区画ガイド" : "注目している場所");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.up * 0.095f;
        var line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = markerMaterial;
        line.loop = true;
        line.widthMultiplier = plot ? 0.065f : 0.045f;
        line.numCornerVertices = 3;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = plot ? 4 : 48;
        if (plot) line.SetPositions(new[] {new Vector3(-1,0,-0.9f), new Vector3(-1,0,0.9f),
            new Vector3(1,0,0.9f), new Vector3(1,0,-0.9f)});
        else for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI*2 / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
        }
        Paint(line, plot ? Pale : Gold);
        line.enabled = false;
        return line;
    }

    static void Paint(LineRenderer line, Color color) { line.startColor = color; line.endColor = color; }
    void Say(string message, float seconds = 2.8f) { notice = message; noticeUntil = Time.unscaledTime + seconds; }
    void ActionDone(string message, int sound)
    {
        if (focus && Panel == "") feedback.Burst(focus.transform.position, sound==1 ? Pale : Gold);
        sounds.Play(sound);
        Say(message);
        if (!goalAnnounced && State.GoalReached) { goalAnnounced = true; Open("goal"); sounds.Play(4); }
        Commit();
    }
    bool Commit()
    {
        if (State == null) return false;
        State.playerX = player.position.x; State.playerZ = player.position.z;
        State.playerYaw = player.eulerAngles.y;
        nextPositionSave = Time.unscaledTime + 5;
        dirtyPosition = false;
        if (State.IsSaveReadOnly) return false;
        if (IslandSave.Write(savePath, State)) return true;
        Say("保存できませんでした。空き容量と保存先をご確認ください。", 8);
        return false;
    }
    public void SaveNow()
    {
        if (State.IsSaveReadOnly) Say("新しいバージョンの記録を保護しています");
        else if (Commit()) Say("島の暮らしを保存しました");
    }
    public void QuitGame()
    {
        Commit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        BrowserTools.ReturnToCover();
#else
        Application.Quit();
#endif
    }
    void OnApplicationFocus(bool focused) { if (!focused && State != null && dirtyPosition) Commit(); }
    void OnApplicationQuit() { if (State != null) Commit(); }
}
