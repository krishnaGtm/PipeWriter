using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Drawing;
namespace PipeWriter
{
    
    public class DxfFile
    {
        Vector2 v1 = new Vector2(-3,0);
        Vector2 v2 = new Vector2(3, 0);
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
        /// 
        #region previous Method
        /*
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
        */
        #endregion
        public void GenerateDXF(Product productDetails, string fileName, string outputFilePath)
        {
            //call validation
            if (Validate(productDetails, fileName))
            {
                //arrance items according to sequence number. sequence number must be unique
                var sequenceItems = GetItemsSequence(productDetails, fileName);
                if (sequenceItems.Any())
                {
                    sequenceItems = sequenceItems.OrderBy(x => x).ToList();
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

                    for (int i = 0; i < sequenceItems.Count; i++)
                    {
                        var layer = AddLayer("Layer" + i);
                        var docLineEntity = productDetails.PartList.Lines.FirstOrDefault(x => x.EntityID == sequenceItems[i]);
                        if (docLineEntity != null)
                        {
                            var line = new Line();
                            var isClockWise = false;
                            if(previousObject != null )
                                if(previousObject.Type == EntityType.Arc)
                                    isClockWise = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItems[i-1]).IsClockwise;
                            line = DrawAndAttachLine(previousObject, isClockWise, docLineEntity.Length, productDetails.ProductWidth, layer);
                            //if(productDetails.startangle != 0 && i==0)
                            //    AddStartOrEnd(productDetails.startangle);
                            bool showWidth = false;
                            bool sAngleClockwise = false;
                            bool eAngleClockwise = false;
                            double sCutAngle = 0;
                            double eCutAngle = 0;
                            if (i == 0)
                                showWidth = true;
                            else
                                showWidth = false;
                            if (i == 0)
                            {
                                sCutAngle = productDetails.StartCutAngle;
                                eCutAngle = 0;
                                sAngleClockwise = productDetails.StartAngleClockWise;
                                eAngleClockwise = false;
                            }                                
                            else if(i== sequenceItems.Count -1)
                            {
                                sCutAngle = 0;
                                eCutAngle = productDetails.EndCutAngle;
                                sAngleClockwise = false;
                                eAngleClockwise = productDetails.EndAngleClockWise;
                            }
                            //var cutAngle = 0;
                            var items = Create2DPipeLine(line, layer, productDetails.ProductWidth,showWidth,dimStyle, sCutAngle, eCutAngle, sAngleClockwise,eAngleClockwise, docLineEntity.Length);

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
                        var docArcEntity = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItems[i]);
                        if (docArcEntity != null)
                        {
                            var arc = new Arc();
                            var isClockWise = false;
                            if (previousObject != null)
                                if(previousObject.Type == EntityType.Arc)
                                    isClockWise = productDetails.PartList.Arcs.FirstOrDefault(x => x.EntityID == sequenceItems[i - 1]).IsClockwise;
                            arc = DrawAndAttachArc(previousObject,isClockWise, layer, docArcEntity.Angle, docArcEntity.Radius, docArcEntity.IsClockwise);
                            double sAngle = 0;
                            double eAngle = 0;
                            bool sAngeleClockwise = false;
                            bool eAngleClosewise = false;
                            bool showWidth = false;
                            if (i == 0)
                                showWidth = true;
                            else
                                showWidth = false;
                            if(i== 0 )
                            {
                                sAngle = productDetails.StartCutAngle;
                                sAngeleClockwise = productDetails.StartAngleClockWise;
                                eAngle = 0;
                                eAngleClosewise = false;
                            }
                            else if(i == sequenceItems.Count -1)
                            {
                                sAngle = 0;
                                sAngeleClockwise = false;
                                eAngle = productDetails.EndCutAngle;
                                eAngleClosewise = productDetails.EndAngleClockWise;
                            }
                            var cutangle = showWidth == true ? 60 : 0;
                            var items = Create2DPipeArc(arc, layer, previousObject, productDetails.ProductWidth, docArcEntity.IsClockwise, showWidth,dimStyle,sAngle, sAngeleClockwise, eAngle, eAngleClosewise);

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
        public bool GenerateDXF(string originalDXFFileNamne, double originalStartX, double originalStartY, double originalEndX, double originalEndY, List<MergeContent> addedContents)
        {
            try
            {
                var driveLocation = "E:\\krishna";
                var originalStart = new Vector2(originalStartX, originalStartY);
                var originalEnd = new Vector2(originalEndX, originalEndY);
                var originalDXF = DxfDocument.Load(Path.Combine(driveLocation, originalDXFFileNamne));
                foreach (var _list in addedContents)
                {
                    var point = new Vector2(_list.StartPointX, _list.StartPointY);
                    var coordinateForDXF = GetPointForDXF(originalStart, originalEnd, Path.Combine(driveLocation, originalDXFFileNamne), point);
                    var addedDXF = DxfDocument.Load(Path.Combine(driveLocation, _list.FileName));

                    #region line
                    foreach (var _dxfline in addedDXF.Lines)
                    {
                        _dxfline.StartPoint = new Vector3(_dxfline.StartPoint.X + coordinateForDXF.X, _dxfline.StartPoint.Y + coordinateForDXF.Y, 0);
                        _dxfline.EndPoint = new Vector3(_dxfline.EndPoint.X + coordinateForDXF.X, _dxfline.EndPoint.Y + coordinateForDXF.Y, 0);
                        var line = _dxfline.Clone() as Line;
                        originalDXF.AddEntity(line);
                    }
                    foreach (var _dxfployline in addedDXF.LwPolylines)
                    {
                        foreach (var _vertices in _dxfployline.Vertexes)
                        {
                            _vertices.Position = new Vector2(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y);
                        }
                        var polyline = _dxfployline.Clone() as LwPolyline;
                        originalDXF.AddEntity(polyline);
                    }
                    foreach (var _dxfployline in addedDXF.Polylines)
                    {
                        foreach (var _vertices in _dxfployline.Vertexes)
                        {
                            _vertices.Position = new Vector3(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y, 0);
                        }
                        var polyline = _dxfployline.Clone() as Polyline;
                        originalDXF.AddEntity(polyline);
                    }
                    #endregion

                    #region Arc
                    foreach (var _dxfArc in addedDXF.Arcs)
                    {
                        //var point1 = new Vector2(_list.StartPointX, _list.StartPointY);
                        //var coordinateForDXF1 = GetPointForDXF(originalStart, originalEnd, Path.Combine(driveLocation, originalDXFFileNamne), point);
                        _dxfArc.Center = new Vector3(_dxfArc.Center.X + coordinateForDXF.X, _dxfArc.Center.Y + coordinateForDXF.Y, 0);
                        var arc = _dxfArc.Clone() as Arc;
                        originalDXF.AddEntity(arc);
                    }
                    #endregion

                }
                originalDXF.Save(Path.Combine(driveLocation, originalDXFFileNamne + "merged.dxf"));

                return true;
            }
            catch
            {
                return false;
            }
            
            
        }

        //public void GenerateDXF(string originalDXFFileNamne, double originalStartX, double originalStartY, double originalEndX, double originalEndY, string addedDXFFileName, double startPointX, double startPointY, double rotation)
        //{
        //    var driveLocation = "E:\\krishna";
        //    var originalStart = new Vector2(originalStartX, originalStartY);
        //    var originalEnd = new Vector2(originalEndX, originalEndY);
        //    var point = new Vector2(startPointX, startPointY);
        //    var coordinateForDXF = GetPointForDXF(originalStart, originalEnd, Path.Combine(driveLocation, originalDXFFileNamne), point);            
        //    var addedDXF = DxfDocument.Load(Path.Combine(driveLocation, addedDXFFileName));
        //    var originalDXF = DxfDocument.Load(Path.Combine(driveLocation, originalDXFFileNamne));            
        //    foreach (var _dxfline in addedDXF.Lines)
        //    {
        //        _dxfline.StartPoint = new Vector3(_dxfline.StartPoint.X + coordinateForDXF.X, _dxfline.StartPoint.Y + coordinateForDXF.Y, 0);
        //        _dxfline.EndPoint = new Vector3(_dxfline.EndPoint.X + coordinateForDXF.X, _dxfline.EndPoint.Y + coordinateForDXF.Y, 0);
        //        var line = _dxfline.Clone() as Line;
        //        originalDXF.AddEntity(line);
        //    }
        //    foreach (var _dxfployline in addedDXF.LwPolylines)
        //    {
        //        foreach(var _vertices in _dxfployline.Vertexes)
        //        {
        //            _vertices.Position = new Vector2(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y);
        //        }
        //        var polyline = _dxfployline.Clone() as LwPolyline;
        //        originalDXF.AddEntity(polyline);
        //    }
        //    foreach (var _dxfployline in addedDXF.Polylines)
        //    {
        //        foreach(var _vertices in _dxfployline.Vertexes)
        //        {
        //            _vertices.Position = new Vector3(_vertices.Position.X + coordinateForDXF.X, _vertices.Position.Y + coordinateForDXF.Y,0);
        //        }
        //        var polyline = _dxfployline.Clone() as Polyline;
        //        originalDXF.AddEntity(polyline);
        //    }
        //    originalDXF.Save(Path.Combine(driveLocation, originalDXFFileNamne + "merged.dxf"));

        //}
        
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
            //process.StartInfo.Arguments = @"/r /p 3 /ad /b 7 /a -2 /f " + (int)outPutFileFormat + " " + dxfFilePath + "";
            process.StartInfo.Arguments = @"/r /ls  /p 3 /ad /b 7 /a -2 /f " + (int)outPutFileFormat + " " + dxfFilePath + "";

            process.Start();

            process.WaitForExit();
            
        }

        private Vector2 GetPointForDXF(Vector2 originalstartA, Vector2 originalEndB, string originalDXF, Vector2 startpointC)
        {
            //getTransformationMatraix();
            //Coordinate system of SVC start from top left corner.
            var quadrant = 0;

            if(startpointC.X >= originalstartA.X && startpointC.Y <= originalstartA.Y)
            {
                quadrant = 1;
            }
            else if (startpointC.X <= originalstartA.X && startpointC.Y <= originalstartA.Y)
            {
                quadrant = 2;
            }
            else if (startpointC.X <= originalstartA.X && startpointC.Y >= originalstartA.Y)
            {
                quadrant = 3;
            }
            else if (startpointC.X >= originalstartA.X && startpointC.Y >= originalstartA.Y)
            {
                quadrant = 4;
            }
            //get length of all sides by creating triangle with dummy line we created with reference to startpointC
            //instead of getting square root get square value which is used to find angle
            var ABsquare = ((originalstartA.X - originalEndB.X) * (originalstartA.X - originalEndB.X)) + ((originalstartA.Y - originalEndB.Y) * (originalstartA.Y - originalEndB.Y)); //Math.Sqrt(((originalstartA.X - originalEndB.X) * (originalstartA.X - originalEndB.X)) + ((originalstartA.Y - originalEndB.Y) * (originalstartA.Y - originalEndB.Y)));
            var ACsquare = ((originalstartA.X - startpointC.X) * (originalstartA.X - startpointC.X)) + ((originalstartA.Y - startpointC.Y) * (originalstartA.Y - startpointC.Y)); //Math.Sqrt(((originalstartA.X - startpointC.X) * (originalstartA.X - startpointC.X)) + ((originalstartA.Y - startpointC.Y) * (originalstartA.Y - startpointC.Y)));
            var BCsquare = ((originalEndB.X - startpointC.X) * (originalEndB.X - startpointC.X)) + ((originalEndB.Y - startpointC.Y) * (originalEndB.Y - startpointC.Y)); //Math.Sqrt(((originalEndB.X - startpointC.X) * (originalEndB.X - startpointC.X)) + ((originalEndB.Y - startpointC.Y) * (originalEndB.Y - startpointC.Y)));
            
            //Apply cosine rule for triangle law with ourDummy line to startPoint to calcuate relative point that need to be created for our original dxfFile.
            var anglea = Math.Acos(((ABsquare) + (ACsquare) - (BCsquare)) / (2 * Math.Sqrt(ABsquare) * Math.Sqrt(ACsquare))); //Math.Acos(((AC * AC) + (BC * BC) - (AB * AB)) / (2 * BC * AC)); 
            var angleb = Math.Acos(((ABsquare) + (BCsquare) - (ACsquare)) / (2 * Math.Sqrt(ABsquare) * Math.Sqrt(BCsquare)));
            var anglec = Math.PI - (anglea + angleb);//Math.Acos(((ACsquare) + (BCsquare) - (ABsquare)) / (2 * Math.Sqrt(BCsquare) * Math.Sqrt(ACsquare))); //Math.Acos(((AB * AB) + (AC * AC) - (BC * BC)) / (2 * AB * AC));


            //apply sine rule for triangle to find lenght of sides to triangle on original dxf.
            var startPointDXF = new Vector2(0, 0);
            var endPointDXF = new Vector2(100, 0);
            var AB1 = Math.Sqrt(((startPointDXF.X - endPointDXF.X) * (startPointDXF.X - endPointDXF.X)) + ((startPointDXF.Y - endPointDXF.Y) * (startPointDXF.Y - endPointDXF.Y)));
            //var AB1 = Math.Sqrt(Math.Pow((startPointDXF.X - endPointDXF.X), 2) + Math.Pow((startPointDXF.Y - endPointDXF.Y), 2));
            
            var AC1 = Math.Sin(angleb) * (AB1  / Math.Sin(anglec));

            var PointInDxf = new Vector2(0, 0);
            //x1 = x0 + cos(angle) * length    -- in this case length is AC1
            //y1 = y0 + sin(angle) * length    -- in this case length is AC1
            if (quadrant == 1)
            {
                PointInDxf.X = startPointDXF.X + (Math.Cos(anglea) * AC1);
                PointInDxf.Y = startPointDXF.Y + (Math.Sin(anglea) * AC1);
            }
            else if (quadrant == 2)
            {
                //PointInDxf.X = startPointDXF.X + (Math.Cos((anglec + 90) * Math.PI / 180) * AC1);
                //PointInDxf.Y = startPointDXF.Y + (Math.Sin((anglec + 90) * Math.PI / 180) * AC1);
                PointInDxf.X = startPointDXF.X + (Math.Cos((anglea + (Math.PI / 2d)) ) * AC1);
                PointInDxf.Y = startPointDXF.Y + (Math.Sin((anglea + (Math.PI / 2d)) ) * AC1);
            }
            else if (quadrant == 3)
            {
                //PointInDxf.X = startPointDXF.X + (Math.Cos((270 - anglec) * Math.PI / 180) * AC1);
                //PointInDxf.Y = startPointDXF.Y + (Math.Sin((270 - anglec) * Math.PI / 180) * AC1);
                PointInDxf.X = startPointDXF.X + (Math.Cos((3d * Math.PI / 2d) - anglea) * AC1);
                PointInDxf.Y = startPointDXF.Y + (Math.Sin((3d * Math.PI / 2d) - anglea) * AC1);
            }
            else if (quadrant == 4)
            {
                PointInDxf.X = startPointDXF.X + (Math.Cos((2d* Math.PI) - anglea) * AC1);
                PointInDxf.Y = startPointDXF.Y + (Math.Sin((2d * Math.PI) - anglea) * AC1);
            }
            return PointInDxf;
        }

        private List<EntityObject> AddText(List<EntityObject> list, Product productDetails)
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

        private void AddScale(string dxfFileLocation,int x,int y, int Unit)
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

        public List<EntityObject> Create2DPipeLine(Line lineEntity, Layer layer, double width, bool showWidth, DimensionStyle dimStyle, double startCutAngle, double endCutAngle, bool startAngelClockWise, bool endAngelClockWise, double length)
        {
            List<EntityObject> objectCollection = new List<EntityObject>();
            if (startCutAngle != 0 || endCutAngle != 0)
            {
                var startorEndOffset1 = lineEntity.StartPoint;
                var startorEndOffset2 = lineEntity.StartPoint;
                var A = (width / 2);
                var extendedLength = A * Math.Tan(startCutAngle * (Math.PI / 180));               
                
                if (startCutAngle != 0)
                {
                    if(startAngelClockWise)
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, -extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, extendedLength);
                    }
                    else
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.EndPoint, lineEntity.StartPoint, -extendedLength);
                    }
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


                    //show width dimension here





                }
                else if(endCutAngle != 0)
                {
                    if (endAngelClockWise)
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, -extendedLength);
                    }
                    else
                    {
                        startorEndOffset1 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, -extendedLength);
                        startorEndOffset2 = Coordinate.CalculateLineEndPoint(lineEntity.StartPoint, lineEntity.EndPoint, extendedLength);
                    }
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

