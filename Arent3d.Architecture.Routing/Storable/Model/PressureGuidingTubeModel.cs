using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public enum TubeTypeEnum
  {
    //Copper tube      
    銅管,
    // Control copper tube
    コントロール銅管,
    // Gas tube
    ガス管,
    // Stainless steel tube
    ステンレス管,
    // Polyethylene tube
    ポリエチレンチューブ,
    // Hard nylon tube
    硬質ナイロンチューブ,
    // Urethane tube
    ウレタンチューブ,
    // Teflon tube
    テフロンチューブ 
  }

  public enum CreationModeEnum
  {
    //Automatic
    自動モード,
    //Manual
    手動モード 
  }
  
  public class PressureGuidingTubeModel
  { 
    public double Height { get ; set ; }
    public string TubeType { get ; set ; }
    public string CreationMode { get ; set ; }

    public PressureGuidingTubeModel()
    {
      Height = 1000 ;
      TubeType = TubeTypeEnum.銅管.GetFieldName();
      CreationMode = CreationModeEnum.自動モード.GetFieldName() ;
    }
    
    public PressureGuidingTubeModel(double? height, string? tubeType, string? creationMode)
    {
      Height = height ?? 1000 ;
      TubeType = tubeType ?? TubeTypeEnum.銅管 .GetFieldName();
      CreationMode = creationMode ?? CreationModeEnum.自動モード.GetFieldName();
    }
  }
}