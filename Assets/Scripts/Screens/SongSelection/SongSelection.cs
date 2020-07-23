﻿using StyleStar;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class SongSelectionMovement
{
    public static void SongMovementBefore(int currentSongIndex, int currentFolderIndex, int currentLevelIndex, Vector2 animationMovement)
    {
        CurrentSongIndex = currentSongIndex;
        CurrentFolderIndex = currentFolderIndex;
        CurrentLevelIndex = currentLevelIndex;
        AnimationMovement = animationMovement*ScrollSpeed;
    }

    public static void SongMovementAfter(int currentSongIndex, int currentFolderIndex,
        int currentLevelIndex)
    {
        if (CurrentSongIndex != currentSongIndex || CurrentFolderIndex != currentFolderIndex ||
            CurrentLevelIndex != currentLevelIndex)
            AnimationFrameCount = 0;
    }

    private static int CurrentSongIndex { get; set; } = 0;
    private static int CurrentFolderIndex { get; set; } = 0;
    private static int CurrentLevelIndex { get; set; } = 0;
    private static int AnimationFrameCount{ get; set; } = 10;

    public static Vector2 AnimationMovement { get; set; } = new Vector2(0, 0); // direction to move the 
    public static bool IsAnimating() { return AnimationFrameCount < MaxNumTransitionFrames; }
    public static void IncrementFrame() { AnimationFrameCount += 1; }

    // NOTE: ScrollSpeed * MaxNumTransitionFrames should be equal to 1 to maintain the current spacing of the menus.
    private const float ScrollSpeed = 0.1f;
    public const int MaxNumTransitionFrames = 10;

};

public static class SongSelection
{
    public static List<SongMetadata> Songlist { get; set; } = new List<SongMetadata>();
    public static List<FolderParams> FolderParams { get; set; } = new List<FolderParams>();

    public static int CurrentSongIndex { get; private set; } = 0;
    public static int CurrentFolderIndex { get; private set; } = 0;
    public static int CurrentLevelIndex { get; private set; } = 0;
    public static int SelectedFolderIndex { get; private set; } = -1;
    public static int SelectedLevelIndex { get; private set; } = -1;
    public static int CurrentSongLevelIndex { get; private set; } = 0; // Used to track difficulty switches

    public static void ImportSongs(string songsFolder)
    {
        if (!Directory.Exists(songsFolder))
            return;
        //DirectoryInfo di = new DirectoryInfo(songsFolder);
        //var folders = di.EnumerateDirectories();
        var folders = Directory.EnumerateDirectories(songsFolder);
        foreach (var folder in folders)
        {
            // If a folder contains an *.ssi file, use that to load charts
            // Otherwise, load each chart individually
            //var files = folder.EnumerateFiles();
            var files = Directory.EnumerateFiles(folder);
            var info = files.Where(f => f.EndsWith(Defines.InfoExtension));
            if (info != null && info.Count() > 0)
            {
                foreach (var file in info)
                {
                    Songlist.Add(new SongMetadata(file));
                }
            }
            else
            {
                var charts = files.Where(f => f.EndsWith(Defines.ChartExtension));
                if (charts != null && charts.Count() > 0)
                {
                    foreach (var chart in charts)
                        Songlist.Add(new SongMetadata(chart));
                }
            }
        }
        FolderParams.Add(new FolderParams() { Type = SortType.Title, Name = ConfigFile.GetLocalizedString("Sort_Title") });
        FolderParams.Add(new FolderParams() { Type = SortType.Artist, Name = ConfigFile.GetLocalizedString("Sort_Artist") });
        FolderParams.Add(new FolderParams() { Type = SortType.Level, Name = ConfigFile.GetLocalizedString("Sort_Level") });
    }

    public static void ScrollDown()
    {
        SongSelectionMovement.SongMovementBefore(CurrentSongIndex, CurrentFolderIndex, CurrentLevelIndex, -Globals.CardOffset);
        if (SelectedFolderIndex == -1)
            CurrentFolderIndex = CurrentFolderIndex < (FolderParams.Count - 1) ? ++CurrentFolderIndex : FolderParams.Count - 1;
        else if (FolderParams[SelectedFolderIndex].Type == SortType.Level && SelectedLevelIndex == -1)
            CurrentLevelIndex = CurrentLevelIndex < 9 ? ++CurrentLevelIndex : 9;
        else
        {
            CurrentSongIndex = CurrentSongIndex < (Songlist.Count - 1) ? ++CurrentSongIndex : Songlist.Count - 1;
        //UpdateMetaLabels(); // FIXME
        }
        SongSelectionMovement.SongMovementAfter(CurrentSongIndex, CurrentFolderIndex, CurrentLevelIndex);
    }

