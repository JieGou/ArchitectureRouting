﻿using System ;
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
        _pickUpModels = ( from pickUpModel in _pickUpModels orderby pickUpModel.Floor ascending select pickUpModel ).ToList() ;
        var viewModel = new PickUpViewModel( _pickUpStorable, _pickUpModels ) ;
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
        var constructionItems = productType == ProductType.Conduit ? constructionItemList[ index ] : DefaultConstructionItem ;
        var equipmentType = productType.GetFieldName() ;
        var productName = string.Empty ;
        var use = string.Empty ;
        var usageName = string.Empty ;
        var construction = string.Empty ;
        var modelNumber = string.Empty ;
        var specification = string.Empty ;
        var specification2 = string.Empty ;
        var size = string.Empty ;
        var quantity = productType == ProductType.Connector ? string.Empty : quantities[ index ].ToString() ;
        var tani = string.Empty ;
        var supplement = string.Empty ;
        var supplement2 = string.Empty ;
        var group = string.Empty ;
        var layer = string.Empty ;
        var classification = string.Empty ;
        var standard = string.Empty ;
        var pickUpNumber = productType == ProductType.Connector ? string.Empty : pickUpNumbers[ index ].ToString() ;
        var direction = productType == ProductType.Conduit ? directionZ[ index ] : string.Empty ;
        var ceeDSetCode = GetCeeDSetCodeOfElement( element ) ;
        if ( _ceeDModels.Any() && ! string.IsNullOrEmpty( ceeDSetCode ) ) {
          var ceeDModel = _ceeDModels.FirstOrDefault( x => x.CeeDSetCode == ceeDSetCode ) ;
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
                specification = hiroiSetMasterNormalModel.Name1 ;
                var materialCode1 = hiroiSetMasterNormalModel.MaterialCode1 ;
                if ( _hiroiMasterModels.Any() && ! string.IsNullOrEmpty( materialCode1 ) ) {
                  var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => int.Parse( h.Buzaicd ) == int.Parse( materialCode1 ) ) ;
                  if ( hiroiMasterModel != null ) {
                    productName = hiroiMasterModel.Hinmei ;
                    size = hiroiMasterModel.Size2 ;
                    tani = hiroiMasterModel.Tani ;
                    standard = hiroiMasterModel.Kikaku ;
                  }
                }
              }
            }
          }
        }

        PickUpModel pickUpModel = new PickUpModel( item, floor, constructionItems, equipmentType, productName, use, usageName, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, group, layer, classification, standard, pickUpNumber, direction ) ;
        pickUpModels.Add( pickUpModel ) ;
        index++ ;
      }
    }

    private string GetCeeDSetCodeOfElement( Element element )
    {
      var ceeDSetCode = string.Empty ;
      if ( element.GroupId == ElementId.InvalidElementId ) return ceeDSetCode ?? string.Empty ;
      var groupId = _document.GetAllElements<Group>().FirstOrDefault( g => g.AttachedParentId == element.GroupId )?.Id ;
      if ( groupId != null )
        ceeDSetCode = _document.GetAllElements<TextNote>().FirstOrDefault( t => t.GroupId == groupId )?.Text.Trim( '\r' ) ;

      return ceeDSetCode ?? string.Empty ;
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
        var fromElementId = cable.ParametersMap.get_Item( "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( _document, "From-Side Connector Id" ) ).AsString() ;
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
      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var endPointKey = toEndPoint.FirstOrDefault()?.Key ;
      var elementId = endPointKey!.GetElementId() ;
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      var fromElementId = fromEndPointKey!.GetElementId() ;
      if ( string.IsNullOrEmpty( elementId ) ) return ;
      var checkPickUp = AddPickUpConnectors( allConnectors, pickUpConnectors, elementId, fromElementId, pickUpNumbers ) ;
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
  }
}