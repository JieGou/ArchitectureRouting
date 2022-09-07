using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
    public class LineExternal : DrawExternalBase
    {
        public LineExternal(UIApplication uiApplication) : base(uiApplication)
        {
        }

        public override void DrawExternal()
        {
            this.DrawingServer.CurveList.Clear();

            if (this.DrawingServer?.BasePoint == null || 
                this.DrawingServer.NextPoint == null || 
                this.DrawingServer.BasePoint.DistanceTo(this.DrawingServer.NextPoint) <= 0.1)
            {
                return;
            }
            if (this.PickedPoints.Count > 1)
            {
                var firstP = this.PickedPoints[0];
                for (var i = 1; i < this.PickedPoints.Count; i++)
                {
                    var nextP = this.PickedPoints[i];
                    if (firstP.DistanceTo(nextP) > 0.001)
                    {
                        var line = Line.CreateBound(firstP, nextP);
                        this.DrawingServer.CurveList.Add(line);
                    }

                    firstP = nextP;
                }
            }
            this.DrawingServer.CurveList.Add(Line.CreateBound(this.DrawingServer.BasePoint, this.DrawingServer.NextPoint));
        }
    }
}
