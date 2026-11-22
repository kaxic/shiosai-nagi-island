using System.Collections.Generic;
using UnityEngine;

public sealed class IslandExhibitionHud : MonoBehaviour
{
    public IslandGame game;
    static readonly Color Ink = new Color32(43, 62, 49, 255), Pine = new Color32(57, 96, 76, 255),
        Paper = new Color32(250, 246, 226, 255), Leaf = new Color32(224, 230, 203, 255),
        Seal = new Color32(157, 66, 47, 255), Rule = new Color32(199, 202, 179, 255);
    static readonly string[] Arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
    Font sans, serif;
    GUIStyle style;
    int selected;
    bool wasOpen;
    bool[] conditions;
    List<ExhibitPlot> plots;

    void Awake()
    {
        sans = Font.CreateDynamicFontFromOSFont(new[] {"Meiryo", "Yu Gothic", "Noto Sans CJK JP", "Arial"}, 24);
        serif = Font.CreateDynamicFontFromOSFont(new[] {"Yu Mincho", "MS Mincho", "Meiryo", "Arial"}, 32);
    }
    void OnDestroy() { if (sans) Destroy(sans); if (serif && serif != sans) Destroy(serif); }

    void RefreshPage()
    {
        plots = game.GardenPlan();
        conditions = IslandExhibitions.Evaluate(selected, plots);
    }

    void OnGUI()
    {
        if (!game || game.State == null || game.Panel != "exhibit") { wasOpen = false; return; }
        if (!wasOpen) { RefreshPage(); wasOpen = true; }
        if (style == null) style = new GUIStyle(GUI.skin.label) { padding = new RectOffset(), clipping = TextClipping.Clip };
        float scale = Mathf.Min(Screen.width / 890f, Screen.height / 590f);
        if (scale <= 0) return;
        float w = Screen.width / scale, h = Screen.height / scale;
        var oldMatrix = GUI.matrix;
        int depth = GUI.depth;
        GUI.depth = -3;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        Fill(0, 0, w, h, new Color(.04f, .13f, .1f, .67f));
        float x = (w - 846) / 2, y = (h - 554) / 2;
        Fill(x + 5, y + 6, 846, 554, new Color(.04f, .1f, .08f, .30f));
        Fill(x, y, 846, 554, Paper);
        Fill(x, y, 8, 554, Pine);
        Text(x + 30, y + 14, 280, 19, "凪島園芸会　公募目録", 13, Pine);
        Text(x + 28, y + 30, 340, 44, "島の庭展", 32, Ink, book: true);
        Text(x + 281, y + 33, 353, 38, "家具を広場に並べ、テーマに合う庭を出品しよう。", 15, Ink);
        if (Button(x + 709, y + 27, 108, 35, "手帳へ戻る", false)) game.Open("pause");
        Fill(x + 28, y + 82, 790, 1, Rule);

        Text(x + 29, y + 95, 229, 25, $"出品印　{game.State.ExhibitionCount} / {IslandExhibitions.Count}", 16, Pine, bold: true);
        for (int i = 0; i < IslandExhibitions.Count; i++)
        {
            float rowY = y + 126 + i * 44;
            var exhibit = IslandExhibitions.Get(i);
            bool active = i == selected, earned = game.State.HasExhibitionStamp(i);
            Fill(x + 26, rowY, 226, 39, active ? Pine : Leaf);
            Fill(x + 33, rowY + 6, 27, 27, earned ? Seal : (active ? Paper : Rule));
            Text(x + 33, rowY + 5, 27, 27, earned ? exhibit.seal : (i + 1).ToString("00"), earned ? 18 : 13,
                earned ? Paper : Pine, TextAnchor.MiddleCenter, book: earned);
            Text(x + 70, rowY, 178, 39, exhibit.name, 16, active ? Paper : Ink);
            if (GUI.Button(new Rect(x + 26, rowY, 226, 39), GUIContent.none, GUIStyle.none))
            { selected = i; RefreshPage(); }
        }
        Fill(x + 265, y + 99, 1, 377, Rule);
        var show = IslandExhibitions.Get(selected);
        Text(x + 284, y + 91, 430, 39, show.name, 25, Ink, book: true);
        string access = show.chapter == 0 ? "最初の家具で出品できます" : $"必要な製作図は第{show.chapter}章までの報告で入手";
        Text(x + 286, y + 127, 475, 25, access, 14, Pine);
        if (game.State.HasExhibitionStamp(selected))
        {
            Fill(x + 761, y + 98, 43, 43, Seal);
            Text(x + 762, y + 98, 41, 40, show.seal, 27, Paper, TextAnchor.MiddleCenter, book: true);
        }
        for (int i = 0; i < show.LineCount; i++)
        {
            float line = y + 159 + i * 25;
            Fill(x + 287, line + 4, 16, 16, conditions[i] ? Pine : Rule);
            if (conditions[i]) Text(x + 286, line + 1, 18, 21, "✓", 13, Paper, TextAnchor.MiddleCenter, bold: true);
            Text(x + 311, line - 1, 497, 25, show.Line(i), 15, Ink);
        }
        DrawPlan(x + 285, y + 299, 523, 174);
        Fill(x + 28, y + 488, 790, 1, Rule);
        Text(x + 29, y + 499, 481, 42, "家具袋で選ぶ → 広場で R 回転・E 配置。出品後も家具は広場に残ります。", 13, Pine);
        if (Button(x + 525, y + 501, 131, 37, "配置を整える", false)) game.ClosePanel();
        bool stamped = game.State.HasExhibitionStamp(selected);
        bool ready = !stamped && System.Array.IndexOf(conditions, false) < 0;
        if (Button(x + 668, y + 501, 148, 37, stamped ? "出品済み" : ready ? "この庭を出品する" : "条件を確認", true, ready))
            game.SubmitExhibition(selected);
        GUI.matrix = oldMatrix;
        GUI.depth = depth;
    }

