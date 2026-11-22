using System;
using UnityEngine;

public partial class IslandState
{
    [SerializeField] private int shells, turnips, completedChapters, repeatOrders, peakPlacedKinds;
    [SerializeField] private int gatheringLevel, gardeningLevel, tradingLevel;

    public int Shells => shells;
    public int Turnips => turnips;
    public int CompletedChapters => completedChapters;
    public int RepeatOrders => repeatOrders;
    public int PeakPlacedKinds => peakPlacedKinds;
    public int DiscoveredKinds => inventory.Count;
    public bool CampaignComplete => completedChapters >= IslandEconomy.ChapterCount;
    public int Rank => 1 + Math.Min(4, completedChapters / 2);
    public string RankName => Rank switch
    {
        1 => "漂着した旅人", 2 => "浜の手伝い人", 3 => "島の職人", 4 => "港の世話役", _ => "凪島の開拓者"
    };
    public IslandOrder CurrentOrder => CampaignComplete ? IslandEconomy.GetReturnTrip(repeatOrders) : IslandEconomy.GetChapter(completedChapters);
    public int OrderReward => CurrentOrder.reward + 2 * tradingLevel;
    public int HarvestYield => 2 + gardeningLevel;

    public int GatherYield(int baseAmount) => (int)Math.Min(int.MaxValue, (long)Math.Max(0, baseAmount) + gatheringLevel);
    public int GrowDuration(int baseSeconds) => (int)Math.Max(1L, ((long)baseSeconds * (100 - 20 * gardeningLevel) + 99) / 100);
    public bool CanMakeRecipe(FurnitureRecipe recipe) => recipe != null && completedChapters >= recipe.unlockAfterChapters;

    public int ToolLevel(IslandTool tool) => tool switch
    {
        IslandTool.Gathering => gatheringLevel, IslandTool.Gardening => gardeningLevel,
        IslandTool.Trading => tradingLevel, _ => 0
    };

    public int UpgradeCost(IslandTool tool)
    {
        if (tool < IslandTool.Gathering || tool > IslandTool.Trading) return 0;
        int level = ToolLevel(tool);
        if (level >= 2) return 0;
        return (level == 0 ? 5 : 12) + (tool == IslandTool.Trading ? 2 : 0);
    }

    public bool CanUpgrade(IslandTool tool)
    {
        int cost = UpgradeCost(tool);
        return cost > 0 && shells >= cost;
    }

    public bool TryUpgrade(IslandTool tool)
    {
        if (!CanUpgrade(tool)) return false;
        shells -= UpgradeCost(tool);
        switch (tool)
        {
            case IslandTool.Gathering: gatheringLevel++; break;
            case IslandTool.Gardening: gardeningLevel++; break;
            case IslandTool.Trading: tradingLevel++; break;
        }
        return true;
    }

    public int TradeReward(IslandGood good) => good switch
    {
        IslandGood.Wood => 2 + tradingLevel, IslandGood.Stone => 2 + tradingLevel,
        IslandGood.Turnip => 3 + tradingLevel, _ => 0
    };

    public bool CanTrade(IslandGood good)
    {
        int quantity = IslandEconomy.TradeQuantity(good);
        int stock = good switch { IslandGood.Wood => wood, IslandGood.Stone => stone, IslandGood.Turnip => turnips, _ => 0 };
        return quantity > 0 && stock >= quantity && shells <= int.MaxValue - TradeReward(good);
    }

    public bool TryTrade(IslandGood good)
    {
        if (!CanTrade(good)) return false;
        int quantity = IslandEconomy.TradeQuantity(good);
        switch (good)
        {
            case IslandGood.Wood: wood -= quantity; break;
            case IslandGood.Stone: stone -= quantity; break;
            case IslandGood.Turnip: turnips -= quantity; break;
        }
        shells += TradeReward(good);
        return true;
    }

    public IslandRequirement[] OrderRequirements()
    {
        var items = CurrentOrder.items;
        var rows = new IslandRequirement[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            rows[i] = new IslandRequirement(item.label, Available(item), item.count, item.Consumed);
        }
        return rows;
    }

    private int Available(OrderItem item)
    {
        switch (item.condition)
        {
            case OrderCondition.Wood: return wood;
            case OrderCondition.Stone: return stone;
            case OrderCondition.Turnips: return turnips;
            case OrderCondition.Furniture: return Count(item.recipeId);
            case OrderCondition.Placed:
                int onIsland = 0;
                foreach (var piece in placed) if (piece.recipeId == item.recipeId) onIsland++;
                return onIsland;
            case OrderCondition.Harvests: return harvests;
            case OrderCondition.PeakKinds: return peakPlacedKinds;
            case OrderCondition.ToolLevels: return gatheringLevel + gardeningLevel + tradingLevel;
            default: throw new InvalidOperationException("未対応の依頼条件: " + item.condition);
        }
    }

    public bool CanDeliverOrder
    {
        get
        {
            if (shells > int.MaxValue - OrderReward || (CampaignComplete && repeatOrders == int.MaxValue)) return false;
            foreach (var item in CurrentOrder.items)
                if (Available(item) < item.count) return false;
            return true;
        }
    }

    public bool TryDeliverOrder()
    {
        if (!CanDeliverOrder) return false;
        foreach (var item in CurrentOrder.items)
        {
            switch (item.condition)
            {
                case OrderCondition.Wood: wood -= item.count; break;
                case OrderCondition.Stone: stone -= item.count; break;
                case OrderCondition.Turnips: turnips -= item.count; break;
                case OrderCondition.Furniture: FindItem(item.recipeId).count -= item.count; break;
            }
        }
        shells += OrderReward;
        if (CampaignComplete) repeatOrders++;
        else completedChapters++;
        return true;
    }

    internal void ImportOriginalIsland()
    {
        // The first release counted harvests but did not have sellable produce.
        turnips = (int)Math.Min(int.MaxValue, (long)harvests * 2);
        peakPlacedKinds = Math.Max(PlacedTypeCount, GoalReached ? 3 : 0);
    }

    private bool CampaignValuesValid() => shells >= 0 && turnips >= 0 && peakPlacedKinds >= 0 &&
        completedChapters >= 0 && completedChapters <= IslandEconomy.ChapterCount && repeatOrders >= 0 &&
        gatheringLevel >= 0 && gatheringLevel <= 2 && gardeningLevel >= 0 && gardeningLevel <= 2 &&
        tradingLevel >= 0 && tradingLevel <= 2;
}
