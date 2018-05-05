using B83.Win32;
using BepInEx;
using ChaCustom;
using Illusion.Game;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
                    BepInLogger.Log("Character load failed", true);
                    BepInLogger.Log(ex.ToString());
                    Utils.Sound.Play(SystemSE.ok_l);
                }
            }
        }

        private void LoadCharacter(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            LoadFlags lf = GetLoadFlags();

            var chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            var chaFile = chaCtrl.chaFile;

            var originalSex = chaCtrl.sex;
            chaFile.LoadFileLimited(path, chaCtrl.sex, lf.face, lf.body, lf.hair, lf.parameters, lf.clothes);

            if (chaFile.GetLastErrorCode() != 0)
                throw new IOException();

            if (chaFile.parameter.sex != originalSex)
            {
                chaFile.parameter.sex = originalSex;
                BepInLogger.Log("Warning: The character's sex has been altered to match the editor mode.", true);
            }
            chaCtrl.ChangeCoordinateType(true);

            chaCtrl.Reload(!lf.clothes, !lf.face, !lf.hair, !lf.body);

            Singleton<CustomBase>.Instance.updateCustomUI = true;
            Singleton<CustomHistory>.Instance.Add5(chaCtrl, chaCtrl.Reload, false, false, false, false);
        }

        private LoadFlags GetLoadFlags()
        {
            var lf = new LoadFlags();
            lf.body = lf.clothes = lf.hair = lf.face = lf.parameters = true;

            foreach (CustomFileWindow cfw in GameObject.FindObjectsOfType<CustomFileWindow>())
            {
                if (cfw.fwType == CustomFileWindow.FileWindowType.CharaLoad)
                {
                    lf.body = cfw.tglChaLoadBody.isOn;
                    lf.clothes = cfw.tglChaLoadCoorde.isOn;
                    lf.hair = cfw.tglChaLoadHair.isOn;
                    lf.face = cfw.tglChaLoadFace.isOn;
                    lf.parameters = cfw.tglChaLoadParam.isOn;

                    break;
                }
            }

            return lf;
        }
    }

    struct LoadFlags
    {
        public bool clothes;
        public bool face;
        public bool hair;
        public bool body;
        public bool parameters;
    }
}
