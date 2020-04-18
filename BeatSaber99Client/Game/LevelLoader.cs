﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BS_Utils.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeatSaber99Client.Game
{
    public static class LevelLoader
    {
        private static bool _loaded = false;
        private static BeatmapLevelsModel _beatmapLevelsModel;
        private static AdditionalContentModel _contentModelSO;
        private static BeatmapCharacteristicSO[] _beatmapCharacteristics;

        public static void Init()
        {
            if (_loaded) return;
            _loaded = true;

            Plugin.log.Debug("Loading level loader");

            _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
            _contentModelSO = Resources.FindObjectsOfTypeAll<AdditionalContentModel>().FirstOrDefault();
            _beatmapCharacteristics = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>();
        }

        public static BeatmapCharacteristicSO[] Characteristics
        {
            get
            {
                if (_beatmapCharacteristics == null)
                    _beatmapCharacteristics = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>();
                return _beatmapCharacteristics;
            }
        }

        public static BeatmapCharacteristicSO StandardCharacteristic =>
            _beatmapCharacteristics.First(c => c.serializedName == "Standard");

        public static IEnumerable<IPreviewBeatmapLevel> AllLevels
        {
            get
            {
                _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();

                if (_beatmapLevelsModel.allLoadedBeatmapLevelPackCollection == null)
                    Plugin.log.Critical("All Loaded beatmap levels is null!");
                if (_beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks == null)
                    Plugin.log.Critical("All Loaded beatmap levels is null!");

                return _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x =>
                    x.beatmapLevelCollection?.beatmapLevels ?? new IPreviewBeatmapLevel[0]);
            }
        }

        public static async void LoadBeatmapLevelAsync(
            BeatmapCharacteristicSO characteristic, IPreviewBeatmapLevel selectedLevel, BeatmapDifficulty difficulty,
            Action<AdditionalContentModel.EntitlementStatus, bool, IBeatmapLevel> callback)
        {
            _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
            _contentModelSO = Resources.FindObjectsOfTypeAll<AdditionalContentModel>().FirstOrDefault();
            _beatmapCharacteristics = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>();

            var token = new CancellationTokenSource();

            Plugin.log.Info("Checking entitlement");

            var entitlementStatus =
                await _contentModelSO.GetLevelEntitlementStatusAsync(selectedLevel.levelID, token.Token);

            if (entitlementStatus == AdditionalContentModel.EntitlementStatus.Owned)
            {
                Plugin.log.Info("Level owned. Loading...");

                var getBeatmapLevelResult =
                    await _beatmapLevelsModel.GetBeatmapLevelAsync(selectedLevel.levelID, token.Token);

                callback?.Invoke(entitlementStatus, !getBeatmapLevelResult.isError, getBeatmapLevelResult.beatmapLevel);

                Plugin.log.Info("Starting...");
                StartLevel(getBeatmapLevelResult.beatmapLevel, characteristic, difficulty,
                    GameplayModifiers.defaultModifiers);
            }
            else
            {
                callback?.Invoke(entitlementStatus, false, null);
            }
        }

        private static void StartLevel(IBeatmapLevel level, BeatmapCharacteristicSO characteristic,
            BeatmapDifficulty difficulty, GameplayModifiers modifiers, float startTime = 0f)
        {
            /*Client.Instance.playerInfo.updateInfo.playerComboBlocks = 0;
            Client.Instance.playerInfo.updateInfo.playerCutBlocks = 0;
            Client.Instance.playerInfo.updateInfo.playerTotalBlocks = 0;
            Client.Instance.playerInfo.updateInfo.playerEnergy = 0f;
            Client.Instance.playerInfo.updateInfo.playerScore = 0;
            Client.Instance.playerInfo.updateInfo.playerLevelOptions = new LevelOptionsInfo(difficulty, modifiers, characteristic.serializedName);*/

            // UpdateLevelOptions

            var menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().FirstOrDefault();


            if (menuSceneSetupData != null)
            {
                var playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault().playerData;

                var playerSettings = playerData.playerSpecificSettings;
                var environmentOverrideSettings = playerData.overrideEnvironmentSettings;

                var colorSchemesSettings = playerData.colorSchemesSettings.overrideDefaultColors
                    ? playerData.colorSchemesSettings.GetColorSchemeForId(playerData.colorSchemesSettings
                        .selectedColorSchemeId)
                    : null;


                var difficultyBeatmap = level.GetDifficultyBeatmap(characteristic, difficulty, false);

                Plugin.log.Debug(
                    $"Starting song: name={level.songName}, levelId={level.levelID}, difficulty={difficulty}");

                try
                {
                    BS_Utils.Gameplay.Gamemode.NextLevelIsIsolated("TestMod");
                }
                catch
                {
                }

                PracticeSettings practiceSettings = null;
                if (startTime > 1f)
                {
                    practiceSettings = new PracticeSettings(PracticeSettings.defaultPracticeSettings);
                    if (startTime > 1f)
                    {
                        practiceSettings.startSongTime = startTime + 1.5f;
                        practiceSettings.startInAdvanceAndClearNotes = true;
                    }

                    practiceSettings.songSpeedMul = modifiers.songSpeedMul;
                }

                var scoreSaber = IPA.Loader.PluginManager.GetPluginFromId("ScoreSaber");

                if (scoreSaber != null)
                {
                    // ScoreSaberInterop.InitAndSignIn();
                }

                menuSceneSetupData.StartStandardLevel(
                    difficultyBeatmap,
                    environmentOverrideSettings,
                    colorSchemesSettings,
                    modifiers,
                    playerSettings,
                    practiceSettings,
                    "Menu",
                    false,
                    () => { },
                    (manager, result) => { });
            }
            else
            {
                Plugin.log.Error("SceneSetupData is null!");
            }
        }

        public static async void PreloadBeatmapLevelAsync(
            BeatmapCharacteristicSO characteristic,
            IPreviewBeatmapLevel selectedLevel,
            BeatmapDifficulty difficulty,
            GameplayModifiers modifiers,
            Action<PreloadedLevel> callback)
        {
            _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
            _contentModelSO = Resources.FindObjectsOfTypeAll<AdditionalContentModel>().FirstOrDefault();
            _beatmapCharacteristics = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>();

            var token = new CancellationTokenSource();

            Plugin.log.Info("Checking entitlement");

            var previewCache = _beatmapLevelsModel
                .GetPrivateField<Dictionary<string, IPreviewBeatmapLevel>>("_loadedPreviewBeatmapLevels");

            if (!previewCache.ContainsKey(selectedLevel.levelID))
            {
                previewCache.Add(selectedLevel.levelID, selectedLevel);
                _beatmapLevelsModel.SetPrivateField("_loadedPreviewBeatmapLevels", previewCache);
                Plugin.log.Info("Updated internal cache");
            }

            var entitlementStatus =
                await _contentModelSO.GetLevelEntitlementStatusAsync(selectedLevel.levelID, token.Token);

            if (entitlementStatus == AdditionalContentModel.EntitlementStatus.Owned)
            {
                Plugin.log.Info("Level owned. Loading...");

                var getBeatmapLevelResult =
                    await _beatmapLevelsModel.GetBeatmapLevelAsync(selectedLevel.levelID, token.Token);

                Plugin.log.Info("Level preload complete.");

                if (getBeatmapLevelResult.isError)
                {
                    Plugin.log.Info("Beatmap level load failure.");
                    callback?.Invoke(null);
                    return;
                }

                Plugin.log.Info("Level preload 1.");
                var playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault().playerData;
                Plugin.log.Info("Level preload 2.");

                var playerSettings = playerData.playerSpecificSettings;
                Plugin.log.Info("Level preload 3.");
                var environmentOverrideSettings = playerData.overrideEnvironmentSettings;
                Plugin.log.Info("Level preload 4.");

                var colorSchemesSettings = playerData.colorSchemesSettings.overrideDefaultColors
                    ? playerData.colorSchemesSettings.GetColorSchemeForId(playerData.colorSchemesSettings
                        .selectedColorSchemeId)
                    : null;

                Plugin.log.Info("Level preload 5.");

                var difficultyBeatmap =
                    getBeatmapLevelResult.beatmapLevel.GetDifficultyBeatmap(characteristic, difficulty, false);

                Plugin.log.Info("Level preload 6.");

                callback?.Invoke(new PreloadedLevel()
                {
                    characteristic = characteristic,
                    levelResult = getBeatmapLevelResult,
                    modifiers = modifiers,
                    difficulty = difficultyBeatmap,
                    playerData = playerData,
                    playerSpecificSettings = playerSettings,
                    environmentSettings = environmentOverrideSettings,
                    colorScheme = colorSchemesSettings
                });
            }
            else
            {
                callback?.Invoke(null);
            }
        }

        public static void SwitchLevel(PreloadedLevel level, float startTime = 0f)
        {
            var _scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().FirstOrDefault();
            var menuSceneSetupData =
                Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().FirstOrDefault();

            if (menuSceneSetupData != null)
            {
                Plugin.log.Debug(
                    $"Starting song: name={level.levelResult.beatmapLevel.songName}, levelId={level.levelResult.beatmapLevel.levelID}");

                try
                {
                    BS_Utils.Gameplay.Gamemode.NextLevelIsIsolated("TestMod");
                }
                catch
                {
                }

                PracticeSettings practiceSettings = null;
                if (startTime > 1f || level.speed != 1f)
                {
                    practiceSettings = new PracticeSettings(PracticeSettings.defaultPracticeSettings);
                    if (startTime > 1f)
                    {
                        practiceSettings.startSongTime = startTime + 1.5f;
                        practiceSettings.startInAdvanceAndClearNotes = true;
                    }

                    practiceSettings.songSpeedMul = level.speed != 1f ? level.speed : level.modifiers.songSpeedMul;
                }

                // var scoreSaber = IPA.Loader.PluginManager.GetPluginFromId("ScoreSaber");

                // if (scoreSaber != null)
                // {
                // ScoreSaberInterop.InitAndSignIn();
                // }

                var transition = Resources.FindObjectsOfTypeAll<StandardLevelScenesTransitionSetupDataSO>().First();
                transition.Init(
                    level.difficulty,
                    level.environmentSettings,
                    level.colorScheme,
                    level.modifiers,
                    level.playerSpecificSettings,
                    practiceSettings,
                    "Menu",
                    false);
                var _gameScenesManager = Object.FindObjectOfType<GameScenesManager>();

                _gameScenesManager.PopScenes(0f, null, 
                    () => { 
                        _gameScenesManager.PushScenes(transition, 0.0f);

                    });
            }
            else
            {
                Plugin.log.Error("SceneSetupData is null!");
            }
        }

        public static IDifficultyBeatmap GetDifficultyBeatmap(this IBeatmapLevel level,
            BeatmapCharacteristicSO characteristic, BeatmapDifficulty difficulty, bool strictDifficulty = false)
        {
            IDifficultyBeatmapSet difficultySet = null;
            if (characteristic == null)
            {
                difficultySet = level.beatmapLevelData.difficultyBeatmapSets.FirstOrDefault();
            }
            else
            {
                difficultySet =
                    level.beatmapLevelData.difficultyBeatmapSets.FirstOrDefault(x =>
                        x.beatmapCharacteristic == characteristic);
                if (difficultySet == null)
                    difficultySet = level.beatmapLevelData.difficultyBeatmapSets.FirstOrDefault();
            }

            if (difficultySet == null)
            {
                Plugin.log.Error("Unable to find any difficulty set!");
                return null;
            }

            var beatmap = difficultySet.difficultyBeatmaps.FirstOrDefault(x => x.difficulty == difficulty);

            if (beatmap == null && !strictDifficulty)
            {
                var index = GetClosestDifficultyIndex(difficultySet.difficultyBeatmaps, difficulty);
                if (index >= 0)
                {
                    return difficultySet.difficultyBeatmaps[index];
                }
                else
                {
                    Plugin.log.Error("Unable to find difficulty!");
                    return null;
                }
            }
            else
            {
                return beatmap;
            }
        }

        public static int GetClosestDifficultyIndex(IDifficultyBeatmap[] beatmaps, BeatmapDifficulty difficulty)
        {
            var num = -1;
            foreach (var difficultyBeatmap in beatmaps)
            {
                if (difficulty < difficultyBeatmap.difficulty) break;
                num++;
            }

            if (num == -1) num = 0;
            return num;
        }
    }
}