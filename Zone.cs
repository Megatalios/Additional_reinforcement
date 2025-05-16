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
        public void FindOptimalRebarSolution(List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary)
        {
            // Инициализируем общую стоимость и длину зоны
            this.Cost = 0;
            this.Length = 0;

            // Инициализируем массив подобранных конфигураций
            this.OptimalRebarConfigs = new RebarConfig[4];

            // Перебираем каждое из четырех направлений армирования
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                // Проверяем, требуется ли армирование по данному направлению
                // Если MaxRequiredLoad <= 0 или равно -1 (если направление не выбрано), пропускаем
                if (MaxRequiredLoad[directionIndex] <= 0 || MaxRequiredLoad[directionIndex] == -1)
                {
                    OptimalRebarConfigs[directionIndex] = null; // Явно указываем, что арматура по этому направлению не нужна
                    continue; // Переходим к следующему направлению
                }

                // Ищем лучшую конфигурацию арматуры для текущего направления
                RebarConfig bestConfigForDirection = null;
                double minCostForDirection = double.MaxValue; // Изначально ставим очень высокую стоимость

                // Перебираем все доступные конфигурации арматуры
                if (availableRebars != null)
                {
                    foreach (var currentConfig in availableRebars)
                    {
                        // Проверяем, удовлетворяет ли выдерживаемая нагрузка конфигурации требуемой нагрузке зоны по данному направлению
                        // Предполагаем, что MaxRequiredLoad и BearingCapacity в одних единицах (кН/м²)
                        if (currentConfig.BearingCapacity >= MaxRequiredLoad[directionIndex])
                        {
                            // Если конфигурация подходит, рассчитываем ее стоимость и длину для зоны
                            (double currentCost, double calculatedLength) = CalculateRebarCostAndLengthForDirection(currentConfig, directionIndex, openings);

                            // Сравниваем с текущей минимальной стоимостью для этого направления
                            if (currentCost < minCostForDirection)
                            {
                                minCostForDirection = currentCost;
                                bestConfigForDirection = currentConfig;
                            }
                        }
                    }
                }

                // После перебора всех конфигураций, сохраняем лучшую для данного направления
                OptimalRebarConfigs[directionIndex] = bestConfigForDirection;

                // Если найдена подходящая конфигурация (bestConfigForDirection не null), добавляем ее стоимость и длину к общим
                if (bestConfigForDirection != null)
                {
                    // Пересчитываем стоимость и длину для найденной лучшей конфигурации
                    // (это нужно, потому что minCostForDirection и calculatedLength были рассчитаны внутри цикла)
                    (double finalCost, double finalLength) = CalculateRebarCostAndLengthForDirection(bestConfigForDirection, directionIndex, openings);

                    this.Cost += finalCost;
                    this.Length += finalLength;
                }
                else
                {
                    // Если для данного направления не найдена ни одна подходящая конфигурация,
                    // это означает, что зона не может быть полностью армирована доступной арматурой.
                    // Присваиваем зоне очень высокую общую стоимость, чтобы она не считалась оптимальным решением.
                    System.Diagnostics.Debug.WriteLine($"Подбор арматуры: Для направления {directionIndex} зоны с точками {string.Join(",", Points.Select(p => p.Number))} не найдена подходящая конфигурация арматуры. Присвоена очень высокая стоимость.");
                    this.Cost = double.MaxValue;
                    this.Length = double.MaxValue; // Также присваиваем высокую длину (опционально)
                    // Можно прервать подбор для других направлений этой зоны, т.к. решение уже не будет полным
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Подбор арматуры: Для зоны с точками {string.Join(",", Points.Select(p => p.Number))}. Общая стоимость: {this.Cost:F2} руб, Общая длина: {this.Length:F2} футов.");
        }

        /// <summary>
        /// Метод для расчета "выгодности" объединения текущей зоны с другой зоной.
        /// Учитывает потенциальную экономию.
        /// </summary>
        /// <param name="otherZone">Другая зона для объединения.</param>
        /// <param name="availableRebars">Список доступных конфигураций арматуры (RebarConfig).</param>
        /// <param name="openings">Список контуров отверстий в плите (в футах).</param>
        /// <param name="plateBoundary">Внешний контур плиты (в футах).</param>
        /// <returns>Потенциальная экономия от объединения (руб). Отрицательное значение означает удорожание.</returns>
        public double CalculateMergeBenefit(Zone otherZone, List<RebarConfig> availableRebars, List<CurveLoop> openings, CurveLoop plateBoundary)
        {
            // 1. Создать временную объединенную зону (используя конструктор Zone(zoneA, zoneB)).
            Zone potentialMergedZone = new Zone(this, otherZone);

            // 2. Для временной зоны подобрать оптимальное арматурное решение, вызвав ее метод FindOptimalRebarSolution(...).
            // Важно: FindOptimalRebarSolution обновит Cost и Length временной зоны
            potentialMergedZone.FindOptimalRebarSolution(availableRebars, openings, plateBoundary);

            // 3. Рассчитать выгоду = (this.Cost + otherZone.Cost) - Стоимость временной зоны.
            // Если FindOptimalRebarSolution вернул double.MaxValue (не найдено решение),
            // то выгода будет отрицательной и очень большой, что правильно.
            double mergeBenefit = (this.Cost + otherZone.Cost) - potentialMergedZone.Cost;

            System.Diagnostics.Debug.WriteLine($"Расчет выгоды: Объединение зон с точками {string.Join(",", this.Points.Select(p => p.Number))} и {string.Join(",", otherZone.Points.Select(p => p.Number))}. Выгода: {mergeBenefit:F2} руб.");

            return mergeBenefit;
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


    }
}
