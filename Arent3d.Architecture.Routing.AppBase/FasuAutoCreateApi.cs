using Autodesk.Revit.DB ;
using System.Collections.Generic;

namespace Arent3d.Architecture.Routing.AppBase
{
    public class FasuAutoCreateApi
    {
        public static IList<Element> GetAllSpaces(Document document)
        {
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaces);
            FilteredElementCollector collector = new FilteredElementCollector(document);
            IList<Element> spaces = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            return spaces;
        }
        public static IList<Element> GetAllGroups(Document document)
        {
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_IOSModelGroups);
            FilteredElementCollector collector = new FilteredElementCollector(document);
            IList<Element> groups = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();
            return groups;
        }
    }
}