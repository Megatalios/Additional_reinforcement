using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom_Project
{
    public class Zone
    {
        /// <summary>
        /// Список точек, которые входят в эту зону.
        /// </summary>
        public List<Node> Points { get; set; }

        /// <summary>
        /// Геометрические границы зоны в виде BoundingBoxXYZ (в футах Revit API).
        /// Охватывает все точки зоны.
        /// </summary>
        public BoundingBoxXYZ Bounds { get; set; }

        /// <summary>
        /// Максимальная требуемая нагрузка по каждому из четырех направлений (As1X, As2X, As3Y, As4Y)
        /// среди всех точек в этой зоне (в кН/м²).
        /// Индексы: 0 - As1X, 1 - As2X, 2 - As3Y, 3 - As4Y.
        /// </summary>
        public double[] MaxRequiredLoad { get; set; }

        /// <summary>
        /// Оптимальные конфигурации арматуры, подобранные для каждого из четырех направлений
        /// в этой зоне. Может быть null, если армирование по данному направлению не требуется
        /// или решение не найдено.
        /// Индексы: 0 - As1X, 1 - As2X, 2 - As3Y, 3 - As4Y.
        /// </summary>
        public RebarConfig[] OptimalRebarConfigs { get; set; }

        /// <summary>
        /// Рассчитанная общая стоимость арматуры для этой зоны (в рублях).
        /// Учитывает подобранные конфигурации и отверстия.
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Рассчитанная общая длина арматуры для этой зоны (в футах).
        /// Сумма длин арматуры по всем направлениям с учетом отверстий.
        /// </summary>
        public double Length { get; set; } // Или в метрах, нужно определиться с единицами для расчетов

        // Возможно, другие поля, например, для хранения информации о плите, к которой относится зона

        /// <summary>
        /// Конструктор для создания начальной зоны из одной точки.
        /// </summary>
        /// <param name="point">Точка, из которой создается зона.</param>
        /// <param name="initialSize">Начальный размер прямоугольника зоны вокруг точки (в футах).</param>
        /// 
        public Zone(List<Node> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("Список узлов не может быть пустым при создании зоны.");
            }
            Points = new List<Node>(nodes); // Копируем список узлов
            CalculateBounds(); // Рассчитываем границы зоны на основе точек
            CalculateMaxRequiredLoad(); // Рассчитываем максимальную требуемую нагрузку
            // Инициализация других свойств
            Cost = double.MaxValue; // Изначально стоимость неизвестна
            Length = 0; // Изначально длина 0
            OptimalRebarConfigs = new RebarConfig[4]; // Инициализация массива для 4 направлений
        }
        public Zone(Node point, double initialSize)
        {
            Points = new List<Node> { point };

            // Определяем начальные границы зоны вокруг точки
            double halfSize = initialSize / 2.0;
            Bounds = new BoundingBoxXYZ
            {
                Min = new XYZ(point.X - halfSize, point.Y - halfSize, point.ZMin), // Используем ZMin для нижней границы
                Max = new XYZ(point.X + halfSize, point.Y + halfSize, point.ZCenter) // Используем ZCenter для верхней границы
            };

            // Инициализируем массивы для требуемой нагрузки и подобранных конфигураций
            MaxRequiredLoad = new double[4];
            OptimalRebarConfigs = new RebarConfig[4];

            // На начальном этапе требуемая нагрузка равна нагрузке самой точки
            MaxRequiredLoad[0] = point.As1X;
            MaxRequiredLoad[1] = point.As2X;
            MaxRequiredLoad[2] = point.As3Y;
            MaxRequiredLoad[3] = point.As4Y;

            // Стоимость и длина будут рассчитаны позже при подборе арматуры
            Cost = 0;
            Length = 0;
        }

        private void CalculateBounds()
        {
            if (Points == null || Points.Count == 0)
            {
                Bounds = null; // Или инициализировать пустой BoundingBoxXYZ, если возможно
                return;
            }

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (var point in Points)
            {
                // Используем координаты точки в футах (предполагается, что они уже в футах в объекте Node)
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                minZ = Math.Min(minZ, point.ZCenter); // Используем ZCenter или ZMin/ZMax если они есть в Node
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.ZCenter); // Используем ZCenter или ZMin/ZMax
            }

            Bounds = new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };
        }

        //private void CalculateBounds(double size_ft)
        //{
        //    if (Points == null || Points.Count != 1)
        //    {
        //        // Этот метод предназначен только для зон с одной точкой
        //        CalculateBounds(); // Возвращаемся к расчету по точкам, если условие не выполнено
        //        return;
        //    }

        //    Node point = Points.First(); // Берем единственную точку
        //    XYZ center = point.GetXYZ(); // Координаты точки в футах

        //    double halfSize = size_ft / 2.0;

        //    Bounds = new BoundingBoxXYZ
        //    {
        //        Min = new XYZ(center.X - halfSize, center.Y - halfSize, center.Z - halfSize), // Учитываем Z
        //        Max = new XYZ(center.X + halfSize, center.Y + halfSize, center.Z + halfSize)  // Учитываем Z
        //    };
        //}

        private void CalculateMaxRequiredLoad()
        {
            MaxRequiredLoad = new double[4]; // Инициализация для 4 направлений

            if (Points == null || Points.Count == 0)
            {
                // Если точек нет, требуемая нагрузка 0 по всем направлениям
                return;
            }

            // Инициализируем максимальные нагрузки минимальными возможными значениями
            MaxRequiredLoad[0] = double.MinValue; // As1X
            MaxRequiredLoad[1] = double.MinValue; // As2X
            MaxRequiredLoad[2] = double.MinValue; // As3Y
            MaxRequiredLoad[3] = double.MinValue; // As4Y

            foreach (var point in Points)
            {
                // Обновляем максимальные нагрузки для каждого направления
                // Учитываем только те направления, которые не были отключены пользователем (значение != -1 в Node)
                if (point.As1X != -1) MaxRequiredLoad[0] = Math.Max(MaxRequiredLoad[0], point.As1X);
                if (point.As2X != -1) MaxRequiredLoad[1] = Math.Max(MaxRequiredLoad[1], point.As2X);
                if (point.As3Y != -1) MaxRequiredLoad[2] = Math.Max(MaxRequiredLoad[2], point.As3Y);
                if (point.As4Y != -1) MaxRequiredLoad[3] = Math.Max(MaxRequiredLoad[3], point.As4Y);
            }

            // Если после прохода по всем точкам максимальное значение осталось double.MinValue,
            // значит, армирование по этому направлению не требовалось ни в одной точке зоны
            // (или направление было отключено пользователем). Устанавливаем требуемую нагрузку в 0.
            for (int i = 0; i < 4; i++)
            {
                if (MaxRequiredLoad[i] == double.MinValue)
                {
                    MaxRequiredLoad[i] = 0;
                }
            }
        }


        /// <summary>
        /// Конструктор для создания объединенной зоны из двух существующих зон.
        /// </summary>
        /// <param name="zoneA">Первая объединяемая зона.</param>
        /// <param name="zoneB">Вторая объединяемая зона.</param>
        public Zone(Zone zoneA, Zone zoneB)
        {
            // Объединяем списки точек
            Points = new List<Node>(zoneA.Points);
            Points.AddRange(zoneB.Points);

            // Пересчитываем границы объединенной зоны
            double minX = Math.Min(zoneA.Bounds.Min.X, zoneB.Bounds.Min.X);
            double minY = Math.Min(zoneA.Bounds.Min.Y, zoneB.Bounds.Min.Y);
            double minZ = Math.Min(zoneA.Bounds.Min.Z, zoneB.Bounds.Min.Z); // Учитываем минимальный Z
            double maxX = Math.Max(zoneA.Bounds.Max.X, zoneB.Bounds.Max.X);
            double maxY = Math.Max(zoneA.Bounds.Max.Y, zoneB.Bounds.Max.Y);
            double maxZ = Math.Max(zoneA.Bounds.Max.Z, zoneB.Bounds.Max.Z); // Учитываем максимальный Z

            Bounds = new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };

            // Пересчитываем максимальную требуемую нагрузку для объединенной зоны
            MaxRequiredLoad = new double[4];
            MaxRequiredLoad[0] = Math.Max(zoneA.MaxRequiredLoad[0], zoneB.MaxRequiredLoad[0]);
            MaxRequiredLoad[1] = Math.Max(zoneA.MaxRequiredLoad[1], zoneB.MaxRequiredLoad[1]);
            MaxRequiredLoad[2] = Math.Max(zoneA.MaxRequiredLoad[2], zoneB.MaxRequiredLoad[2]);
            MaxRequiredLoad[3] = Math.Max(zoneA.MaxRequiredLoad[3], zoneB.MaxRequiredLoad[3]);

            // Оптимальные конфигурации, стоимость и длина будут рассчитаны позже
            OptimalRebarConfigs = new RebarConfig[4]; // Инициализация массива
            Cost = 0;
            Length = 0;
        }

        /// <summary>
        /// Метод для подбора оптимального арматурного решения для текущей зоны.
        /// Должен использовать список доступных конфигураций арматуры.
        /// </summary>
        /// <param name="availableRebars">Список доступных конфигураций арматуры (RebarConfig).</param>
        /// <param name="openings">Список контуров отверстий в плите (в футах).</param>
        /// <param name="plateBoundary">Внешний контур плиты (в футах).</param>
        //public void FindOptimalRebarSolution(List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary)
        //{
        //    // Инициализируем общую стоимость и длину зоны
        //    this.Cost = 0;
        //    this.Length = 0;

        //    // Инициализируем массив подобранных конфигураций
        //    this.OptimalRebarConfigs = new RebarConfig[4];

        //    // Перебираем каждое из четырех направлений армирования
        //    for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        //    {
        //        // Проверяем, требуется ли армирование по данному направлению
        //        // Если MaxRequiredLoad <= 0 или равно -1 (если направление не выбрано), пропускаем
        //        if (MaxRequiredLoad[directionIndex] <= 0 || MaxRequiredLoad[directionIndex] == -1)
        //        {
        //            OptimalRebarConfigs[directionIndex] = null; // Явно указываем, что арматура по этому направлению не нужна
        //            continue; // Переходим к следующему направлению
        //        }

        //        // Ищем лучшую конфигурацию арматуры для текущего направления
        //        RebarConfig bestConfigForDirection = null;
        //        double minCostForDirection = double.MaxValue; // Изначально ставим очень высокую стоимость

        //        // Перебираем все доступные конфигурации арматуры
        //        if (availableRebars != null)
        //        {
        //            foreach (var currentConfig in availableRebars)
        //            {
        //                // Проверяем, удовлетворяет ли выдерживаемая нагрузка конфигурации требуемой нагрузке зоны по данному направлению
        //                // Предполагаем, что MaxRequiredLoad и BearingCapacity в одних единицах (кН/м²)
        //                if (currentConfig.BearingCapacity >= MaxRequiredLoad[directionIndex])
        //                {
        //                    // Если конфигурация подходит, рассчитываем ее стоимость и длину для зоны
        //                    (double currentCost, double calculatedLength) = CalculateRebarCostAndLengthForDirection(currentConfig, directionIndex, openings);

        //                    // Сравниваем с текущей минимальной стоимостью для этого направления
        //                    if (currentCost < minCostForDirection)
        //                    {
        //                        minCostForDirection = currentCost;
        //                        bestConfigForDirection = currentConfig;
        //                    }
        //                }
        //            }
        //        }

        //        // После перебора всех конфигураций, сохраняем лучшую для данного направления
        //        OptimalRebarConfigs[directionIndex] = bestConfigForDirection;

        //        // Если найдена подходящая конфигурация (bestConfigForDirection не null), добавляем ее стоимость и длину к общим
        //        if (bestConfigForDirection != null)
        //        {
        //            // Пересчитываем стоимость и длину для найденной лучшей конфигурации
        //            // (это нужно, потому что minCostForDirection и calculatedLength были рассчитаны внутри цикла)
        //            (double finalCost, double finalLength) = CalculateRebarCostAndLengthForDirection(bestConfigForDirection, directionIndex, openings);

        //            this.Cost += finalCost;
        //            this.Length += finalLength;
        //        }
        //        else
        //        {
        //            // Если для данного направления не найдена ни одна подходящая конфигурация,
        //            // это означает, что зона не может быть полностью армирована доступной арматурой.
        //            // Присваиваем зоне очень высокую общую стоимость, чтобы она не считалась оптимальным решением.
        //            System.Diagnostics.Debug.WriteLine($"Подбор арматуры: Для направления {directionIndex} зоны с точками {string.Join(",", Points.Select(p => p.Number))} не найдена подходящая конфигурация арматуры. Присвоена очень высокая стоимость.");
        //            this.Cost = double.MaxValue;
        //            this.Length = double.MaxValue; // Также присваиваем высокую длину (опционально)
        //            // Можно прервать подбор для других направлений этой зоны, т.к. решение уже не будет полным
        //            break;
        //        }
        //    }

        //    System.Diagnostics.Debug.WriteLine($"Подбор арматуры: Для зоны с точками {string.Join(",", Points.Select(p => p.Number))}. Общая стоимость: {this.Cost:F2} руб, Общая длина: {this.Length:F2} футов.");
        //}

        public void FindOptimalRebarSolution(List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary, double[] basicReinforcementThresholds)
        {
            this.Cost = 0; // Начинаем с 0, будем суммировать
            this.Length = 0;
            this.OptimalRebarConfigs = new RebarConfig[4]; // Сбрасываем предыдущие

            if (availableRebars == null || availableRebars.Count == 0 || Bounds == null || basicReinforcementThresholds == null || basicReinforcementThresholds.Length != 4)
            {
                this.Cost = double.MaxValue; // Невозможно найти решение
                System.Diagnostics.Debug.WriteLine("Zone.FindOptimalRebarSolution: Недостаточно входных данных.");
                return;
            }

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                double requiredLoadForDirection = MaxRequiredLoad[directionIndex];
                double basicThreshold = basicReinforcementThresholds[directionIndex];

                if (requiredLoadForDirection <= 0 || (basicThreshold != -1 && requiredLoadForDirection <= basicThreshold))
                {
                    OptimalRebarConfigs[directionIndex] = null;
                    continue;
                }

                RebarConfig bestConfigForDir = null;
                double minCostForDir = double.MaxValue;

                foreach (var currentConfig in availableRebars)
                {
                    if (currentConfig.BearingCapacity >= requiredLoadForDirection)
                    {
                        // Рассчитываем стоимость только для ЭТОГО направления с ЭТОЙ конфигурацией
                        // CalculateRebarCostAndLengthForDirection должен быть адаптирован,
                        // чтобы считать стоимость для ОДНОГО направления, а не для всей зоны.
                        // Или же вынести эту логику сюда.
                        // Пока предположим, что CalculateRebarCostAndLengthForDirection считает для одного направления.
                        (double currentDirectionCost, double currentDirectionLength) =
                            CalculateRebarCostAndLengthForDirection(currentConfig, directionIndex, openings); // Этот метод у вас уже есть

                        if (currentDirectionCost < minCostForDir)
                        {
                            minCostForDir = currentDirectionCost;
                            bestConfigForDir = currentConfig;
                        }
                    }
                }

                OptimalRebarConfigs[directionIndex] = bestConfigForDir;

                if (bestConfigForDir != null)
                {
                    // Если нашли лучшую конфигурацию, то ее стоимость уже minCostForDir
                    // и ее длина была рассчитана вместе с currentDirectionCost.
                    // Но CalculateRebarCostAndLengthForDirection возвращает и длину,
                    // ее нужно где-то сохранить, если вы хотите суммировать общую длину по оптимальным конфигам.
                    // Однако, обновление this.Cost и this.Length всей зоны лучше делать ОДИН РАЗ в конце,
                    // вызвав this.CalculateCostAndLength(openings, plateBoundary);
                }
                else
                {
                    // Не найдена арматура для обязательного направления
                    System.Diagnostics.Debug.WriteLine($"Zone.FindOptimalRebarSolution: Для направления {directionIndex} (требуемая нагрузка {requiredLoadForDirection:F2}) не найдена подходящая конфигурация.");
                    this.Cost = double.MaxValue; // Делаем всю зону невалидной
                    return; // Выходим, так как решение для зоны неполное
                }
            }

            // После того как OptimalRebarConfigs заполнены для всех необходимых направлений,
            // рассчитываем итоговую стоимость и длину всей зоны.
            // Если мы дошли сюда, значит, для всех требуемых направлений была найдена арматура.
            if (this.Cost != double.MaxValue) // Если зона не была помечена как невалидная
            {
                this.CalculateCostAndLength(openings, plateBoundary); // Обновляем Cost и Length на основе OptimalRebarConfigs
            }
        }
        //public void FindOptimalRebarSolution(List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary, double[] basicReinforcementThresholds)
        //{
        //    // Сбрасываем предыдущее решение
        //    Cost = 0; // Начинаем с 0, будем суммировать по направлениям
        //    Length = 0; // Начинаем с 0
        //    OptimalRebarConfigs = new RebarConfig[4]; // Сбрасываем массив конфигураций

        //    // Проверяем входные данные
        //    // Добавлена проверка basicReinforcementThresholds
        //    if (availableRebars == null || availableRebars.Count == 0 || Bounds == null || basicReinforcementThresholds == null || basicReinforcementThresholds.Length != 4)
        //    {
        //        Cost = double.MaxValue; // Невозможно найти решение без данных или границ
        //        System.Diagnostics.Debug.WriteLine("Zone.FindOptimalRebarSolution: Недостаточно входных данных (availableRebars, Bounds, или basicReinforcementThresholds).");
        //        return;
        //    }

        //    // Итерируемся по каждому из 4 направлений армирования
        //    for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        //    {
        //        double requiredLoad = MaxRequiredLoad[directionIndex];
        //        // Получаем порог для текущего направления из переданного массива
        //        double basicThreshold = basicReinforcementThresholds[directionIndex];

        //        // Если требуемая нагрузка по данному направлению <= 0, или она меньше или равна порогу основного армирования (если порог задан),
        //        // дополнительное армирование не требуется
        //        if (requiredLoad <= 0 || (basicThreshold != -1 && requiredLoad <= basicThreshold))
        //        {
        //            OptimalRebarConfigs[directionIndex] = null; // Нет необходимости в арматуре по этому направлению
        //            continue; // Переходим к следующему направлению
        //        }

        //        // ... (остальная часть метода FindOptimalRebarSolution, включая расчет стоимости и длины)
        //        // Убедитесь, что внутри этого метода вы также передаете basicReinforcementThresholds
        //        // при вызове CalculateRebarCostAndLengthForDirection, если этот метод его требует.
        //        // (В предоставленном мной коде CalculateRebarCostAndLengthForDirection не требует basicReinforcementThresholds напрямую,
        //        // т.к. requiredLoad уже учитывает порог).

        //        // ... (остальная часть метода FindOptimalRebarSolution)
        //    }
        //    // ... (конец метода FindOptimalRebarSolution)
        //}
        /// <summary>
        /// Метод для расчета "выгодности" объединения текущей зоны с другой зоной.
        /// Учитывает потенциальную экономию.
        /// </summary>
        /// <param name="otherZone">Другая зона для объединения.</param>
        /// <param name="availableRebars">Список доступных конфигураций арматуры (RebarConfig).</param>
        /// <param name="openings">Список контуров отверстий в плите (в футах).</param>
        /// <param name="plateBoundary">Внешний контур плиты (в футах).</param>
        /// <returns>Потенциальная экономия от объединения (руб). Отрицательное значение означает удорожание.</returns>
        //public double CalculateMergeBenefit(Zone otherZone, List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary)
        //{
        //    // 1. Создать временную объединенную зону (используя конструктор Zone(zoneA, zoneB)).
        //    Zone potentialMergedZone = new Zone(this, otherZone);

        //    // 2. Для временной зоны подобрать оптимальное арматурное решение, вызвав ее метод FindOptimalRebarSolution(...).
        //    // Важно: FindOptimalRebarSolution обновит Cost и Length временной зоны
        //    potentialMergedZone.FindOptimalRebarSolution(availableRebars, openings, plateBoundary);

        //    // 3. Рассчитать выгоду = (this.Cost + otherZone.Cost) - Стоимость временной зоны.
        //    // Если FindOptimalRebarSolution вернул double.MaxValue (не найдено решение),
        //    // то выгода будет отрицательной и очень большой, что правильно.
        //    double mergeBenefit = (this.Cost + otherZone.Cost) - potentialMergedZone.Cost;

        //    System.Diagnostics.Debug.WriteLine($"Расчет выгоды: Объединение зон с точками {string.Join(",", this.Points.Select(p => p.Number))} и {string.Join(",", otherZone.Points.Select(p => p.Number))}. Выгода: {mergeBenefit:F2} руб.");

        //    return mergeBenefit;
        //}

        //public double CalculateMergeBenefit(Zone otherZone, List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary, double[] basicReinforcementThresholds) // Добавлен basicReinforcementThresholds в сигнатуру
        //{
        //    // 1. Создать временную объединенную зону (используя конструктор Zone(zoneA, zoneB)).
        //    Zone potentialMergedZone = new Zone(this, otherZone);

        //    // 2. Для временной зоны подобрать оптимальное арматурное решение, вызвав ее метод FindOptimalRebarSolution(...).
        //    // Важно: FindOptimalRebarSolution обновит Cost и Length временной зоны
        //    // ПЕРЕДАЕМ BASICREINFORCEMENTTHRESHOLDS!
        //    potentialMergedZone.FindOptimalRebarSolution(availableRebars, openings, plateBoundary, basicReinforcementThresholds); // Передаем basicReinforcementThresholds

        //    // 3. Рассчитать выгоду = (this.Cost + otherZone.Cost) - Стоимость временной зоны.
        //    // Если FindOptimalRebarSolution вернул double.MaxValue (не найдено решение),
        //    // то выгода будет отрицательной и очень большой, что правильно.
        //    double mergeBenefit = (this.Cost + otherZone.Cost) - potentialMergedZone.Cost;

        //    // Отладочный вывод (можно оставить или удалить)
        //    System.Diagnostics.Debug.WriteLine($"Расчет выгоды: Объединение зон с точками {string.Join(",", this.Points.Select(p => p.Number))} и {string.Join(",", otherZone.Points.Select(p => p.Number))}. Выгода: {mergeBenefit:F2} руб.");

        //    return mergeBenefit;
        //}
        public double CalculateMergeBenefit(Zone otherZone, List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary, double[] basicReinforcementThresholds, double zoneReductionBenefitFactor)
        {
            // 1. Создать временную объединенную зону.
            Zone potentialMergedZone = new Zone(this, otherZone);

            // 2. Для временной зоны подобрать оптимальное арматурное решение.
            potentialMergedZone.FindOptimalRebarSolution(availableRebars, openings, plateBoundary, basicReinforcementThresholds);



            // 3. Рассчитать стоимость и длину для объединенной зоны на основе подобранной арматуры.
            // Убедитесь, что CalculateCostAndLength корректно вызывается внутри FindOptimalRebarSolution
            // или вызовите его здесь явно, если это не так.
            // Сейчас, судя по коду ZoneOptimizer, CalculateCostAndLength вызывается для initialZone,
            // но неясно, вызывается ли он для mergedZone после FindOptimalRebarSolution.
            // Давайте для надежности вызовем его здесь, если FindOptimalRebarSolution его не вызывает для обновления Cost.
            // Однако, если FindOptimalRebarSolution уже обновляет Cost, этот вызов может быть избыточен.
            // В вашем Zone.FindOptimalRebarSolution метод CalculateCostAndLength не вызывается.
            // А в ZoneOptimizer.FindBestReinforcementSolutions CalculateCostAndLength вызывается для initialZone, но не для mergedZone.
            // Это значит, что Cost у mergedZone после FindOptimalRebarSolution может быть неактуальным.

            // Для корректного расчета выгоды, убедимся, что Cost для всех зон актуален.
            // Предполагаем, что this.Cost и otherZone.Cost уже актуальны.
            // Для potentialMergedZone нужно обновить Cost после FindOptimalRebarSolution.
            // Это должно делаться либо в FindOptimalRebarSolution, либо здесь.
            // Судя по вашему коду Zone.cs, FindOptimalRebarSolution не вызывает CalculateCostAndLength.
            // А CalculateCostAndLength обновляет this.Cost.

            // !!! ВАЖНО: FindOptimalRebarSolution должен обновить this.Cost и this.Length зоны, для которой он вызван,
            // либо его нужно вызывать из CalculateCostAndLength.
            // Давайте предположим, что FindOptimalRebarSolution обновил this.Cost для potentialMergedZone
            // (Если это не так, его нужно доработать или вызывать CalculateCostAndLength здесь)

            // Пересчитаем стоимость для объединенной зоны, чтобы быть уверенными
            potentialMergedZone.CalculateCostAndLength(openings, plateBoundary); // Это обновит potentialMergedZone.Cost и .Length

            double costBenefit = (this.Cost + otherZone.Cost) - potentialMergedZone.Cost;

            // Выгода от уменьшения количества зон (уменьшаем на 1 зону)
            // Если zoneReductionBenefitFactor = 0, то учитывается только выгода по стоимости.
            double totalBenefit = costBenefit + zoneReductionBenefitFactor;

            System.Diagnostics.Debug.WriteLine(
                $"Расчет выгоды для объединения зон (точки: {string.Join(",", this.Points.Select(p => p.Number))} и {string.Join(",", otherZone.Points.Select(p => p.Number))}): " +
                $"Cost_A={this.Cost:F2}, Cost_B={otherZone.Cost:F2}, Cost_Merged={potentialMergedZone.Cost:F2}. " +
                $"Выгода по стоимости: {costBenefit:F2} руб. " +
                $"Бонус за сокращение зон: {zoneReductionBenefitFactor:F2}. " +
                $"Общая выгода: {totalBenefit:F2} руб."
            );

            return totalBenefit;
        }


        /// <summary>
        /// Метод для проверки геометрической возможности объединения текущей зоны с другой зоной.
        /// Проверяет, не пересекает ли потенциальный прямоугольник объединенной зоны отверстия
        /// и находится ли он внутри границ плиты.
        /// </summary>
        /// <param name="otherZone">Другая зона для объединения.</param>
        /// <param name="openings">Список контуров отверстий в плите (в футах).</param>
        /// <param name="plateBoundary">Внешний контур плиты (в футах).</param>
        /// <returns>True, если объединение геометрически возможно, иначе False.</returns>
        public bool IsMergeGeometricallyPossible(Zone otherZone, List<CurveLoop> openings, CurveLoop plateBoundary)
        {
            // 1. Определить потенциальные границы объединенной зоны
            double minX = Math.Min(this.Bounds.Min.X, otherZone.Bounds.Min.X);
            double minY = Math.Min(this.Bounds.Min.Y, otherZone.Bounds.Min.Y);
            double maxX = Math.Max(this.Bounds.Max.X, otherZone.Bounds.Max.X);
            double maxY = Math.Max(this.Bounds.Max.Y, otherZone.Bounds.Max.Y);

            // Создаем BoundingBoxXYZ потенциальной объединенной зоны. Z Bounds можно взять из одной из зон.
            // Предполагаем, что зоны находятся примерно на одной высоте.
            //BoundingBoxXYZ potentialBounds = new BoundingBoxXYZ
            //{
            //    Min = new XYZ(minX, minY, this.Bounds.Min.Z),
            //    Max = new XYZ(maxX, maxY, this.Bounds.Max.Z)
            //};

            
            // Получаем 4 угловые точки потенциального прямоугольника в плоскости XY
            double checkZ = this.Bounds.Min.Z; // Используем Z из одной из зон (можно использовать ZCenter или другой подходящий Z)
            XYZ p1 = new XYZ(minX, minY, 0); // Z-координата не важна для планарной проверки
            XYZ p2 = new XYZ(maxX, minY, 0);
            XYZ p3 = new XYZ(maxX, maxY, 0);
            XYZ p4 = new XYZ(minX, maxY, 0);

            List<XYZ> corners = new List<XYZ> { p1, p2, p3, p4 };

            // 2. Проверить, находится ли каждая угловая точка внутри внешнего контура плиты
            // и снаружи всех контуров отверстий.
            if (plateBoundary == null)
            {
                // Если нет внешнего контура плиты, считаем, что геометрическое объединение невозможно
                System.Diagnostics.Debug.WriteLine("Геометрическая проверка: Внешний контур плиты не найден.");
                return false;
            }

            foreach (var corner in corners)
            {
                // Проверяем, находится ли угол внутри внешнего контура плиты
                if (!ReinforcementInputWindow.IsPointInsideCurveLoop(plateBoundary, corner)) // Используем public static метод
                {
                    System.Diagnostics.Debug.WriteLine($"Геометрическая проверка: Угол {corner} вне внешнего контура плиты.");
                    return false; // Если хоть один угол вне плиты, объединение невозможно
                }

                // Проверяем, находится ли угол внутри любого отверстия
                if (openings != null)
                {
                    foreach (var openingLoop in openings)
                    {
                        if (ReinforcementInputWindow.IsPointInsideCurveLoop(openingLoop, corner)) // Используем public static метод
                        {
                            System.Diagnostics.Debug.WriteLine($"Геометрическая проверка: Угол {corner} внутри отверстия.");
                            return false; // Если хоть один угол внутри отверстия, объединение невозможно
                        }
                    }
                }
            }

            // 3. Дополнительная проверка на пересечение ребер потенциального прямоугольника
            // с ребрами отверстий и внешнего контура плиты.
            // Этот шаг более сложный и требует проверки пересечений между CurveLoop'ами.

            // Создаем CurveLoop для потенциального прямоугольника
            CurveLoop potentialBoundsLoop = new CurveLoop();
            try
            {
                XYZ p1_loop = new XYZ(minX, minY, checkZ);
                XYZ p2_loop = new XYZ(maxX, minY, checkZ);
                XYZ p3_loop = new XYZ(maxX, maxY, checkZ);
                XYZ p4_loop = new XYZ(minX, maxY, checkZ);

                potentialBoundsLoop.Append(Line.CreateBound(p1_loop, p2_loop));
                potentialBoundsLoop.Append(Line.CreateBound(p2_loop, p3_loop));
                potentialBoundsLoop.Append(Line.CreateBound(p3_loop, p4_loop));
                potentialBoundsLoop.Append(Line.CreateBound(p4_loop, p1_loop));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Геометрическая проверка: Ошибка при создании CurveLoop для потенциальных границ: {ex.Message}");
                // Если не удалось создать CurveLoop, считаем, что объединение невозможно (или нужна другая обработка)
                return false;
            }

            // Получаем сегменты потенциального прямоугольника
            List<Curve> potentialSegments = potentialBoundsLoop.ToList();

            // Проверка пересечения с внешним контуром плиты
            List<Curve> plateSegments = plateBoundary.ToList();
            // Проверка пересечения с внешним контуром плиты
            //IntersectionResultArray intersectionResults;
            foreach (var potentialSegment in potentialSegments)
            {
                foreach (var plateSegment in plateSegments)
                {
                    IntersectionResultArray intersectionResults;
                    // Используем метод Intersect класса Curve
                    SetComparisonResult intersectResult = potentialSegment.Intersect(plateSegment, out intersectionResults);

                    // Если есть любое пересечение, которое не является просто касанием в конечной точке,
                    // считаем, что есть пересечение контуров.
                    // SetComparisonResult.Overlap или SetComparisonResult.Subset/Superset (хотя для прямых это маловероятно)
                    // SetComparisonResult.Disjoint означает отсутствие пересечений.
                    // SetComparisonResult.Equal означает полное совпадение (тоже маловероятно для разных объектов).
                    // SetComparisonResult.Overlap означает частичное перекрытие или пересечение.
                    // SetComparisonResult.Subset/Superset означает, что одна кривая является частью другой.

                    // Для простоты, если результат не Disjoint, считаем, что есть пересечение.
                    // Это может быть слишком строго и не учитывать касания.
                    // Более точная проверка может потребовать анализа точек пересечения в intersectionResults.
                    // Пока используем упрощенную проверку.
                    if (intersectResult != SetComparisonResult.Disjoint)
                    {
                        // Если есть пересечение, и оно не является простым касанием в угловой точке (что уже проверено),
                        // то прямоугольник пересекает границу плиты.
                        // Учитывая, что мы уже проверили, что все углы внутри плиты, любое другое пересечение ребер
                        // с границей плиты означает, что прямоугольник "вылезает" за границу или касается ее
                        // не в углу, что для нашей задачи недопустимо.
                        System.Diagnostics.Debug.WriteLine("Геометрическая проверка: Обнаружено пересечение ребер потенциальных границ с внешним контуром плиты.");
                        return false;
                    }
                }
            }

            //if (potentialBoundsLoop.Intersect(plateBoundary, out intersectionResults) != SetComparisonResult.Disjoint)
            //{
            //    // Если есть пересечения, это может означать, что прямоугольник перекрывает границу или касается ее.
            //    // Поскольку мы уже проверили углы нахождение внутри плиты, пересечение ребер может быть допустимо
            //    // только если это совпадение ребер или касание. Более точная проверка сложна.
            //    // Для начала, давайте считать любое пересечение ребер с границей плиты недопустимым,
            //    // если только это не полное совпадение контуров (что маловероятно).
            //    // Можно добавить допуск для сравнения, но это усложнит проверку.
            //    // Если углы внутри, и есть пересечение, это подозрительно.
            //    // В рамках упрощения, если углы внутри и нет пересечений с отверстиями,
            //    // и нет пересечений с границей плиты, считаем, что прямоугольник внутри.

            //    // TODO: Более надежная проверка пересечения ребер с внешним контуром плиты.
            //    // Если IntersectionResult.Overlap или другое значимое пересечение, return false.
            //    // Пока пропускаем детальную проверку ребер с границей плиты, полагаясь на проверку углов.
            //    System.Diagnostics.Debug.WriteLine("Геометрическая проверка: Обнаружено пересечение потенциальных границ с внешним контуром плиты. Требуется доработка проверки.");
            //    // Временно, если углы внутри, считаем, что с границей плиты все ОК (нужна доработка!)
            //}


            // Проверка пересечения с каждым отверстием
            if (openings != null)
            {
                foreach (var openingLoop in openings)
                {
                    //if (potentialBoundsLoop.Intersect(openingLoop, out intersectionResults) != SetComparisonResult.Disjoint)
                    //{
                    //    // Если есть любое пересечение ребер потенциального прямоугольника с ребрами отверстия,
                    //    // это недопустимо.
                    //    System.Diagnostics.Debug.WriteLine($"Геометрическая проверка: Обнаружено пересечение потенциальных границ с отверстием.");
                    //    return false;
                    //}
                    List<Curve> openingSegments = openingLoop.ToList();
                    foreach (var potentialSegment in potentialSegments)
                    {
                        foreach (var openingSegment in openingSegments)
                        {
                            IntersectionResultArray intersectionResults;
                            // Используем метод Intersect класса Curve
                            SetComparisonResult intersectResult = potentialSegment.Intersect(openingSegment, out intersectionResults);

                            // Если есть любое пересечение (кроме Disjoint), считаем, что прямоугольник
                            // пересекает отверстие, что недопустимо.
                            if (intersectResult != SetComparisonResult.Disjoint)
                            {
                                System.Diagnostics.Debug.WriteLine($"Геометрическая проверка: Обнаружено пересечение ребер потенциальных границ с отверстием.");
                                return false;
                            }
                        }
                    }
                }
            }


            // Если все проверки пройдены (углы внутри плиты и вне отверстий, нет пересечений с отверстиями)
            System.Diagnostics.Debug.WriteLine("Геометрическая проверка: Объединение геометрически возможно.");
            return true;
        }


        /// <summary>
        /// Метод для расчета стоимости и длины арматуры для зоны с конкретной конфигурацией
        /// для заданного направления. Учитывает отверстия.
        /// </summary>
        /// <param name="rebarConfig">Конфигурация арматуры (диаметр, шаг, стоимость, нагрузка).</param>
        /// <param name="directionIndex">Индекс направления (0: As1X, 1: As2X, 2: As3Y, 3: As4Y).</param>
        /// <param name="openings">Список контуров отверстий в плите (в футах).</param>
        /// <returns>Пара: (Стоимость, Длина арматуры) для данного направления в зоне.</returns>
        private (double cost, double length) CalculateRebarCostAndLengthForDirection(RebarConfig rebarConfig, int directionIndex, List<CurveLoop> openings)
        {
            // Проверяем, что конфигурация арматуры предоставлена
            if (rebarConfig == null)
            {
                System.Diagnostics.Debug.WriteLine($"Расчет стоимости: RebarConfig для направления {directionIndex} не предоставлен.");
                return (0, 0); // Если нет арматуры, стоимость и длина равны 0
            }

            // Определяем размеры прямоугольной зоны по X и Y (в футах)
            double zoneLengthX_ft = Bounds.Max.X - Bounds.Min.X;
            double zoneLengthY_ft = Bounds.Max.Y - Bounds.Min.Y;

            // Определяем длину зоны по направлению армирования и перпендикулярному направлению
            // в зависимости от directionIndex.
            double lengthOfRebar_ft; // Длина одного стержня
            double widthForSpacing_ft; // Ширина зоны для определения количества стержней

            if (directionIndex == 0 || directionIndex == 1) // Направления As1X, As2X (армирование по X)
            {
                lengthOfRebar_ft = zoneLengthX_ft;
                widthForSpacing_ft = zoneLengthY_ft;
            }
            else // Направления As3Y, As4Y (арmiрование по Y)
            {
                lengthOfRebar_ft = zoneLengthY_ft;
                widthForSpacing_ft = zoneLengthX_ft;
            }

            // Проверяем, что размеры зоны положительны
            if (lengthOfRebar_ft <= 0 || widthForSpacing_ft <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"Расчет стоимости: Некорректные размеры зоны для направления {directionIndex}. Длина: {lengthOfRebar_ft}, Ширина: {widthForSpacing_ft}");
                return (0, 0);
            }

            // Конвертируем Шаг арматуры из мм в футы Revit API (1 фут = 304.8 мм)
            double spacing_ft = rebarConfig.Spacing / 304.8;

            // Проверяем, что шаг арматуры положительный
            if (spacing_ft <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"Расчет стоимости: Некорректный шаг арматуры ({rebarConfig.Spacing} мм) для направления {directionIndex}.");
                return (0, 0);
            }


            // Рассчитываем необходимое количество стержней в данном направлении
            // Количество стержней = (Ширина зоны / Шаг) + 1 (для первого стержня)
            int numberOfRebars = (int)Math.Ceiling(widthForSpacing_ft / spacing_ft) + 1;

            // Рассчитываем общую длину арматуры в данном направлении без учета отверстий (в футах)
            double totalLengthWithoutOpenings_ft = numberOfRebars * lengthOfRebar_ft;

            // === Учет отверстий ===
            // Рассчитываем площадь прямоугольной зоны (в кв. футах)
            double zoneArea_ft2 = zoneLengthX_ft * zoneLengthY_ft;

            // Рассчитываем общую площадь отверстий, попадающих внутрь зоны (в кв. футах)
            double areaOfOpeningsInZone_ft2 = 0;
            if (openings != null)
            {
                // TODO: Реализовать точный расчет площади пересечения каждого отверстия с прямоугольником зоны.
                // Сейчас используется упрощенная заглушка.
                // Для точного расчета потребуется геометрическая библиотека или функции API для вычисления площади пересечения CurveLoop'ов.
                // Пока оставим 0, что означает, что отверстия не влияют на расчет длины/стоимости.
                // Это место требует доработки для корректного учета отверстий.
                System.Diagnostics.Debug.WriteLine($"Расчет стоимости: Учет отверстий пока не реализован полностью. Площадь отверстий в зоне = {areaOfOpeningsInZone_ft2} кв. футов.");
            }

            // Рассчитываем процент "чистой" площади зоны (без отверстий)
            // Если площадь зоны 0 или меньше (что не должно произойти при положительных размерах), процент = 0
            double cleanAreaPercentage = (zoneArea_ft2 > 0) ? (zoneArea_ft2 - areaOfOpeningsInZone_ft2) / zoneArea_ft2 : 0;

            // Пропорционально уменьшаем общую длину арматуры на процент площади отверстий
            // Это упрощенный подход. Более точный - вычислять длину стержней, проходящих через "чистые" области.
            double totalLengthWithOpenings_ft = totalLengthWithoutOpenings_ft * cleanAreaPercentage;

            // TODO: Учет фиксированной длины стержней из CSV и отходов
            // В вашем CSV указана "Стоимость за 1 м". Предполагается, что стоимость указана за погонный метр
            // без учета отходов при раскрое стандартных стержней фиксированной длины.
            // Сложная оптимизация раскроя (например, с использованием линейного программирования) не включена в данный алгоритм.
            // Расчет стоимости идет просто по общей требуемой длине арматуры.

            // Конвертируем Стоимость за 1 м (руб/м) в руб/фут (1 фут = 0.3048 м)
            double costPerFoot = rebarConfig.CostPerMeter * 0.3048; // 1 метр = ~3.28 фута, 1 фут = ~0.3048 метра

            // Рассчитываем общую стоимость арматуры в данном направлении (в рублях)
            double totalCost = totalLengthWithOpenings_ft * costPerFoot;

            System.Diagnostics.Debug.WriteLine($"Расчет стоимости: Направление {directionIndex}. Конфигурация: {rebarConfig.Name} (D={rebarConfig.Diameter}, S={rebarConfig.Spacing}, Load={rebarConfig.BearingCapacity}). Длина зоны: {lengthOfRebar_ft:F2}x{widthForSpacing_ft:F2} футов. Шаг (футы): {spacing_ft:F4}. Стержней: {numberOfRebars}. Длина без отверстий: {totalLengthWithoutOpenings_ft:F2} футов. Чистая площадь %: {cleanAreaPercentage:P}. Длина с отверстиями: {totalLengthWithOpenings_ft:F2} футов. Стоимость/фут: {costPerFoot:F2} руб/фут. Общая стоимость: {totalCost:F2} руб.");


            return (totalCost, totalLengthWithOpenings_ft);
        }


        public void CalculateCostAndLength(List<CurveLoop> openings, CurveLoop plateBoundary)
        {
            // Сбрасываем предыдущие значения
            Cost = 0;
            Length = 0;

            // Проверяем наличие необходимых данных
            // OptimalRebarConfigs должен быть массивом из 4 элементов, даже если некоторые null
            if (Bounds == null || OptimalRebarConfigs == null || OptimalRebarConfigs.Length != 4 || MaxRequiredLoad == null || MaxRequiredLoad.Length != 4)
            {
                System.Diagnostics.Debug.WriteLine($"Zone: Невозможно рассчитать стоимость/длину для зоны из-за отсутствия Bounds, OptimalRebarConfigs (или не 4 элемента) или MaxRequiredLoad.");
                return;
            }

            // Получаем размеры зоны из Bounds в футах
            double dx = Bounds.Max.X - Bounds.Min.X;
            double dy = Bounds.Max.Y - Bounds.Min.Y;

            // Перебираем каждое из четырех направлений армирования
            // Индексы: 0 - As1X, 1 - As2X, 2 - As3Y, 3 - As4Y
            for (int i = 0; i < 4; i++)
            {
                RebarConfig rebarConfig = OptimalRebarConfigs[i];
                double requiredLoad = MaxRequiredLoad[i];

                // Проверяем, требуется ли армирование по данному направлению (нагрузка > 0)
                // и определена ли оптимальная конфигурация
                if (rebarConfig != null && requiredLoad > 0)
                {
                    double lengthOfRebar_ft;     // Длина отдельного стержня в этом направлении (в футах)
                    double widthForSpacing_ft;   // Размер зоны перпендикулярно направлению армирования (для расчета количества стержней) (в футах)

                    // Определяем размеры в зависимости от направления
                    if (i == 0 || i == 1) // Направления X (As1X, As2X)
                    {
                        lengthOfRebar_ft = dx;
                        widthForSpacing_ft = dy;
                    }
                    else // Направления Y (As3Y, As4Y)
                    {
                        lengthOfRebar_ft = dy;
                        widthForSpacing_ft = dx;
                    }

                    // Получаем шаг армирования в футах (предполагаем, что RebarConfig.Spacing в мм)
                    double spacing_mm = rebarConfig.Spacing;
                    // Используем UnitTypeId.Millimeters для Revit API 2021+
                    double spacing_ft = UnitUtils.ConvertToInternalUnits(spacing_mm, UnitTypeId.Millimeters);

                    // Проверка на нулевой или отрицательный шаг, чтобы избежать деления на ноль
                    if (spacing_ft <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Zone: Некорректный шаг армирования ({spacing_mm} мм) для конфигурации {rebarConfig.Name} в направлении {i}. Расчет пропущен.");
                        continue; // Пропускаем это направление
                    }


                    // Рассчитываем количество стержней. Округляем вверх, чтобы покрыть всю ширину.
                    // Добавляем небольшое эпсилон для учета погрешностей вычислений с плавающей точкой.
                    int numberOfRebars = (int)Math.Ceiling(widthForSpacing_ft / spacing_ft + 1e-9);

                    // TODO: Учет отверстий.
                    // Это сложная часть. Необходимо определить, какие части стержней
                    // попадают в отверстия и вычесть их длину.
                    // На данном этапе для упрощения будем использовать полную длину стержня.
                    // В реальном проекте здесь потребуется более сложная геометрическая логика
                    // для пересечения линий арматуры с контурами отверстий.
                    double totalLengthForDirection_ft = numberOfRebars * lengthOfRebar_ft;

                    // Получаем стоимость за фут (предполагаем, что RebarConfig.CostPerMeter в руб/м)
                    double costPerMeter = rebarConfig.CostPerMeter;
                    // Конвертируем руб/м в руб/фут (1 фут = 0.3048 м)
                    double costPerFoot = costPerMeter * 0.3048; // 1 метр = ~3.28 фута, 1 фут = ~0.3048 метра


                    // Рассчитываем стоимость арматуры для данного направления (в рублях)
                    double costForDirection = totalLengthForDirection_ft * costPerFoot;

                    // Добавляем к общей стоимости и длине зоны
                    Cost += costForDirection;
                    Length += totalLengthForDirection_ft; // Суммируем длины по всем направлениям
                                                          // (это общая погонная длина всей арматуры в зоне)

                    System.Diagnostics.Debug.WriteLine($"Zone: Расчет для направления {i}. Конфигурация: {rebarConfig.Name} (D={rebarConfig.Diameter}, S={rebarConfig.Spacing}). Треб. нагрузка: {requiredLoad:F2}. Длина стержня: {lengthOfRebar_ft:F2} футов. Ширина для шага: {widthForSpacing_ft:F2} футов. Шаг (футы): {spacing_ft:F4}. Стержней: {numberOfRebars}. Длина направления: {totalLengthForDirection_ft:F2} футов. Стоимость направления: {costForDirection:F2} руб.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Zone: Направление {i} пропущено. RebarConfig: {rebarConfig?.Name ?? "null"}, Треб. нагрузка: {requiredLoad:F2}.");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Zone: Общая рассчитанная Стоимость: {Cost:F2} руб, Общая Длина: {Length:F2} футов.");
        }


    }
}
