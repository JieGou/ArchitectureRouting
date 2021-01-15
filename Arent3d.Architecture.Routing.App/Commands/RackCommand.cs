using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Setup Racks" )]
  [Image( "resources/MEP.ico" )]
  public class RackCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var racks = DocumentMapper.Instance.Get( document ).RackCollection ;

      // TODO
      racks.Clear() ;
      {
        var connector = document.FindConnector( 17299721, 3 )! ;
        var z = connector.Origin.Z - connector.Radius ;

        foreach ( var familyInstance in GetRackInstances( document ).NonNull() ) {
          var (min, max) = familyInstance.get_BoundingBox( commandData.View ).To3d() ;
          min.z = max.z = z ;

          racks.AddRack( new Rack.Rack { Box = new Box3d( min, max ), IsMainRack = true, BeamInterval = 5 } ) ;
        }
      }

      racks.CreateLinkages() ;
      
      return Result.Succeeded ;
    }

    private static IEnumerable<FamilyInstance?> GetRackInstances( Document document )
    {
      yield return document.GetElementById<FamilyInstance>( 18204914 ) ;
      yield return document.GetElementById<FamilyInstance>( 18205151 ) ;
    }
  }
}