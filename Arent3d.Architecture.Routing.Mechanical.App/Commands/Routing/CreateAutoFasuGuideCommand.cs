using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using FasuApi = Arent3d.Architecture.Routing.AppBase.FasuAutoCreateApi;
using ImageType = Arent3d.Revit.UI.ImageType;
using System;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Mechanical.App.Commands.Routing.CreateAutoFasuGuideCommand", DefaultString = "Create Auto\nFASU")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateAutoFasuGuideCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            // Get all the spaces in the document
            IList<Element> spaces = FasuApi.GetAllSpaces(document);

            // Get fasu height
            double heightFasu = 0;
            bool bHeightFasu = FasuApi.GetHeightFasu(document, "FASU", ref heightFasu);

            // Start Transaction
            using (Transaction tr = new Transaction(document))
            {
                tr.Start("Create Auto Fasu and Vav Guide");
                foreach (var space in spaces)
                {
                    // Add object to the document
                    BoundingBoxXYZ boxSpace = space.get_BoundingBox(document.ActiveView);
                    if (boxSpace != null)
                    {
                        // Fasu object
                        if (!bHeightFasu) heightFasu = (boxSpace.Max.Z + boxSpace.Min.Z) / 2;
                        var locationFasu = new XYZ((boxSpace.Max.X + boxSpace.Min.X) / 2,
                            (boxSpace.Max.Y + boxSpace.Min.Y) / 2, heightFasu);
                        var fasuInstance = document.AddFasu(locationFasu, null);
                        ElementTransformUtils.RotateElement(document, fasuInstance.Id,
                            Line.CreateBound(locationFasu, locationFasu + XYZ.BasisZ), Math.PI / 2);

                        // Vav object 
                        BoundingBoxXYZ boxFasu = fasuInstance.get_BoundingBox(document.ActiveView);
                        if (boxFasu != null)
                        {
                            if (!bHeightFasu) heightFasu = (boxFasu.Max.Z + boxFasu.Min.Z) / 2;
                            var locVav = new XYZ((boxFasu.Max.X + boxFasu.Min.X) / 2,
                                (boxFasu.Max.Y + boxFasu.Min.Y) / 2, heightFasu);
                            double distance = (boxFasu.Max.X - boxFasu.Min.X) / 2;
                            FamilyInstance vavInstance = document.AddVav(locVav, null);
                            vavInstance.LookupParameter("ダクト径").SetValueString("250");
                            vavInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).SetValueString("3250");
                            BoundingBoxXYZ box = vavInstance.get_BoundingBox(document.ActiveView);
                            distance += (box.Max.X - box.Min.X) / 8;
                            ElementTransformUtils.MoveElement(document, vavInstance.Id, new XYZ(distance, 0, 0));
                        }
                    }
                }

                tr.Commit();
            }

            return Result.Succeeded;
        }
    }
}