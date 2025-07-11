using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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

    private string? _filename = "emerald-factory.sav";
    

    private string _name = string.Empty;

    private SaveFile? _saveFile;



    [ObservableProperty]
    private string? _selectedGame;


    [Required]
    [MinLength(1)]
    [MaxLength(7)]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, true);
    }



    [ObservableProperty]
    private int _selectedAbility;
    
    [ObservableProperty]
    private int _selectedLevel;
    
    [ObservableProperty]
    private int _selectedIv;
    
    [ObservableProperty]
    private string _selectedFrontierMon;


    public ObservableCollection<Poketmonster> Poketmonsters { get; set; } = new()
    {
        new Poketmonster(Constants.FrontierMons.FrontierMonNames, "Mon 1"),
        new Poketmonster(Constants.FrontierMons.FrontierMonNames, "Mon 2"),
        new Poketmonster(Constants.FrontierMons.FrontierMonNames, "Mon 3"),
        new Poketmonster(Constants.FrontierMons.FrontierMonNames, "Opp 1"),
        new Poketmonster(Constants.FrontierMons.FrontierMonNames, "Opp 2"),
        new Poketmonster(Constants.FrontierMons.FrontierMonNames, "Opp 3"),
    };

    [ObservableProperty]
    private string _selectedSet;

    [ObservableProperty]
    private int _selectedSetIndex;

    [ObservableProperty]
    private bool _includeTeam;

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

    public async Task LoadFileFromDisk(string path)
    {
        Stream datain = File.OpenRead(path);
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
            int real = 131072;
            var data = byteArray[..real].ToArray();
            var hmm = data[0xE70];
            var hmm2 = data[0xE71];
            _saveFile = SaveUtil.GetVariantSAV(byteArray);
            if (_saveFile != null)
            {
                CanSaveFile = true;
                Name = _saveFile.OT;
            }
        }


        if (_saveFile is not SAV3E save) return;
        int record = 0xE70;


        const int flag = 0xEFA;
        save.Small[flag] = 1;
        
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
        
        
        const int flag = 0xEFA;
        save.Small[flag] = 1;
    }
    
    [RelayCommand]
    public async Task DoIt()
    {
        if (SelectedLevel == 0)
        {
            await GetPreppedFile("avares://PkFactory/Assets/lv50.sav");
        }
        else
        {
            await GetPreppedFile("avares://PkFactory/Assets/ol.sav");

        }

        if (_saveFile is not SAV3E save) return;
        int record = 0xE70;
        int ii = 0;
        foreach (Poketmonster monster in Poketmonsters)
        {
            // find the string because binding is silly
            int index = Array.IndexOf(Constants.FrontierMons.FrontierMonNames, monster.FrontierMon);
            if (index == -1) return;
            int nature = (int)Constants.FrontierMons.FrontierMonNatures[index];
            monster.Ivs ??= ii <= 2 ? 31 : 3;
            monster.Ability ??= 0;
            ii++;
            WriteMonToSave(save, ref record, (ushort)index, (uint)nature, (byte)monster.Ivs, (byte)monster.Ability);
        }
        
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
        if (fileOut != null)
        {
            await using Stream stream = await fileOut.OpenWriteAsync();
            await stream.WriteAsync(_saveFile.Write());
        }
    }
}