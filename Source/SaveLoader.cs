using Verse;
using RimWorld;
using System;
using System.IO;

namespace RandomPlus
{
    public class SaveLoader
    {
        public static readonly string filename = "RandomPlus.xml";

        public static string GetFilePath()
        {
            string configFolder = Path.GetDirectoryName(GenFilePaths.ModsConfigFilePath);
            return Path.Combine(configFolder, filename);
        }

        public static void SaveOverwrite(int index, PawnFilter pawnFilter)
        {
            if (index >= 0)
                RandomSettings.pawnFilterList[index] = pawnFilter;

            SaveAll();
        }

        public static void Save(PawnFilter pawnFilter)
        {
            if (!RandomSettings.pawnFilterList.Contains(pawnFilter))
                RandomSettings.pawnFilterList.Add(pawnFilter);

            SaveAll();
        }

        public static void SaveAll()
        {
            
            try
            {
                Scribe.saver.InitSaving(GetFilePath(), "RandomPlus");
                Scribe_Collections.Look<PawnFilter>(ref RandomSettings.pawnFilterList, "list", LookMode.Deep, null);
            }
            catch (Exception e)
            {
                Log.Error("Failed to save file");
                throw e;
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
            }
        }

        public static void Load(PawnFilter pawnFilter)
        {
            RandomSettings.PawnFilter = pawnFilter;
        }

        public static void LoadAll()
        {
            string filePath = GetFilePath();
            if (!File.Exists(filePath))
                return;
                
            try
            {
                Scribe.loader.InitLoading(GetFilePath());
                Scribe_Collections.Look<PawnFilter>(ref RandomSettings.pawnFilterList, "list");
            }
            catch (Exception)
            {
                Log.Error("Failed to load file");
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
                Scribe.mode = LoadSaveMode.Inactive;

                // disable fast algorithm when HAR mod is enable
                if (ModsConfig.IsActive("erdelf.HumanoidAlienRaces"))
                {
                    foreach (var filter in RandomSettings.pawnFilterList)
                    {
                        filter.RerollAlgorithm = PawnFilter.RerollAlgorithmOptions.Normal;
                    }
                }
            }
        }

        public static void Delete(PawnFilter pawnFilter)
        {
            RandomSettings.pawnFilterList.Remove(pawnFilter);
            SaveAll();
        }
    }
}
