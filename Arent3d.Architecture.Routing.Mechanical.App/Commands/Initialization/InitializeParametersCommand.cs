using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Initialization.InitializeParametersCommand", DefaultString = "Initialize Parameters" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeParametersCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {      
      var uiDocument = commandData.Application.ActiveUIDocument;
      var document = uiDocument.Document;
      using Transaction tr = new Transaction( document ) ;
      tr.Start( "Add Space Parameters" );
      document.LoadAllParametersFromFile(BuiltInCategorySets.SpaceElements, AssetManager.GetSpaceSharedParameterPath() ) ;
      tr.Commit();
      return Result.Succeeded;
    }
  }
}