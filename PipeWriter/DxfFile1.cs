using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace PipeWriter
{
  public  class DxfFile1
    {
        /// <summary>
        /// Generates the dxf file according to passed product details.
        /// </summary>
        /// <param name="productDetails">Product Detail.</param>
        /// <param name="fileName">Dxf file name.</param>
        /// <param name="outputFilePath">Desired location where dxf file needs to be generated.</param>
        /// <returns>void</returns>
        /// <remarks>
        /// If the file with same name already exists it will be replaced.
        /// </remarks>
        public static void Generate(Product productDetails, string fileName, string outputFilePath)
      {
          var worngInputs = productDetails.PartList.Arcs.Where(x => x.Radius <= productDetails.ProductWidth/2).ToList();
          if (worngInputs.Count > 0)
          {
              //var errorFilePath = Path.Combine(outputFilePath, "Error.txt");
              if (!Directory.Exists(outputFilePath))
                  Directory.CreateDirectory(outputFilePath);

              foreach (var item in worngInputs)
              {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains an arc defination with radius less than or equal to half of the product width.",
                              "  ---  Sequence Num  ---  ", item.EntityID));
                  //File.AppendAllLines(errorFilePath,
                  //    new[]
                  //    {
                  //        string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                  //            "contains an arc defination with radius less than or equal to half of the product width.",
                  //            "  ---  Sequence Num  ---  ", item.EntityID)
                  //    });
              }
              return;
          }
          DxfDocument dxf = new DxfDocument();
            
          var orgiEntityCollection = GetEntityObjCollection(productDetails.PartList);
          var textStartPoint = GetTextStartPoint(orgiEntityCollection);
          //var sign = Math.Sign(textStartPoint.Y);
          textStartPoint.Y = (textStartPoint.Y - 4*productDetails.FontSizeDimension- productDetails.ProductWidth);

          var newCollection = new List<EntityObject>();
          var text1 = new Text(string.Concat(" Customer :  ", productDetails.Customer), textStartPoint,
              productDetails.FontSizeDimension);
          var text2 = new Text(string.Concat(" Order    :  ", productDetails.OrderText),
              new Vector2(textStartPoint.X, textStartPoint.Y - (productDetails.FontSizeDimension*1.67)),
              productDetails.FontSizeDimension);
          newCollection.Add(text1);
          newCollection.Add(text2);

          var i = 1;
          var dimStyle = new DimensionStyle("dimStyle")
          {
              TextHeight = productDetails.FontSizeDimension,
              ArrowSize = productDetails.FontSizeDimension,
              DimSuffix = " mm",
              TextOffset = productDetails.FontSizeDimension*0.4,
              DimLineColor = AciColor.Blue,
              ExtLineColor = AciColor.Yellow
          };
          foreach (var item in orgiEntityCollection)
          {
              var layer = new Layer(string.Concat("Layer", i));
              if (item.Type == EntityType.Line)
              {
                  var thisItem = item as Line;

                  var lwPolyLine = new LwPolyline
                  {
                      Vertexes =
                      {
                          new LwPolylineVertex(thisItem.StartPoint.X, thisItem.StartPoint.Y),
                          new LwPolylineVertex(thisItem.EndPoint.X, thisItem.EndPoint.Y)
                      }
                  };
                  var offset1 = lwPolyLine.GetOffsetCurves(productDetails.ProductWidth/2);
                  var offset2 = lwPolyLine.GetOffsetCurves(-productDetails.ProductWidth/2);
                  var endLine1 = new Line(
                      new Vector2(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y),
                      new Vector2(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y));
                  var endLine2 = new Line(
                      new Vector2(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y),
                      new Vector2(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y));

                  offset1.Layer = layer;
                  offset2.Layer = layer;
                  endLine1.Layer = layer;
                  endLine2.Layer = layer;
                  item.Layer = layer;

                  offset1.Color = AciColor.Blue;
                  offset2.Color = AciColor.Blue;
                  endLine1.Color = AciColor.Blue;
                  endLine2.Color = AciColor.Blue;
                  item.Color = AciColor.Blue;

                  newCollection.Add(offset1);
                  newCollection.Add(offset2);
                  newCollection.Add(endLine1);
                  newCollection.Add(endLine2);
                  newCollection.Add(item);
                  if (productDetails.ShowDimensionLines)
                  {
                      var dimension = new LinearDimension();

                      dimension.FirstReferencePoint = new Vector2(thisItem.StartPoint.X, thisItem.StartPoint.Y);
                      dimension.SecondReferencePoint = new Vector2(thisItem.EndPoint.X, thisItem.EndPoint.Y);
                      dimension.Offset = productDetails.ProductWidth*1.5;
                      var slope = (thisItem.EndPoint.Y - thisItem.StartPoint.Y)/
                                  (thisItem.EndPoint.X - thisItem.StartPoint.X);

                      dimension.Rotation = dimension.Rotation > 180
                          ? (360 - Math.Atan(slope)*180/Math.PI)
                          : Math.Atan(slope)*180/Math.PI;
                      dimension.Layer = layer;
                      dimension.Style = dimStyle;

                      if (i == 1)
                      {
                          var widthDimension = new LinearDimension();

                          widthDimension.FirstReferencePoint = new Vector2(endLine1.StartPoint.X, endLine1.StartPoint.Y);
                          widthDimension.SecondReferencePoint = new Vector2(endLine1.EndPoint.X, endLine1.EndPoint.Y);
                          widthDimension.Offset = productDetails.ProductWidth*0.75;
                          var slope1 = (endLine1.EndPoint.Y - endLine1.StartPoint.Y)/
                                       (endLine1.EndPoint.X - endLine1.StartPoint.X);

                          widthDimension.Rotation = widthDimension.Rotation > 180
                              ? (360 - Math.Atan(slope1)*180/Math.PI)
                              : Math.Atan(slope1)*180/Math.PI;
                          widthDimension.Layer = layer;
                          widthDimension.Style = dimStyle;
                          newCollection.Add(widthDimension);
                      }

                      newCollection.Add(dimension);
                  }

              }
              if (item.Type == EntityType.Arc)
              {
                  var thisItem = item as Arc;
                  var offset1 = new Arc
                  {
                      Linetype = Linetype.Continuous,
                      Center = new Vector3(thisItem.Center.X, thisItem.Center.Y, 0),
                      StartAngle = thisItem.StartAngle,
                      EndAngle = thisItem.EndAngle,
                      Radius = thisItem.Radius + productDetails.ProductWidth/2
                  };
                  var offset2 = new Arc
                  {
                      Linetype = Linetype.Continuous,
                      Center = new Vector3(thisItem.Center.X, thisItem.Center.Y, 0),
                      StartAngle = thisItem.StartAngle,
                      EndAngle = thisItem.EndAngle,
                      Radius = thisItem.Radius - productDetails.ProductWidth/2
                  };
                  var verticesOffset1 = offset1.GetVertexesOfArc();
                  var verticesOffset2 = offset2.GetVertexesOfArc();
                  var endLine1 = new Line(verticesOffset1[0], verticesOffset2[0]);
                  var endLine2 = new Line(verticesOffset1[1], verticesOffset2[1]);
                  offset1.Layer = layer;
                  offset2.Layer = layer;
                  endLine1.Layer = layer;
                  endLine2.Layer = layer;

                  item.Layer = layer;

                  offset1.Color = AciColor.Green;
                  offset2.Color = AciColor.Green;
                  endLine1.Color = AciColor.Green;
                  endLine2.Color = AciColor.Green;
                  item.Color = AciColor.Green;

                  newCollection.Add(offset1);
                  newCollection.Add(offset2);
                  newCollection.Add(endLine1);
                  newCollection.Add(endLine2);
                  newCollection.Add(item);


                  var startAngle = thisItem.StartAngle > thisItem.EndAngle
                      ? thisItem.StartAngle - 360
                      : thisItem.StartAngle;
                  if (productDetails.ShowDimensionLines)
                  {
                      var dimension = new RadialDimension(thisItem, (startAngle + thisItem.EndAngle)/2, 0);
                      dimension.Layer = layer;
                      dimension.Style = dimStyle;
                      if (i == 1)
                      {
                          var widthDimension = new LinearDimension();
                            
                          widthDimension.FirstReferencePoint = new Vector2(endLine2.StartPoint.X, endLine2.StartPoint.Y);
                          widthDimension.SecondReferencePoint = new Vector2(endLine2.EndPoint.X, endLine2.EndPoint.Y);
                          widthDimension.Offset = productDetails.ProductWidth*0.75;
                          var slope1 = (endLine2.EndPoint.Y - endLine2.StartPoint.Y)/
                                       (endLine2.EndPoint.X - endLine2.StartPoint.X);

                          widthDimension.Rotation = widthDimension.Rotation > 180
                              ? (360 - Math.Atan(slope1)*180/Math.PI)
                              : Math.Atan(slope1)*180/Math.PI;
                          widthDimension.Layer = layer;
                          widthDimension.Style = dimStyle;
                          newCollection.Add(widthDimension);
                      }

                      newCollection.Add(dimension);
                  }
              }
              i++;
          }
          dxf.AddEntity(newCollection);
          if (!Directory.Exists(outputFilePath))
          {
              Directory.CreateDirectory(outputFilePath);
          }
          dxf.Save(Path.Combine(outputFilePath, string.Concat(fileName, ".dxf")));
            
            
      }
        public void GenerateDXF(Product productDetails, string fileName, string outputFilePath)
        {
            var sequence = new List<int>();
            //validate, will return exception when validation failed.
            ValidateDxf(productDetails, fileName, outputFilePath, sequence);
            DxfDocument doc = new DxfDocument();

            //foreach(var _item in sequence)
            for(int i=0; i< sequence.Count; i++)
            {
                var docLineEntity = productDetails.PartList.Lines.FirstOrDefault(x => x.EntityID == sequence[i]);
                if(docLineEntity != null)
                {
                    var layer = new Layer("Layer" + sequence[i]);//defined a layer that will be added on dxfEntity object
                    if(i!= 0)
                    {
                        DrawLine(null, docLineEntity.Length, productDetails.ProductWidth); // this will draw straight line
                    }
                    else
                    {

                    }



                }
                var docArcEntity = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequence[i]);
                if(docArcEntity != null)
                {

                }



            }


            var lineItem = DrawLine(null, 100, 10);
            //doc.AddEntity(lineItem);
            drawLayerAndDimensions(doc, lineItem, 10);



            if (!Directory.Exists(outputFilePath))
            {
                Directory.CreateDirectory(outputFilePath);
            }
            doc.Save(Path.Combine(outputFilePath, string.Concat(fileName, ".dxf")));
            //doc.
            //List<EntityObject> collection = new List<EntityObject>();
            //doc.AddEntity(DrawLine(new Vector3(0, 0, 0), 100,20));

        }

        public void ValidateDxf(Product productDetails, string fileName, string outputFilePath, List<int> sequence)
        {
            
            
            foreach(var _productItem in productDetails.PartList.Lines.OrderBy(x=>x.EntityID))
            {
                if(sequence.Contains(_productItem.EntityID))
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains Multiple sequence number.",
                              "  ---  Sequence Num  ---  ", _productItem.EntityID));
                    return; 
                }
                sequence.Add(_productItem.EntityID);
            }
            foreach (var _productItem in productDetails.PartList.Lines.OrderBy(x=>x.EntityID))
            {
                if (sequence.Contains(_productItem.EntityID))
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains Multiple sequence number.",
                              "  ---  Sequence Num  ---  ", _productItem.EntityID));
                    return;
                }
                sequence.Add(_productItem.EntityID);
            }

            //validate wrong input
            var worngInputs = productDetails.PartList.Arcs.Where(x => x.Radius <= productDetails.ProductWidth / 2).ToList();
            if (worngInputs.Count > 0)
            {
                if (!Directory.Exists(outputFilePath))
                    Directory.CreateDirectory(outputFilePath);

                foreach (var item in worngInputs)
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains an arc defination with radius less than or equal to half of the product width.",
                              "  ---  Sequence Num  ---  ", item.EntityID));
                }
                return;
            }
        }
        private EntityObject DrawLine(EntityObject previousEntityObj, double length, double width)
        {
            //create main line object
            var lineObj = new Line();
            lineObj.Linetype = Linetype.Dashed;
            if (previousEntityObj == null)
            {
                lineObj.StartPoint = new Vector3(0, 0, 0);
                lineObj.EndPoint = new Vector3(length, 0, 0);
            }
            else if(previousEntityObj.Type == EntityType.Line)
            {
                var prevLineObj = previousEntityObj as Line;
                lineObj.StartPoint = prevLineObj.EndPoint;
                lineObj.EndPoint = Coordinate.CalculateLineEndPoint(prevLineObj.StartPoint, prevLineObj.EndPoint, length);
            }

            //if previous element is arc.
            //if(previousEntityObj.Type == EntityType.Arc)
            //{
            //    var prevArcObj = previousEntityObj as Arc;
            //    lineObj.StartPoint = prevArcObj.GetVertexesOfArc(prevArcObj.)
            //}


            

            //var layer = new Layer(string.Concat("Layer", i));


            return lineObj;
        }

        public void drawLayerAndDimensions(DxfDocument docDxf, EntityObject item, double itemWidth)
        {
            //dimension style
            var dimStyle = new DimensionStyle("dimStyle")
            {
                TextHeight = 3,
                ArrowSize = 3,
                DimSuffix = " mm",
                TextOffset = 3 * 0.4,
                DimLineColor = AciColor.Blue,
                ExtLineColor = AciColor.Yellow
            };

            //layer
            var layer = new Layer("layer test");

            if(item.Type == EntityType.Line)
            {
                var thisItem = item as Line;

                var lwPolyLine = new LwPolyline
                {
                    Vertexes =
                      {
                          new LwPolylineVertex(thisItem.StartPoint.X, thisItem.StartPoint.Y),
                          new LwPolylineVertex(thisItem.EndPoint.X, thisItem.EndPoint.Y)
                      }
                };
                var offset1 = lwPolyLine.GetOffsetCurves(itemWidth / 2);
                var offset2 = lwPolyLine.GetOffsetCurves(-itemWidth / 2);
                var endLine1 = new Line(
                    new Vector2(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y),
                    new Vector2(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y));
                var endLine2 = new Line(
                    new Vector2(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y),
                    new Vector2(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y));

                offset1.Layer = layer;
                offset2.Layer = layer;
                endLine1.Layer = layer;
                endLine2.Layer = layer;
                item.Layer = layer;

                offset1.Color = AciColor.Blue;
                offset2.Color = AciColor.Blue;
                endLine1.Color = AciColor.Blue;
                endLine2.Color = AciColor.Blue;
                item.Color = AciColor.Blue;

                docDxf.AddEntity(offset1);
                docDxf.AddEntity(offset2);
                docDxf.AddEntity(endLine1);
                docDxf.AddEntity(endLine2);
                docDxf.AddEntity(item);
                //if (productDetails.ShowDimensionLines)
                if(true)
                {
                    var dimension = new LinearDimension();

                    dimension.FirstReferencePoint = new Vector2(thisItem.StartPoint.X, thisItem.StartPoint.Y+ (itemWidth/2));
                    dimension.SecondReferencePoint = new Vector2(thisItem.EndPoint.X, thisItem.EndPoint.Y + (itemWidth/2));
                    dimension.Offset = itemWidth * 1.5;
                    var slope = (thisItem.EndPoint.Y - thisItem.StartPoint.Y) /
                                (thisItem.EndPoint.X - thisItem.StartPoint.X);

                    dimension.Rotation = dimension.Rotation > 180
                        ? (360 - Math.Atan(slope) * 180 / Math.PI)
                        : Math.Atan(slope) * 180 / Math.PI;
                    dimension.Layer = layer;
                    dimension.Style = dimStyle;

                    //if (i == 1)
                    if(true)
                    {
                        var widthDimension = new LinearDimension();

                        widthDimension.FirstReferencePoint = new Vector2(endLine1.StartPoint.X, endLine1.StartPoint.Y);
                        widthDimension.SecondReferencePoint = new Vector2(endLine1.EndPoint.X, endLine1.EndPoint.Y);
                        widthDimension.Offset = itemWidth * 0.75;
                        var slope1 = (endLine1.EndPoint.Y - endLine1.StartPoint.Y) /
                                     (endLine1.EndPoint.X - endLine1.StartPoint.X);

                        widthDimension.Rotation = widthDimension.Rotation > 180
                            ? (360 - Math.Atan(slope1) * 180 / Math.PI)
                            : Math.Atan(slope1) * 180 / Math.PI;
                        widthDimension.Layer = layer;
                        widthDimension.Style = dimStyle;
                        docDxf.AddEntity(widthDimension);
                    }

                     docDxf.AddEntity(dimension);
                }
            }

        }
        private Arc DrawArc(Vector2 startPoint, Vector2 endpoint, int radius)
        {
            var arcObj = new Arc();
            

            return arcObj;

        }
        private Arc DrawArc(Vector3 startPoint, Vector3 endpoint, int radius)
        {
            var arcObj = new Arc();
           
            return arcObj;
        }
        

        

        /// <summary>
        /// Converts dxf file to different file format like svg, png, pdf, jpeg, etc.
        /// </summary>
        /// <param name="programPath">Location where AcmeCadConverter.exe exits. For eg: "C:\Program Files (x86)\Acme CAD Converter\AcmeCADConverter.exe"</param>
        /// <param name="dxfFilePath">Location where dxf file exits. For eg : "E:\AutoCad\PipeWriter\bin\Debug\DxfFiles\MyProduct.dxf"</param>
        /// <param name="outPutFileFormat">Desired file format.</param>
        /// <returns>void</returns>
        /// <remarks>
        /// If the file with same name and same format already exists it will be replaced.
        /// </remarks>
        public static void Convert(string programPath, string dxfFilePath, FileFormat outPutFileFormat)
      {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            process.StartInfo.FileName = programPath;
            process.StartInfo.Arguments = @"/r /p 3 /ad /b 7 /a -2 /f "+(int)outPutFileFormat+" "+dxfFilePath+"";

            process.Start();

            process.WaitForExit();
        }

      private static List<EntityObject> GetEntityObjCollection(Parts partList)
        {
            var orgiEntityCollection = new List<EntityObject>();
            bool continueLoop = true;
            int i = 1;
            while (continueLoop)
            {
                var line = partList.Lines.FirstOrDefault(x => x.EntityID == i);
                if (line != null)
                {
                    if (i == 1)
                    {
                        orgiEntityCollection.Add(new Line
                        {
                            Linetype = Linetype.Center,
                            StartPoint = new Vector3(0, 0, 0),
                            EndPoint = new Vector3(line.Length, 0, 0)
                        });
                    }
                    else
                    {
                        var preElement = orgiEntityCollection[i - 2];

                        // if the preceeding element is Arc
                        if (preElement.Type == EntityType.Arc)
                        {
                            var preElementArc = preElement as Arc;
                            var preArc = partList.Arcs.FirstOrDefault(x => x.EntityID == i - 1);
                            var startPoint = preElementArc.GetVertexesOfArc(preArc.IsClockwise)[1];

                            var endPoint = Coordinate.CalculateLineEndPoint(preElementArc.Center, startPoint,
                                line.Length, preArc.IsClockwise);

                            orgiEntityCollection.Add(new Line
                            {
                                Linetype = Linetype.Center,
                                StartPoint = startPoint,
                                EndPoint = endPoint
                            });
                        }
                        // if the preceeding element is line
                        else
                        {
                            var preElementLine = preElement as Line;
                            var startPoint = preElementLine.EndPoint;
                            var endPoint = Coordinate.CalculateLineEndPoint(preElementLine.StartPoint, preElementLine.EndPoint, line.Length);

                            orgiEntityCollection.Add(new Line
                            {
                                Linetype = Linetype.Center,
                                StartPoint = startPoint,
                                EndPoint = endPoint
                            });
                        }


                    }
                }
                else
                {
                    var arc = partList.Arcs.FirstOrDefault(x => x.EntityID == i);
                    continueLoop = arc != null;
                    if (continueLoop)
                    {
                        if (i == 1)
                        {
                            var center = Coordinate.CalculateArcCenter(new Vector3(-1, 0, 0),
                                new Vector3(0, 0, 0), arc.Radius, arc.IsClockwise);
                            var angles = Coordinate.GetArcAngles(new Vector3(0, 0, 0), center, arc.Radius,
                                arc.Angle, arc.IsClockwise);
                            orgiEntityCollection.Add(new Arc
                            {
                                Linetype = Linetype.Center,
                                Center = center,
                                StartAngle = angles[0],
                                EndAngle = angles[1],
                                Radius = arc.Radius,
                            });
                            
                        }
                        else
                        {
                            var preElement = orgiEntityCollection[i - 2];

                            if (preElement.Type == EntityType.Line)
                            {
                                var preElementLine = preElement as Line;
                                var center = Coordinate.CalculateArcCenter(preElementLine.StartPoint,
                                    preElementLine.EndPoint, arc.Radius, arc.IsClockwise);
                                var angles = Coordinate.GetArcAngles(preElementLine.EndPoint, center, arc.Radius,
                                    arc.Angle, arc.IsClockwise);

                                orgiEntityCollection.Add(new Arc
                                {
                                    Linetype = Linetype.Center,
                                    Center = center,
                                    StartAngle = angles[0],
                                    EndAngle = angles[1],
                                    Radius = arc.Radius,
                                });
                            }
                            else
                            {
                                var preElementArc = preElement as Arc;
                                var preArc = partList.Arcs.FirstOrDefault(x => x.EntityID == i - 1);
                                Vector3 contactPoint;
                                var center = Coordinate.CalculateArcCenter(preElementArc, preArc.IsClockwise, arc.Radius,
                                    arc.IsClockwise, out contactPoint);
                                var angles = Coordinate.GetArcAngles(contactPoint, center, arc.Radius,
                                    arc.Angle, arc.IsClockwise);

                                orgiEntityCollection.Add(new Arc
                                {
                                    Linetype = Linetype.Center,
                                    Center = center,
                                    StartAngle = angles[0],
                                    EndAngle = angles[1],
                                    Radius = arc.Radius,
                                });
                            }

                        }
                    }
                }
                i++;
            }
            return orgiEntityCollection;

        }

     private static Vector2 GetTextStartPoint(List<EntityObject> collection)
      {
            var result = new Vector2(0,0);
          foreach (var item in collection)
          {
               
              if (item.Type == EntityType.Line)
              {
                    var thisItem = item as Line;
                  result.X = thisItem.StartPoint.X < result.X ? thisItem.StartPoint.X : result.X;
                    result.X = thisItem.EndPoint.X < result.X ? thisItem.EndPoint.X : result.X;
                    result.Y = thisItem.StartPoint.Y < result.Y ? thisItem.StartPoint.Y : result.Y;
                    result.Y = thisItem.EndPoint.Y < result.Y ? thisItem.EndPoint.Y : result.Y;
                }
              else
              {
                    var thisItem = item as Arc;
                  result.X = (thisItem.Center.X - thisItem.Radius) < result.X
                      ? (thisItem.Center.X - thisItem.Radius)
                      : result.X;
                    result.Y = (thisItem.Center.Y - thisItem.Radius) < result.Y
                      ? (thisItem.Center.Y - thisItem.Radius)
                      : result.Y;
                }
          }
          return result;
      }


    }

   
}
