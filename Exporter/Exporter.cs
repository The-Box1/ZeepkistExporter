using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Exporter;

public class ExportedData
{
    public List<LevelData> levels { get; set; }
    public CosmeticData cosmetics { get; set; }
}
    
public class CosmeticData
{
    public List<HatData> hats { get; set; }
    public List<ZeepkistData> zeepkists { get; set; }
    public List<SkinData> skins { get; set; }
}
    
public class HatData
{
    public string family { get; set; }
    public string name { get; set; }
    public string id { get; set; }
    public string unlockedBy { get; set; }
}
    
public class ZeepkistData
{
    public string family { get; set; }
    public string name { get; set; }
    public string id { get; set; }
    public string unlockedBy { get; set; }
}
    
public class SkinData
{
    public string family { get; set; }
    public string name { get; set; }
    public string id { get; set; }
    public string unlockedBy { get; set; }
    public string color { get; set; }
}
    
public class LevelData
{
    public string name { get; set; }
    public float bronzeTime { get; set; }
    public float silverTime { get; set; }
    public float goldTime { get; set; }
    public float authorTime { get; set; }
    public List<string> finishRewards { get; set; }
    public List<string> bronzeRewards { get; set; }
    public List<string> silverRewards { get; set; }
    public List<string> goldRewards { get; set; }
    public List<string> authorRewards { get; set; }
    public string thumbnail { get; set; }
}
    
public class LevelPositionData
{
    public string name { get; set; }
    public float x { get; set; }
    public float z { get; set; }
    public List<string> unlocks { get; set; }
}
    
[HarmonyPatch(typeof(AvonturenGarage), "Awake")]
public class AvonturenGarageAwakePatch
{
    static string GetUnlockReason(CosmeticItemBase cosmetic, List<LevelData> levels, ProgressionMaestro progression)
    {
        if (cosmetic.parentShelf.isAchievement) return cosmetic.parentShelf.Achievement_Name;
        if (cosmetic.parentShelf.isDLC) return "DLC " + cosmetic.parentShelf.DLC_appID;
        if (cosmetic.parentShelf.isPlayerSpecific) return "Players " + string.Join(", ", cosmetic.parentShelf.PlayerSpecificIDs);
        
        string id = GetCosmeticId(cosmetic);
        
        foreach (LevelData level in levels)
        {
            if (level.finishRewards.Contains(id)) return level.name + ", Finish";
            if (level.bronzeRewards.Contains(id)) return level.name + ", Bronze";
            if (level.silverRewards.Contains(id)) return level.name + ", Silver";
            if (level.goldRewards.Contains(id)) return level.name + ", Gold";
            if (level.authorRewards.Contains(id)) return level.name + ", Author";
        }
        
        const string Default = "Default Cosmetic";
    
        foreach (CosmeticItemBase initialHat in progression.initialHats) if (GetCosmeticId(initialHat) == GetCosmeticId(cosmetic)) return Default;
        foreach (CosmeticItemBase initialZeepkist in progression.initialZeepkists) if (GetCosmeticId(initialZeepkist) == GetCosmeticId(cosmetic)) return Default;
        foreach (CosmeticItemBase initialColor in progression.initialColors) if (GetCosmeticId(initialColor) == GetCosmeticId(cosmetic)) return Default;
        
        return "Unobtainable";
    }
    
    static string GetCosmeticName(CosmeticItemBase cosmetic)
    {
        string name = cosmetic.name.Contains(" - ")
            ? cosmetic.name.Split(new[] { " - " }, StringSplitOptions.None)[1]
            : cosmetic.name;
        
        return name;
    }
    
    static string GetCosmeticFamilyName(CosmeticItemBase cosmetic)
    {
        return cosmetic.parentShelf.name.Split(new[] { " - " }, StringSplitOptions.None)[1];
    }
    
    static string GetCosmeticId(CosmeticItemBase cosmetic)
    {
        string prefix = "";
            
        if (cosmetic.itemType == CosmeticShelf.FamilyType.zeepkist)
        {
            if (cosmetic.parentShelf.isDLC) prefix = "DLC Zeepkist ";
            else if (cosmetic.parentShelf.isAchievement) prefix = "ACH Zeepkist ";
            else prefix = "Zeepkist ";
        } else if (cosmetic.itemType == CosmeticShelf.FamilyType.hat)
        {
            if (cosmetic.parentShelf.isDLC) prefix = "DLC Hat ";
            else if (cosmetic.parentShelf.isAchievement) prefix = "ACH Hat ";
            else prefix = "Hat ";
        } else if (cosmetic.itemType == CosmeticShelf.FamilyType.skin)
        {
            if (cosmetic.parentShelf.isDLC) prefix = "DLC Skin ";
            else if (cosmetic.parentShelf.isAchievement) prefix = "ACH Skin ";
            else prefix = "Skin ";
        }
        
        return prefix + cosmetic.GetCompleteID();
    }
    
    static string FormatSkinColor(CosmeticColor skin)
    {
        Color color = skin.skinColor.color;
        return color.r * 255 + ", " + color.g * 255 + ", " + color.b * 255 + ", " + color.a * 255;
    }
    
