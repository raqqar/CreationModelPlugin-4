﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            
            Document doc = commandData.Application.ActiveUIDocument.Document;
           
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();


            Level level1 = listLevel
                .Where(x=>x.Name.Equals("Уровень 1"))          
                .FirstOrDefault();

            Level level2 = listLevel
               .Where(x => x.Name.Equals("Уровень 2"))
               .FirstOrDefault();

            double width=UnitUtils.ConvertToInternalUnits(10000,UnitTypeId.Millimeters);
            double depth=UnitUtils.ConvertToInternalUnits(5000,UnitTypeId.Millimeters);

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            //вызов метода создания стен
            List<Wall> walls = CreateWalls(doc, width, depth, level1, level2);//изменен в соответствии с заданием 5
            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1]);

            transaction.Commit();
            return Result.Succeeded;
        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

           LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
            {
                doorType.Activate();
            }
            doc.Create.NewFamilyInstance(point, doorType, wall, StructuralType.NonStructural);
        }

        public List<Wall>  CreateWalls(Document doc, double width, double depth, Level lvlstr, Level lvlfn)//отдельный метод построения стен в модели.
        {
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

           

            List<Wall> walls = new List<Wall>();

            
            for (int i = 0; i < 4; i++)
            {

                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, lvlstr.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(lvlfn.Id);

            }
            return walls;//возвращаем список для возможности вызова в методе ADD.Door
            
        }

        private void AddWindow(Document doc, Level level1, Wall wall)
        {

            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1220 мм"))
                .Where(x => x.Family.Name.Equals("Фиксированные"))
                .FirstOrDefault();


            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!windowType.IsActive)
            {
                windowType.Activate();
            }

            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            window.flipFacing();

            double height = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(height);


        }
    }


}
