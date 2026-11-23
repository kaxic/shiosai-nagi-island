using System.Collections.Generic;
using UnityEngine;

public partial class IslandHud : MonoBehaviour
{
    public IslandGame game;

    static readonly Color Ink = new Color32(29, 59, 51, 255);
    static readonly Color Forest = new Color32(35, 73, 60, 255);
    static readonly Color Mint = new Color32(195, 218, 182, 255);
    static readonly Color Paper = new Color32(250, 246, 227, 255);
    static readonly Color Page = new Color32(238, 232, 207, 255);
    static readonly Color Edge = new Color32(194, 190, 163, 255);
    static readonly Color Muted = new Color32(79, 99, 83, 255);
    static readonly Color Gold = new Color32(242, 192, 108, 255);
    static readonly Color Shadow = new Color32(18, 42, 36, 58);

    Font body, title;
    GUIStyle textStyle, frame;
    Texture2D corner;
    readonly Dictionary<string, Texture2D> symbols = new Dictionary<string, Texture2D>();
    Vector2 listScroll;
    string previousPanel;
    float width, height;
    const float Margin = 17;

    void Awake()
    {
        body = BrowserTools.ReadableFont(new[] { "Meiryo", "Yu Gothic", "Hiragino Sans", "Noto Sans CJK JP", "Noto Sans JP", "Arial Unicode MS", "Arial" }, 24);
        title = BrowserTools.ReadableFont(new[] { "Yu Mincho", "MS Mincho", "Hiragino Mincho ProN", "Noto Serif CJK JP", "Meiryo", "Arial Unicode MS", "Arial" }, 32);
        corner = RoundedTexture();
        foreach (string name in SymbolNames)
            symbols.Add(name, MakeSymbol(name));
    }

    void OnDestroy()
    {
        foreach (Texture2D texture in symbols.Values) Destroy(texture);
        if (corner) Destroy(corner);
        if (body) Destroy(body);
        if (title && title != body) Destroy(title);
    }

