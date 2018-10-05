using System;
using System.Collections.Generic;
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

namespace DragAndDrop
{
    [BepInPlugin("com.immi.koikatu.draganddrop", "Drag and Drop", "1.2")]
    internal class DragAndDrop : BaseUnityPlugin
    {
        private const string charaToken = "【KoiKatuChara】";
        private const string studioToken = "【KStudio】";

        private UnityDragAndDropHook _hook;

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
            var path = aFiles[0];
            if (path == null)
                return;
            var extension = Path.GetExtension(path).ToLower();
            if (extension != ".png")
            {
                Logger.Log(LogLevel.Message, $"Unsupported file type {extension}. Cards are only in .png format!");
                return;
            }

            if (Singleton<Scene>.IsInstance())
                try
                {
                    var pngType = CheckPngType(path);
                    if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "CustomScene"))
                    {
                        if (Singleton<CustomBase>.IsInstance() && pngType == PngType.KoikatuChara)
                        {
                            LoadCharacter(path);
                            Utils.Sound.Play(SystemSE.ok_s);
                        }
                    }
                    else if (Singleton<Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "Studio"))
                    {
                        if (pngType == PngType.KoikatuChara)
                        {
                            AddChara(path);
                            Utils.Sound.Play(SystemSE.ok_s);
                        }
                        else if (pngType == PngType.KStudio)
                        {
                            LoadScene(path);
                            Utils.Sound.Play(SystemSE.ok_s);
                        }
                    }
                    if (pngType == PngType.Unknown)
                        Utils.Sound.Play(SystemSE.ok_l);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Message, $"Character load failed - {ex.Message}");
                    Logger.Log(LogLevel.Error, $"[DragAndDrop] {ex}");
                    Utils.Sound.Play(SystemSE.ok_l);
                }
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
            if (charaCtrl.LoadCharaFile(path, 1, true, true))
            {
                var ocichar = Studio.Studio.GetCtrlInfo(Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode) as OCIChar;
                if (ocichar != null && charaCtrl.parameter.sex == ocichar.sex)
                {
                    var array = (from v in Singleton<GuideObjectManager>.Instance.selectObjectKey
                        select Studio.Studio.GetCtrlInfo(v) as OCIChar
                        into v
                        where v != null
                        where v.oiCharInfo.sex == (int) charaCtrl.parameter.sex
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
        }

        private static PngType CheckPngType(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
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
                        if (binaryReader.ReadString() == "【KoiKatuChara】")
                            return PngType.KoikatuChara;
                    }
                    catch (EndOfStreamException)
                    {
                    }
                    var byteCount = Encoding.UTF8.GetByteCount("【KStudio】");
                    binaryReader.BaseStream.Seek(-(long) byteCount, SeekOrigin.End);
                    try
                    {
                        if (Encoding.UTF8.GetString(binaryReader.ReadBytes(byteCount)) == "【KStudio】")
                            return PngType.KStudio;
                    }
                    catch (EndOfStreamException)
                    {
                    }
                }
            }
            return PngType.Unknown;
        }

        private void LoadCharacter(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            var chaFile = chaCtrl.chaFile;

            var originalSex = chaCtrl.sex;
            if (!chaFile.LoadCharaFile(path, chaCtrl.sex))
                throw new IOException();
            if (chaFile.parameter.sex != originalSex)
            {
                chaFile.parameter.sex = originalSex;
                Logger.Log(LogLevel.Message, "Warning: The character's sex has been altered to match the editor mode.");
            }
            chaCtrl.ChangeCoordinateType(true);
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
    }
}