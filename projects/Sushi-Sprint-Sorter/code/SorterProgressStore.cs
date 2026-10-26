using System;
using System.Collections.Generic;
using UnityEngine;

namespace SorterGame
{
    public enum ShopItemCategory
    {
        Character,
        TrackDecoration
    }

    [Serializable]
    public sealed class ShopItemDefinition
    {
        public string Id;
        public ShopItemCategory Category;
        public string DisplayName;
        public int Price;
        public Color PrimaryColor;
        public Color AccentColor;
        public string ResourcePath;

        public ShopItemDefinition(string id, ShopItemCategory category, string displayName, int price, Color primaryColor, Color accentColor, string resourcePath = null)
        {
            Id = id;
            Category = category;
            DisplayName = displayName;
            Price = price;
            PrimaryColor = primaryColor;
            AccentColor = accentColor;
            ResourcePath = resourcePath;
        }
    }

    public sealed class SorterProgressStore
    {
        private const string KeyPrefix = "SorterGame.Progress.";
        private const string WalletTipsKey = KeyPrefix + "WalletTips";
        private const string OwnedItemsKey = KeyPrefix + "OwnedItems";
        private const string EquippedCharacterKey = KeyPrefix + "EquippedCharacter";
        private const string EquippedTrackKey = KeyPrefix + "EquippedTrack";

        public const string DefaultCharacterId = "moss_pup";
        public const string DefaultTrackId = "classic_belt";

        private readonly HashSet<string> ownedItems = new HashSet<string>();

        public int WalletTips { get; private set; }
        public string EquippedCharacterId { get; private set; } = DefaultCharacterId;
        public string EquippedTrackId { get; private set; } = DefaultTrackId;

        public static IReadOnlyList<ShopItemDefinition> Items { get; } = new List<ShopItemDefinition>
        {
            new ShopItemDefinition(DefaultCharacterId, ShopItemCategory.Character, "Moss Pup", 0, new Color(0.42f, 0.64f, 0.34f), new Color(0.86f, 0.94f, 0.70f), "SorterGameArt/Monsters/moss_pup"),
            new ShopItemDefinition("button_imp", ShopItemCategory.Character, "Button Imp", 3200, new Color(0.86f, 0.36f, 0.28f), new Color(0.98f, 0.82f, 0.42f), "SorterGameArt/Monsters/button_imp"),
            new ShopItemDefinition("lantern_bug", ShopItemCategory.Character, "Lantern Bug", 4800, new Color(0.88f, 0.58f, 0.20f), new Color(0.34f, 0.18f, 0.12f), "SorterGameArt/Monsters/lantern_bug"),
            new ShopItemDefinition("moon_jelly", ShopItemCategory.Character, "Moon Jelly", 7000, new Color(0.58f, 0.64f, 0.96f), new Color(0.90f, 0.84f, 1.00f), "SorterGameArt/Monsters/moon_jelly"),
            new ShopItemDefinition("tiny_clerk", ShopItemCategory.Character, "Tiny Clerk", 9500, new Color(0.36f, 0.50f, 0.74f), new Color(0.92f, 0.58f, 0.46f), "SorterGameArt/Monsters/tiny_clerk"),
            new ShopItemDefinition("rabbit_thing", ShopItemCategory.Character, "Rabbit Thing", 13000, new Color(0.82f, 0.82f, 0.74f), new Color(0.56f, 0.34f, 0.62f), "SorterGameArt/Monsters/rabbit_thing"),
            new ShopItemDefinition(DefaultTrackId, ShopItemCategory.TrackDecoration, "Sakura", 0, new Color(0.15f, 0.17f, 0.17f), new Color(0.98f, 0.62f, 0.72f), "GeneratedUI/TrackSideDecorations/track_side_decor_01_sakura"),
            new ShopItemDefinition("flower_belt", ShopItemCategory.TrackDecoration, "Lantern", 3600, new Color(0.18f, 0.28f, 0.22f), new Color(0.96f, 0.72f, 0.34f), "GeneratedUI/TrackSideDecorations/track_side_decor_02_bamboo_lantern"),
            new ShopItemDefinition("flame_belt", ShopItemCategory.TrackDecoration, "Gold Rope", 5400, new Color(0.26f, 0.14f, 0.10f), new Color(1.00f, 0.72f, 0.24f), "GeneratedUI/TrackSideDecorations/track_side_decor_03_gold_rope"),
            new ShopItemDefinition("star_belt", ShopItemCategory.TrackDecoration, "Wave", 7600, new Color(0.13f, 0.16f, 0.28f), new Color(0.42f, 0.74f, 1.00f), "GeneratedUI/TrackSideDecorations/track_side_decor_04_blue_wave"),
            new ShopItemDefinition("leaf_belt", ShopItemCategory.TrackDecoration, "Wasabi", 9800, new Color(0.12f, 0.26f, 0.18f), new Color(0.56f, 0.86f, 0.36f), "GeneratedUI/TrackSideDecorations/track_side_decor_05_wasabi_leaf"),
            new ShopItemDefinition("star_ticket_belt", ShopItemCategory.TrackDecoration, "Star Ticket", 12500, new Color(0.18f, 0.16f, 0.30f), new Color(1.00f, 0.86f, 0.28f), "GeneratedUI/TrackSideDecorations/track_side_decor_06_star_ticket"),
        };

