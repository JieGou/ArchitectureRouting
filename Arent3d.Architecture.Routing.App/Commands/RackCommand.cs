using System ;
using System.ComponentModel ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

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
        var box = document.GetElementById<FamilyInstance>( 18204914 )! ;
        var boundingBox = box.get_BoundingBox( commandData.View ) ;

        var min = boundingBox.Min.To3d() ;
        var max = boundingBox.Max.To3d() ;

        var rack = new Rack.Rack { Center = ( min + max ) * 0.5, Size = ( max - min ), IsMainRack = true, BeamInterval = 6 } ;
        racks.AddRack( rack ) ;
      }
      
      return Result.Succeeded ;
    }
  }
}