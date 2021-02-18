namespace Arent3d.Revit.Csv
{
  public class LengthParameterData : ParameterData<LengthParameterData>
  {
    static LengthParameterData()
    {
      SetUnitInfo( new UnitInfos.Length() ) ;
    }

    internal LengthParameterData()
    {
    }
  }
}