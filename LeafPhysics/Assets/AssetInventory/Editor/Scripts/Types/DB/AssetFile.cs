﻿using System;
using SQLite;

namespace AssetInventory
{
    [Serializable]
    public class AssetFile : TreeElement
    {
        public enum PreviewOptions
        {
            Supplied = 0,
            None = 1,
            Redo = 2,
            Custom = 3
        }

        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int AssetId { get; set; }
        [Indexed] public string Guid { get; set; }
        [Indexed] public string Path { get; set; }
        [Indexed] public string FileName { get; set; }
        public string SourcePath { get; set; }
        [Indexed] public string Type { get; set; }
        public string PreviewFile { get; set; }
        public PreviewOptions PreviewState { get; set; }
        public string DominantColor { get; set; }
        public string DominantColorGroup { get; set; }
        public long Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Length { get; set; }

        public string ShortPath => !string.IsNullOrEmpty(Path) && Path.StartsWith("Assets/") ? Path.Substring(7) : Path;

        public override string ToString()
        {
            return $"Asset File '{Path}'";
        }
    }
}