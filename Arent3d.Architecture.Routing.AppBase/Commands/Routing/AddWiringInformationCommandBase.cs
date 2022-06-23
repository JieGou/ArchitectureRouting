using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AddWiringInformationCommandBase : IExternalCommand
  {
    private const string NoPlumping = "配管なし" ;
    private const string DefaultConstructionItems = "未設定" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;

        var csvStorable = document.GetCsvStorable() ;
        var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
        var cnsStorable = document.GetCnsSettingStorable() ;
        var detailSymbolStorable = document.GetDetailSymbolStorable() ;
        
        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to get info.", AddInType.Electrical ) ;
        
        CreateDetailSymbolModel( document, pickInfo.Element, csvStorable, detailSymbolStorable) ;
        var conduits = new List<Element> { pickInfo.Element } ;
        var elementIds = new List<string> { pickInfo.Element.UniqueId } ;
        var ( detailTableModels, _, _) = CreateDetailTableCommandBase.CreateDetailTableAddWiringInfo( document, csvStorable, detailSymbolStorable, conduits, elementIds, false ) ;

        var hiroiMasterModels = csvStorable.HiroiMasterModelData.Where( x => x.Type == "LAN" && x.Size1 == "ケーブル" ) ;
        
        if ( IsExistSymBol( detailTableModels ) ) {
          MessageBox.Show(@"You must select route don't have symbol", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return Result.Cancelled ;
        }
        
        var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
        var conduitTypes = ( from conduitTypeName in conduitTypeNames select new DetailTableModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
        conduitTypes.Add( new DetailTableModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;

        var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
        var constructionItems = constructionItemNames.Any()
          ? ( from constructionItemName in constructionItemNames select new DetailTableModel.ComboboxItemType( constructionItemName, constructionItemName ) ).ToList()
          : new List<DetailTableModel.ComboboxItemType>() { new(DefaultConstructionItems, DefaultConstructionItems) } ;

        var levelNames = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).Select( l => l.Name ).ToList() ;
        var levels = ( from levelName in levelNames select new DetailTableModel.ComboboxItemType( levelName, levelName ) ).ToList() ;

        var wireTypeNames = wiresAndCablesModelData.Select( w => w.WireType ).Distinct().ToList() ;
        var wireTypes = ( from wireType in wireTypeNames select new DetailTableModel.ComboboxItemType( wireType, wireType ) ).ToList() ;

        var earthTypes = new List<DetailTableModel.ComboboxItemType>() { new("IV", "IV"), new("EM-IE", "EM-IE") } ;

        var numbers = new List<DetailTableModel.ComboboxItemType>() ;
        for ( var i = 1 ; i <= 10 ; i++ ) {
          numbers.Add( new DetailTableModel.ComboboxItemType( i.ToString(), i.ToString() ) ) ;
        }

        var constructionClassificationTypeNames = hiroiSetCdMasterNormalModelData.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
        var constructionClassificationTypes = ( from constructionClassification in constructionClassificationTypeNames select new DetailTableModel.ComboboxItemType( constructionClassification, constructionClassification ) ).ToList() ;

        var signalTypes = ( from signalType in (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType ) ) select new DetailTableModel.ComboboxItemType( signalType.GetFieldName(), signalType.GetFieldName() ) ).ToList() ;

        var viewModel = new DetailTableViewModel( document, detailTableModels, new ObservableCollection<DetailTableModel>(), conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes, conduitsModelData, wiresAndCablesModelData, false, true ) ;
        var dialog = new DetailTableDialog(  viewModel ) ;
        dialog.ShowDialog() ;
        
        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    private bool IsExistSymBol(IEnumerable<DetailTableModel> detailTableModels)
    {
      return detailTableModels.Any( x => x.DetailSymbol != "*" ) ;
    }

    public void CreateDetailSymbolModel( Document document, Element pickConduit, CsvStorable csvStorable, DetailSymbolStorable detailSymbolStorable )
    {
      var allConduit = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).ToList() ;
      var representativeRouteName = ( (Conduit) pickConduit ).GetRepresentativeRouteName() ;
      var routeNameSamePosition = GetRouteNameSamePosition( document, representativeRouteName!, pickConduit ) ;
      
      foreach ( var routeName in routeNameSamePosition ) {
        var conduitOfRoutes = allConduit.Where( c => c.GetRouteName() == routeName ).ToList() ;
        var toConnector = ConduitUtil.GetConnectorOfRoute( document, routeName!, false ) ;
        if ( null == toConnector ) 
          continue ;
        
        toConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCodeModel ) ;
        toConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? connectorIsEcoMode ) ;
        var ceedSetCode = ceedSetCodeModel?.Split( ':' ).ToList() ;
        var ceedCode = ceedSetCode?[ 0 ] ;
        
        var isEcoMode = bool.TryParse( connectorIsEcoMode, out var value ) && value ;
        var hiroiSetCdMasterModels = isEcoMode ? csvStorable.HiroiSetCdMasterEcoModelData : csvStorable.HiroiSetCdMasterNormalModelData ;
        var hiroiSetCdMasterModel = hiroiSetCdMasterModels.FirstOrDefault( h => h.SetCode == ceedCode ) ;
        if(null == hiroiSetCdMasterModel)
          continue;
        
        var hiroiSetMasterModels = isEcoMode ? csvStorable.HiroiSetMasterEcoModelData : csvStorable.HiroiSetMasterNormalModelData ;
        var hiroiSetMasterModel = hiroiSetMasterModels.FirstOrDefault( h => h.ParentPartModelNumber == hiroiSetCdMasterModel.LengthParentPartModelNumber ) ;
        if(null == hiroiSetMasterModel)
          continue;
        
        var plumbingType = GetPlumpingType( hiroiSetMasterModel, csvStorable.ConduitsModelData ) ;
        
        foreach ( var conduitOfRoute in conduitOfRoutes ) {
          var detailSymbolModel = new DetailSymbolModel( toConnector.UniqueId, "*", conduitOfRoute.UniqueId, routeName, ceedCode, conduitOfRoute.Id.ToString(), false, 1, ceedSetCode?.Count> 2? ceedSetCode[1] : string.Empty, plumbingType ) ;
          if(null == detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( x => x.DetailSymbolId == detailSymbolModel.DetailSymbolId && x.ConduitId == detailSymbolModel.ConduitId )) 
            detailSymbolStorable.DetailSymbolModelData.Add( detailSymbolModel );
        } 
      } 
    }

    private string GetPlumpingType( HiroiSetMasterModel hiroiSetMasterModel, List<ConduitsModel> conduitsModels)
    {
      if ( string.IsNullOrEmpty( hiroiSetMasterModel.Name2 ) )
        return NoPlumping ;

      var conduitsModel = conduitsModels.FirstOrDefault( x => $"{x.PipingType}{x.Size}".Equals( hiroiSetMasterModel.Name2 ) ) ;
      if ( null != conduitsModel )
        return conduitsModel.PipingType ;

      return NoPlumping ;
    }

    public List<string> GetRouteNameSamePosition( Document doc, string representativeRouteName, Element pickConduit )
    {
      List<string> routeNames = new List<string>() ;
      if ( pickConduit is Conduit ) {
        var conduits = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).ToList() ;
        var location = ( pickConduit.Location as LocationCurve ) ! ;
        var line = ( location.Curve as Line ) ! ;
        var origin = line.Origin ;
        var direction = line.Direction ;
        foreach ( var conduit in conduits ) {
          var anotherLocation = ( conduit.Location as LocationCurve ) ! ;
          var anotherLine = ( anotherLocation.Curve as Line ) ! ;
          var anotherOrigin = anotherLine.Origin ;
          var anotherDirection = anotherLine.Direction ;
          if ( anotherOrigin.DistanceTo( origin ) < GeometryHelper.Tolerance && 
               anotherDirection.DistanceTo( direction ) < GeometryHelper.Tolerance && 
               ! routeNames.Contains( conduit.GetRouteName()! ))
            routeNames.Add( conduit.GetRouteName()! ) ;
        }
      }
      else {
        var routeNamesSamePosition = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).Select( c => c.GetRouteName() ).Distinct().ToList() ;
        var conduitFittingsOfRoutes = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => routeNamesSamePosition.Contains( c.GetRouteName() ) ).ToList() ;
        var pickConduitFitting = doc.GetElementById<FamilyInstance>( pickConduit.Id )! ;
        var location = ( pickConduitFitting.Location as LocationPoint )! ;
        var origin = location.Point ;
        var direction = pickConduitFitting.FacingOrientation ;
        foreach ( var conduitFitting in conduitFittingsOfRoutes ) {
          var anotherConduitFitting = doc.GetElementById<FamilyInstance>( conduitFitting.Id )! ;
          var anotherLocation = ( anotherConduitFitting.Location as LocationPoint )! ;
          var anotherOrigin = anotherLocation.Point ;
          var anotherDirection = anotherConduitFitting.FacingOrientation ;
          if ( anotherOrigin.DistanceTo( origin ) == 0 && anotherDirection.DistanceTo( direction ) == 0 && ! routeNames.Contains( conduitFitting.GetRouteName()! ) )
            routeNames.Add( conduitFitting.GetRouteName()! ) ;
        }
      }

      return routeNames ;
    }
  }
}