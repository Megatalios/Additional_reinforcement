using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom_Project
{
    public class RebarConfig
    {
        /// <summary>
        /// Название арматуры (например, "А240 (АI)").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Диаметр арматуры в миллиметрах.
        /// </summary>
        public double Diameter { get; set; }

        /// <summary>
        /// Стоимость арматуры за 1 метр в рублях.
        /// </summary>
        public double CostPerMeter { get; set; }

        /// <summary>
        /// Шаг армирования в миллиметрах.
        /// </summary>
        public double Spacing { get; set; }

        /// <summary>
        /// Выдерживаемая нагрузка для данной конфигурации (диаметр + шаг) в кН/м².
        /// </summary>
        public double BearingCapacity { get; set; }

        // Возможно, добавить конструктор для удобного парсинга из строки CSV
        // public RebarConfig(string name, double diameter, double costPerMeter, double spacing, double bearingCapacity)
        // {
        //     Name = name;
        //     Diameter = diameter;
        //     CostPerMeter = costPerMeter;
        //     Spacing = spacing;
        //     BearingCapacity = bearingCapacity;
        // }
    }
}
