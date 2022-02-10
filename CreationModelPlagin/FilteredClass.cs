using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlagin
{
    public class FilteredClass
    {
        public static List<Level> GetLevelsList(ExternalCommandData commandData)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

           var levelList = new FilteredElementCollector(doc).OfClass(typeof(Level)).OfType<Level>().ToList();

            return levelList;
        }

        public static List<Wall> GetWallsList(ExternalCommandData commandData)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            var wallList = new FilteredElementCollector(doc).OfClass(typeof(Wall)).OfType<Wall>().ToList();

            return wallList;
        }
    }
}
