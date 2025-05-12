using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Xml.Linq;

namespace Diplom_Project
{

    /// <summary>
    /// Обработчик ExternalEvent для визуализации точек в модели Revit.
    /// </summary>
    public class VisualizationHandler : IExternalEventHandler
    {
        // Поля для хранения данных, которые нужно передать в обработчик
        // Списки точек, которые нужно визуализировать
        public List<List<Additional_Reinforcement_point>> PointsInsideFloors { get; set; }
        public List<Additional_Reinforcement_point> PointsOutsideFloors { get; set; }

        // Ссылка на UIDocument для доступа к текущему документу
        public UIDocument UiDocument { get; set; }

        // Метод, который будет вызван ExternalEvent'ом в потоке Revit API
        public void Execute(UIApplication app)
        {
            // Проверяем, что UIDocument доступен
            if (UiDocument == null) return;

            Document doc = UiDocument.Document;

            // === Логика создания графических элементов в Revit ===
            // Эта логика должна выполняться внутри транзакции
            using (Transaction trans = new Transaction(doc, "Visualize Points"))
            {
                trans.Start();

                try
                {
                    // TODO: Реализовать логику создания DirectShape или других элементов для визуализации точек.
                    // Используйте PointsInsideFloors и PointsOutsideFloors.
                    // Создайте разные категории или цвета для точек внутри и вне плит.

                    // Пример: Создание DirectShape для точек (нужна адаптация)
                    // Вам потребуется получить или создать подходящие ElementId для категории, материала и т.д.
                    // ElementId modelCategoryId = new ElementId(BuiltInCategory.OST_GenericModel); // Пример категории

                    // Визуализация точек внутри плит (например, синим цветом)
                    if (PointsInsideFloors != null)
                    {
                        foreach (var slabPointsList in PointsInsideFloors)
                        {
                            foreach (var pointData in slabPointsList)
                            {
                                // TODO: Создать графический элемент для pointData (например, DirectShape)
                                // Используйте pointData.GetXYZ(scaleFactor) для получения координат в футах Revit
                                // Пример (требует доработки):
                                // XYZ pointXYZ_ft = pointData.GetXYZ(ReinforcementInputWindow.METERS_TO_FEET); // Нужен доступ к METERS_TO_FEET
                                // DirectShape ds = DirectShape.CreateElement(doc, modelCategoryId);
                                // ds.SetShape(new GeometryObject[] { Point.Create(pointXYZ_ft) });
                                // ds.Name = $"Point_{pointData.Number}_Inside";
                                // TODO: Назначить цвет или материал
                            }
                        }
                    }

                    // Визуализация точек вне плит (например, красным цветом)
                    if (PointsOutsideFloors != null)
                    {
                        foreach (var pointData in PointsOutsideFloors)
                        {
                            // TODO: Создать графический элемент для pointData (например, DirectShape)
                            // Используйте pointData.GetXYZ(scaleFactor) для получения координат в футах Revit
                            // Пример (требует доработки):
                            // XYZ pointXYZ_ft = pointData.GetXYZ(ReinforcementInputWindow.METERS_TO_FEET); // Нужен доступ к METERS_TO_FEET
                            // DirectShape ds = DirectShape.CreateElement(doc, modelCategoryId);
                            // ds.SetShape(new GeometryObject[] { Point.Create(pointXYZ_ft) });
                            // ds.Name = $"Point_{pointData.Number}_Outside";
                            // TODO: Назначить другой цвет или материал
                        }
                    }

                    // TODO: Добавить логику для создания DirectShape или других элементов

                    trans.Commit(); // Завершаем транзакцию
                }
                catch (Exception ex)
                {
                    // Откатываем транзакцию при ошибке
                    if (trans.HasStarted())
                    {
                        trans.RollBack();
                    }
                    // Показываем сообщение об ошибке (может быть TaskDialog, так как мы в потоке Revit)
                    TaskDialog.Show("Ошибка визуализации", $"Произошла ошибка при визуализации точек: {ex.Message}");
                }
            }
        }

        // Метод, возвращающий имя обработчика (для отладки)
        public string GetName()
        {
            return "Point Visualization Handler";
        }
    }

    /// <summary>
    /// Обработчик ExternalEvent для удаления визуализированных точек из модели Revit.
    /// </summary>
    public class CleanHandler : IExternalEventHandler
    {
        // Ссылка на UIDocument для доступа к текущему документу
        public UIDocument UiDocument { get; set; }

        // Метод, который будет вызван ExternalEvent'ом в потоке Revit API
        public void Execute(UIApplication app)
        {
            if (UiDocument == null) return;

            Document doc = UiDocument.Document;

            // === Логика удаления графических элементов в Revit ===
            // Эта логика должна выполняться внутри транзакции
            using (Transaction trans = new Transaction(doc, "Clean Visualization"))
            {
                trans.Start();

                try
                {
                    // TODO: Реализовать логику поиска и удаления ранее созданных графических элементов.
                    // Например, искать DirectShape по имени или по параметру, который вы им назначили.

                    // Пример: Поиск и удаление DirectShape (нужна адаптация)
                    // FilteredElementCollector collector = new FilteredElementCollector(doc)
                    //     .OfClass(typeof(DirectShape));

                    // List<ElementId> elementsToDelete = new List<ElementId>();
                    // foreach (Element elem in collector)
                    // {
                    //     // TODO: Проверить, является ли элемент одним из тех, что вы создали для визуализации
                    //     // Например, по имени или по наличию определенного параметра
                    //     // if (elem.Name.StartsWith("Point_")) // Пример проверки по имени
                    //     // {
                    //     //     elementsToDelete.Add(elem.Id);
                    //     // }
                    // }

                    // if (elementsToDelete.Count > 0)
                    // {
                    //     doc.Delete(elementsToDelete); // Удаляем найденные элементы
                    // }


                    trans.Commit(); // Завершаем транзакцию
                }
                catch (Exception ex)
                {
                    // Откатываем транзакцию при ошибке
                    if (trans.HasStarted())
                    {
                        trans.RollBack();
                    }
                    // Показываем сообщение об ошибке
                    TaskDialog.Show("Ошибка очистки визуализации", $"Произошла ошибка при удалении точек: {ex.Message}");
                }
            }
        }

