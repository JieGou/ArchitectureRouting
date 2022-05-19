using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class LocationByFilterViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly LocationTypeStorable _settingStorable ;

    private ObservableCollection<string>? _typeNames ;

    public ObservableCollection<string> TypeNames
    {
      get { return _typeNames ??= new ObservableCollection<string>( PatternElementHelper.PatternNames.Select( x => x.Value ) ) ; }
      set
      {
        _typeNames = value ;
        OnPropertyChanged() ;
      }
    }

    private string? _typeNameSelected ;

    public string TypeNameSelected
    {
      get { return _typeNameSelected ??= TypeNames.FirstOrDefault( x => x == _settingStorable.LocationType ) ?? TypeNames.First() ; }
      set
      {
        _typeNameSelected = value ;
        OnPropertyChanged() ;
      }
    }

    public ExternalEventHandler? ExternalEventHandler { get ; set ; }

    private List<Category>? _filterCategories ;

    public List<Category> FilterCategories
    {
      get { return _filterCategories ??= FilterHelper.CreateCategorySet( _uiDocument.Document ).OfType<Category>().ToList() ; }
    }

    public LocationByFilterViewModel( UIDocument uiDocument )
    {
      _uiDocument = uiDocument ;
      _settingStorable = _uiDocument.Document.GetLocationTypeStorable() ;
    }

    #region Commands

    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          wd.Close() ;
          ExternalEventHandler?.AddAction( ChangeLocationType )?.Raise() ;
        } ) ;
      }
    }

    #endregion

    #region Methods

    private void ChangeLocationType()
    {
      var elements = SelectElements() ;
      if ( ! elements.Any() )
        return ;

      InitialFilter() ;

      using var transaction = new Transaction( _uiDocument.Document ) ;
      transaction.Start( "Set Parameter" ) ;
      foreach ( var element in elements ) {
        if ( element.LookupParameter( FilterHelper.LocationTypeParameterName ) is not { } parameter )
          continue ;

        parameter.Set( TypeNameSelected ) ;
      }

      _settingStorable.LocationType = TypeNameSelected ;
      _settingStorable.Save() ;
      transaction.Commit() ;
    }

    private IList<Element> SelectElements()
    {
      var elements = new List<Element>() ;
      try {
        var references = _uiDocument.Selection.PickObjects( ObjectType.Element, new ChangeLocationTypeFilter( FilterCategories ), "Please select the element in project!" ) ;
        if ( ! references.Any() )
          return elements ;

        elements = references.Select( x => _uiDocument.Document.GetElement( x ) ).ToList() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        // Ignore
      }

      return elements ;
    }

    private void InitialFilter()
    {
      var filterNames = _uiDocument.ActiveView.GetFilters().Select( x => _uiDocument.Document.GetElement( x ).Name ) ;
      if ( PatternElementHelper.PatternNames.Select(x => x.Value).All( x => filterNames.Any( y => y == x ) ) )
        return ;
      FilterHelper.InitialFilters( _uiDocument.Document ) ;
    }

    #endregion
  }

  public class ChangeLocationTypeFilter : ISelectionFilter
  {
    private readonly List<Category> _categories ;

    public ChangeLocationTypeFilter( List<Category> categories )
    {
      _categories = categories ;
    }

    public bool AllowElement( Element elem )
    {
      return _categories.Any( c => c.Id.IntegerValue == elem.Category.Id.IntegerValue ) ;
    }

    public bool AllowReference( Reference reference, XYZ position )
    {
      return false ;
    }
  }
}