using System ;
using System.Collections.Generic ;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.IO ;
using System.Linq ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    [Schema("A9038CB7-D1F9-49DD-9CEE-9E306F71F358", nameof(CeedModel))]
    public class CeedModel : IDataModel
    {
        [Field(Documentation = "Legend Display")]
        public string LegendDisplay { get ; set ; }
        
        [Field(Documentation = "Ceed Model Number")]
        public string CeedModelNumber { get ; set ; }
        
        [Field(Documentation = "Ceed Set Code")]
        public string CeedSetCode { get ; set ; }
        
        [Field(Documentation = "General Display Device Symbol")]
        public string GeneralDisplayDeviceSymbol { get ; set ; }
        
        [Field(Documentation = "Model Number")]
        public string ModelNumber { get ; set ; }
        
        [Field(Documentation = "Floor Plan Symbol")]
        public string FloorPlanSymbol { get ; set ; }
        
        [Field(Documentation = "Instrumentation Symbol")]
        public string InstrumentationSymbol { get ; set ; }
        
        [Field(Documentation = "Name")]
        public string Name { get ; set ; }
        public string Condition { get ; set ; }
        
        [Field(Documentation = "Floor Plan Type")]
        public string FloorPlanType { get ; set ; }
        
        [Field(Documentation = "Base64 Instrumentation Image String")]
        public string Base64InstrumentationImageString { get ; set ; }
        
        [Field(Documentation = "Base64 Floor Plan Images")]
        public string Base64FloorPlanImages { get ; set ; }
        public BitmapImage? FloorPlanImages { get ; set ; }
        public List<BitmapImage?>? InstrumentationImages { get ; set ; }

        [Field(Documentation = "Is Added")]
        public bool IsAdded { get ; set ; }
        
        [Field(Documentation = "Is Edit Floor Plan")]
        public bool IsEditFloorPlan { get ; set ; }
        
        [Field(Documentation = "Is Edit Instrumentation")]
        public bool IsEditInstrumentation { get ; set ; }
        
        [Field(Documentation = "Is Edit Condition")]
        public bool IsEditCondition { get ; set ; }
        
        

        public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, 
            string name, string base64InstrumentationImageString, string base64FloorPlanImages, string floorPlanType )
        {
            const string dummySymbol = "Dummy" ;
            LegendDisplay = legendDisplay ;
            CeedModelNumber = ceedModelNumber ;
            CeedSetCode = ceedSetCode ;
            GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
            ModelNumber = modelNumber ;
            FloorPlanSymbol = floorPlanSymbol ;
            InstrumentationSymbol = instrumentationSymbol ;
            Name = name ;
            Condition = GetCondition( ceedModelNumber ) ;
            FloorPlanType = floorPlanType ;
            Base64InstrumentationImageString = base64InstrumentationImageString ;
            Base64FloorPlanImages = base64FloorPlanImages ;
            InstrumentationImages = null ;
            FloorPlanImages = null ;
            if ( floorPlanSymbol != dummySymbol ) {
                var temporaryFloorPlanImage = new BitmapImage() ;
                if ( FloorPlanImages == null && ! string.IsNullOrEmpty( Base64FloorPlanImages ) ) {
                    temporaryFloorPlanImage = BitmapToImageSource( Base64StringToBitmap( Base64FloorPlanImages ) ) ;
                }

                FloorPlanImages = temporaryFloorPlanImage ;
            }

            if ( InstrumentationImages != null || string.IsNullOrEmpty( Base64InstrumentationImageString ) ) return ;
            var listBimapImage = ( from image in Base64InstrumentationImageString.Split( new[] { "||" }, StringSplitOptions.None ) select Base64StringToBitmap( image ) into bmpFromString select BitmapToImageSource( bmpFromString ) ).ToList() ;
            InstrumentationImages = listBimapImage ;
        }

        public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Image>? floorPlanImages, List<Image>? instrumentationImages, string floorPlanSymbol,
            string instrumentationSymbol, string name, string base64InstrumentationImageString, string floorPlanType )
        {
            CeedModelNumber = ceedModelNumber ;
            LegendDisplay = legendDisplay ;
            CeedSetCode = ceedSetCode ;
            GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
            ModelNumber = modelNumber ;
            FloorPlanSymbol = floorPlanSymbol ;
            InstrumentationSymbol = instrumentationSymbol ;
            Name = name ;
            Condition = GetCondition( ceedModelNumber ) ;
            FloorPlanType = floorPlanType ;
            FloorPlanImages = BitmapToImageSource( GetImage( floorPlanImages ) ) ;
            InstrumentationImages = GetImages( instrumentationImages ) ;
            Base64InstrumentationImageString = base64InstrumentationImageString ;
            var tempFloorPlanString = string.Empty ;
            if ( FloorPlanImages != null ) {
                tempFloorPlanString = ConvertBitmapToBase64( FloorPlanImages ) ;
            }

            Base64FloorPlanImages = tempFloorPlanString ;
            if ( InstrumentationImages == null || ! InstrumentationImages.Any() ) return ;
            var tempImage = ( from item in InstrumentationImages select ConvertBitmapToBase64( item ) ).ToList() ;
            Base64InstrumentationImageString = string.Join( "||", tempImage ) ;
        }

        public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, List<Image>? floorPlanImages, List<Image>? instrumentationImages, string floorPlanSymbol,
            string instrumentationSymbol, string name, string floorPlanType )
        {
            LegendDisplay = legendDisplay ;
            CeedModelNumber = ceedModelNumber ;
            CeedSetCode = ceedSetCode ;
            GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
            ModelNumber = modelNumber ;
            FloorPlanSymbol = floorPlanSymbol ;
            InstrumentationSymbol = instrumentationSymbol ;
            Name = name ;
            Condition = GetCondition( ceedModelNumber ) ;
            FloorPlanType = floorPlanType ;
            FloorPlanImages = BitmapToImageSource( GetImage( floorPlanImages ) ) ;
            InstrumentationImages = GetImages( instrumentationImages ) ;
            Base64InstrumentationImageString = string.Empty ;
            var tempFloorPlanString = string.Empty ;
            if ( FloorPlanImages != null ) {
                tempFloorPlanString = ConvertBitmapToBase64( FloorPlanImages ) ;
                FloorPlanImages = null ;
            }

            Base64FloorPlanImages = tempFloorPlanString ;
            if ( InstrumentationImages == null || ! InstrumentationImages.Any() ) return ;
            var tempImage = ( from item in InstrumentationImages select ConvertBitmapToBase64( item ) ).ToList() ;
            Base64InstrumentationImageString = string.Join( "||", tempImage ) ;
        }

        public CeedModel( string legendDisplay, string ceedModelNumber, string ceedSetCode, string generalDisplayDeviceSymbol, string modelNumber, string floorPlanSymbol, string instrumentationSymbol, string name,
            string base64InstrumentationImageString, string base64FloorPlanImages, string floorPlanType, bool? isAdded, bool? isEditFloorPlan, bool? isEditInstrumentation, bool? isEditCondition )
        {
            const string dummySymbol = "Dummy" ;
            LegendDisplay = legendDisplay ;
            CeedModelNumber = ceedModelNumber ;
            CeedSetCode = ceedSetCode ;
            GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ;
            ModelNumber = modelNumber ;
            FloorPlanSymbol = floorPlanSymbol ;
            InstrumentationSymbol = instrumentationSymbol ;
            Name = name ;
            Condition = GetCondition( ceedModelNumber ) ;
            FloorPlanType = floorPlanType ;
            Base64InstrumentationImageString = base64InstrumentationImageString ;
            Base64FloorPlanImages = base64FloorPlanImages ;
            InstrumentationImages = null ;
            FloorPlanImages = null ;
            IsAdded = isAdded ?? false ;
            IsEditFloorPlan = isEditFloorPlan ?? false ;
            IsEditInstrumentation = isEditInstrumentation ?? false ;
            IsEditCondition = isEditCondition ?? false ;
            if ( floorPlanSymbol != dummySymbol ) {
                var temporaryFloorPlanImage = new BitmapImage() ;
                if ( FloorPlanImages == null && ! string.IsNullOrEmpty( Base64FloorPlanImages ) ) {
                    temporaryFloorPlanImage = BitmapToImageSource( Base64StringToBitmap( Base64FloorPlanImages ) ) ;
                }

                FloorPlanImages = temporaryFloorPlanImage ;
            }

            if ( InstrumentationImages != null || string.IsNullOrEmpty( Base64InstrumentationImageString ) ) return ;
            var listBimapImage = ( from image in Base64InstrumentationImageString.Split( new[] { "||" }, StringSplitOptions.None ) select Base64StringToBitmap( image ) into bmpFromString select BitmapToImageSource( bmpFromString ) ).ToList() ;
            InstrumentationImages = listBimapImage ;
        }

        private static BitmapImage? BitmapToImageSource( Bitmap? bitmap )
        {
            if ( null == bitmap )
                return null ;
            
            using var memory = new MemoryStream() ;
            bitmap.Save( memory, ImageFormat.Bmp ) ;

            memory.Position = 0 ;
            var bitmapimage = new BitmapImage() ;
            bitmapimage.BeginInit() ;
            bitmapimage.StreamSource = memory ;
            bitmapimage.CacheOption = BitmapCacheOption.OnLoad ;
            bitmapimage.EndInit() ;
            return bitmapimage ;
        }

        private static List<BitmapImage?> GetImages( List<Image>? images )
        {
            var listImages = new List<BitmapImage?>() ;
            try {
                if ( images != null ) 
                    listImages.AddRange( from image in images select BitmapToImageSource( (Bitmap) image ) ) ;
            }
            catch ( Exception e ) {
                Console.WriteLine( e ) ;
            }

            return listImages ;
        }

        private static Bitmap? GetImage( List<Image>? symbolImages )
        {
            try {
                if ( symbolImages is { Count: 1 } ) return (Bitmap) symbolImages[ 0 ] ;
                return MergeImages( symbolImages ) ;
            }
            catch ( Exception e ) {
                Console.WriteLine( e ) ;
                return null ;
            }
        }

        private static Bitmap MergeImages( List<Image>? images )
        {
            try {
                if ( images != null ) {
                    var maxImageHeight = images.OrderByDescending( c => c.Height ).Select( c => c.Height ).First() ;
                    const int padding = 45 ;
                    var imageWidth = images.Sum( item => item.Width ) + ( images.Count - 1 ) * padding ;
                    var finalImage = new Bitmap( imageWidth, maxImageHeight, PixelFormat.Format32bppArgb ) ;
                    using var g = Graphics.FromImage( finalImage ) ;
                    g.Clear( Color.White ) ;
                    var offset = 0 ;

                    foreach ( var image in images ) {
                        g.DrawImage( image, new Rectangle( new Point( offset, 0 ), image.Size ), new Rectangle( new Point(), image.Size ), GraphicsUnit.Pixel ) ;
                        offset += image.Width + padding ;
                    }

                    return finalImage ;
                }
            }
            catch ( Exception e ) {
                Console.WriteLine( e ) ;
                throw ;
            }

            return new Bitmap( 1, 1 ) ;
        }

        private static string ConvertBitmapToBase64( BitmapImage? bmp )
        {
            var bImage = BitmapImageBitmap( bmp ) ;
            var ms = new MemoryStream() ;
            bImage.Save( ms, ImageFormat.Bmp ) ;
            var byteImage = ms.ToArray() ;
            var result = Convert.ToBase64String( byteImage ) ;
            return result ;
        }

        private static Bitmap BitmapImageBitmap( BitmapImage? bitmapImage )
        {
            using var outStream = new MemoryStream() ;
            BitmapEncoder enc = new BmpBitmapEncoder() ;
            if ( bitmapImage != null ) enc.Frames.Add( BitmapFrame.Create( bitmapImage ) ) ;
            enc.Save( outStream ) ;
            var bitmap = new Bitmap( outStream ) ;

            return new Bitmap( bitmap ) ;
        }

        private static Bitmap Base64StringToBitmap( string base64String )
        {
            var bitmapData = Convert.FromBase64String( FixBase64ForImage( base64String ) ) ;
            var streamBitmap = new MemoryStream( bitmapData ) ;
            var bitImage = new Bitmap( (Bitmap) Image.FromStream( streamBitmap ) ) ;
            return bitImage ;
        }

        private static string FixBase64ForImage( string image )
        {
            var sbText = new System.Text.StringBuilder( image, image.Length ) ;
            sbText.Replace( "\r\n", string.Empty ) ;
            sbText.Replace( " ", string.Empty ) ;
            return sbText.ToString() ;
        }

        private static string GetCondition( string ceedModelNumber )
        {
            string condition ;
            if ( ceedModelNumber.EndsWith( "P" ) ) {
                condition = "隠蔽 、床隠蔽" ;
            }
            else if ( ceedModelNumber.EndsWith( "K" ) ) {
                condition = "コロガシ" ;
            }
            else if ( ceedModelNumber.EndsWith( "F" ) ) {
                condition = "フリアク" ;
            }
            else if ( ceedModelNumber.EndsWith( "E" ) ) {
                condition = "露出" ;
            }
            else if ( ceedModelNumber.EndsWith( "G" ) ) {
                condition = "屋外" ;
            }
            else if ( ceedModelNumber.EndsWith( "M" ) ) {
                condition = "モール" ;
            }
            else if ( ceedModelNumber.EndsWith( "U" ) ) {
                condition = "地中埋設" ;
            }
            else {
                condition = string.Empty ;
            }

            return condition ;
        }
    }
}