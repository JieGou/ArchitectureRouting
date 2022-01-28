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
      ObservableCollection<DetailTableModel> detailTableModels = new ObservableCollection<DetailTableModel>() ;
      var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      CnsSettingStorable cnsStorable = doc.GetCnsSettingStorable() ;
      try {
        var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
        var pickedObjectIds = pickedObjects.Select( p => p.Id.IntegerValue.ToString() ).ToList() ;
        var detailSymbolModelsByDetailSymbolId = detailSymbolStorable.DetailSymbolModelData.Where( x => pickedObjectIds.Contains( x.ConduitId ) ).OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.IsParentSymbol ).GroupBy( x => x.DetailSymbolId, ( key, p ) => new { DetailSymbolId = key, DetailSymbolModels = p.ToList() } ) ;
        foreach ( var detailSymbolModelByDetailSymbolId in detailSymbolModelsByDetailSymbolId ) {
          var firstDetailSymbolModelByDetailSymbolId = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault() ;
          var routeNames = detailSymbolModelByDetailSymbolId.DetailSymbolModels.Select( d => d.RouteName ).Distinct().ToList() ;
          var parentRouteName = firstDetailSymbolModelByDetailSymbolId!.CountCableSamePosition == 1 ? firstDetailSymbolModelByDetailSymbolId.RouteName : GetParentRouteName( doc, routeNames ) ;
          if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
            var parentDetailSymbolModel = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == parentRouteName ) ;
            AddDetailSymbolModel( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModels, pickedObjects, parentDetailSymbolModel!, true ) ;
            routeNames = routeNames.Where( n => n != parentRouteName ).OrderByDescending( n => n ).ToList() ;
          }

          foreach ( var childDetailSymbolModel in from routeName in routeNames select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == routeName ) ) {
            AddDetailSymbolModel( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModels, pickedObjects, childDetailSymbolModel, false ) ;
          }
        }

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
      var dialog = new DetailTableDialog( viewModel, conduitsModelData ) ;
      dialog.ShowDialog() ;

      if ( dialog.DialogResult ?? false ) {
        if ( dialog.RoutesChangedConstructionItem.Any() ) {
          var connectorGroups = UpdateConnectorAndConduitConstructionItem( doc, dialog.RoutesChangedConstructionItem ) ;
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

        return doc.Transaction( "TransactionName.Commands.Routing.CreateDetailTable".GetAppStringByKeyOrDefault( "Set detail table" ), _ =>
        {
          if ( viewModel.IsCreateSchedule ) {
            var (originX, originY, originZ) = uiDoc.Selection.PickPoint() ;
            var level = uiDoc.ActiveView.GenLevel ;
            var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

            ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
            var noteWidth = 0.4 ;
            TextNoteOptions opts = new(defaultTextTypeId) ;
            var txtPosition = new XYZ( originX, originY, heightOfConnector ) ;
            TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, GenerateTextTable( viewModel, level.Name ), opts ) ;
            viewModel.IsCreateSchedule = false ;
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

    private static void SetPlumbingData( List<ConduitsModel> conduitsModelData, ref ObservableCollection<DetailTableModel> detailTableModels, string plumbingType )
    {
      Dictionary<string?, List<DetailTableModel>> detailTableModelsByDetailSymbol = new Dictionary<string?, List<DetailTableModel>>() ;
      foreach ( var detailTableModel in detailTableModels ) {
        if ( ! detailTableModelsByDetailSymbol.ContainsKey( detailTableModel.DetailSymbolId ) ) {
          detailTableModelsByDetailSymbol.Add( detailTableModel.DetailSymbolId, new List<DetailTableModel>() { detailTableModel } ) ;
        }
        else {
          detailTableModelsByDetailSymbol.TryGetValue( detailTableModel.DetailSymbolId, out List<DetailTableModel> value ) ;
          value.Add( detailTableModel ) ;
        }
      }

      foreach ( var detailSymbolId in detailTableModelsByDetailSymbol.Keys ) {
        List<DetailTableModel> detailTableModelsByDetailSymbolId = detailTableModelsByDetailSymbol[ detailSymbolId ]! ;
        SetPlumbingDataForOneSymbol( conduitsModelData, ref detailTableModelsByDetailSymbolId, plumbingType ) ;
      }
    }

    protected internal static void SetPlumbingDataForOneSymbol( List<ConduitsModel> conduitsModelData, ref List<DetailTableModel> detailTableModelsByDetailSymbolId, string plumbingType )
    {
      const double percentage = 0.32 ;
      const string defaultChildPlumbingSymbol = "↑" ;

      var parentDetailTableModel = detailTableModelsByDetailSymbolId.First() ;
      var plumbingCount = 0 ;
      var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
      var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
      double currentPlumbingCrossSectionalArea = 0.0 ;

      foreach ( var currentDetailTableModel in detailTableModelsByDetailSymbolId ) {
        currentPlumbingCrossSectionalArea += currentDetailTableModel.WireCrossSectionalArea / percentage ;

        if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
          var plumbing = conduitsModels.Last() ;
          parentDetailTableModel.PlumbingType = parentDetailTableModel == detailTableModelsByDetailSymbolId.First() ? plumbingType : plumbingType + defaultChildPlumbingSymbol ;
          parentDetailTableModel.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          plumbingCount++ ;
          parentDetailTableModel = currentDetailTableModel ;
          currentPlumbingCrossSectionalArea = currentDetailTableModel.WireCrossSectionalArea ;
          if ( currentDetailTableModel != detailTableModelsByDetailSymbolId.Last() ) continue ;
          plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea - currentDetailTableModel.WireCrossSectionalArea ) ;
          currentDetailTableModel.PlumbingType = currentDetailTableModel == detailTableModelsByDetailSymbolId.First() ? plumbingType : plumbingType + defaultChildPlumbingSymbol ;
          currentDetailTableModel.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
          plumbingCount++ ;
        }
        else {
          if ( currentDetailTableModel == detailTableModelsByDetailSymbolId.Last() ) {
            var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
            parentDetailTableModel.PlumbingType = parentDetailTableModel == detailTableModelsByDetailSymbolId.First() ? plumbingType : plumbingType + defaultChildPlumbingSymbol ;
            parentDetailTableModel.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
            plumbingCount++ ;
          }

          if ( currentDetailTableModel == detailTableModelsByDetailSymbolId.First() ) continue ;
          currentDetailTableModel.PlumbingType = defaultChildPlumbingSymbol ;
          currentDetailTableModel.PlumbingSize = defaultChildPlumbingSymbol ;
          currentDetailTableModel.NumberOfPlumbing = defaultChildPlumbingSymbol ;
        }
      }

      foreach ( var detailTableModel in detailTableModelsByDetailSymbolId.Where( d => d.PlumbingSize != defaultChildPlumbingSymbol ).ToList() ) {
        detailTableModel.NumberOfPlumbing = plumbingCount.ToString() ;
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

          element.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, constructionItem ) ;
        }
      }

      transaction.Commit() ;

      return connectorGroups ;
    }

    private List<Element> GetToConnectorAndConduitOfRoute( Document document, IReadOnlyCollection<Element> allConnectors, string routeName )
    {
      var conduitsAndConnectorOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsAndConnectorOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
        var toElementId = toEndPointKey!.GetElementId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
        if ( toConnector == null || toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) continue ;
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

    private void AddDetailSymbolModel( Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData, ICollection<DetailTableModel> detailTableModels, List<Element> pickedObjects, DetailSymbolModel detailSymbolModel, bool isParentRoute )
    {
      var ceeDCode = string.Empty ;
      var constructionClassification = string.Empty ;
      var classification = string.Empty ;
      var wireType = string.Empty ;
      var wireSize = string.Empty ;
      var wireStrip = string.Empty ;
      double wireCrossSectionalArea = 0 ;
      var element = pickedObjects.FirstOrDefault( p => p.Id.IntegerValue.ToString() == detailSymbolModel.ConduitId ) ;
      string floor = doc.GetElementById<Level>( element!.GetLevelId() )?.Name ?? string.Empty ;
      string constructionItem = element!.LookupParameter( "Construction Item" ).AsString() ;
      string isEcoMode = element.LookupParameter( "IsEcoMode" ).AsString() ;

      var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeeDSetCode == detailSymbolModel.Code ) ;
      if ( ceedModel != null && ! string.IsNullOrEmpty( ceedModel.CeeDSetCode ) && ! string.IsNullOrEmpty( ceedModel.CeeDModelNumber ) ) {
        ceeDCode = ceedModel.CeeDSetCode ;
        var hiroiCdModel = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetCdMasterEcoModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeeDSetCode ) : hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeeDSetCode ) ;
        var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeeDModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeeDModelNumber ) ).Skip( 1 ) ;
        constructionClassification = hiroiCdModel?.ConstructionClassification ;
        foreach ( var item in hiroiSetModels ) {
          List<string> listMaterialCode = new List<string>() ;
          if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
            listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
          }

          if ( ! listMaterialCode.Any() ) continue ;
          var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
          foreach ( var master in masterModels ) {
            var conduitModels = conduitsModelData.Where( x => x.PipingType == master.Type && x.Size == master.Size1 ).ToList() ;
            classification = conduitModels.FirstOrDefault()?.Classification ?? string.Empty ;
            wireType = master.Type ;
            wireSize = master.Size1 ;
            wireStrip = master.Size2 ;
            var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && wireStrip == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && wireStrip == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel != null ) wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
          }
        }
      }

      var detailTableModel = new DetailTableModel( false, floor, ceeDCode, detailSymbolModel.DetailSymbol, detailSymbolModel.DetailSymbolId, wireType, wireSize, wireStrip, "1", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, constructionClassification, classification, constructionItem, constructionItem, "", wireCrossSectionalArea, detailSymbolModel.CountCableSamePosition, detailSymbolModel.RouteName, isEcoMode, isParentRoute, ! isParentRoute ) ;
      detailTableModels.Add( detailTableModel ) ;
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