    void DrawPlan(float x, float y, float w, float h)
    {
        Fill(x, y, w, h, Leaf);
        Text(x + 12, y + 5, 150, 25, "いまの広場", 15, Ink, bold: true);
        Text(x + 148, y + 5, w - 160, 25, "上が島の奥　／　矢印は家具の正面", 13, Pine, TextAnchor.MiddleRight);
        if (plots.Count == 0) return;
        Vector2 min = plots[0].position, max = min;
        foreach (var plot in plots) { min = Vector2.Min(min, plot.position); max = Vector2.Max(max, plot.position); }
        float inner = w - 119;
        foreach (var plot in plots)
        {
            float px = x + 59 + inner * (max.x == min.x ? .5f : Mathf.InverseLerp(min.x, max.x, plot.position.x));
            float py = y + 62 + (h - 89) * (max.y == min.y ? .5f : 1 - Mathf.InverseLerp(min.y, max.y, plot.position.y));
            bool empty = plot.furniture == null;
            Fill(px - 47, py - 18, 94, 36, empty ? Rule : Pine);
            if (empty) Fill(px - 45, py - 16, 90, 32, Leaf);
            string name = empty ? "空き区画" : ShortName(plot.furniture) + " " +
                Arrows[(Mathf.RoundToInt(Mathf.Atan2(plot.front.x, plot.front.y) / (Mathf.PI / 4)) + 8) % 8];
            Text(px - 47, py - 18, 94, 36, name, 14, empty ? Pine : Paper, TextAnchor.MiddleCenter);
        }
    }

    string ShortName(string id)
    {
        switch (id)
        {
            case "bench": return "ベンチ"; case "lantern": return "ランタン"; case "planter": return "花";
            case "table": return "テーブル"; case "bookshelf": return "本棚"; case "birdhouse": return "巣箱";
            case "brazier": return "火鉢"; case "arch": return "アーチ"; case "daybed": return "デイベッド";
            default:
                foreach (var recipe in game.Recipes) if (recipe.id == id) return recipe.displayName;
                return "家具";
        }
    }

    void Text(float x, float y, float w, float h, string text, int size, Color color,
        TextAnchor align = TextAnchor.MiddleLeft, bool book = false, bool bold = false)
    {
        style.font = book ? serif : sans;
        style.fontSize = size; style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        style.normal.textColor = color; style.alignment = align;
        GUI.Label(new Rect(x, y, w, h), text, style);
    }
    bool Button(float x, float y, float w, float h, string label, bool primary, bool enabled = true)
    {
        var rect = new Rect(x, y, w, h);
        bool hot = rect.Contains(Event.current.mousePosition);
        Fill(x, y, w, h, !enabled ? Rule : primary ? (hot ? Ink : Pine) : (hot ? Rule : Leaf));
        Text(x, y, w, h, label, 15, enabled && primary ? Paper : Ink, TextAnchor.MiddleCenter, bold: primary);
        return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
    static void Fill(float x, float y, float w, float h, Color color) =>
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, 0);
}
