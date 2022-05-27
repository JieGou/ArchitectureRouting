using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public static class CreateDetailTableUtil
  {
    private const string DefaultParentPlumbingType = "E" ;
    private const string DefaultConstructionItems = "未設定" ;
    
    private static bool CheckMixConstructionItems( List<DetailTableModel> detailTableModelsData, List<string> detailSymbolIds )
    {
      var detailTableModelRowGroupMixConstructionItems = detailTableModelsData.FirstOrDefault( d => detailSymbolIds.Contains( d.DetailSymbolId ) && ! string.IsNullOrEmpty( d.GroupId ) && bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      var detailTableModelRowGroupNoMixConstructionItems = detailTableModelsData.FirstOrDefault( d => detailSymbolIds.Contains( d.DetailSymbolId ) && ! string.IsNullOrEmpty( d.GroupId ) && ! bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      return detailTableModelRowGroupNoMixConstructionItems == null && detailTableModelRowGroupMixConstructionItems != null ;
    }

    private static string GetParentRouteName( Document document, List<string> routeNames )
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
    
    private static void AddDetailTableModelRow( Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<DetailTableModel> detailTableModelsData, ICollection<DetailTableModel> detailTableModels, List<Element> pickedObjects, DetailSymbolModel detailSymbolModel, bool isParentRoute, bool mixConstructionItems )
    {
      var element = pickedObjects.FirstOrDefault( p => p.UniqueId == detailSymbolModel.ConduitId ) ;
      string floor = doc.GetElementById<Level>( element!.GetLevelId() )?.Name ?? string.Empty ;
      string constructionItem = element!.LookupParameter( "Construction Item" ).AsString() ?? DefaultConstructionItems ;
      string isEcoMode = element.LookupParameter( "IsEcoMode" ).AsString() ;
      string plumbingType = detailSymbolModel.PlumbingType ;

      var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == detailSymbolModel.Code && x.GeneralDisplayDeviceSymbol == detailSymbolModel.DeviceSymbol ) ;
      if ( ceedModel != null && ! string.IsNullOrEmpty( ceedModel.CeedSetCode ) && ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ) {
        var ceedCode = ceedModel.CeedSetCode ;
        var remark = ceedModel.GeneralDisplayDeviceSymbol ;
        var hiroiCdModel = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetCdMasterEcoModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) : hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) ;
        var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) ;
        var constructionClassification = hiroiCdModel?.ConstructionClassification ;
        foreach ( var item in hiroiSetModels ) {
          List<string> listMaterialCode = new() ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode1 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode2 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode2 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode3 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode3 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode4 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode4 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode5 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode5 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode6 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode6 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode7 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode7 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode8 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode8 ).ToString() ) ;

          if ( ! listMaterialCode.Any() ) continue ;
          var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
          foreach ( var master in masterModels ) {
            var wireType = master.Type ;
            var wireSize = master.Size1 ;
            var wireStrip = string.IsNullOrEmpty( master.Size2 ) || master.Size2 == "0" ? "-" : master.Size2 ;
            var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && master.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && master.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel == null ) continue ;
            var signalType = wiresAndCablesModel.Classification ;
            var wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
            
            var wireSizesOfWireType = wiresAndCablesModelData.Where( w => w.WireType == wireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
            var wireSizes = wireSizesOfWireType.Any() ? ( from wireSizeType in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
            
            var wireStripsOfWireType = wiresAndCablesModelData.Where( w => w.WireType == wireType && w.DiameterOrNominal == wireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
            var wireStrips = wireStripsOfWireType.Any() ? ( from wireStripType in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStripType, wireStripType ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;

            var plumbingItemTypes = new List<DetailTableModel.ComboboxItemType> { new( constructionItem, constructionItem ) } ;
            
            var detailTableRow = new DetailTableModel( false, floor, ceedCode, detailSymbolModel.DetailSymbol, detailSymbolModel.DetailSymbolId, wireType, wireSize, wireStrip, "1", 
              string.Empty, string.Empty, string.Empty, plumbingType, string.Empty, string.Empty, constructionClassification, signalType, 
              constructionItem, constructionItem, remark, wireCrossSectionalArea, detailSymbolModel.CountCableSamePosition, detailSymbolModel.RouteName, isEcoMode, isParentRoute, ! isParentRoute, 
              string.Empty, string.Empty, true, mixConstructionItems, string.Empty, false, false, false, wireSizes, wireStrips, new List<DetailTableModel.ComboboxItemType>(), new List<DetailTableModel.ComboboxItemType>(), plumbingItemTypes ) ;
            detailTableModels.Add( detailTableRow ) ;
          }
        }
      }
    }
    
    public static string GetDetailTableRowPlumbingIdentityInfo( DetailTableModel detailTableRow, bool mixConstructionItems )
    {
      return mixConstructionItems ? 
        string.Join( "-", detailTableRow.PlumbingType + detailTableRow.PlumbingSize, detailTableRow.SignalType, detailTableRow.RouteName, detailTableRow.DetailSymbolId, detailTableRow.CopyIndex ) : 
        string.Join( "-", detailTableRow.PlumbingType + detailTableRow.PlumbingSize, detailTableRow.SignalType, detailTableRow.RouteName, detailTableRow.DetailSymbolId, detailTableRow.CopyIndex, detailTableRow.ConstructionItems ) ;
    }
    
    private static void SetPlumbingDataForEachWiring( List<DetailTableModel> detailTableModelData, List<ConduitsModel> conduitsModelData, ref ObservableCollection<DetailTableModel> detailTableModels, string plumbingType )
    {
      const double percentage = 0.32 ;
      var newDetailTableRows = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in detailTableModels ) {
        const int plumbingCount = 1 ;
        var oldDetailTableRows = detailTableModelData.Where( d => d.DetailSymbolId == detailTableRow.DetailSymbolId && d.RouteName == detailTableRow.RouteName ).ToList() ;
        if ( ! oldDetailTableRows.Any() ) {
          var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
          var currentPlumbingCrossSectionalArea = detailTableRow.WireCrossSectionalArea / percentage ;
          if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
            var plumbing = conduitsModels.Last() ;
            detailTableRow.PlumbingType = plumbingType ;
            detailTableRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
          }
          else {
            var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
            detailTableRow.PlumbingType = plumbingType ;
            detailTableRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
          }

          detailTableRow.NumberOfPlumbing = plumbingCount.ToString() ;
          detailTableRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( detailTableRow, false ) ;
          detailTableRow.IsParentRoute = true ;
          detailTableRow.IsReadOnly = false ;
          detailTableRow.IsReadOnlyPlumbingItems = true ;
          var plumbingSizesOfPlumbingType = conduitsModels.Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableRow.PlumbingSizes = ( from plumbingSizeType in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
        }
        else {
          var oldDetailTableRow = oldDetailTableRows.First() ;
          detailTableRow.PlumbingType = oldDetailTableRow.PlumbingType ;
          detailTableRow.PlumbingSize = oldDetailTableRow.PlumbingSize ;
          detailTableRow.NumberOfPlumbing = oldDetailTableRow.NumberOfPlumbing ;
          detailTableRow.PlumbingIdentityInfo = oldDetailTableRow.PlumbingIdentityInfo ;
          detailTableRow.IsParentRoute = oldDetailTableRow.IsParentRoute ;
          detailTableRow.IsReadOnly = oldDetailTableRow.IsReadOnly ;
          detailTableRow.IsReadOnlyPlumbingItems = oldDetailTableRow.IsReadOnlyPlumbingItems ;
          detailTableRow.IsReadOnlyPlumbingSize = oldDetailTableRow.IsReadOnlyPlumbingSize ;
          var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == detailTableRow.PlumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableRow.PlumbingSizes = ( from plumbingSizeType in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
          if ( oldDetailTableRows.Count <= 1 ) continue ;
          oldDetailTableRows.Remove( oldDetailTableRow ) ;
          newDetailTableRows.AddRange( oldDetailTableRows ) ;
        }
      }

      foreach ( var newDetailTableRow in newDetailTableRows ) {
        detailTableModels.Add( newDetailTableRow ) ;
      }
    }
    
    private static void SortDetailTableModel( ref ObservableCollection<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      List<DetailTableModel> sortedDetailTableModelsList = detailTableModels.ToList() ;
      DetailTableViewModel.SortDetailTableModel( ref sortedDetailTableModelsList, isMixConstructionItems ) ;
      detailTableModels = new ObservableCollection<DetailTableModel>( sortedDetailTableModelsList ) ;
    }
    
    public static ( ObservableCollection<DetailTableModel>, bool, bool ) CreateDetailTable( Document doc, CsvStorable csvStorable, DetailSymbolStorable detailSymbolStorable, List<Element> conduits, List<string> elementIds, bool isReferenceDetailTableModels )
    {
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
      var hiroiSetCdMasterEcoModelData = csvStorable.HiroiSetCdMasterEcoModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      var detailTableModelsData = doc.GetDetailTableStorable().DetailTableModelData ;
      var detailTableModels = new ObservableCollection<DetailTableModel>() ;

      var allDetailSymbolIdsOnDetailTableModels = detailTableModelsData.Select( d => d.DetailSymbolId ).Distinct().ToHashSet() ;
      var detailSymbolIdsOnDetailTableModels = detailSymbolStorable.DetailSymbolModelData.Where( x => elementIds.Contains( isReferenceDetailTableModels ? x.DetailSymbolId : x.ConduitId ) && allDetailSymbolIdsOnDetailTableModels.Contains( x.DetailSymbolId ) ).Select( d => d.DetailSymbolId )
        .Distinct().ToList() ;
      var isMixConstructionItems = detailSymbolIdsOnDetailTableModels.Any() && CheckMixConstructionItems( detailTableModelsData, detailSymbolIdsOnDetailTableModels ) ;
      var detailSymbolModelsByDetailSymbolId = detailSymbolStorable.DetailSymbolModelData.Where( x => elementIds.Contains( isReferenceDetailTableModels ? x.DetailSymbolId : x.ConduitId ) && ! allDetailSymbolIdsOnDetailTableModels.Contains( x.DetailSymbolId ) ).OrderBy( x => x.DetailSymbol )
        .ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.IsParentSymbol ).GroupBy( x => x.DetailSymbolId, ( key, p ) => new { DetailSymbolId = key, DetailSymbolModels = p.ToList() } ) ;

      foreach ( var detailSymbolModelByDetailSymbolId in detailSymbolModelsByDetailSymbolId ) {
        var firstDetailSymbolModelByDetailSymbolId = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault() ;
        var routeNames = detailSymbolModelByDetailSymbolId.DetailSymbolModels.Select( d => d.RouteName ).Distinct().ToList() ;
        // var parentRouteName = firstDetailSymbolModelByDetailSymbolId!.CountCableSamePosition == 1 ? firstDetailSymbolModelByDetailSymbolId.RouteName : GetParentRouteName( doc, routeNames ) ;
        // if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
        //   var parentDetailSymbolModel = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == parentRouteName ) ;
        //   if ( isReferenceDetailTableModels ) {
        //     var conduitOfFirstRoute = doc.GetElement( parentDetailSymbolModel!.ConduitId ) ;
        //     conduits = new List<Element>() { conduitOfFirstRoute } ;
        //   }
        //
        //   AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, csvStorable.WiresAndCablesModelData, detailTableModelsData, detailTableModels, conduits,
        //     parentDetailSymbolModel!, true, isMixConstructionItems ) ;
        //   routeNames = routeNames.Where( n => n != parentRouteName ).OrderByDescending( n => n ).ToList() ;
        // }

        foreach ( var childDetailSymbolModel in from routeName in routeNames select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == routeName ) ) {
          if ( isReferenceDetailTableModels ) {
            var conduitOfFirstRoute = doc.GetElement( childDetailSymbolModel.ConduitId ) ;
            conduits = new List<Element>() { conduitOfFirstRoute } ;
          }

          AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, csvStorable.WiresAndCablesModelData, detailTableModelsData, detailTableModels, conduits,
            childDetailSymbolModel, false, isMixConstructionItems ) ;
        }
      }

      if ( detailTableModels.Any() ) {
        SetPlumbingDataForEachWiring( detailTableModelsData, csvStorable.ConduitsModelData, ref detailTableModels, DefaultParentPlumbingType ) ;
      }

      if ( detailSymbolIdsOnDetailTableModels.Any() ) {
        var index = "-" + DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
        var detailTableModelRowsOnDetailTableStorable = detailTableModelsData.Where( d => detailSymbolIdsOnDetailTableModels.Contains( d.DetailSymbolId ) ).ToList() ;
        foreach ( var detailTableRow in detailTableModelRowsOnDetailTableStorable ) {
          if ( isReferenceDetailTableModels ) {
            if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) detailTableRow.GroupId += index ;
            detailTableRow.PlumbingIdentityInfo += index ;
          }

          detailTableModels.Add( detailTableRow ) ;
        }
      }

      SortDetailTableModel( ref detailTableModels, isMixConstructionItems ) ;
      return ( detailTableModels, isMixConstructionItems, detailSymbolIdsOnDetailTableModels.Any() ) ;
    }
  }
}