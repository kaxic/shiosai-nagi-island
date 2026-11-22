using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlacedFurniture
{
    public string slotId;
    public string recipeId;
    public int turns;
}

[Serializable]
public partial class IslandState
{
    [Serializable]
    private class InventoryItem
    {
        public string recipeId;
        public int count;
    }

    [Serializable]
    private class GatheredNode
    {
        public string nodeId;
        public long readyAt;
    }

    public int wood, stone, harvests;
    public float playerX, playerZ = -0.4f, playerYaw;
    public List<PlacedFurniture> placed = new List<PlacedFurniture>();

    [SerializeField] private List<InventoryItem> inventory = new List<InventoryItem>();
    [SerializeField] private List<GatheredNode> gathered = new List<GatheredNode>();
    [SerializeField] private long cropReadyAt;
    [SerializeField] private bool goalReached;

    public bool GoalReached => goalReached;
    public bool IsSaveReadOnly { get; internal set; }

    public int PlacedTypeCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                bool first = true;
                for (int j = 0; j < i; j++)
                {
                    if (placed[j].recipeId != placed[i].recipeId) continue;
                    first = false;
                    break;
                }
                if (first) count++;
            }
            return count;
        }
    }

    public int Count(string recipeId) => FindItem(recipeId)?.count ?? 0;

    public PlacedFurniture At(string slotId)
    {
        foreach (var piece in placed)
            if (piece.slotId == slotId) return piece;
        return null;
    }

    public int ResourceWait(string nodeId, long now)
    {
        var node = FindNode(nodeId);
        return node == null ? 0 : SecondsUntil(node.readyAt, now);
    }

    public bool TryGather(string nodeId, bool givesWood, int amount, int cooldownSeconds, long now)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || amount <= 0 || cooldownSeconds <= 0 ||
            now < 0 || now > long.MaxValue - cooldownSeconds) return false;

        var node = FindNode(nodeId);
        if (node != null && node.readyAt > now) return false;
        int awarded = GatherYield(amount);
        if ((givesWood ? wood : stone) > int.MaxValue - awarded) return false;

        if (node == null)
        {
            node = new GatheredNode { nodeId = nodeId };
            gathered.Add(node);
        }
        node.readyAt = now + cooldownSeconds;
        if (givesWood) wood += awarded;
        else stone += awarded;
        return true;
    }

    public bool TryCraft(FurnitureRecipe recipe)
    {
        if (!CanMakeRecipe(recipe) || string.IsNullOrWhiteSpace(recipe.id) ||
            recipe.woodCost < 0 || recipe.stoneCost < 0 ||
            wood < recipe.woodCost || stone < recipe.stoneCost) return false;
        if (!AddItem(recipe.id)) return false;
        wood -= recipe.woodCost;
        stone -= recipe.stoneCost;
        return true;
    }

    public bool TryPlace(string slotId, string recipeId, int turns)
    {
        if (string.IsNullOrWhiteSpace(slotId) || At(slotId) != null) return false;
        var item = FindItem(recipeId);
        if (item == null || item.count <= 0) return false;
        placed.Add(new PlacedFurniture { slotId = slotId, recipeId = recipeId, turns = (turns % 4 + 4) % 4 });
        item.count--;
        peakPlacedKinds = Math.Max(peakPlacedKinds, PlacedTypeCount);
        UpdateGoal();
        return true;
    }

    public bool TryRemove(string slotId)
    {
        var piece = At(slotId);
        if (piece == null || !AddItem(piece.recipeId)) return false;
        placed.Remove(piece);
        return true;
    }

    public int FarmWait(long now) => cropReadyAt == 0 ? -1 : SecondsUntil(cropReadyAt, now);

    public bool TryPlant(int growSeconds, long now)
    {
        if (cropReadyAt != 0 || growSeconds <= 0 || now < 0) return false;
        int duration = GrowDuration(growSeconds);
        if (now > long.MaxValue - duration) return false;
        cropReadyAt = now + duration;
        return true;
    }

    public bool TryHarvest(long now)
    {
        if (FarmWait(now) != 0 || harvests == int.MaxValue || turnips > int.MaxValue - HarvestYield) return false;
        harvests++;
        turnips += HarvestYield;
        cropReadyAt = 0;
        UpdateGoal();
        return true;
    }

    private InventoryItem FindItem(string id)
    {
        foreach (var item in inventory)
            if (item.recipeId == id) return item;
        return null;
    }

    private bool AddItem(string id)
    {
        var item = FindItem(id);
        if (item == null)
        {
            inventory.Add(new InventoryItem { recipeId = id, count = 1 });
            return true;
        }
        if (item.count == int.MaxValue) return false;
        item.count++;
        return true;
    }

    private GatheredNode FindNode(string id)
    {
        foreach (var node in gathered)
            if (node.nodeId == id) return node;
        return null;
    }

    private static int SecondsUntil(long readyAt, long now)
    {
        long seconds = readyAt - Math.Max(0L, now);
        return (int)Math.Max(0L, Math.Min(seconds, int.MaxValue));
    }

    private void UpdateGoal()
    {
        if (!goalReached && harvests > 0 && PlacedTypeCount >= 3) goalReached = true;
    }

    // Validate IDs without consulting today's catalogue. A temporarily missing prefab can return later.
    internal bool IsValid()
    {
        if (wood < 0 || stone < 0 || harvests < 0 || cropReadyAt < 0 || !CampaignValuesValid() ||
            !IsFinite(playerX) || !IsFinite(playerZ) || !IsFinite(playerYaw) ||
            placed == null || inventory == null || gathered == null) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var piece in placed)
        {
            if (piece == null || string.IsNullOrWhiteSpace(piece.slotId) ||
                string.IsNullOrWhiteSpace(piece.recipeId) || !ids.Add(piece.slotId) ||
                piece.turns < 0 || piece.turns > 3) return false;
        }
        ids.Clear();
        foreach (var item in inventory)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.recipeId) ||
                item.count < 0 || !ids.Add(item.recipeId)) return false;
        }
        ids.Clear();
        foreach (var node in gathered)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId) ||
                node.readyAt < 0 || !ids.Add(node.nodeId)) return false;
        }
        return true;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

