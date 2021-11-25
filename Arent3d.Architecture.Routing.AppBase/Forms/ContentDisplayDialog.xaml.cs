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
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ContentDisplayDialog : Window
  {
    private readonly Document _document ;
    private List<PickUpModel> _pickUpModels ;
    private PickUpStorable _pickUpStorable ;
    private readonly List<CeedModel> _ceeDModels ;
    private readonly List<HiroiSetMasterModel> _hiroiSetMasterNormalModels ;
    private readonly List<HiroiMasterModel> _hiroiMasterModels ;
    private readonly List<HiroiSetCdMasterModel> _hiroiSetCdMasterNormalModels ;
    private readonly Dictionary<string, string> _productType = new Dictionary<string, string>() { { "Connector", "コネクター" }, { "Conduit", "配線" }, { "Cable", "ケーブルラック" } } ;

    public ContentDisplayDialog( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _ceeDModels = new List<CeedModel>() ;
      _hiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _hiroiMasterModels = new List<HiroiMasterModel>() ;
      _hiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;

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
      const string fileName = "ドーコンOFF.dat" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, Filter = "CSV files (*.dat)|*.dat", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      try {
        using ( StreamWriter sw = new StreamWriter( saveFileDialog.FileName ) ) {
          foreach ( var pickUpModel in _pickUpModels ) {
            string line = "\"" + pickUpModel.ProductName + "\",\"" + pickUpModel.ModelNumber + "\"" ;
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

    private List<PickUpModel> GetPickUpData()
    {
      List<PickUpModel> pickUpModels = new List<PickUpModel>() ;
      List<Element> allConnector = _document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e.GroupId != ElementId.InvalidElementId ).ToList() ;
      SetPickUpModels( pickUpModels, allConnector, _productType.ElementAt( 0 ).Value ) ;
      var connectors = _document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      GetToConnectorsOfConduit( connectors, pickUpModels ) ;
      GetToConnectorsOfCables( connectors, pickUpModels ) ;
      return pickUpModels ;
    }

    private void SetPickUpModels( List<PickUpModel> pickUpModels, List<Element> elements, string productType )
    {
      foreach ( var connector in elements ) {
        if ( connector.LevelId == ElementId.InvalidElementId ) continue ;
        var element = _document.GetElement( connector.Id ) ;
        var item = string.Empty ;
        var floor = _document.GetAllElements<Level>().FirstOrDefault( l => l.Id == connector.LevelId )?.Name ;
        var constructionItems = string.Empty ;
        var facility = string.Empty ;
        var productName = string.Empty ;
        var use = string.Empty ;
        var construction = string.Empty ;
        var modelNumber = string.Empty ;
        var specification = string.Empty ;
        var specification2 = string.Empty ;
        var size = string.Empty ;
        var quantity = string.Empty ;
        var tani = string.Empty ;
        var ceeDSetCode = GetCeeDSetCodeOfElement( element ) ;
        if ( _ceeDModels.Any() && ! string.IsNullOrEmpty( ceeDSetCode ) ) {
          var ceeDModel = _ceeDModels.FirstOrDefault( x => x.CeeDSetCode == ceeDSetCode ) ;
          if ( ceeDModel != null ) {
            modelNumber = ceeDModel.CeeDModelNumber ;
            specification2 = ceeDModel.CeeDSetCode ;
            if ( _hiroiSetMasterNormalModels.Any() && ! string.IsNullOrEmpty( modelNumber ) ) {
              var hiroiSetMasterNormalModel = _hiroiSetMasterNormalModels.FirstOrDefault( h => h.ParentPartModelNumber == modelNumber ) ;
              if ( hiroiSetMasterNormalModel != null ) {
                specification = hiroiSetMasterNormalModel.ParentPartName ;
                quantity = hiroiSetMasterNormalModel.ParentPartsQuantity ;
                var materialCode1 = hiroiSetMasterNormalModel.MaterialCode1 ;
                if ( _hiroiMasterModels.Any() && ! string.IsNullOrEmpty( materialCode1 ) ) {
                  var hiroiMasterModel = _hiroiMasterModels.FirstOrDefault( h => int.Parse( h.Buzaicd ) == int.Parse( materialCode1 ) ) ;
                  if ( hiroiMasterModel != null ) {
                    facility = hiroiMasterModel.Setubisyu + "（" + productType + "）" ;
                    productName = hiroiMasterModel.Hinmei ;
                    size = hiroiMasterModel.Size2 ;
                    tani = hiroiMasterModel.Tani ;
                  }
                }
              }
            }

            if ( _hiroiSetCdMasterNormalModels.Any() ) {
              var hiroiSetCdMasterNormalModel = _hiroiSetCdMasterNormalModels.FirstOrDefault( h => h.SetCode == ceeDSetCode ) ;
              if ( hiroiSetCdMasterNormalModel != null )
                construction = hiroiSetCdMasterNormalModel.ConstructionClassification ;
            }
          }
        }

        var supplement = string.Empty ;
        var supplement2 = string.Empty ;
        var glue = string.Empty ;
        var layer = string.Empty ;
        var classification = string.Empty ;
        PickUpModel pickUpModel = new PickUpModel( item, floor, constructionItems, facility, productName, use, construction, modelNumber, specification, specification2, size, quantity, tani, supplement, supplement2, glue, layer, classification ) ;
        pickUpModels.Add( pickUpModel ) ;
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

    private void GetToConnectorsOfConduit( IReadOnlyCollection<Element> allConnectors, List<PickUpModel> pickUpModels )
    {
      List<Element> pickUpConnectors = new List<Element>() ;
      var conduits = _document.GetAllElements<Conduit>().OfCategory( BuiltInCategorySets.Conduits ).Distinct().ToList() ;
      foreach ( var conduit in conduits ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ) ;
        var endPointKey = toEndPoint.FirstOrDefault()?.Key ;
        var elementId = endPointKey!.GetElementId() ;
        if ( string.IsNullOrEmpty( elementId ) ) continue ;
        AddPickUpConnectors( allConnectors, pickUpConnectors, elementId ) ;
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, _productType.ElementAt( 1 ).Value ) ;
    }

    private void GetToConnectorsOfCables( IReadOnlyCollection<Element> allConnectors, List<PickUpModel> pickUpModels )
    {
      List<Element> pickUpConnectors = new List<Element>() ;
      var cables = _document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).Distinct().ToList() ;
      foreach ( var cable in cables ) {
        var elementId = cable.ParametersMap.get_Item( "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( _document, "To-Side Connector Id" ) ).AsString() ;
        if ( string.IsNullOrEmpty( elementId ) ) continue ;
        AddPickUpConnectors( allConnectors, pickUpConnectors, elementId ) ;
      }

      SetPickUpModels( pickUpModels, pickUpConnectors, _productType.ElementAt( 2 ).Value ) ;
    }

    private void AddPickUpConnectors( IReadOnlyCollection<Element> allConnectors, List<Element> pickUpConnectors, string elementId )
    {
      var connector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == elementId ) ;
      if ( connector!.IsTerminatePoint() ) {
        connector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? connectorId ) ;
        if ( ! string.IsNullOrEmpty( connectorId ) )
          connector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == connectorId ) ;
      }

      if ( connector != null && connector.GroupId != ElementId.InvalidElementId ) {
        pickUpConnectors.Add( connector ) ;
      }
    }
  }
}