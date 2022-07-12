using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using Arent3d.Architecture.Routing.ExtensibleStorages ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Demo.TestStorageCommand", DefaultString = "Storable" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class TestStorageCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      using var trans = new Transaction( document ) ;
      trans.Start( "Set Data" ) ;
      
      var element = document.GetElement( selection.PickObject( ObjectType.Element ) ) ;
      var model = new ComplexModel
      {
        IntProperty = int.MaxValue,
        ShortProperty = short.MinValue,
        ByteProperty = 0,
        DoubleProperty = 8.23123456789,
        FloatProperty = 0.23345F,
        BoolProperty = true,
        StringProperty = "The quick brown fox jumps over the lazy dog",
        GuidProperty = new Guid( "DFCD07E5-7218-4052-8731-1F8B74ABFCF3" ),
        ElementIdProperty = new ElementId( 9872 ),
        XyzProperty = new XYZ( 10.01, 20.02, 30.03 ),
        UvProperty = new UV( 1001, 2222.333 ),
        DeepModelProperty = new DeepModel { Count = 789, ElementId = new ElementId( 7777 ) },
        IntArrayProperty = new Collection<int> { 1, 5, 8, 4, 37, 183403853, -243512, -4122345 },
        ShortArrayProperty = new List<short> { -23, 13456, 4236, 125, 752, 246, -234 },
        ByteArrayProperty = new BindingList<byte> { 0, 1, 2, 3, 255 },
        DoubleArrayProperty = new ObservableCollection<double> { -23.45, 34.56 },
        FloatArrayProperty = new List<float> { 99.8877665544332211F },
        BoolArrayProperty = new Collection<bool> { true, true, false, true, false, false },
        StringArrayProperty = new List<string> { "QWERTY", "ASDFGH", "ZxCvBN" },
        GuidArrayProperty = new List<Guid> { new( "9E7941F8-03EE-48AC-90B7-4352911F06F7" ), new ( "78304C8D-B904-47A2-BDF6-C52A6B569D86" ), new ( "8B1ADB16-4974-4820-A0E2-129F16620331" ) },
        ElementIdArrayProperty = new List<ElementId> { new( 1 ), new( 2 ), new( 3 ) },
        XyzArrayProperty = new Collection<XYZ> { XYZ.Zero, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ },
        UvArrayProperty = new BindingList<UV> { UV.Zero, UV.BasisU, UV.BasisV, },
        SubModelArray = new List<SubModel>
        {
          new()
          {
            ArrayField = new List<int> { 123, 456 },
            StringProperty = "Hello, world!",
            IntProperty = 99,
            DoubleProperty = 0.0000000056,
            DeepModels = new List<DeepModel> { new() { Count = 1, ElementId = new ElementId( 43 ) }, new() { Count = 589, ElementId = new ElementId( 55 ) } },
            DeepModel = new DeepModel { Count = 0, ElementId = ElementId.InvalidElementId }
          },
          new()
          {
            ArrayField = new List<int>() { 789, 101112 },
            StringProperty = "Hello, again!",
            IntProperty = 88,
            DoubleProperty = -0.0000000056,
            DeepModels = new List<DeepModel> { new() { Count = 100, ElementId = new ElementId( 555 ) }, new() { Count = 345, ElementId = new ElementId( 666 ) } },
            DeepModel = new DeepModel { Count = 12, ElementId = ElementId.InvalidElementId }
          }
        },
        BoolXyzMap = new Dictionary<bool, XYZ> { { true, new XYZ( 1, 2, 3 ) }, { false, new XYZ( -3, -2, -1 ) } },
        ByteGuidMap = new SortedDictionary<byte, Guid> { { 0, new Guid( "D2EF3FB3-0EF9-4F5A-BCBD-A1F84EA658B8" ) }, { 255, new Guid( "71DA88AA-6D47-4BF9-972A-DDB6F90BFAE0" ) }, { 124, new Guid( "1DDF733C-5AA1-4079-99E9-D621DBDFD928" ) } },
        ShortElementIdMap = new Dictionary<short, ElementId> { { -23, ElementId.InvalidElementId }, { 124, new ElementId( 245 ) }, { 156, new ElementId( 984534 ) }, { -145, new ElementId( 991233516 ) } },
        IntSubModelMap = new SortedDictionary<int, SubModel>
        {
          {
            -1, new SubModel
            {
              ArrayField = new List<int> { 234, 1112 },
              StringProperty = "Hello from map!",
              IntProperty = 33,
              DoubleProperty = -0.0000200056,
              DeepModels = new List<DeepModel> { new() { Count = 100, ElementId = new ElementId( 555 ) }, new() { Count = 345, ElementId = new ElementId( 666 ) } },
              DeepModel = new DeepModel { Count = 12, ElementId = ElementId.InvalidElementId }
            }
          },
          {
            775993884, new SubModel
            {
              ArrayField = new List<int> { 0, 123, 345564, -31243, 51454 },
              StringProperty = "Hello from map 2!",
              IntProperty = 33,
              DoubleProperty = -0.0000200056,
              DeepModels = new List<DeepModel> { new() { Count = 100, ElementId = new ElementId( 555 ) }, new() { Count = 345, ElementId = new ElementId( 666 ) } },
              DeepModel = new DeepModel { Count = 12, ElementId = ElementId.InvalidElementId }
            }
          }
        },
        ElementIdStringMap = new Dictionary<ElementId, string> { { new ElementId( BuiltInParameter.LEVEL_DATA_OWNING_LEVEL ), "LEVEL_DATA_OWNING_LEVEL" }, { ElementId.InvalidElementId, "Invalid" } },
        GuidDeepModelMap = new Dictionary<Guid, DeepModel> { { new Guid( "A85D94A3-162D-4611-BA9B-C268700ECDB1" ), new() { Count = 23, ElementId = new ElementId( 24 ) } } },
        StringDoubleMap = new SortedDictionary<string, double> { { "one point zero five", 0.05 }, { "one hundred and sixty six point one two three", 166.123 } }
      } ;
      element.SetEntity( model ) ;
      
      trans.Commit() ;

      var data = element.GetEntity<ComplexModel>() ;

      return Result.Succeeded ;
    }
  }

  [Schema( "685551F3-04D4-4A34-94CA-0C2E34B2A5BF", nameof( ComplexModel ), Documentation = "The class I want to save in the project" )]
  public class ComplexModel : IModelEntity
  {
    
    #region Simple properties

    [Field( Documentation = "Int32 Property" )]
    public int IntProperty { get ; set ; }

    [Field( Documentation = "Int16 Property" )]
    public short ShortProperty { get ; set ; }

    [Field( Documentation = "Byte Property" )]
    public byte ByteProperty { get ; set ; }

    [Field( Documentation = "Double Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public double DoubleProperty { get ; set ; }

    [Field( Documentation = "Float Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public float FloatProperty { get ; set ; }

    [Field( Documentation = "Boolean Property" )]
    public bool BoolProperty { get ; set ; }

    [Field( Documentation = "String Property" )]
    public string? StringProperty { get ; set ; }

    [Field( Documentation = "Guid Property" )]
    public Guid GuidProperty { get ; set ; }

    [Field( Documentation = "ElementId Property" )]
    public ElementId? ElementIdProperty { get ; set ; }

    [Field( Documentation = "XYZ Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public XYZ? XyzProperty { get ; set ; }

    [Field( Documentation = "UV Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public UV? UvProperty { get ; set ; }

    [Field]
    public DeepModel? DeepModelProperty { get ; set ; }

    #endregion

    #region ArrayProperties

    [Field( Documentation = "Int32 Collection Property" )]
    public Collection<int>? IntArrayProperty { get ; set ; }

    [Field( Documentation = "Int16 List Property" )]
    public List<short>? ShortArrayProperty { get ; set ; }

    [Field( Documentation = "BindingList of Byte Property" )]
    public BindingList<byte>? ByteArrayProperty { get ; set ; }

    [Field( Documentation = "ObservableCollection of Double Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public ObservableCollection<double>? DoubleArrayProperty { get ; set ; }

    [Field( Documentation = "Float List Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public List<float>? FloatArrayProperty { get ; set ; }

    [Field( Documentation = "Boolean List Property" )]
    public Collection<bool>? BoolArrayProperty { get ; set ; }

    [Field( Documentation = "String List Property" )]
    public List<string>? StringArrayProperty { get ; set ; }

    [Field( Documentation = "Guid List Property" )]
    public List<Guid>? GuidArrayProperty { get ; set ; }

    [Field( Documentation = "ElementId List Property" )]
    public List<ElementId>? ElementIdArrayProperty { get ; set ; }

    [Field( Documentation = "XYZ List Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public Collection<XYZ>? XyzArrayProperty { get ; set ; }

    [Field( Documentation = "UV BindingList Property", SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public BindingList<UV>? UvArrayProperty { get ; set ; }

    [Field]
    public List<SubModel>? SubModelArray { get ; set ; }

    #endregion


    #region Map properties

    [Field( SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public Dictionary<bool, XYZ>? BoolXyzMap { get ; set ; }

    [Field]
    public SortedDictionary<byte, Guid>? ByteGuidMap { get ; set ; }

    [Field]
    public Dictionary<short, ElementId>? ShortElementIdMap { get ; set ; }

    [Field]
    public SortedDictionary<int, SubModel>? IntSubModelMap { get ; set ; }

    [Field]
    public Dictionary<ElementId, string>? ElementIdStringMap { get ; set ; }

    [Field]
    public Dictionary<Guid, DeepModel>? GuidDeepModelMap { get ; set ; }

    [Field( SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public SortedDictionary<string, double>? StringDoubleMap { get ; set ; }

    #endregion
    
  }

  [Schema( "6C0EEADC-9B38-4CDF-A5C9-28296D37EE23", nameof( DeepModel ) )]
  public class DeepModel : IModelEntity
  {
    [Field]
    public ElementId? ElementId { get ; set ; }

    [Field]
    public int Count { get ; set ; }
  }

  [Schema( "1488C456-66B2-445F-817C-34C1A4DF4546", nameof( SubModel ) )]
  public class SubModel : IModelEntity
  {
    [Field( Documentation = "Field store some string property" )]
    public string? StringProperty { get ; set ; }

    [Field( Documentation = "Integer property" )]
    public int IntProperty { get ; set ; }

    [Field( SpecTypeId = SpecType.Length, UnitTypeId = UnitType.Millimeters )]
    public double DoubleProperty { get ; set ; }

    [Field]
    public DeepModel? DeepModel { get ; set ; }

    [Field]
    public List<int>? ArrayField { get ; set ; }

    [Field]
    public List<DeepModel>? DeepModels { get ; set ; }
  }
}