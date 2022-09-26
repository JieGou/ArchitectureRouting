using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Globalization ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ShaftSettingViewModel : NotifyPropertyChanged
  {
    private readonly Document _document ;
    public ObservableCollection<ShaftModel> Shafts { get ; }
    public List<string> Sizes { get ; }
    private string _size ;

    public string Size
    {
      get => _size ;
      set
      {
        _size = value ;
        OnPropertyChanged() ;
      }
    }
    
    public ICommand SelectAllCommand => new RelayCommand( SelectAll ) ;

    public ICommand DeSelectAllCommand => new RelayCommand( DeSelectAll ) ;
    
    public ICommand CreateShaftCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          wd.DialogResult = true ;
          wd.Close() ;
        } ) ;
      }
    }

    public ShaftSettingViewModel( Document document )
    {
      _document = document ;
      var shafts = CreateShaftModels() ;
      Shafts = new ObservableCollection<ShaftModel>( shafts ) ;
      Sizes = GetDefaultShaftSizes() ;
      _size = Sizes.First() ;
    }

    private List<ShaftModel> CreateShaftModels()
    {
      var shafts = new List<ShaftModel>() ;
      var levels = _document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).ToList() ;
      for ( var i = 0 ; i < levels.Count - 1 ; i++ ) {
        var shaftModel = new ShaftModel( levels.ElementAt( i ), levels.ElementAt( i + 1 ) ) ;
        shafts.Add( shaftModel ) ;
      }

      return shafts ;
    }
    
    public void SetShaftModels( IEnumerable<ShaftOpeningModel> shaftOpeningModels )
    {
      var levels = _document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).ToList() ;
      foreach ( var shaftOpeningModel in shaftOpeningModels ) {
        var shaftOpening = _document.GetElementById<Opening>( shaftOpeningModel.ShaftOpeningUniqueId ) ;
        if ( shaftOpening == null ) continue ;
        var baseLevel = (Level) _document.GetElement( shaftOpening.get_Parameter( BuiltInParameter.WALL_BASE_CONSTRAINT ).AsElementId() ) ;
        var topLevel = (Level) _document.GetElement( shaftOpening.get_Parameter( BuiltInParameter.WALL_HEIGHT_TYPE ).AsElementId() ) ;
        var shaftLevelIds = levels.Where( l => l.Elevation >= baseLevel.Elevation && l.Elevation < topLevel.Elevation ).Select( l => l.Id ).EnumerateAll() ;
        Shafts.Where( s => shaftLevelIds.Contains( s.FromLevel.Id ) ).ForEach( s => s.IsShafted = true ) ;
        var cableTrays = _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_CableTrayFitting ).Where( e => shaftOpeningModel.CableTrayUniqueIds.Contains( e.UniqueId ) ).EnumerateAll() ;
        var cableTrayLevelIds = cableTrays.Select( c => c.LevelId ).EnumerateAll() ;
        Shafts.Where( s => cableTrayLevelIds.Contains( s.FromLevel.Id ) ).ForEach( s => s.IsRacked = true ) ;
        Size = shaftOpeningModel.Size.ToString( CultureInfo.InvariantCulture ) ;
      }
    }

    private List<string> GetDefaultShaftSizes()
    {
      var sizes = new List<string>() ;
      for ( var i = 1 ; i <= 10 ; i++ ) {
        sizes.Add( ( i * 100 ).ToString() ) ;
      }

      return sizes ;
    }

    private void SelectAll()
    {
      Shafts.ForEach( s => s.IsShafted = true ) ;
      Shafts.ForEach( s => s.IsRacked = true ) ;
    }
    
    private void DeSelectAll()
    {
      Shafts.ForEach( s => s.IsShafted = false ) ;
      Shafts.ForEach( s => s.IsRacked = false ) ;
    }
  }
}