    public static void ScrollUp()
    {
        SongSelectionMovement.SongMovementBefore(CurrentSongIndex, CurrentFolderIndex, CurrentLevelIndex, Globals.CardOffset);
        if (SelectedFolderIndex == -1)
            CurrentFolderIndex = CurrentFolderIndex > 0 ? --CurrentFolderIndex : 0;
        else if (FolderParams[SelectedFolderIndex].Type == SortType.Level && SelectedLevelIndex == -1)
            CurrentLevelIndex = CurrentLevelIndex > 0 ? --CurrentLevelIndex : 0;
        else
        {
            CurrentSongIndex = CurrentSongIndex > 0 ? --CurrentSongIndex : 0;
        //UpdateMetaLabels(); // FIXME
        }
        SongSelectionMovement.SongMovementAfter(CurrentSongIndex, CurrentFolderIndex, CurrentLevelIndex);
    }

    public static void GoBack()
    {
        if (SelectedFolderIndex != -1 && FolderParams[SelectedFolderIndex].Type == SortType.Level && SelectedLevelIndex != -1)
        {
            SelectedLevelIndex = -1;
        }
        else
        {
            SelectedFolderIndex = -1;
            CurrentSongIndex = 0;
        }
    }

    public static bool Select()
    {
        if (SelectedFolderIndex == -1)
        {
            SelectedFolderIndex = CurrentFolderIndex;
            switch (FolderParams[SelectedFolderIndex].Type)
            {
                case SortType.Title:
                    Songlist = Songlist.OrderBy(x => x.Title).ToList();
                    //UpdateMetaLabels();
                    break;
                case SortType.Artist:
                    Songlist = Songlist.OrderBy(x => x.Artist).ToList();
                    //UpdateMetaLabels();
                    break;
                case SortType.Level:
                    SelectedLevelIndex = -1;
                    break;
                case SortType.Genre:
                    break;
                default:
                    break;
            }
        }
        else if (FolderParams[SelectedFolderIndex].Type == SortType.Level && SelectedLevelIndex == -1)
        {
            SelectedLevelIndex = CurrentLevelIndex;
            Songlist = Songlist.OrderBy(x => x.GetLevel(CurrentSongLevelIndex)).ToList();
            CurrentSongIndex = Songlist.FindLastIndex(x => x.GetLevel(CurrentSongLevelIndex) < (SelectedLevelIndex + 1)) + 1;
            //UpdateMetaLabels();
            if (CurrentSongIndex >= Songlist.Count)
                CurrentSongIndex--;
        }
        else
        {
            if (!IsSongReady())
                return false;

            return true;
        }

        return false;
    }

    public static void CycleDifficulty()
    {
        if (SelectedFolderIndex < 0)
            return;

        CurrentSongLevelIndex++;
        if (CurrentSongLevelIndex > 2)
            CurrentSongLevelIndex = 0;

        if (FolderParams[SelectedFolderIndex].Type == SortType.Level)
        {
            // Find out which song that's currently selected before reorganization
            var title = Songlist[CurrentSongIndex].Title;
            var artist = Songlist[CurrentSongIndex].Artist;

            // Reorganize list here
            Songlist = Songlist.OrderBy(x => x.GetLevel(CurrentSongLevelIndex)).ToList();
            CurrentSongIndex = Songlist.IndexOf(Songlist.First(x => x.Title == title && x.Artist == artist));
        }
    }

    private static bool IsSongReady()
    {
        if ((Songlist[CurrentSongIndex].IsMetadataFile && Songlist[CurrentSongIndex].ChildMetadata.FirstOrDefault(x => (int)x.Difficulty == CurrentSongLevelIndex) == null) ||
                (!Songlist[CurrentSongIndex].IsMetadataFile && (int)Songlist[CurrentSongIndex].Difficulty != CurrentSongLevelIndex))
            return false;

        return true;
    }

    public static SongMetadata GetSelectedMetadata()
    {
        if (Songlist[CurrentSongIndex].IsMetadataFile)
            return Songlist[CurrentSongIndex].ChildMetadata.FirstOrDefault(x => (int)x.Difficulty == CurrentSongLevelIndex);
        else
            return Songlist[CurrentSongIndex];
    }
}

public class FolderParams
{
    public SortType Type;
    public int Value;
    public string Category;
    public string Name;
}

public enum SortType
{
    Title,
    Artist,
    Level,
    Genre
}