    void OnGUI()
    {
        if (!game || game.State == null) return;
        string panel = game.Panel ?? "";
        if (panel == "title" || panel == "tide" || panel == "album" || panel == "exhibit") return;
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { padding = new RectOffset(), clipping = TextClipping.Clip, richText = false };
            frame = new GUIStyle { border = new RectOffset(6, 6, 6, 6) };
            frame.normal.background = corner;
        }
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1080f, Screen.height / 660f), .84f, 2.1f);
        scale = Mathf.Min(scale, Screen.width / 694f, Screen.height / 435f);
        width = Screen.width / scale;
        height = Screen.height / scale;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        if (panel != previousPanel)
        {
            listScroll = Vector2.zero;
            previousPanel = panel;
            if (panel == "orders") showArchive = false;
            if (panel == "tools") showMarket = false;
        }

        if (panel.Length > 0) Dialog(panel);
        else { Header(); Footer(); }
        GUI.matrix = oldMatrix;
    }

    void Header()
    {
        float x = Margin, y = Margin;
        Card(x, y, 206, 61, Ink);
        Box(x + 9, y + 9, 43, 43, Gold);
        Symbol("island", x + 12, y + 12, 37, Forest);
        Label("N A G I   I S L A N D", x + 63, y + 6, 137, 17, 10, Mint, bold: true);
        Label("凪島だより", x + 61, y + 20, 141, 34, 26, Paper, serif: true);

        var order = new Rect(x, y + 69, 225, 57);
        bool ready = game.State.CanDeliverOrder;
        Card(order.x, order.y, order.width, order.height, order.Contains(Event.current.mousePosition) ? Color.white : Paper);
        Symbol("letter", order.x + 9, order.y + 14, 27, Forest);
        Label(ready ? "納品できます" : game.State.CampaignComplete ? "港の定期便" : $"第 {game.State.CompletedChapters + 1} 章の依頼", order.x + 45, order.y + 3, 142, 23, 12, Muted, bold: true);
        Label(game.State.CurrentOrder.title, order.x + 44, order.y + 25, order.width - 52, 25, 15, Ink, bold: true);
        Box(order.xMax - 28, order.y + 7, 20, 18, ready ? Gold : Page);
        Label("J", order.xMax - 28, order.y + 6, 20, 20, 11, Ink, TextAnchor.MiddleCenter, bold: true);
        if (Hit(order)) game.Open("orders");

        float sx = width - Margin - 348;
        Card(sx, y, 348, 52, Paper);
        Resource(sx, y, "wood", "木材", game.State.wood);
        Resource(sx + 87, y, "stone", "石材", game.State.stone);
        Resource(sx + 174, y, "leaf", "かぶ", game.State.Turnips);
        Resource(sx + 261, y, "shell", "貝貨", game.State.Shells);
        for (int i = 1; i <= 3; i++) Solid(sx + 87 * i, y + 11, 1, 30, Edge);
        if (Hit(new Rect(sx + 261, y, 87, 52))) game.Open("tools");
    }

    void Resource(float x, float y, string symbol, string name, int value)
    {
        Symbol(symbol, x + 9, y + 13, 25, Forest);
        Label(name, x + 41, y + 5, 44, 17, 11, Muted, bold: true);
        Label(CompactCount(value), x + 39, y + 20, 46, 26, value < 1000 ? 18 : 13, Ink, bold: true);
    }

    void Footer()
    {
        float y = height - Margin - 37;
        KeyButton(new Rect(Margin, y, 134, 37), "F1", "島の案内", "help");
        if (!game.IsPlacing && Button(new Rect(Margin + 142, y, 159, 37), game.State.passage ? "T　凪島に帰る" : "T　白砂の離島へ", true, false)) game.Sail();
        KeyButton(new Rect(width - Margin - 239, y, 128, 37), "Tab", "家具袋", "bag");
        KeyButton(new Rect(width - Margin - 104, y, 104, 37), "Esc", "手帳", "pause");

        float messageY = y - 57;
        if (game.IsPlacing)
        {
            float w = Mathf.Min(width - Margin * 2, 662), x = (width - w) / 2;
            Card(x, messageY - 9, w, 55, Paper);
            Label(game.PlacementName, x + 11, messageY - 6, w - 348, 26, 17, Ink, bold: true);
            string placementHint = game.PlacementValid ? "この区画に飾れます" : game.Prompt;
            Label(placementHint, x + 11, messageY + 18, w - 348, 20, 13, Muted);
            if (Button(new Rect(x + w - 330, messageY, 105, 37), "E　飾る", game.PlacementValid)) game.Interact();
            if (Button(new Rect(x + w - 218, messageY, 99, 37), "R　回転", true, false)) game.TurnPlacement();
            if (Button(new Rect(x + w - 112, messageY, 102, 37), "Esc　戻す", true, false)) game.CancelPlacement();
            messageY -= 58;
        }
        else
        {
            string prompt = game.Prompt ?? "";
            if (prompt.Length > 0)
            {
                float w = Mathf.Clamp(prompt.Length * 15 + 43, 180, Mathf.Min(width - Margin * 2, 570));
                var r = new Rect((width - w) / 2, messageY + 7, w, 39);
                Card(r.x, r.y, r.width, r.height, r.Contains(Event.current.mousePosition) ? Forest : Ink);
                Label(prompt, r.x + 4, r.y, r.width - 8, r.height, 16, Paper, TextAnchor.MiddleCenter, bold: true);
                if (Hit(r)) game.Interact();
                messageY -= 46;
            }
        }
        if (!string.IsNullOrEmpty(game.Notice))
        {
            float w = Mathf.Clamp(game.Notice.Length * 14 + 38, 150, width - 2 * Margin);
            float x = (width - w) / 2;
            Card(x, messageY + 11, w, 32, Paper);
            Label(game.Notice, x + 8, messageY + 11, w - 16, 32, 14, Ink, TextAnchor.MiddleCenter, bold: true);
        }
        if (!game.IsPlacing && width >= 800)
            Label(width >= 1020 ? "WASD ／ 矢印で移動　・　Shift で走る" : "WASD ／ 矢印で移動", Margin + 316, y + 1, width - 598, 35, 13, Paper, shadow: true);
    }

    void KeyButton(Rect r, string key, string caption, string panel)
    {
        Card(r.x, r.y, r.width, r.height, r.Contains(Event.current.mousePosition) ? Forest : Ink);
        Box(r.x + 6, r.y + 6, 34, r.height - 12, Paper);
        Label(key, r.x + 6, r.y + 5, 34, r.height - 10, 13, Ink, TextAnchor.MiddleCenter, bold: true);
        Label(caption, r.x + 42, r.y, r.width - 48, r.height, 15, Paper, TextAnchor.MiddleCenter, bold: true);
        if (Hit(r)) game.Open(panel);
    }

    void Dialog(string panel)
    {
        Solid(0, 0, width, height, new Color(.035f, .103f, .087f, .56f));
        bool inventory = panel == "craft" || panel == "bag";
        float w = Mathf.Min(width - Margin * 2, 588);
        float wantedHeight = inventory ? 540
            : panel == "orders" ? 549 : panel == "tools" ? 520 : panel == "credits" ? 513
            : panel == "help" ? 541 : panel == "goal" ? 395 : 435;
        float h = Mathf.Min(height - 24, wantedHeight);
        float x = (width - w) / 2, y = (height - h) / 2;
        Card(x, y, w, h, Paper);
        Solid(x + 13, y + 16, 4, 42, Forest);
        string heading = panel == "craft" ? "作業台" : panel == "bag" ? "家具袋" : panel == "help" ? "島の案内" : panel == "goal" ? "暮らしの目標"
            : panel == "orders" ? "島の依頼帳" : panel == "tools" ? "道具と交易" : panel == "credits" ? "おわりの便り" : "開拓手帳";
        string section = panel == "craft" ? "WORKSHOP" : panel == "bag" ? "FURNITURE" : panel == "help" ? "FIELD GUIDE" : panel == "goal" ? "ISLAND JOURNAL"
            : panel == "orders" ? "ISLAND POST" : panel == "tools" ? "TRADING POST" : panel == "credits" ? "NAGI ISLAND  /  CREDITS" : "FIELD NOTES";
        Label(section, x + 26, y + 11, w - 115, 17, 10, Muted, bold: true);
        Label(heading, x + 24, y + 26, w - 112, 36, 25, Ink, serif: true);
        if (Button(new Rect(x + w - 80, y + 18, 64, 32), "閉じる", true, false)) game.ClosePanel();
        Solid(x + 16, y + 66, w - 32, 1, Edge);
        if (inventory) Inventory(panel == "craft", x, y, w, h);
        else if (panel == "orders") Orders(x, y, w, h);
        else if (panel == "tools") Tools(x, y, w, h);
        else if (panel == "credits") Credits(x, y, w, h);
        else if (panel == "help") Guide(x, y, w, h);
        else if (panel == "goal") Goal(x, y, w, h);
        else Pause(x, y, w, h);
    }

    void Inventory(bool craft, float x, float y, float w, float h)
    {
        string tip = craft ? $"所持素材　 木材 {game.State.wood}　 石材 {game.State.stone}" : "飾りたい家具を選んで、広場の空き区画へ。";
        Label(tip, x + 18, y + 72, w - 36, 27, 15, Muted, bold: craft);
        FurnitureRecipe[] recipes = game.Recipes;
        Rect area = new Rect(x + 14, y + 109, w - 28, h - 159);
        int length = recipes?.Length ?? 0;
        float rowWidth = area.width - (length * 65 > area.height ? 18 : 1);
        listScroll = GUI.BeginScrollView(area, listScroll, new Rect(0, 0, rowWidth, length * 65));
        for (int i = 0; i < length; i++)
        {
            FurnitureRecipe recipe = recipes[i];
            if (recipe == null) continue;
            float ry = i * 65;
            Box(0, ry, rowWidth, 58, Page);
            bool unlocked = game.State.CanMakeRecipe(recipe);
            Box(7, ry + 7, 45, 44, unlocked ? Forest : Edge);
            Symbol(symbols.ContainsKey(recipe.id ?? "") ? recipe.id : "furniture", 12, ry + 12, 35, unlocked ? Paper : Muted);
            Label(recipe.displayName, 63, ry + 4, rowWidth - 190, 26, 17, Ink, bold: true);
            int count = game.State.Count(recipe.id);
            string cost = craft ? $"木材 {recipe.woodCost}   石材 {recipe.stoneCost}   ｜   所持 {count}" : $"所持　{count} 個";
            Label(!unlocked && count == 0 ? $"第 {recipe.unlockAfterChapters} 章の依頼を届けると解放" : cost, 64, ry + 30, rowWidth - 183, 22, 13, Muted);
            bool enabled = craft ? unlocked && game.HasWorkbenchInReach && game.State.wood >= recipe.woodCost && game.State.stone >= recipe.stoneCost : count > 0;
            string action = craft ? (enabled ? "作る" : !unlocked ? "未解放" : "素材不足") : (enabled ? "飾る" : "未所持");
            if (Button(new Rect(rowWidth - 102, ry + 10, 93, 38), action, enabled))
            {
                if (craft) game.Craft(recipe);
                else game.SelectFurniture(recipe);
            }
        }
        GUI.EndScrollView();
        string foot = !string.IsNullOrEmpty(game.Notice) ? game.Notice : craft ? "作った家具は家具袋に入ります。" : "家具は家の前の作業台で作れます。";
        Label(foot, x + 17, y + h - 41, w - 170, 31, 13, Muted);
        bool canSwitch = craft || game.HasWorkbenchInReach;
        if (Button(new Rect(x + w - 145, y + h - 42, 130, 32), craft ? "家具袋を開く" : "作業台を開く", canSwitch, false))
            game.Open(craft ? "bag" : "craft");
    }

    void Guide(float x, float y, float w, float h)
    {
        string[] keys = { "WASD ／ 矢印", "Shift ／ ホイール", "E", "Tab", "R", "J ／ U", "Esc ／ F1" };
        string[] actions = { "画面の上下左右へ移動", "走る ／ カメラを拡大・縮小", "近くを調べる・採集・畑の世話・家具の配置", "家具袋を開く", "配置中に90°回転・飾った家具のそばで回収", "島の依頼帳 ／ 道具と交易を開く", "配置を取りやめる・手帳を開く ／ 島の案内" };
        float sw = BeginPage(new Rect(x + 16, y + 77, w - 32, h - 92), 647);
        for (int i = 0; i < keys.Length; i++)
        {
            float ly = i * 34;
            Box(0, ly, 119, 30, Page);
            Label(keys[i], 0, ly, 119, 30, 13, Ink, TextAnchor.MiddleCenter, bold: true);
            Label(actions[i], 130, ly, sw - 134, 30, 14, Ink);
        }
        GuideNote(sw, 245, "compass", "島の歩き方", "家のすぐ手前に作業台、右奥にかぶ畑、手前の広場に家具の区画があります。木と岩で素材を集め、作業台で家具を作りましょう。");
        GuideNote(sw, 346, "letter", "港からの依頼", "依頼帳で納品条件を確かめましょう。受け取った貝貨は道具の強化に使えます。交易では、余った木材・石材・かぶも貝貨に交換できます。");
        GuideNote(sw, 447, "shell", "浜辺の図鑑", "Tで白砂の離島へ渡れます。二つの島の6か所で、潮の引く瞬間に標本を拾いましょう。開拓手帳から浜辺図鑑を開いて、これまでの発見を振り返れます。");
        GuideNote(sw, 548, "planter", "島の庭展", "開拓手帳から島の庭展を開くと、公募のお題を読めます。家具の種類・並び方・向きを工夫して、広場を整えたら応募しましょう。");
        GUI.EndScrollView();
    }

    void Goal(float x, float y, float w, float h)
    {
        bool complete = game.State.GoalReached;
        Box(x + 16, y + 76, w - 32, 33, complete ? Forest : Page);
        Symbol(complete ? "seal" : "compass", x + 24, y + 80, 25, complete ? Gold : Forest);
        Label(complete ? "暮らしの目標を達成しました。" : "島に家具を飾り、畑で初めての収穫を迎えよう。", x + 59, y + 76, w - 83, 33, 16, complete ? Paper : Ink, bold: true);
        ProgressRow(x + 17, y + 117, w - 34, "bag", "広場に違う家具を3種類飾る", complete ? 3 : game.PlacedKinds, 3);
        ProgressRow(x + 17, y + 172, w - 34, "leaf", "畑で作物を1回収穫する", game.State.harvests, 1);
        Label(complete ? "島の依頼帳には、港から届いた便りが待っています。" : "家具は3種類を同時に飾ると達成です。", x + 19, y + 227, w - 38, 33, 14, Muted);
        Solid(x + 16, y + 269, w - 32, 1, Edge);
        Label($"開拓ランク {game.State.Rank}　{game.State.RankName}", x + 18, y + 281, w - 36, 29, 16, Forest, bold: true);
        Label($"作った家具 {game.State.DiscoveredKinds} 種　／　収穫の記録 {game.State.harvests} 回", x + 19, y + 310, w - 38, 23, 13, Muted);
        if (Button(new Rect(x + 17, y + h - 55, 144, 40), "島の依頼帳へ", true, false)) game.Open("orders");
        if (Button(new Rect(x + w - 145, y + h - 55, 128, 40), "島に戻る", true)) game.ClosePanel();
    }

    void ProgressRow(float x, float y, float w, string icon, string words, int count, int total)
    {
        Box(x, y, w, 47, Page);
        Symbol(icon, x + 12, y + 10, 27, Forest);
        Label(words, x + 51, y, w - 165, 47, 16, Ink);
        Label($"{Mathf.Min(count, total)}  /  {total}", x + w - 88, y, 70, 47, 17, count >= total ? Forest : Muted, TextAnchor.MiddleRight, bold: true);
    }

    void Pause(float x, float y, float w, float h)
    {
        float sw = BeginPage(new Rect(x + 17, y + 78, w - 34, h - 94), 349);
        float half = (sw - 9) / 2;
        if (Button(new Rect(0, 0, sw, 39), "島に戻る", true)) game.ClosePanel();
        string[] panels = { "orders", "tools", "goal", "album", "exhibit", "help", "credits", "title" };
        string[] labels = { "島の依頼帳", "道具と交易", "暮らしの目標", "浜辺図鑑", "島の庭展", "島の案内", "おわりの便り", "タイトルへ" };
        for (int i = 0; i < panels.Length; i++)
        {
            bool enabled = panels[i] != "credits" || game.State.CampaignComplete;
            if (Button(new Rect((i % 2) * (half + 9), 49 + i / 2 * 44, half, 37), labels[i], enabled, false)) game.Open(panels[i]);
        }
        Solid(0, 229, sw, 1, Edge);
        if (Button(new Rect(0, 241, half, 37), "セーブする", true, false)) game.SaveNow();
        if (Button(new Rect(half + 9, 241, half, 37), "ゲームを終了", true, false)) game.QuitGame();
        Label(game.Notice ?? "", 1, 290, sw - 2, 44, 14, Muted, TextAnchor.MiddleCenter, wrap: true);
        GUI.EndScrollView();
    }

    static bool Hit(Rect r) => GUI.Button(r, GUIContent.none, GUIStyle.none);

    bool Button(Rect r, string words, bool enabled, bool primary = true)
    {
        bool hover = enabled && r.Contains(Event.current.mousePosition);
        Color bg = enabled ? (primary ? (hover ? Ink : Forest) : (hover ? Mint : Page)) : new Color32(225, 222, 200, 255);
        Box(r.x, r.y, r.width, r.height, bg);
        Label(words, r.x + 3, r.y, r.width - 6, r.height, 15, enabled && primary ? Paper : enabled ? Ink : Muted, TextAnchor.MiddleCenter, bold: true);
        return enabled && Hit(r);
    }

    void Label(string words, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false, bool serif = false, bool wrap = false, bool shadow = false)
    {
        if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(words)) return;
        textStyle.font = serif ? title : body;
        textStyle.fontSize = size;
        textStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        textStyle.alignment = align;
        textStyle.wordWrap = wrap;
        if (!wrap)
        {
            float nativeWidth = textStyle.CalcSize(new GUIContent(words)).x;
            if (nativeWidth > w) textStyle.fontSize = Mathf.Clamp(Mathf.FloorToInt(size * w / nativeWidth), 10, size);
        }
        if (shadow)
        {
            textStyle.normal.textColor = Ink;
            GUI.Label(new Rect(x + 1, y + 1, w, h), words, textStyle);
        }
        textStyle.normal.textColor = color;
        GUI.Label(new Rect(x, y, w, h), words, textStyle);
    }

    void Card(float x, float y, float w, float h, Color c)
    {
        Box(x + 1, y + 3, w, h, Shadow);
        Box(x, y, w, h, c);
    }

    void Box(float x, float y, float w, float h, Color c)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color old = GUI.color;
        GUI.color = c;
        frame.Draw(new Rect(x, y, w, h), GUIContent.none, false, false, false, false);
        GUI.color = old;
    }

    static void Solid(float x, float y, float w, float h, Color c)
    {
        if (Event.current.type == EventType.Repaint)
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, c, 0, 0);
    }

    void Symbol(string name, float x, float y, float size, Color color)
    {
        if (Event.current.type == EventType.Repaint)
            GUI.DrawTexture(new Rect(x, y, size, size), symbols[name], ScaleMode.ScaleToFit, true, 1, color, 0, 0);
    }

    static Texture2D RoundedTexture()
    {
        const int n = 16;
        var data = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Max(Mathf.Abs(x + .5f - 8) - 2.5f, 0), dy = Mathf.Max(Mathf.Abs(y + .5f - 8) - 2.5f, 0);
            byte a = (byte)(255 * Mathf.Clamp01(6 - Mathf.Sqrt(dx * dx + dy * dy)));
            data[y * n + x] = new Color32(255, 255, 255, a);
        }
        return Texture(n, data);
    }

    // Draw the little field-guide stamps on a 32-unit grid, supersampled for large screens.
    static Texture2D MakeSymbol(string name)
    {
        const int n = 64;
        var data = new Color32[n * n];
        for (int k = 0; k < data.Length; k++) data[k] = new Color32(255, 255, 255, 0);
        void Stroke(float weight, params float[] xy)
        {
            for (int i = 2; i < xy.Length; i += 2)
            {
                Vector2 a = new Vector2(xy[i-2], xy[i-1]), b = new Vector2(xy[i], xy[i+1]);
                Vector2 d = b - a;
                float sq = Mathf.Max(.001f, d.sqrMagnitude);
                for (int yy = 0; yy < n; yy++)
                for (int xx = 0; xx < n; xx++)
                {
                    Vector2 p = new Vector2((xx + .5f) / 2, (yy + .5f) / 2);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, d) / sq);
                    byte alpha = (byte)(255 * Mathf.Clamp01((weight * .5f + .24f - (p - a - t * d).magnitude) * 2));
                    int idx = (n - 1 - yy) * n + xx;
                    if (alpha > data[idx].a) data[idx].a = alpha;
                }
            }
        }
        switch (name)
        {
            case "island":
                Stroke(2.8f, 3,22, 10,18, 17,18, 21,15, 28,20);
                Stroke(1.8f, 5,25, 13,25, 19,23, 27,23);
                Stroke(1.7f, 12,28, 18,27, 23,27);
                Stroke(1.8f, 12,18, 12,13); Stroke(2, 6,14, 12,5, 17,14, 6,14);
                Stroke(4.5f, 23,7, 23,7); break;
            case "wood":
                Stroke(2, 4,18, 19,6, 24,6, 28,10, 28,15, 14,26, 8,26, 4,22, 4,18);
                Stroke(1.8f, 4,18, 10,17, 14,20, 14,26); Stroke(1.6f, 17,14, 24,9);
                Stroke(1.6f, 8,20, 10,21, 10,23); break;
            case "stone":
                Stroke(2, 3,22, 8,13, 17,7, 24,9, 29,21, 23,25, 10,25, 3,22);
                Stroke(1.5f, 8,13, 17,14, 24,9); Stroke(1.5f, 17,14, 22,23); break;
            case "leaf":
                Stroke(2, 7,27, 22,10); Stroke(2, 12,22, 9,16, 11,10, 15,13, 15,18);
                Stroke(2, 17,17, 15,10, 17,5, 20,9, 20,13);
                Stroke(2, 17,17, 23,16, 27,12, 21,11); Stroke(2, 12,22, 20,21, 24,18, 18,17);
                Stroke(2, 21,11, 22,6, 26,3, 26,7, 21,11); break;
            case "compass":
                Stroke(1.6f, 16,3, 19,12, 28,16, 19,19, 16,29, 13,19, 4,16, 13,12, 16,3);
                Stroke(1.6f, 16,3, 16,16, 28,16); Stroke(1.6f, 16,16, 16,29); break;
            case "bag":
                Stroke(2, 6,11, 26,11, 27,26, 5,26, 6,11); Stroke(2, 11,11, 12,6, 15,4, 18,4, 20,6, 21,11);
                Stroke(1.8f, 6,16, 26,16); Stroke(2, 15,16, 15,19, 18,19, 18,16); break;
            case "bench":
                Stroke(2, 5,8, 26,8, 26,12, 5,12, 5,8); Stroke(2, 7,13, 7,18, 25,18, 25,13);
                Stroke(2, 5,18, 27,18, 28,21, 4,21, 5,18); Stroke(2, 7,22, 6,27); Stroke(2, 25,22, 26,27); break;
            case "lantern":
                Stroke(2, 11,10, 11,7, 14,4, 18,4, 21,7, 21,10);
                Stroke(2, 7,12, 10,9, 22,9, 25,12, 7,12); Stroke(2, 9,13, 11,24, 21,24, 23,13);
                Stroke(2, 10,25, 22,25, 24,28, 8,28, 10,25); Stroke(1.8f, 16,21, 15,19, 17,16, 18,19, 16,21); break;
            case "planter":
                Stroke(2, 5,18, 27,18, 26,21, 6,21, 5,18); Stroke(2, 8,22, 10,28, 22,28, 24,22);
                Stroke(1.8f, 15,18, 15,10, 12,7, 9,8, 10,11, 15,12);
                Stroke(1.8f, 15,15, 19,10, 22,10, 22,13, 19,15, 15,15); Stroke(2.7f, 14,6, 15,5); break;
            case "shell":
                Stroke(2, 12,26, 6,19, 3,12, 5,8, 8,7, 11,8, 12,5, 15,4, 18,6, 20,5, 24,7, 24,9, 27,9, 29,13, 26,20, 20,26, 12,26);
                Stroke(1.5f, 12,25, 7,11); Stroke(1.5f, 15,24, 13,9); Stroke(1.5f, 17,24, 20,9); Stroke(1.5f, 20,24, 25,12); break;
            case "letter":
                Stroke(2, 3,8, 29,8, 29,25, 3,25, 3,8, 16,18, 29,8);
                Stroke(1.5f, 5,23, 12,17); Stroke(1.5f, 27,23, 21,17); break;
            case "seal":
                Stroke(2, 16,3, 20,5, 24,6, 26,10, 28,15, 26,20, 23,24, 19,25, 16,28, 12,26, 7,24, 6,20, 3,16, 5,11, 7,7, 12,5, 16,3);
                Stroke(2.7f, 10,15, 14,19, 23,11); break;
            case "hammer":
                Stroke(2, 18,10, 23,14, 9,28, 5,25, 18,10);
                Stroke(2, 12,7, 15,4, 22,6, 28,12, 24,17, 18,11, 16,9, 12,7); break;
            case "watering":
                Stroke(2, 10,13, 23,13, 25,25, 8,25, 10,13);
                Stroke(2, 10,18, 6,16, 3,12, 4,10, 9,12); Stroke(2, 23,16, 28,13, 28,9, 26,8, 23,10);
                Stroke(1.7f, 13,12, 13,10, 16,8, 21,9, 21,12); break;
            case "crate":
                Stroke(2, 3,9, 29,9, 29,25, 3,25, 3,9); Stroke(1.8f, 7,13, 25,13, 7,22, 25,22);
                Stroke(1.7f, 7,13, 25,22); break;
            case "table":
                Stroke(2, 3,12, 8,9, 23,9, 28,12, 23,15, 8,15, 3,12);
                Stroke(2, 8,16, 7,25); Stroke(2, 23,16, 24,25); Stroke(1.5f, 12,15, 12,20); break;
            case "bookshelf":
                Stroke(2, 5,5, 27,5, 27,27, 5,27, 5,5); Stroke(1.8f, 5,16, 27,16);
                Stroke(2, 9,14, 9,9); Stroke(2, 13,14, 12,9); Stroke(2, 20,14, 18,9);
                Stroke(2, 10,24, 10,19); Stroke(2, 16,24, 14,20); Stroke(2, 22,24, 22,19); break;
            case "brazier":
                Stroke(2, 4,20, 28,20, 24,25, 8,25, 4,20); Stroke(2, 10,25, 8,28); Stroke(2, 22,25, 24,28);
                Stroke(2, 11,18, 9,14, 13,8, 15,12, 18,4, 22,11, 23,15, 20,18); break;
            case "arch":
                Stroke(2, 5,27, 5,12, 8,7, 14,4, 18,4, 24,7, 27,12, 27,27);
                Stroke(1.9f, 9,27, 9,13, 11,10, 15,8, 18,8, 22,10, 23,13, 23,27);
                Stroke(2, 3,27, 11,27); Stroke(2, 21,27, 29,27); Stroke(2.7f, 15,6, 15,6); break;
            case "birdhouse":
                Stroke(2, 3,13, 15,4, 28,13); Stroke(2, 8,12, 8,23, 24,23, 24,12);
                Stroke(2, 15,23, 15,29); Stroke(3.8f, 16,14, 16,14); Stroke(1.6f, 14,19, 18,19); break;
            case "daybed":
                Stroke(2, 3,9, 3,25); Stroke(2, 29,12, 29,25); Stroke(2, 4,18, 28,18, 28,22, 4,22, 4,18);
                Stroke(2, 5,16, 7,13, 12,13, 14,16, 27,16); break;
            default:
                Stroke(2, 5,12, 16,6, 27,12, 27,23, 16,28, 5,23, 5,12, 16,17, 27,12); Stroke(2, 16,17, 16,28); break;
        }
        return Texture(n, data);
    }

    static Texture2D Texture(int size, Color32[] data)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels32(data);
        tex.Apply(false, true);
        return tex;
    }
}
