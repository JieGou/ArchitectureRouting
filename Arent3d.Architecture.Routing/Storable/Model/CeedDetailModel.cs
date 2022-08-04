namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CeedDetailModel : NotifyPropertyChanged
  {
    public const string Dash = "-" ;
    private const string DefaultQuantity = "100" ;
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

        switch ( value ) {
          case nameof( ClassificationType.露出 ) when IsConduit && CeedCode != string.Empty :
          {
            if ( CeedCode != string.Empty )
              Quantity = Dash ;
            AllowInputQuantity = false ;
            break ;
          }
          case nameof( ClassificationType.隠蔽 ) when IsConduit :
          {
            if ( Quantity == Dash && CeedCode != string.Empty )
              Quantity = DefaultQuantity ;
            AllowInputQuantity = true ;
            break ;
          }
        }
      }
    }

    public string Size1 { get ; set ; }
    public string Size2 { get ; set ; }

    private string _quantity = Dash ;

    public string Quantity
    {
      get => _quantity ;
      set
      {
        _quantity = value ;
        OnPropertyChanged( nameof( Quantity ) ) ;

        var doubleValue = value == Dash ? "0" : value ;
        Total = ( double.Parse( doubleValue ) + QuantityCalculate ) * QuantitySet ;
      }
    }

    public string Unit { get ; set ; }
    public string ParentId { get ; set ; }
    public string Trajectory { get ; set ; }

    public string Specification { get ; set ; }
    public int Order { get ; set ; }
    public string Type { get ; set ; }
    public string ParentPartModel { get ; set ; }
    public string CeedCode { get ; set ; }

    private string _constructionClassification = string.Empty ;

    public string ConstructionClassification
    {
      get => _constructionClassification ;
      set
      {
        _constructionClassification = value ;
        OnPropertyChanged( nameof( ConstructionClassification ) ) ;

        switch ( value ) {
          case nameof( ClassificationType.露出 ) when IsConduit :
          {
            Classification = nameof( ClassificationType.露出 ) ;
            if ( CeedCode != string.Empty ) {
              Quantity = Dash ;
              AllowInputQuantity = false ;
            }

            break ;
          }
          case nameof( ConstructionClassificationType.地中埋設 ) or nameof( ConstructionClassificationType.床隠蔽 ) or nameof( ConstructionClassificationType.冷房配管共巻配線 ) when IsConduit :
            Classification = nameof( ClassificationType.隠蔽 ) ;
            AllowInputQuantity = true ;
            break ;
        }

        if ( string.IsNullOrEmpty( Classification ) && value is (nameof( ConstructionClassificationType.天井ふところ ) or nameof( ConstructionClassificationType.ケーブルラック配線 ) or nameof( ConstructionClassificationType.二重床 ) ) && IsConduit ) {
          Classification = nameof( ClassificationType.隠蔽 ) ;
          AllowInputQuantity = true ;
        }

        AllowChangeClassification = IsConduit & value is nameof( ConstructionClassificationType.天井ふところ ) or nameof( ConstructionClassificationType.ケーブルラック配線 ) or nameof( ConstructionClassificationType.二重床 ) ;
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

        var doubleValue = Quantity == Dash ? "0" : Quantity ;
        Total = ( double.Parse( doubleValue ) + QuantityCalculate ) * QuantitySet ;
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

        var doubleValue = Quantity == Dash ? "0" : Quantity ;
        Total = ( double.Parse( doubleValue ) + QuantityCalculate ) * value ;
      }
    }

    private double _total ;

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

    private bool _allowInputQuantity ;

    public bool AllowInputQuantity
    {
      get => _allowInputQuantity ;
      set
      {
        _allowInputQuantity = value ;
        OnPropertyChanged( nameof( AllowInputQuantity ) ) ;
      }
    }

    private bool _allowChangeClassification ;

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
    public string Supplement { get ; set ; }

    public CeedDetailModel( string? productCode, string? productName, string? standard, string? classification, string? quantity, string? unit, string? parentId, string? trajectory, string? size1, string? size2, string? specification, int? order, string? type, string? parentPartModel, string? modeNumber, string? ceedCode, string? constructionClassification, double? quantityCalculate, double? quantitySet, double? total, string? description, bool? allowInputQuantity, string? supplement, bool isConduit = false )
    {
      IsConduit = isConduit ;
      ProductCode = productCode ?? string.Empty ;
      ProductName = productName ?? string.Empty ;
      Standard = standard ?? string.Empty ;
      Classification = classification ?? string.Empty ;
      Quantity = quantity ?? Dash ;
      Unit = unit ?? string.Empty ;
      ParentId = parentId ?? string.Empty ;
      Trajectory = trajectory ?? string.Empty ;
      Size1 = size1 ?? string.Empty ;
      Size2 = size2 ?? string.Empty ;
      Specification = specification ?? string.Empty ;
      Order = order ?? 1 ;
      Type = type ?? string.Empty ;
      ParentPartModel = parentPartModel ?? string.Empty ;
      ModeNumber = modeNumber ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      QuantityCalculate = quantityCalculate ?? 0 ;
      QuantitySet = quantitySet ?? 0 ;
      Total = total ?? 0 ;
      Description = description ?? string.Empty ;
      AllowInputQuantity = allowInputQuantity ?? false ;
      AllowChangeClassification = isConduit & constructionClassification is nameof( ConstructionClassificationType.天井ふところ ) or nameof( ConstructionClassificationType.ケーブルラック配線 ) or nameof( ConstructionClassificationType.二重床 ) ;
      Supplement = supplement ?? string.Empty ;
    }
  }

  public enum ClassificationType
  {
    露出,
    隠蔽
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