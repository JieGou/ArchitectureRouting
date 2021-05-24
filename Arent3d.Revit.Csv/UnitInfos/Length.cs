using Autodesk.Revit.DB ;

#if REVIT2019 || REVIT2020
using SpecTypeProxy = Autodesk.Revit.DB.UnitType ;
using DisplayUnitTypeProxy = Autodesk.Revit.DB.DisplayUnitType ;
#else
using SpecTypeProxy = Autodesk.Revit.DB.ForgeTypeId ;
using DisplayUnitTypeProxy = Autodesk.Revit.DB.ForgeTypeId ;
#endif

namespace Arent3d.Revit.Csv.UnitInfos
{
  internal class Length : UnitInfo
  {
    private static readonly UnitAbbrList LengthAbbrList = new UnitAbbrList( new[] { ( "meters", "m" ), ( "feet", "ft" ), ( "inches", "in" ) } ) ;

    protected override UnitAbbrList GetUnitAbbrList() => LengthAbbrList ;

#if REVIT2019 || REVIT2020
    protected override SpecTypeProxy GetSpecTypeId() => UnitType.UT_Length ;
#else
    protected override SpecTypeProxy GetSpecTypeId() => SpecTypeId.Length ;
#endif
  }
}