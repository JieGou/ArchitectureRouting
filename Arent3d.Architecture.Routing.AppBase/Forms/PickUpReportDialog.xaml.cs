using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Windows ;
using System.Windows.Forms ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpReportDialog : Window
  {
    private readonly List<ListBoxItem> _fileTypes ;
    private readonly List<ListBoxItem> _outputItems ;
    private List<ListBoxItem> _itemTypes ;
    private List<string> _fileTypesSelected ;
    private string _itemSelected ;
    private List<string> _itemsTypesSelected;
    private string path ;
    public PickUpReportDialog( Document document)
    {
      InitializeComponent() ;
      _fileTypes = new List<ListBoxItem>() ;
      _outputItems = new List<ListBoxItem>() ;
      _itemTypes = new List<ListBoxItem>() ;
      _fileTypesSelected = new List<string>() ;
      _itemSelected = string.Empty ;
      _itemsTypesSelected = new List<string>() ;
      path = string.Empty ;
      CreateCheckBoxList();
    }
    
    private void Button_Register( object sender, RoutedEventArgs e )
    {

    }
    
    private void Button_Delete( object sender, RoutedEventArgs e )
    {

    }
    
    private void Button_DeleteAll( object sender, RoutedEventArgs e )
    {

    }
    
    private void Button_Setting( object sender, RoutedEventArgs e )
    {
      ChangeValueItemType() ;
      var dialog = new PickUpItemSelectionDialog( _itemTypes ) ;

      dialog.ShowDialog() ;
      _itemsTypesSelected = dialog.ItemsTypesSelected;
    }
    
    private void Button_Reference( object sender, RoutedEventArgs e )
    {
      const string fileName = "file_name.xlsx" ;
      SaveFileDialog saveFileDialog = new SaveFileDialog { FileName = fileName, Filter = "Csv files (*.xlsx)|*.xlsx", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;

      if ( saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return ;
      path = saveFileDialog.FileName ;
      TbFolder.Text = Path.GetDirectoryName( path ) ;
      TbFileName.Text = Path.GetFileName( path ) ;
    }

    private void Button_Execute( object sender, RoutedEventArgs e )
    {
      _fileTypesSelected = new List<string>() ;
      _itemSelected = string.Empty ;
      foreach ( var fileType in _fileTypes ) {
        if ( fileType.TheValue == true )
          _fileTypesSelected.Add( fileType.TheText! ) ;
      }

      foreach ( var outputItem in _outputItems ) {
        if ( outputItem.TheValue == true )
          _itemSelected = outputItem.TheText! ;
      }

      //DialogResult = true ;
      //Close() ;
    }
    
    private void Button_Cancel( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }
    
    public class ListBoxItem
    {
      public string? TheText { get; set; }
      public bool TheValue { get; set; }
    }

    private void CreateCheckBoxList()
    {
      _fileTypes.Add( new ListBoxItem { TheText = "拾い根拠確認表", TheValue = false } ) ;
      _fileTypes.Add( new ListBoxItem { TheText = "拾い出し集計表", TheValue = false } ) ;
      _fileTypes.Add( new ListBoxItem { TheText = "ユーザファイル", TheValue = true } ) ;
      LbFileType.ItemsSource = _fileTypes ;
      
      _outputItems.Add( new ListBoxItem { TheText = "全項目出力", TheValue = true } ) ;
      _outputItems.Add( new ListBoxItem { TheText = "出力項目選択", TheValue = false } ) ;
      LbOutputItem.ItemsSource = _outputItems ;
      
      _itemTypes.Add( new ListBoxItem { TheText = "長さ物", TheValue = true } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "工事部材", TheValue = true } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "機器取付", TheValue = false } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "結線", TheValue = false } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "盤搬入据付", TheValue = false } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "内装・補修・設備", TheValue = true } ) ;
      _itemTypes.Add( new ListBoxItem { TheText = "その他", TheValue = false } ) ;

      _itemsTypesSelected = new List<string>() { "長さ物", "工事部材", "内装・補修・設備" } ;
    }

    private void ChangeValueItemType()
    {
      foreach ( var itemType in _itemTypes ) {
        itemType.TheValue = _itemsTypesSelected.Contains( itemType.TheText! ) ? true : false ;
      }
    }
  }
}