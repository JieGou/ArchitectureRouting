using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Data ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class AddWiringInformationViewModel: NotifyPropertyChanged
  {

    private readonly Document _document ;
    public RelayCommand<Window> SaveCommand => new( Save ) ; 
    public RelayCommand<Window> CancelCommand => new(Cancel) ;
    public ObservableCollection<string> ConduitPropertyList { get ; set ; } = new() ;
    
    private ObservableCollection<string> _conduitPropertyDisplay = new() ;

    public ObservableCollection<string> ConduitPropertyDisplay
    {
      get => _conduitPropertyDisplay ;
      set
      {
        _conduitPropertyDisplay = value ;
        OnPropertyChanged( "ConduitPropertyDisplay" ) ;
      }
    }
    
    public string? SelectedProperty { get ; set ; }
    public Route? SelectedElement { get ; set ; }
    
    private string _searchText = String.Empty ;
    public string SearchText
    {
      get => _searchText;
      set
      {
        if (_searchText== value) return;
        _searchText = value;
        ConduitPropertyDisplay = new ObservableCollection<string>(ConduitPropertyList.Where( x => x.Contains( _searchText ) ))  ;
        CollectionViewSource.GetDefaultView( ConduitPropertyDisplay ).Refresh() ;
        OnPropertyChanged( ) ;
      }
    }
    
    public AddWiringInformationViewModel( Document document, Route element )
    {
      _document = document ;
      SelectedElement = element ;
      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      if ( csvStorable != null ) { 
        var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        ConduitPropertyList = new ObservableCollection<string>(hiroiMasterModels.GroupBy( x => x.Kikaku ).Select( g => g.Key )) ;
        ConduitPropertyDisplay = ConduitPropertyList ;
      }
    }
     
    private void Save(Window window)
    {
      try {
        using Transaction t = new Transaction( _document, "Save wiring data" ) ;
        t.Start() ;
        // _pickUpStorable.AllPickUpModelData = _pickUpModels ;
        // _pickUpStorable.Save() ;
        t.Commit() ;
        window.DialogResult = true ; 
        window.Close();
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        throw ;
      }
    }
    
    private void Cancel(Window window)
    {
      window.DialogResult = false ;
      window.Close();
    }
    
  }
}