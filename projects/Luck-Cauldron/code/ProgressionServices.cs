// CLASS EXTRACTION: original class bodies; only imports, namespace and source comments added.
// Source: Assets/Scripts/Slot/SlotMachineController.cs
// Dependencies: AlchemyRules.cs, UnityEngine PlayerPrefs and JsonUtility.

using System;
using UnityEngine;

namespace LuckyCauldron
{
// Source lines 127-292
    public sealed class SpinCostService
    {
        public int CompletedSpinCount { get; private set; }
        public bool HasLubricatedReels { get; private set; }

        public int CurrentCost => Mathf.Max(1, 2 + CompletedSpinCount / 25 - (HasLubricatedReels ? 1 : 0));

        public void MarkSpinCompleted()
        {
            CompletedSpinCount++;
        }

        public bool BuyLubrication()
        {
            if (HasLubricatedReels) return false;
            HasLubricatedReels = true;
            return true;
        }

        public void Restore(int completedSpinCount, bool hasLubricatedReels)
        {
            CompletedSpinCount = Mathf.Max(0, completedSpinCount);
            HasLubricatedReels = hasLubricatedReels;
        }

        public void ResetRun()
        {
            CompletedSpinCount = 0;
            HasLubricatedReels = false;
        }
    }

    public sealed class WalletService
    {
        public int Coins { get; private set; } = 20;
        public int HighestCoins { get; private set; } = 20;

        public bool CanSpend(int amount) => Coins >= amount;

        public bool Spend(int amount)
        {
            if (!CanSpend(amount)) return false;
            Coins -= amount;
            return true;
        }

        public void Add(int amount)
        {
            Coins += amount;
            HighestCoins = Mathf.Max(HighestCoins, Coins);
        }

        public void Restore(int coins, int highestCoins)
        {
            Coins = Mathf.Max(0, coins);
            HighestCoins = Mathf.Max(20, Coins, highestCoins);
        }

        public void ResetRun()
        {
            Coins = 20;
        }
    }

    public sealed class ProductSellService
    {
        public int GlobalBonus { get; set; }
        public int SludgeBonus { get; set; }

        public int GetPrice(RecipeResult recipe)
        {
            var bonus = GlobalBonus + (recipe.Product == ProductId.Sludge ? SludgeBonus : 0);
            return recipe.BasePrice + bonus;
        }
    }

    public sealed class UpgradeService
    {
        public int StableHeatLevel { get; private set; }
        public int GoldLiningLevel { get; private set; }
        public int CrystalResonanceLevel { get; private set; }
        public int LuckyDustLevel { get; private set; }
        public bool HasLubricatedReels { get; private set; }
        public bool HasLockReel { get; private set; }

        public readonly int[] GoldLiningPrices = { 15, 25, 40, 60, 85 };
        public readonly int[] CrystalPrices = { 16, 28, 45 };
        public readonly int[] LuckyDustPrices = { 20, 36, 58 };

        public int PriceFor(UpgradeKind kind)
        {
            return kind switch
            {
                UpgradeKind.StableHeat => StableHeatLevel < 2 ? 8 + StableHeatLevel * 12 : -1,
                UpgradeKind.GoldLining => GoldLiningLevel < GoldLiningPrices.Length ? GoldLiningPrices[GoldLiningLevel] : -1,
                UpgradeKind.CrystalResonance => CrystalResonanceLevel < CrystalPrices.Length ? CrystalPrices[CrystalResonanceLevel] : -1,
                UpgradeKind.LuckyDust => LuckyDustLevel < LuckyDustPrices.Length ? LuckyDustPrices[LuckyDustLevel] : -1,
                UpgradeKind.LubricatedReels => HasLubricatedReels ? -1 : 18,
                UpgradeKind.LockReel => HasLockReel ? -1 : 25,
                _ => -1
            };
        }

