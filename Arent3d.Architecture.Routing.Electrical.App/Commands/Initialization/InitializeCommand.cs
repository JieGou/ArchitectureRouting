using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : InitializeCommandBase
  {
    protected override bool Setup( Document document )
    {
      document.MakeCertainAllElectricalRoutingFamilies() ;
      
      RoutingElementExtensions.AddArentConduitType( document ) ;

      // TODO:　Initializeのエラーになるが、必要なさそう（要確認）なので消せる
      //Add connector type value
      // var connectorOneSide = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Connectors ) ;
      // foreach ( var connector in connectorOneSide ) {
      //   connector.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
      // }
      
      return base.Setup( document ) ;
    }
  }
}