namespace Arent3d.Architecture.Routing.AppBase.ViewModel.Models
{
  public class PickUpOutPutLevelItem
  {
    public int LevelIndex { get ; }
    public string OutputString { get ; }

    public PickUpOutPutLevelItem( int levelIndex, string outputString )
    {
      LevelIndex = levelIndex ;
      OutputString = outputString ;
    }
  }
}