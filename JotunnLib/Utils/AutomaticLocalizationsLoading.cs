using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Managers;

namespace Jotunn.Utils
{
    internal static class AutomaticLocalizationsLoading
    {
        public const string TranslationsFolderName = "Translations";
        public const string CommunityTranslationFileName = "community_translation.json";

        public static void Init()
        {
            var jsonFormat = new HashSet<FileInfo>();
            var unityFormat = new HashSet<FileInfo>();
            var yamlFormat = new HashSet<FileInfo>();

            // Json format community files
            AddTranslationFiles(jsonFormat, Paths.LanguageTranslationsFolder, CommunityTranslationFileName);
            AddTranslationFiles(jsonFormat, Paths.LanguageTranslationsFolder, "*.json");
            AddTranslationFiles(unityFormat, Paths.LanguageTranslationsFolder, "*.language");
            AddTranslationFiles(yamlFormat, Paths.LanguageTranslationsFolder, "*.yaml");
            AddTranslationFiles(yamlFormat, Paths.LanguageTranslationsFolder, "*.yml");

            foreach (var fileInfo in jsonFormat)
            {
                try
                {
                    var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                    LocalizationManager.Instance.GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName, true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                }
            }

            foreach (var fileInfo in unityFormat)
            {
                try
                {
                    var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                    LocalizationManager.Instance.GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                }
            }

            if (yamlFormat.Count > 0 && !Entities.CustomLocalization.IsYamlDotNetAvailable())
            {
                Logger.LogWarning($"Found {yamlFormat.Count.ToString()} YAML localization file(s) but YamlDotNet is not loaded. " +
                    "Mods using .yaml/.yml localization must include YamlDotNet.dll as a dependency.");
            }
            else
            {
                foreach (var fileInfo in yamlFormat)
                {
                    try
                    {
                        var mod = BepInExUtils.GetPluginInfoFromPath(fileInfo)?.Metadata;
                        LocalizationManager.Instance.GetLocalization(mod ?? Main.Instance.Info.Metadata).AddFileByPath(fileInfo.FullName);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Exception caught while loading localization file {fileInfo}: {ex}");
                    }
                }
            }
        }

        private static void AddTranslationFiles(HashSet<FileInfo> target, string path, string searchPattern)
        {
            var pathDirectoryInfo = new DirectoryInfo(path);
            if (!pathDirectoryInfo.Exists)
            {
                return;
            }

            var files = Directory.GetFiles(pathDirectoryInfo.FullName, searchPattern, SearchOption.AllDirectories);

            foreach (var filePath in files)
            {
                if (new DirectoryInfo(filePath).Parent?.Parent?.Name == TranslationsFolderName)
                {
                    target.Add(new FileInfo(filePath));
                }
            }
        }
    }
}
