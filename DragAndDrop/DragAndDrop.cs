using B83.Win32;
using BepInEx;
using ChaCustom;
using Illusion.Game;
using System;
using System.Collections.Generic;

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
            bool loadFaceEnabled = true;
            bool loadBodyEnabled = true;
            bool loadHairEnabled = true;
            bool loadCharacterEnabled = true;
            bool loadCoordinateEnabled = true;
            Utils.Sound.Play(SystemSE.ok_s);
            var chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            var chaFile = chaCtrl.chaFile;
            chaFile.LoadFileLimited(path, chaCtrl.sex,
                loadFaceEnabled, loadBodyEnabled, loadHairEnabled, loadCharacterEnabled, loadCoordinateEnabled);
            chaCtrl.ChangeCoordinateType(true);
            chaCtrl.Reload(!loadCoordinateEnabled,
                !loadFaceEnabled && !loadCoordinateEnabled, !loadHairEnabled, !loadBodyEnabled);
            Singleton<CustomBase>.Instance.updateCustomUI = true;
            Singleton<CustomHistory>.Instance.Add5(chaCtrl, chaCtrl.Reload,
                !loadCoordinateEnabled, !loadFaceEnabled && !loadCoordinateEnabled, !loadHairEnabled, !loadBodyEnabled);
        }
    }
}