        // Метод, возвращающий имя обработчика
        public string GetName()
        {
            return "Clean Visualization Handler";
        }
    }
    /// <summary>
    /// Логика взаимодействия для ReinforcementInputWindow.xaml
    /// </summary>
    //public partial class ReinforcementInputWindow : Window
    //{
    //    public ReinforcementInputWindow()
    //    {
    //        InitializeComponent();
    //    }
    //}
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

        // Конструктор (опционально, если нужно создавать Node напрямую, но обычно создается через Linq Select)
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

    /// <summary>
    /// Скелет класса для представления решения по зоне армирования.
    /// </summary>
    public class ZoneSolution
    {
        // public Rectangle Boundary { get; set; } // Границы зоны (нужно определить класс Rectangle или использовать BoundingBoxXYZ)
        public double Diameter { get; set; } // Диаметр арматуры в зоне
        public double Spacing { get; set; } // Шаг арматуры в зоне
        public double ZoneCost { get; set; } // Стоимость арматуры в зоне
        public double ZoneLength { get; set; } // Длина арматуры в зоне
        // ... другие свойства ...
    }

    /// <summary>
    /// Скелет класса для представления полного решения (набора зон) для плиты.
    /// </summary>
    public class ReinforcementSolution
    {
        public List<ZoneSolution> Zones { get; set; } // Список зон в этом решении
        public double TotalCost { get; set; } // Общая стоимость решения
        public double TotalLength { get; set; } // Общая длина арматуры
        public int Num { get; set; } // Порядковый номер решения

        // public double FitnesCost { get; set; } // Показатель приспособленности для GA (если используется)
        // ... другие свойства ...
    }

    /// <summary>
    /// Скелет класса для представления конфигурации арматуры (диаметр, шаги, цена).
    /// </summary>
    public class RebarConfig
    {
        public int Diameter { get; set; } // Диаметр
        public List<double> AvailableSpacings { get; set; } // Список доступных шагов для этого диаметра
        public double PricePerMeter { get; set; } // Цена за метр
    }


    /// <summary>
    /// Скелет класса для расчетного алгоритма (оптимизатора).
    /// </summary>
    public class ReinforcementOptimizer
    {
        // public List<List<XYZ>> Openings { get; set; } // Отверстия в плитах (если нужны оптимизатору)
        public int NumOfSol { get; set; } // Количество решений для поиска
        public double[] BasicReinforcement { get; set; } // Массив порогов для As1X, As2X, As3Y, As4Y (-1 если не выбрано)
        public List<double> StandardLengths { get; set; } // Стандартные длины стержней
        public List<RebarConfig> AvailableRebars { get; set; } // Доступные конфигурации арматуры
        public List<List<XYZ>> Openings { get; set; } // Отверстия в плитах (если нужны оптимизатору)
        // public double FitnesCoef { get; set; } = 100000; // Коэффициент для штрафа за количество зон (если используется GA)

        // Скелет метода для получения отверстий из Revit
        // Этот метод должен быть реализован и, вероятно, работать в контексте Revit API
        public static List<List<XYZ>> GetOpeningsFromRevit(List<Floor> floors)
        {
            // TODO: Реализовать логику получения контуров отверстий из плит
            // Вам нужно будет получить геометрию каждой плиты, найти грани отверстий
            // и преобразовать их в список списков XYZ или аналогичную структуру.
            // Убедитесь, что координаты отверстий также в футах Revit.
            return new List<List<XYZ>>(); // Возвращаем пустой список как заглушку
        }

        // Скелет основного метода поиска лучших решений
        // Этот метод должен быть реализован
        // Он принимает узлы (сгруппированные по плитам), количество решений и плиты
        public List<ReinforcementSolution> FindBestSolutions(List<List<Node>> nodesForOptimizer, int maxSolutions, List<Floor> floors)
        {
            // TODO: Реализовать логику расчетного алгоритма (кластеризация, генетический алгоритм и т.д.)
            // Используйте входные данные (nodesForOptimizer, maxSolutions, floors, Openings, BasicReinforcement, StandardLengths, AvailableRebars)
            // для поиска оптимальных зон армирования.
            // Возвращайте список найденных решений ReinforcementSolution.

            // Пример: Возвращаем пустой список как заглушку
            return new List<ReinforcementSolution>();
        }

        // TODO: Добавить другие вспомогательные методы оптимизатора, если они есть в референсе (например, CreateOptimalZone, MergeZones, CalculateCost, CheckIntersection и т.д.)
    }
    public partial class ReinforcementInputWindow : Window // Наследуется от Window, так как это отдельное окно
    {
        // Ссылка на UIDocument, переданная из команды AboutShow
        // Позволяет получить доступ к текущему документу Revit и его UI
        private UIDocument uiDocument;
        // Ссылка на Document (база данных проекта)
        private Document document;

        // --- Поле для хранения ВСЕХ точек, загруженных из CSV ---
        // Здесь будут храниться все точки после загрузки файла, до фильтрации
        private List<Additional_Reinforcement_point> allCsvPoints;
        // -----------------------------------------------------

        private List<List<Additional_Reinforcement_point>> pointsInsideFloorsGrouped;
        // Точки, которые находятся вне плит
        private List<Additional_Reinforcement_point> pointsOutsideFloors;

        // Поле для отображения результатов расчета в UI DataGrid
        private DataTable ZonesTable;
        private List <Floor> floors; // Список плит, найденных на уровне Revit
        private double scaleFactor = 1.0;
        private const double METERS_TO_FEET = 3.28084;


        private ExternalEvent visualizationEvent;
        private VisualizationHandler visualizationHandler;

        private ExternalEvent cleanEvent;
        private CleanHandler cleanHandler;


