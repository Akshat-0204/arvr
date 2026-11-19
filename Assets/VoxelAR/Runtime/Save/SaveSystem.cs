using System;
using System.IO;
using UnityEngine;
using VoxelAR.Voxel;

namespace VoxelAR.Save
{
    /// <summary>
    /// Slot-based save files at {root}/slot{n}.json. Writes go to .tmp first, then replace,
    /// so a crash mid-write never leaves a half-written save. IO errors return false, never throw.
    /// </summary>
    public class SaveSystem
    {
        public const int SlotCount = 3;

        public string RootDirectory { get; }
        public string LastError { get; private set; }

        public SaveSystem(string rootDirectory = null)
        {
            RootDirectory = rootDirectory ?? Path.Combine(Application.persistentDataPath, "builds");
        }

        public string PathFor(int slot) => Path.Combine(RootDirectory, $"slot{slot}.json");

        public bool Exists(int slot) => ValidSlot(slot) && File.Exists(PathFor(slot));

        public bool Save(int slot, VoxelGrid g)
        {
            LastError = null;
            if (!ValidSlot(slot)) { LastError = $"Invalid slot {slot}."; return false; }
            try
            {
                Directory.CreateDirectory(RootDirectory);
                string path = PathFor(slot);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, VoxelSerializer.ToJson(g));
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                LastError = "Save failed: " + e.Message;
                Debug.LogWarning("[SaveSystem] " + LastError);
                return false;
            }
        }

        public bool Load(int slot, VoxelGrid g, out string error)
        {
            error = null;
            if (!ValidSlot(slot)) error = $"Invalid slot {slot}.";
            else if (!File.Exists(PathFor(slot))) error = $"Slot {slot + 1} is empty.";
            else
            {
                try
                {
                    string json = File.ReadAllText(PathFor(slot));
                    if (VoxelSerializer.TryFromJson(json, g, out error)) { LastError = null; return true; }
                }
                catch (Exception e)
                {
                    error = "Load failed: " + e.Message;
                }
            }
            LastError = error;
            return false;
        }

        private static bool ValidSlot(int slot) => slot >= 0 && slot < SlotCount;
    }
}
