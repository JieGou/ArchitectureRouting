using System ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class PullBoxPicker
  {
    private static AddInType? AddInType { get ; set ; }

    public class PullBoxPickerInfo
    {
      public Element Element { get ; }
      public XYZ Position { get ; }

      public PullBoxPickerInfo( Element element, XYZ pos)
      {
        Element = element ;
        Position = pos ;
      }
    }

    public static PullBoxPickerInfo PickPullBox( UIDocument uiDocument, string message, AddInType addInType, Predicate<Element>? elementFilter = null )
    {
      var document = uiDocument.Document ;
      AddInType = addInType ;
      
      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.PointOnElement, new PullBoxFilter(), message ) ;

        var elm = document.GetElement( pickedObject.ElementId ) ;

        return new PullBoxPickerInfo( elm, pickedObject.GlobalPoint ) ;
      }
    }
    
    private class PullBoxFilter : ISelectionFilter
    {
      public bool AllowElement( Element elem )
      {
        return elem.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ;
      }

      public bool AllowReference( Reference reference, XYZ position )
      {
        return true ;
      }
    }
  }
}