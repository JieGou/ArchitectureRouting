using System.Linq;
using Arent3d.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Electrical;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
	public class ConstructionSelectionFilter : ISelectionFilter
	{
		public static ISelectionFilter Instance { get; } = new ConstructionSelectionFilter();

		private ConstructionSelectionFilter()
		{
		}

		public bool AllowElement(Element elem)
		{
			return (BuiltInCategorySets.Conduits.Any(p => p == elem.GetBuiltInCategory())
			        && (elem is FamilyInstance || elem is Conduit));
		}

		public bool AllowReference(Reference reference, XYZ position) => false;
	}
}