    static bool Prefix(AvonturenGarage __instance)
    {
        __instance.manager = GameObject.Find("Game Manager").GetComponent<PlayerManager>();
        
        if (Main.garage) return false;
        Main.garage = true;
        
        ExportedData exportedData = new ExportedData();
        exportedData.levels = new List<LevelData>();
        
        AvonturenDing[] levels = Object.FindObjectsOfType<AvonturenDing>();
        
        Dictionary<string, string> cosmeticIdToName = new Dictionary<string, string>();
        
        foreach (AvonturenDing level in levels)
        {
            if (level.gameObject.name.Contains("Garage")) continue;
            
            LevelData levelData = new LevelData();
            
            levelData.name = level.theLevel.level_name;
            
            levelData.bronzeTime = level.theLevel.bronzeTime;
            levelData.silverTime = level.theLevel.silverTime;
            levelData.goldTime = level.theLevel.goldTime;
            levelData.authorTime = level.theLevel.authorTime;
            levelData.finishRewards = new List<string>();
            foreach (CosmeticItemBase unlocked in level.theLevel.unlockListFinished) levelData.finishRewards.Add(GetCosmeticId(unlocked));
            levelData.bronzeRewards = new List<string>();
            foreach (CosmeticItemBase unlocked in level.theLevel.unlockListBronze) levelData.bronzeRewards.Add(GetCosmeticId(unlocked));
            levelData.silverRewards = new List<string>();
            foreach (CosmeticItemBase unlocked in level.theLevel.unlockListSilver) levelData.silverRewards.Add(GetCosmeticId(unlocked));
            levelData.goldRewards = new List<string>();
            foreach (CosmeticItemBase unlocked in level.theLevel.unlockListGold) levelData.goldRewards.Add(GetCosmeticId(unlocked));
            levelData.authorRewards = new List<string>();
            foreach (CosmeticItemBase unlocked in level.theLevel.unlockListAuthor) levelData.authorRewards.Add(GetCosmeticId(unlocked));
            
            levelData.thumbnail = level.theLevel.level_thumbnail.name;
            
            exportedData.levels.Add(levelData);
        }
        
        exportedData.cosmetics = new CosmeticData();
        
        exportedData.cosmetics.hats = new List<HatData>();
        foreach (CosmeticItemBase hat in __instance.manager.objectsList.wardrobe.everyHat.Values)
        {
            cosmeticIdToName.Add(GetCosmeticId(hat), GetCosmeticName(hat));
            
            exportedData.cosmetics.hats.Add(new HatData
            {
                family = GetCosmeticFamilyName(hat),
                name = GetCosmeticName(hat),
                id = GetCosmeticId(hat),
                unlockedBy = GetUnlockReason(hat, exportedData.levels, __instance.manager.progression)
            });
        }
        
        exportedData.cosmetics.zeepkists = new List<ZeepkistData>();
        foreach (CosmeticItemBase zeepkist in __instance.manager.objectsList.wardrobe.everyZeepkist.Values)
        {
            cosmeticIdToName.Add(GetCosmeticId(zeepkist), GetCosmeticName(zeepkist));
            
            exportedData.cosmetics.zeepkists.Add(new ZeepkistData
            {
                family = GetCosmeticFamilyName(zeepkist),
                name = GetCosmeticName(zeepkist),
                id = GetCosmeticId(zeepkist),
                unlockedBy = GetUnlockReason(zeepkist, exportedData.levels, __instance.manager.progression)
            });
        }
        
        exportedData.cosmetics.skins = new List<SkinData>();
        foreach (CosmeticItemBase skin in __instance.manager.objectsList.wardrobe.everyColor.Values)
        {
            cosmeticIdToName.Add(GetCosmeticId(skin), GetCosmeticName(skin));
            
            exportedData.cosmetics.skins.Add(new SkinData
            {
                family = GetCosmeticFamilyName(skin),
                name = GetCosmeticName(skin),
                id = GetCosmeticId(skin),
                unlockedBy = GetUnlockReason(skin, exportedData.levels, __instance.manager.progression),
                color = FormatSkinColor((CosmeticColor) skin)
            });
        }
        
        File.WriteAllText(Path.Combine(Paths.GameRootPath, "Zeepkist_Data", "ExportedData", "data.json"), LitJson.JsonMapper.ToJson(exportedData));
        
        List<LevelPositionData> levelPositions = new List<LevelPositionData>();
        foreach (AvonturenDing level in levels)
        {
            if (level.gameObject.name.Contains("Garage")) continue;
            
            List<string> unlocks = new List<string>();
            foreach (AvonturenLevelPrefab unlockLevel in level.theLevel.unlockLevels) unlocks.Add(unlockLevel.level_name);
            
            LevelPositionData positionData = new LevelPositionData
            {
                name = level.theLevel.level_name,
                x = level.transform.position.x,
                z = level.transform.position.z,
                unlocks = unlocks
            };
            
            levelPositions.Add(positionData);
        }
        
        File.WriteAllText(Path.Combine(Paths.GameRootPath, "Zeepkist_Data", "ExportedData", "positions.json"), LitJson.JsonMapper.ToJson(levelPositions));
        
        Main.Logger.LogMessage("Wrote levels to file");
        
        return false;
    }
}