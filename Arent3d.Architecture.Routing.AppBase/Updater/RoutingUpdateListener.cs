using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  public class RoutingUpdateListener : IDocumentUpdateListener
  {
    public string Name { get ; }
    public string Description { get ; }
    public ChangePriority ChangePriority { get ; }
    public DocumentUpdateListenType ListenType { get ; }

    public ElementFilter GetElementFilter()
    {
      //Please change this method to filter the target families
      ElementFilter filter = new ElementCategoryFilter( BuiltInCategory.OST_MechanicalEquipment ) ;
      return filter ;
    }

    public IEnumerable<ParameterProxy> GetListeningParameters()
    {
      throw new System.NotImplementedException() ;
    }

    public RoutingUpdateListener()
    {
      Name = this.ToString() ;
      Description = "Update location " ;
      ListenType = DocumentUpdateListenType.Deletion | DocumentUpdateListenType.Geometry;
    }


    public void Execute( UpdaterData data )
    {
        //Please implement the movement process here.
        ICollection<ElementId>? elementIds = SelectedFromToViewModel.UiDoc?.Selection.GetElementIds();
        if ( elementIds?.Count == 1 ) {
            FilteredElementCollector filteredElementCollector = new FilteredElementCollector( SelectedFromToViewModel.UiDoc?.Document );
            List<Element> elementList = new List<Element>();
            elementList = filteredElementCollector.OfClass( typeof( FamilyInstance ) ).OfCategory( BuiltInCategory.OST_MechanicalEquipment ).ToList();
            foreach ( Element element in elementList ) {

                foreach ( Parameter parameter in element.Parameters ) {

                    if ( parameter.Definition.Name == "LinkedInstanceId" ) {
                        if ( parameter.AsString() == elementIds.ElementAt( 0 ).ToString() ) {
                            Element? moveElement = SelectedFromToViewModel.UiDoc?.Document.GetElement( elementIds.ElementAt( 0 ) );
                            LocationPoint? LpF = element.Location as LocationPoint;
                            XYZ? epFrom = LpF?.Point;

                            LocationPoint? LpT = moveElement?.Location as LocationPoint;
                            XYZ? epTo = LpT?.Point;
                            element.Location.Move( epTo - epFrom );
                        }
                    }
                }
            }
        }
    }
  }
}