using UnityEngine;

public partial class IslandHud
{
    static readonly IslandTool[] ToolTypes = { IslandTool.Gathering, IslandTool.Gardening, IslandTool.Trading };
    static readonly IslandGood[] TradeGoods = { IslandGood.Wood, IslandGood.Stone, IslandGood.Turnip };
    static readonly string[] SymbolNames =
    {
        "island", "wood", "stone", "leaf", "bag", "compass", "bench", "lantern", "planter", "furniture",
        "shell", "letter", "seal", "hammer", "watering", "crate", "table", "bookshelf", "brazier", "arch", "birdhouse", "daybed"
    };
    bool showArchive, showMarket;
    string receipt;
    float receiptUntil;

    static string CompactCount(int value)
    {
        if (value < 10000) return value.ToString();
        return value < 100000000 ? (value / 10000) + "万" : (value / 100000000) + "億";
    }

    float BeginPage(Rect bounds, float contentHeight)
    {
        float innerWidth = bounds.width - (contentHeight > bounds.height ? 19 : 1);
        listScroll = GUI.BeginScrollView(bounds, listScroll, new Rect(0, 0, innerWidth, contentHeight));
        return innerWidth;
    }

    void GuideNote(float w, float y, string icon, string heading, string bodyText)
    {
        Solid(0, y, w, 1, Edge);
        Symbol(icon, 1, y + 10, 25, Forest);
        Label(heading, 34, y + 5, w - 37, 35, 17, Forest, bold: true);
        Label(bodyText, 2, y + 43, w - 7, 54, 14, Ink, TextAnchor.UpperLeft, wrap: true);
    }

    void Tabs(float x, float y, float w, string left, string right, ref bool second)
    {
        float half = (w - 6) / 2;
        for (int i = 0; i < 2; i++)
        {
            bool selected = second == (i == 1);
            Rect r = new Rect(x + i * (half + 6), y, half, 32);
            Box(r.x, r.y, r.width, r.height, selected ? Forest : r.Contains(Event.current.mousePosition) ? Mint : Page);
            Label(i == 0 ? left : right, r.x + 3, r.y, r.width - 6, r.height, 14, selected ? Paper : Ink, TextAnchor.MiddleCenter, bold: true);
            if (Hit(r) && !selected)
            {
                second = i == 1;
                listScroll = Vector2.zero;
            }
        }
    }

