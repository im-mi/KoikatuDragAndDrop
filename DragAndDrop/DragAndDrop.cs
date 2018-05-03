using B83.Win32;
using BepInEx;
using ChaCustom;
using Illusion.Game;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragAndDrop
{
    [BepInPlugin(GUID: "com.immi.koikatu.draganddrop", Name: "Drag and Drop", Version: "1.1")]
    class DragAndDrop : BaseUnityPlugin
    {
        private UnityDragAndDropHook hook;

        private void OnEnable()
        {
            hook = new UnityDragAndDropHook();
            hook.InstallHook();
            hook.OnDroppedFiles += OnDroppedFiles;
        }

        private void OnDisable()
        {
            hook.UninstallHook();
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
                    BepInLogger.Log($"Character load failed", true);
                    BepInLogger.Log(ex.ToString());
                    Utils.Sound.Play(SystemSE.ok_l);
                }
            }
        }

        private void LoadCharacter(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            var chaFile = chaCtrl.chaFile;

            if (!chaFile.LoadCharaFile(path, chaCtrl.sex))
                throw new IOException();
            chaCtrl.ChangeCoordinateType(true);
            chaCtrl.Reload();
            Singleton<CustomBase>.Instance.updateCustomUI = true;
            Singleton<CustomHistory>.Instance.Add5(chaCtrl, chaCtrl.Reload, false, false, false, false);
        }
    }
}
