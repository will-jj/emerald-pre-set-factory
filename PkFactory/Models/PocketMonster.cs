using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PkFactory.ViewModels;

namespace PkFactory.Models;

public partial class PocketMonster : ViewModelBase
{
    [ObservableProperty]
    string _frontierMon = string.Empty;
    
    [ObservableProperty]
    int? _frontierMonIndex;
    
    [ObservableProperty]
    string _placeHolder = string.Empty;

    [ObservableProperty]
    int _frontierMonIdx;

    [ObservableProperty]
    private int? _ability;

    [ObservableProperty]
    private int? _ivs;
    
    
    [ObservableProperty]
    private string _ivStr;
    
    public ObservableCollection<string> Mons { get; set; }

    public ObservableCollection<int> IvRs { get; set; } = new(Enumerable.Range(0, 32));
    
    public ObservableCollection<int> Abilities { get; set; } = new(Enumerable.Range(0, 2));


    public PocketMonster(string[] pocketMonsters, string placeHolder, string ivStr)
    {
        Mons  = new(pocketMonsters);
        PlaceHolder =  placeHolder;
        IvStr = ivStr;
    }
    
}
