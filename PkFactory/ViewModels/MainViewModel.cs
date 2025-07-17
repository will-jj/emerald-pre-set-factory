using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PkFactory.Services;
using PKHeX.Core;
using PkFactory.Models;


namespace PkFactory.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    private bool _canSaveFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChooseByName))]
    private bool _chooseByIndex;

    public bool ChooseByName => !ChooseByIndex;

    private string? _filename = "emerald-factory.sav";
    

    private SaveFile? _saveFile;
    
    private enum Levels
    {
        Singles50,
        Singles100,
        Doubles50,
        Doubles100,
        OwnSave
    }
    
    public ObservableCollection<PocketMonster> Poketmonsters { get; set; } =
    [
        new PocketMonster(Constants.FrontierMons.FrontierMonNames, "Mon 1", "IVs [31]"),
        new PocketMonster(Constants.FrontierMons.FrontierMonNames, "Mon 2", "IVs [31]"),
        new PocketMonster(Constants.FrontierMons.FrontierMonNames, "Mon 3", "IVs [31]"),
        new PocketMonster(Constants.FrontierMons.FrontierMonNames, "Opp 1", "IVs [3]"),
        new PocketMonster(Constants.FrontierMons.FrontierMonNames, "Opp 2", "IVs [3]"),
        new PocketMonster(Constants.FrontierMons.FrontierMonNames, "Opp 3", "IVs [3]")
    ];
    

    [ObservableProperty]
    private int _selectedLevel;

    [ObservableProperty]
    private bool _useOwnSave;

    [RelayCommand]
    public async Task GetPreppedFile(string asset)
    {
        Stream datain = AssetLoader.Open(new Uri(asset));
        using MemoryStream memoryStream = new();
        await datain.CopyToAsync(memoryStream);
        byte[] byteArray = memoryStream.ToArray();
        _saveFile = SaveUtil.GetVariantSAV(byteArray);
        if (_saveFile != null) CanSaveFile = true;
    }
    
    [RelayCommand]
    public async Task GetFile()
    {
        TopLevel? topLevel = DialogManager.GetTopLevelForContext(this);
        if (topLevel == null) return;

        IReadOnlyList<IStorageFile> openedFile =
            await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions());
        IStorageFile? firstFile = openedFile.FirstOrDefault();
        if (firstFile != null)
        {
            _filename = firstFile.Name;
            Stream datain = await firstFile.OpenReadAsync();
            using MemoryStream memoryStream = new();
            await datain.CopyToAsync(memoryStream);
            byte[] byteArray = memoryStream.ToArray();
            _saveFile = SaveUtil.GetVariantSAV(byteArray);
            if (_saveFile is SAV3E)
            {
                CanSaveFile = true;
            }
        }
    }

    private static void WriteMonToSave(SAV3E save, ref int record, ushort frontierIdx, uint naturality, byte ivs, byte ability)
    {
        // Mon Id
        WriteUInt16LittleEndian(save.Small.AsSpan(record), frontierIdx);
        record += 2;
        record += 2;
        
        // Personality
        WriteUInt32LittleEndian(save.Small.AsSpan(record), naturality);
        record += 4;
        
        // Ivs
        save.Small[record] = ivs;
        record++;
        
        //Ability
        save.Small[record] = ability;
        record++;
        record += 2;
    }
    
    [RelayCommand]
    public async Task DoIt()
    {
        switch (SelectedLevel)
        {
            case (int)Levels.Singles50:
                await GetPreppedFile("avares://PkFactory/Assets/pokeemerald-50.sav");
                break;
            case (int)Levels.Singles100:
                await GetPreppedFile("avares://PkFactory/Assets/pokeemerald-ol.sav");
                break;
            case (int)Levels.Doubles50:
                await GetPreppedFile("avares://PkFactory/Assets/pokeemerald-d50.sav");
                break;
            case (int)Levels.Doubles100:
                await GetPreppedFile("avares://PkFactory/Assets/pokeemerald-d100.sav");
                break;
            case (int)Levels.OwnSave:
                if(!CanSaveFile) return;
                break;
        }

        if (_saveFile is not SAV3E save) return;
        int record = 0xE70;
        int ii = 0;
        foreach (PocketMonster monster in Poketmonsters)
        {
            int index = -1;
            if (ChooseByIndex)
            {
                index = monster.FrontierMonIndex ?? -1;
            }
            else
            {
                // find the string because binding is silly
                index = Array.IndexOf(Constants.FrontierMons.FrontierMonNames, monster.FrontierMon);
            }

            if (index == -1) return;
            int nature = (int)Constants.FrontierMons.FrontierMonNatures[index];
            monster.Ivs ??= ii <= 2 ? 31 : 3;
            monster.Ability ??= 0;
            ii++;
            WriteMonToSave(save, ref record, (ushort)index, (uint)nature, (byte)monster.Ivs, (byte)monster.Ability);
        }
        
        const int flag = 0xEFA;
        save.Small[flag] = 1;
        
        await SaveFile();
    }

    [RelayCommand(CanExecute = nameof(CanSaveFile))]
    public async Task SaveFile()
    {
        TopLevel? topLevel = DialogManager.GetTopLevelForContext(this);
        if (topLevel == null) return;
        FilePickerSaveOptions options = new()
        {
            SuggestedFileName = _filename
        };
        IStorageFile? fileOut = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        if (fileOut != null && _saveFile != null)
        {
            await using Stream stream = await fileOut.OpenWriteAsync();
            await stream.WriteAsync(_saveFile.Write());
        }
    }

    partial void OnSelectedLevelChanged(int value)
    {
        UseOwnSave = value == (int)Levels.OwnSave;
    }
}