﻿using System.Collections.Generic ;
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
    public string SelectedTableType { get ; set ; }

    public bool IsCreateTableEachFloors => CheckBoxEachFloor.IsChecked ?? false ;

    public CreateTableByFloors( Document doc , IEnumerable<string> tableTypes)
    {
      InitializeComponent() ;
      LevelList = new ObservableCollection<LevelInfo>( doc.GetAllElements<Level>()
        .OfCategory( BuiltInCategory.OST_Levels ).Select( ToLevelInfo ).OrderBy( l => l.Elevation ) ) ;
      TableTypes = new ObservableCollection<string>(tableTypes) ;
      SelectedTableType = TableTypes.FirstOrDefault() ?? string.Empty ;
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
      DialogResult = true ;
      Close() ;
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