using System;

public enum IslandTool { Gathering, Gardening, Trading }
public enum IslandGood { Wood, Stone, Turnip }

public readonly struct IslandRequirement
{
    public readonly string label;
    public readonly int have, need;
    public readonly bool consumes;

    internal IslandRequirement(string label, int have, int need, bool consumes)
    {
        this.label = label;
        this.have = have;
        this.need = need;
        this.consumes = consumes;
    }
}

internal enum OrderCondition { Wood, Stone, Turnips, Furniture, Placed, Harvests, PeakKinds, ToolLevels }

internal readonly struct OrderItem
{
    public readonly OrderCondition condition;
    public readonly string label, recipeId;
    public readonly int count;
    public bool Consumed => condition == OrderCondition.Wood || condition == OrderCondition.Stone ||
        condition == OrderCondition.Turnips || condition == OrderCondition.Furniture;

    public OrderItem(OrderCondition condition, string label, int count, string recipeId = null)
    {
        this.condition = condition;
        this.label = label;
        this.count = count;
        this.recipeId = recipeId;
    }
}

public sealed class IslandOrder
{
    public readonly string title, client, description;
    public readonly int reward;
    internal readonly OrderItem[] items;

    internal IslandOrder(string title, string client, string description, int reward, params OrderItem[] items)
    {
        this.title = title;
        this.client = client;
        this.description = description;
        this.reward = reward;
        this.items = items;
    }
}

public static class IslandEconomy
{
    static OrderItem Wood(int n) => new OrderItem(OrderCondition.Wood, "木材", n);
    static OrderItem Stone(int n) => new OrderItem(OrderCondition.Stone, "石", n);
    static OrderItem Turnips(int n) => new OrderItem(OrderCondition.Turnips, "かぶ", n);
    static OrderItem Furniture(string id, string name) => new OrderItem(OrderCondition.Furniture, name, 1, id);
    static OrderItem Harvests(int n) => new OrderItem(OrderCondition.Harvests, "畑の累計収穫回数", n);
    static OrderItem Peak(int n) => new OrderItem(OrderCondition.PeakKinds, "同時に飾った家具の最多種類", n);

    // Units of furniture for delivery come from the bag. Placed furniture is displayed separately as a milestone.
    static readonly IslandOrder[] Chapters =
    {
        new IslandOrder("船着き場の修繕", "渡し守のナギ",
            "初めての便が水路を渡ってきた。木と岩から材料を集めて、桟橋のぐらついた板を打ち直そう。", 5,
            Wood(3), Stone(2)),
        new IslandOrder("潮待ちの腰掛け", "渡し守のナギ",
            "次の船を待つおばあさんに、ひと休みする場所を。作業台でベンチを作り、家具袋に入れて届けよう。", 6,
            Furniture("bench", "海辺のベンチ")),
        new IslandOrder("浜辺の朝ごはん", "浜の台所・ミオ",
            "潮風の中で朝市を開きたい。新しく作れるテーブルと、畑で育てたかぶを最初のお客さんに届けよう。", 8,
            Furniture("table", "テーブル"), Turnips(2)),
        new IslandOrder("島の小さな本屋", "古書船のユイ",
            "船に眠っていた本を並べる棚が必要だ。広場に家具を二種類飾ったら、本棚を家具袋に入れて届けよう。", 9,
            Furniture("bookshelf", "本棚"), Peak(2)),
        new IslandOrder("渡り鳥の休み宿", "野鳥係のサヨ",
            "畑に春の小鳥が帰ってきた。収穫を二度経験したら、巣箱と畑のかぶを持ち寄ろう。", 10,
            Furniture("birdhouse", "鳥の巣箱"), Turnips(2), Harvests(2)),
        new IslandOrder("灯を囲む広場", "夜番のレン",
            "夜の寄り合いに火をともそう。家具三種類を同時に飾った記録と、火鉢、炉を支える石を用意しよう。", 11,
            Furniture("brazier", "火鉢"), Stone(3), Peak(3)),
        new IslandOrder("昼寝のできる港", "旅客船のエナ",
            "長旅の乗客が島で休めるように。広場にはアーチを飾り、船にはデイベッドを一台届けよう。", 12,
            new OrderItem(OrderCondition.Placed, "広場に飾っているアーチ", 1, "arch"), Furniture("daybed", "デイベッド")),
        new IslandOrder("凪島の開港祭", "島のみんな",
            "初めての開港祭。広場に四種類を並べた記録を残し、道具を合計二段階育て、お祝いのかぶを届けよう。", 18,
            Turnips(2), Peak(4), Harvests(3), new OrderItem(OrderCondition.ToolLevels, "道具の合計強化段階", 2))
    };

    static readonly IslandOrder[] ReturnTrips =
    {
        new IslandOrder("夜市の明かり", "港の市場組合", "宵の便にランタンを積もう。補修用の木材も一緒に。", 8,
            Furniture("lantern", "浜のランタン"), Wood(2)),
        new IslandOrder("花咲く食堂", "浜の台所・ミオ", "食堂の窓辺を飾る花と、今日のスープに使うかぶを届けよう。", 9,
            Furniture("planter", "花のプランター"), Turnips(2)),
        new IslandOrder("船上図書室", "古書船のユイ", "新しい図書船に机と本棚を一つずつ届けよう。", 13,
            Furniture("bookshelf", "本棚"), Furniture("table", "テーブル")),
        new IslandOrder("船員の昼休み", "渡し守のナギ", "潮待ちの船員に寝床と、舟をつなぐ重石を届けよう。", 10,
            Furniture("daybed", "デイベッド"), Stone(2)),
        new IslandOrder("小鳥の島便り", "野鳥係のサヨ", "今年生まれた鳥たちに、もう一つ巣箱を用意しよう。", 9,
            Furniture("birdhouse", "鳥の巣箱"), Wood(3)),
        new IslandOrder("冬を越す火", "夜番のレン", "冷たい風をしのげるよう、火鉢と炉石を次の船に積もう。", 10,
            Furniture("brazier", "火鉢"), Stone(3))
    };

    public static int ChapterCount => Chapters.Length;
    public static IslandOrder GetChapter(int zeroBased) => Chapters[zeroBased];
    internal static IslandOrder GetReturnTrip(int completed) => ReturnTrips[completed % ReturnTrips.Length];

    public static string ToolName(IslandTool tool) => tool switch
    {
        IslandTool.Gathering => "採集道具", IslandTool.Gardening => "畑のじょうろ",
        IslandTool.Trading => "船大工の荷箱", _ => ""
    };

    public static string ToolEffect(IslandTool tool, int level) => tool switch
    {
        IslandTool.Gathering => level == 0 ? "採集量は基本値" : $"採集ごとに素材 +{level}",
        IslandTool.Gardening => level == 0 ? "収穫はかぶ2個" : $"成長時間 −{level * 20}%、収穫はかぶ{2 + level}個",
        IslandTool.Trading => level == 0 ? "依頼と交易の基本報酬" : $"依頼ごとに貝貨 +{2 * level}、交易ごとに +{level}",
        _ => ""
    };

    public static string GoodName(IslandGood good) => good switch
    {
        IslandGood.Wood => "木材", IslandGood.Stone => "石", IslandGood.Turnip => "かぶ", _ => ""
    };

    public static int TradeQuantity(IslandGood good) => good switch
    {
        IslandGood.Wood => 3, IslandGood.Stone => 3, IslandGood.Turnip => 2, _ => 0
    };
}
