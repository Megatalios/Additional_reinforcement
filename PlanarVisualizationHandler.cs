using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom_Project
{
    public class PlanarVisualizationHandler : IExternalEventHandler // Определение класса
    {
        /// <summary>
        /// Ссылка на UIDocument для доступа к текущему документу Revit.
        /// Это свойство необходимо для выполнения операций с документом в потоке Revit API.
        /// </summary>
        public UIDocument UiDocument { get; set; } // Добавлено свойство UiDocument
        public const double METERS_TO_FEET = 3.28084;
        /// <summary>
        /// Список решений зон для визуализации.
        /// Эти данные передаются из окна WPF перед вызовом ExternalEvent.
        /// </summary>
        public List<ZoneSolution> ZonesToVisualize { get; set; }

        /// <summary>
        /// Идентификатор элемента плиты, к которой относятся зоны.
        /// Нужен для привязки линий детализации.
        /// </summary>
        public ElementId FloorId { get; set; } // TODO: Возможно, это поле не нужно, если зоны уже содержат ссылку на плиту или ее ID. Проверить необходимость.

        public void Execute(UIApplication app)
        {
            // Проверяем, что UIDocument доступен
            if (UiDocument == null)
            {
                TaskDialog.Show("Ошибка визуализации", "UIDocument недоступен в обработчике PlanarVisualizationHandler.");
                System.Diagnostics.Debug.WriteLine("PlanarVisualizationHandler: UIDocument недоступен.");
                return;
            }

            // Используем UIDocument для получения доступа к текущему документу
            Document doc = UiDocument.Document;

            // Получаем активный вид
            View activeView = doc.ActiveView;
            ViewPlan planView = activeView as ViewPlan;

            // Проверяем, что активный вид является планом этажа
            if (planView == null)
            {
                TaskDialog.Show("Ошибка визуализации", "Активный вид не является планом этажа.");
                System.Diagnostics.Debug.WriteLine("PlanarVisualizationHandler: Активный вид не является планом этажа.");
                return;
            }

            // Проверяем, есть ли зоны для визуализации
            if (ZonesToVisualize == null || ZonesToVisualize.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("PlanarVisualizationHandler: Нет зон для визуализации.");
                // Возможно, здесь нужно очистить предыдущую визуализацию, если она была
                // TODO: Реализовать очистку предыдущей визуализации
                return;
            }

            // TODO: Реализовать очистку предыдущей визуализации перед рисованием новой
            // Возможно, CleanHandler должен уметь удалять DetailCurve, созданные этим обработчиком.
            // Или PlanarVisualizationHandler должен сам отслеживать и удалять свои предыдущие элементы.


            // Пример: Создание транзакции для внесения изменений в модель
            using (Transaction trans = new Transaction(doc, "Visualize Reinforcement Zones on Plan"))
            {
                trans.Start();

                try
                {
                    // Получаем уровень плана для определения Z-координаты линий
                    // Если у плана есть связанный уровень, используем его отметку
                    double viewLevelElevation = 0.0; // Значение по умолчанию
                    if (planView.GenLevel != null)
                    {
                        Level viewLevel = doc.GetElement(planView.GenLevel.Id) as Level;
                        if (viewLevel != null)
                        {
                            // Переводим отметку уровня из футов в футы проекта (обычно одно и то же, но лучше быть точным)
                            // viewLevelElevation = UnitUtils.ConvertFromInternalUnits(viewLevel.Elevation, DisplayUnitType.DUT_FEET); // Если проектные единицы не футы
                            viewLevelElevation = viewLevel.Elevation; // Если проектные единицы футы
                        }
                    }
                    // Если у плана нет связанного уровня или не удалось его получить,
                    // можно использовать Z-координату из Bounds зон, но это может быть некорректно
                    // для отображения на конкретном плане. Лучше использовать отметку уровня плана.

                    System.Diagnostics.Debug.WriteLine("PlanarVisualizationHandler: Выполняется 2D визуализация на плане...");

                    // Логика рисования контуров зон на плане
                    foreach (ZoneSolution zoneSolution in ZonesToVisualize)
                    {
                        // Проверяем, что у зоны есть границы
                        if (zoneSolution.Bounds != null)
                        {
                            BoundingBoxXYZ bounds = zoneSolution.Bounds;

                            // Создаем CurveLoop, представляющий прямоугольник BoundingBox в плоскости XY
                            // Используем отметку уровня плана для Z-координаты точек
                            // Важно: Revit API работает в футах, убедитесь, что координаты bounds.Min и bounds.Max тоже в футах
                            XYZ p1 = new XYZ(bounds.Min.X/ METERS_TO_FEET, bounds.Min.Y/ METERS_TO_FEET, viewLevelElevation);
                            XYZ p3 = new XYZ(bounds.Max.X/METERS_TO_FEET, bounds.Max.Y/METERS_TO_FEET, viewLevelElevation);
                            XYZ p2 = new XYZ(bounds.Max.X/METERS_TO_FEET, bounds.Min.Y/METERS_TO_FEET, viewLevelElevation);
                            XYZ p4 = new XYZ(bounds.Min.X/ METERS_TO_FEET, bounds.Max.Y/METERS_TO_FEET, viewLevelElevation);

                            CurveLoop zoneOutline = new CurveLoop();
                            try
                            {
                                zoneOutline.Append(Line.CreateBound(p1, p2));
                                zoneOutline.Append(Line.CreateBound(p2, p3));
                                zoneOutline.Append(Line.CreateBound(p3, p4));
                                zoneOutline.Append(Line.CreateBound(p4, p1));

                                // Создаем линии детализации для каждого сегмента контура
                                foreach (Curve curve in zoneOutline)
                                {
                                    // Проверяем, что кривая действительна
                                    if (curve != null && curve.Length > 0)
                                    {
                                        doc.Create.NewDetailCurve(planView, curve);
                                    }
                                }
                                System.Diagnostics.Debug.WriteLine($"PlanarVisualizationHandler: Нарисован контур зоны с границами Min({bounds.Min.X:F2},{bounds.Min.Y:F2}) Max({bounds.Max.X:F2},{bounds.Max.Y:F2}).");
                            }
                            catch (Exception loopEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"PlanarVisualizationHandler: Ошибка при создании CurveLoop или DetailCurve для зоны: {loopEx.Message}");
                                // Продолжаем рисовать другие зоны, если возможно
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("PlanarVisualizationHandler: У зоны нет определенных границ (Bounds == null). Пропускается визуализация.");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("PlanarVisualizationHandler: 2D визуализация на плане завершена.");

                    trans.Commit(); // Завершаем транзакцию
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlanarVisualizationHandler: Ошибка при выполнении 2D визуализации: {ex.Message}");
                    TaskDialog.Show("Ошибка визуализации", $"Произошла ошибка при визуализации зон: {ex.Message}");
                    trans.RollBack(); // Откатываем изменения при ошибке
                }
            }
        }

        public string GetName()
        {
            return "Reinforcement Zone Planar Visualization Handler";
        }
    }
}
