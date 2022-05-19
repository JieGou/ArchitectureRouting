using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MoreLinq.Extensions ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class LocationByDetailViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly IEnumerable<Element> _allConduits ;
    private readonly List<Line> _lineConduits ;
    private readonly List<Curve> _horizontalFittings ;
    private readonly LocationTypeStorable _settingStorable ;

    private ObservableCollection<string>? _typeNames ;

    public ObservableCollection<string> TypeNames
    {
      get { return _typeNames ??= new ObservableCollection<string>( ComponentHelper.ComponentNames.Select( x => x.Value ) ) ; }
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

    public LocationByDetailViewModel( UIDocument uiDocument, IEnumerable<Element> allConduits, List<Line> lineConduits, List<Curve> horizontalFittings )
    {
      _uiDocument = uiDocument ;
      _allConduits = allConduits ;
      _lineConduits = lineConduits ;
      _horizontalFittings = horizontalFittings ;
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
      var repeatType = _uiDocument.Document.GetAllTypes<ElementType>( x => x.Name == TypeNameSelected ).FirstOrDefault() ;
      if(null == repeatType)
        return;

      using var transaction = new Transaction( _uiDocument.Document ) ;
      transaction.Start( "Change Location Type" ) ;

      _horizontalFittings.ForEach( x => { _uiDocument.Document.Create.NewDetailCurve( _uiDocument.ActiveView, x ) ; });
      _lineConduits.ForEach( x =>
      {
        var detail = _uiDocument.Document.Create.NewDetailCurve( _uiDocument.ActiveView, x ) ;
        detail.ChangeTypeId( repeatType.Id ) ;
      });
      
      _uiDocument.ActiveView.HideElements(_allConduits.Select(x => x.Id).ToList());
      
      transaction.Commit() ;
    }

    #endregion
  }
}