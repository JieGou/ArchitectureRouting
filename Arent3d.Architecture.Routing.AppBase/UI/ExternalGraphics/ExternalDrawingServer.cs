using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
    public class ExternalDrawingServer : DrawingServer
    {
        public List<Line> LineList { get; set; }
        public ExternalDrawingServer(Document doc) : base(doc)
        {
            this.LineList = new List<Line>();
        }

        public override string GetName()
        {
            return "IMPACT External Drawing Server";
        }

        public override string GetDescription()
        {
            return "IMPACT External Drawing Server";
        }

        public XYZ? BasePoint { get; set; }

        public XYZ? NextPoint { get; set; }

        public override List<Line> PrepareProfile()
        {
            return LineList;
        }

        public override bool CanExecute(View view)
        {
            return true;
        }

        public override Outline? GetBoundingBox(View view)
        {
            if (this.LineList.Count > 0)
            {
                return new Outline(this.LineList[0].GetEndPoint(0), this.LineList[0].GetEndPoint(1));
            }

            return null;
        }
    }
}
