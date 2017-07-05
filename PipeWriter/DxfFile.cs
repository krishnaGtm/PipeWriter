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
    public class DxfFile
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
            var worngInputs = productDetails.PartList.Arcs.Where(x => x.Radius <= productDetails.ProductWidth / 2).ToList();
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
            double width;
            double height;
            var textStartPoint = GetTextStartPoint(orgiEntityCollection,out height,out width);
            //var textStartPoint = GetTextStartPoint(orgiEntityCollection);
            //var sign = Math.Sign(textStartPoint.Y);
            textStartPoint.Y = (textStartPoint.Y - 4 * productDetails.FontSizeDimension - productDetails.ProductWidth);

            var newCollection = new List<EntityObject>();
            var text1 = new Text(string.Concat(" Customer :  ", productDetails.Customer), textStartPoint,
                productDetails.FontSizeDimension);
            var text2 = new Text(string.Concat(" Order    :  ", productDetails.OrderText),
                new Vector2(textStartPoint.X, textStartPoint.Y - (productDetails.FontSizeDimension * 1.67)),
                productDetails.FontSizeDimension);
            newCollection.Add(text1);
            newCollection.Add(text2);

            var i = 1;
            var dimStyle = new DimensionStyle("dimStyle")
            {
                TextHeight = productDetails.FontSizeDimension,
                ArrowSize = productDetails.FontSizeDimension,
                DimSuffix = " mm",
                TextOffset = productDetails.FontSizeDimension * 0.4,
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
                    var offset1 = lwPolyLine.GetOffsetCurves(productDetails.ProductWidth / 2);
                    var offset2 = lwPolyLine.GetOffsetCurves(-productDetails.ProductWidth / 2);
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
                    //if(thisItem.s)
                    {
                        var dimension = new LinearDimension();

                        dimension.FirstReferencePoint = new Vector2(thisItem.StartPoint.X, thisItem.StartPoint.Y);
                        dimension.SecondReferencePoint = new Vector2(thisItem.EndPoint.X, thisItem.EndPoint.Y);
                        dimension.Offset = productDetails.ProductWidth * 1.5;
                        var slope = (thisItem.EndPoint.Y - thisItem.StartPoint.Y) /
                                    (thisItem.EndPoint.X - thisItem.StartPoint.X);

                        dimension.Rotation = dimension.Rotation > 180
                            ? (360 - Math.Atan(slope) * 180 / Math.PI)
                            : Math.Atan(slope) * 180 / Math.PI;
                        dimension.Layer = layer;
                        dimension.Style = dimStyle;

                        if (i == 1)
                        {
                            var widthDimension = new LinearDimension();

                            widthDimension.FirstReferencePoint = new Vector2(endLine1.StartPoint.X, endLine1.StartPoint.Y);
                            widthDimension.SecondReferencePoint = new Vector2(endLine1.EndPoint.X, endLine1.EndPoint.Y);
                            widthDimension.Offset = productDetails.ProductWidth * 0.75;
                            var slope1 = (endLine1.EndPoint.Y - endLine1.StartPoint.Y) /
                                         (endLine1.EndPoint.X - endLine1.StartPoint.X);

                            widthDimension.Rotation = widthDimension.Rotation > 180
                                ? (360 - Math.Atan(slope1) * 180 / Math.PI)
                                : Math.Atan(slope1) * 180 / Math.PI;
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
                        Radius = thisItem.Radius + productDetails.ProductWidth / 2
                    };
                    var offset2 = new Arc
                    {
                        Linetype = Linetype.Continuous,
                        Center = new Vector3(thisItem.Center.X, thisItem.Center.Y, 0),
                        StartAngle = thisItem.StartAngle,
                        EndAngle = thisItem.EndAngle,
                        Radius = thisItem.Radius - productDetails.ProductWidth / 2
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
                        var dimension = new RadialDimension(thisItem, (startAngle + thisItem.EndAngle) / 2, 0);
                        dimension.Layer = layer;
                        dimension.Style = dimStyle;
                        if (i == 1)
                        {
                            var widthDimension = new LinearDimension();

                            widthDimension.FirstReferencePoint = new Vector2(endLine2.StartPoint.X, endLine2.StartPoint.Y);
                            widthDimension.SecondReferencePoint = new Vector2(endLine2.EndPoint.X, endLine2.EndPoint.Y);
                            widthDimension.Offset = productDetails.ProductWidth * 0.75;
                            var slope1 = (endLine2.EndPoint.Y - endLine2.StartPoint.Y) /
                                         (endLine2.EndPoint.X - endLine2.StartPoint.X);

                            widthDimension.Rotation = widthDimension.Rotation > 180
                                ? (360 - Math.Atan(slope1) * 180 / Math.PI)
                                : Math.Atan(slope1) * 180 / Math.PI;
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
            //call validation
            if (Validate(productDetails, fileName))
            {
                //arrance items according to sequence number. sequence number must be unique
                var sequenceItens = GetItemsSequence(productDetails, fileName);
                if (sequenceItens.Any())
                {
                    sequenceItens = sequenceItens.OrderBy(x => x).ToList();
                    DxfDocument dxf = new DxfDocument();
                    //create directory if not exists
                    if (!Directory.Exists(outputFilePath))
                        Directory.CreateDirectory(outputFilePath);

                    //define dimenstion value that can be assigned if dimension is defined for individual entityitems
                    var dimStyle = new DimensionStyle("dimStyle")
                    {
                        TextHeight = 3,
                        ArrowSize = 3,
                        DimSuffix = " mm",
                        TextOffset = 3 * 0.4,
                        DimLineColor = AciColor.Blue,
                        ExtLineColor = AciColor.Yellow
                    };

                    //define prevObject as null for first time.
                    var entityObjectCollection = new List<EntityObject>();
                    EntityObject previousObject = null;
                    for (int i = 0; i < sequenceItens.Count; i++)
                    {
                        var layer = AddLayer("Layer" + i);
                        var docLineEntity = productDetails.PartList.Lines.FirstOrDefault(x => x.EntityID == sequenceItens[i]);
                        if (docLineEntity != null)
                        {
                            var line = new Line();
                            var isClockWise = false;
                            if(previousObject != null )
                                if(previousObject.Type == EntityType.Arc)
                                    isClockWise = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItens[i-1]).IsClockwise;
                            line = DrawAndAttachLine(previousObject, isClockWise, docLineEntity.Length, productDetails.ProductWidth, layer);
                            //if(productDetails.startangle != 0 && i==0)
                            //    AddStartOrEnd(productDetails.startangle);
                            bool showWidth = false;
                            if (i == 0)
                                showWidth = true;
                            var cutAngle = showWidth == true ? 60 : 0;
                            //var cutAngle = 0;
                            var items = Create2DPipeLine(line, layer, productDetails.ProductWidth,showWidth,dimStyle, cutAngle,true,docLineEntity.Length);

                            //show dimensionValue 
                            var linearDimension = ShowLength(line, layer, productDetails.ProductWidth * 1.5, dimStyle);

                            //assign previous object
                            previousObject = line;

                            //add entity items to dxf file.
                            dxf.AddEntity(items);
                            dxf.AddEntity(line);
                            dxf.AddEntity(linearDimension);

                            entityObjectCollection.Add(line);
                        }
                        var docArcEntity = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItens[i]);
                        if (docArcEntity != null)
                        {
                            var arc = new Arc();
                            var isClockWise = false;
                            if (previousObject != null)
                                if(previousObject.Type == EntityType.Arc)
                                    isClockWise = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItens[i - 1]).IsClockwise;
                            arc = DrawAndAttachArc(previousObject,isClockWise, layer, docArcEntity.Angle, docArcEntity.Radius, docArcEntity.IsClockwise);
                            //if (productDetails.startangle != 0 && i == 0)
                            //    AddStartOrEnd(productDetails.startangle);
                            bool showWidth = false;
                            if (i == 0)
                                showWidth = true;
                            var cutangle = showWidth == true ? 60 : 0;
                            var items = Create2DPipeArc(arc, layer, previousObject, productDetails.ProductWidth, showWidth,dimStyle,isClockWise, cutangle, true);

                            //show dimensionValue
                            var radialDimension = ShowRadius(arc, layer, productDetails.ProductWidth * 0.75, dimStyle);

                            //assign previous object
                            previousObject = arc;

                            //add entity items to dxf file.
                            dxf.AddEntity(items);
                            dxf.AddEntity(arc);
                            //if(radialDimension.Measurement > 500)
                            //{
                            //    radialDimension.IsVisible = false;
                            //    dxf.AddEntity(radialDimension);
                            //}

                            dxf.AddEntity(radialDimension);
                            entityObjectCollection.Add(arc);
                        }

                    }

                    //drawScale(entityObjectCollection);
                    //draw customer label
                    var textCollection = AddText(entityObjectCollection,productDetails);
                    dxf.AddEntity(textCollection);
                    

                    dxf.Save(Path.Combine(outputFilePath, string.Concat(fileName, ".dxf")));
                }
            }





        }

        private void drawScale(List<EntityObject> entityObjectCollection)
        {
            //throw new NotImplementedException();
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
            process.StartInfo.Arguments = @"/r /p 3 /ad /b 7 /a -2 /f " + (int)outPutFileFormat + " " + dxfFilePath + "";

            process.Start();

            process.WaitForExit();
        }

        public List<EntityObject> AddText(List<EntityObject> list, Product productDetails)
        {
            List<EntityObject> textList = new List<EntityObject>();
            double height;
            double width;
            var textStartPoint = GetTextStartPoint(list,out height,out width);
            //var textStartPoint = GetTextStartPoint(list);
            //var sign = Math.Sign(textStartPoint.Y);
            textStartPoint.Y = (textStartPoint.Y - 4 * productDetails.FontSizeDimension - productDetails.ProductWidth);


            var text1 = new Text(string.Concat(" Customer :  ", productDetails.Customer), textStartPoint,
                productDetails.FontSizeDimension);
            var text2 = new Text(string.Concat(" Order    :  ", productDetails.OrderText),
                new Vector2(textStartPoint.X, textStartPoint.Y - (productDetails.FontSizeDimension * 1.67)),
                productDetails.FontSizeDimension);
            textList.Add(text1);
            textList.Add(text2);
            return textList;

        }

        public void AddScale(string dxfFileLocation,int x,int y, int Unit)
        {
            Vector3 centerPoint = new Vector3(x, y, 0);
            //DxfDocument doc = new DxfDocument();
            var loadedDoc = DxfDocument.Load(dxfFileLocation);

            Line xAxis = new Line();
            xAxis.StartPoint = centerPoint;
            xAxis.EndPoint = new Vector3(centerPoint.X , centerPoint.X+ 200, 0);
            xAxis.Color = AciColor.Red;

            Line yAxis = new Line();
            yAxis.StartPoint = centerPoint;
            yAxis.EndPoint = new Vector3(centerPoint.Y + 200, centerPoint.Y, 0);
            yAxis.Color = AciColor.Red;

            loadedDoc.AddEntity(xAxis);
            loadedDoc.AddEntity(yAxis);

            for (int i= (int) centerPoint.X +1; i <= (int) yAxis.EndPoint.X; i++)
            {
                //X segments
                Line l = new Line();
                if((i+Unit)%Unit == 0)
                {
                    l.StartPoint = new Vector3(i, y, 0);
                    l.EndPoint = new Vector3(i, y + 5, 0);
                }
                else
                {
                    l.StartPoint = new Vector3(i, y, 0);
                    l.EndPoint = new Vector3(i, y + 2, 0);
                }                
                l.Color = AciColor.Red;
                loadedDoc.AddEntity(l);
            }

            for (int i = (int)centerPoint.Y +1; i <= (int) xAxis.EndPoint.Y; i++)
            {
                //Y segments
                Line l = new Line();
                if((i+ Unit)%Unit == 0)
                {
                    l.StartPoint = new Vector3(x, i, 0);
                    l.EndPoint = new Vector3(x + 5, i, 0);
                }
                else
                {
                    l.StartPoint = new Vector3(x, i, 0);
                    l.EndPoint = new Vector3(x + 2, i, 0);
                }
                
                l.Color = AciColor.Red;
                loadedDoc.AddEntity(l);
                
            }
            loadedDoc.Save(@"E:\krishna\testScale.dxf");

        }

        public void MergeDXFFiles(List<string> dxfDocFilePath)
        {
            DxfDocument doc = new DxfDocument();
            foreach (var _dxfdocFile in dxfDocFilePath)
            {
                DxfDocument doc1 = DxfDocument.Load(_dxfdocFile);
                foreach (var _docEntity in doc1.Lines)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as Line;
                    doc.AddEntity(_docEntity11);
                }
                foreach(var _docEntity in doc1.Arcs)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as Arc;
                    //var test = _docEntity11.PolygonalVertexes(100);
                    doc.AddEntity(_docEntity11);
                }
                foreach (var _docEntity in doc1.LwPolylines)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as LwPolyline;
                    doc.AddEntity(_docEntity11);
                }
                foreach (var _docEntity in doc1.Dimensions)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as Dimension;
                    doc.AddEntity(_docEntity11);
                }                
                foreach (var _docEntity in doc1.Polylines)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as Polyline;
                    doc.AddEntity(_docEntity11);
                }
                foreach (var _docEntity in doc1.Texts)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as Text;
                    doc.AddEntity(_docEntity11);
                }
                foreach (var _docEntity in doc1.Viewports)
                {
                    var _docEntity1 = _docEntity.Clone();
                    var _docEntity11 = _docEntity1 as Viewport;
                    doc.AddEntity(_docEntity11);
                }
            }
            doc.Save(@"E:\krishna\mergeTest1.dxf");
        }

        public List<EntityObject> Create2DPipeLine(Line lineEntity, Layer layer, double width, bool showWidth, DimensionStyle dimStyle, double cutAngle, bool isCutAngleClockWise, double length)
        {
            List<EntityObject> objectCollection = new List<EntityObject>();
            //Line line1 = new Line();
            //Line line2 = new Line();

            //line1.Linetype = Linetype.Continuous;
            //line2.Linetype = Linetype.Continuous;

           
            //var endPointOffset1 = lineEntity.EndPoint;
            //var endPointOffset2 = lineEntity.EndPoint;

            if (cutAngle != 0)
            {
                var startorEndOffset1 = lineEntity.StartPoint;
                var startorEndOffset2 = lineEntity.StartPoint;
                var A = (width / 2);
                var extendedLength = A * Math.Tan(cutAngle * (Math.PI / 180));
                if (isCutAngleClockWise && showWidth)
                {
                    startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, -extendedLength);
                    //endPointOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, extendedLength);
                    startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, extendedLength);
                    //endPointOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, -extendedLength);
                    var lwPolyLine1 = new LwPolyline
                    {
                        Vertexes =
                        {
                            new LwPolylineVertex(startorEndOffset1.X, startorEndOffset1.Y),
                            new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                        }
                    };
                    var lwPolyLine2 = new LwPolyline
                    {
                        Vertexes =
                        {
                            new LwPolylineVertex(startorEndOffset2.X, startorEndOffset2.Y),
                            new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                        }
                    };
                    var offset1 = lwPolyLine1.GetOffsetCurves(-width / 2);
                    var offset2 = lwPolyLine2.GetOffsetCurves(width / 2);

                    var endline1 = new Line(
                    new Vector2(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y),
                    new Vector2(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y));
                    var endLine2 = new Line(
                        new Vector2(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y),
                        new Vector2(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y));

                    offset1.Layer = layer;
                    offset2.Layer = layer;
                    endline1.Layer = layer;
                    endLine2.Layer = layer;

                    offset1.Color = AciColor.Blue;
                    offset2.Color = AciColor.Blue;
                    endline1.Color = AciColor.Blue;
                    endLine2.Color = AciColor.Blue;
                    objectCollection.Add(offset1);
                    objectCollection.Add(offset2);
                    objectCollection.Add(endline1);
                    objectCollection.Add(endLine2);

                }
                else
                {
                    //startPointOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, extendedLength);
                    //endPointOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, -extendedLength);
                    //startPointOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, -extendedLength);
                    //endPointOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, extendedLength);
                }
                
                //lwPolyLine1.

                //var offset1 = lwPolyLine1.GetOffsetCurves(-width / 2);
                //var offset2 = lwPolyLine2.GetOffsetCurves(width / 2);
                //line1.StartPoint = new Vector3(offset1.Vertexes[0].Position.X, offset1.Vertexes[0].Position.Y, 0);
                //line1.EndPoint = new Vector3(offset2.Vertexes[0].Position.X, offset2.Vertexes[0].Position.Y, 0);

                //line2.StartPoint = new Vector3(offset1.Vertexes[1].Position.X, offset1.Vertexes[1].Position.Y, 0);
                //line2.EndPoint = new Vector3(offset2.Vertexes[1].Position.X, offset2.Vertexes[1].Position.Y, 0);

            }
            else
            {
                var lwPolyLine = new LwPolyline
                {
                    Vertexes =
                      {
                          new LwPolylineVertex(lineEntity.StartPoint.X, lineEntity.StartPoint.Y),
                          new LwPolylineVertex(lineEntity.EndPoint.X, lineEntity.EndPoint.Y)
                      }
                };
                var offset1 = lwPolyLine.GetOffsetCurves(width / 2);
                var offset2 = lwPolyLine.GetOffsetCurves(-width / 2);
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

                offset1.Color = AciColor.Blue;
                offset2.Color = AciColor.Blue;
                endLine1.Color = AciColor.Blue;
                endLine2.Color = AciColor.Blue;


                objectCollection.Add(offset1);
                objectCollection.Add(offset2);
                objectCollection.Add(endLine1);
                objectCollection.Add(endLine2);

                if (showWidth)
                {
                    var shownWidth = ShowWidth(endLine1, layer, dimStyle, width * 0.75);
                    objectCollection.Add(shownWidth);
                }
            }
            return objectCollection;
        }

        public List<EntityObject> Create2DPipeArc(Arc arcEntity, Layer layer, EntityObject previousObject, double width,bool showWidth, DimensionStyle dimStyle,bool isClockWise, double CutAngle, bool isCutAngleClockwise)
        {
            List<EntityObject> objectCollection = new List<EntityObject>();
            var Offset1StartAngle = arcEntity.StartAngle;
            var offset2StartAngle = arcEntity.StartAngle;
            var offset1EndAngle = arcEntity.EndAngle;
            var offset2EndAngle = arcEntity.EndAngle;
            

            var offset1 = new Arc
            {
                Linetype = Linetype.Continuous,
                Center = new Vector3(arcEntity.Center.X, arcEntity.Center.Y, 0),
                StartAngle = Offset1StartAngle,// arcEntity.StartAngle,
                EndAngle = offset1EndAngle,//arcEntity.EndAngle,
                Radius = arcEntity.Radius + width / 2
            };
            var offset2 = new Arc
            {
                Linetype = Linetype.Continuous,
                Center = new Vector3(arcEntity.Center.X, arcEntity.Center.Y, 0),
                StartAngle = offset2StartAngle,// arcEntity.StartAngle,
                EndAngle = offset2EndAngle,// arcEntity.EndAngle,
                Radius = arcEntity.Radius - width / 2
            };

            if (CutAngle != 0 && showWidth)
            {
                //calculate length of cord C^2 = A^2 + B^2 -2abCos(c) where c is angle 
                var A = (width / 2);

                //var lengthofcord = Math.Sqrt((A * A) + (A * A) - (2 * A * A * Math.Cos(CutAngle * (Math.PI / 180))));
                var lengthofcord = Math.Sqrt(Math.Pow(A,2) + Math.Pow(A,2) - (2 * A * A * Math.Cos(CutAngle * (Math.PI / 180))));

                var RadianDegree =Math.Acos(((offset1.Radius * offset1.Radius) + (offset1.Radius* offset1.Radius) - (lengthofcord * lengthofcord)) / (2 * offset1.Radius * offset1.Radius));
                var degreeAngle = RadianDegree * (180 / Math.PI);
                if(isCutAngleClockwise)
                {
                    offset1.StartAngle = offset1.StartAngle - degreeAngle;
                    offset2.StartAngle = offset2.StartAngle + degreeAngle;
                }
                else
                {
                    offset1.StartAngle = offset1.StartAngle + degreeAngle;
                    offset2.StartAngle = offset2.StartAngle - degreeAngle;
                }



                #region previous logic
                /*
                
                //get center point inside of pipe for cut angle
                var Vertexes = arcEntity.GetVertexesOfArc(isClockWise);
                if (previousObject == null)
                {
                    //draw arc taking center as pipe center with cut angle
                    var centerPoint = Vertexes[0]; //start point will be the start point for cut edge.
                    var startPoint = offset1.GetVertexesOfArc(isClockWise)[1];
                    var angles = Coordinate.GetArcAngles(startPoint, centerPoint, width / 2, CutAngle, isCutAngleClockwise);
                    var arc1 = new Arc();
                    arc1.StartAngle = arcEntity.StartAngle;
                    arc1.EndAngle = angles[1];
                    arc1.Radius = width / 2;
                    arc1.Center = centerPoint;


                    Vector3 contactPoint;
                    var center = Coordinate.CalculateArcCenter(arc1, isCutAngleClockwise, arc1.Radius,
                        isCutAngleClockwise, out contactPoint);
                    var angles1 = Coordinate.GetArcAngles(contactPoint, center, arc1.Radius,
                        CutAngle, isCutAngleClockwise);

                    ////get  angle of arc1 from endpoint of arc1 which will be actual start angle for offset with reference of center of offset1
                    //var arc1Points = arc1.GetVertexesOfArc(isCutAngleClockwise); 
                    //var endpoint = Coordinate.CalculateLineEndPoint(arc1.Center, startPoint,
                    //            line.Length, preArc.IsClockwise);

                    //offset1.StartAngle = angles[0];
                }
                else
                {
                    //draw arc taking center as pipe center with cut angle
                    var centerPoint = Vertexes[1]; //end point will be the start point for cut edge1.
                    var startPoint = offset1.GetVertexesOfArc(isClockWise)[1];
                    var angles = Coordinate.GetArcAngles(startPoint, centerPoint, width / 2, CutAngle, isCutAngleClockwise);
                    var arc1 = new Arc();
                    arc1.StartAngle = arcEntity.StartAngle;
                    arc1.EndAngle = angles[1];
                    arc1.Radius = width / 2;
                    arc1.Center = centerPoint;


                    Vector3 contactPoint;
                    var center = Coordinate.CalculateArcCenter(arc1, isCutAngleClockwise, arc1.Radius,
                        isCutAngleClockwise, out contactPoint);
                    var angles1 = Coordinate.GetArcAngles(contactPoint, center, arc1.Radius,
                        CutAngle, isCutAngleClockwise);
                    //offset1.StartAngle = angles[0];

                }

                //var angles = Coordinate.GetArcAngles(arcEntity.)
                
                */
                #endregion

            }
            var verticesOffset1 = offset1.GetVertexesOfArc();
            var verticesOffset2 = offset2.GetVertexesOfArc();
            var endLine1 = new Line(verticesOffset1[0], verticesOffset2[0]);
            var endLine2 = new Line(verticesOffset1[1], verticesOffset2[1]);
            offset1.Layer = layer;
            offset2.Layer = layer;
            endLine1.Layer = layer;
            endLine2.Layer = layer;

            offset1.Color = AciColor.Green;
            offset2.Color = AciColor.Green;
            endLine1.Color = AciColor.Green;
            endLine2.Color = AciColor.Green;



            objectCollection.Add(offset1);
            objectCollection.Add(offset2);
            objectCollection.Add(endLine1);
            objectCollection.Add(endLine2);

            if(showWidth)
            {
                var shownWidth = ShowWidth(endLine1,layer,dimStyle, width * 0.75);
                objectCollection.Add(shownWidth);
            }
            return objectCollection;
        }

        private LinearDimension ShowWidth(Line endLine1, Layer layer, DimensionStyle dimStyle, double width)
        {
            var WDimension = new LinearDimension();

            WDimension.FirstReferencePoint = new Vector2(endLine1.StartPoint.X, endLine1.StartPoint.Y);
            WDimension.SecondReferencePoint = new Vector2(endLine1.EndPoint.X, endLine1.EndPoint.Y);
            WDimension.Offset = width; //width * 0.75;
            var slope1 = (endLine1.EndPoint.Y - endLine1.StartPoint.Y) /
                         (endLine1.EndPoint.X - endLine1.StartPoint.X);

            WDimension.Rotation = WDimension.Rotation > 180
                ? (360 - Math.Atan(slope1) * 180 / Math.PI)
                : Math.Atan(slope1) * 180 / Math.PI;
            WDimension.Layer = layer;
            WDimension.Style = dimStyle;
            //objectCollection.Add(WDimension);
            return WDimension;
        }

        public Layer AddLayer(string layerName)
        {
            Layer layer = new Layer(layerName);
            return layer;
        }
        public LinearDimension ShowLength(Line lineEntity, Layer layer, double offset, DimensionStyle dimStyle)
        {
            var dimension = new LinearDimension();

            dimension.FirstReferencePoint = new Vector2(lineEntity.StartPoint.X, lineEntity.StartPoint.Y);
            dimension.SecondReferencePoint = new Vector2(lineEntity.EndPoint.X, lineEntity.EndPoint.Y);
            dimension.Offset = offset;//productDetails.ProductWidth * 1.5;
            var slope = (lineEntity.EndPoint.Y - lineEntity.StartPoint.Y) /
                        (lineEntity.EndPoint.X - lineEntity.StartPoint.X);

            dimension.Rotation = dimension.Rotation > 180
                ? (360 - Math.Atan(slope) * 180 / Math.PI)
                : Math.Atan(slope) * 180 / Math.PI;
            dimension.Layer = layer;
            dimension.Style = dimStyle;
            return dimension;
        }

        public RadialDimension ShowRadius(Arc arcEntity, Layer layer, double offset, DimensionStyle dimStyle)
        {
            var startAngle = arcEntity.StartAngle > arcEntity.EndAngle
                      ? arcEntity.StartAngle - 360
                      : arcEntity.StartAngle;
            var dimension = new RadialDimension(arcEntity, (startAngle + arcEntity.EndAngle) / 2, offset);
            //dimension.SetDimensionLinePosition(new Vector2(0, 0)); 
            if(dimension.Measurement > 40)
            {
                Vector2 v = new Vector2(0, 0);
                var endpoint = Coordinate.CalculateLineEndPoint(new Vector3(dimension.CenterPoint.X, dimension.CenterPoint.Y, 0), new Vector3(dimension.ReferencePoint.X, dimension.ReferencePoint.Y, 0),  40 - dimension.Measurement);

                dimension.SetDimensionLinePosition(new Vector2(endpoint.X, endpoint.Y));
                dimension.Color = AciColor.DarkGray;
            }           
            dimension.Layer = layer;
            dimension.Style = dimStyle;
            return dimension;
        }

        public Line DrawAndAttachLine(EntityObject previousEntityObj,bool isPreviousArcClockWise, double length, double width, Layer layer)
        {
            Line line = new Line();
            line.Linetype = Linetype.Dashed;
            if (previousEntityObj == null)
            {
                line.StartPoint = new Vector3(0, 0, 0);
                line.EndPoint = new Vector3(length, 0, 0);
            }
            else if (previousEntityObj.Type == EntityType.Line)
            {
                var prevLineObj = previousEntityObj as Line;
                line.StartPoint = prevLineObj.EndPoint;
                line.EndPoint = Coordinate.CalculateLineEndPoint(prevLineObj.StartPoint, prevLineObj.EndPoint, length);
            }
            else if (previousEntityObj.Type == EntityType.Arc)
            {
                var prevArcObj = previousEntityObj as Arc;
                var startPoint = prevArcObj.GetVertexesOfArc(isPreviousArcClockWise)[1];
                var endPoint = Coordinate.CalculateLineEndPoint(prevArcObj.Center, startPoint,
                                length, isPreviousArcClockWise);
                line.StartPoint = startPoint;
                line.EndPoint = endPoint;
            }
            line.Layer = layer;
            line.Color = AciColor.Blue;
            
            return line;
        }

        public Arc DrawAndAttachArc(EntityObject previousEntityObj,bool isPrevArcClockWise, Layer layer, double angle, double radius, bool isClockWise)
        {
            Arc arc = new Arc();
            arc.Linetype = Linetype.Dashed;
            if (previousEntityObj == null)
            {
                var center = Coordinate.CalculateArcCenter(new Vector3(0, 0, 0),
                               new Vector3(0, 0, 0), radius, isClockWise);
                var angles = Coordinate.GetArcAngles(new Vector3(0, 0, 0), center, radius,
                    angle, isClockWise);
                arc.Linetype = Linetype.Center;
                arc.Center = center;
                arc.StartAngle = angles[0];
                arc.EndAngle = angles[1];
                arc.Radius = radius;
            }
            else if (previousEntityObj.Type == EntityType.Line)
            {
                var previousLineObj = previousEntityObj as Line;
                var center = Coordinate.CalculateArcCenter(previousLineObj.StartPoint,
                                      previousLineObj.EndPoint, radius, isClockWise);
                var angles = Coordinate.GetArcAngles(previousLineObj.EndPoint, center, radius,
                    angle, isClockWise);
                arc.Linetype = Linetype.Dashed;
                arc.Center = center;
                arc.StartAngle = angles[0];
                arc.EndAngle = angles[1];
                arc.Radius = radius;

            }
            else if (previousEntityObj.Type == EntityType.Arc)
            {
                var prevArcObj = previousEntityObj as Arc;
                Vector3 contactPoint;
                var center = Coordinate.CalculateArcCenter(prevArcObj, isPrevArcClockWise, radius,
                    isClockWise, out contactPoint);
                var angles = Coordinate.GetArcAngles(contactPoint, center, radius,
                    angle, isClockWise);

                arc.Linetype = Linetype.Dashed;
                arc.Center = center;
                arc.StartAngle = angles[0];
                arc.EndAngle = angles[1];
                arc.Radius = radius;
            }
            return arc;
        }

        public void DrawPipe()
        {

        }

        public void AddCustomerDetail()
        {

        }

        public bool Validate(Product prod, string fileName)
        {
            //validate wrong input
            var worngInputs = prod.PartList.Arcs.Where(x => x.Radius <= prod.ProductWidth / 2).ToList();
            if (worngInputs.Count > 0)
            {
                foreach (var item in worngInputs)
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains an arc defination with radius less than or equal to half of the product width.",
                              "  ---  Sequence Num  ---  ", item.EntityID));
                }
                return false;
            }
            return true;
        }

        public void AddStartOrEnd(double angle)
        {

        }

        public List<int> GetItemsSequence(Product prod, string fileName)
        {
            List<int> sequence = new List<int>();
            foreach (var _productItem in prod.PartList.Lines.OrderBy(x => x.EntityID))
            {
                if (sequence.Contains(_productItem.EntityID))
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains Multiple sequence number.",
                              "  ---  Sequence Num  ---  ", _productItem.EntityID));
                    //return;
                }
                sequence.Add(_productItem.EntityID);
            }
            foreach (var _productItem in prod.PartList.Arcs.OrderBy(x => x.EntityID))
            {
                if (sequence.Contains(_productItem.EntityID))
                {
                    throw new Exception(string.Concat(DateTime.Now, "  ---  File ", "\"", fileName, ".xml\" ",
                              "contains Multiple sequence number.",
                              "  ---  Sequence Num  ---  ", _productItem.EntityID));
                    //return;
                }
                sequence.Add(_productItem.EntityID);
            }
            return sequence;
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

        private static Vector2 GetTextStartPoint(List<EntityObject> collection,out double height,out double width)
        //private static Vector2 GetTextStartPoint(List<EntityObject> collection)
        {
            var result = new Vector2(0, 0);
            var result1 = new Vector2(0, 0);
            foreach (var item in collection)
            {

                if (item.Type == EntityType.Line)
                {
                    var thisItem = item as Line;
                    result.X = thisItem.StartPoint.X < result.X ? thisItem.StartPoint.X : result.X;
                    result.X = thisItem.EndPoint.X < result.X ? thisItem.EndPoint.X : result.X;
                    result.Y = thisItem.StartPoint.Y < result.Y ? thisItem.StartPoint.Y : result.Y;
                    result.Y = thisItem.EndPoint.Y < result.Y ? thisItem.EndPoint.Y : result.Y;

                    result1.X = thisItem.StartPoint.X > result1.X ? thisItem.StartPoint.X : result1.X;
                    result1.X = thisItem.EndPoint.X > result1.X ? thisItem.EndPoint.X : result1.X;
                    result1.Y = thisItem.StartPoint.Y > result1.Y ? thisItem.StartPoint.Y : result1.Y;
                    result1.Y = thisItem.EndPoint.Y > result1.Y ? thisItem.EndPoint.Y : result1.Y;

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

                   
                    result1.X = (thisItem.Center.X - thisItem.Radius) > result1.X
                        ? (thisItem.Center.X - thisItem.Radius)
                        : result1.X;
                    result1.Y = (thisItem.Center.Y - thisItem.Radius) > result1.Y
                      ? (thisItem.Center.Y - thisItem.Radius)
                      : result1.Y;
                }
            }
            width = result1.X - result.X;
            height = result1.Y - result.Y;
                        
            return result;
        }
    }

    public enum FileFormat
    {
        bitmap = 1,
        Jpeg = 2,
        GIF = 3,
        PCX = 4,
        TIFF = 5,
        PNG = 6,
        TGA = 7,
        WMF = 8,
        SVG = 101,
        PLT = 102,
        HGL = 103,
        PDF = 104,
        SVGZ = 106,
        CGM = 107,
        EPS = 108
    }
}
