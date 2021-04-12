using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Media.Imaging ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public class FromToItem
  {
    public string? Name { get ; set ; }
    public string? ItemTag { get ; set ; }
    public ElementId? ElementId { get ; set ; }
    public List<FromToItem>? Children { get ; set ; }
    public BitmapImage? Bmi { get ; set ; }

    public bool Selected
    {
      get { return this.Selected ; }
      set
      {
        this.Selected = value ;
        if ( value == true ) {
          OnSelected() ;
        }
      }
    }

    public bool DoubleClicked
    {
      get { return this.DoubleClicked ; }
      set
      {
        this.DoubleClicked = value ;
        if ( value == true ) {
          OnDoubleClicked() ;
        }
      }
    }

    public bool DisplaySelectedFromTo { get ; set ; }

    public static IReadOnlyCollection<Route>? AllRoutes { get ; set ; }
    public static Document? Doc { get ; set ; }
    public static UIDocument? UiDoc { get ; set ; }

    public FromToItem()
    {
      Name = null ;
      ItemTag = null ;
      ElementId = null ;
      Children = new List<FromToItem>() ;
      DisplaySelectedFromTo = false ;
    }

    public virtual void OnSelected()
    {
    }

    public virtual void OnDoubleClicked()
    {
    }


    public class RouteItem : FromToItem
    {
      public Route? SelectedRoute ;

      private List<ElementId>? TargetElements ;

      public RouteItem()
      {
        Bmi = new BitmapImage( new Uri( "../../resources/MEP.ico", UriKind.Relative ) ) ;
      }

      public override void OnSelected()
      {
        TargetElements = new List<ElementId>() ;

        SelectedRoute = AllRoutes?.ToList().Find( r => r.OwnerElement?.Id == ElementId ) ;

        if ( SelectedRoute != null ) {
          TargetElements = Doc?.GetAllElementsOfRouteName<Element>( SelectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
          //Select targetElements
          UiDoc?.Selection.SetElementIds( TargetElements ) ;
        }
      }

      public override void OnDoubleClicked()
      {
        if ( SelectedRoute != null ) {
          //Select targetElements
          UiDoc?.ShowElements( TargetElements ) ;
        }
      }
    }

    public class ParentItem : RouteItem
    {
    }

    public class ConnectorItem : FromToItem
    {
      public Route? SelectedRoute ;

      private List<ElementId>? TargetElements ;

      public ConnectorItem()
      {
        Bmi = new BitmapImage( new Uri( "../../resources/InsertBranchPoint.png", UriKind.Relative ) ) ;
      }

      public override void OnSelected()
      {
        if ( ElementId != null ) {
          TargetElements = new List<ElementId>() { ElementId } ;
          UiDoc?.Selection.SetElementIds( TargetElements ) ;
        }
      }

      public override void OnDoubleClicked()
      {
        UiDoc?.ShowElements( TargetElements ) ;
      }
    }

    public class BranchItem : RouteItem
    {
    }

    public class SubRouteItem : RouteItem
    {
    }
  }
}