        /// <summary>
        /// Конструктор окна UI.
        /// </summary>
        /// <param name="uidoc">Текущий UIDocument из Revit.</param>
        public ReinforcementInputWindow(UIDocument uidoc)
        {
            InitializeComponent(); // Инициализация UI компонентов из XAML (генерируется автоматически)

            // Сохраняем переданные ссылки на объекты Revit
            uiDocument = uidoc;
            document = uiDocument.Document;

            // === Инициализация списков для хранения отфильтрованных точек ===
            // Инициализируем поля класса здесь
            pointsInsideFloorsGrouped = new List<List<Additional_Reinforcement_point>>();
            pointsOutsideFloors = new List<Additional_Reinforcement_point>();
            // ==============================================================

            // === Инициализация External Event и обработчиков ===
            // Создаем экземпляры обработчиков
            visualizationHandler = new VisualizationHandler();
            cleanHandler = new CleanHandler();

            // Создаем экземпляры ExternalEvent, связывая их с обработчиками
            visualizationEvent = ExternalEvent.Create(visualizationHandler);
            cleanEvent = ExternalEvent.Create(cleanHandler);

            // Передаем UIDocument обработчикам, чтобы они могли работать с текущим документом
            visualizationHandler.UiDocument = uiDocument;
            cleanHandler.UiDocument = uiDocument;
            // ==================================================

            try
            {
                // Получаем все элементы типа Level в документе
                List<Level> allLevels = new FilteredElementCollector(document)
                    .OfClass(typeof(Level)) // Фильтруем по классу Level
                    .Cast<Level>() // Приводим найденные элементы к типу Level
                    .OrderBy(level => level.Elevation) // Опционально: сортируем уровни по высоте
                    .ToList(); // Преобразуем в список

                // Добавляем имена уровней в ItemsSource ComboBox'а
                // ItemsSource лучше, чем Items.Add, если вы используете привязку данных,
                // но для простого списка имен Items.Add тоже работает.
                // LevelComboBox.ItemsSource = allLevels.Select(level => level.Name).ToList();

                // Добавляем имена уровней в ComboBox
                foreach (Level level in allLevels)
                {
                    LevelComboBox.Items.Add(level.Name);
                }

                // Опционально: выбрать первый элемент по умолчанию, если список не пуст
                if (LevelComboBox.Items.Count > 0)
                {
                    LevelComboBox.SelectedIndex = 0;
                }

                // Опционально: загрузить последний выбранный уровень из настроек
                // string lastLevelName = Properties.Settings.Settings.Default.FlrName;
                // if (LevelComboBox.Items.Contains(lastLevelName))
                // {
                //     LevelComboBox.SelectedItem = lastLevelName;
                // }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка уровней из Revit: {ex.Message}", "Ошибка Revit API", MessageBoxButton.OK, MessageBoxImage.Error);
                // Деактивировать ComboBox или кнопку расчета, если уровни не загружены
                LevelComboBox.IsEnabled = false;
            }

            // Инициализация таблицы DataTable для DataGrid
            ZonesTable = new DataTable();
            InitializeDataTables(); // Метод инициализации структуры таблицы DataTable



            // Изначально кнопки расчета и визуализации неактивны
            CalculateButton.IsEnabled = false;
            ApplyButton.IsEnabled = false;
            PlanarButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            // Привязка DataGrid к DataTable для отображения результатов
            SolutionsView.ItemsSource = ZonesTable.DefaultView;

            // Подписка на событие закрытия окна для сохранения настроек
            this.Closed += ReinforcementInputWindow_Closed;
        }

        /// <summary>
        /// Метод инициализации структуры DataTable для отображения результатов.
        /// </summary>
        private void InitializeDataTables()
        {
            // Определение колонок для DataTable, которая будет источником данных для DataGrid SolutionsView
            // Пока используем простые колонки, соответствующие UI
            ZonesTable.Columns.Add("Count", typeof(int)); // Количество зон в решении
            ZonesTable.Columns.Add("TotalLength", typeof(double)); // Общая длина арматуры
            ZonesTable.Columns.Add("TotalCost", typeof(double)); // Общая стоимость
            ZonesTable.Columns.Add("Num", typeof(int)); // Порядковый номер решения
            ZonesTable.Columns.Add("Level", typeof(string)); // Уровень
            // Добавьте другие колонки, если они нужны для отображения результатов
        }


        // === Обработчики событий UI элементов (будут реализованы далее) ===

        /// <summary>
        /// Обработчик нажатия кнопки "Загрузить CSV".
        /// </summary>
        //private async void LoadCsvButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Логика загрузки CSV файла будет здесь
        //    // Она вызовет CsvFileReader.ReadAllPointsOfType
        //    // Сохранит результат в allCsvPoints
        //    // Активирует кнопку CalculateButton
        //    MessageBox.Show("Логика загрузки CSV будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
        //}

