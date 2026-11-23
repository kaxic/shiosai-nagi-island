using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct ExhibitPlot
{
    public readonly Vector2 position, front;
    public readonly string furniture;

    public ExhibitPlot(Vector2 position, string furniture, Vector2 front)
    {
        this.position = position;
        this.furniture = furniture;
        this.front = front.normalized;
    }
}

public static class IslandExhibitions
{
    internal enum Rule { NextTo, Flank, Face, EmptyGap, Corner, Square }
    internal readonly struct Condition
    {
        internal readonly Rule rule;
        internal readonly int a, b, c, d;
        internal readonly string text;
        internal Condition(string text, Rule rule, int a, int b, int c = -1, int d = -1)
        { this.text = text; this.rule = rule; this.a = a; this.b = b; this.c = c; this.d = d; }
    }

    public sealed class Gallery
    {
        public readonly string name, seal, materials;
        public readonly int chapter;
        internal readonly string[] pieces;
        internal readonly Condition[] conditions;
        internal Gallery(string name, string seal, int chapter, string materials, string[] pieces, params Condition[] conditions)
        {
            this.name = name; this.seal = seal; this.chapter = chapter; this.materials = materials;
            this.pieces = pieces; this.conditions = conditions;
        }
        public int LineCount => conditions.Length + 1;
        public string Line(int i) => i == 0 ? materials : conditions[i - 1].text;
    }

    // Array order is the permanent stamp number. Append new shows so existing stamps keep their identity.
    static readonly Gallery[] Shows =
    {
        new Gallery("夕灯りの腰掛け", "灯", 0, "ベンチとランタンを1台ずつ飾る", new[] {"bench", "lantern"},
            new Condition("ベンチとランタンを縦か横の隣同士にする", Rule.NextTo, 0, 1)),
        new Gallery("花に挟まれた待合所", "花", 0, "ベンチ1台とプランター2鉢を飾る", new[] {"bench", "planter", "planter"},
            new Condition("ベンチの両隣に花を置き、3区画を一直線にする", Rule.Flank, 0, 1, 2)),
        new Gallery("向かい合う朝市", "市", 2, "テーブル1台とベンチ2台を飾る", new[] {"table", "bench", "bench"},
            new Condition("テーブルを挟んで、ベンチを両隣に置く", Rule.Flank, 0, 1, 2),
            new Condition("片方のベンチの正面をテーブルに向ける", Rule.Face, 1, 0),
            new Condition("もう片方のベンチもテーブルに向ける", Rule.Face, 2, 0)),
        new Gallery("風の読書室", "本", 3, "ベンチ、本棚、ランタンを1台ずつ飾る", new[] {"bench", "bookshelf", "lantern"},
            new Condition("ベンチと本棚を一直線に置き、間の1区画を空ける", Rule.EmptyGap, 0, 1),
            new Condition("ベンチの正面を本棚に向ける", Rule.Face, 0, 1),
            new Condition("ランタンをベンチの隣に置く", Rule.NextTo, 0, 2)),
        new Gallery("小鳥の花だより", "鳥", 4, "プランター、鳥の巣箱、ランタンを飾る", new[] {"planter", "birdhouse", "lantern"},
            new Condition("花を角にして、隣の巣箱とランタンでL字を作る", Rule.Corner, 0, 1, 2)),
        new Gallery("火を囲む夜会", "炉", 4, "火鉢、ベンチ、テーブル、ランタンを飾る", new[] {"brazier", "bench", "table", "lantern"},
            new Condition("4区画を四角く埋め、火鉢の対角にランタンを置く", Rule.Square, 0, 1, 2, 3),
            new Condition("ベンチの正面を火鉢に向ける", Rule.Face, 1, 0)),
        new Gallery("花門の歓迎式", "門", 5, "アーチ1基、プランター2鉢、ランタン1台を飾る", new[] {"arch", "planter", "planter", "lantern"},
            new Condition("アーチの両隣に花を置き、3区画を一直線にする", Rule.Flank, 0, 1, 2),
            new Condition("ランタンをアーチの隣に置く", Rule.NextTo, 0, 3),
            new Condition("アーチの正面をランタンに向ける", Rule.Face, 0, 3)),
        new Gallery("港の休日", "港", 6, "テーブル、デイベッド、ベンチ、本棚、鳥の巣箱を飾る", new[] {"table", "daybed", "bench", "bookshelf", "birdhouse"},
            new Condition("テーブルを角に、隣のデイベッドとベンチでL字を作る", Rule.Corner, 0, 1, 2),
            new Condition("本棚をベンチの隣に置く", Rule.NextTo, 2, 3),
            new Condition("ベンチの正面を本棚に向ける", Rule.Face, 2, 3),
            new Condition("デイベッドと巣箱を一直線に置き、間の1区画を空ける", Rule.EmptyGap, 1, 4))
    };

    public static int Count => Shows.Length;
    public static Gallery Get(int index) => Shows[index];

