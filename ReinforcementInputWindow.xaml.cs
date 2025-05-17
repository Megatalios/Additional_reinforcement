// ReinforcementInputWindow.xaml.cs
// Этот файл содержит код логики для окна ReinforcementInputWindow.xaml

using System;
using System.Collections.Generic;
using System.Linq; // Для использования методов расширения Linq
using System.Text;
using System.Threading.Tasks;
using System.Windows; // Базовые классы для WPF (Window, MessageBox и т.д.)
using System.Windows.Controls; // Элементы управления (Button, TextBox, CheckBox, DataGrid, ComboBox)
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input; // Для обработки событий ввода (например, PreviewTextInput)
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32; // Для OpenFileDialog в WPF

using Autodesk.Revit.UI; // Для UIDocument, UIApplication, TaskDialog, ExternalEvent, IExternalEventHandler
using Autodesk.Revit.DB; // Для Document, ElementId, XYZ, Level, FilteredElementCollector, Floor, PlanarFace, CurveLoop, UnitUtils, DisplayUnitType, Units, FormatOptions, etc.
using Autodesk.Revit.DB.Architecture; // Для Room (если понадобится в будущем)
using System.Data; // Для DataTable (если используете для DataGrid)
using System.Globalization; // Для CultureInfo
using System.IO; // Для работы с файлами (например, StreamReader)

// Убедитесь, что ваш namespace совпадает с namespace проекта и XAML файла
namespace Diplom_Project // Ваш namespace
{
    /// <summary>
    /// Логика взаимодействия для ReinforcementInputWindow.xaml
    /// </summary>
    /// 



    public partial class ReinforcementInputWindow : Window
    {
        private UIDocument uiDocument; // Ссылка на UIDocument
        private Document document; // Ссылка на Document

        // --- Поле для хранения ВСЕХ точек, загруженных из CSV ---
        private List<Additional_Reinforcement_point> allCsvPoints;
        // Поле для отображения результатов расчета в UI DataGrid
        private DataTable ZonesTable;
        // Поля для хранения данных из референсного проекта, которые потребуются для расчета
        // private DataTable DiamStep; // Данные Диаметр-Шаг
        // private DataTable DiamCost; // Данные Диаметр-Цена
        // private DataTable Length; // Данные Длина стержней
        // private List<ReinforcementSolution> bestSolutions; // Список лучших найденных решений
        private List<Floor> floors; // Список плит перекрытия на выбранном уровне (будет заполнен здесь)

        // Коэффициент конвертации единиц из CSV (метры) в футы Revit API
        // Используем фиксированный коэффициент, если UnitUtils.Convert с DisplayUnitType вызывает ошибки
        public const double METERS_TO_FEET = 3.28084;

        // --- Поля для хранения точек после геометрической фильтрации ---
        // Точки, которые находятся внутри плит
        private List<List<Additional_Reinforcement_point>> pointsInsideFloorsGrouped;
        // Точки, которые находятся вне плит
        private List<Additional_Reinforcement_point> pointsOutsideFloors;

        private List<ReinforcementSolution> bestSolutions;
        // -------------------------------------------------------------
        // --- Поля для External Event ---
        private ExternalEvent visualizationEvent;
        private VisualizationHandler visualizationHandler;
        private PlanarVisualizationHandler planarVisualizationHandler;
        private ExternalEvent cleanEvent;
        private CleanHandler cleanHandler;
        private ExternalEvent planarVisualizationEvent;
        // -----------------------------


