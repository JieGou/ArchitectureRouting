using System ;
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
using Arent3d.Architecture.Routing.Utils ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.DB.Electrical ;
using MoreLinq ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class ChangeWireSymbolUsingDetailItemViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly LocationTypeStorable _settingStorable ;

    private Dictionary<string, string>? _wireSymbolOptions ;

    public Dictionary<string, string> WireSymbolOptions => _wireSymbolOptions ??= new Dictionary<string, string>
    {
      { "漏水帯（布）", "Circle Repeat" }, 
      { "漏水帯（発色）", "Square Repeat" }, 
      { "漏水帯（塩ビ）", "Vertical Repeat" }
    } ;

    private ObservableCollection<string>? _typeNames ;

    public ObservableCollection<string> TypeNames
    {
      get
      {
        if ( null != _typeNames ) return _typeNames ;
        var typeNames = WireSymbolOptions.Select(x => x.Key).ToList() ;
        PatternElementHelper.PatternNames.Select(x => x.Value).ToList().ForEach(x => typeNames.Add(x));
        _typeNames = new ObservableCollection<string>( typeNames ) ;
        return _typeNames ;
      }
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

    public Func<FamilyInstance, XYZ> GetCenterPoint =>
      familyInsatance =>
      {
        var connectors = familyInsatance.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ;
        return 0.5 * ( connectors[ 0 ].Origin + connectors[ 1 ].Origin ) ;
      } ;

    public ChangeWireSymbolUsingDetailItemViewModel( UIDocument uiDocument )
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

      using var transactionGroup = new TransactionGroup( _uiDocument.Document ) ;
      transactionGroup.Start( "Change Type" ) ;

      var (lines, curves) = GetLocationConduits( elements ) ;
      
      if ( WireSymbolOptions.Keys.Contains( TypeNameSelected ) ) {
        var familySymbol = _uiDocument.Document.GetAllTypes<FamilySymbol>( x => x.Name == WireSymbolOptions[ TypeNameSelected ] ).FirstOrDefault() ;
        if ( null == familySymbol )
          return ;

        using var transaction = new Transaction( _uiDocument.Document ) ;
        transaction.Start( "Change Location Type" ) ;

        if(!familySymbol.IsActive)
          familySymbol.Activate();

        curves.ForEach( x => { _uiDocument.Document.Create.NewDetailCurve( _uiDocument.ActiveView, x ) ; } ) ;
        lines.ForEach( x => { _uiDocument.Document.Create.NewFamilyInstance( x, familySymbol, _uiDocument.ActiveView ) ; } ) ;

        transaction.Commit() ;
      }
      else {
        var (patternName, patternId) = PatternElementHelper.GetLinePatterns( _uiDocument.Document ).SingleOrDefault(x => x.PatternName == TypeNameSelected) ;
        
        using var transaction = new Transaction( _uiDocument.Document ) ;
        transaction.Start( "Change Location Type" ) ;

        var category = _uiDocument.Document.Settings.Categories.get_Item(BuiltInCategory.OST_Lines) ;
        Category subCategory ;
        if ( ! category.SubCategories.Contains( patternName ) ) {
          subCategory = _uiDocument.Document.Settings.Categories.NewSubcategory( category, patternName ) ;
          subCategory.SetLinePatternId(patternId, GraphicsStyleType.Projection);
        }
        else {
          subCategory = category.SubCategories.get_Item( patternName ) ;
        }
        
        var graphicsStyle = subCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        
        curves.ForEach( x => { _uiDocument.Document.Create.NewDetailCurve( _uiDocument.ActiveView, x ) ; } ) ;
        lines.ForEach( x =>
        {
          var detailCurve = _uiDocument.Document.Create.NewDetailCurve(_uiDocument.ActiveView, x) ;
          detailCurve.LineStyle = graphicsStyle ;
        } ) ;
        
        transaction.Commit() ;
      }

      using var trans = new Transaction( _uiDocument.Document ) ;
      trans.Start( "Hidden Element" ) ;
      _uiDocument.ActiveView.HideElements( elements.Select( x => x.Id ).ToList() ) ;

      var conduitCategory = _uiDocument.Document.Settings.Categories.get_Item( BuiltInCategory.OST_Conduit ) ;
      if(conduitCategory?.SubCategories.get_Item("Drop") is {} subCat)
        _uiDocument.ActiveView.SetCategoryHidden(subCat.Id, true);
        
      _settingStorable.LocationType = TypeNameSelected ;
      _settingStorable.Save();
      trans.Commit() ;
      
      RefreshView() ;

      transactionGroup.Assimilate() ;
    }

    private List<Element> SelectElements()
    {
      var elements = new List<Element>() ;

      try {
        elements = _uiDocument.Selection.PickObjects( ObjectType.Element, new ChangeLocationTypeFilter( new List<Category> { Category.GetCategory( _uiDocument.Document, BuiltInCategory.OST_Conduit ), Category.GetCategory( _uiDocument.Document, BuiltInCategory.OST_ConduitFitting ) } ), "Please select conduit, conduit fitting in project!" ).Select( x => _uiDocument.Document.GetElement( x ) ).ToList() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        // Ignore
      }

      return elements ;
    }

    private (List<Line> lineConduits, List<Curve> curveHorizontal) GetLocationConduits( List<Element> elements )
    {
      var conduits = elements.OfType<Conduit>().ToList() ;
      var curveConduits = GetCurveFromElements( _uiDocument.Document, conduits ) ;

      var conduitFittings = elements.OfType<FamilyInstance>().ToList() ;
      var fittingHorizontals = conduitFittings.Where( x => Math.Abs( x.GetTransform().OfVector( XYZ.BasisZ ).Z - 1 ) < GeometryUtil.Tolerance ).ToList() ;
      var fittingVerticals = conduitFittings.Where( x => Math.Abs( x.GetTransform().OfVector( XYZ.BasisZ ).Z ) < GeometryUtil.Tolerance ).ToList() ;

      var lineConduits = curveConduits.OfType<Line>().ToList() ;
      var lineVerticalFittings = GetLineVerticalFittings( fittingVerticals ) ;

      lineConduits.AddRange( lineVerticalFittings ) ;
      var lines = ConnectLines( lineConduits ) ;
      var curves = GetCurveHorizontalFittings( _uiDocument.Document, fittingHorizontals ) ;
      return ( lines, curves ) ;
    }

    private List<Curve> GetCurveHorizontalFittings( Document document, IEnumerable<FamilyInstance> fittingHorizontals )
    {
      var comparer = new XyzComparer() ;
      fittingHorizontals = fittingHorizontals.Where( x => x.MEPModel.ConnectorManager.Connectors.Size == 2 ) ;
      fittingHorizontals = fittingHorizontals.DistinctBy( x => GetCenterPoint( x ), comparer ) ;
      return GetCurveFromElements( document, fittingHorizontals ) ;
    }

    private List<Line> GetLineVerticalFittings( IEnumerable<FamilyInstance> fittingVerticals )
    {
      var comparer = new XyzComparer() ;
      var connectors = fittingVerticals.DistinctBy( x => ( (LocationPoint) x.Location ).Point, comparer ).Select( x => x.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ) ;

      var lines = new List<Line>() ;
      foreach ( var connector in connectors ) {
        if ( connector.Count != 2 )
          continue ;

        var maxZ = connector[ 0 ].Origin.Z > connector[ 1 ].Origin.Z ? connector[ 0 ].Origin.Z : connector[ 1 ].Origin.Z ;
        lines.Add( Line.CreateBound( new XYZ( connector[ 0 ].Origin.X, connector[ 0 ].Origin.Y, maxZ ), new XYZ( connector[ 1 ].Origin.X, connector[ 1 ].Origin.Y, maxZ ) ) ) ;
      }

      return lines ;
    }

    private List<Line> ConnectLines( List<Line> lines )
    {
      var lineConnects = new List<Line>() ;
      while ( lines.Any() ) {
        var line = lines[ 0 ] ;
        lines.RemoveAt( 0 ) ;

        if ( lines.Count > 0 ) {
          int count ;
          do {
            count = lines.Count ;

            var middleFirst = line.Evaluate( 0.5, true ) ;
            for ( var i = lines.Count - 1 ; i >= 0 ; i-- ) {
              var middleSecond = lines[ i ].Evaluate( 0.5, true ) ;
              if ( middleFirst.DistanceTo( middleSecond ) < GeometryHelper.Tolerance ) {
                if ( lines[ i ].Length > line.Length )
                  line = lines[ i ] ;
                lines.RemoveAt( i ) ;
              }
              else {
                var lineTemp = Line.CreateBound( middleFirst, middleSecond ) ;
                if ( Math.Abs( Math.Abs( lineTemp.Direction.DotProduct( line.Direction ) ) - 1 ) < GeometryHelper.Tolerance && 0.5 * line.Length + 0.5 * lines[ i ].Length + GeometryHelper.Tolerance >= lineTemp.Length ) {
                  if ( GeometryHelper.GetMaxLengthLine( line, lines[ i ] ) is { } ml )
                    line = ml ;
                  lines.RemoveAt( i ) ;
                }
              }
            }
          } while ( count != lines.Count ) ;
        }

        lineConnects.Add( line ) ;
      }

      var lineOnPlanes = new List<Line>() ;
      var elevation = _uiDocument.ActiveView.GenLevel.Elevation ;
      foreach ( var lineConnect in lineConnects ) {
        var firstPoint = lineConnect.GetEndPoint(0) ;
        var secondPoint = lineConnect.GetEndPoint( 1 ) ;
        lineOnPlanes.Add(Line.CreateBound(new XYZ(firstPoint.X, firstPoint.Y, elevation), new XYZ(secondPoint.X, secondPoint.Y, elevation)));
      }

      return lineOnPlanes ;
    }

    private List<Curve> GetCurveFromElements( Document document, IEnumerable<Element> elements )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Get Geometry" ) ;

      var detailLevel = document.ActiveView.DetailLevel ;
      document.ActiveView.DetailLevel = ViewDetailLevel.Coarse ;

      var curves = new List<Curve>() ;
      var options = new Options { View = document.ActiveView } ;

      foreach ( var element in elements ) {
        if ( element.get_Geometry( options ) is { } geometryElement )
          RecursiveCurves( geometryElement, ref curves ) ;
      }

      document.ActiveView.DetailLevel = detailLevel ;
      transaction.Commit() ;

      return curves ;
    }

    private void RecursiveCurves( GeometryElement geometryElement, ref List<Curve> curves )
    {
      foreach ( var geometry in geometryElement ) {
        switch ( geometry ) {
          case GeometryInstance geometryInstance :
          {
            if ( geometryInstance.GetInstanceGeometry() is { } subGeometryElement )
              RecursiveCurves( subGeometryElement, ref curves ) ;
            break ;
          }
          case Curve curve :
            curves.Add( curve.Clone() ) ;
            break ;
        }
      }
    }

    private void RefreshView()
    {
      if ( _uiDocument.ActiveView.DetailLevel != ViewDetailLevel.Fine ) 
        return ;
      
      using var transaction = new Transaction( _uiDocument.Document ) ;
      
      transaction.Start( "Detail Level Coarse" ) ;
      _uiDocument.ActiveView.DetailLevel = ViewDetailLevel.Coarse ;
      transaction.Commit() ;
      
      transaction.Start( "Detail Level Fine" ) ;
      _uiDocument.ActiveView.DetailLevel = ViewDetailLevel.Fine ;
      transaction.Commit() ;
    }

    #endregion
  }
}