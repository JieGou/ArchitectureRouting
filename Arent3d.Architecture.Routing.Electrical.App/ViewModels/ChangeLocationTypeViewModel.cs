using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class ChangeLocationTypeViewModel: NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;

    private ObservableCollection<string>? _typeNames ;
    public ObservableCollection<string> TypeNames
    {
      get { return _typeNames ??= new ObservableCollection<string>( PatternElementHelper.PatternNames ) ; }
      set
      {
        _typeNames = value ;
        OnPropertyChanged();
      }
    }

    public ChangeLocationTypeViewModel( UIDocument uiDocument )
    {
      _uiDocument = uiDocument ;
    }

    #region Methods

    private bool IsInitialFilter()
    {
      var parameterFilterElements = _uiDocument.Document.GetAllElements<ParameterFilterElement>() ;
      return parameterFilterElements.Any() && TypeNames.All( x => parameterFilterElements.Any( y => y.Name == x ) ) ;
    }

    #endregion
    
  }
}