        public bool Buy(UpgradeKind kind, WalletService wallet, ProductSellService sell, SpinCostService cost)
        {
            var price = PriceFor(kind);
            if (price < 0 || !wallet.Spend(price)) return false;

            switch (kind)
            {
                case UpgradeKind.StableHeat:
                    StableHeatLevel++;
                    sell.SludgeBonus = StableHeatLevel;
                    break;
                case UpgradeKind.GoldLining:
                    GoldLiningLevel++;
                    sell.GlobalBonus = GoldLiningLevel;
                    break;
                case UpgradeKind.CrystalResonance:
                    CrystalResonanceLevel++;
                    break;
                case UpgradeKind.LuckyDust:
                    LuckyDustLevel++;
                    break;
                case UpgradeKind.LubricatedReels:
                    HasLubricatedReels = true;
                    cost.BuyLubrication();
                    break;
                case UpgradeKind.LockReel:
                    HasLockReel = true;
                    break;
            }

            return true;
        }

        public void Restore(SaveData data)
        {
            StableHeatLevel = Mathf.Clamp(data.StableHeatLevel, 0, 2);
            GoldLiningLevel = Mathf.Clamp(data.GoldLiningLevel, 0, GoldLiningPrices.Length);
            CrystalResonanceLevel = Mathf.Clamp(data.CrystalResonanceLevel, 0, CrystalPrices.Length);
            LuckyDustLevel = Mathf.Clamp(data.LuckyDustLevel, 0, LuckyDustPrices.Length);
            HasLubricatedReels = data.HasLubricatedReels;
            HasLockReel = data.HasLockReel;
        }

        public void ResetRun()
        {
            StableHeatLevel = 0;
            GoldLiningLevel = 0;
            CrystalResonanceLevel = 0;
            LuckyDustLevel = 0;
            HasLubricatedReels = false;
            HasLockReel = false;
        }
    }

    public enum UpgradeKind
    {
        StableHeat,
        GoldLining,
        CrystalResonance,
        LuckyDust,
        LubricatedReels,
        LockReel
    }

// Source lines 360-425
    public sealed class SaveData
    {
        public int HighestCoins;
        public int HighestSpinCount;
        public int HighestSingleWin;
        public string MostExpensiveProduct;
        public int RunStateVersion;
        public int CurrentCoins;
        public int RunSpinCount;
        public int Gems;
        public int Stars;
        public int BetMultiplier;
        public int StableHeatLevel;
        public int GoldLiningLevel;
        public int CrystalResonanceLevel;
        public int LuckyDustLevel;
        public bool HasLubricatedReels;
        public bool HasLockReel;
    }

    public sealed class SaveService
    {
        private const string Key = "LuckyCauldron.SaveData";
        public SaveData Data { get; private set; } = new();

        public void Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return;
            try
            {
                Data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)) ?? new SaveData();
            }
            catch (ArgumentException)
            {
                Data = new SaveData();
            }
        }

        public void Record(WalletService wallet, SpinCostService cost, UpgradeService upgrades,
            int gems, int stars, int betMultiplier, int singleWin, string product)
        {
            Data.HighestCoins = Mathf.Max(Data.HighestCoins, wallet.HighestCoins);
            Data.HighestSpinCount = Mathf.Max(Data.HighestSpinCount, cost.CompletedSpinCount);
            if (singleWin > Data.HighestSingleWin)
            {
                Data.HighestSingleWin = singleWin;
                Data.MostExpensiveProduct = product;
            }

            Data.RunStateVersion = 1;
            Data.CurrentCoins = wallet.Coins;
            Data.RunSpinCount = cost.CompletedSpinCount;
            Data.Gems = gems;
            Data.Stars = stars;
            Data.BetMultiplier = betMultiplier;
            Data.StableHeatLevel = upgrades.StableHeatLevel;
            Data.GoldLiningLevel = upgrades.GoldLiningLevel;
            Data.CrystalResonanceLevel = upgrades.CrystalResonanceLevel;
            Data.LuckyDustLevel = upgrades.LuckyDustLevel;
            Data.HasLubricatedReels = upgrades.HasLubricatedReels;
            Data.HasLockReel = upgrades.HasLockReel;

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }
}
