﻿/* DMagic Orbital Science - AutoSave
 * Creates a backup save file upon initial game load
 *
 * Copyright (c) 2014, DMagic
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation and/or other materials 
 * provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;


namespace AutoSave
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class AutoSave : MonoBehaviour
    {
        public void Start()
        {
            GameEvents.onGameSceneLoadRequested.Add(saveBackup);
        }        
        
        public void saveBackup(GameScenes scene)
        {
            scene = HighLogic.LoadedScene;
            if (scene == GameScenes.MAINMENU)
            {
                //This doesn't seem to like combining three strings into one path for some reason, so I combine two strings twice, I'm guessing the "saves" string needs some kind of / \.
                string activeDirectory = Path.Combine(Path.Combine(new System.IO.DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "saves"), HighLogic.fetch.GameSaveFolder);
                System.IO.FileInfo oldBackup = new System.IO.FileInfo(Path.Combine(activeDirectory, "Persistent Backup.sfs"));
                if (oldBackup.Exists)
                {
                    System.IO.FileInfo newBackup = new System.IO.FileInfo(Path.Combine(activeDirectory, "Persistent Backup Most Recent.sfs"));
                    if (newBackup.Exists) newBackup.Replace(Path.Combine(activeDirectory, "Persistent Backup.sfs"), Path.Combine(activeDirectory, "Persistent Backup Most Recent.sfs"));
                    var save = GamePersistence.SaveGame("Persistent Backup Most Recent", HighLogic.fetch.GameSaveFolder, 0);
                    GameEvents.onGameSceneLoadRequested.Remove(saveBackup);
                }
                else
                {
                    var save = GamePersistence.SaveGame("Persistent Backup", HighLogic.fetch.GameSaveFolder, 0);
                    GameEvents.onGameSceneLoadRequested.Remove(saveBackup);
                }
            }
        }

    }
}