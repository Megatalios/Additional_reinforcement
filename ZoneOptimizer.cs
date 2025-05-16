using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom_Project
{
    public class ZoneOptimizer
    {
        // === Поля класса ZoneOptimizer (входные данные и настройки) ===

        /// <summary>
        /// Список доступных конфигураций арматуры.
        /// </summary>
        public List<RebarConfig> AvailableRebars { get; set; }

        /// <summary>
        /// Список контуров отверстий в плите (в футах).
        /// </summary>
        public List<CurveLoop> Openings { get; set; }

        /// <summary>
        /// Внешний контур плиты (в футах).
        /// </summary>
        public CurveLoop PlateBoundary { get; set; }

        /// <summary>
        /// Максимальное количество решений для поиска.
        /// </summary>
        public int MaxSolutions { get; set; }

        /// <summary>
        /// Пороговые значения основного армирования по направлениям (-1 если не выбрано).
        /// Индексы: 0 - As1X, 1 - As2X, 2 - As3Y, 3 - As4Y.
        /// </summary>
        public double[] BasicReinforcement { get; set; }

        // TODO: Добавить другие необходимые поля (например, стандартные длины стержней, параметры алгоритма)
        // public List<double> StandardLengths { get; set; }
        // Параметр для начального размера зоны вокруг точки (в футах)
        private const double InitialZoneSize = 0.1; // 0.1 фута (примерно 3 см)

        // Порог выгоды для необязательных слияний (если выгода меньше этого порога, слияние не происходит)
        private const double MergeBenefitThreshold = 0.01; // Например, 1 копейка


        // === Основной метод оптимизации ===

        /// <summary>
        /// Выполняет поиск N лучших решений по зонированию армирования.
        /// </summary>
        /// <param name="nodesForOptimization">Список узлов, нуждающихся в армировании (сгруппированных по плитам, если оптимизация идет по плитам).</param>
        /// <param name="floor">Плита, для которой выполняется оптимизация (если оптимизация по плитам).</param>
        /// <returns>Список лучших найденных решений ReinforcementSolution.</returns>
        /// 
        public List<ReinforcementSolution> FindBestReinforcementSolutions(List<Node> nodesForOptimization, Floor floor)
        {
            // Список для хранения N лучших найденных решений
            List<ReinforcementSolution> bestSolutions = new List<ReinforcementSolution>();

            // Проверяем входные данные
            if (nodesForOptimization == null || nodesForOptimization.Count == 0 || AvailableRebars == null || AvailableRebars.Count == 0 || PlateBoundary == null)
            {
                System.Diagnostics.Debug.WriteLine("ZoneOptimizer: Недостаточно входных данных для оптимизации.");
                return bestSolutions; // Возвращаем пустой список, если данных нет
            }

            // === 1. Инициализация: Создать начальные зоны (каждая Node = Zone). ===
            List<Zone> activeZones = new List<Zone>();
            foreach (var node in nodesForOptimization)
            {
                Zone initialZone = new Zone(node, InitialZoneSize); // Создаем начальную зону из точки
                // Для каждой начальной зоны сразу подбираем оптимальное арматурное решение
                // Передаем ей доступные конфигурации арматуры, отверстия и контур плиты
                initialZone.FindOptimalRebarSolution(AvailableRebars, Openings, PlateBoundary); // TODO: Передавать Openings и PlateBoundary в Zone.FindOptimalRebarSolution

                // Проверяем, удалось ли найти решение для начальной зоны (если Cost не double.MaxValue)
                if (initialZone.Cost < double.MaxValue)
                {
                    activeZones.Add(initialZone); // Добавляем зону в список активных, если для нее найдено решение
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Для начальной зоны из точки {node.Number} не найдено подходящего арматурного решения. Зона пропущена.");
                }
            }

            // Если после инициализации нет активных зон, прерываем работу
            if (activeZones.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("ZoneOptimizer: После инициализации нет активных зон.");
                return bestSolutions;
            }

            // Добавляем начальное решение (все точки как отдельные зоны) в список лучших решений
            // Создаем ReinforcementSolution из текущего набора активных зон
            ReinforcementSolution initialSolution = EvaluateSolution(activeZones);
            UpdateBestSolutions(bestSolutions, initialSolution);


            // === 2. Итеративный цикл объединения зон ===
            // Цикл продолжается, пока есть потенциальные слияния, которые стоит рассмотреть.
            // Мы будем искать лучшего кандидата на слияние на каждой итерации.
            // Чтобы избежать бесконечного цикла и гарантировать завершение, можно ограничить количество итераций
            // или останавливаться, когда выгода от слияний становится очень маленькой.

            int iteration = 0;
            const int MaxIterations = 1000; // Ограничение на количество итераций (для безопасности)

            while (iteration < MaxIterations)
            {
                iteration++;
                System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Итерация {iteration}. Активных зон: {activeZones.Count}");

                // 2.1. Найти лучшего кандидата на слияние на текущей итерации
                // Этот метод будет искать пару зон с наибольшей выгодой или обязательным слиянием
                (Zone zoneA, Zone zoneB, double benefit, bool isMandatory) bestMergeCandidate = FindBestMergeCandidate(activeZones);

                // 2.2. Проверить, стоит ли выполнять слияние
                // Если benefit <= MergeBenefitThreshold (для необязательных слияний) и нет обязательных слияний,
                // или если не найдено ни одного кандидата, прекращаем цикл.
                if (bestMergeCandidate.zoneA == null || (!bestMergeCandidate.isMandatory && bestMergeCandidate.benefit <= MergeBenefitThreshold))
                {
                    System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Критерий остановки достигнут. Лучшая выгода: {bestMergeCandidate.benefit:F2} руб. Обязательное слияние: {bestMergeCandidate.isMandatory}.");
                    break; // Критерий остановки
                }

                // 2.3. Выполнить слияние выбранной пары зон
                Zone mergedZone = new Zone(bestMergeCandidate.zoneA, bestMergeCandidate.zoneB);

                // 2.4. Для новой объединенной зоны подобрать оптимальное арматурное решение
                // Передаем ей доступные конфигурации арматуры, отверстия и контур плиты
                mergedZone.FindOptimalRebarSolution(AvailableRebars, Openings, PlateBoundary); // TODO: Передавать Openings и PlateBoundary

                // Проверяем, удалось ли найти решение для объединенной зоны
                if (mergedZone.Cost < double.MaxValue)
                {
                    // 2.5. Обновить список активных зон
                    // Удаляем две исходные зоны и добавляем новую объединенную
                    activeZones.Remove(bestMergeCandidate.zoneA);
                    activeZones.Remove(bestMergeCandidate.zoneB);
                    activeZones.Add(mergedZone);

                    // 2.6. Оценить полученное решение (текущий набор активных зон)
                    ReinforcementSolution currentSolution = EvaluateSolution(activeZones);

                    // 2.7. Обновить список лучших решений (`bestSolutions`)
                    UpdateBestSolutions(bestSolutions, currentSolution);

                    System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Выполнено слияние зон. Новых активных зон: {activeZones.Count}. Стоимость текущего решения: {currentSolution.TotalCost:F2} руб.");
                }
                else
                {
                    // Если для объединенной зоны не найдено решения, это слияние недопустимо.
                    // Мы не должны были дойти до этого шага, если IsMergeGeometricallyPossible
                    // и логика FindBestMergeCandidate работают корректно, но добавим на всякий случай.
                    System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Для потенциально объединенной зоны не найдено решения. Слияние отменено.");
                    // В этом случае мы не обновляем список activeZones и не добавляем решение.
                    // Цикл может продолжиться с другими парами.
                }

            } // Конец итеративного цикла

            // 3. Финальная обработка: bestSolutions содержит N лучших найденных решений.
            // Они уже отсортированы по стоимости внутри UpdateBestSolutions.

            System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Оптимизация завершена. Найдено {bestSolutions.Count} лучших решений.");

            return bestSolutions;
        }


        private (Zone zoneA, Zone zoneB, double benefit, bool isMandatory) FindBestMergeCandidate(List<Zone> activeZones)
        {
            Zone bestZoneA = null;
            Zone bestZoneB = null;
            double maxBenefit = double.NegativeInfinity; // Ищем максимальную выгоду
            bool foundMandatoryMerge = false; // Флаг для обязательного слияния

            // Проходим по всем возможным парам зон
            for (int i = 0; i < activeZones.Count; i++)
            {
                for (int j = i + 1; j < activeZones.Count; j++)
                {
                    Zone zoneA = activeZones[i];
                    Zone zoneB = activeZones[j];

                    // 1. Геометрическая проверка: возможно ли объединение?
                    // Передаем отверстия и контур плиты
                    if (!zoneA.IsMergeGeometricallyPossible(zoneB, Openings, PlateBoundary)) // TODO: Передавать Openings и PlateBoundary
                    {
                        continue; // Если геометрически невозможно, пропускаем эту пару
                    }

                    // 2. Проверка на перекрытие (обязательное слияние)
                    // Проверяем, перекрываются ли BoundingBox'ы зон
                    //bool areOverlapping = zoneA.Bounds.IntersectsBox(zoneB.Bounds); // Используем метод IntersectsBox
                    bool areOverlapping =
                        zoneA.Bounds.Min.X <= zoneB.Bounds.Max.X && zoneA.Bounds.Max.X >= zoneB.Bounds.Min.X &&
                        zoneA.Bounds.Min.Y <= zoneB.Bounds.Max.Y && zoneA.Bounds.Max.Y >= zoneB.Bounds.Min.Y &&
                        zoneA.Bounds.Min.Z <= zoneB.Bounds.Max.Z && zoneA.Bounds.Max.Z >= zoneB.Bounds.Min.Z;

                    if (areOverlapping)
                    {
                        // Если перекрываются, это обязательное слияние.
                        // Присваиваем очень высокую выгоду, чтобы оно было выбрано с приоритетом.
                        // Точную выгоду для обязательного слияния можно рассчитать, но для приоритета достаточно большого числа.
                        double mandatoryBenefit = double.PositiveInfinity; // Очень большая выгода для обязательного слияния

                        // Если мы нашли обязательное слияние, ищем среди них лучшее (например, по количеству объединенных точек)
                        // Или просто берем первое найденное обязательное слияние на текущей итерации как "лучшее"
                        if (!foundMandatoryMerge) // Если это первое найденное обязательное слияние на этой итерации
                        {
                            bestZoneA = zoneA;
                            bestZoneB = zoneB;
                            maxBenefit = mandatoryBenefit;
                            foundMandatoryMerge = true;
                        }
                        // TODO: Если нужно выбирать лучшее среди обязательных, добавьте здесь логику сравнения (например, по количеству точек)

                        // Если мы уже нашли обязательное слияние, ищем только среди них.
                        // Если текущее слияние тоже обязательное, и оно "лучше" по какому-то критерию, обновляем bestCandidate.
                        // Пока просто берем первое найденное обязательное как лучшее на этой итерации.

                    }
                    else if (!foundMandatoryMerge) // Если нет обязательных слияний на этой итерации, ищем выгодное
                    {
                        // 3. Расчет выгоды по стоимости (для необязательных слияний)
                        // Передаем доступные конфигурации арматуры, отверстия и контур плиты
                        double currentBenefit = zoneA.CalculateMergeBenefit(zoneB, AvailableRebars, Openings, PlateBoundary); // TODO: Передавать AvailableRebars, Openings, PlateBoundary

                        // 4. Сравнение с максимальной найденной выгодой
                        if (currentBenefit > maxBenefit)
                        {
                            maxBenefit = currentBenefit;
                            bestZoneA = zoneA;
                            bestZoneB = zoneB;
                            foundMandatoryMerge = false; // Убеждаемся, что это не обязательное слияние
                        }
                    }
                }
            }

            // Возвращаем найденного лучшего кандидата
            return (bestZoneA, bestZoneB, maxBenefit, foundMandatoryMerge);
        }

        /// <summary>
        /// Оценивает текущий набор активных зон как одно решение.
        /// </summary>
        /// <param name="currentZones">Список текущих активных зон.</param>
        /// <returns>Объект ReinforcementSolution, представляющий текущее решение.</returns>
        private ReinforcementSolution EvaluateSolution(List<Zone> currentZones)
        {
            // TODO: Реализовать оценку решения.
            // 1. Суммировать стоимость всех зон в currentZones.
            // 2. Создать ReinforcementSolution.
            // 3. Заполнить ReinforcementSolution данными (список зон, общая стоимость, номер решения).

            double totalCost = currentZones.Sum(zone => zone.Cost);
            double totalLength = currentZones.Sum(zone => zone.Length); // Суммируем общую длину

            // Создаем список ZoneSolution из текущих зон (для ReinforcementSolution)
            List<ZoneSolution> zoneSolutions = currentZones.Select(zone => new ZoneSolution // Предполагаем, что ZoneSolution определен
            {
                // TODO: Заполнить свойства ZoneSolution данными из Zone
                // Например:
                // Boundary = zone.Bounds, // Нужно преобразовать BoundingBoxXYZ в подходящий тип для ZoneSolution
                // Diameter = (zone.OptimalRebarConfigs != null && zone.OptimalRebarConfigs[0] != null) ? zone.OptimalRebarConfigs[0].Diameter : 0, // Пример для одного направления
                // Spacing = (zone.OptimalRebarConfigs != null && zone.OptimalRebarConfigs[0] != null) ? zone.OptimalRebarConfigs[0].Spacing : 0, // Пример для одного направления
                // ZoneCost = zone.Cost,
                // ZoneLength = zone.Length
                // ... другие свойства ...
            }).ToList();


            ReinforcementSolution solution = new ReinforcementSolution // Предполагаем, что ReinforcementSolution определен
            {
                Zones = zoneSolutions, // Список зон в этом решении
                TotalCost = totalCost, // Общая стоимость решения
                TotalLength = totalLength, // Общая длина арматуры
                Num = 0 // TODO: Присвоить уникальный номер решения (например, по порядку нахождения)
            };

            System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Оценено решение с {currentZones.Count} зонами. Общая стоимость: {totalCost:F2} руб.");

            return solution;
        }

        /// <summary>
        /// Обновляет список лучших решений, добавляя новое решение, если оно лучше
        /// текущего худшего решения, и поддерживая список отсортированным.
        /// </summary>
        /// <param name="bestSolutions">Список лучших решений.</param>
        /// <param name="currentSolution">Текущее оцениваемое решение.</param>
        private void UpdateBestSolutions(List<ReinforcementSolution> bestSolutions, ReinforcementSolution currentSolution)
        {
            // Проверяем, что текущее решение имеет конечную стоимость (не double.MaxValue)
            if (currentSolution.TotalCost >= double.MaxValue)
            {
                System.Diagnostics.Debug.WriteLine("ZoneOptimizer: Текущее решение имеет бесконечную стоимость. Не добавляется в лучшие решения.");
                return; // Не добавляем нерешаемые зоны
            }

            // Если список еще не заполнен до MaxSolutions, просто добавляем решение
            if (bestSolutions.Count < MaxSolutions)
            {
                bestSolutions.Add(currentSolution);
                System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Добавлено решение #{bestSolutions.Count} в список лучших. Стоимость: {currentSolution.TotalCost:F2} руб.");
            }
            else
            {
                // Если список заполнен, сравниваем текущее решение с худшим (самым дорогим)
                // Находим худшее решение (последнее после сортировки по возрастанию стоимости)
                ReinforcementSolution worstSolution = bestSolutions.LastOrDefault(); // Требует, чтобы bestSolutions был отсортирован

                if (worstSolution != null && currentSolution.TotalCost < worstSolution.TotalCost)
                {
                    // Если текущее решение лучше худшего, удаляем худшее и добавляем текущее
                    bestSolutions.Remove(worstSolution);
                    bestSolutions.Add(currentSolution);
                    System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Заменено худшее решение (стоимость {worstSolution.TotalCost:F2}) на новое (стоимость {currentSolution.TotalCost:F2}).");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ZoneOptimizer: Текущее решение (стоимость {currentSolution.TotalCost:F2}) не лучше худшего в списке ({worstSolution?.TotalCost:F2}). Не добавляется.");
                }
            }

            // Сортируем список лучших решений по возрастанию общей стоимости
            bestSolutions.Sort((s1, s2) => s1.TotalCost.CompareTo(s2.TotalCost));

            // TODO: Присвоить номера решениям после сортировки, если Num используется для отображения
            for (int i = 0; i < bestSolutions.Count; i++)
            {
                bestSolutions[i].Num = i + 1;
            }
        }



        // TODO: Добавить вспомогательные методы для алгоритма (если нужны)
        // private List<Zone> InitializeZones(List<Node> nodes) { ... } // Создание начальных зон
        // private (Zone zoneA, Zone zoneB, double benefit, bool isMandatory) FindBestMergeCandidate(List<Zone> activeZones) { ... } // Поиск лучшей пары
        // private ReinforcementSolution EvaluateSolution(List<Zone> currentZones) { ... } // Оценка текущего набора зон
        // private void UpdateBestSolutions(List<ReinforcementSolution> bestSolutions, ReinforcementSolution currentSolution) { ... } // Обновление списка лучших решений


        // TODO: Возможно, методы для получения геометрии плиты и отверстий
        // (хотя мы решили получать их в ReinforcementInputWindow и передавать в Optimizer)
        // public static List<CurveLoop> GetOpeningsCurveLoops(Floor floor) { ... }
        // public static CurveLoop GetPlateBoundary(Floor floor) { ... }
    }
}
