
    private TextNoteType CheckExistedOrCreateTextNoteArent(Document doc)
    {

      var list = new FilteredElementCollector(doc).WhereElementIsElementType().OfClass(typeof(TextNoteType));
      if (list.Select(x => x.Name).Contains(TextNoteArent.ArentTextNoteType))
      {
        var result =list.First(x=>x.Name == TextNoteArent.ArentTextNoteType) as TextNoteType;
        return result!;
      }
      using var tran = new Transaction(doc);
      tran.Start("Create Arent TextNote Type");
      var newTextNoteType = ((TextNoteType)list.First()).Duplicate(TextNoteArent.ArentTextNoteType) as TextNoteType;
      //newTextNoteType.get_Parameter("Text Font").Set("Arial Narrow");
      tran.Commit();
      return newTextNoteType!;
    }
    
    private XYZ GetMiddlePoint(DetailLine line)
    {
      var (x, y, _) = line.GeometryCurve.GetEndPoint(0);
      var (x1, y1, _) = line.GeometryCurve.GetEndPoint(1);
      
      return new XYZ((x + x1)/2,(y + y1)/2,0);
    }

    private IEnumerable<XYZ> GetListPointRef(BoundingBoxXYZ bb)
    {
      var minX = bb.Min.X;
      var minY = bb.Min.Y;
      var maxX = bb.Max.X;
      var maxY = bb.Max.Y;

      return new List<XYZ>()
      {
        new XYZ((minX + maxX)/2, minY, 0),
        new XYZ(maxX, (maxY+minY)/2, 0),
        new XYZ((minX + maxX)/2, maxY, 0),
        new XYZ(minX, (maxY+minY)/2, 0),
      };
    }

    private void CheckAndUpdateStorageLines(Document document)
    {
      var allText = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType().OfClass(typeof(TextNote)).Cast<TextNote>().ToList();
      var allLines = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType().OfClass(typeof(CurveElement)).Where(x=>x is DetailLine).Cast<DetailLine>().ToList();
      
      allText.ForEach(text =>
      {
        var refPoints = GetListPointRef(text.get_BoundingBox(document.ActiveView));
        var listLines = allLines.Where(line =>
        {
          return refPoints.Any(p => p.IsAlmostEqualTo(GetMiddlePoint(line)));
        }).Select(x=>x.Id).ToList();
        TextNoteArent.StorageLines[text.Id] = listLines;
      });
    }
  }
  
}