public static class IslandSave
{
    [Serializable]
    private class SaveFile
    {
        public int version;
        public IslandState state;
    }

    private const int CurrentVersion = 2;
    private enum FileStatus { Missing, Valid, Damaged, Newer }
    private static readonly object Sync = new object();

    public static IslandState Load(string path)
    {
        lock (Sync)
        {
            var status = Read(path, out var state);
            if (status == FileStatus.Valid) return state;
            if (status != FileStatus.Newer)
            {
                status = Read(path + ".bak", out state);
                if (status == FileStatus.Valid) return state;
            }
            return new IslandState { IsSaveReadOnly = status == FileStatus.Newer };
        }
    }

    public static bool Write(string path, IslandState state)
    {
        if (state == null || state.IsSaveReadOnly || !state.IsValid() || string.IsNullOrWhiteSpace(path))
            return false;
        lock (Sync)
        {
            string temp = null;
            try
            {
                path = Path.GetFullPath(path);
                var status = Read(path, out _);
                if (status == FileStatus.Newer) return false;
                string backup = path + ".bak";
                if (status != FileStatus.Valid && Read(backup, out _) == FileStatus.Newer) return false;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                var data = new SaveFile { version = CurrentVersion, state = state };
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (status == FileStatus.Missing) File.Move(temp, path);
                else
                {
                    // Do not rotate a corrupt primary over the last usable backup.
                    BrowserTools.ReplaceFile(temp, path, status == FileStatus.Valid ? backup : null);
                }
                BrowserTools.Saved();
                return true;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                Debug.LogWarning("島の保存に失敗しました: " + ex.Message);
                return false;
            }
            finally
            {
                try { if (temp != null && File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static FileStatus Read(string path, out IslandState state)
    {
        state = null;
        if (!File.Exists(path)) return FileStatus.Missing;
        try
        {
            var data = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            if (data != null && data.version > CurrentVersion) return FileStatus.Newer;
            if (data == null || data.version < 1 || data.state == null || !data.state.IsValid())
                return FileStatus.Damaged;
            state = data.state;
            if (data.version == 1) state.ImportOriginalIsland();
            return FileStatus.Valid;
        }
        catch (Exception ex) when (IsFileError(ex))
        {
            return FileStatus.Damaged;
        }
    }

    private static bool IsFileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
        ex is ArgumentException || ex is NotSupportedException || ex is System.Security.SecurityException;
}
