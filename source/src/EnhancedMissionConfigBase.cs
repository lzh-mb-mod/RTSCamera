using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TaleWorlds.Core;

namespace EnhancedMission
{
    public abstract class EnhancedMissionConfigBase<T> where T : EnhancedMissionConfigBase<T>
    {

        private static string ApplicationName = "Mount and Blade II Bannerlord";
        private static string ModuleName = "EnhancedMission";

        protected static string SavePath => Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" +
                                            ApplicationName + "\\Configs\\" + ModuleName + "\\";

        protected abstract void CopyFrom(T other);
        protected abstract void UpgradeToCurrentVersion();
        protected abstract XmlSerializer serializer { get; }

        public virtual bool Serialize()
        {
            try
            {
                EnsureSaveDirectory();
                XmlSerializer serializer = this.serializer;
                using (TextWriter writer = new StreamWriter(SaveName))
                {
                    serializer.Serialize(writer, this);
                }
                Utility.DisplayLocalizedText("str_em_saved_config");
                return true;
            }
            catch (Exception e)
            {
                Utility.DisplayLocalizedText("str_em_save_config_failed");
                Utility.DisplayLocalizedText("str_em_exception_caught");
                Utility.DisplayMessage(e.ToString());
                Console.WriteLine(e);
            }

            return false;
        }

        public virtual bool Deserialize()
        {
            try
            {
                EnsureSaveDirectory();
                XmlSerializer deserializer = this.serializer;
                using (TextReader reader = new StreamReader(SaveName))
                {
                    var config = (T)deserializer.Deserialize(reader);
                    this.CopyFrom(config);
                }
                Utility.DisplayLocalizedText("str_em_loaded_config");
                UpgradeToCurrentVersion();
                return true;
            }
            catch (Exception e)
            {
                Utility.DisplayLocalizedText("str_em_load_config_failed");
                Utility.DisplayLocalizedText("str_em_exception_caught");
                Utility.DisplayMessage(e.ToString());
                Console.WriteLine(e);
            }

            return false;
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
            Utility.DisplayLocalizedText("str_em_create_default_config");
            ResetToDefault();
            Serialize();
        }

        public abstract void ResetToDefault();

        protected void RemoveOldConfig()
        {
            foreach (var oldName in OldNames)
            {
                if (File.Exists(oldName))
                {
                    Utility.DisplayMessage(GameTexts.FindText("str_em_found_old_config").ToString() + $" \"{oldName}\".");
                    Utility.DisplayLocalizedText("str_em_delete_old_config");
                    File.Delete(oldName);
                }
            }
        }

        private void MoveOldConfig()
        {
            string firstOldName = OldNames.FirstOrDefault(File.Exists);
            if (firstOldName != null && !firstOldName.IsEmpty())
            {
                Utility.DisplayLocalizedText("str_em_rename_old_config");
                File.Move(firstOldName, SaveName);
            }
            RemoveOldConfig();
        }
        [XmlIgnore]
        protected abstract string SaveName { get; }
        [XmlIgnore]
        protected abstract string[] OldNames { get; }
        protected void EnsureSaveDirectory()
        {
            Directory.CreateDirectory(SavePath);
        }
    }
}