    public static List<ExhibitPlot> ReadPlan(IslandState state, IReadOnlyList<IslandSpot> spots)
    {
        var result = new List<ExhibitPlot>();
        foreach (var spot in spots)
        {
            if (spot.kind != IslandSpot.Kind.FurnitureSlot || (spot.Center.x>32f)!=state.passage) continue;
            var item = state.At(spot.id);
            // The seat and shelf both face local −Z, opposite the bench backrest and the shelf back.
            Vector3 front = spot.transform.rotation * Quaternion.Euler(0, 90 * (item?.turns ?? 0), 0) * Vector3.back;
            result.Add(new ExhibitPlot(spot.Center, item?.recipeId, new Vector2(front.x, front.z)));
        }
        return result;
    }

    // Try matching duplicate furniture in either order. Every checked line refers to the same arrangement.
    public static bool[] Evaluate(int index, IReadOnlyList<ExhibitPlot> plots)
    {
        var show = Get(index);
        var best = new bool[show.LineCount];
        var checks = new bool[best.Length];
        var assignment = new int[show.pieces.Length];
        var used = new bool[plots.Count];
        float pitch = GridSpacing(plots);
        int bestScore = -1;
        void Match(int role)
        {
            if (bestScore == best.Length) return;
            if (role < assignment.Length)
            {
                for (int i = 0; i < plots.Count; i++)
                {
                    if (used[i] || plots[i].furniture != show.pieces[role]) continue;
                    assignment[role] = i;
                    used[i] = true; Match(role + 1); used[i] = false;
                }
                assignment[role] = -1;
                Match(role + 1);
                return;
            }
            checks[0] = Array.IndexOf(assignment, -1) < 0;
            int score = checks[0] ? 1 : 0;
            for (int i = 0; i < show.conditions.Length; i++)
            {
                checks[i + 1] = Satisfies(show.conditions[i], plots, assignment, pitch);
                if (checks[i + 1]) score++;
            }
            if (score <= bestScore) return;
            bestScore = score;
            Array.Copy(checks, best, checks.Length);
        }
        Match(0);
        return best;
    }

    static float GridSpacing(IReadOnlyList<ExhibitPlot> plots)
    {
        var nearest = new List<float>(plots.Count);
        for (int i = 0; i < plots.Count; i++)
        {
            float distance = float.PositiveInfinity;
            for (int j = 0; j < plots.Count; j++)
            {
                float gap = Vector2.Distance(plots[i].position, plots[j].position);
                if (gap > .001f && gap < distance) distance = gap;
            }
            if (!float.IsInfinity(distance)) nearest.Add(distance);
        }
        if (nearest.Count == 0) return 0;
        nearest.Sort();
        return nearest[nearest.Count / 2];
    }

    static bool Satisfies(Condition rule, IReadOnlyList<ExhibitPlot> plots, int[] roles, float pitch)
    {
        if (roles[rule.a] < 0 || roles[rule.b] < 0 ||
            (rule.c >= 0 && roles[rule.c] < 0) || (rule.d >= 0 && roles[rule.d] < 0) || pitch <= 0) return false;
        var a = plots[roles[rule.a]];
        Vector2 ab = plots[roles[rule.b]].position - a.position;
        bool Next(Vector2 offset) => offset.sqrMagnitude > .01f * pitch * pitch && offset.magnitude <= 1.22f * pitch;
        if (rule.rule == Rule.NextTo) return Next(ab);
        if (rule.rule == Rule.Face) return Vector2.Dot(a.front, ab.normalized) >= .9f;
        if (rule.rule == Rule.EmptyGap)
        {
            Vector2 middle = a.position + ab / 2;
            if (ab.magnitude < 1.75f * pitch || ab.magnitude > 2.38f * pitch) return false;
            foreach (var p in plots)
                if (p.furniture == null && Vector2.Distance(p.position, middle) <= .18f * pitch) return true;
            return false;
        }
        Vector2 ac = plots[roles[rule.c]].position - a.position;
        if (!Next(ab) || !Next(ac)) return false;
        float dot = Vector2.Dot(ab.normalized, ac.normalized);
        if (rule.rule == Rule.Flank) return dot <= -.94f;
        if (Mathf.Abs(dot) > .18f) return false;
        if (rule.rule == Rule.Corner) return true;
        return Vector2.Distance(plots[roles[rule.d]].position, a.position + ab + ac) <= .2f * pitch;
    }
}

public partial class IslandState
{
    [SerializeField] private int exhibitionStamps;
    public bool HasExhibitionStamp(int index) => index >= 0 && index < IslandExhibitions.Count &&
        (exhibitionStamps & (1 << index)) != 0;
    public int ExhibitionCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < IslandExhibitions.Count; i++) if (HasExhibitionStamp(i)) count++;
            return count;
        }
    }
    public bool TryExhibit(int index, IReadOnlyList<ExhibitPlot> plots)
    {
        if (index < 0 || index >= IslandExhibitions.Count || plots == null || HasExhibitionStamp(index)) return false;
        if (Array.IndexOf(IslandExhibitions.Evaluate(index, plots), false) >= 0) return false;
        exhibitionStamps |= 1 << index;
        return true;
    }
}

public sealed partial class IslandGame
{
    public List<ExhibitPlot> GardenPlan() => IslandExhibitions.ReadPlan(State, spots);
    public bool SubmitExhibition(int index)
    {
        if (Panel != "exhibit" || !State.TryExhibit(index, GardenPlan())) return false;
        ActionDone("庭展の出品印「" + IslandExhibitions.Get(index).seal + "」を押しました", 3);
        return true;
    }
}
