using System.ComponentModel ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CeedDetailModel : INotifyPropertyChanged
  {
    public string ProductCode { get ; set ; }
    public string ProductName { get ; set ; }
    public string Standard { get ; set ; }

    private string _classification = string.Empty ;

    public string Classification
    {
      get => _classification ;
      set
      {
        _classification = value ;
        OnPropertyChanged( nameof( Classification ) ) ;
      }
    }

    public string Size1 { get ; set ; }
    public string Size2 { get ; set ; }

    private double _quantity = 0 ;

    public double Quantity
    {
      get => _quantity ;
      set
      {
        _quantity = value ;
        OnPropertyChanged( nameof( Quantity ) ) ;

        Total = ( value + QuantityCalculate ) * QuantitySet ;
      }
    }

    public string Unit { get ; set ; }
    public string ParentId { get ; set ; }
    public string Trajectory { get ; set ; }

    public string Specification { get ; set ; }
    public int Order { get ; set ; }
    public string CeedCode { get ; set ; }

    private string _constructionClassification = string.Empty ;

    public string ConstructionClassification
    {
      get => _constructionClassification ;
      set
      {
        _constructionClassification = value ;
        OnPropertyChanged( nameof( ConstructionClassification ) ) ;

        if ( ! string.IsNullOrEmpty( Classification ) && value is not ("天井ふところ" or "ケーブルラック配線" or "二重床" or "露出") )
          Classification = string.Empty ;

        if ( value is "露出" && IsConduit )
          Classification = "露出" ;

        if ( value is ( "地中埋設" or "床隠蔽" or "冷房配管共巻配線" ) && IsConduit )
          Classification = "隠蔽" ;

        AllowChangeClassification = AllowInputQuantity && IsConduit & value is "天井ふところ" or "ケーブルラック配線" or "二重床" ;
      }
    }

    private double _quantityCalculate = 1 ;

    public double QuantityCalculate
    {
      get => _quantityCalculate ;
      set
      {
        _quantityCalculate = value ;
        OnPropertyChanged( nameof( QuantityCalculate ) ) ;

        Total = ( Quantity + QuantityCalculate ) * QuantitySet ;
      }
    }

    private double _quantitySet = 1 ;

    public double QuantitySet
    {
      get => _quantitySet ;
      set
      {
        _quantitySet = value ;
        OnPropertyChanged( nameof( QuantitySet ) ) ;

        Total = ( Quantity + QuantityCalculate ) * value ;
      }
    }

    private double _total = 0 ;

    public double Total
    {
      get => _total ;
      set
      {
        _total = value ;
        OnPropertyChanged( nameof( Total ) ) ;
      }
    }

    public string Description { get ; set ; }

    public bool AllowInputQuantity { get ; set ; }

    private bool _allowChangeClassification = false ;

    public bool AllowChangeClassification
    {
      get => _allowChangeClassification ;
      set
      {
        _allowChangeClassification = value ;
        OnPropertyChanged( nameof( AllowChangeClassification ) ) ;
      }
    }

    public string ModeNumber { get ; set ; }
    public bool IsConduit { get ; set ; }

    public CeedDetailModel( string? productCode, string? productName, string? standard, string? classification, double? quantity, string? unit, string? parentId, string? trajectory, string? size1, string? size2, string? specification, int? order, string? modeNumber, string? ceedCode, string? constructionClassification, double? quantityCalculate, double? quantitySet, double? total, string? description, bool? allowInputQuantity, bool isConduit = false )
    {
      ProductCode = productCode ?? string.Empty ;
      ProductName = productName ?? string.Empty ;
      Standard = standard ?? string.Empty ;
      Classification = classification ?? string.Empty ;
      Quantity = quantity ?? 0 ;
      Unit = unit ?? string.Empty ;
      ParentId = parentId ?? string.Empty ;
      Trajectory = trajectory ?? string.Empty ;
      Size1 = size1 ?? string.Empty ;
      Size2 = size2 ?? string.Empty ;
      Specification = specification ?? string.Empty ;
      Order = order ?? 1 ;
      ModeNumber = modeNumber ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      QuantityCalculate = quantityCalculate ?? 0 ;
      QuantitySet = quantitySet ?? 0 ;
      Total = total ?? 0 ;
      Description = description ?? string.Empty ;
      AllowInputQuantity = allowInputQuantity ?? false ;
      AllowChangeClassification = false ;
      IsConduit = isConduit ;
    }

    public event PropertyChangedEventHandler? PropertyChanged ;

    private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }
  }
  
  public enum ConstructionClassificationType
  {
    ケーブルラック配線,
    天井ふところ,
    二重床,
    地中埋設,
    床隠蔽,
    冷房配管共巻配線,
    露出
  }
}