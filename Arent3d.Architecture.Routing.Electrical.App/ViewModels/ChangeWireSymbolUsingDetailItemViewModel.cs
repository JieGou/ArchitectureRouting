using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MoreLinq ; 

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class ChangeWireSymbolUsingDetailItemViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly StorageService<Level, LocationTypeModel> _locationTypeStorage;
    private readonly StorageService<Level, ConduitAndDetailCurveModel> _conduitAndDetailCurveStorage;

    private static Dictionary<string, string>? _wireSymbolOptions ;

    public static Dictionary<string, string> WireSymbolOptions => _wireSymbolOptions ??= ChangeWireTypeCommand.WireSymbolOptions ;

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
      get { return _typeNameSelected ??= TypeNames.FirstOrDefault( x => x == _locationTypeStorage.Data.LocationType ) ?? TypeNames.First() ; }
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
      _locationTypeStorage = new StorageService<Level, LocationTypeModel>(((ViewPlan)uiDocument.ActiveView).GenLevel) ;
      _conduitAndDetailCurveStorage = new StorageService<Level, ConduitAndDetailCurveModel>(((ViewPlan)uiDocument.ActiveView).GenLevel) ;
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

      var (lines, curves) = ChangeWireTypeCommand.GetLocationConduits( _uiDocument.Document, _uiDocument.ActiveView, elements ) ;
      
      if ( WireSymbolOptions.Keys.Contains( TypeNameSelected ) ) {
        var familySymbol = _uiDocument.Document.GetAllTypes<FamilySymbol>( x => x.Name == WireSymbolOptions[ TypeNameSelected ] ).FirstOrDefault() ;
        if ( null == familySymbol )
          return ;

        using var transaction = new Transaction( _uiDocument.Document ) ;
        transaction.Start( "Change Location Type" ) ;

        if(!familySymbol.IsActive)
          familySymbol.Activate();

        var lineStyle = GetLineStyle( _uiDocument.Document, ChangeWireTypeCommand.SubcategoryName ) ;
        
        curves.ForEach( x =>
        {
          var detailCurve = _uiDocument.Document.Create.NewDetailCurve( _uiDocument.ActiveView, x.Key ) ;
          detailCurve.LineStyle = lineStyle.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
          _conduitAndDetailCurveStorage.Data.ConduitAndDetailCurveData.Add( new ConduitAndDetailCurveItemModel
          {
            ConduitId = x.Value,
            DetailCurveId = detailCurve.UniqueId,
            WireType = WireSymbolOptions[ TypeNameSelected ],
            IsLeakRoute = false
          } ) ;
        } ) ;
        lines.ForEach( x =>
        {
          var line = _uiDocument.Document.Create.NewFamilyInstance( x.Key, familySymbol, _uiDocument.ActiveView ) ;
          _conduitAndDetailCurveStorage.Data.ConduitAndDetailCurveData.Add( new ConduitAndDetailCurveItemModel
          {
            ConduitId = x.Value,
            DetailCurveId = line.UniqueId,
            WireType = WireSymbolOptions[ TypeNameSelected ],
            IsLeakRoute = false
          }) ;
        } ) ;
        
        _conduitAndDetailCurveStorage.SaveChange() ;  

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
        
        curves.ForEach( x =>
        {
          var detailCurve = _uiDocument.Document.Create.NewDetailCurve( _uiDocument.ActiveView, x.Key ) ;
          detailCurve.LineStyle = graphicsStyle ;
        } ) ;
        lines.ForEach( x =>
        {
          var detailCurve = _uiDocument.Document.Create.NewDetailCurve(_uiDocument.ActiveView, x.Key) ;
          detailCurve.LineStyle = graphicsStyle ;
        } ) ;
        
        transaction.Commit() ;
      }

      using var trans = new Transaction( _uiDocument.Document ) ;
      trans.Start( "Hidden Element" ) ;
      _uiDocument.ActiveView.HideElements( elements.Select( x => x.Id ).ToList() ) ;

      var dropCategory = Category.GetCategory(_uiDocument.Document, BuiltInCategory.OST_ConduitDrop);
      if(null != dropCategory)
        _uiDocument.ActiveView.SetCategoryHidden(dropCategory.Id, true);
        
      _locationTypeStorage.Data.LocationType = TypeNameSelected ;
      _locationTypeStorage.SaveChange() ;
      trans.Commit() ;
      
      transactionGroup.Assimilate() ;
    }
    
    private static Category GetLineStyle( Document document, string subCategoryName)
    {
      var categories = document.Settings.Categories ;
      var category = document.Settings.Categories.get_Item( BuiltInCategory.OST_Lines ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
      }
      else {
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;
      }

      return subCategory ;
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

    #endregion
    
  }
}