using System ;
using System.ComponentModel ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.ImageType ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Initialize" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  public class InitializeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      doc.MakeCertainAllFamilies() ;

      return Result.Succeeded ;
    }
  }
}