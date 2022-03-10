using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel
{
  public class ReplaceFlexDuctViewModel : NotifyPropertyChanged
  {
    #region Members

    private Document _document ;
    private ( List<Connector> ConnectorRefs, List<(XYZ Origin, XYZ Direction)> Points, IList<Element> DeletedElements) _data ;
    private DisplayUnit DisplayUnit => _document.DisplayUnitSystem ;

    private ObservableCollection<FlexDuctType>? _flexDuctTypes ;

    public ObservableCollection<FlexDuctType> FlexDuctTypes
    {
      get
      {
        if ( null == _flexDuctTypes )
          _flexDuctTypes = new ObservableCollection<FlexDuctType>( _document.GetAllElements<FlexDuctType>().Where( x => x.Shape == ConnectorProfileType.Round ) ) ;

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
          _diameters = new ObservableCollection<string>( ductSize.Select( DisplayDiameter ) ) ;
        }

        return _diameters ;
      }
    }

    private string? _diameter ;

    public string? Diameter
    {
      get
      {
        if ( null == _diameter && _data.ConnectorRefs.Count > 0 ) {
          var value = SuggestionDiameter( _data.ConnectorRefs ) ;
          _diameter = DisplayDiameter( value ) ;
        }

        return _diameter ??= Diameters.FirstOrDefault() ;
      }
      set
      {
        _diameter = value ;
        OnPropertyChanged() ;
      }
    }

    #endregion

    public ReplaceFlexDuctViewModel( Document document, ( List<Connector> ConnectorRefs, List<(XYZ, XYZ)> Points, IList<Element> DeletedElements) data )
    {
      _document = document ;
      _data = data ;
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
          wd.Close() ;
          try {
            if ( null == FlexDuctType )
              MessageBox.Show( "Not found the flex duct type!" ) ;
            else {
              var (canParse, diameter) = TryParseDiameter( Diameter ) ;
              if ( ! canParse )
                MessageBox.Show( "The diameter is invalid!" ) ;
              else {
                using Transaction transaction = new Transaction( _document ) ;
                transaction.Start( "Change Flex Duct" ) ;

                FlexDuct flexDuct ;
                if ( _data.ConnectorRefs.Count == 2 )
                  flexDuct = _document.Create.NewFlexDuct( _data.ConnectorRefs[ 0 ], _data.ConnectorRefs[ 1 ], FlexDuctType ) ;
                else if ( _data.ConnectorRefs.Count == 1 ) {
                  flexDuct = _document.Create.NewFlexDuct( _data.ConnectorRefs[ 0 ], _data.Points.Select( x => x.Origin ).ToList(), FlexDuctType ) ;
                  flexDuct.EndTangent = _data.Points[ 0 ].Direction ;
                }
                else {
                  flexDuct = _document.Create.NewFlexDuct( _data.Points.Select( x => x.Origin ).ToList(), FlexDuctType ) ;
                  flexDuct.StartTangent = _data.Points[ 0 ].Direction.Negate() ;
                  flexDuct.EndTangent = _data.Points[ 1 ].Direction ;
                }

                flexDuct.get_Parameter( BuiltInParameter.RBS_CURVE_DIAMETER_PARAM ).Set( diameter ) ;

                _document.Delete( _data.DeletedElements.Select( x => x.Id ).ToList() ) ;

                transaction.Commit() ;
              }
            }
          }
          catch ( Exception exception ) {
            MessageBox.Show( exception.Message ) ;
          }
        } ) ;
      }
    }

    #endregion

    #region Methods

    private (bool CanParse, double Diameter) TryParseDiameter( string? diameter )
    {
      if ( DisplayUnit == DisplayUnit.METRIC )
        return ( double.TryParse( diameter?.Replace( "mm", "" ).Replace( "MM", "" ).Trim(), out double value ), value.MillimetersToRevitUnits() ) ;
      else
        return ( double.TryParse( diameter?.Trim(), out double value ), UnitUtils.ConvertFromInternalUnits( value, DisplayUnitTypes.Inches ) ) ;
    }

    private double SuggestionDiameter( IEnumerable<Connector> connectors )
    {
      var values = new List<double>() ;

      foreach ( var connector in connectors ) {
        if ( connector.Shape == ConnectorProfileType.Round )
          values.Add( 2 * connector.Radius ) ;
        else if ( connector.Shape == ConnectorProfileType.Rectangular || connector.Shape == ConnectorProfileType.Oval )
          values.Add( Math.Min( connector.Width, connector.Height ) ) ;
      }

      return values.Min() ;
    }

    private string DisplayDiameter( double diameter )
    {
      if ( DisplayUnit == DisplayUnit.METRIC )
        return $"{Math.Round( diameter.RevitUnitsToMillimeters() )} mm" ;
      else
        return $"{Math.Round( UnitUtils.ConvertFromInternalUnits( diameter, DisplayUnitTypes.Inches ) )}" ;
    }

    #endregion
  }
}