﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;
using RibbonButton = Autodesk.Windows.RibbonButton ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public class FromToTreeManager : IUIApplicationHolder
  {
    public UIApplication? UiApp { get ; set ; } = null ;
    public FromToTreeUiManager? FromToTreeUiManager { get ; set ; } = null ;

    public FromToTreeManager()
    {
    }

    // view activated event
    public virtual void OnViewActivated( ViewActivatedEventArgs e, AddInType addInType )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeUiManager?.FromToTreeView == null || UiApp == null ) return ;
      var doc = UiApp.ActiveUIDocument.Document ;

      //Initialize TreeView
      FromToTreeUiManager?.FromToTreeView.ClearSelection() ;
      FromToTreeUiManager?.FromToTreeView.CustomInitiator( UiApp, addInType ) ;
      SetSelectionInViewToFromToTree( doc, addInType ) ;

      UpdateIsEnableButton( doc ) ;
    }

    /// <summary>
    /// Set Selection to FromToTree if any route or connector are selected in View
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="selectedRoutes"></param>
    /// <param name="selectedConnectors"></param>
    private void SetSelectionInViewToFromToTree( Document doc, AddInType addInType )
    {
      if ( UiApp == null ) return ;

      //Get ElementIds in activeview
      FilteredElementCollector collector = new FilteredElementCollector( doc, doc.ActiveView.Id ) ;
      var elementsInActiveView = collector.ToElementIds() ;

      // Get Selected Routes
      var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( UiApp.ActiveUIDocument ) ;
      var connectorsInView = doc.CollectRoutes( addInType ).SelectMany( r => r.GetAllConnectors() ).Where( c => elementsInActiveView.Contains( c.Owner.Id ) ) ;


      if ( selectedRoutes.FirstOrDefault() is { } selectedRoute ) {
        var selectedRouteName = selectedRoute.RouteName ;
        var targetElements = doc?.GetAllElementsOfRouteName<Element>( selectedRouteName ).Select( elem => elem.Id ).ToList() ;

        if ( targetElements == null ) return ;
        if ( elementsInActiveView.Any( ids => targetElements.Contains( ids ) ) ) {
          //Select TreeViewItem
          FromToTreeViewModel.GetSelectedElementId( selectedRoute.OwnerElement?.UniqueId ) ;
        }
      }
      else if ( connectorsInView.Any( c => UiApp.ActiveUIDocument.Selection.GetElementIds().Contains( c.Owner.Id ) ) ) {
        var selectedElementId = UiApp.ActiveUIDocument.Selection.GetElementIds().First() ;
        FromToTreeViewModel.GetSelectedElementId( doc.GetElementById<Element>( selectedElementId )?.UniqueId ) ;
      }
      else {
        FromToTreeViewModel.ClearSelection() ;
      }
    }

    private void UpdateIsEnableButton( Document document )
    {
      const string dummyName = "Dummy" ;
      var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => ! string.IsNullOrEmpty( c.GetRouteName() ) && c.GetRouteName()!.Contains( dummyName ) ).Select( c => c.Id ).Distinct().ToList() ;
      var isEnable = ! allConduits.Any() ;
      var targetTabName = "Electrical.App.Routing.TabName".GetAppStringByKey() ;
      var selectionTab = UIHelper.GetRibbonTabFromName( targetTabName ) ;
      if ( selectionTab == null ) return ;

      using var transaction = new Transaction( document, "Enable buttons" ) ;
      transaction.Start() ;
      foreach ( var panel in selectionTab.Panels ) {
        if ( panel.Source.Title == "Electrical.App.Panels.Routing.Confirmation".GetAppStringByKeyOrDefault( "Confirmation" ) ) {
          foreach ( var item in panel.Source.Items ) {
            if ( ! ( item is RibbonButton ribbonButton && ribbonButton.Text == "Electrical.App.Commands.Routing.CreateDummyConduitsIn3DViewCommand".GetDocumentStringByKeyOrDefault( document, "Show\nConduits in 3D" ) ) ) {
              item.IsEnabled = isEnable ;
            }
          }
        }
        else {
          panel.IsEnabled = isEnable ;
        }
      }

      transaction.Commit() ;
    }
    
    // document opened event
    public virtual void OnDocumentOpened( AddInType addInType )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeUiManager is { } fromToTreeUiManager && UiApp != null ) {
        fromToTreeUiManager.FromToTreeView.CustomInitiator( UiApp, addInType ) ;
        FromToTreeViewModel.FromToTreePanel = FromToTreeUiManager?.FromToTreeView ;
        if ( fromToTreeUiManager.Dockable == null ) {
          fromToTreeUiManager.Dockable = UiApp.GetDockablePane( fromToTreeUiManager.DpId ) ;
        }
      }
    }

    // document opened event
    public virtual void OnDocumentChanged( Autodesk.Revit.DB.Events.DocumentChangedEventArgs e, AddInType addInType )
    {
      if ( FromToTreeUiManager?.FromToTreeView is not { } fromToTreeView || UiApp == null ) return ;
      if ( UpdateSuppressed() ) return ;

      var changedElementIds = e.GetAddedElementIds().Concat( e.GetModifiedElementIds() ) ;
      var changedRoute = e.GetDocument().FilterStorableElements<Route>( changedElementIds ) ;

      // provide ExternalCommandData object to dockable page
      if ( changedRoute.Any() || fromToTreeView.HasInvalidRoute() ) {
        fromToTreeView.CustomInitiator( UiApp, addInType ) ;
      }
    }

    // document opened event
    public virtual void UpdateTreeView( AddInType addInType )
    {
      if ( FromToTreeUiManager?.FromToTreeView is not { } fromToTreeView || UiApp == null ) return ;
      if ( UpdateSuppressed() ) return ;

      fromToTreeView.CustomInitiator( UiApp, addInType ) ;
    }

    #region Change suppression

    public static IDisposable SuppressUpdate()
    {
      return new UpdateSuppressor() ;
    }

    public static bool UpdateSuppressed()
    {
      lock ( _suppressCountLocker ) {
        return ( 0 < _suppressCount ) ;
      }
    }

    private static readonly object _suppressCountLocker = new object() ;
    private static int _suppressCount = 0 ;

    private static void IncreaseSuppressCount()
    {
      lock ( _suppressCountLocker ) {
        ++_suppressCount ;
      }
    }
    private static void DecreaseSuppressCount()
    {
      lock ( _suppressCountLocker ) {
        if ( 0 == _suppressCount ) throw new InvalidOperationException() ;
        --_suppressCount ;
      }
    }

    private class UpdateSuppressor : MustBeDisposed
    {
      public UpdateSuppressor()
      {
        IncreaseSuppressCount() ;
      }

      protected override void Finally()
      {
        DecreaseSuppressCount() ;
      }
    }

    #endregion
  }
}