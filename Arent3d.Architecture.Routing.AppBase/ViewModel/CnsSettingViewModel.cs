using System ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Utility ;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog ;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CnsSettingViewModel : ViewModelBase
  {
    private const string DefaultCategoryName = "未設定" ;
    public ObservableCollectionEx<CnsSettingModel> CnsSettingModels { get ; set ; }

    public CnsSettingStorable CnsSettingStorable { get ; }

    public string ApplyToSymbolsText { get ; set ; }
    public string ReadCnsFilePath { get ; set ; }

    public CnsSettingViewModel( CnsSettingStorable cnsStorables )
    {
      CnsSettingStorable = cnsStorables ;
      ApplyToSymbolsText = string.Empty ;
      ReadCnsFilePath = string.Empty ;
      CnsSettingModels = new ObservableCollectionEx<CnsSettingModel>( cnsStorables.CnsSettingData ) ;
      CnsSettingModels.ItemPropertyChanged += CnsSettingModelsOnItemPropertyChanged;
      AddDefaultValue() ;
      ReadFileCommand = new RelayCommand<object>( _ => true, // CanExecute()
        p => { ReadFile() ; } // Execute()
      ) ;

      WriteFileCommand = new RelayCommand<object>( _ => true, // CanExecute()
        p => { WriteFile() ; } // Execute()
      ) ;

      AddRowCommand = new RelayCommand<object>( _ => true, // CanExecute()
        p => { AddRow() ; } // Execute()
      ) ;

      DeleteRowCommand = new RelayCommand<int>( _ => true, // CanExecute()
        DeleteRow // Execute()
      ) ;

      SaveCommand = new RelayCommand<object>( _ => true, // CanExecute()
        _ => { Save() ; } // Execute()
      ) ;
      
      SetConstructionItemForAllCommand = new RelayCommand<int>( _ => true, // CanExecute()
        selectedIndex => { SetConstructionItemForSymbol( cnsStorables, selectedIndex, CnsSettingStorable.UpdateItemType.All ) ; } // Execute()
      ) ;
      ApplyRangSelectionCommand = new RelayCommand<int>( p => true, // CanExecute()
        _ => { SetConstructionItemForRange( cnsStorables ) ; } // Execute()
      ) ;
    }

    private void CnsSettingModelsOnItemPropertyChanged( object sender, ItemPropertyChangedEventArgs e )
    {
      var cnsSettingModelChanged = CnsSettingModels[ e.CollectionIndex ] ;
      if ( cnsSettingModelChanged is not { IsDefaultItemChecked: true } ) return ;

      ApplyToSymbolsText = cnsSettingModelChanged.CategoryName ;
      var restCnsSettingModels = CnsSettingModels.Where( x => x.CategoryName != cnsSettingModelChanged.CategoryName ).EnumerateAll() ;
      foreach ( var item in restCnsSettingModels ) {
        item.IsDefaultItemChecked = false ;
      }
    }

    public ICommand ReadFileCommand { get ; set ; }
    public ICommand WriteFileCommand { get ; set ; }
    public ICommand AddRowCommand { get ; set ; }
    public ICommand DeleteRowCommand { get ; set ; }
    public ICommand SaveCommand { get ; set ; }
    public ICommand SetConstructionItemForAllCommand { get ; set ; }
    public ICommand ApplyRangSelectionCommand { get ; set ; }

    private void ReadFile()
    {
      // Configure open file dialog box
      var dlg = new OpenFileDialog { 
        FileName = "Document", // Default file name
        DefaultExt = ".cns", // Default file extension
        Filter = "CNS files|*.cns" // Filter files by extension
      } ;

      // Show open file dialog box
      var result = dlg.ShowDialog() ;
      if ( result != true ) return ;
      
      // Process open file dialog box results
      // Open document
      var fileName = dlg.FileName ;
      CnsSettingModels.Clear() ;
      var index = 1 ;
      var inputData = File.ReadLines( fileName ) ;
      foreach ( var line in inputData ) {
        if ( string.IsNullOrWhiteSpace( line ) || line.Equals( DefaultCategoryName ) ) continue ;
        
        var partsOfLine = line.Split( '_' ) ;

        if ( partsOfLine.Length < 1 ) {
          MessageBox.Show( @"Wrong format in file CNS" ) ;
          return ;
        }
        var categoryName = partsOfLine[ 0 ].Trim() ;
        var isDefaultItemChecked = partsOfLine.Length == 2 && Convert.ToBoolean( partsOfLine[1] ) ;
        CnsSettingModels.Add( new CnsSettingModel( index, categoryName, isDefaultItemChecked ) ) ;
        index++ ;
      }
      CnsSettingModels.Insert( 0,new CnsSettingModel( 0, DefaultCategoryName ) );
      AddDefaultValue() ;
      ReadCnsFilePath = fileName ;
      CnsSettingStorable.CnsSettingData = CnsSettingModels ;
    }

    private void WriteFile()
    {
      // Configure open file dialog box
      var dlg = new SaveFileDialog { 
        FileName = "", // Default file name
        DefaultExt = ".cns", // Default file extension
        Filter = "CNS files|*.cns" // Filter files by extension
      } ;

      // Show open file dialog box
      var result = dlg.ShowDialog() ;

      // Process open file dialog box results
      if ( result == true ) {
        WriteContentsToFile( dlg.FileName ) ;
        if ( ! string.IsNullOrEmpty( ReadCnsFilePath ) )
          WriteContentsToFile( ReadCnsFilePath );
      }
    }

    private void Save()
    {
      CnsSettingStorable.CnsSettingData = CnsSettingModels ;
      if ( ! string.IsNullOrEmpty( ReadCnsFilePath ) )
        WriteContentsToFile( ReadCnsFilePath );
    }

    public void WriteContentsToFile( string fileName )
    {
      var contents = "" ;
      foreach ( var cnsSettingModel in CnsSettingModels )
        if ( ! string.IsNullOrEmpty( cnsSettingModel.CategoryName ) && ! cnsSettingModel.CategoryName.Equals( DefaultCategoryName ) )
          contents += cnsSettingModel.CategoryName.Trim() + "_" + cnsSettingModel.IsDefaultItemChecked + Environment.NewLine + Environment.NewLine ;

      File.WriteAllText( fileName, contents ) ;
    }

    private void AddRow()
    {
      CnsSettingModels.Add( new CnsSettingModel( CnsSettingModels.Count + 1, "" ) ) ;
    }

    private void DeleteRow( int index )
    {
      if ( index == -1 ) return ;
      CnsSettingModels.RemoveAt( index ) ;
      AddDefaultValue() ;
      UpdateSequence() ;
    }

    private void UpdateSequence()
    {
      for ( var i = 0 ; i < CnsSettingModels.Count ; i++ ) {
        CnsSettingModels[ i ].Sequence = i + 1 ;
      }
    }

    private void AddDefaultValue()
    {
      if ( CnsSettingModels.Count == 0 )
        CnsSettingModels.Add( new CnsSettingModel( sequence: 0, categoryName: DefaultCategoryName, true ) ) ;
      else if ( CnsSettingModels.All( t => ! t.IsDefaultItemChecked ) ) {
        var defaultCnsModel = CnsSettingModels.FirstOrDefault( t => t.CategoryName.Equals( DefaultCategoryName ) ) ;
        if ( defaultCnsModel == null )
          CnsSettingModels.Add( new CnsSettingModel( sequence: 0, categoryName: DefaultCategoryName, true ) ) ;
        else
          defaultCnsModel.IsDefaultItemChecked = true ;
      }
    }
    
    private void SetConstructionItemForSymbol( CnsSettingStorable cnsStorable, int selectedIndex, CnsSettingStorable.UpdateItemType updateType )
    {
      if ( selectedIndex == -1 )
        ApplyToSymbolsText = DefaultCategoryName ;
      else {
        var cnsSettingModel = CnsSettingModels.ElementAt( selectedIndex ) ;
        ApplyToSymbolsText = cnsSettingModel.CategoryName ;
        cnsStorable.ElementType = updateType ;
        cnsStorable.CnsSettingData = CnsSettingModels ;
      }
    }

    private void SetConstructionItemForRange( CnsSettingStorable cnsStorable )
    {
      var item = CnsSettingModels.FirstOrDefault( x => x.IsDefaultItemChecked ) ;
      ApplyToSymbolsText = item != null ? item.CategoryName : string.Empty ;
      cnsStorable.ElementType = CnsSettingStorable.UpdateItemType.Range ;
      cnsStorable.CnsSettingData = CnsSettingModels ;
    }
  }
}
