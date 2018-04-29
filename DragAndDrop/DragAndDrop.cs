using B83.Win32;
using BepInEx;
using ChaCustom;
using Illusion.Game;
using System;
using System.Collections.Generic;

namespace DragAndDrop
{
    [BepInPlugin(GUID: "com.immi.koikatu.draganddrop", Name: "Drag and Drop", Version: "1.0.0")]
    class DragAndDrop : BaseUnityPlugin
    {
        UnityDragAndDropHook hook;

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

            try
            {
                LoadCharacter(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when loading character: {ex}");
            }
        }

        private void LoadCharacter(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            Utils.Sound.Play(SystemSE.ok_s);
            var chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            var chaFile = chaCtrl.chaFile;
            bool flag6 = true; // face
            bool flag7 = true; // body
            bool flag8 = true; // hair
            bool parameter = true; // character
            bool flag9 = true; // coordinates
            chaFile.LoadFileLimited(path, chaCtrl.sex, flag6, flag7, flag8, parameter, flag9);
            chaCtrl.ChangeCoordinateType(true);
            chaCtrl.Reload(!flag9, !flag6 && !flag9, !flag8, !flag7);
            Singleton<CustomBase>.Instance.updateCustomUI = true;
            Singleton<CustomHistory>.Instance.Add5(chaCtrl, chaCtrl.Reload, !flag9, !flag6 && !flag9, !flag8, !flag7);
        }
    }
}