        // Конструктор окна UI
        public ReinforcementInputWindow(UIDocument uidoc)
        {
            InitializeComponent(); // Инициализация UI компонентов из XAML

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
            // Инициализируем обработчик 2D визуализации
            planarVisualizationHandler = new PlanarVisualizationHandler();


            // Создаем экземпляры ExternalEvent, связывая их с обработчиками
            visualizationEvent = ExternalEvent.Create(visualizationHandler);
            cleanEvent = ExternalEvent.Create(cleanHandler);
            // Создаем ExternalEvent для 2D визуализации
            planarVisualizationEvent = ExternalEvent.Create(planarVisualizationHandler);


            // Передаем UIDocument обработчикам, чтобы они могли работать с текущим документом
            visualizationHandler.UiDocument = uiDocument;
            cleanHandler.UiDocument = uiDocument;
            // Передаем UIDocument обработчику 2D визуализации
            planarVisualizationHandler.UiDocument = uiDocument;

            // ==================================================


            // === Заполняем ComboBox доступными уровнями из документа Revit ===
            try
            {
                // Получаем все элементы типа Level в документе
                List<Level> allLevels = new FilteredElementCollector(document)
                    .OfClass(typeof(Level)) // Фильтруем по классу Level
                    .Cast<Level>() // Приводим найденные элементы к типу Level
                    .OrderBy(level => level.Elevation) // Опционально: сортируем уровни по высоте
                    .ToList(); // Преобразуем в список

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

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка уровней из Revit: {ex.Message}", "Ошибка Revit API", MessageBoxButton.OK, MessageBoxImage.Error);
                // Деактивировать ComboBox или кнопку расчета, если уровни не загружены
                LevelComboBox.IsEnabled = false;
            }
            // ================================================================


            // Инициализация таблицы DataTable для DataGrid
            ZonesTable = new DataTable();
            InitializeDataTables(); // Метод инициализации структуры таблиц

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

        public void Dispose()
        {
            visualizationEvent?.Dispose();
            planarVisualizationEvent?.Dispose();
            cleanEvent?.Dispose();
        }

        /// <summary>
        /// Метод инициализации структуры DataTable для отображения результатов.
        /// </summary>
        private void InitializeDataTables()
        {
            ZonesTable.Columns.Add("Count", typeof(int));
            ZonesTable.Columns.Add("TotalLength", typeof(double));
            ZonesTable.Columns.Add("TotalCost", typeof(double));
            ZonesTable.Columns.Add("Num", typeof(int));
            ZonesTable.Columns.Add("Level", typeof(string));
        }


        // === Обработчики событий UI элементов ===

        /// <summary>
        /// Обработчик нажатия кнопки "Загрузить CSV".
        /// Открывает диалог выбора файла, читает данные и сохраняет их.
        /// </summary>
        private async void LoadCsvButton_Click(object sender, RoutedEventArgs e)
        {
            // Используем Microsoft.Win32.OpenFileDialog для WPF
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            // Показываем диалог выбора файла
            if (openFileDialog.ShowDialog() == true) // а() для Microsoft.Win32
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
                bestSolutions = null; // Очищаем предыдущие решения (если поле bestSolutions существует)


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

        /// <summary>
        /// Обработчик нажатия кнопки "Выполнить расчет" (или "Применить фильтры").
        /// Считывает параметры из UI, фильтрует загруженные точки, привязывает к плитам.
        /// </summary>
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
            bestSolutions = null; // Очищаем предыдущие решения (если поле bestSolutions существует)
            // Инициализируем список плит пустым списком в начале метода
            floors = new List<Floor>();
            // Списки pointsInsideFloorsGrouped и pointsOutsideFloors инициализируются в конструкторе
            // Очищаем их перед новым расчетом
            pointsInsideFloorsGrouped.Clear();
            pointsOutsideFloors.Clear();


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

            double as1xThreshold = useAs1X ? mainFitValue : -1;
            double as2xThreshold = useAs2X ? mainFitValue : -1;
            double as3yThreshold = useAs3Y ? mainFitValue : -1;
            double as4yThreshold = useAs4Y ? mainFitValue : -1;

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

                if (floors == null || floors.Count == 0) // Проверяем, что список floors не пустой
                {
                    MessageBox.Show($"На уровне '{levelName}' не найдено плит перекрытия.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.IsEnabled = true;
                    return;
                }

                // 3.3. Определяем коэффициент конвертации единиц из CSV (метры) в футы Revit API.
                // Используем фиксированный коэффициент, если UnitUtils.Convert с DisplayUnitType вызывает ошибки
                double scaleFactor = METERS_TO_FEET; // Используем определенную константу


                // 3.4. Выполняем геометрическую фильтрацию и привязку точек к плитам
                // Списки pointsInsideFloorsGrouped и pointsOutsideFloors инициализируются в конструкторе
                // Очищаем их перед новым расчетом
                pointsInsideFloorsGrouped.Clear();
                pointsOutsideFloors.Clear();

                // Инициализируем списки внутри pointsInsideFloorsGrouped по количеству найденных плит
                for (int i = 0; i < floors.Count; i++)
                {
                    pointsInsideFloorsGrouped.Add(new List<Additional_Reinforcement_point>());
                }

                // Создаем список для точек, которые не попали ни в одну плиту
                // pointsOutsideFloors = new List<Additional_Reinforcement_point>(); // Уже инициализировано в конструкторе


                // Итерируемся по отфильтрованным точкам из CSV
                foreach (var pointData in filteredPoints)
                {
                    // Конвертируем координаты точки из CSV в футы Revit API
                    // Используем метод GetXYZ класса точки
                    //XYZ pointXYZ_ft = pointData.GetXYZ(scaleFactor);
                    XYZ pointXYZ_ft = pointData.GetXYZ();

                    bool foundSlab = false; // Флаг для определения, найдена ли плита для точки

                    // Проверяем каждую плиту на этом уровне
                    for (int i = 0; i < floors.Count; i++)
                    {
                        Floor floor = floors[i];

                        // --- Логика получения верхней грани и контуров плиты (адаптировано из референса) ---
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
                        // Используем перенесенный метод IsPointInsideCurveLoop
                        // Проверяем, находится ли точка внутри внешнего контура плиты
                        if (!IsPointInsideCurveLoop(outerBoundary, pointXYZ_ft)) continue;

                        // Проверяем, не попадает ли точка в отверстие (контуры с индексом > 0)
                        bool isInsideHole = false;
                        for (int j = 1; j < boundaryLoops.Count; j++)
                        {
                            if (IsPointInsideCurveLoop(boundaryLoops[j], pointXYZ_ft)) // Используем перенесенный метод IsPointInsideCurveLoop
                            {
                                isInsideHole = true;
                                break;
                            }
                        }
                        if (isInsideHole) continue; // Если точка в отверстии, пропускаем ее

                        // Если точка прошла все проверки, она находится внутри этой плиты
                        // Добавляем отфильтрованную и привязанную точку в список для соответствующей плиты
                        Level floorLevel = document.GetElement(floor.LevelId) as Level;
                        if (floorLevel == null) continue; // Если не удалось получить уровень, пропускаем плиту

                        double levelElevation_ft = floorLevel.Elevation; // Отметка уровня плиты в футах

                        // Получаем толщину плиты. Это может быть параметр "Structural Depth" или "Thickness".
                        // Название параметра может варьироваться. Нужно найти правильный параметр.
                        // Пример: Поиск параметра по BuiltInParameter
                        Parameter thicknessParam = floor.get_Parameter(BuiltInParameter.STRUCTURAL_FLOOR_CORE_THICKNESS); // Пример для структурной толщины
                        if (thicknessParam == null)
                        {
                            // Если структурная толщина не найдена, попробуйте другие параметры или пропустите проверку Z
                            System.Diagnostics.Debug.WriteLine($"Не найден параметр толщины для плиты {floor.Id}. Проверка Z может быть неточной.");
                            // Для простоты, пока пропустим проверку Z, если толщина не найдена.
                            // В реальном проекте нужно найти надежный способ получения толщины.
                            // continue; // Или можно пропустить эту плиту, если толщина критична
                        }

                        double floorThickness_ft = (thicknessParam != null) ? thicknessParam.AsDouble() : 0; // Толщина в футах

                        // Определяем диапазон Z-координат для плиты
                        // Нижняя грань плиты: levelElevation_ft - floorThickness_ft
                        // Верхняя грань плиты: levelElevation_ft
                        double minZ_floor_ft = levelElevation_ft - floorThickness_ft;
                        double maxZ_floor_ft = levelElevation_ft;

                        // Проверяем, находится ли Z-координата точки в пределах Z-диапазона плиты
                        // Используем небольшой допуск для сравнения чисел с плавающей точкой
                        double zTolerance = 1e-6; // Допуск в футах
                        if (pointXYZ_ft.Z >= minZ_floor_ft - zTolerance && pointXYZ_ft.Z <= maxZ_floor_ft + zTolerance)
                        {
                            // Если точка находится внутри внешнего контура (XY), вне отверстий (XY) И в пределах Z-диапазона плиты (Z)
                            // Добавляем отфильтрованную и привязанную точку в список для соответствующей плиты
                            pointsInsideFloorsGrouped[i].Add(pointData); // Добавляем Additional_Reinforcement_point сюда
                            foundSlab = true; // Устанавливаем флаг, что плита найдена

                            // Точка найдена в одной плите, переходим к следующей отфильтрованной точке
                            break; // Точка может принадлежать только одной плите на одном уровне
                        }
                        else
                        {
                            // Если точка находится по XY в пределах плиты, но не по Z,
                            // она не принадлежит этой плите. Продолжаем проверять другие плиты (если есть).
                            System.Diagnostics.Debug.WriteLine($"Точка {pointData.Number} (Z={pointXYZ_ft.Z:F3} футов) находится вне Z-диапазона плиты {floor.Id} ({minZ_floor_ft:F3} - {maxZ_floor_ft:F3} футов).");
                        }
                        // === КОНЕЦ ДОБАВЛЕННОЙ ПРОВЕРКИ ПО Z ===
                    }

                    // Если после проверки всех плит точка не была добавлена ни в один список, значит, она вне плит
                    if (!foundSlab)
                    {
                        pointsOutsideFloors.Add(pointData);
                    }
                }

                // Проверяем, остались ли точки после геометрической фильтрации и привязки к плитам
                // pointsInsideFloorsGrouped.Any(sn => sn.Count > 0) проверяет, есть ли хотя бы в одном списке внутри pointsInsideFloorsGrouped элементы
                int totalPointsInsideFloors = pointsInsideFloorsGrouped.Sum(sn => sn.Count);
                int totalPointsOutsideFloors = pointsOutsideFloors.Count;
                //var nodesForOptimizer = new List<List<Node>>();

                if (totalPointsInsideFloors == 0 && totalPointsOutsideFloors == 0)
                {
                    MessageBox.Show($"После привязки к плитам на уровне '{levelName}' не осталось узлов, нуждающихся в дополнительном армировании.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.IsEnabled = true;
                    return;
                }
                else
                {
                    MessageBox.Show($"Геометрическая фильтрация завершена.\nНайдено {totalPointsInsideFloors} узлов внутри плит.\nНайдено {totalPointsOutsideFloors} узлов вне плит.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Активируем кнопки визуализации, если есть точки для отображения
                    if (totalPointsInsideFloors > 0 || totalPointsOutsideFloors > 0)
                    {
                        ApplyButton.IsEnabled = true; // Применить (3D)
                        PlanarButton.IsEnabled = true; // План (2D) - если планируется 2D виз.
                        CancelButton.IsEnabled = true; // Отменить виз.
                    }
                }

                // === 4. Преобразование точек в узлы для оптимизации ===
                var nodesForOptimizer = new List<List<Node>>();
                for (int i = 0; i < pointsInsideFloorsGrouped.Count; i++)
                {
                    List<Additional_Reinforcement_point> pointsOnThisSlab = pointsInsideFloorsGrouped[i]; // Теперь это List<Additional_Reinforcement_point>
                    List<Node> nodesForThisSlab = pointsOnThisSlab
                        .Select(p => new Node // Используем класс Node, определение которого добавлено ниже
                        {
                            Type = p.Type,
                            Number = p.Number,
                            // Координаты в Node должны быть в футах, т.к. с ними работает Optimizer
                            // Использовать p.X, p.Y, p.Z (заглавные буквы)
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
                    // Если после преобразования в Node нет узлов для оптимизации,
                    // выводим сообщение и завершаем работу.
                    MessageBox.Show($"После привязки к плитам и преобразования данных не осталось узлов для расчета.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.IsEnabled = true;
                    return;
                }

                // === 5. Получение геометрии плиты и отверстий для оптимизатора ===
                // Нам нужны контуры плиты и отверстий для каждой плиты, к которой привязаны узлы.
                // Пока для простоты возьмем геометрию первой плиты, к которой привязаны узлы.
                // В будущей реализации нужно будет обрабатывать каждую плиту отдельно.
                List<CurveLoop> openings = new List<CurveLoop>();
                CurveLoop plateBoundary = null;
                ElementId floorIdForCalculation = ElementId.InvalidElementId;

                if (floors.Count > 0)
                {
                    // Находим индекс плиты, к которой привязаны узлы (берем первую группу узлов)
                    int firstSlabIndexWithNodes = nodesForOptimizer.First(nl => nl.Count > 0).First().SlabId;
                    Floor targetFloor = floors[firstSlabIndexWithNodes]; // Получаем объект плиты по индексу
                    floorIdForCalculation = targetFloor.Id; // Сохраняем ID плиты

                    // Получаем контур плиты и отверстий
                    Options geomOptions = new Options();
                    geomOptions.ComputeReferences = true;
                    GeometryElement geomElem = targetFloor.get_Geometry(geomOptions);
                    PlanarFace topFace = null;

                    foreach (GeometryObject geomObj in geomElem)
                    {
                        if (geomObj is Solid solid)
                        {
                            foreach (Face face in solid.Faces)
                            {
                                if (face is PlanarFace planarFace && planarFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                                {
                                    topFace = planarFace;
                                    break;
                                }
                            }
                        }
                        if (topFace != null) break;
                    }

                    if (topFace != null)
                    {
                        IList<CurveLoop> boundaryLoops = topFace.GetEdgesAsCurveLoops();
                        if (boundaryLoops.Count > 0)
                        {
                            plateBoundary = boundaryLoops[0]; // Первый контур - внешний
                            for (int i = 1; i < boundaryLoops.Count; i++)
                            {
                                openings.Add(boundaryLoops[i]); // Остальные - отверстия
                            }
                        }
                    }
                }

                // Проверяем, получена ли геометрия плиты
                if (plateBoundary == null)
                {
                    MessageBox.Show("Не удалось получить геометрию плиты для оптимизации.", "Ошибка геометрии", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.IsEnabled = true;
                    return;
                }

                // === 6. Загрузка конфигураций арматуры из CSV ===
                List<RebarConfig> availableRebars = new List<RebarConfig>();
                string rebarCsvFilePath = "C:\\Users\\kamil\\Desktop\\rebars.csv";

                string rebarLoadingError;
                availableRebars = LoadRebarConfigsFromCsv(rebarCsvFilePath, out rebarLoadingError);

                if (!string.IsNullOrEmpty(rebarLoadingError))
                {
                    MessageBox.Show($"Ошибка при загрузке данных об арматуре:\n{rebarLoadingError}", "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.IsEnabled = true;
                    return;
                }

                // Проверяем, что данные об арматуре загружены
                if (availableRebars == null || availableRebars.Count == 0)
                {
                    MessageBox.Show("Данные о доступной арматуре не загружены или пусты.", "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.IsEnabled = true;
                    return;
                }


                // === 7. Запускаем расчетный алгоритм (ZoneOptimizer) ===
                // Этот шаг может быть длительным и, возможно, должен выполняться в фоновом потоке (Task.Run).
                // Сам ZoneOptimizer не должен напрямую работать с Revit API.

                // Создаем экземпляр ZoneOptimizer
                ZoneOptimizer optimizer = new ZoneOptimizer
                {
                    AvailableRebars = availableRebars, // Передаем доступную арматуру
                    Openings = openings, // Передаем информацию об отверстиях
                    PlateBoundary = plateBoundary, // TODO: Получить внешний контур плиты и передать сюда
                    MaxSolutions = maxSolutions, // Максимальное число решений из UI
                                                 // Передаем ПОРОГОВЫЕ значения основного армирования по выбранным направлениям
                                                 // Эти значения используются в ZoneOptimizer для определения требуемой нагрузки
                    BasicReinforcement = new[] { as1xThreshold, as2xThreshold, as3yThreshold, as4yThreshold },
                    // TODO: Передать стандартные длины стержней, если они используются в расчете стоимости
                    // StandardLengths = ...
                };

                // Вызываем основной метод оптимизатора для поиска лучших решений
                // Передаем списки узлов, сгруппированные по плитам.
                // Этот вызов может быть обернут в Task.Run, если он длительный и не использует API.
                // Предполагаем, что FindBestReinforcementSolutions не вызывает API напрямую.
                // TODO: Адаптировать FindBestReinforcementSolutions для работы со списком узлов, сгруппированных по плитам
                // Пока передаем только первую группу узлов и первую плиту для примера.
                List<ReinforcementSolution> bestSolutions = await Task.Run(() => optimizer.FindBestReinforcementSolutions(nodesForOptimizer.FirstOrDefault(), floors.FirstOrDefault())); // TODO: Передавать нужную плиту или адаптировать FindBestReinforcementSolutions для работы со списком плит
                

                // Очищаем оптимизатор после использования (опционально)
                optimizer = null;


                // === 8. Обработка и отображение результатов ===

                // Проверяем, были ли найдены решения
                if (bestSolutions == null || bestSolutions.Count == 0)
                {
                    MessageBox.Show("Алгоритм оптимизации не нашел подходящих решений по зонированию.", "Результат расчета", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Деактивируем кнопки визуализации, если решений нет
                    ApplyButton.IsEnabled = false;
                    PlanarButton.IsEnabled = false;
                    CancelButton.IsEnabled = false;
                }
                else
                {
                    // Заполняем ZonesTable данными из полученных решений bestSolutions
                    // Сначала очищаем таблицу
                    ZonesTable.Clear();

                    // Итерируемся по каждому найденному решению
                    foreach (var solution in bestSolutions)
                    {
                        // Создаем новую строку в DataTable
                        DataRow row = ZonesTable.NewRow();

                        // Заполняем столбцы данными из ReinforcementSolution
                        // TODO: Убедитесь, что свойства ReinforcementSolution и ZoneSolution соответствуют колонкам ZonesTable
                        row["Num"] = solution.Num; // Порядковый номер решения
                        row["Count"] = solution.Zones.Count; // Количество зон в решении
                        row["TotalCost"] = solution.TotalCost; // Общая стоимость
                        row["TotalLength"] = solution.TotalLength; // Общая длина
                        // Добавляем информацию об уровне
                        // Предполагаем, что все зоны в решении относятся к одной плите,
                        // и мы знаем ID этой плиты (floorIdForCalculation).
                        // Находим имя уровня по ID плиты.
                        string solutionLevelName = "Неизвестен";
                        Floor solutionFloor = floors.FirstOrDefault(f => f.Id == floorIdForCalculation);
                        if (solutionFloor != null)
                        {
                            Level floorLevel = document.GetElement(solutionFloor.LevelId) as Level;
                            if (floorLevel != null)
                            {
                                solutionLevelName = floorLevel.Name;
                            }
                        }
                        row["Level"] = solutionLevelName;


                        // Добавляем заполненную строку в DataTable
                        ZonesTable.Rows.Add(row);
                    }

                    // Активируем кнопки визуализации, если найдены решения
                    ApplyButton.IsEnabled = true;
                    PlanarButton.IsEnabled = true; // Активируем кнопку 2D визуализации
                    CancelButton.IsEnabled = true; // Активируем кнопку отмены визуализации

                    MessageBox.Show($"Расчет завершен. Найдено {bestSolutions.Count} лучших решений.", "Расчет завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // TODO: Сохранить bestSolutions в поле класса, чтобы использовать их для визуализации
                this.bestSolutions = bestSolutions; // Предполагаем, что поле bestSolutions определено в ReinforcementInputWindow

                // Передаем ID плиты в обработчик визуализации
                // Это нужно сделать после успешного расчета и определения floorIdForCalculation
                if (floorIdForCalculation != ElementId.InvalidElementId)
                {
                    planarVisualizationHandler.FloorId = floorIdForCalculation;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ReinforcementInputWindow: Не удалось определить ID плиты для визуализации.");
                }


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
                bestSolutions = null; // Очищаем решения при ошибке
            }
            finally
            {
                this.IsEnabled = true; // Включаем UI обратно после завершения (или ошибки) этой части логики
            }
            
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Применить (3D)".
        /// Запускает ExternalEvent для визуализации точек в модели Revit.
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Передаем списки точек обработчику визуализации
            visualizationHandler.PointsInsideFloors = pointsInsideFloorsGrouped;
            visualizationHandler.PointsOutsideFloors = pointsOutsideFloors;

            // Запускаем ExternalEvent для выполнения логики визуализации в потоке Revit API
            visualizationEvent.Raise();

            // MessageBox.Show("Логика 3D визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Обработчик нажатия кнопки "План (2D)".
        /// </summary>
        private void PlanarButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли рассчитанные решения
            if (bestSolutions == null || bestSolutions.Count == 0)
            {
                MessageBox.Show("Нет рассчитанных решений для визуализации.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Выбираем лучшее решение (например, первое)
            ReinforcementSolution bestSolution = bestSolutions.First();

            // Извлекаем ZoneSolution из лучшего решения
            // Передаем список ZoneSolution, а не Zone, так как ZoneSolution содержит Bounds
            List<ZoneSolution> zoneSolutionsToDraw = bestSolution.Zones; // Используем bestSolution.Zones

            // Обновляем список зон в обработчике 2D визуализации
            planarVisualizationHandler.ZonesToVisualize = zoneSolutionsToDraw;

            // ID плиты уже должен быть установлен в planarVisualizationHandler.FloorId
            // после успешного расчета в CalculateButton_Click

            // Вызываем External Event для выполнения 2D визуализации в Revit
            planarVisualizationEvent.Raise();

            System.Diagnostics.Debug.WriteLine("External Event для 2D визуализации вызван.");

        }

        

        /// <summary>
        /// Обработчик нажатия кнопки "Отменить виз.".
        /// Запускает ExternalEvent для удаления визуализированных точек из модели Revit.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Запускаем ExternalEvent для выполнения логики очистки в потоке Revit API
            cleanEvent.Raise();

            // Деактивируем кнопки визуализации после очистки
            ApplyButton.IsEnabled = false;
            PlanarButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            // MessageBox.Show("Логика отмены визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Обработчик ввода текста в поле основного армирования (разрешает цифры и один разделитель).
        /// </summary>
        private void ArmTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
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
            try
            {
               
            }
            catch (Exception ex)
            {
                // MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет, находится ли точка внутри заданного CurveLoop (полигона).
        /// Адаптировано из референсного проекта.
        /// </summary>
        /// <param name="loop">Контур (полигон) для проверки.</param>
        /// <param name="point">Проверяемая точка (в тех же единицах, что и контур - футы Revit API).</param>
        /// <returns>True, если точка находится внутри контура, иначе False.</returns>
        // Этот метод должен быть статическим, чтобы его можно было вызвать без создания экземпляра класса ReinforcementInputWindow
        public static bool IsPointInsideCurveLoop(CurveLoop loop, XYZ point)
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


        /// <summary>
        /// Читает конфигурации арматуры из указанного CSV файла.
        /// Формат: Название арматуры;Диаметр (мм);Стоимость за 1 м (руб);Шаг армирования (мм);Выдерживаемая нагрузка (кН/м?)
        /// Разделитель: точка с запятой (;)
        /// Десятичный разделитель: запятая (,)
        /// </summary>
        /// <param name="filePath">Путь к CSV файлу с данными об арматуре.</param>
        /// <param name="error">Выходной параметр для сообщения об ошибках парсинга строк.</param>
        /// <returns>Список объектов RebarConfig или null при критической ошибке файла.</returns>
        private static List<RebarConfig> LoadRebarConfigsFromCsv(string filePath, out string error)
        {
            error = ""; // Инициализируем сообщение об ошибках
            List<RebarConfig> rebarConfigs = new List<RebarConfig>();
            StringBuilder errorBuilder = new StringBuilder();
            int lineNumber = 0;

            // Используем CultureInfo.InvariantCulture для парсинга чисел, чтобы избежать проблем с локальными настройками
            // Если в CSV используется запятая как десятичный разделитель, нужно использовать CultureInfo с соответствующим NumberFormat
            // Например, для русского формата с запятой: new CultureInfo("ru-RU")
            // Или создать NumberFormatInfo с нужным DecimalSeparator
            NumberFormatInfo nfi = new NumberFormatInfo
            {
                NumberDecimalSeparator = "," // Указываем, что десятичный разделитель - запятая
            };


            try
            {
                using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8)) // Указываем кодировку UTF8
                {
                    string headerLine = reader.ReadLine(); // Пропускаем заголовок
                    lineNumber++;

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        lineNumber++;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] values = line.Split(';'); // Разделитель - точка с запятой

                        // Ожидаемый формат: Название арматуры;Диаметр (мм);Стоимость за 1 м (руб);Шаг армирования (мм);Выдерживаемая нагрузка (кН/м?)
                        // Проверяем количество столбцов (ожидаем 5)
                        if (values.Length < 5)
                        {
                            errorBuilder.AppendLine($"Строка {lineNumber}: Недостаточно столбцов ({values.Length}). Ожидается 5. Пропущена.");
                            continue;
                        }

                        try
                        {
                            string name = values[0].Trim();
                            // Парсим числовые значения, используя NumberFormatInfo с запятой как разделителем
                            double diameter = double.Parse(values[1].Trim(), nfi);
                            double costPerMeter = double.Parse(values[2].Trim(), nfi);
                            double spacing = double.Parse(values[3].Trim(), nfi);
                            double bearingCapacity = double.Parse(values[4].Trim(), nfi);

                            // Создаем новый объект RebarConfig и добавляем его в список
                            rebarConfigs.Add(new RebarConfig
                            {
                                Name = name,
                                Diameter = diameter,
                                CostPerMeter = costPerMeter,
                                Spacing = spacing,
                                BearingCapacity = bearingCapacity
                            });
                        }
                        catch (FormatException)
                        {
                            errorBuilder.AppendLine($"Строка {lineNumber}: Ошибка парсинга числовых данных. Пропущена.");
                        }
                        catch (IndexOutOfRangeException)
                        {
                            errorBuilder.AppendLine($"Строка {lineNumber}: Ошибка доступа к столбцу. Пропущена.");
                        }
                        catch (Exception ex)
                        {
                            errorBuilder.AppendLine($"Строка {lineNumber}: Неизвестная ошибка при парсинге: {ex.Message}. Пропущена.");
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // Не показываем MessageBox здесь, чтобы не блокировать UI поток.
                // Ошибка будет возвращена через выходной параметр.
                error = $"Файл арматуры не найден: {filePath}";
                return null; // Критическая ошибка
            }
            catch (Exception ex)
            {
                // Не показываем MessageBox здесь.
                error = $"Произошла ошибка при чтении файла арматуры CSV: {ex.Message}";
                return null; // Критическая ошибка
            }

            error = errorBuilder.ToString(); // Сохраняем накопленные ошибки парсинга строк
            return rebarConfigs; // Возвращаем список прочитанных конфигураций
        }



    } // Конец класса ReinforcementInputWindow

    // === Добавленные скелеты классов для компиляции ===
    // Эти классы нужно будет полностью адаптировать или реализовать на следующих шагах.
    // Они добавлены здесь временно, чтобы код в ReinforcementInputWindow компилировался.

    /// <summary>
    /// Представляет точку армирования в формате, используемом расчетным алгоритмом (ReinforcementOptimizer).
    /// Координаты хранятся в футах Revit API.
    /// Значения As* хранятся в исходных единицах CSV, или -1, если направление не выбрано.
    /// </summary>
    

    /// <summary>
    /// Скелет класса для представления решения по зоне армирования.
    /// </summary>
    public class ZoneSolution
    {
        //// public Rectangle Boundary { get; set; } // Границы зоны (нужно определить класс Rectangle или использовать BoundingBoxXYZ)
        //public double Diameter { get; set; } // Диаметр арматуры в зоне
        //public double Spacing { get; set; } // Шаг арматуры в зоне
        //public double ZoneCost { get; set; } // Стоимость арматуры в зоне
        //public double ZoneLength { get; set; } // Длина арматуры в зоне

        /// <summary>
        /// Геометрические границы зоны в виде BoundingBoxXYZ (в футах Revit API).
        /// Это свойство необходимо для визуализации зоны.
        /// </summary>
        public BoundingBoxXYZ Bounds { get; set; } // Добавлено свойство Bounds

        /// <summary>
        /// Список узлов, которые входят в эту зону в данном решении.
        /// </summary>
        public List<Node> NodesInZone { get; set; }

        /// <summary>
        /// Оптимальные конфигурации арматуры, подобранные для каждого из четырех направлений
        /// в этой зоне в данном решении. Может быть null, если армирование по данному направлению не требуется.
        /// Индексы: 0 - As1X, 1 - As2X, 2 - As3Y, 3 - As4Y.
        /// </summary>
        public RebarConfig[] OptimalRebarConfigs { get; set; } // Добавлено свойство OptimalRebarConfigs

        /// <summary>
        /// Максимальная требуемая нагрузка по каждому из четырех направлений
        /// среди всех точек в этой зоне в данном решении (в кН/м²).
        /// Индексы: 0 - As1X, 1 - As2X, 2 - As3Y, 3 - As4Y.
        /// </summary>
        public double[] MaxRequiredLoad { get; set; } // Добавлено свойство MaxRequiredLoad

        /// <summary>
        /// Рассчитанная общая стоимость арматуры для этой зоны в данном решении (в рублях).
        /// </summary>
        public double ZoneCost { get; set; }

        /// <summary>
        /// Рассчитанная общая длина арматуры для этой зоны в данном решении (в футах).
        /// </summary>
        public double ZoneLength { get; set; } // Добавлено свойство ZoneLength

        // TODO: Возможно, добавить другие свойства, если нужно (например, информацию о типе армирования)

        /// <summary>
        /// Конструктор по умолчанию (может быть полезен).
        /// </summary>
        public ZoneSolution()
        {
            NodesInZone = new List<Node>();
            OptimalRebarConfigs = new RebarConfig[4]; // Инициализация массива
            MaxRequiredLoad = new double[4]; // Инициализация массива
        }

        // Возможно, добавить конструктор для удобного создания из объекта Zone
        // public ZoneSolution(Zone zone)
        // {
        //     Bounds = zone.Bounds;
        //     NodesInZone = new List<Node>(zone.Points);
        //     OptimalRebarConfigs = (RebarConfig[])zone.OptimalRebarConfigs.Clone();
        //     MaxRequiredLoad = (double[])zone.MaxRequiredLoad.Clone();
        //     ZoneCost = zone.Cost;
        //     ZoneLength = zone.Length;
        // }
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
    }


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
            if (UiDocument == null) {
                TaskDialog.Show("Визуализация", "UIDocument недоступен.");
                return; 
            }

            Document doc = UiDocument.Document;

            // === Логика создания графических элементов в Revit ===
            // Эта логика должна выполняться внутри транзакции
            using (Transaction trans = new Transaction(doc, "Visualize Points"))
            {
                //TaskDialog.Show("Визуализация", "Начало транзакции.");
                trans.Start();
                //TaskDialog.Show("Визуализация", "Транзакция начата.");
                try
                {
                    // Получаем ElementId для категории "Обобщенные модели" (Generic Models)
                    // В этой категории удобно создавать DirectShape
                    ElementId modelCategoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                    //TaskDialog.Show("Визуализация", $"Получен ID категории Обобщенные модели: {modelCategoryId.IntegerValue}");

                    // Попытка найти или создать материалы для визуализации
                    Material materialInside = FindOrCreateMaterial(doc, "Material_InsideFloors", new Autodesk.Revit.DB.Color(0, 0, 255)); // Синий
                    Material materialOutside = FindOrCreateMaterial(doc, "Material_OutsideFloors", new Autodesk.Revit.DB.Color(255, 0, 0)); // Красный
                    ElementId graphicsStyleId = ElementId.InvalidElementId;

                    ElementId materialInsideId = materialInside != null ? materialInside.Id : ElementId.InvalidElementId;
                    ElementId materialOutsideId = materialOutside != null ? materialOutside.Id : ElementId.InvalidElementId;
                    //SolidOptions solidOptions = new SolidOptions(GeometryPrecisionLevels.Medium);

                    //TaskDialog.Show("Визуализация", "SolidOptions созданы.");

                    // Визуализация точек внутри плит (например, синим цветом)
                    if (PointsInsideFloors != null)
                    {
                        //TaskDialog.Show("Визуализация", $"Найдено групп точек внутри плит: {PointsInsideFloors.Count}");
                        int pointCountInside = 0;
                        // Создаем SolidOptions для точек внутри плит, используя ID материала и InvalidElementId для графического стиля
                        SolidOptions solidOptionsInside = new SolidOptions(materialInsideId, graphicsStyleId);
                        foreach (var slabPointsList in PointsInsideFloors)
                        {
                            foreach (var pointData in slabPointsList)
                            {
                                pointCountInside++;
                                

                                // Получаем координаты точки в футах Revit API
                                // Используем константу METERS_TO_FEET из ReinforcementInputWindow
                                XYZ pointXYZ_ft = pointData.GetXYZ();

                                // === Создание простой геометрии для визуализации (вместо Sphere) ===
                                // Создадим небольшой куб с помощью GeometryCreationUtilities.CreateExtrusionGeometry
                                List<GeometryObject> geometryObjects = new List<GeometryObject>();
                                double size = 1.0; // Размер элемента (например, 0.1 фута = ~3 см)

                                try
                                {
                                    // Создаем BoundingBoxXYZ вокруг точки
                                    BoundingBoxXYZ bbox = new BoundingBoxXYZ();
                                    bbox.Min = pointXYZ_ft - new XYZ(size / 2, size / 2, size / 2);
                                    bbox.Max = pointXYZ_ft + new XYZ(size / 2, size / 2, size / 2);

                                    // Создаем Solid из BoundingBoxXYZ
                                    // Создаем контур для экструзии (нижняя грань куба)
                                    CurveLoop baseLoop = new CurveLoop();
                                    XYZ p0 = new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Min.Z);
                                    XYZ p1 = new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Min.Z);
                                    XYZ p2 = new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Min.Z);
                                    XYZ p3 = new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Min.Z);

                                    baseLoop.Append(Autodesk.Revit.DB.Line.CreateBound(p0, p1));
                                    baseLoop.Append(Autodesk.Revit.DB.Line.CreateBound(p1, p2));
                                    baseLoop.Append(Autodesk.Revit.DB.Line.CreateBound(p2, p3));
                                    baseLoop.Append(Autodesk.Revit.DB.Line.CreateBound(p3, p0));

                                    List<CurveLoop> loops = new List<CurveLoop> { baseLoop };

                                    // Направление и высота экструзии
                                    XYZ extrusionDir = new XYZ(0, 0, size); // Выдавливаем вверх на высоту 'size'
                                    double extrusionDist = size; // Расстояние экструзии равно размеру куба

                                    // Создаем Solid
                                    Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, extrusionDir,extrusionDist, solidOptionsInside);

                                    if (solid != null && solid.Volume > 0)
                                    {
                                        geometryObjects.Add(solid);
                                    }
                                    else
                                    {
                                        // Если создание Solid не удалось, попробуем создать просто точку
                                        // В некоторых версиях API DirectShape может не визуализировать одиночные точки.
                                        geometryObjects.Add(Autodesk.Revit.DB.Point.Create(pointXYZ_ft)); // Попытка создать геометрическую точку
                                    }
                                }
                                catch (Exception geomEx)
                                {
                                    // Если создание любой геометрии не удалось
                                    System.Diagnostics.Debug.WriteLine($"Ошибка создания геометрии для точки {pointData.Number}: {geomEx.Message}");
                                    TaskDialog.Show("Ошибка геометрии", $"Ошибка создания геометрии для точки {pointData.Number}: {geomEx.Message}");
                                    continue; // Пропускаем текущую точку
                                }

                                // Проверяем, удалось ли создать хоть какую-то геометрию
                                if (geometryObjects.Count == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Не удалось создать геометрию для точки {pointData.Number}. Пропускаем.");
                                    continue;
                                }


                                // Создаем DirectShape
                                DirectShape ds = DirectShape.CreateElement(doc, modelCategoryId);
                                ds.SetShape(geometryObjects);
                                ds.Name = $"Point_{pointData.Number}_Inside"; // Присваиваем имя для идентификации

                                
                            }
                        }
                        TaskDialog.Show("Визуализация", $"Обработано точек внутри плит: {pointCountInside}");
                    }

                    trans.Commit(); // Завершаем транзакцию
                    //TaskDialog.Show("Визуализация", "Транзакция завершена.");
                }
                catch (Exception ex)
                {
                    // Откатываем транзакцию при ошибке
                    if (trans.HasStarted())
                    {
                        trans.RollBack();
                        TaskDialog.Show("Визуализация", "Транзакция откачена из-за ошибки.");
                    }
                    // Показываем сообщение об ошибке (может быть TaskDialog, так как мы в потоке Revit)
                    TaskDialog.Show("Ошибка визуализации", $"Произошла ошибка при визуализации точек: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Вспомогательный метод для поиска или создания материала по имени и цвету.
        /// </summary>
        private Material FindOrCreateMaterial(Document doc, string materialName, Autodesk.Revit.DB.Color color)
        {
            // Поиск существующего материала
            Material material = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));

            // Если материал не найден, создаем новый
            if (material == null)
            {
                try
                {
                    // Создаем новый материал
                    ElementId materialId = Material.Create(doc, materialName);
                    material = doc.GetElement(materialId) as Material;

                    if (material != null)
                    {
                        // Назначаем цвет
                        material.Color = color;
                        // Возможно, настроить другие свойства материала (прозрачность, глянец и т.д.)
                    }
                }
                catch (Exception ex)
                {
                    // Обработка ошибок создания материала
                    TaskDialog.Show("Ошибка создания материала", $"Не удалось создать материал '{materialName}': {ex.Message}");
                    return null; // Возвращаем null, если создание не удалось
                }
            }
            return material;
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
                    // Ищем DirectShape по категории "Обобщенные модели"
                    FilteredElementCollector collector = new FilteredElementCollector(doc)
                        .OfClass(typeof(DirectShape));

                    List<ElementId> elementsToDelete = new List<ElementId>();
                    foreach (Element elem in collector)
                    {
                        // Проверяем, является ли элемент одним из тех, что мы создали для визуализации
                        // Мы присваивали им имена, начинающиеся с "Point_"
                        if (elem.Name != null && elem.Name.StartsWith("Point_"))
                        {
                            elementsToDelete.Add(elem.Id);
                        }
                    }

                    if (elementsToDelete.Count > 0)
                    {
                        doc.Delete(elementsToDelete); // Удаляем найденные элементы
                    }


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


    

} // Конец namespace
