using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.Show3DViewsCommand", DefaultString = "階ごとの3dビュー作成" )]
  [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class Show3DViewsCommand : Show3DViewsCommandBase
  {
    
  }
}