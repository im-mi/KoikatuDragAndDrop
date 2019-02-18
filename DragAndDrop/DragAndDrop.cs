using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using B83.Win32;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using Illusion.Game;
using Manager;
using Studio;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace DragAndDrop
{
    [BepInPlugin("com.immi.koikatu.draganddrop", "Drag and Drop", "1.2")]
    internal class DragAndDrop : BaseUnityPlugin
    {
        private const string CharaToken = "【KoiKatuChara】";
        private const string StudioToken = "【KStudio】";

        private UnityDragAndDropHook _hook;
        private static readonly byte[] StudioTokenBytes = Encoding.UTF8.GetBytes(StudioToken);

        [DisplayName("Use maker load preferences")]
        [Description("Enables partial character loading using the options in the character maker's \"Load character\" menu.")]
        public ConfigWrapper<bool> UseMakerLoadPreferences { get; private set; }

        protected void Start()
        {
            UseMakerLoadPreferences = new ConfigWrapper<bool>("useMakerPrefs", this, true);
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
            if (aFiles.Count == 0)
                return;

            if (!Singleton<Scene>.IsInstance())
                return;

            var goodFiles = aFiles.Where(f =>
            {
                if (string.IsNullOrEmpty(f)) return false;

                var extension = Path.GetExtension(f).ToLower();
                if (extension == ".png") return true;

                Logger.Log(LogLevel.Message, $"Unsupported file type {extension}. Only .png files are supported.");
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
                    Logger.Log(LogLevel.Message, "Unknown file format.");
                    PlaySound(SystemSE.ok_l);
                }

                return pngType;
            }

            try
            {
                if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                {
                    if (Singleton<CustomBase>.IsInstance())
                    {
                        if (goodFiles.Count > 1)
                            Logger.Log(LogLevel.Message, "Warning: Only the first card will be loaded.");

                        var path = goodFiles.First();
                        var pngType = GetType(path);

                        if (pngType == PngType.KoikatuChara)
                        {
                            LoadMakerCharacter(path);
                            PlaySound(SystemSE.ok_s);
                        }
                        else if (pngType == PngType.KStudio)
                        {
                            Logger.Log(LogLevel.Message, "Scene files cannot be loaded in the character maker.");
                            PlaySound(SystemSE.ok_l);
                        }
                    }
                }
                else if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "Studio"))
                {
                    var goodFiles2 = goodFiles
                        .Select(x => new KeyValuePair<string, PngType>(x, GetType(x))).ToList();
                    var scenes = goodFiles2.Where(x => x.Value == PngType.KStudio).ToList();
                    var cards = goodFiles2.Where(x => x.Value == PngType.KoikatuChara).ToList();

                    StartCoroutine(StudioLoadCoroutine(scenes, cards));
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
            Utils.Sound.Play(se);
        }

        private static void PrintError(Exception ex)
        {
            Logger.Log(LogLevel.Message, $"Character load failed: {ex.Message}");
            Logger.Log(LogLevel.Error, $"[DragAndDrop] {ex}");
            PlaySound(SystemSE.ok_l);
        }

        private IEnumerator StudioLoadCoroutine(List<KeyValuePair<string, PngType>> scenes, List<KeyValuePair<string, PngType>> cards)
        {
            if (scenes.Count > 0)
            {
                if (scenes.Count > 1)
                    Logger.Log(LogLevel.Message, "Warning: Only the first scene will be loaded.");

                var scene = scenes[0];

                try
                {
                    LoadScene(scene.Key);
                }
                catch (Exception ex)
                {
                    PrintError(ex);
                }

                yield return new WaitForEndOfFrame();
            }

            var first = true;
            foreach (var card in cards)
            {
                try
                {
                    LoadSceneCharacter(card.Key, !first);
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

        private void LoadScene(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            StartCoroutine(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(path));
        }

        private static void LoadSceneCharacter(string path, bool forceAdd)
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
                        Logger.Log(LogLevel.Message, "Warning: The character's sex has been changed to match the selected character(s).");

                    // Prevent adding a new character if we alraedy replaced an existing one
                    if(anyChanged)
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
                try
                {
                    PngFile.SkipPng(binaryReader);
                    binaryReader.ReadInt32();
                }
                catch (EndOfStreamException)
                {
                    return PngType.Unknown;
                }
                try
                {
                    if (binaryReader.ReadString() == CharaToken)
                        return PngType.KoikatuChara;
                }
                catch (EndOfStreamException)
                {
                }

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

            var chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
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
                Logger.Log(LogLevel.Message, "Warning: The character's sex has been changed to match the editor mode.");
            }
            chaCtrl.ChangeCoordinateType(true);

            if (lf != null)
                chaCtrl.Reload(!lf.Clothes, !lf.Face, !lf.Hair, !lf.Body);
            else
                chaCtrl.Reload();

            Singleton<CustomBase>.Instance.updateCustomUI = true;
            Singleton<CustomHistory>.Instance.Add5(chaCtrl, chaCtrl.Reload, false, false, false, false);
        }

        private enum PngType
        {
            Unknown,
            KoikatuChara,
            KStudio
        }

        private static LoadFlags GetLoadFlags()
        {
            var cfw = FindObjectsOfType
                <CustomFileWindow>()
                .FirstOrDefault(i => i.fwType == CustomFileWindow.FileWindowType.CharaLoad);
            if (cfw == null) return new LoadFlags();

            return new LoadFlags
            {
                Body = cfw.tglChaLoadBody.isOn,
                Clothes = cfw.tglChaLoadCoorde.isOn,
                Hair = cfw.tglChaLoadHair.isOn,
                Face = cfw.tglChaLoadFace.isOn,
                Parameters = cfw.tglChaLoadParam.isOn
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