    void Orders(float x, float y, float w, float h)
    {
        IslandState state = game.State;
        Tabs(x + 16, y + 78, w - 32, state.CampaignComplete ? "港の定期便" : "届いた依頼", "納品の記録", ref showArchive);
        Rect viewport = new Rect(x + 16, y + 122, w - 32, h - 183);
        if (showArchive)
        {
            float wide = BeginPage(viewport, 90 + IslandEconomy.ChapterCount * 52);
            Label($"届けた便り　{state.CompletedChapters} / {IslandEconomy.ChapterCount} 章", 1, 0, wide, 31, 21, Ink, serif: true);
            Label($"開拓ランク {state.Rank}　{state.RankName}　　 定期便 {state.RepeatOrders} 件", 2, 34, wide, 23, 14, Muted);
            ChapterStrip(1, 66, wide - 2);
            for (int i = 0; i < IslandEconomy.ChapterCount; i++)
            {
                float sy = 86 + i * 52;
                bool done = i < state.CompletedChapters, current = i == state.CompletedChapters;
                IslandOrder order = IslandEconomy.GetChapter(i);
                Box(0, sy, wide, 47, done ? Mint : Page);
                if (done) Symbol("seal", 10, sy + 9, 28, Forest);
                else Label((i + 1).ToString("00"), 7, sy + 3, 36, 41, 15, Muted, TextAnchor.MiddleCenter, bold: true);
                Label(done || current ? order.title : $"第 {i + 1} 章の便り", 49, sy + 1, wide - 180, 25, 16, Ink, bold: true);
                Label(done || current ? order.client : "前の便りを届けると開封", 50, sy + 23, wide - 181, 22, 12, Muted);
                Label(done ? "納品済" : current ? "受付中" : "未開封", wide - 95, sy + 2, 82, 42, 14, done || current ? Forest : Muted, TextAnchor.MiddleRight, bold: true);
            }
            GUI.EndScrollView();
            if (Button(new Rect(x + w - 163, y + h - 49, 147, 36), "届いた依頼へ", true)) { showArchive = false; listScroll = Vector2.zero; }
            return;
        }

        IslandOrder currentOrder = state.CurrentOrder;
        IslandRequirement[] needs = state.OrderRequirements();
        bool ready = state.CanDeliverOrder;
        bool compact = h < 460;
        float header = compact ? 63 : 101;
        float narrativeY = header + (compact ? 20 : 26);
        float lineY = narrativeY + (compact ? 42 : 55);
        float rowsY = lineY + (compact ? 24 : 28);
        float rowStep = compact ? 35 : 43;
        float sw = BeginPage(viewport, rowsY + needs.Length * rowStep);
        Box(0, 0, sw, header, Forest);
        Symbol("letter", 12, compact ? 8 : 12, compact ? 23 : 28, Gold);
        string number = state.CampaignComplete ? $"定期便　{(long)state.RepeatOrders + 1} 通目" : $"第 {state.CompletedChapters + 1} 章　／　全 {IslandEconomy.ChapterCount} 章";
        Label(number, compact ? 42 : 50, compact ? 5 : 7, sw * .6f, 25, 12, Mint, bold: true);
        Label(currentOrder.title, 14, compact ? 29 : 36, sw - 28, compact ? 30 : 35, compact ? 22 : 25, Paper, serif: true);
        if (compact) Label(currentOrder.client + " より", sw * .54f, 6, sw * .46f - 13, 23, 12, Mint, TextAnchor.MiddleRight);
        else Label(currentOrder.client + " より", 16, 73, sw - 32, 22, 13, Mint);
        ChapterStrip(2, header + 7, sw - 4);
        Label(currentOrder.description, 2, narrativeY, sw - 4, lineY - narrativeY - 1, compact ? 14 : 15, Ink, TextAnchor.UpperLeft, wrap: true);
        Solid(0, lineY, sw, 1, Edge);
        Label("必要なもの・達成条件", 1, lineY + 1, sw - 3, 24, 13, Muted, bold: true);
        for (int i = 0; i < needs.Length; i++)
        {
            var need = needs[i];
            float sy = rowsY + i * rowStep;
            float rowHeight = rowStep - 4;
            bool complete = need.have >= need.need;
            Box(0, sy, sw, rowHeight, complete ? Mint : Page);
            if (complete) Symbol("seal", 8, sy + (rowHeight - 21) / 2, 21, Forest);
            else
            {
                Box(11, sy + (rowHeight - 12) / 2, 13, 12, Edge);
                Box(14, sy + (rowHeight - 6) / 2, 7, 6, Page);
            }
            Label(need.label, 39, sy, sw - 200, rowHeight, 15, Ink, bold: true);
            Label(need.consumes ? "納品" : "記録", sw - 155, sy, 43, rowHeight, 12, Muted, TextAnchor.MiddleCenter);
            Label($"{CompactCount(need.have)} / {need.need}", sw - 106, sy, 94, rowHeight, 15, Ink, TextAnchor.MiddleRight, bold: true);
        }
        GUI.EndScrollView();

        float fy = y + h - 49;
        if (Time.unscaledTime < receiptUntil && receipt != null)
        {
            Color sealColor = Color.Lerp(Mint, Gold, Mathf.Clamp01((receiptUntil - Time.unscaledTime - 2.4f) / 1.5f));
            Box(x + 16, fy - 1, w - 211, 37, sealColor);
            Symbol("seal", x + 23, fy + 6, 22, Forest);
            Label(receipt, x + 51, fy - 1, w - 250, 37, 14, Ink, bold: true);
        }
        else
        {
            Symbol("shell", x + 18, fy + 5, 26, Forest);
            Label($"報酬　貝貨 {state.OrderReward}", x + 51, fy, w - 248, 36, 17, Ink, bold: true);
        }
        if (Button(new Rect(x + w - 187, fy, 171, 36), ready ? "依頼を納品する" : "納品準備中", ready))
        {
            int reward = state.OrderReward;
            int chapter = state.CompletedChapters, trips = state.RepeatOrders;
            game.DeliverOrder();
            if (state.CompletedChapters > chapter || state.RepeatOrders > trips)
            {
                receipt = $"納品済　{currentOrder.title}　+{reward} 貝貨";
                receiptUntil = Time.unscaledTime + 4.2f;
                listScroll = Vector2.zero;
            }
        }
    }

    void ChapterStrip(float x, float y, float w)
    {
        int n = IslandEconomy.ChapterCount;
        float segment = (w - 4 * (n - 1)) / n;
        for (int i = 0; i < n; i++)
            Box(x + i * (segment + 4), y, segment, 6, i < game.State.CompletedChapters ? Forest : i == game.State.CompletedChapters ? Gold : Edge);
    }

