﻿using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DetailTableViewModel : ViewModelBase
  {
    public ObservableCollection<DetailTableModel> DetailTableModels { get ; set ; }

    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }
    
    public  bool IsCancelCreateDetailTable { get; set; }

    public ICommand SaveDetailTableCommand { get; set; }

    public ICommand SaveAndCreateDetailTableCommand { get ; set ; }

    public List<CreateDetailTableCommandBase.ComboboxItemType> ConduitTypes { get ; set ; }

    public List<CreateDetailTableCommandBase.ComboboxItemType> ConstructionItems { get ; set ; }

    public DetailTableViewModel( ObservableCollection<DetailTableModel> detailTableModels, List<CreateDetailTableCommandBase.ComboboxItemType> conduitTypes, List<CreateDetailTableCommandBase.ComboboxItemType> constructionItems )
    {
      DetailTableModels = detailTableModels ;
      IsCreateDetailTableOnFloorPlanView = false ;

      SaveDetailTableCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { SaveDetailTable() ; } // Execute()
      ) ;

      SaveAndCreateDetailTableCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { SaveAndCreateDetailTable() ; } // Execute()
      ) ;

      ConduitTypes = conduitTypes ;
      ConstructionItems = constructionItems ;
    }

    private void SaveDetailTable()
    {
      const string defaultConstructionItems = "未設定" ;
      // Configure open file dialog box
      SaveFileDialog dlg = new() { FileName = string.Empty, DefaultExt = ".ctl", Filter = "CTL files|*.ctl" } ;

      // Show open file dialog box
      var result = dlg.ShowDialog() ;

      // Process open file dialog box results
      if ( result != DialogResult.OK ) return ;
      string createText = string.Empty ;
      foreach ( var item in DetailTableModels ) {
        string line = string.Join( ",", item.Floor, item.Remark, item.ConstructionClassification, item.ConstructionItems, item.DetailSymbol, item.EarthSize, item.EarthType, 
          item.PlumbingSize, item.PlumbingType, item.PlumbingItems, item.WireBook, item.WireSize, item.WireStrip, item.WireType, item.NumberOfGrounds, item.NumberOfPlumbing ) ;
        createText += line.Trim() + Environment.NewLine ;
      }

      if ( ! string.IsNullOrWhiteSpace( createText.Trim() ) && createText.Trim() != defaultConstructionItems ) {
        File.WriteAllText( dlg.FileName, createText.Trim() ) ;
      }
    }

    private void SaveAndCreateDetailTable()
    {
      if ( IsCancelCreateDetailTable ) return ;
      SaveDetailTable() ;
      IsCreateDetailTableOnFloorPlanView = true ;
    }

    public static void UnGroupDetailTableRowsAfterChangeConstructionItems( ref List<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
    {
      var groupIdOfDetailTableRowsWithConstructionItemHasChanged = detailTableModels.Where( d => routeNames.Contains( d.RouteName ) && ! string.IsNullOrEmpty( d.GroupId ) ).Select( d => d.GroupId ).Distinct() ;
      foreach ( var groupId in groupIdOfDetailTableRowsWithConstructionItemHasChanged ) {
        var detailTableRowsWithSameGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems != constructionItems ).ToList() ;
        var detailTableRowsWithConstructionItemHasChanged = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems == constructionItems ).ToList() ;
        if ( detailTableRowsWithSameGroupId.Any() ) {
          if ( detailTableRowsWithSameGroupId.Count == 1 ) {
            detailTableRowsWithSameGroupId.First().GroupId = string.Empty ;
          }
          if ( detailTableRowsWithConstructionItemHasChanged.Count == 1 ) {
            detailTableRowsWithConstructionItemHasChanged.First().GroupId = string.Empty ;
          }
        }

        if ( detailTableRowsWithConstructionItemHasChanged.Count <= 1 ) continue ;
        foreach ( var detailTableRow in detailTableRowsWithConstructionItemHasChanged ) {
          var newGroupId = string.Join( "-",detailTableRow.DetailSymbolId, detailTableRow.PlumbingIdentityInfo, detailTableRow.ConstructionItems, detailTableRow.WireType + detailTableRow.WireSize + detailTableRow.WireStrip ) ;
          detailTableRow.GroupId = newGroupId ;
        }
      }
    }

    public static void UpdatePlumbingItemsAfterChangeConstructionItems( ref List<DetailTableModel> detailTableModels, string routeName, string constructionItems )
    {
      var plumbingIdentityInfos = detailTableModels.Where( d => d.RouteName == routeName && d.IsParentRoute ).Select( d => d.PlumbingIdentityInfo ).Distinct() ;
      foreach ( var plumbingIdentityInfo in plumbingIdentityInfos ) {
        var detailTableRowsWithSamePlumbing = detailTableModels.Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo ) ;
        foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
          detailTableRow.PlumbingItems = constructionItems ;
        }
      }
    }

    public static void SetGroupIdForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      const bool isMixConstructionItems = false ;
      var detailTableRowsGroupByPlumbingIdentityInfo = 
        detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var detailTableRowsGroupByWiringType = 
          detailTableRowsWithSamePlumbingIdentityInfo
            .GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableRowsWithSameWiringType in detailTableRowsGroupByWiringType ) {
          var detailTableRowsGroupByConstructionItem = 
            detailTableRowsWithSameWiringType
              .GroupBy( d => d.ConstructionItems )
              .Select( g => g.ToList() ) ;
          foreach ( var detailTableRowsWithSameConstructionItem in detailTableRowsGroupByConstructionItem ) {
            var oldDetailTableRow = detailTableRowsWithSameConstructionItem.First() ;
            if ( detailTableRowsWithSameConstructionItem.Count == 1 ) {
              oldDetailTableRow.GroupId = string.Empty ;
              oldDetailTableRow.PlumbingItems = oldDetailTableRow.ConstructionItems ;
            }
            else {
              var groupId = string.Join( "-", isMixConstructionItems, oldDetailTableRow.DetailSymbolId, oldDetailTableRow.PlumbingIdentityInfo,
                oldDetailTableRow.WireType + oldDetailTableRow.WireSize + oldDetailTableRow.WireStrip ) ;
              foreach ( var detailTableRow in detailTableRowsWithSameConstructionItem ) {
                detailTableRow.GroupId = groupId ;
                detailTableRow.PlumbingItems = detailTableRow.ConstructionItems ;
              }
            }
          }
        }
      }
    }

    public static void SetGroupIdForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      const bool isMixConstructionItems = true ;
      var detailTableRowsGroupByPlumbingIdentityInfo = 
        detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var parentConstructionItems = detailTableRowsWithSamePlumbingIdentityInfo.First().ConstructionItems ;
        var detailTableRowsGroupByWiringType = 
          detailTableRowsWithSamePlumbingIdentityInfo
            .GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableRowsWithSameWiringType in detailTableRowsGroupByWiringType ) {
          var oldDetailTableRow = detailTableRowsWithSameWiringType.First() ;
          if ( detailTableRowsWithSameWiringType.Count == 1 ) {
            oldDetailTableRow.GroupId = string.Empty ;
            oldDetailTableRow.PlumbingItems = parentConstructionItems ;
          }
          else {
            var groupId = string.Join( "-", isMixConstructionItems, oldDetailTableRow.DetailSymbolId, oldDetailTableRow.PlumbingIdentityInfo,
              oldDetailTableRow.WireType + oldDetailTableRow.WireSize + oldDetailTableRow.WireStrip ) ;
            foreach ( var detailTableRowWithSameWiringType in detailTableRowsWithSameWiringType ) {
              detailTableRowWithSameWiringType.GroupId = groupId ;
              detailTableRowWithSameWiringType.PlumbingItems = parentConstructionItems ;
            }
          }
        }
      }
    }

    public static void SetPlumbingItemsForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      foreach ( var detailTableRow in detailTableRowsWithSameDetailSymbolId ) {
        detailTableRow.PlumbingItems = detailTableRow.ConstructionItems ;
      }
    }

    public static void SetPlumbingItemsForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      var detailTableRowsGroupByPlumbingIdentityInfo = 
        detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRows in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var parentDetailRow = detailTableRows.First().ConstructionItems ;
        foreach ( var detailTableRow in detailTableRows ) {
          detailTableRow.PlumbingItems = parentDetailRow ;
        }
      }
    }
  }
}