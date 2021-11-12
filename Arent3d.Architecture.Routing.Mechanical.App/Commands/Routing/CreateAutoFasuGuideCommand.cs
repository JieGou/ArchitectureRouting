using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic;
using FasuApi = Arent3d.Architecture.Routing.AppBase.FasuAutoCreateApi;
using ImageType = Arent3d.Revit.UI.ImageType ;
using Arent3d.Revit;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "Mechanical.App.Commands.Routing.CreateAutoFasuGuideCommand", DefaultString = "Create Auto\nFASU" )]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateAutoFasuGuideCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            //Get all the spaces in the document
            IList<Element> spaces = FasuApi.GetAllSpaces(document);

            // Get all the group in the document
            IList<Element> groups = FasuApi.GetAllGroups(document);

            // Find group fasu_vav
            foreach (var group in groups)
            {
                if (group.Name == "fasu_vav")
                {
                    // Get the group's oz axis coordinates 
                    LocationPoint locationPoint = (group.Location as LocationPoint)!;

                    // Start Transaction
                    using (Transaction tr = new Transaction(document))
                    {
                        tr.Start( "Add fasu_vav group to document" );
                        foreach (var space in spaces)
                        {
                            // Add object to the document
                            BoundingBoxXYZ boxSpace = space.get_BoundingBox(document.ActiveView);
                            if (boxSpace != null)
                            {
                                XYZ location =
                                    new XYZ((boxSpace.Max.X + boxSpace.Min.X) / 2, (boxSpace.Max.Y + boxSpace.Min.Y) / 2, locationPoint.Point.Z) -
                                    new XYZ(locationPoint.Point.X, locationPoint.Point.Y, 0);
                                ElementTransformUtils.CopyElement(document, group.GetValidId(), location);
                            }
                        } 
                        tr.Commit();
                    }
                    break;
                }
            }

            return Result.Succeeded;
        }
    }
}