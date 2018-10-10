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
    [BepInPlugin("com.immi.koikatu.draganddrop", "Drag and Drop", "1.2.1")]
    internal class DragAndDrop : BaseUnityPlugin
    {
        private const string CharaToken = "【KoiKatuChara】";
        private const string StudioToken = "【KStudio】";

        private UnityDragAndDropHook _hook;
        private static readonly byte[] StudioTokenBytes = Encoding.UTF8.GetBytes(StudioToken);

        [DisplayName("Use maker load preferences")]
        [Description("You can partially load the dragged character by changing settings under the \"Load character\" list in maker")]
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

                Logger.Log(LogLevel.Error, $"Unsupported file type {extension}. Only .png files are supported.");
                return false;
            }).ToList();

            if (goodFiles.Count == 0)
                return;

            PngType GetType(string path)
            {
                var pngType = CheckPngType(path);

                if (pngType == PngType.Unknown)
                {
                    Logger.Log(LogLevel.Error, "Unknown file format.");
                    Utils.Sound.Play(SystemSE.ok_l);
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
                            Logger.Log(LogLevel.Warning, "Only the first card will be loaded.");

                        var path = goodFiles.First();
                        var pngType = GetType(path);

                        if (pngType == PngType.KoikatuChara)
                        {
                            LoadCharacter(path);
                            Utils.Sound.Play(SystemSE.ok_s);
                        }
                        else if (pngType == PngType.KStudio)
                        {
                            Logger.Log(LogLevel.Error, "Scene files cannot be loaded in the character maker.");
                            Utils.Sound.Play(SystemSE.ok_l);
                        }
                    }
                }
                else if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "Studio"))
                {
                    var goodFiles2 = goodFiles.Select(x => new KeyValuePair<string, PngType>(x, GetType(x))).ToList();
                    var scenes = goodFiles2.Where(x => x.Value == PngType.KStudio).ToList();

                    var cards = goodFiles2.Where(x => x.Value == PngType.KoikatuChara).ToList();

                    StartCoroutine(StudioLoadCoroutine(scenes, cards));
                }
            }
            catch (Exception ex)
            {
                PrintError(ex);
            }
        }

        private static void PrintError(Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Character load failed: {ex.Message}");
            Logger.Log(LogLevel.Error, $"[DragAndDrop] {ex}");
            Utils.Sound.Play(SystemSE.ok_l);
        }

        private IEnumerator StudioLoadCoroutine(List<KeyValuePair<string, PngType>> scenes, List<KeyValuePair<string, PngType>> cards)
        {
            if (scenes.Count > 0)
            {
                if (scenes.Count > 1)
                    Logger.Log(LogLevel.Warning, "Only the first scene will be loaded.");

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

            foreach (var card in cards)
            {
                try
                {
                    AddChara(card.Key);
                }
                catch (Exception ex)
                {
                    PrintError(ex);
                }

                yield return new WaitForEndOfFrame();
            }

            Utils.Sound.Play(SystemSE.ok_s);
        }
        
        private void LoadScene(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            StartCoroutine(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(path));
        }

        private static void AddChara(string path)
        {
            var charaCtrl = new ChaFileControl();
            if (!charaCtrl.LoadCharaFile(path, 1, true, true)) return;

            var ocichar = Studio.Studio.GetCtrlInfo(Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode) as OCIChar;
            if (ocichar != null && charaCtrl.parameter.sex == ocichar.sex)
            {
                var array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                             select Studio.Studio.GetCtrlInfo(v) as OCIChar
                    into v
                             where v != null
                             where v.oiCharInfo.sex == (int)charaCtrl.parameter.sex
                             select v).ToArray();
                var i = 0;
                var num = array.Length;
                while (i < num)
                {
                    array[i].ChangeChara(path);
                    i++;
                }
                return;
            }
            if (charaCtrl.parameter.sex == 0)
            {
                Singleton<Studio.Studio>.Instance.AddMale(path);
                return;
            }
            if (charaCtrl.parameter.sex == 1)
                Singleton<Studio.Studio>.Instance.AddFemale(path);
        }

        private static PngType CheckPngType(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                    if (Utilities.FindPosition(fileStream, StudioTokenBytes) > 0)
                        return PngType.KStudio;
                }
                catch (EndOfStreamException)
                {
                }
            }
            return PngType.Unknown;
        }

        private void LoadCharacter(string path)
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
                Logger.Log(LogLevel.Warning, "The character's sex has been changed to match the editor mode.");
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
            var lf = new LoadFlags();

            foreach (var cfw in FindObjectsOfType<CustomFileWindow>())
            {
                if (cfw.fwType != CustomFileWindow.FileWindowType.CharaLoad)
                    continue;

                lf.Body = cfw.tglChaLoadBody.isOn;
                lf.Clothes = cfw.tglChaLoadCoorde.isOn;
                lf.Hair = cfw.tglChaLoadHair.isOn;
                lf.Face = cfw.tglChaLoadFace.isOn;
                lf.Parameters = cfw.tglChaLoadParam.isOn;

                break;
            }

            return lf;
        }

        private class LoadFlags
        {
            public bool Clothes;
            public bool Face;
            public bool Hair;
            public bool Body;
            public bool Parameters;

            public LoadFlags()
            {
                Body = Clothes = Hair = Face = Parameters = true;
            }
        }
    }
}
