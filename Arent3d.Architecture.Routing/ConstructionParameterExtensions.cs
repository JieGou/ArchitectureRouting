using Arent3d.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;

namespace Arent3d.Architecture.Routing
{
	public static class ConstructionParameterExtensions
	{
		public static void SetConstructionType(this Conduit element, string value)
		{
			element.SetProperty(ConstructionParameter.Construction, value);
		}
		
		public static void SetConstructionType(this FamilyInstance element, string value)
		{
			element.SetProperty(ConstructionParameter.Construction, value);
		}
	}
}