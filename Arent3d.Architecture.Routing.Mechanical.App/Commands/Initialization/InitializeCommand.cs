using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : InitializeCommandBase
  {
    protected override bool RoutingSettingsAreInitialized( Document document )
    {
      // 設備ルートアシスト用のファミリを追加する必要があるため、追加のチェックを入れる
      return base.RoutingSettingsAreInitialized(document) &&  document.AllFamiliesAreLoaded<MechanicalRoutingFamilyType>() ;
    }
    protected override bool Setup( Document document )
    {
      document.MakeBranchNumberParameter() ;
      document.MakeAHUNumberParameter() ;
      document.MakeCertainAllMechanicalRoutingFamilies() ;
      return base.Setup( document ) ;
    }
  }
}