﻿namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class GradeSettingModel
  {
    public int GradeMode { get ; set ; }

    public GradeSettingModel()
    {
      GradeMode = 1 ;
    }

    public GradeSettingModel( int? gradeMode )
    {
      GradeMode = gradeMode ?? 1 ;
    }
  }
}