    void Tools(float x, float y, float w, float h)
    {
        IslandState state = game.State;
        Tabs(x + 16, y + 78, w - 32, "道具の強化", "島の買取所", ref showMarket);
        Rect page = new Rect(x + 16, y + 121, w - 32, h - 178);
        bool compact = h < 460;
        float intro = compact ? 36 : 45;
        float step = compact ? 65 : showMarket ? 76 : 86;
        float sw = BeginPage(page, intro + step * 3);
        Label(showMarket ? "品物をまとめて貝貨に交換できます。" : "貝貨で道具を育てよう。各道具は2段階まで強化できます。", 0, 0, sw - 2, 34, 14, Muted);
        if (!showMarket)
        {
            for (int i = 0; i < ToolTypes.Length; i++)
            {
                IslandTool tool = ToolTypes[i];
                float sy = intro + i * step;
                int level = state.ToolLevel(tool), cost = state.UpgradeCost(tool);
                bool can = state.CanUpgrade(tool), maxed = cost == 0;
                string icon = tool == IslandTool.Gathering ? "hammer" : tool == IslandTool.Gardening ? "watering" : "crate";
                Box(0, sy, sw, step - 5, Page);
                Box(8, sy + (compact ? 6 : 12), 44, 48, Forest);
                Symbol(icon, 13, sy + (compact ? 13 : 19), 34, Paper);
                Label(IslandEconomy.ToolName(tool), 63, sy + 5, sw - 240, 26, 17, Ink, bold: true);
                Label($"Lv.{level} / 2", sw - 202, sy + 6, 72, 24, 13, Forest, TextAnchor.MiddleRight, bold: true);
                Label((maxed ? "効果　" : "強化後　") + IslandEconomy.ToolEffect(tool, maxed ? level : level + 1), 63, sy + (compact ? 29 : 37), sw - 201, compact ? 26 : 32, 13, Muted, wrap: true);
                if (Button(new Rect(sw - 119, sy + (compact ? 4 : 9), 110, compact ? 31 : 35), maxed ? "強化済" : "強化する", can)) game.BuyTool(tool);
                Label(maxed ? "最高段階" : $"{cost} 貝貨", sw - 119, sy + (compact ? 35 : 45), 110, 24, 13, Muted, TextAnchor.MiddleCenter);
            }
        }
        else
        {
            for (int i = 0; i < TradeGoods.Length; i++)
            {
                IslandGood good = TradeGoods[i];
                int amount = IslandEconomy.TradeQuantity(good), reward = state.TradeReward(good);
                int stock = good == IslandGood.Wood ? state.wood : good == IslandGood.Stone ? state.stone : state.Turnips;
                float sy = intro + i * step;
                Box(0, sy, sw, step - 6, Page);
                Symbol(good == IslandGood.Wood ? "wood" : good == IslandGood.Stone ? "stone" : "leaf", 11, sy + (compact ? 12 : 16), 34, Forest);
                Label(IslandEconomy.GoodName(good) + $"　{amount} 個", 58, sy + 5, sw - 200, 27, 17, Ink, bold: true);
                Label($"在庫 {CompactCount(stock)} 個　／　売値 {reward} 貝貨", 59, sy + 31, sw - 196, 27, 14, Muted);
                if (Button(new Rect(sw - 128, sy + (compact ? 10 : 15), 116, 37), "1組を売る", state.CanTrade(good))) game.SellGoods(good);
            }
        }
        GUI.EndScrollView();
        Symbol("shell", x + 18, y + h - 44, 26, Forest);
        Label($"貝貨　{state.Shells:N0}", x + 50, y + h - 52, 147, 42, 16, Ink, bold: true);
        Label(game.Notice ?? "", x + 200, y + h - 52, w - 216, 45, 13, Muted, TextAnchor.MiddleRight, wrap: true);
    }

    void Credits(float x, float y, float w, float h)
    {
        float sw = BeginPage(new Rect(x + 16, y + 78, w - 32, h - 142), 355);
        Box(0, 0, sw, 120, Forest);
        Box(sw * .5f - 21, 11, 42, 40, Gold);
        Symbol("island", sw * .5f - 16, 14, 32, Forest);
        Label("凪島の開港祭", 5, 53, sw - 10, 38, 27, Paper, TextAnchor.MiddleCenter, serif: true);
        Label($"{IslandEconomy.ChapterCount} 章の便りを、すべて届けました", 5, 92, sw - 10, 22, 13, Mint, TextAnchor.MiddleCenter);
        Label("桟橋に船が着き、広場にはひと休みできる場所ができました。開拓者のあなたに、島のみんなから感謝を込めて。", 11, 133, sw - 22, 57, 16, Ink, TextAnchor.UpperLeft, wrap: true);
        Solid(6, 199, sw - 12, 1, Edge);
        Label("便りをくれた人たち", 9, 205, sw - 18, 25, 14, Muted, bold: true);
        Label("渡し守のナギ　　浜の台所・ミオ　　古書船のユイ\n野鳥係のサヨ　　夜番のレン　　旅客船のエナ", 9, 237, sw - 18, 52, 15, Ink, TextAnchor.UpperLeft, wrap: true);
        Symbol("letter", 10, 312, 27, Forest);
        Label("港には新しい定期便が届いています。", 46, 308, sw - 55, 36, 15, Ink, bold: true);
        GUI.EndScrollView();
        if (Button(new Rect(x + 16, y + h - 52, 132, 38), "島に戻る", true, false)) game.ClosePanel();
        if (Button(new Rect(x + w - 211, y + h - 52, 195, 38), "港の定期便を開く", true)) game.Open("orders");
    }
}