        private static bool IsPointInsideCurveLoop(CurveLoop loop, XYZ point)
        {
            // Реализация алгоритма "ray casting" (бросание луча) или аналогичного.
            // Проверяем, сколько раз луч из точки пересекает ребра полигона.
            // Нечетное количество пересечений означает, что точка внутри.

            // Для простоты, предположим, что контур лежит в плоскости XY (Z = константа)
            // и луч направлен вдоль положительной оси X.

            int crossings = 0;
            double pointX = point.X;
            double pointY = point.Y;
            double tolerance = 1e-9; // Допуск для сравнения координат

            // Итерируемся по всем кривым (ребрам) в контуре
            foreach (Curve curve in loop)
            {
                // Получаем начальную и конечную точки кривой
                XYZ p1 = curve.GetEndPoint(0);
                XYZ p2 = curve.GetEndPoint(1);

                // Проверяем, является ли ребро горизонтальным (параллельным оси X)
                if (Math.Abs(p1.Y - p2.Y) < tolerance)
                {
                    // Горизонтальное ребро не пересекает горизонтальный луч, пропускаем
                    continue;
                }

                // Проверяем, находится ли точка Y между Y-координатами концов ребра
                // (Строго больше минимума и меньше или равно максимуму, чтобы избежать двойного подсчета вершин)
                if ((pointY >= Math.Min(p1.Y, p2.Y) && pointY < Math.Max(p1.Y, p2.Y)) ||
                    (pointY <= Math.Max(p1.Y, p2.Y) && pointY > Math.Min(p1.Y, p2.Y))) // Добавлена проверка для случая, когда луч проходит через вершину
                {
                    // Вычисляем X-координату точки пересечения луча с прямой, проходящей через ребро
                    // Уравнение прямой по двум точкам (x1, y1) и (x2, y2):
                    // (x - x1) / (x2 - x1) = (y - y1) / (y2 - y1)
                    // x = x1 + (y - y1) * (x2 - x1) / (y2 - y1)
                    double intersectX = p1.X + (pointY - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);

                    // Если точка пересечения находится справа от проверяемой точки (pointX),
                    // то луч пересекает ребро.
                    if (intersectX > pointX - tolerance) // Используем допуск для сравнения
                    {
                        crossings++;
                    }
                }
                // Дополнительная проверка: если точка Y совпадает с Y-координатой одной из вершин
                else if (Math.Abs(pointY - p1.Y) < tolerance)
                {
                    // Если точка Y совпадает с Y1, и Y2 находится выше Y1, а X2 правее X1,
                    // и точка X находится левее X1, то луч пересекает ребро, выходящее из этой вершины
                    if (p2.Y > p1.Y && p2.X > p1.X && pointX < p1.X)
                    {
                        crossings++;
                    }
                    // Аналогично, если Y2 ниже Y1, а X2 правее X1, и точка X левее X1
                    else if (p2.Y < p1.Y && p2.X > p1.X && pointX < p1.X)
                    {
                        crossings++;
                    }
                }
                else if (Math.Abs(pointY - p2.Y) < tolerance)
                {
                    // Если точка Y совпадает с Y2, и Y1 находится выше Y2, а X1 правее X2,
                    // и точка X находится левее X2, то луч пересекает ребро, выходящее из этой вершины
                    if (p1.Y > p2.Y && p1.X > p2.X && pointX < p2.X)
                    {
                        crossings++;
                    }
                    // Аналогично, если Y1 ниже Y2, а X1 правее X2, и точка X левее X2
                    else if (p1.Y < p2.Y && p1.X > p2.X && pointX < p2.X)
                    {
                        crossings++;
                    }
                }
            }

            // Если количество пересечений нечетное, точка находится внутри полигона.
            return (crossings % 2 == 1);
        }
        private async void LoadCsvButton_Click(object sender, RoutedEventArgs e)
        {
            // Используем Microsoft.Win32.OpenFileDialog для WPF
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            // Показываем диалог выбора файла
            if (openFileDialog.ShowDialog() == true) // ShowDialog() для Microsoft.Win32
            {
                string filePath = openFileDialog.FileName;
                FilePathTextBox.Text = filePath; // Отображаем путь к файлу в TextBox

                // === Определяем тип объекта для чтения из CSV ===
                // Судя по вашему примеру CSV, тип объекта - "Floor". Укажите нужный тип.
                string typeToRead = "Floor"; // Или "Node", если такие строки есть в вашем файле
                // ================================================

                // Деактивируем UI на время загрузки
                this.IsEnabled = false;
                CalculateButton.IsEnabled = false; // Кнопка расчета неактивна
                ApplyButton.IsEnabled = false;
                PlanarButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                ZonesTable.Clear(); // Очищаем предыдущие результаты в таблице
                // bestSolutions = null; // Очищаем предыдущие решения (если поле bestSolutions существует)


                // --- Вызываем метод чтения ВСЕХ точек заданного типа из CsvFileReader ---
                // Убедитесь, что CsvFileReader доступен (статический класс)
                // Убедитесь, что метод ReadAllPointsOfType возвращает ваш тип Additional_Reinforcement_point
                string csvErrorMessage = ""; // Переменная для получения сообщения об ошибках чтения строк
                // Выполняем чтение в фоновом потоке, чтобы не блокировать UI
                allCsvPoints = await Task.Run(() => CsvFileReader.ReadAllPointsOfType(filePath, typeToRead, out csvErrorMessage));
                // -----------------------------------------------------------------------

                // Активируем UI после загрузки
                this.IsEnabled = true;

                // Обрабатываем результат чтения
                if (allCsvPoints == null) // CsvFileReader вернул null при критической ошибке файла/структуры
                {
                    // Сообщение об ошибке уже показано внутри CsvFileReader.ReadAllPointsOfType (MessageBox)
                    MessageBox.Show("Не удалось загрузить точки из файла. Проверьте формат файла и сообщения об ошибках.", "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
                    allCsvPoints = null; // Убедимся, что список пуст
                    FilePathTextBox.Text = ""; // Очищаем путь файла в UI
                                               // Логируем ошибку (если доступен Tools.CreateLogMessage)
                                               // ConsoleLog.AppendText(Tools.CreateLogMessage(Tools.ErrLoadFit + ". Критическая ошибка чтения файла."));
                    return; // Выходим из метода
                }

                if (!string.IsNullOrEmpty(csvErrorMessage))
                {
                    // Показываем предупреждение о частичных ошибках парсинга строк
                    MessageBox.Show($"При чтении файла были пропущены строки из-за ошибок формата:\n{csvErrorMessage}", "Предупреждение при чтении CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Логируем предупреждение (если доступен Tools.CreateLogMessage)
                    // ConsoleLog.AppendText(Tools.CreateLogMessage(Tools.ErrLoadFit + ". Ошибки формата в строках."));
                }


                if (allCsvPoints.Count == 0)
                {
                    MessageBox.Show($"В файле не найдено ни одной точки типа '{typeToRead}', или все строки с этим типом содержали ошибки.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    CalculateButton.IsEnabled = false; // Кнопка расчета неактивна, если нет точек
                    allCsvPoints = null; // Очищаем список, если нет точек
                    FilePathTextBox.Text = ""; // Очищаем путь файла в UI
                                               // Логируем (если доступен Tools.CreateLogMessage)
                                               // ConsoleLog.AppendText(Tools.CreateLogMessage(Tools.SucLoadFit + ". В файле нет точек заданного типа."));
                    return; // Выходим из метода
                }

                // Если точки успешно загружены и их количество > 0
                CalculateButton.IsEnabled = true; // Активируем кнопку расчета
                MessageBox.Show($"Успешно загружено {allCsvPoints.Count} точек типа '{typeToRead}'.", "Загрузка завершена", MessageBoxButton.OK, MessageBoxImage.Information);

                // Логируем успех загрузки (если доступен Tools.CreateLogMessage)
                // ConsoleLog.AppendText(Tools.CreateLogMessage(Tools.SucLoadFit + $" ({allCsvPoints.Count} точек)"));
            }
        }

        
        [Obsolete]
        private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что исходные точки из CSV загружены
            if (allCsvPoints == null || allCsvPoints.Count == 0)
            {
                MessageBox.Show("Сначала загрузите точки из CSV файла.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Деактивируем UI на время выполнения фильтрации и подготовки
            this.IsEnabled = false;
            ApplyButton.IsEnabled = false;
            PlanarButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            ZonesTable.Clear(); // Очищаем предыдущие результаты в таблице
            // bestSolutions = null; // Очищаем предыдущие решения (если поле bestSolutions существует)
            floors = null; // Очищаем предыдущий список плит
            var pointsInsideFloorsGrouped = new List<List<Additional_Reinforcement_point>>();
            var pointsOutsideFloors = new List<Additional_Reinforcement_point>();


            // === 1. Получение параметров из UI ===

            // Получаем значение основного армирования (порог)
            double mainFitValue = 0;
            if (!double.TryParse(ArmTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out mainFitValue))
            {
                MessageBox.Show("Некорректное значение основного армирования.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return;
            }

            // Получаем максимальное количество решений (пока не используется для фильтрации, но нужно для расчета)
            int maxSolutions = 0;
            if (!int.TryParse(MaxSolTextBox.Text, out maxSolutions) || maxSolutions <= 0)
            {
                MessageBox.Show("Некорректное значение максимального числа решений.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return;
            }

            // === Получаем выбранное имя уровня Revit из ComboBox ===
            string levelName = LevelComboBox.SelectedItem?.ToString();
            // ======================================================

            if (string.IsNullOrWhiteSpace(levelName))
            {
                MessageBox.Show("Не выбран уровень Revit.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return;
            }

            // Определяем, какие направления армирования выбраны пользователем
            bool useAs1X = As1X_CheckBox.IsChecked.HasValue ? As1X_CheckBox.IsChecked.Value : false;
            bool useAs2X = As2X_CheckBox.IsChecked.HasValue ? As2X_CheckBox.IsChecked.Value : false;
            bool useAs3Y = As3Y_CheckBox.IsChecked.HasValue ? As3Y_CheckBox.IsChecked.Value : false;
            bool useAs4Y = As4Y_CheckBox.IsChecked.HasValue ? As4Y_CheckBox.IsChecked.Value : false;

            // Проверяем, выбрано ли хотя бы одно направление
            if (!useAs1X && !useAs2X && !useAs3Y && !useAs4Y)
            {
                MessageBox.Show("Не выбрано ни одно направление для расчета.", "Ошибка выбора", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return;
            }

            // === 2. Фильтрация загруженных точек по значениям As* ===

            List<Additional_Reinforcement_point> filteredPoints = new List<Additional_Reinforcement_point>();

            filteredPoints = allCsvPoints
                // Фильтр по типу (например, исключаем "Wall", если нужно)
                // .Where(p => p.Type.ToLower() != "wall") // Раскомментируйте, если нужно исключать "Wall"
                .Where(p =>
                    // Проверяем условия для каждого выбранного направления
                    (useAs1X && p.As1X > mainFitValue) ||
                    (useAs2X && p.As2X > mainFitValue) ||
                    (useAs3Y && p.As3Y > mainFitValue) ||
                    (useAs4Y && p.As4Y > mainFitValue)
                )
                // !!! Важно: Здесь также добавьте фильтрацию по Z-координате, если она все еще нужна
                // Если нужно фильтровать точки, находящиеся на определенной Z-отметке (например, -9.67847769), добавьте:
                // Убедитесь, что точка.Z - это именно та координата, по которой нужно фильтровать
                // .Where(p => Math.Abs(p.Z - (-9.67847769028871)) < 0.001) // Пример вашей старой фильтрации по Z с допуском
                .ToList(); // Преобразуем результат фильтрации в новый список

            // Проверяем, остались ли точки после первой фильтрации
            if (filteredPoints.Count == 0)
            {
                MessageBox.Show("После фильтрации по основному армированию и выбранным направлениям не осталось узлов, нуждающихся в дополнительном армировании.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                this.IsEnabled = true; // Включаем UI обратно
                return; // Прерываем выполнение метода
            }

            // === 3. Поиск уровня и плит в Revit и геометрическая фильтрация точек ===

            // Этот блок кода ДОЛЖЕН ВЫПОЛНЯТЬСЯ В ОСНОВНОМ ПОТОКЕ REVIT API.
            // Поскольку CalculateButton_Click является async void, прямой вызов API здесь допустим,
            // но для длительных операций с API лучше использовать ExternalEvent.
            // Поиск уровня и плит обычно достаточно быстрый.

            try
            {
                // 3.1. Ищем элемент Level по выбранному имени
                Level targetLevel = new FilteredElementCollector(document)
                    .OfClass(typeof(Level)) // Ищем элементы класса Level
                    .Cast<Level>() // Приводим к типу Level
                    .FirstOrDefault(level => level.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase)); // Ищем по имени (без учета регистра)

                if (targetLevel == null)
                {
                    MessageBox.Show($"Уровень с именем '{levelName}' не найден в текущем документе Revit.", "Ошибка Revit API", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.IsEnabled = true;
                    return;
                }

                // 3.2. Ищем все плиты (Floor) на найденном уровне
                floors = new FilteredElementCollector(document)
                    .OfClass(typeof(Floor)) // Ищем элементы класса Floor
                    .WhereElementIsNotElementType() // Исключаем типы (остаются только экземпляры)
                    .Cast<Floor>() // Приводим к типу Floor
                    .Where(floor => floor.LevelId == targetLevel.Id) // Фильтруем по ID найденного уровня
                    .ToList(); // Преобразуем в список

                if (floors == null || floors.Count == 0)
                {
                    MessageBox.Show($"На уровне '{levelName}' не найдено плит перекрытия.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.IsEnabled = true;
                    return;
                }

                // 3.3. Определяем коэффициент конвертации единиц из CSV в футы Revit API.
                // ЭТО КРИТИЧНО ВАЖНО для правильных геометрических проверок и координат в объектах Node.
                // Предполагаем, что CSV в метрах, как в вашем примере.
                try
                {
                    // Получаем единицы длины документа Revit
                    Units projectUnits = document.GetUnits();
                    FormatOptions fo = projectUnits.GetFormatOptions(UnitType.UT_Length);
                    DisplayUnitType displayUnits = fo.DisplayUnits;

                    // Конвертируем из метров в футы (единицы Revit API)
                    scaleFactor = METERS_TO_FEET;

                    System.Diagnostics.Debug.WriteLine($"CSV Units Scale Factor (Meters to Feet): {scaleFactor}"); // Логируем коэффициент

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при определении или применении коэффициента конвертации единиц: {ex.Message}");
                    MessageBox.Show($"Ошибка при определении единиц проекта. Убедитесь, что единицы заданы корректно. Использование масштаба по умолчанию (1:1).", "Предупреждение о единицах", MessageBoxButton.OK, MessageBoxImage.Warning);
                    scaleFactor = 1.0; // Используем масштаб 1:1 как запасной вариант, если конвертация не удалась
                }


                // 3.4. Выполняем геометрическую фильтрацию и привязку точек к плитам
                // Создаем новый список для точек, которые находятся внутри плит
                List<Additional_Reinforcement_point> pointsInsideFloors = new List<Additional_Reinforcement_point>();

                // Вам потребуется адаптировать или реализовать метод IsPointInsideCurveLoop
                // и логику получения геометрии (верхней грани и контуров) из плит.
                // Эта логика, вероятно, находится в референсном Command.cs, возможно, в методе GetNodeTable или аналогичном.

                // Пример структуры для хранения точек, сгруппированных по плитам (для следующего шага - создания Node)
                var slabsNodes = new List<List<Additional_Reinforcement_point>>(); // Список списков узлов, сгруппированных по плитам (Node - класс для Optimizer)
                // Инициализируем список списков по количеству найденных плит
                for (int i = 0; i < floors.Count; i++)
                {
                    slabsNodes.Add(new List<Additional_Reinforcement_point>());
                }


                // Итерируемся по отфильтрованным точкам из CSV
                foreach (var pointData in filteredPoints)
                {
                    // Конвертируем координаты точки из CSV в футы Revit API
                    XYZ pointXYZ_ft = pointData.GetXYZ(scaleFactor); // Используем метод GetXYZ класса точки

                    // Проверяем каждую плиту на этом уровне
                    for (int i = 0; i < floors.Count; i++)
                    {
                        Floor floor = floors[i];

                        // --- Логика получения верхней грани и контуров плиты (адаптировано из референса) ---
                        // Этот код нужно будет адаптировать из референсного проекта (например, из Command.GetNodeTable)
                        // Он должен получить PlanarFace верхней грани и ее контуры (CurveLoop)

                        Options geomOptions = new Options();
                        geomOptions.ComputeReferences = true; // Важно для получения CurveLoop из граней
                        GeometryElement geomElem = floor.get_Geometry(geomOptions);
                        PlanarFace topFace = null;

                        foreach (GeometryObject geomObj in geomElem)
                        {
                            if (geomObj is Solid solid)
                            {
                                foreach (Face face in solid.Faces)
                                {
                                    if (face is PlanarFace planarFace)
                                    {
                                        if (planarFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) // Проверяем нормаль грани
                                        {
                                            topFace = planarFace;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (topFace != null) break;
                        }
                        // --- Конец логики получения верхней грани ---

                        if (topFace == null) continue; // Пропускаем плиту без верхней грани

                        IList<CurveLoop> boundaryLoops = topFace.GetEdgesAsCurveLoops();
                        if (boundaryLoops.Count == 0) continue; // Пропускаем плиту без контуров

                        CurveLoop outerBoundary = boundaryLoops[0]; // Внешний контур

                        // --- Логика проверки нахождения точки внутри контура (адаптировано из референса) ---
                        // Вам потребуется адаптировать или реализовать статический метод IsPointInsideCurveLoop(CurveLoop loop, XYZ point)
                        // Этот метод должен проверять, находится ли точка внутри полигона, заданного CurveLoop.

                        // Проверяем, находится ли точка внутри внешнего контура плиты
                        if (!IsPointInsideCurveLoop(outerBoundary, pointXYZ_ft)) continue; // Предполагаем доступ к Command.IsPointInsideCurveLoop

                        // Проверяем, не попадает ли точка в отверстие (контуры с индексом > 0)
                        bool isInsideHole = false;
                        for (int j = 1; j < boundaryLoops.Count; j++)
                        {
                            if (IsPointInsideCurveLoop(boundaryLoops[j], pointXYZ_ft)) // Предполагаем доступ к Command.IsPointInsideCurveLoop
                            {
                                isInsideHole = true;
                                break;
                            }
                        }
                        if (isInsideHole) continue; // Если точка в отверстии, пропускаем ее

                        // Если точка прошла все проверки, она находится внутри этой плиты
                        // Добавляем ее в список точек, которые будут использоваться для создания Node для этой плиты
                        // На этом этапе мы еще не создаем Node, просто группируем Additional_Reinforcement_point по плитам
                        // pointsInsideFloors.Add(pointData); // Этот список больше не нужен, если группируем сразу по плитам

                        // Добавляем отфильтрованную и привязанную точку в список для соответствующей плиты
                        // На следующем шаге эти Additional_Reinforcement_point будут преобразованы в Node
                        slabsNodes[i].Add(pointData);

                        // Точка найдена в одной плите, переходим к следующей отфильтрованной точке
                        break;
                    }
                }

                // Проверяем, остались ли точки после геометрической фильтрации и привязки к плитам
                // slabsNodes.Any(sn => sn.Count > 0) проверяет, есть ли хотя бы в одном списке внутри slabsNodes элементы
                int totalPointsInsideFloors = slabsNodes.Sum(sn => sn.Count);
                if (totalPointsInsideFloors == 0)
                {
                    MessageBox.Show($"После привязки к плитам на уровне '{levelName}' не осталось узлов, нуждающихся в дополнительном армировании.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.IsEnabled = true;
                    return;
                }

                // === 4. Преобразование Additional_Reinforcement_point в Node для каждой плиты ===
                // Этот шаг готовит данные для ReinforcementOptimizer
                // Вам потребуется адаптировать или реализовать класс Node
                // В этом цикле мы пройдемся по slabsNodes и создадим соответствующие объекты Node
                var nodesForOptimizer = new List<List<Node>>();

                // Определяем пороговые значения основного армирования для каждого направления (-1 если не выбрано)
                // Эти значения нужны для создания Node, так как Optimizer использует их для расчета requiredAs
                double as1xThreshold = useAs1X ? mainFitValue : -1;
                double as2xThreshold = useAs2X ? mainFitValue : -1;
                double as3yThreshold = useAs3Y ? mainFitValue : -1;
                double as4yThreshold = useAs4Y ? mainFitValue : -1;


                for (int i = 0; i < slabsNodes.Count; i++)
                {
                    List<Additional_Reinforcement_point> pointsOnThisSlab = slabsNodes[i];
                    List<Node> nodesForThisSlab = pointsOnThisSlab
                    .Select(p => new Node // Предполагаем, что класс Node определен (например, в Command.cs)
                        {
                            Type = p.Type,
                            Number = p.Number,
                            // Координаты в Node должны быть в футах, т.к. с ними работает Optimizer
                            X = p.x * scaleFactor,
                            Y = p.y * scaleFactor,
                            ZCenter = p.z * scaleFactor,
                            ZMin = p.ZMin * scaleFactor,

                            // Заполняем As* в Node. Если направление не выбрано пользователем (порог == -1),
                            // сохраняем -1 в Node для этого As*. Иначе сохраняем исходное значение точки.
                            As1X = (as1xThreshold == -1) ? -1 : p.As1X,
                            As2X = (as2xThreshold == -1) ? -1 : p.As2X,
                            As3Y = (as3yThreshold == -1) ? -1 : p.As3Y,
                            As4Y = (as4yThreshold == -1) ? -1 : p.As4Y,

                            SlabId = i // Индекс плиты в списке 'floors'
                        })
                        .ToList();

                    nodesForOptimizer.Add(nodesForThisSlab); // Добавляем список узлов для этой плиты
                }

                // Проверяем, есть ли узлы для оптимизации после привязки и преобразования
                if (!nodesForOptimizer.Any(nl => nl.Count > 0))
                {
                    MessageBox.Show($"После привязки к плитам и подготовки данных не осталось узлов для расчета.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.IsEnabled = true;
                    return;
                }


                // === 5. Получаем информацию об отверстиях для всех найденных плит ===
                // ReinforcementOptimizer.GetOpeningsFromRevit - этот метод, вероятно, должен быть доступен
                // Он должен принимать List<Floor> и возвращать List<List<XYZ>> или аналогичную структуру для отверстий
                // Этот метод также должен выполняться в контексте Revit API, если он работает с геометрией
                // Если он просто преобразует уже полученные CurveLoop, то может работать и в фоне.
                // Предполагаем, что он статический и доступен.
                var openings = ReinforcementOptimizer.GetOpeningsFromRevit(floors); // Получаем отверстия


                // === 6. Запускаем расчетный алгоритм (ReinforcementOptimizer) ===
                // Этот шаг может быть длительным и, возможно, должен выполняться в фоновом потоке,
                // но сам ReinforcementOptimizer не должен напрямую работать с Revit API.
                // Если ReinforcementOptimizer.FindBestSolutions не вызывает API, его можно обернуть в Task.Run.
                // Вам потребуется адаптировать или реализовать класс ReinforcementOptimizer и его зависимости (Node, ReinforcementSolution, RebarConfig).

                // Создаем экземпляр ReinforcementOptimizer
                // Вам нужно будет передать ему необходимые данные (отверстия, параметры арматуры, настройки)
                var optimizer = new ReinforcementOptimizer // Предполагаем, что класс ReinforcementOptimizer определен
                {
                    Openings = openings, // Передаем информацию об отверстиях
                    NumOfSol = maxSolutions, // Максимальное число решений
                                             // Передаем ПОРОГОВЫЕ значения основного армирования по выбранным направлениям
                    BasicReinforcement = new[] { as1xThreshold, as2xThreshold, as3yThreshold, as4yThreshold },
                    // Стандартные длины стержней и доступные конфигурации арматуры (диаметр-шаг-цена)
                    // Эти данные должны быть загружены из JSON при открытии окна (с помощью DataFile)
                    // Вам нужно будет адаптировать загрузку JSON и преобразование в нужные структуры (List<double> и List<RebarConfig>)
                    // StandardLengths = Length.AsEnumerable().Select(r => Convert.ToDouble(r["Length"])).ToList(), // Пример преобразования DataTable Length
                    // AvailableRebars = DiamStep.AsEnumerable() // Пример преобразования DiamStep и DiamCost
                    // .GroupBy(r => Convert.ToInt32(r["Diam"]))
                    // .Select(g => new RebarConfig
                    // {
                    //     Diameter = g.Key,
                    //     AvailableSpacings = g.Select(r => Convert.ToDouble(r["Step"])).ToList(),
                    //     PricePerMeter = DiamCost.AsEnumerable()
                    //     .FirstOrDefault(r => Convert.ToInt32(r["Diam"]) == g.Key)?["Cost"] != null
                    //        ? Convert.ToDouble(DiamCost.AsEnumerable()
                    //         .First(r => Convert.ToInt32(r["Diam"]) == g.Key)["Cost"]) : 0,
                    // })
                    // .Where(r => r.PricePerMeter > 0)
                    // .ToList()
                };

                // Вызываем основной метод оптимизатора для поиска лучших решений
                // Передаем списки узлов, сгруппированные по плитам, количество решений и сами плиты
                // bestSolutions = await Task.Run(() => optimizer.FindBestSolutions(nodesForOptimizer, maxSolutions, floors));
                // optimizer = null; // Очищаем оптимизатор после использования

                // === 7. Обработка и отображение результатов (будет реализована далее) ===
                // - Проверка, были ли найдены решения
                // - Заполнение ZonesTable данными из полученных решений bestSolutions
                // - Активация кнопок ApplyButton, PlanarButton, CancelButton

                MessageBox.Show($"Геометрическая фильтрация завершена. Найдено {totalPointsInsideFloors} точек внутри плит. Дальнейшая логика расчета и оптимизации будет реализована.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);


            }
            catch (Exception ex)
            {
                // Обработка любых ошибок, произошедших в блоке try (при поиске уровня/плит или геометрических проверках)
                MessageBox.Show($"Произошла ошибка при поиске плит или геометрической фильтрации: {ex.Message}", "Ошибка расчета", MessageBoxButton.OK, MessageBoxImage.Error);
                // Логирование ошибки (если доступен Tools.CreateLogMessage)
                // ConsoleLog.AppendText(Tools.CreateLogMessage(Tools.CalcErr + " '" + levelName + "'. Ошибка: " + ex.Message));

                // Деактивируем кнопки визуализации при ошибке
                ApplyButton.IsEnabled = false;
                PlanarButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                ZonesTable.Clear(); // Очищаем таблицу результатов при ошибке
                // bestSolutions = null; // Очищаем решения при ошибке
            }
            finally
            {
                this.IsEnabled = true; // Включаем UI обратно после завершения (или ошибки) этой части логики
            }
            // Сохранение настроек после успешного получения параметров (опционально)
            // Properties.Settings.Settings.Default.FlrName = levelName;
            // Properties.Settings.Settings.Default.MainFit = mainFitValue.ToString(CultureInfo.InvariantCulture);
            // Properties.Settings.Settings.Default.MaxSol = maxSolutions;
            // Properties.Settings.Settings.Default.Save();
        }


        /// <summary>
        /// Обработчик нажатия кнопки "Применить (3D)".
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика запуска ExternalEvent для 3D визуализации будет здесь
            //MessageBox.Show("Логика 3D визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
            // Передаем списки точек обработчику визуализации
            visualizationHandler.PointsOutsideFloors = pointsOutsideFloors;

            // Запускаем ExternalEvent для выполнения логики визуализации в потоке Revit API
            visualizationEvent.Raise();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "План (2D)".
        /// </summary>
        private void PlanarButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика запуска ExternalEvent для 2D визуализации будет здесь
            MessageBox.Show("Логика 2D визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Отменить виз.".
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика запуска ExternalEvent для отмены визуализации будет здесь
            //MessageBox.Show("Логика отмены визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
            // Запускаем ExternalEvent для выполнения логики очистки в потоке Revit API
            cleanEvent.Raise();

            // Деактивируем кнопки визуализации после очистки
            ApplyButton.IsEnabled = false;
            PlanarButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
        }

        /// <summary>
        /// Обработчик ввода текста в поле основного армирования (разрешает цифры и один разделитель).
        /// </summary>
        private void ArmTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Реализация проверки ввода (как в предыдущих примерах)
            char decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != decimalSeparator.ToString())
            {
                e.Handled = true;
            }
            if (e.Text == decimalSeparator.ToString())
            {
                if ((sender as System.Windows.Controls.TextBox).Text.Contains(decimalSeparator))
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Обработчик ввода текста в поле максимального числа решений (разрешает только цифры).
        /// </summary>
        private void MaxSolTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Реализация проверки ввода (как в предыдущих примерах)
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Обработчик события закрытия окна для сохранения настроек.
        /// </summary>
        private void ReinforcementInputWindow_Closed(object sender, EventArgs e)
        {
            // Логика сохранения настроек UI будет здесь (если требуется)
            // Например, можно сохранить значения из FlrTextBox, ArmTextBox, MaxSolTextBox
            // Это потребует настройки Properties.Settings в проекте
        }


        // --- Вспомогательные классы и методы (нужно будет адаптировать или реализовать) ---
        // CsvFileReader (статический класс с методом ReadAllPointsOfType) - уже начали его адаптировать
        // Additional_Reinforcement_point (ваш класс точки) - уже адаптировали
        // Tools (ваш статический класс с константами и методами) - уже есть в проекте
        // Остальные классы и методы из референсного проекта (DataFile, ProgressWindow, Node, ReinforcementSolution,
        // RebarConfig, ReinforcementOptimizer, IsPointInsideCurveLoop, GetOpeningsFromRevit, ValidateLevel,
        // ExternalEvent, IExternalEventHandler) нужно будет адаптировать или реализовать по мере необходимости.


    } // Конец класса ReinforcementInputWindow
}
