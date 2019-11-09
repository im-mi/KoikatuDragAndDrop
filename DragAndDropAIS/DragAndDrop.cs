using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AIChara;
using B83.Win32;
using BepInEx;
using BepInEx.Configuration;
using CharaCustom;
using Common;
using Housing;
using Illusion.Game;
using Manager;
using Studio;
using UnityEngine;
using UnityEngine.UI;
using Input = UnityEngine.Input;

namespace DragAndDrop
{
    [BepInPlugin(GUID, "Drag and Drop", Version)]
    internal class DragAndDrop : BaseUnityPlugin
    {
        public const string GUID = "com.immi.aisyoujyo.draganddrop";
        internal const string Version = Metadata.Version;

        private const string CharaToken = "【AIS_Chara】";
        private const string StudioToken = "【KStudio】";
        private const string HouseToken = "【AIS_Housing】";

        private UnityDragAndDropHook _hook;
        private static readonly byte[] StudioTokenBytes = Encoding.UTF8.GetBytes(StudioToken);

        [DisplayName("Use maker load preferences")]
        [Description("Enables partial character loading using the options in the character maker's \"Load character\" menu.")]
        public ConfigEntry<bool> UseMakerLoadPreferences { get; private set; }

        protected void Start()
        {
            UseMakerLoadPreferences = Config.AddSetting("General", "useMakerPrefs", true,
                "Enables partial character loading using the options in the character maker's \"Load character\" menu.");
        }

        protected void OnEnable()
        {
            _hook = new UnityDragAndDropHook();
            _hook.InstallHook();
            _hook.OnDroppedFiles += OnDroppedFiles;
        }

        protected void OnDisable()
        {
            _hook.UninstallHook();
        }

        private void OnDroppedFiles(List<string> aFiles, POINT aPos)
        {
            var import = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Logger.Log(BepInEx.Logging.LogLevel.Debug, import ? "Importing" : "Overwriting");

            if (aFiles.Count == 0)
                return;

            if (!Singleton<Scene>.IsInstance())
                return;

            var goodFiles = aFiles.Where(f =>
            {
                if (string.IsNullOrEmpty(f)) return false;

                var extension = Path.GetExtension(f).ToLower();
                if (extension == ".png") return true;

                Logger.Log(BepInEx.Logging.LogLevel.Message, $"Unsupported file type {extension}. Only .png files are supported.");
                return false;
            }).ToList();

            if (goodFiles.Count == 0)
            {
                PlaySound(SystemSE.ok_l);
                return;
            }

            PngType GetType(string path)
            {
                var pngType = GetPngType(path);

                if (pngType == PngType.Unknown)
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Message, "Unknown file format.");
                    PlaySound(SystemSE.ok_l);
                }

                return pngType;
            }

