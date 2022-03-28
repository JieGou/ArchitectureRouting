using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models
{
  public class FolderItemModel
  {
    public string Name { get ; set ; } = string.Empty ;
    public List<FolderItemModel> FolderItems { get ; set ; } = new() ;
  }
}