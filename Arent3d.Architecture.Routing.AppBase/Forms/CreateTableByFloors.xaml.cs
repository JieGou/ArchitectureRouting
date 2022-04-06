using System.Linq ;
using System.Windows ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using static Arent3d.Architecture.Routing.AppBase.Forms.GetLevel ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// Interaction logic for CreateTableByFloors.xaml
  /// </summary>
  public partial class CreateTableByFloors : Window
  {
    public ObservableCollection<string> TableTypes { get ; set ; }
    public ObservableCollection<LevelInfo> LevelList { get ; }
    public string SelectedTableType { get ; set ; } = "Detail Table" ;

    public CreateTableByFloors( Document doc )
    {
      InitializeComponent() ;
      LevelList = new ObservableCollection<LevelInfo>( doc.GetAllElements<Level>()
        .OfCategory( BuiltInCategory.OST_Levels ).Select( ToLevelInfo ).OrderBy( l => l.Elevation ) ) ;
      TableTypes = new ObservableCollection<string>() ;
      TableTypes.Add( "Detail Table" ) ;
      TableTypes.Add( "Electrical Symbol Table" ) ;
    }

    private static LevelInfo ToLevelInfo( Level level )
    {
      return new LevelInfo
      {
        Elevation = level.Elevation, LevelId = level.Id, IsSelected = false, LevelName = level.Name
      } ;
    }

    private void CheckAll( object sender, RoutedEventArgs e )
    {
      SelectAll( true ) ;
    }

    private void UncheckAll( object sender, RoutedEventArgs e )
    {
      SelectAll( false ) ;
    }

    private void ToggleAll( object sender, RoutedEventArgs e )
    {
      SelectToggle() ;
    }

    private void SelectButton_Click( object sender, RoutedEventArgs e )
    {
      this.DialogResult = true ;
      this.Close() ;
    }

    private void SelectAll( bool select )
    {
      LevelList.ForEach( level => level.IsSelected = select ) ;
    }

    private void SelectToggle()
    {
      LevelList.ForEach( level => level.IsSelected = ! level.IsSelected ) ;
    }
  }
}