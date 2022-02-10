using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CreationModelPlagin
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;



            Level leve1 = FilteredClass.GetLevelsList(commandData).Where(x => x.Name.Equals("Уровень 1")).FirstOrDefault();
            Level leve2 = FilteredClass.GetLevelsList(commandData).Where(x => x.Name.Equals("Уровень 2")).FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;
            List<XYZ> points = PointList(dx, dy);
            List<Wall> walls = new List<Wall>();

            using (var ts = new Transaction(doc, "Создание стен"))
            {
                ts.Start();

                for (int i = 0; i < points.Count-1; i++)
                {
                    Line line = Line.CreateBound(points[i], points[i + 1]);
                    Wall wall = Wall.Create(doc, line, leve1.Id, false);
                    walls.Add(wall);
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(leve2.Id);

                }
                AddDoor(doc, leve1, walls[0]);
                //AddRoof(doc, leve2, walls);
                AddExtrusionRoof(doc, leve2, walls);

                for (int i = 1; i < points.Count - 1; i++)
                {
                    AddWindow(doc, leve1, walls[i]);
                }
                ts.Commit();

            }

            ////Типы семейств через WhereElementIsElementType()
            //var familyList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsElementType().OfType<FamilyInstance>().ToList();
            ////Экземпляры семейств через WhereElementIsNotElementType()
            //var familyList2 = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().OfType<FamilyInstance>().ToList();         
            return Result.Succeeded;

        }

        private void AddExtrusionRoof(Document doc, Level leve2, List<Wall> walls)
        {
            CurveArray curveArray = new CurveArray();
            RoofType roofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).OfType<RoofType>().Where(x => x.Name.Equals("Типовой - 400мм")).Where(x => x.FamilyName.Equals("Базовая крыша")).FirstOrDefault();
            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));
            List<Line> lines = new List<Line>();
            for (int i = 0; i < walls.Count; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
                lines.Add(line);      
            }
            XYZ point1 = new XYZ(lines[3].GetEndPoint(0).X, lines[3].GetEndPoint(0).Y, leve2.Elevation);
            XYZ point2 = new XYZ((lines[3].GetEndPoint(0).X+ lines[1].GetEndPoint(0).X)/2, (lines[3].GetEndPoint(0).Y + lines[1].GetEndPoint(0).Y) / 2, leve2.Elevation+10);
            XYZ point3 = new XYZ(lines[1].GetEndPoint(0).X, lines[1].GetEndPoint(0).Y, leve2.Elevation);
            XYZ refpoint = new XYZ(lines[2].GetEndPoint(0).X, lines[2].GetEndPoint(0).Y, leve2.Elevation);
            XYZ refpoint1 = new XYZ((lines[3].GetEndPoint(0).X + lines[2].GetEndPoint(0).X) / 2, (lines[3].GetEndPoint(0).Y + lines[2].GetEndPoint(0).Y) / 2, leve2.Elevation + 10);
            curveArray.Append(Line.CreateBound(point1, point2));
            curveArray.Append(Line.CreateBound(point2,point3));


            ReferencePlane plane = doc.Create.NewReferencePlane2(point1,refpoint1,refpoint, doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, leve2, roofType, 2*(lines[0].GetEndPoint(0).Y), 0);

          
        }

        private void AddRoof(Document doc, Level leve2, List<Wall> walls)
        {
            Application app = doc.Application;
            RoofType roofType = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).OfType<RoofType>().Where(x => x.Name.Equals("Типовой - 400мм")).Where(x => x.FamilyName.Equals("Базовая крыша")).FirstOrDefault();

            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));

            CurveArray footprint = app.Create.NewCurveArray();

            for (int i = 0; i < walls.Count; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
                footprint.Append(line);
            }
            ModelCurveArray footPrintModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(footprint, leve2, roofType,out footPrintModelCurveMapping);
            
            foreach (ModelCurve m in footPrintModelCurveMapping)
            {
                footPrintRoof.set_DefinesSlope(m, true);
                footPrintRoof.set_SlopeAngle(m, 0.5);
            }
        }

        public List<XYZ> PointList(double dx, double dy)
        {
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));
            return points;

        }
        public void AddDoor(Document doc,Level level,Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_Doors).OfType<FamilySymbol>().Where(x=>x.Name.Equals("0915 x 2134 мм")).FirstOrDefault();

            LocationCurve locationCurve = wall.Location as LocationCurve;

            XYZ point1 = locationCurve.Curve.GetEndPoint(0);
            XYZ point2 = locationCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
            {
                doorType.Activate();
                doc.Regenerate();
            }
            doc.Create.NewFamilyInstance(point, doorType, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }
        public void AddWindow(Document doc, Level level, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_Windows).OfType<FamilySymbol>().Where(x => x.Name.Equals("0915 x 1830 мм")).FirstOrDefault();

            LocationCurve locationCurve = wall.Location as LocationCurve;

            XYZ point1 = locationCurve.Curve.GetEndPoint(0);
            XYZ point2 = locationCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            XYZ offset = new XYZ(point.X, point.Y, UnitUtils.ConvertToInternalUnits(800,UnitTypeId.Millimeters));        
            if (!windowType.IsActive)
            {
                windowType.Activate();
                doc.Regenerate();
            }
            doc.Create.NewFamilyInstance(offset, windowType, wall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        }

    }
    
}
