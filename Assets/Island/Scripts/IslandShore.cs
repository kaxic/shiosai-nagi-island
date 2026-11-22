using UnityEngine;

public partial class IslandState
{
    [SerializeField] int specimenBits;
    public bool HasSpecimen(int index) => index >= 0 && index < 9 && (specimenBits & (1 << index)) != 0;
    public bool RecordSpecimen(int index)
    {
        if (index < 0 || index >= 9 || HasSpecimen(index)) return false;
        specimenBits |= 1 << index;
        return true;
    }
    public int SpecimenCount
    {
        get { int n = 0; for (int i = 0; i < 9; i++) if (HasSpecimen(i)) n++; return n; }
    }
}

public sealed partial class IslandGame
{
    public static readonly string[] ShoreNames = {"桟橋の入り江","北の磯","東の砂州"};
    public static readonly string[] SpecimenNames = {"桜色のホタテ","琥珀の巻き貝","すみれタカラガイ",
        "白磁の二枚貝","星形のカシパン","紅枝サンゴ","波磨きのガラス","月縞オウムガイ","瑠璃の巻き貝"};
    public static readonly string[] SpecimenNotes = {
        "扇のような殻に、夕焼けを写したような薄紅色が差している。",
        "渦の奥には砂粒が二つ。陽にかざすと飴色の稜が光る。",
        "小さな口を裏に隠した、指先に収まる紫色の宝物。",
        "潮が引いたあと、まっさらな砂の上に並んでいた。",
        "花のような五つの模様は、小さなウニが残した星。",
        "長い旅で角が取れたサンゴのかけら。枝先だけがほのかに白い。",
        "かつて何色の瓶だったのだろう。海がその表面を曇らせた。",
        "曲線に沿って交互に走る縞。耳を寄せると潮騒が聞こえる。",
        "深い青の渦に金色の筋が一本。島の浜辺図鑑を飾る一品。"};
    float cameraZoom = 1, tideStart;
    public int Shore { get; private set; }
    public int LastSample { get; private set; } = -1;
    public string SampleMessage { get; private set; } = "";
    public float TideLevel => Mathf.PingPong((Time.unscaledTime - tideStart) * 0.43f, 1);
    public bool MusicOn => PlayerPrefs.GetInt("nagi-music",1) != 0;

    void InitializeShore()
    {
        gameObject.AddComponent<IslandFolio>().game = this;
        gameObject.AddComponent<IslandPhoto>().game = this;
        gameObject.AddComponent<IslandExhibitionHud>().game = this;
        gameObject.AddComponent<IslandScore>().SetEnabled(MusicOn);
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(EditorTestSavePath)) return;
#endif
        Open("title");
    }
    public void ToggleMusic()
    {
        bool on = !MusicOn;
        PlayerPrefs.SetInt("nagi-music", on ? 1 : 0); PlayerPrefs.Save();
        GetComponent<IslandScore>().SetEnabled(on);
    }
    void StartTide(int shore)
    {
        Shore = Mathf.Clamp(shore,0,2); tideStart = Time.unscaledTime; LastSample = -1;
        SampleMessage = "波の印が色の帯と重なる瞬間に拾おう。帯ごとに違うものが見つかります。";
        Open("tide");
    }
    public void TakeSample()
    {
        if (Panel != "tide") return;
        float level = TideLevel;
        int band = Mathf.Clamp(Mathf.FloorToInt(level * 3), 0, 2);
        float width = 0.115f - 0.022f * band;
        if (Mathf.Abs(level - (band + .5f) / 3f) > width)
        { SampleMessage = "波がまだ高いようです。色の帯を狙って、もう一度。"; return; }
        LastSample = Shore * 3 + band;
        bool fresh = State.RecordSpecimen(LastSample);
        SampleMessage = (fresh ? "新発見　" : "観察　") + SpecimenNames[LastSample];
        if (fresh && State.SpecimenCount == 9) SampleMessage = "浜辺図鑑が完成しました！";
        if (fresh) ActionDone(SampleMessage, 3); else sounds.Play(1);
    }
    public void DeliverOrder()
    {
        if (Panel != "orders") return;
        bool postgame = State.CampaignComplete;
        int pay = State.OrderReward;
        if (!State.TryDeliverOrder()) { Say("品物と達成条件を確認してください"); return; }
        ActionDone($"便を出しました　貝貨 +{pay}", 3);
        if (!postgame && State.CampaignComplete) Open("credits");
    }
    public void BuyTool(IslandTool tool)
    {
        if (Panel != "tools" || !State.TryUpgrade(tool)) return;
        ActionDone("道具を強化しました", 2);
    }
    public void SellGoods(IslandGood good)
    {
        if (Panel != "tools" || !State.TryTrade(good)) return;
        ActionDone("品物を出荷しました", 0);
    }
}
