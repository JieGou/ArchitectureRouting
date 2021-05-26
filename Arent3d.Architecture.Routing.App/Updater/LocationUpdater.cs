using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Updater
{
  public class LocationUpdater : IDocumentUpdateListener
  {
    public string Name { get ; }
    public string Description { get ; }
    public ChangePriority ChangePriority { get ; }
    public DocumentUpdateListenType ListenType { get ; }
    public ElementFilter GetElementFilter()
    { 
      //Please change this method to filter the target families
      ElementFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_MechanicalEquipment ) ;
      return filter ;
    }

    public IEnumerable<ParameterProxy> GetListeningParameters()
    {
      throw new System.NotImplementedException() ;
    }

    public LocationUpdater(  )
    {
      Name = this.ToString() ;
      Description = "Update location " ;
      ListenType = DocumentUpdateListenType.Any ;
    }
    

    public void Execute( UpdaterData data )
    {
      //Please implement the movement process here.
      var elemsIds = data.GetModifiedElementIds() ;

      TaskDialog.Show( "test", elemsIds.FirstOrDefault().ToString() ) ;
    }
  }
}