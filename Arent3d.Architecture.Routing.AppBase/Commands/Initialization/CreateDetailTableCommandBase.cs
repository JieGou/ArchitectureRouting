using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class CreateDetailTableCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      const string defaultParentPlumbingType = "E" ;
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var csvStorable = doc.GetCsvStorable() ;
      var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = doc.GetCsvStorable().HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
      var hiroiSetCdMasterEcoModelData = doc.GetCsvStorable().HiroiSetCdMasterEcoModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      var detailTableModelsData = doc.GetDetailTableStorable().DetailTableModelData ;
      ObservableCollection<DetailTableModel> detailTableModels = new ObservableCollection<DetailTableModel>() ;
      var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      CnsSettingStorable cnsStorable = doc.GetCnsSettingStorable() ;
      try {
        var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
        var pickedObjectIds = pickedObjects.Select( p => p.UniqueId ).ToList() ;
        var detailSymbolModelsByDetailSymbolId = detailSymbolStorable.DetailSymbolModelData.Where( x => pickedObjectIds.Contains( x.ConduitId ) ).OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.IsParentSymbol ).GroupBy( x => x.DetailSymbolId, ( key, p ) => new { DetailSymbolId = key, DetailSymbolModels = p.ToList() } ) ;
        foreach ( var detailSymbolModelByDetailSymbolId in detailSymbolModelsByDetailSymbolId ) {
          var firstDetailSymbolModelByDetailSymbolId = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault() ;
          var routeNames = detailSymbolModelByDetailSymbolId.DetailSymbolModels.Select( d => d.RouteName ).Distinct().ToList() ;
          var parentRouteName = firstDetailSymbolModelByDetailSymbolId!.CountCableSamePosition == 1 ? firstDetailSymbolModelByDetailSymbolId.RouteName : GetParentRouteName( doc, routeNames ) ;
          if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
            var parentDetailSymbolModel = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == parentRouteName ) ;
            AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, pickedObjects, parentDetailSymbolModel!, true ) ;
            routeNames = routeNames.Where( n => n != parentRouteName ).OrderByDescending( n => n ).ToList() ;
          }

          foreach ( var childDetailSymbolModel in from routeName in routeNames select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == routeName ) ) {
            AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, pickedObjects, childDetailSymbolModel, false ) ;
          }
        }

        SortDetailTableModel( ref detailTableModels ) ;
        SetPlumbingData( conduitsModelData, ref detailTableModels, defaultParentPlumbingType ) ;
      }
      catch {
        return Result.Cancelled ;
      }

      var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
      List<ComboboxItemType> conduitTypes = ( from conduitTypeName in conduitTypeNames select new ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;

      var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
      List<ComboboxItemType> constructionItems = ( from constructionItemName in constructionItemNames select new ComboboxItemType( constructionItemName, constructionItemName ) ).ToList() ;

      DetailTableViewModel viewModel = new DetailTableViewModel( detailTableModels, conduitTypes, constructionItems ) ;
      var dialog = new DetailTableDialog( doc, viewModel, conduitsModelData ) ;
      dialog.ShowDialog() ;

      if ( dialog.DialogResult ?? false ) {
        if ( dialog.RoutesWithConstructionItemHasChanged.Any() ) {
          var connectorGroups = UpdateConnectorAndConduitConstructionItem( doc, dialog.RoutesWithConstructionItemHasChanged ) ;
          if ( connectorGroups.Any() ) {
            using Transaction transaction = new Transaction( doc, "Group connector" ) ;
            transaction.Start() ;
            foreach ( var (connectorId, textNoteIds) in connectorGroups ) {
              // create group for updated connector (with new property) and related text note if any
              List<ElementId> groupIds = new List<ElementId> { connectorId } ;
              groupIds.AddRange( textNoteIds ) ;
              doc.Create.NewGroup( groupIds ) ;
            }

            transaction.Commit() ;
          }
        }

        if ( dialog.DetailSymbolIdsWithPlumbingTypeHasChanged.Any() ) {
          UpdateDetailSymbolPlumbingType( doc, detailSymbolStorable, dialog.DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
        }

        return doc.Transaction( "TransactionName.Commands.Routing.CreateDetailTable".GetAppStringByKeyOrDefault( "Set detail table" ), _ =>
        {
          if ( viewModel.IsCreateDetailTableOnFloorPlanView || dialog.DetailTableViewModelSummary.IsCreateDetailTableOnFloorPlanView ) {
            var (originX, originY, originZ) = uiDoc.Selection.PickPoint() ;
            var level = uiDoc.ActiveView.GenLevel ;
            var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

            ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
            var noteWidth = 0.4 ;
            TextNoteOptions opts = new( defaultTextTypeId ) ;
            var txtPosition = new XYZ( originX, originY, heightOfConnector ) ;
            TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, GenerateTextTable( viewModel.IsCreateDetailTableOnFloorPlanView ? viewModel : dialog.DetailTableViewModelSummary, level.Name ), opts ) ;
            viewModel.IsCreateDetailTableOnFloorPlanView = false ;
            dialog.DetailTableViewModelSummary.IsCreateDetailTableOnFloorPlanView = false ;
          }

          return Result.Succeeded ;
        } ) ;
      }
      else {
        return Result.Cancelled ;
      }
    }

    private string GenerateTextTable( DetailTableViewModel viewModel, string level )
    {
      string line = new string( '＿', 32 ) ;
      string result = string.Empty ;
      var detailTableModels = viewModel.DetailTableModels ;
      var maxWireType = detailTableModels.Max( x => ( x.WireType + x.WireSize ).Length ) ;
      var maxWireStrip = detailTableModels.Max( x => x.WireStrip?.Length ) ?? 0 ;
      var maxPlumbingType = detailTableModels.Max( x => ( x.PlumbingType + x.PlumbingSize ).Length ) ;
      var detailTableDictionary = detailTableModels.GroupBy( x => x.DetailSymbol ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      result += $"{line}\r{level}階平面图" ;
      foreach ( var group in detailTableDictionary ) {
        result += $"\r{line}\r{group.Key}" ;
        result = @group.Value.Aggregate( result, ( current, item ) => current + $"\r{line}\r{AddFullString( item.WireType + item.WireSize, maxWireType )}\t-{AddFullString( item.WireStrip ?? string.Empty, maxWireStrip )}\tX1\t{AddFullString( CheckEmptyString( item.PlumbingType + item.PlumbingSize, maxPlumbingType ), maxPlumbingType )}\t{item.Remark}" ) ;
      }

      result += $"\r{line}" ;
      return result ;
    }

    private string CheckEmptyString( string str, int lenght )
    {
      return ! string.IsNullOrEmpty( str ) ? $"({str})" : new string( '　', lenght ) ;
    }

    private string AddFullString( string str, int length )
    {
      if ( str.Length < length ) {
        str += new string( '　', length - str.Length ) ;
      }

      return str ;
    }

    private static void SortDetailTableModel( ref ObservableCollection<DetailTableModel> detailTableModels )
    {
      var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      List<DetailTableModel> sortedDetailTableModelsList = new() ;
      foreach ( var detailSymbolId in detailTableModelsGroupByDetailSymbolId.Keys ) {
        List<DetailTableModel> detailTableRowsByDetailSymbolId = detailTableModelsGroupByDetailSymbolId[ detailSymbolId ]!.OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.SignalType ).ToList() ;
        var detailTableRowsBySignalType = detailTableRowsByDetailSymbolId.GroupBy( d => d.SignalType ).OrderByDescending( g => g.ToList().Any( d => d.IsParentRoute ) ).Select( g => g.ToList() ).ToList() ;
        foreach ( var item in detailTableRowsBySignalType ) {
          sortedDetailTableModelsList.AddRange( item ) ;
        }
      }

      detailTableModels = new ObservableCollection<DetailTableModel>( sortedDetailTableModelsList ) ;
    }

    private static void SetPlumbingData( List<ConduitsModel> conduitsModelData, ref ObservableCollection<DetailTableModel> detailTableModels, string plumbingType )
    {
      var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      foreach ( var detailSymbolId in detailTableModelsGroupByDetailSymbolId.Keys ) {
        List<DetailTableModel> detailTableRowsByDetailSymbolId = detailTableModelsGroupByDetailSymbolId[ detailSymbolId ]! ;
        SetPlumbingDataForOneSymbol( conduitsModelData, ref detailTableRowsByDetailSymbolId, plumbingType ) ;
      }
    }

    protected internal static void SetPlumbingDataForOneSymbol( List<ConduitsModel> conduitsModelData, ref List<DetailTableModel> detailTableModelsByDetailSymbolId, string plumbingType, bool isPlumbingTypeHasBeenChanged = false )
    {
      const double percentage = 0.32 ;
      const string defaultChildPlumbingSymbol = "↑" ;
      var plumbingCount = 0 ;

      if ( ! isPlumbingTypeHasBeenChanged ) {
        var parentDetailRow = detailTableModelsByDetailSymbolId.First() ;
        if ( parentDetailRow != null ) plumbingType = string.IsNullOrEmpty( parentDetailRow.PlumbingType ) ? plumbingType : parentDetailRow.PlumbingType ;
      }
      var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
      var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
      var detailTableModelsBySignalType = detailTableModelsByDetailSymbolId.GroupBy( d => d.SignalType ).Select( g =>  g.ToList()).ToList() ;

      foreach ( var detailTableRows in detailTableModelsBySignalType ) {
        Dictionary<string, List<DetailTableModel>> detailTableRowsGroupByPlumbingType = new Dictionary<string, List<DetailTableModel>>() ;
        List<DetailTableModel> childDetailRows = new List<DetailTableModel>() ;
        var parentDetailRow = detailTableRows.First() ;
        var currentPlumbingCrossSectionalArea = 0.0 ;
        foreach ( var currentDetailTableRow in detailTableRows ) {
          currentPlumbingCrossSectionalArea += currentDetailTableRow.WireCrossSectionalArea / percentage ;

          if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
            var plumbing = conduitsModels.Last() ;
            parentDetailRow.PlumbingType = parentDetailRow.IsParentRoute ? plumbingType : plumbingType + defaultChildPlumbingSymbol ;
            parentDetailRow.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
            parentDetailRow.PlumbingIdentityInfo = parentDetailRow.PlumbingType + parentDetailRow.PlumbingSize + "-" + parentDetailRow.SignalType + "-" + parentDetailRow.RouteName ;
            if ( ! detailTableRowsGroupByPlumbingType.ContainsKey( parentDetailRow.PlumbingIdentityInfo ) )
              detailTableRowsGroupByPlumbingType.Add( parentDetailRow.PlumbingIdentityInfo, childDetailRows ) ;
            else {
              detailTableRowsGroupByPlumbingType[ parentDetailRow.PlumbingIdentityInfo ].AddRange( childDetailRows ) ;
            }
            childDetailRows = new List<DetailTableModel>() ;
            plumbingCount++ ;
            parentDetailRow = currentDetailTableRow ;
            currentPlumbingCrossSectionalArea = currentDetailTableRow.WireCrossSectionalArea ;
            if ( currentDetailTableRow != detailTableRows.Last() ) continue ;
            plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea - currentDetailTableRow.WireCrossSectionalArea ) ;
            currentDetailTableRow.PlumbingType = currentDetailTableRow == detailTableModelsByDetailSymbolId.First() ? plumbingType : plumbingType + defaultChildPlumbingSymbol ;
            currentDetailTableRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
            currentDetailTableRow.PlumbingIdentityInfo = plumbingType + currentDetailTableRow.PlumbingSize + "-" + currentDetailTableRow.SignalType + "-" + currentDetailTableRow.RouteName ;
            plumbingCount++ ;
          }
          else {
            if ( currentDetailTableRow == detailTableRows.Last() ) {
              var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
              parentDetailRow.PlumbingType = parentDetailRow.IsParentRoute ? plumbingType : plumbingType + defaultChildPlumbingSymbol ;
              parentDetailRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
              parentDetailRow.PlumbingIdentityInfo = parentDetailRow.PlumbingType + parentDetailRow.PlumbingSize + "-" + parentDetailRow.SignalType + "-" + parentDetailRow.RouteName ;
              if ( ! detailTableRowsGroupByPlumbingType.ContainsKey( parentDetailRow.PlumbingIdentityInfo ) )
                detailTableRowsGroupByPlumbingType.Add( parentDetailRow.PlumbingIdentityInfo, childDetailRows ) ;
              else {
                detailTableRowsGroupByPlumbingType[ parentDetailRow.PlumbingIdentityInfo ].AddRange( childDetailRows ) ;
                detailTableRowsGroupByPlumbingType[ parentDetailRow.PlumbingIdentityInfo ].Add( currentDetailTableRow ) ;
              }
              plumbingCount++ ;
            }

            if ( currentDetailTableRow == detailTableRows.First() ) continue ;
            currentDetailTableRow.PlumbingType = defaultChildPlumbingSymbol ;
            currentDetailTableRow.PlumbingSize = defaultChildPlumbingSymbol ;
            currentDetailTableRow.NumberOfPlumbing = defaultChildPlumbingSymbol ;
            childDetailRows.Add( currentDetailTableRow ) ;
          }
        }

        foreach ( var (parentPlumbingType, detailTableRowsWithSamePlumbing) in detailTableRowsGroupByPlumbingType ) {
          foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
            detailTableRow.PlumbingIdentityInfo = parentPlumbingType ;
          }
        }
      }

      foreach ( var detailTableRowsWithSameSignalType in detailTableModelsBySignalType ) {
        foreach ( var detailTableRow in detailTableRowsWithSameSignalType.Where( d => d.PlumbingSize != defaultChildPlumbingSymbol ).ToList() ) {
          detailTableRow.NumberOfPlumbing = plumbingCount.ToString() ;
        }
      }
    }

    private Dictionary<ElementId, List<ElementId>> UpdateConnectorAndConduitConstructionItem( Document document, Dictionary<string, string> routesChangedConstructionItem )
    {
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      List<Element> allConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).ToList() ;
      using Transaction transaction = new Transaction( document, "Group connector" ) ;
      transaction.Start() ;
      foreach ( var (routeName, constructionItem) in routesChangedConstructionItem ) {
        var elements = GetToConnectorAndConduitOfRoute( document, allConnector, routeName ) ;
        foreach ( var element in elements ) {
          var parentGroup = document.GetElement( element.GroupId ) as Group ;
          if ( parentGroup != null ) {
            // ungroup before set property
            var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
            List<ElementId> listTextNoteIds = new List<ElementId>() ;
            // ungroup textNote before ungroup connector
            foreach ( var group in attachedGroup ) {
              var ids = @group.GetMemberIds() ;
              listTextNoteIds.AddRange( ids ) ;
              @group.UngroupMembers() ;
            }

            connectorGroups.Add( element.Id, listTextNoteIds ) ;
            parentGroup.UngroupMembers() ;
          }

          element.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem ) ;
        }
      }

      transaction.Commit() ;

      return connectorGroups ;
    }

    private void UpdateDetailSymbolPlumbingType( Document document, DetailSymbolStorable detailSymbolStorable, Dictionary<string, string> detailSymbolsChangedPlumbingType )
    {
      using Transaction transaction = new Transaction( document, "Update Detail Symbol Data" ) ;
      transaction.Start() ;
      foreach ( var (detailSymbolId, plumbingType) in detailSymbolsChangedPlumbingType ) {
        var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbolId == detailSymbolId ).ToList() ;
        foreach ( var detailSymbolModel in detailSymbolModels ) {
          detailSymbolModel.PlumbingType = plumbingType ;
        }
      }

      detailSymbolStorable.Save() ;
      transaction.Commit() ;
    }

    private List<Element> GetToConnectorAndConduitOfRoute( Document document, IReadOnlyCollection<Element> allConnectors, string routeName )
    {
      var conduitsAndConnectorOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsAndConnectorOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementUniqueId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementUniqueId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
        conduitsAndConnectorOfRoute.Add( toConnector ) ;
        return conduitsAndConnectorOfRoute ;
      }

      return conduitsAndConnectorOfRoute ;
    }

    private string GetParentRouteName( Document document, List<string> routeNames )
    {
      foreach ( var routeName in routeNames ) {
        var route = document.CollectRoutes( AddInType.Electrical ).FirstOrDefault( x => x.RouteName == routeName ) ;
        if ( route == null ) continue ;
        var parentRouteName = route.GetParentBranches().ToList().LastOrDefault()?.RouteName ;
        if ( string.IsNullOrEmpty( parentRouteName ) || parentRouteName == routeName ) {
          return routeName ;
        }
      }

      return string.Empty ;
    }

    private void AddDetailTableModelRow( Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<DetailTableModel> detailTableModelsData, ICollection<DetailTableModel> detailTableModels, List<Element> pickedObjects, DetailSymbolModel detailSymbolModel, bool isParentRoute )
    {
      var ceedCode = string.Empty ;
      var constructionClassification = string.Empty ;
      var signalType = string.Empty ;
      var wireType = string.Empty ;
      var wireSize = string.Empty ;
      var wireStrip = string.Empty ;
      var remark = string.Empty ;
      var groupId = string.Empty ;
      double wireCrossSectionalArea = 0 ;
      var element = pickedObjects.FirstOrDefault( p => p.UniqueId == detailSymbolModel.ConduitId ) ;
      string floor = doc.GetElementById<Level>( element!.GetLevelId() )?.Name ?? string.Empty ;
      string constructionItem = element!.LookupParameter( "Construction Item" ).AsString() ?? string.Empty ;
      string isEcoMode = element.LookupParameter( "IsEcoMode" ).AsString() ;
      string plumbingType = detailSymbolModel.PlumbingType ;

      var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == detailSymbolModel.Code && x.GeneralDisplayDeviceSymbol == detailSymbolModel.DeviceSymbol ) ;
      if ( ceedModel != null && ! string.IsNullOrEmpty( ceedModel.CeedSetCode ) && ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ) {
        ceedCode = ceedModel.CeedSetCode ;
        remark = ceedModel.GeneralDisplayDeviceSymbol ;
        var hiroiCdModel = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetCdMasterEcoModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) : hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) ;
        var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) ;
        constructionClassification = hiroiCdModel?.ConstructionClassification ;
        foreach ( var item in hiroiSetModels ) {
          List<string> listMaterialCode = new List<string>() ;
          if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
            listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
          }

          if ( ! listMaterialCode.Any() ) continue ;
          var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
          foreach ( var master in masterModels ) {
            wireType = master.Type ;
            wireSize = master.Size1 ;
            wireStrip = master.Size2 ;
            var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && wireStrip == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && wireStrip == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel == null ) continue ;
            signalType = wiresAndCablesModel.Classification ;
            wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
          }
        }
      }

      if ( detailTableModelsData.Any() ) {
        var oldDetailTableRow = detailTableModelsData.FirstOrDefault( d => d.DetailSymbolId == detailSymbolModel.DetailSymbolId && d.RouteName == detailSymbolModel.RouteName ) ;
        groupId = oldDetailTableRow == null ? string.Empty : oldDetailTableRow.GroupId ;
      }

      var detailTableRow = new DetailTableModel( false, floor, ceedCode, detailSymbolModel.DetailSymbol, detailSymbolModel.DetailSymbolId, wireType, wireSize, wireStrip, "1", string.Empty, string.Empty, string.Empty, plumbingType, string.Empty, string.Empty, constructionClassification, signalType, constructionItem, constructionItem, remark, wireCrossSectionalArea, detailSymbolModel.CountCableSamePosition, detailSymbolModel.RouteName, isEcoMode, isParentRoute, ! isParentRoute, string.Empty, groupId ) ;
      detailTableModels.Add( detailTableRow ) ;
    }

    public class ComboboxItemType
    {
      public string Type { get ; set ; }
      public string Name { get ; set ; }

      public ComboboxItemType( string type, string name )
      {
        Type = type ;
        Name = name ;
      }
    }
  }
}