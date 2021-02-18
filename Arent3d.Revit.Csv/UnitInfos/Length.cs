using Autodesk.Revit.DB ;

namespace Arent3d.Revit.Csv.UnitInfos
{
  internal class Length : UnitInfo
  {
    private static readonly UnitAbbrList LengthAbbrList = new UnitAbbrList( new[] { ( "meters", "m" ), ( "feet", "ft" ), ( "inches", "in" ) } ) ;

    protected override UnitAbbrList GetUnitAbbrList() => LengthAbbrList ;

    protected override ForgeTypeId GetSpecTypeId() => SpecTypeId.Length ;
  }
}