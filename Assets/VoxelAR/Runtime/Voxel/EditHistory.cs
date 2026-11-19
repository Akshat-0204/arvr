using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelAR.Voxel
{
    public struct EditCommand
    {
        public Vector3Int Cell;
        public byte Before;
        public byte After;

        public EditCommand(Vector3Int cell, byte before, byte after)
        {
            Cell = cell;
            Before = before;
            After = after;
        }
    }

    public enum ToolMode { Build, Break }

    /// <summary>
    /// Undoable edits on a <see cref="VoxelGrid"/>. Each step is one or more commands
    /// (a single block edit, or a whole-grid clear).
    /// </summary>
    public class EditHistory
    {
        private readonly VoxelGrid grid;
        private readonly int depth;
        private readonly LinkedList<EditCommand[]> steps = new LinkedList<EditCommand[]>();

        public event Action Changed;

        public EditHistory(VoxelGrid grid, int depth = 50)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth));
            this.depth = depth;
        }

        public VoxelGrid Grid => grid;
        public bool CanUndo => steps.Count > 0;
        public int UndoCount => steps.Count;

        /// <summary>Sets a cell. Recorded only if the grid actually changed.</summary>
        public bool Apply(Vector3Int cell, byte id)
        {
            byte before = grid.Get(cell);
            if (!grid.Set(cell, id)) return false;
            Push(new[] { new EditCommand(cell, before, id) });
            return true;
        }

        /// <summary>Clears the grid as a single undoable step. No-op on an empty grid.</summary>
        public bool Clear()
        {
            if (grid.Count == 0) return false;
            var snapshot = new List<EditCommand>(grid.Count);
            foreach (var (cell, id) in grid.All())
                snapshot.Add(new EditCommand(cell, id, 0));
            grid.Clear();
            Push(snapshot.ToArray());
            return true;
        }

        public bool Undo()
        {
            if (steps.Count == 0) return false;
            var step = steps.Last.Value;
            steps.RemoveLast();
            for (int i = step.Length - 1; i >= 0; i--)
                grid.Set(step[i].Cell, step[i].Before);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Drops all undo steps without touching the grid (e.g. after loading a save).</summary>
        public void ResetHistory()
        {
            if (steps.Count == 0) return;
            steps.Clear();
            Changed?.Invoke();
        }

        private void Push(EditCommand[] step)
        {
            steps.AddLast(step);
            while (steps.Count > depth) steps.RemoveFirst();
            Changed?.Invoke();
        }
    }
}
