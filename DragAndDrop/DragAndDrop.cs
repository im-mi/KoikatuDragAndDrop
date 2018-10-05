using System;
using System.Collections.Generic;
using System.IO;
using B83.Win32;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using Illusion.Game;

namespace DragAndDrop
{
    [BepInPlugin("com.immi.koikatu.draganddrop", "Drag and Drop", "1.2")]
    internal class DragAndDrop : BaseUnityPlugin
    {
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
            if (aFiles.Count == 0) return;
            var path = aFiles[0];
            if (path == null) return;

            if (Singleton<CustomBase>.IsInstance())
            {
                try
                {
                    LoadCharacter(path);
                    Utils.Sound.Play(SystemSE.ok_s);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Message, $"Character load failed - {ex.Message}");
                    Logger.Log(LogLevel.Error, $"[DragAndDrop] {ex}");
                    Utils.Sound.Play(SystemSE.ok_l);
                }
            }
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
    }
}