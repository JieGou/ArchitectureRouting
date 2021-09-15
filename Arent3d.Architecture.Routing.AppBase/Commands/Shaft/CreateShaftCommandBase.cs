using Arent3d.Revit;
using Arent3d.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
    public abstract class CreateShaftCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDocument = commandData.Application.ActiveUIDocument;
            Document document = uiDocument.Document;
            try
            {
                XYZ selectedPoint = uiDocument.Selection.PickPoint("Pick a point");
                FilteredElementCollector collector = new FilteredElementCollector(document).OfClass(typeof(ConduitType));
                ConduitType? type = collector.FirstElement() as ConduitType;

                IList<Element> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).ToElements();
                Level? fl1 = levels.ElementAt(0) as Level;
                Level? fl2 = levels.ElementAt(1) as Level;
                var result = document.Transaction("Create Shaft", _ =>
                {
                    if (fl1 != null && fl2 != null)
                    {
                        double heightOfFloor = fl2.Elevation - fl1.Elevation;
                        var familySymbol = document.GetFamilySymbol(RoutingFamilyType.ConnectorTwoSide)!;
                        var bottomConnector = familySymbol.Instantiate(selectedPoint, fl1, StructuralType.NonStructural);
                        bottomConnector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(0);
                        var topConnector = familySymbol.Instantiate(selectedPoint, fl1, StructuralType.NonStructural);
                        topConnector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(heightOfFloor);

                        var firstPoint = new XYZ(selectedPoint.X, selectedPoint.Y, fl1.Elevation);
                        var secondPoint = new XYZ(selectedPoint.X, selectedPoint.Y, fl2.Elevation);
                        Conduit.Create(document, type!.Id, firstPoint, secondPoint, fl1.Id);
                    }
                    return Result.Succeeded;
                });

                return result;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception e)
            {
                CommandUtils.DebugAlertException(e);
                return Result.Failed;
            }
        }
    }
}