        public static SorterProgressStore Load()
        {
            SorterProgressStore store = new SorterProgressStore();
            store.WalletTips = Mathf.Max(0, PlayerPrefs.GetInt(WalletTipsKey, 0));
            store.LoadOwnedItems(PlayerPrefs.GetString(OwnedItemsKey, string.Empty));
            store.EquippedCharacterId = PlayerPrefs.GetString(EquippedCharacterKey, DefaultCharacterId);
            store.EquippedTrackId = PlayerPrefs.GetString(EquippedTrackKey, DefaultTrackId);
            store.EnsureDefaults();
            return store;
        }

        public static void DeleteSaveForTest()
        {
            PlayerPrefs.DeleteKey(WalletTipsKey);
            PlayerPrefs.DeleteKey(OwnedItemsKey);
            PlayerPrefs.DeleteKey(EquippedCharacterKey);
            PlayerPrefs.DeleteKey(EquippedTrackKey);
            PlayerPrefs.Save();
        }

        public void Save()
        {
            EnsureDefaults();
            PlayerPrefs.SetInt(WalletTipsKey, WalletTips);
            PlayerPrefs.SetString(OwnedItemsKey, string.Join(",", ownedItems));
            PlayerPrefs.SetString(EquippedCharacterKey, EquippedCharacterId);
            PlayerPrefs.SetString(EquippedTrackKey, EquippedTrackId);
            PlayerPrefs.Save();
        }

        public bool IsOwned(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && ownedItems.Contains(itemId);
        }

        public bool CanAfford(ShopItemDefinition item)
        {
            return item != null && WalletTips >= item.Price;
        }

        public bool Purchase(string itemId)
        {
            ShopItemDefinition item = FindItem(itemId);
            if (item == null)
            {
                return false;
            }
            if (IsOwned(itemId))
            {
                return Equip(itemId);
            }
            if (!CanAfford(item))
            {
                return false;
            }

            WalletTips -= item.Price;
            ownedItems.Add(itemId);
            EquipOwned(item);
            Save();
            return true;
        }

        public bool Equip(string itemId)
        {
            ShopItemDefinition item = FindItem(itemId);
            if (item == null || !IsOwned(itemId))
            {
                return false;
            }

            EquipOwned(item);
            Save();
            return true;
        }

        public void AddTips(int amount)
        {
            WalletTips = Mathf.Max(0, WalletTips + amount);
            Save();
        }

        public void SetWalletTipsForTest(int amount)
        {
            WalletTips = Mathf.Max(0, amount);
            Save();
        }

        public ShopItemDefinition EquippedCharacter => FindItem(EquippedCharacterId) ?? FindItem(DefaultCharacterId);
        public ShopItemDefinition EquippedTrack => FindItem(EquippedTrackId) ?? FindItem(DefaultTrackId);

        public static ShopItemDefinition FindItem(string itemId)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Id == itemId)
                {
                    return Items[i];
                }
            }

            return null;
        }

        private void LoadOwnedItems(string serialized)
        {
            ownedItems.Clear();
            if (!string.IsNullOrEmpty(serialized))
            {
                string[] ids = serialized.Split(',');
                for (int i = 0; i < ids.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(ids[i]))
                    {
                        ownedItems.Add(ids[i].Trim());
                    }
                }
            }
        }

        private void EnsureDefaults()
        {
            ownedItems.Add(DefaultCharacterId);
            ownedItems.Add(DefaultTrackId);
            if (!IsOwned(EquippedCharacterId) || !IsValidEquippedItem(EquippedCharacterId, ShopItemCategory.Character))
            {
                EquippedCharacterId = DefaultCharacterId;
            }
            if (!IsOwned(EquippedTrackId) || !IsValidEquippedItem(EquippedTrackId, ShopItemCategory.TrackDecoration))
            {
                EquippedTrackId = DefaultTrackId;
            }
        }

        private static bool IsValidEquippedItem(string itemId, ShopItemCategory category)
        {
            ShopItemDefinition item = FindItem(itemId);
            return item != null && item.Category == category;
        }

        private void EquipOwned(ShopItemDefinition item)
        {
            if (item.Category == ShopItemCategory.Character)
            {
                EquippedCharacterId = item.Id;
            }
            else if (item.Category == ShopItemCategory.TrackDecoration)
            {
                EquippedTrackId = item.Id;
            }
        }
    }
}
