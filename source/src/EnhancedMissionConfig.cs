using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TaleWorlds.Core;

namespace EnhancedMission
{
    public class EnhancedMissionConfig
    {
        protected static Version BinaryVersion => new Version(1, 0);

        protected void UpgradeToCurrentVersion()
        {
            switch (ConfigVersion?.ToString())
            {
                default:
                    Utility.DisplayLocalizedText("str_config_incompatible");
                    ResetToDefault();
                    Serialize();
                    break;
                case "1.10":
                    break;
            }
        }

        private static EnhancedMissionConfig _instance;

        public string ConfigVersion { get; set; } = BinaryVersion.ToString();

        public int playerFormation = 4;

        public bool DisableDeath = false;

        public bool UseRealisticBlocking = false;

        public bool ChangeCombatAI;
        public int CombatAI;


        public static EnhancedMissionConfig Get()
        {
            if (_instance == null)
            {
                _instance = CreateDefault();
                _instance.SyncWithSave();
            }

            return _instance;
        }

        private static EnhancedMissionConfig CreateDefault()
        {
            return new EnhancedMissionConfig();
        }

        protected void EnsureSaveDirectory()
        {
            Directory.CreateDirectory(SavePath);
        }
        public bool Serialize()
        {
            try
            {
                EnsureSaveDirectory();
                XmlSerializer serializer = new XmlSerializer(typeof(EnhancedMissionConfig));
                using (TextWriter writer = new StreamWriter(SaveName))
                {
                    serializer.Serialize(writer, this);
                }
                Utility.DisplayLocalizedText("str_saved_config");
                return true;
            }
            catch (Exception e)
            {
                Utility.DisplayLocalizedText("str_save_config_failed");
                Utility.DisplayLocalizedText("str_exception_caught");
                Utility.DisplayMessage(e.ToString());
                Console.WriteLine(e);
            }

            return false;
        }

        public bool Deserialize()
        {
            try
            {
                EnsureSaveDirectory();
                XmlSerializer deserializer = new XmlSerializer(typeof(EnhancedMissionConfig));
                using (TextReader reader = new StreamReader(SaveName))
                {
                    var config = (EnhancedMissionConfig)deserializer.Deserialize(reader);
                    this.CopyFrom(config);
                }
                Utility.DisplayLocalizedText("str_loaded_config");
                UpgradeToCurrentVersion();
                return true;
            }
            catch (Exception e)
            {
                Utility.DisplayLocalizedText("str_load_config_failed");
                Utility.DisplayLocalizedText("str_exception_caught");
                Utility.DisplayMessage(e.ToString());
                Console.WriteLine(e);
            }

            return false;
        }
        protected void CopyFrom(EnhancedMissionConfig other)
        {
            this.ConfigVersion = other.ConfigVersion;
            this.playerFormation = other.playerFormation;
            this.DisableDeath = other.DisableDeath;
            this.UseRealisticBlocking = other.UseRealisticBlocking;
            this.ChangeCombatAI = other.ChangeCombatAI;
            this.CombatAI = other.CombatAI;
        }

        public void ResetToDefault()
        {
            CopyFrom(CreateDefault());
        }
        protected void SyncWithSave()
        {
            if (File.Exists(SaveName) && Deserialize())
            {
                RemoveOldConfig();
                return;
            }

            MoveOldConfig();
            if (File.Exists(SaveName) && Deserialize())
                return;
            Utility.DisplayLocalizedText("str_create_default_config");
            ResetToDefault();
            Serialize();
        }

        private void RemoveOldConfig()
        {
            foreach (var oldName in OldNames)
            {
                if (File.Exists(oldName))
                {
                    Utility.DisplayMessage(GameTexts.FindText("str_found_old_config").ToString() + $" \"{oldName}\".");
                    Utility.DisplayLocalizedText("str_delete_old_config");
                    File.Delete(oldName);
                }
            }
        }

        private void MoveOldConfig()
        {
            string firstOldName = OldNames.FirstOrDefault(File.Exists);
            if (firstOldName != null && !firstOldName.IsEmpty())
            {
                Utility.DisplayLocalizedText("str_rename_old_config");
                File.Move(firstOldName, SaveName);
            }
            RemoveOldConfig();
        }

        private static string ApplicationName = "Mount and Blade II Bannerlord";
        private static string ModuleName = "EnhancedMission";

        protected static string SavePath => Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" +
                                            ApplicationName + "\\Configs\\" + ModuleName + "\\";
        protected string SaveName => SavePath + nameof(EnhancedMissionConfig) + ".xml";
        protected string[] OldNames { get; } = { };
    }
}
