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
            this.DrawingServer.LineList.Clear();

            if (this.DrawingServer == null ||
                this.DrawingServer.BasePoint == null ||
                this.DrawingServer.NextPoint == null ||
                this.DrawingServer.BasePoint.DistanceTo(this.DrawingServer.NextPoint) <= 0.1)
            {
                return;
            }

            this.DrawingServer.LineList.Add(Line.CreateBound(this.DrawingServer.BasePoint, this.DrawingServer.NextPoint));
        }
    }
}