        public List<EntityObject> Create2DPipeArc(Arc arcEntity, Layer layer, EntityObject previousObject, double width, bool isArcAngleClockwise, bool showWidth, DimensionStyle dimStyle, double startAngle,bool sAngleClockwise, double endAngle, bool eAngleClockwise)
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

            if (startAngle != 0 || endAngle != 0)
            {
                //calculate length of cord C^2 = A^2 + B^2 -2abCos(c) where c is angle 
                var A = (width / 2);                
                if (startAngle != 0)
                {
                    //let suport centerpoint of main arc is A Center of arc that is middle arc is point B and arc intersection point is C.
                    //than we know some sides and some angles for Triangle ABC.
                    //we can use sine rule of triangle to fine angle c.

                    //sina/a = sinb/b == sinc/c
                    if(sAngleClockwise)
                    {
                        //for offset 2
                        var sineAngleC = (Math.Sin(startAngle * (Math.PI / 180)) * (offset2.Radius + A)) / (offset2.Radius);
                        var anglec = Math.Asin(sineAngleC);
                        anglec = 180 - ((180/Math.PI) * anglec);
                        var anglea = 180 - anglec - startAngle;
                        offset2.StartAngle = offset2.StartAngle + anglea;

                        //for offset1 
                        offset1.StartAngle = offset1.StartAngle - anglea;

                    }
                    else
                    {
                        var sineAngleC = (Math.Sin(startAngle * (Math.PI / 180)) * (offset2.Radius + (width / 2))) / (offset2.Radius);
                        var anglec = Math.Asin(sineAngleC);
                        anglec = 180 - ((180 / Math.PI) * anglec);
                        var anglea = 180 - anglec - startAngle;
                        offset2.StartAngle = offset2.StartAngle - anglea;
                        //for offset1 
                        offset1.StartAngle = offset1.StartAngle + anglea;

                    }   
                }
                else
                {
                }
 

                //var startPoint = arcEntity.GetVertexesOfArc(isPreviousArcClockWise)[1];
                //var endPoint = Coordinate.CalculateLineEndPoint(prevArcObj.Center, startPoint,
                //                length, isPreviousArcClockWise);
                //line.StartPoint = startPoint;
                //line.EndPoint = endPoint;

                //var Center = 

                //var getAngles1 = Coordinate.GetArcAngles() 





                //var CutAngle = startAngle == 0 ? endAngle : startAngle;
                ////var lengthofcord = Math.Sqrt((A * A) + (A * A) - (2 * A * A * Math.Cos(CutAngle * (Math.PI / 180))));
                //var lengthofcord = Math.Sqrt(Math.Pow(A,2) + Math.Pow(A,2) - (2 * A * A * Math.Cos(CutAngle * (Math.PI / 180))));

                //var RadianDegree =Math.Acos(((offset1.Radius * offset1.Radius) + (offset1.Radius* offset1.Radius) - (lengthofcord * lengthofcord)) / (2 * offset1.Radius * offset1.Radius));
                //var degreeAngle = RadianDegree * (180 / Math.PI);
                //if(isCutAngleClockwise)
                //{
                //    offset1.StartAngle = offset1.StartAngle - degreeAngle;
                //    offset2.StartAngle = offset2.StartAngle + degreeAngle;
                //}
                //else
                //{
                //    offset1.StartAngle = offset1.StartAngle + degreeAngle;
                //    offset2.StartAngle = offset2.StartAngle - degreeAngle;
                //}

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

            if (showWidth)
            {
                var shownWidth = ShowWidth(endLine1, layer, dimStyle, width * 0.75);
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

        private Layer AddLayer(string layerName)
        {
            Layer layer = new Layer(layerName);
            return layer;
        }
        private LinearDimension ShowLength(Line lineEntity, Layer layer, double offset, DimensionStyle dimStyle)
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

        private RadialDimension ShowRadius(Arc arcEntity, Layer layer, double offset, DimensionStyle dimStyle)
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

        private Line DrawAndAttachLine(EntityObject previousEntityObj,bool isPreviousArcClockWise, double length, double width, Layer layer)
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

        private Arc DrawAndAttachArc(EntityObject previousEntityObj,bool isPrevArcClockWise, Layer layer, double angle, double radius, bool isClockWise)
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
        
        private bool Validate(Product prod, string fileName)
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

        private List<int> GetItemsSequence(Product prod, string fileName)
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
