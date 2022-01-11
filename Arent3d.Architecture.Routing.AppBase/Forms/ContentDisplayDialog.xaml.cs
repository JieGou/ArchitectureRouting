using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ContentDisplayDialog : Window
  {
    private const string DefaultConstructionItem = "未設定" ;
    private readonly Document _document ;
    private List<PickUpModel> _pickUpModels ;
    private PickUpStorable _pickUpStorable ;
    private readonly List<CeedModel> _ceeDModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModels ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterNormalModels ;
    private Dictionary<int, string> _pickUpNumbers ;
    private int _pickUpNumber ;

    public ContentDisplayDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _ceeDModels = new List<CeedModel>() ;
      _hiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _hiroiMasterModels = new List<HiroiMasterModel>() ;
      _hiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      _pickUpNumbers = new Dictionary<int, string>() ;
      _pickUpNumber = 1 ;

      var ceeDStorable = _document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable != null ) _ceeDModels = ceeDStorable.CeedModelData ;

      var csvStorable = _document.GetAllStorables<CsvStorable>().FirstOrDefault() ;
      if ( csvStorable != null ) {
        _hiroiSetMasterNormalModels = csvStorable.HiroiSetMasterNormalModelData ;
        _hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        _hiroiSetCdMasterNormalModels = csvStorable.HiroiSetCdMasterNormalModelData ;
      }

      _pickUpModels = GetPickUpData() ;
      _pickUpStorable = _document.GetPickUpStorable() ;
      if ( ! _pickUpModels.Any() ) MessageBox.Show( "Don't have element.", "Result Message" ) ;
      else {
        List<PickUpModel> pickUpConduitByNumbers = PickUpModelByNumber( ProductType.Conduit ) ;
        List<PickUpModel> pickUpRackByNumbers = PickUpModelByNumber( ProductType.Cable ) ;
        var pickUpModels = _pickUpModels.Where( p => p.EquipmentType == ProductType.Connector.GetFieldName() ).ToList() ;
        if ( pickUpConduitByNumbers.Any() ) pickUpModels.AddRange( pickUpConduitByNumbers ) ;
        if ( pickUpRackByNumbers.Any() ) pickUpModels.AddRange( pickUpRackByNumbers ) ;
        pickUpModels = ( from pickUpModel in pickUpModels orderby pickUpModel.Floor ascending select pickUpModel ).ToList() ;
        var viewModel = new PickUpViewModel( _pickUpStorable, pickUpModels ) ;
        this.DataContext = viewModel ;
      }
    }

    private void DataGrid_LoadingRow( object sender, DataGridRowEventArgs e )
    {
      e.Row.Header = ( e.Row.GetIndex() + 1 ).ToString() ;
    }

    private void Button_Update( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_DisplaySwitching( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_ExportFile( object sender, RoutedEventArgs e )
    {
      const string fileName = "file_name.dat" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, Filter = "CSV files (*.dat)|*.dat", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      try {
        using ( StreamWriter sw = new StreamWriter( saveFileDialog.FileName ) ) {
          foreach ( var p in _pickUpModels ) {
            List<string> param = new List<string>()
            {
              p.Floor,
              p.ConstructionItems,
              p.EquipmentType,
              p.ProductName,
              p.Use,
              p.Construction,
              p.ModelNumber,
              p.Specification,
              p.Specification2,
              p.Size,
              p.Quantity,
              p.Tani
            } ;
            string line = "\"" + string.Join( "\",\"", param ) + "\"" ;
            sw.WriteLine( line ) ;
          }

          sw.Flush() ;
          sw.Close() ;
        }

        MessageBox.Show( "Export data successfully.", "Result Message" ) ;
      }
      catch ( Exception ex ) {
        MessageBox.Show( "Export data failed because " + ex, "Error Message" ) ;
      }
    }

    private void Button_Delete( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Save( object sender, RoutedEventArgs e )
    {
      try {
        using Transaction t = new Transaction( _document, "Save data" ) ;
        t.Start() ;
        _pickUpStorable.AllPickUpModelData = _pickUpModels ;
        _pickUpStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save Data Failed.", "Error Message" ) ;
        DialogResult = false ;
      }

      DialogResult = true ;
      Close() ;
    }

    private void Button_Cancel( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }

    public enum ProductType
    {
      Connector,
      Conduit,
      Cable
    }

    private List<PickUpModel> GetPickUpData()
    {
      List<PickUpModel> pickUpModels = new List<PickUpModel>() ;
      List<double> quantities = new List<double>() ;
      List<int> pickUpNumbers = new List<int>() ;
      List<string> directionZ = new List<string>() ;
      List<string> constructionItems = new List<string>() ;

      List<Element> allConnector = _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e.GroupId != ElementId.InvalidElementId ).ToList() ;
      foreach ( var connector in allConnector ) {
        connector.TryGetProperty( RoutingFamilyLinkedParameter.ConstructionItem, out string? constructionItem ) ;
        constructionItems.Add( string.IsNullOrEmpty( constructionItem ) ? DefaultConstructionItem : constructionItem! ) ;
      }

      SetPickUpModels( pickUpModels, allConnector, ProductType.Connector, quantities, pickUpNumbers, directionZ, constructionItems ) ;
      var connectors = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      GetToConnectorsOfConduit( connectors, pickUpModels ) ;
      GetToConnectorsOfCables( connectors, pickUpModels ) ;
      return pickUpModels ;
    }

    private void SetPickUpModels( List<PickUpModel> pickUpModels, List<Element> elements, ProductType productType, List<double> quantities, List<int> pickUpNumbers, List<string> directionZ, List<string> constructionItemList )
    {
      var index = 0 ;
      foreach ( var connector in elements ) {
        if ( connector.LevelId == ElementId.InvalidElementId ) continue ;
        var element = _document.GetElement( connector.Id ) ;
        var item = string.Empty ;
        var floor = _document.GetAllElements<Level>().FirstOrDefault( l => l.Id == connector.LevelId )?.Name ;
        var constructionItems = productType != ProductType.Cable ? constructionItemList[ index ] : DefaultConstructionItem ;
        var equipmentType = productType.GetFieldName() ;
        var productName = string.Empty ;
        var use = string.Empty ;
        var usageName = string.Empty ;
        var construction = string.Empty ;
        var modelNumber = string.Empty ;
        var specification = string.Empty ;
        var specification2 = string.Empty ;
        var size = string.Empty ;
        var quantity = productType == ProductType.Connector ? "1" : quantities[ index ].ToString() ;
        var tani = string.Empty ;
        var supplement = string.Empty ;
        var supplement2 = string.Empty ;
        var group = string.Empty ;
        var layer = string.Empty ;
        var classification = string.Empty ;
        var standard = string.Empty ;
        var pickUpNumber = productType == ProductType.Connector ? string.Empty : pickUpNumbers[ index ].ToString() ;
        var direction = productType == ProductType.Conduit ? directionZ[ index ] : string.Empty ;
        var ceeDCodeModel = GetCeeDSetCodeOfElement( element ) ;
        if ( _ceeDModels.Any() && ceeDCodeModel.Any() ) {
          var ceeDSetCode = ceeDCodeModel.First() ;
          var symbol = ceeDCodeModel.Count > 1 ? ceeDCodeModel.ElementAt( 1 ) : string.Empty ;
          modelNumber = ceeDCodeModel.Count > 2 ? ceeDCodeModel.ElementAt( 2 ) : string.Empty ;
          var ceeDModels = _ceeDModels.Where( x => x.CeeDSetCode == ceeDSetCode && x.GeneralDisplayDeviceSymbol == symbol && x.ModelNumber == modelNumber ).ToList() ;
          var ceeDModel = ceeDModels.FirstOrDefault() ;
          if ( ceeDModel != null ) {
            modelNumber = ceeDModel.ModelNumber ;
            specification2 = ceeDModel.CeeDSetCode ;
            supplement = ceeDModel.Name ;

            var ceeDModelNumber = string.Empty ;
            if ( _hiroiSetCdMasterNormalModels.Any() ) {
              var hiroiSetCdMasterNormalModel = _hiroiSetCdMasterNormalModels.FirstOrDefault( h => h.SetCode == ceeDSetCode ) ;
              if ( hiroiSetCdMasterNormalModel != null ) {
                ceeDModelNumber = productType == ProductType.Connector ? hiroiSetCdMasterNormalModel.QuantityParentPartModelNumber : hiroiSetCdMasterNormalModel.LengthParentPartModelNumber ;
                construction = productType == ProductType.Conduit ? hiroiSetCdMasterNormalModel.ConstructionClassification : string.Empty ;
              }
            }

            if ( _hiroiSetMasterNormalModels.Any() && ! string.IsNullOrEmpty( ceeDModelNumber ) ) {
              var hiroiSetMasterNormalModel = _hiroiSetMasterNormalModels.FirstOrDefault( h => h.ParentPartModelNumber == ceeDModelNumber ) ;
              if ( hiroiSetMasterNormalModel != null ) {
                var materialCodes = GetMaterialCodes( hiroiSetMasterNormalModel ) ;
                if ( _hiroiMasterModels.Any() && materialCodes.Any() ) {
                  foreach ( var ( materialCode, name) in materialCodes ) {
                    specification = name ;
                    var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => int.Parse( h.Buzaicd ) == int.Parse( materialCode ) ) ;
                    if ( hiroiMasterModel != null ) {
                      productName = hiroiMasterModel.Hinmei ;
                      size = hiroiMasterModel.Size2 ;
                      tani = hiroiMasterModel.Tani ;
                      standard = hiroiMasterModel.Kikaku ;
                    }

                    if ( productType == ProductType.Connector ) {
                      var pickUpModel = pickUpModels.FirstOrDefault( p => p.Floor == floor && p.ConstructionItems == constructionItems && p.ProductName == productName && p.Construction == construction && p.ModelNumber == modelNumber && p.Specification == specification && p.Specification2 == specification2 && p.Size == size && p.Tani == tani ) ;
                      if ( pickUpModel != null )
                        pickUpModel.Quantity = ( int.Parse( pickUpModel.Quantity ) + 1 ).ToString() ;
                      else {
                        pickUpModel = new PickUpModel( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, group, layer, classification, standard, pickUpNumber, direction, materialCode ) ;
                        pickUpModels.Add( pickUpModel ) ;
                      }
                    }
                    else {
                      PickUpModel pickUpModel = new PickUpModel( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, group, layer, classification, standard, pickUpNumber, direction, materialCode ) ;
                      pickUpModels.Add( pickUpModel ) ;
                    }
                  }
                }
              }
            }
          }
        }
        index++ ;
      }
    }

    private Dictionary<string, string> GetMaterialCodes( HiroiSetMasterModel hiroiSetMasterNormalModel )
    {
      Dictionary<string, string> materialCodes = new Dictionary<string, string>() ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode1 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode1, hiroiSetMasterNormalModel.Name1 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode2 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode2, hiroiSetMasterNormalModel.Name2 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode3 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode3, hiroiSetMasterNormalModel.Name3 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode4 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode4, hiroiSetMasterNormalModel.Name4 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode5 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode5, hiroiSetMasterNormalModel.Name5 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode6 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode6, hiroiSetMasterNormalModel.Name6 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode7 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode7, hiroiSetMasterNormalModel.Name7 ) ;
      if ( ! string.IsNullOrEmpty( hiroiSetMasterNormalModel.MaterialCode8 ) ) materialCodes.Add( hiroiSetMasterNormalModel.MaterialCode8, hiroiSetMasterNormalModel.Name8 ) ;
      return materialCodes ;
    }

    private List<string> GetCeeDSetCodeOfElement( Element element )
    {
      element.TryGetProperty( ConnectorFamilyParameter.CeeDCode, out string? ceeDSetCode ) ;
      return ! string.IsNullOrEmpty( ceeDSetCode ) ? ceeDSetCode!.Split( '-' ).ToList() : new List<string>() ;
    }

    private enum ConduitType
    {
      Conduit,
      ConduitFitting
    }

    private void GetToConnectorsOfConduit( IReadOnlyCollection<Element> allConnectors, List<PickUpModel> pickUpModels )
    {
      _pickUpNumber = 1 ;
      _pickUpNumbers = new Dictionary<int, string>() ;
      List<Element> pickUpConnectors = new List<Element>() ;
      List<double> quantities = new List<double>() ;
      List<int> pickUpNumbers = new List<int>() ;
      List<string> directionZ = new List<string>() ;
      List<string> constructionItems = new List<string>() ;

      var conduits = _document.GetAllElements<Conduit>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduit in conduits ) {
        var quantity = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( _document, "Length" ) ).AsDouble() ;
        var constructionItem = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.ConstructionItem".GetDocumentStringByKeyOrDefault( _document, "Construction Item" ) ).AsString() ;
        AddPickUpConduit( allConnectors, pickUpConnectors, quantities, pickUpNumbers, directionZ, conduit, quantity, ConduitType.Conduit, constructionItems, constructionItem ) ;
      }

      var conduitFittings = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduitFitting in conduitFittings ) {
        var quantity = conduitFitting.ParametersMap.get_Item( "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( _document, "電線管長さ" ) ).AsDouble() ;
        var constructionItem = conduitFitting.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.ConstructionItem".GetDocumentStringByKeyOrDefault( _document, "Construction Item" ) ).AsString() ;
        AddPickUpConduit( allConnectors, pickUpConnectors, quantities, pickUpNumbers, directionZ, conduitFitting, quantity, ConduitType.ConduitFitting, constructionItems, constructionItem ) ;
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, ProductType.Conduit, quantities, pickUpNumbers, directionZ, constructionItems ) ;
    }

    private void GetToConnectorsOfCables( IReadOnlyCollection<Element> allConnectors, List<PickUpModel> pickUpModels )
    {
      List<Element> pickUpConnectors = new List<Element>() ;
      List<double> quantities = new List<double>() ;
      List<int> pickUpNumbers = new List<int>() ;
      List<string> directionZ = new List<string>() ;
      List<string> constructionItems = new List<string>() ;

      var cables = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).Distinct().ToList() ;
      foreach ( var cable in cables ) {
        var elementId = cable.ParametersMap.get_Item( "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( _document, "To-Side Connector Id" ) ).AsString() ;
        var fromElementId = cable.ParametersMap.get_Item( "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( _document, "From-Side Connector Id" ) ).AsString() ;
        if ( string.IsNullOrEmpty( elementId ) ) continue ;
        var checkPickUp = AddPickUpConnectors( allConnectors, pickUpConnectors, elementId, fromElementId, pickUpNumbers ) ;
        if ( ! checkPickUp ) continue ;
        var quantity = cable.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( _document, "トレイ長さ" ) ).AsDouble() ;
        quantities.Add( Math.Round( quantity, 2 ) ) ;
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, ProductType.Cable, quantities, pickUpNumbers, directionZ, constructionItems ) ;
    }

    private void AddPickUpConduit( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, List<double> quantities, List<int> pickUpNumbers, List<string> directionZ, Element conduit, double quantity, ConduitType conduitType, List<string> constructionItems, string constructionItem )
    {
      var routeName = conduit.GetRouteName() ;
      if ( string.IsNullOrEmpty( routeName ) ) return ;
      var checkPickUp = AddPickUpConnectors( allConnectors, pickUpConnectors, routeName!, pickUpNumbers ) ;
      if ( ! checkPickUp ) return ;
      quantities.Add( Math.Round( quantity, 2 ) ) ;
      switch ( conduitType ) {
        case ConduitType.Conduit :
          var location = ( conduit.Location as LocationCurve )! ;
          var line = ( location.Curve as Line )! ;
          var isDirectionZ = line.Direction.Z is 1.0 or -1.0 ? line.Origin.X.ToString() + ", " + line.Origin.Y.ToString() : string.Empty ;
          directionZ.Add( isDirectionZ ) ;
          break ;
        case ConduitType.ConduitFitting :
          directionZ.Add( string.Empty ) ;
          break ;
      }

      constructionItems.Add( string.IsNullOrEmpty( constructionItem ) ? DefaultConstructionItem : constructionItem ) ;
    }

    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, string routeName, List<int> pickUpNumbers )
    {
      var toConnector = GetToConnectorOfRoute( allConnectors, routeName ) ;
      if ( toConnector == null || toConnector.GroupId == ElementId.InvalidElementId ) return false ;
      pickUpConnectors.Add( toConnector ) ;
      if ( ! _pickUpNumbers.ContainsValue( routeName ) ) {
        _pickUpNumbers.Add( _pickUpNumber, routeName ) ;
        pickUpNumbers.Add( _pickUpNumber ) ;
        _pickUpNumber++ ;
      }
      else {
        var pickUpNumber = _pickUpNumbers.FirstOrDefault( n => n.Value == routeName ).Key ;
        pickUpNumbers.Add( pickUpNumber ) ;
      }

      return true ;
    }

    private bool AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, string elementId, string fromElementId, List<int> pickUpNumbers )
    {
      var connector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == elementId ) ;
      if ( connector!.IsTerminatePoint() || connector!.IsPassPoint() ) {
        connector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? connectorId ) ;
        if ( ! string.IsNullOrEmpty( connectorId ) ) {
          connector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == connectorId ) ;
          elementId = connectorId! ;
        }
      }

      if ( ! string.IsNullOrEmpty( fromElementId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == fromElementId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorId, out string? fromConnectorId ) ;
          fromElementId = fromConnectorId! ;
        }
      }

      if ( connector != null && connector.GroupId != ElementId.InvalidElementId ) {
        pickUpConnectors.Add( connector ) ;
        if ( ! _pickUpNumbers.ContainsValue( fromElementId + ", " + elementId ) ) {
          _pickUpNumbers.Add( _pickUpNumber, fromElementId + ", " + elementId ) ;
          pickUpNumbers.Add( _pickUpNumber ) ;
          _pickUpNumber++ ;
        }
        else {
          var pickUpNumber = _pickUpNumbers.FirstOrDefault( n => n.Value == fromElementId + ", " + elementId ).Key ;
          pickUpNumbers.Add( pickUpNumber ) ;
        }

        return true ;
      }

      return false ;
    }

    private List<PickUpModel> PickUpModelByNumber( ProductType productType )
    {
      List<PickUpModel> pickUpModels = new List<PickUpModel>() ;
      var equipmentType = productType.GetFieldName() ;
      var pickUpModelsByNumber = _pickUpModels.Where( p => p.EquipmentType == equipmentType ).GroupBy( x => x.PickUpNumber, ( key, p ) => new { Number = key, PickUpModels = p.ToList() } ) ;
      foreach ( var pickUpModelByNumber in pickUpModelsByNumber ) {
        var pickUpModelsByProductCode = pickUpModelByNumber.PickUpModels.GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
        foreach ( var pickUpModelByProductCode in pickUpModelsByProductCode ) {
          var sumQuantity = pickUpModelByProductCode.PickUpModels.Sum( p => Convert.ToDouble( p.Quantity ) ) ;
          var pickUpModel = pickUpModelByProductCode.PickUpModels.FirstOrDefault() ;
          if ( pickUpModel == null ) continue ;
          PickUpModel newPickUpModel = new PickUpModel( pickUpModel.Item, pickUpModel.Floor, pickUpModel.ConstructionItems, pickUpModel.EquipmentType, pickUpModel.ProductName, pickUpModel.Use, pickUpModel.UsageName, pickUpModel.Construction, pickUpModel.ModelNumber, pickUpModel.Specification, pickUpModel.Specification2, pickUpModel.Size, sumQuantity.ToString(), pickUpModel.Tani, pickUpModel.Supplement, pickUpModel.Supplement2, pickUpModel.Group, pickUpModel.Layer, pickUpModel.Classification, pickUpModel.Standard, pickUpModel.PickUpNumber, pickUpModel.Direction, pickUpModel.ProductCode ) ;
          pickUpModels.Add( newPickUpModel ) ;
        }
      }

      return pickUpModels ;
    }

    private Element? GetToConnectorOfRoute( IReadOnlyCollection<Element> allConnectors, string routeName )
    {
      var conduitsOfRoute = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
        var toElementId = toEndPointKey!.GetElementId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
        if ( toConnector == null || toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) continue ;
        return toConnector ;
      }

      return null ;
    }
  }
}