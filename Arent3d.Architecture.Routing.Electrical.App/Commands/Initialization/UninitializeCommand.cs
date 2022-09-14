using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Shaft ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Initialization.UnnitializeCommand", DefaultString = "Erase all addin data" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class UninitializeCommand : UninitializeCommandBase
  {
    protected override void UnSetup( Document document )
    {
      document.EraseAllConnectorFamilies() ;
      document.EraseAllElectricalRoutingFamilies();
      CreateCylindricalShaftCommandBase.DeleteAllShaftOpening(document);
      base.UnSetup( document ) ;
      document.DeleteEntireSchema();
      DeleteArentElectricalSetting( document ) ;
    }

    private static void DeleteArentElectricalSetting( Document document )
    {
      // Delete "Arent電線" table in [Manage -> MEPSetting -> electrical setting -> conduit sizes]
      var conduitTypeName = RoutingElementExtensions.GetConduitTypeName( document ) ;
      var arentConduitStandard = document.GetAllElements<ElementType>().OfCategory( BuiltInCategory.OST_ConduitStandards ).SingleOrDefault( x => x.Name == conduitTypeName ) ;
      if ( arentConduitStandard is { } )
        document.Delete( arentConduitStandard.Id ) ;
    }
  }
}