            try
            {
                if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "CharaCustom"))
                {
                    if (Singleton<CustomBase>.IsInstance())
                    {
                        if (goodFiles.Count > 1)
                            Logger.Log(BepInEx.Logging.LogLevel.Message, "Warning: Only the first card will be loaded.");

                        var path = goodFiles.First();
                        var pngType = GetType(path);

                        if (pngType == PngType.AIS_Chara)
                        {
                            LoadMakerCharacter(path);
                            PlaySound(SystemSE.ok_s);
                        }
                        else if (pngType == PngType.KStudio)
                        {
                            Logger.Log(BepInEx.Logging.LogLevel.Message, "Scene files cannot be loaded in the character maker.");
                            PlaySound(SystemSE.ok_l);
                        }
                    }
                }
                else if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "Map"))
                {
                    if (FindObjectsOfType<UICtrl>().Any(i => i.IsInit))
                    {
                        if (goodFiles.Count > 1)
                            Logger.Log(BepInEx.Logging.LogLevel.Message, "Warning: Only the first card will be loaded.");

                        var path = goodFiles.First();
                        var pngType = GetType(path);

                        if (pngType == PngType.AIS_Housing)
                        {
                            LoadHousing(path);
                            PlaySound(SystemSE.ok_s);
                        }
                    }
                }
                else if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "Studio"))
                {
                    var goodFiles2 = goodFiles
                        .Select(x => new KeyValuePair<string, PngType>(x, GetType(x))).ToList();
                    var scenes = goodFiles2.Where(x => x.Value == PngType.KStudio).ToList();
                    var cards = goodFiles2.Where(x => x.Value == PngType.AIS_Chara).ToList();

                    StartCoroutine(StudioLoadCoroutine(scenes, cards, import));
                }
            }
            catch (Exception ex)
            {
                PrintError(ex);
                PlaySound(SystemSE.ok_l);
            }
        }

        private static void PlaySound(SystemSE se)
        {
            if (!Singleton<CustomBase>.IsInstance()) return;
            Illusion.Game.Utils.Sound.Play(se);
        }

        private void PrintError(Exception ex)
        {
            Logger.Log(BepInEx.Logging.LogLevel.Message, $"Character load failed: {ex.Message}");
            Logger.Log(BepInEx.Logging.LogLevel.Error, $"[DragAndDrop] {ex}");
            PlaySound(SystemSE.ok_l);
        }

        private IEnumerator StudioLoadCoroutine(List<KeyValuePair<string, PngType>> scenes, List<KeyValuePair<string, PngType>> cards, bool import)
        {
            // Load scenes
            if (scenes.Count > 0)
            {
                if (!import)
                {
                    if (scenes.Count > 1)
                        Logger.Log(BepInEx.Logging.LogLevel.Message, "Warning: Only the first scene will be loaded. If you want to import multiple scenes, hold Shift while dropping them.");

                    try
                    {
                        LoadScene(scenes[0].Key, false);
                    }
                    catch (Exception ex)
                    {
                        PrintError(ex);
                    }

                    yield return null;
                }
                else
                {
                    foreach (var scene in scenes)
                    {
                        try
                        {
                            LoadScene(scene.Key, true);
                        }
                        catch (Exception ex)
                        {
                            PrintError(ex);
                        }

                        yield return null;
                    }
                }
            }

            // Load characters
            var first = true;
            foreach (var card in cards)
            {
                try
                {
                    LoadSceneCharacter(card.Key, !first || import);
                    first = false;
                }
                catch (Exception ex)
                {
                    PrintError(ex);
                }

                yield return new WaitForEndOfFrame();
            }

            PlaySound(SystemSE.ok_s);
        }

        private void LoadScene(string path, bool import)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (import)
                Singleton<Studio.Studio>.Instance.ImportScene(path);
            else
                StartCoroutine(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(path));
        }

        private void LoadSceneCharacter(string path, bool forceAdd)
        {
            var charaCtrl = new ChaFileControl();
            if (!charaCtrl.LoadCharaFile(path, 1, true, true)) return;

            if (!forceAdd)
            {
                if (Studio.Studio.GetCtrlInfo(Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode) is OCIChar)
                {
                    var anySexChanged = false;
                    var anyChanged = false;
                    foreach (var oCIChar in Singleton<GuideObjectManager>.Instance.selectObjectKey
                        .Select(Studio.Studio.GetCtrlInfo)
                        .OfType<OCIChar>())
                    {
                        if (oCIChar.oiCharInfo.sex != charaCtrl.parameter.sex)
                            anySexChanged = true;

                        charaCtrl.parameter.sex = (byte)oCIChar.oiCharInfo.sex;

                        oCIChar.ChangeChara(path);

                        anyChanged = true;
                    }

                    if (anySexChanged)
                        Logger.Log(BepInEx.Logging.LogLevel.Message, "Warning: The character's sex has been changed to match the selected character(s).");

                    // Prevent adding a new character if we already replaced an existing one
                    if (anyChanged)
                        return;
                }
            }

            if (charaCtrl.parameter.sex == 0)
                Singleton<Studio.Studio>.Instance.AddMale(path);
            else if (charaCtrl.parameter.sex == 1)
                Singleton<Studio.Studio>.Instance.AddFemale(path);
        }

        private static PngType GetPngType(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                string str;
                try
                {
                    PngFile.SkipPng(binaryReader);
                    binaryReader.ReadInt32();
                    str = binaryReader.ReadString();
                }
                catch (EndOfStreamException)
                {
                    return PngType.Unknown;
                }

                if (str.StartsWith(CharaToken, StringComparison.OrdinalIgnoreCase))
                    return PngType.AIS_Chara;

                if (str.StartsWith(HouseToken, StringComparison.OrdinalIgnoreCase))
                    return PngType.AIS_Housing;

                try
                {
                    if (fileStream.TryReadUntilSequence(StudioTokenBytes))
                        return PngType.KStudio;
                }
                catch (EndOfStreamException)
                {
                }
            }

            return PngType.Unknown;
        }

        private void LoadMakerCharacter(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var lf = UseMakerLoadPreferences.Value ? GetLoadFlags() : null;

            var customBase = Singleton<CustomBase>.Instance;
            var chaCtrl = customBase.chaCtrl;
            var chaFile = chaCtrl.chaFile;

            var originalSex = chaCtrl.sex;

            if (lf != null)
            {
                chaFile.LoadFileLimited(path, chaCtrl.sex, lf.Face, lf.Body, lf.Hair, lf.Parameters, lf.Clothes);
                if (chaFile.GetLastErrorCode() != 0)
                    throw new IOException("LoadFileLimited failed.");
            }
            else
            {
                if (!chaFile.LoadCharaFile(path, chaCtrl.sex))
                    throw new IOException("LoadCharaFile failed.");
            }

            if (chaFile.parameter.sex != originalSex)
            {
                chaFile.parameter.sex = originalSex;
                Logger.Log(BepInEx.Logging.LogLevel.Message, "Warning: The character's sex has been changed to match the editor mode.");
            }
            chaCtrl.ChangeNowCoordinate();

            if (lf != null)
                chaCtrl.Reload(!lf.Clothes, !lf.Face, !lf.Hair, !lf.Body);
            else
                chaCtrl.Reload();

            customBase.updateCustomUI = true;

            for (var i = 0; i < 20; i++)
            {
                customBase.ChangeAcsSlotName(i);
            }
            customBase.SetUpdateToggleSetting();
            customBase.forceUpdateAcsList = true;
        }

        private void LoadHousing(string path)
        {
            var craftInfo = CraftInfo.LoadStatic(path);
            var housingID = Singleton<CraftScene>.Instance.HousingID;
            if (Singleton<Manager.Housing>.Instance.dicAreaInfo.TryGetValue(housingID, out var areaInfo))
            {
                if (Singleton<Manager.Housing>.Instance.dicAreaSizeInfo.TryGetValue(areaInfo.size, out var areaSizeInfo))
                {
                    if (areaSizeInfo.compatibility.Contains(
                        Singleton<Manager.Housing>.Instance.GetSizeType(craftInfo.AreaNo)))
                    {
                        Singleton<Selection>.Instance.SetSelectObjects(null);
                        Singleton<Housing.UndoRedoManager>.Instance.Clear();
                        Singleton<Manager.Housing>.Instance.Load(path, true, true);
                        Singleton<Manager.Housing>.Instance.CheckOverlap();
                        FindObjectsOfType<UICtrl>().First(i => i.IsInit).ListUICtrl.UpdateUI();
                    }
                }
            }
        }

        private enum PngType
        {
            Unknown,
            AIS_Chara,
            KStudio,
            AIS_Housing
        }

        private static LoadFlags GetLoadFlags()
        {
            var ccw = FindObjectsOfType<CustomCharaWindow>()
                .FirstOrDefault(i => i.onClick01 == null && i.onClick03 != null);
            if (ccw == null) return new LoadFlags();

            if (!(typeof(CustomCharaWindow)
                .GetField("tglLoadOption", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(ccw) is Toggle[] opt))
                return new LoadFlags();

            return new LoadFlags
            {
                Face = opt[0].isOn,
                Body = opt[1].isOn,
                Hair = opt[2].isOn,
                Clothes = opt[3].isOn,
                Parameters = opt[4].isOn
            };
        }

        private class LoadFlags
        {
            public bool Clothes;
            public bool Face;
            public bool Hair;
            public bool Body;
            public bool Parameters;
        }
    }
}