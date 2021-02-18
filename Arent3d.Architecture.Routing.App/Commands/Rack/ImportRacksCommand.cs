using System ;
using System.ComponentModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Rack
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Import Racks" )]
  [Image( "resources/MEP.ico" )]
  public class ImportRacksCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      throw new NotImplementedException() ;
    }
  }
}