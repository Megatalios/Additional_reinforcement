using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom_Project
{
    public class Node
    {
        // Исходные данные точки
        public string Type { get; set; }
        public int Number { get; set; }

        // Координаты точки в футах Revit API
        public double X { get; set; }
        public double Y { get; set; }
        public double ZCenter { get; set; } // Z центр в футах
        public double ZMin { get; set; } // Z минимум в футах

        // Требуемое армирование по направлениям в исходных единицах CSV
        // Значение -1 указывает, что это направление было исключено пользователем
        public double As1X { get; set; }
        public double As2X { get; set; }
        public double As3Y { get; set; }
        public double As4Y { get; set; }

        // Индекс плиты, к которой привязана эта точка (индекс в списке floors)
        public int SlabId { get; set; }

        // Дополнительные поля, которые могут потребоваться для алгоритма (например, для кластеризации)
        public int ClusterID { get; set; } = 0; // Идентификатор кластера
        // public double RequiredAs { get; set; } // Требуемая площадь армирования для этой точки (может рассчитываться в Optimizer)
        // public XYZ PointXYZ { get; set; } // Координаты в виде XYZ (опционально)

        // Конструктор (опционально)
        // public Node(string type, int number, double x_ft, double y_ft, double zCenter_ft, double zMin_ft, double as1x, double as2x, double as3y, double as4y, int slabId)
        // {
        //     Type = type;
        //     Number = number;
        //     X = x_ft;
        //     Y = y_ft;
        //     ZCenter = zCenter_ft;
        //     ZMin = zMin_ft;
        //     As1X = as1x;
        //     As2X = as2x;
        //     As3Y = as3y;
        //     As4Y = as4y;
        //     SlabId = slabId;
        // }
    }
}
