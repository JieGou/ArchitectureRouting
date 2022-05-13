namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class GradeSettingModel
  {
    public bool IsInGrade3Mode { get ; set ; }

    public GradeSettingModel()
    {
      IsInGrade3Mode = false ;
    }

    public GradeSettingModel( bool? isInGrade3Mode )
    {
      IsInGrade3Mode = isInGrade3Mode ?? false ;
    }
  }
}