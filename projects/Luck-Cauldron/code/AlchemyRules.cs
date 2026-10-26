// CLASS EXTRACTION: original class bodies; only imports, namespace and source comments added.
// Source: Assets/Scripts/Slot/SlotMachineController.cs
// Dependencies: UnityEngine and System.Linq; extracted enums and RecipeResult are included.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuckyCauldron
{
// Source lines 11-42
    public enum MaterialSymbol
    {
        Herb,
        Mushroom,
        IronOre,
        Gunpowder,
        Crystal,
        GoldDust
    }

    public enum ProductId
    {
        Sludge,
        Antidote,
        HealingPotion,
        Bomb,
        MegaBomb,
        PureEssence,
        MagicGem,
        CoinBag,
        GrandCrystal,
        PhilosopherCoin
    }

    public enum WinTier
    {
        None,
        Normal,
        Good,
        Big,
        Jackpot
    }

// Source lines 66-72
    public sealed class RecipeResult
    {
        public ProductId Product;
        public string DisplayName;
        public int BasePrice;
        public WinTier Tier;
    }

// Source lines 81-125
    public sealed class ProbabilityTableService
    {
        private static readonly Dictionary<MaterialSymbol, float> BaseWeights = new()
        {
            { MaterialSymbol.Herb, 24f },
            { MaterialSymbol.Mushroom, 24f },
            { MaterialSymbol.IronOre, 22f },
            { MaterialSymbol.Gunpowder, 13f },
            { MaterialSymbol.Crystal, 12f },
            { MaterialSymbol.GoldDust, 5f }
        };

        public Dictionary<MaterialSymbol, float> BuildTable(int crystalLevel, int goldDustLevel)
        {
            var crystal = 12f + crystalLevel * 4f;
            var gold = 5f + goldDustLevel * 2f;
            var remaining = Mathf.Max(1f, 100f - crystal - gold);
            var otherBase = BaseWeights
                .Where(kv => kv.Key != MaterialSymbol.Crystal && kv.Key != MaterialSymbol.GoldDust)
                .Sum(kv => kv.Value);

            var table = new Dictionary<MaterialSymbol, float>();
            foreach (var kv in BaseWeights)
            {
                if (kv.Key == MaterialSymbol.Crystal) table[kv.Key] = crystal;
                else if (kv.Key == MaterialSymbol.GoldDust) table[kv.Key] = gold;
                else table[kv.Key] = kv.Value / otherBase * remaining;
            }

            return table;
        }

        public MaterialSymbol Roll(Dictionary<MaterialSymbol, float> table)
        {
            var total = table.Values.Sum();
            var cursor = UnityEngine.Random.Range(0f, total);
            foreach (var kv in table)
            {
                cursor -= kv.Value;
                if (cursor <= 0f) return kv.Key;
            }

            return MaterialSymbol.Herb;
        }
    }

// Source lines 294-317
    public sealed class WinLineChecker
    {
        public RecipeResult Evaluate(MaterialSymbol[] s)
        {
            var c = s.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            bool Has(MaterialSymbol id, int count) => c.TryGetValue(id, out var value) && value >= count;

            if (Has(MaterialSymbol.GoldDust, 3)) return Recipe(ProductId.PhilosopherCoin, "Philosopher Coin", 60, WinTier.Jackpot);
            if (Has(MaterialSymbol.Crystal, 3)) return Recipe(ProductId.GrandCrystal, "Grand Crystal", 26, WinTier.Big);
            if (Has(MaterialSymbol.Gunpowder, 2) && Has(MaterialSymbol.IronOre, 1)) return Recipe(ProductId.MegaBomb, "Mega Bomb", 15, WinTier.Good);
            if (Has(MaterialSymbol.Crystal, 2) && Has(MaterialSymbol.GoldDust, 1)) return Recipe(ProductId.MagicGem, "Magic Gem", 18, WinTier.Big);
            if (Has(MaterialSymbol.GoldDust, 2)) return Recipe(ProductId.CoinBag, "Coin Bag", 22, WinTier.Big);
            if (Has(MaterialSymbol.Gunpowder, 1) && Has(MaterialSymbol.IronOre, 2)) return Recipe(ProductId.Bomb, "Bomb", 9, WinTier.Good);
            if (Has(MaterialSymbol.Herb, 1) && Has(MaterialSymbol.Mushroom, 1) && Has(MaterialSymbol.Crystal, 1)) return Recipe(ProductId.HealingPotion, "Healing Potion", 7, WinTier.Good);
            if (Has(MaterialSymbol.Herb, 2) && Has(MaterialSymbol.Mushroom, 1)) return Recipe(ProductId.Antidote, "Antidote", 4, WinTier.Normal);
            if (c.Values.Any(v => v == 3)) return Recipe(ProductId.PureEssence, "Pure Essence", 12, WinTier.Good);
            return Recipe(ProductId.Sludge, "Sludge", 1, WinTier.None);
        }

        private static RecipeResult Recipe(ProductId id, string name, int price, WinTier tier)
        {
            return new RecipeResult { Product = id, DisplayName = name, BasePrice = price, Tier = tier };
        }
    }
}
