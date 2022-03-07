using System ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel
{
  public class ReplaceFlexDuctViewModel : NotifyPropertyChanged
  {
    #region Members

    private Document _document ;
    private const string Title = "Arent" ; 

    private ObservableCollection<FlexDuctType>? _flexDuctTypes ;
    public ObservableCollection<FlexDuctType> FlexDuctTypes
    {
      get
      {
        if ( null == _flexDuctTypes )
          _flexDuctTypes = new ObservableCollection<FlexDuctType>( _document.GetAllElements<FlexDuctType>()
            .Where( x => x.Shape == ConnectorProfileType.Round ) ) ;

        return _flexDuctTypes ;
      }
      set
      {
        _flexDuctTypes = value ;
        OnPropertyChanged() ;
      }
    }

    private FlexDuctType? _flexDuctType ;
    public FlexDuctType? FlexDuctType
    {
      get { return _flexDuctType ??= FlexDuctTypes.FirstOrDefault() ; }
      set
      {
        _flexDuctType = value ;
        OnPropertyChanged() ;
      }
    }

    private ObservableCollection<string>? _diameters ;
    public ObservableCollection<string> Diameters
    {
      get
      {
        if ( null == _diameters ) {
          var ductSize = DuctSizeSettings.GetDuctSizeSettings( _document )[ DuctShape.Round ].Select( x => x.NominalDiameter ).OrderBy( x => x ) ;
          Func<double, string> displayLength = d =>
          {
            if ( DisplayUnit == DisplayUnit.METRIC )
              return $"{Math.Round( d.RevitUnitsToMillimeters() )} mm" ;
            else
              return $"{Math.Round( UnitUtils.ConvertFromInternalUnits( d, DisplayUnitTypes.Inches ) )}" ;
          } ;
          _diameters = new ObservableCollection<string>( ductSize.Select( displayLength ) ) ;
        }

        return _diameters ;
      }
    }

    private string? _diameter ;
    public string? Diameter
    {
      get { return _diameter ??= Diameters.FirstOrDefault() ; }
      set
      {
        _diameter = value ;
        OnPropertyChanged() ;
      }
    }
    protected DisplayUnit DisplayUnit => _document.DisplayUnitSystem ;
    #endregion

    public ReplaceFlexDuctViewModel( Document document )
    {
      _document = document ;
    }

    #region Commands

    public ICommand CloseCommand
    {
      get { return new RelayCommand<Window>( ( wd ) => { return null != wd ; }, ( wd ) => { wd.Close() ; } ) ; }
    }

    public ICommand OkCommand
    {
      get 
      { 
        return new RelayCommand<Window>( ( wd ) => { return null != wd ; }, ( wd ) =>
        {
          wd.Close();
          TaskDialog.Show( Title, Diameter ) ;
        } ) ; 
      }
    }
    #endregion
    
  }
}