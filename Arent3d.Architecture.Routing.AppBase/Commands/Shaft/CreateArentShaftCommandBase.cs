using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Shaft
{
    public abstract class CreateArentShaftCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDocument = uiApp.ActiveUIDocument;
            Document document = uiDocument.Document;
            Application app = uiApp.Application;
            Selection selection = uiDocument.Selection;
            try
            {
                // Store selected points
                List<XYZ> pointsSelected = new();
                // Pick first point 
                XYZ firstPoint = selection.PickPoint("Pick first point");
                // Add first point to list selected points
                pointsSelected.Add(firstPoint);

                XYZ? nextPoint = null;
                // This is the object to render the guide line
                LineExternal lineExternal = new LineExternal(uiApp);
                try
                {
                    // Add first point to list picked points
                    lineExternal.PickedPoints.Add(firstPoint);
                    var tmp = firstPoint;
                    // Loop to select point in plane
                    // If next point equal first point -> End loop
                    while (firstPoint != nextPoint)
                    {
                        // Assign first point
                        lineExternal.DrawingServer.BasePoint = tmp;
                        // Render the guide line
                        lineExternal.DrawExternal();

                        // Pick next point 
                        nextPoint = selection.PickPoint("Pick next point");
                        // Check distance from previous point to next point
                        if (firstPoint.DistanceTo(nextPoint) > 0.1)
                            pointsSelected.Add(nextPoint);
                        else
                        {
                            // If distance < 0.1 add first point to list selected points to create Polygon and break loop
                            pointsSelected.Add(firstPoint);
                            break;
                        }
                        // Set current point equal next point
                        tmp = nextPoint;
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    // End to render guide line
                    lineExternal.Dispose();
                }
                // If next point is null. Return success to end command
                if (nextPoint == null) return Result.Succeeded;
                // If only 2 points are selected and have similarity -> Show message -> Cancel command
                if (pointsSelected.Count == 2 && nextPoint.DistanceTo(firstPoint) <= 0.1)
                {
                    lineExternal.Dispose();
                    TaskDialog.Show("Dialog.Commands.Draw.Common.Title.Error".GetAppStringByKeyOrDefault("エラー"),
                                    "Dialog.Commands.Draw.Common.Body.Error".GetAppStringByKeyOrDefault("始点と終点が同じです。"));
                    return Result.Cancelled;
                }
                // If more than 2 points are selected and the starting and ending points are not close to each other -> Return fail
                if (pointsSelected.First().DistanceTo(pointsSelected.Last()) > 0.1) return Result.Failed;

                // Get height setting
                HeightSettingStorable heightSetting = document.GetHeightSettingStorable();
                var levels = heightSetting.Levels.OrderBy(x => x.Elevation).ToList();
                // Get lowest and highest level
                Level? lowestLevel = levels.FirstOrDefault();
                Level? highestLevel = levels.LastOrDefault();
                if (lowestLevel == null && highestLevel == null) return Result.Failed;

                using (Transaction trans = new Transaction(document, "Create Arent Shaft"))
                {
                    trans.Start();

                    // Create CurveArray for NewOpening method from list selected points
                    CurveArray shaftProfile = app.Create.NewCurveArray();
                    var firstP = pointsSelected[0];
                    for (var i = 1; i < pointsSelected.Count; i++)
                    {
                        var nextP = pointsSelected[i];
                        if (firstP.DistanceTo(nextP) > 0.001)
                        {
                            Curve curve = Line.CreateBound(firstP, nextP);
                            shaftProfile.Append(curve);
                        }

                        firstP = nextP;
                    }
                    // Create Shaft opening
                    Opening shaftOpening = document.Create.NewOpening(lowestLevel, highestLevel, shaftProfile);
                    // Set offset from top
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(0);
                    // Set offset from base
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);
                    // Set base level is lowest level
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).Set(lowestLevel!.Id);
                    // Set top level is highest level
                    shaftOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(highestLevel!.Id);

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
        }
    }
}
