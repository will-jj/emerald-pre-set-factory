using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PkFactory.ViewModels;

namespace PkFactory.Models;

public partial class Set : ViewModelBase
{
    [ObservableProperty]
    string _showdownText = string.Empty;

    [ObservableProperty]
    private bool _isNotValid;
    
    [ObservableProperty]
    string _errors = string.Empty;
    
    public uint? PID { get; set; }

    public Set()
    {
    }

    public Set(string showdownText, uint? pID = null)
    {
        ShowdownText = showdownText;
        IsNotValid = false;
        PID = pID;
    }
}


public partial class Poketmonster : ViewModelBase
{
    [ObservableProperty]
    string _frontierMon = string.Empty;
    
    [ObservableProperty]
    string _placeHolder = string.Empty;

    [ObservableProperty]
    int _frontierMonIdx;

    [ObservableProperty]
    private int? _ability;

    [ObservableProperty]
    private int? _ivs;
    
    public ObservableCollection<string> Mons { get; set; }

    public ObservableCollection<int> IvRs { get; set; } = new(Enumerable.Range(0, 32));
    
    public ObservableCollection<int> Abilities { get; set; } = new(Enumerable.Range(0, 2));


    public Poketmonster(string[] poketmonsters, string placeHolder)
    {
        Mons  = new(poketmonsters);
        PlaceHolder =  placeHolder;
    }
    
}
