using Arent3d.Revit;
using Arent3d.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
    public abstract class CreateShaftCommandBase : IExternalCommand
    {
        private const double WIDTH = 3000;
        private const double HEIGHT = 2000;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDocument = uiApp.ActiveUIDocument;
            Document document = uiDocument.Document;
            Application app = uiApp.Application;
            var selection = uiDocument.Selection;
            try
            {
                XYZ selectedPoint = selection.PickPoint("Pick a point");
                FilteredElementCollector collector = new FilteredElementCollector(document).OfClass(typeof(ConduitType));
                ConduitType? type = collector.FirstElement() as ConduitType;

                IList<Element> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).ToElements();
                Level? lowestLevel = levels.ElementAt(0) as Level;
                Level? highestLevel = levels.ElementAt(levels.Count - 1) as Level;
                if (lowestLevel == null && highestLevel == null) return Result.Failed;
                var result = document.Transaction("Create Shaft Connector", _ =>
                {
                    XYZ point1 = new XYZ(selectedPoint.X - (WIDTH / 2).MillimetersToRevitUnits(),
                                         selectedPoint.Y + (HEIGHT / 2).MillimetersToRevitUnits(),
                                         selectedPoint.Z);
                    XYZ point2 = new XYZ(selectedPoint.X + (WIDTH / 2).MillimetersToRevitUnits(),
                                         point1.Y,
                                         selectedPoint.Z);
                    XYZ point3 = new XYZ(point2.X,
                                         selectedPoint.Y - (HEIGHT / 2).MillimetersToRevitUnits(),
                                         selectedPoint.Z);
                    XYZ point4 = new XYZ(point1.X,
                                         point3.Y,
                                         selectedPoint.Z);
                    Curve left = Line.CreateBound(point1, point4);
                    Curve upper = Line.CreateBound(point1, point2);
                    Curve right = Line.CreateBound(point2, point3);
                    Curve lower = Line.CreateBound(point3, point4);
                    CurveArray shaftProfile = app.Create.NewCurveArray();
                    shaftProfile.Append(left);
                    shaftProfile.Append(upper);
                    shaftProfile.Append(right);
                    shaftProfile.Append(lower);
                    Opening shaftOpening = document.Create.NewOpening(lowestLevel, highestLevel, shaftProfile);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(0);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).Set(lowestLevel!.Id);
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(highestLevel!.Id);

                    double heightOfFloor = highestLevel!.Elevation - lowestLevel!.Elevation;
                    var familySymbol = document.GetFamilySymbols(RoutingFamilyType.ConnectorTwoSide).FirstOrDefault() ?? throw new InvalidOperationException() ;
                    var bottomConnector = familySymbol.Instantiate(selectedPoint, lowestLevel, StructuralType.NonStructural);
                    bottomConnector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(0);
                    var topConnector = familySymbol.Instantiate(selectedPoint, lowestLevel, StructuralType.NonStructural);
                    topConnector.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(heightOfFloor);

                    var firstPoint = new XYZ(selectedPoint.X, selectedPoint.Y, lowestLevel.Elevation);
                    var secondPoint = new XYZ(selectedPoint.X, selectedPoint.Y, highestLevel.Elevation);
                    Conduit.Create(document, type!.Id, firstPoint, secondPoint, lowestLevel.Id);

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
