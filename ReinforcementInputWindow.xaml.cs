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
        // -----------------------------------------------------

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
        // -------------------------------------------------------------

        // --- Поля для External Event ---
        private ExternalEvent visualizationEvent;
        private VisualizationHandler visualizationHandler;

        private ExternalEvent cleanEvent;
        private CleanHandler cleanHandler;
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

            // Создаем экземпляры ExternalEvent, связывая их с обработчиками
            visualizationEvent = ExternalEvent.Create(visualizationHandler);
            cleanEvent = ExternalEvent.Create(cleanHandler);

            // Передаем UIDocument обработчикам, чтобы они могли работать с текущим документом
            visualizationHandler.UiDocument = uiDocument;
            cleanHandler.UiDocument = uiDocument;
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

                // Опционально: загрузить последний выбранный уровень из настроек
                // try
                // {
                //     string lastLevelName = Properties.Settings.Settings.Default.FlrName;
                //     if (LevelComboBox.Items.Contains(lastLevelName))
                //     {
                //         LevelComboBox.SelectedItem = lastLevelName;
                //     }
                // }
                // catch { /* Игнорируем ошибки загрузки настроек */ }

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

            // === Загрузка данных из JSON и настроек (адаптировать из референса) ===
            // Этот код должен быть здесь, чтобы данные и настройки были загружены при открытии окна
            // Вам нужно будет адаптировать или реализовать класс DataFile и его методы
            // Убедитесь, что Tools доступен (например, в том же namespace или через using)
            // try
            // {
            //     string msg = DataFile.ValidateJSONFile(); // Проверка и создание JSON файла
            //     // Здесь можно добавить вывод msg в лог или статус-бар окна
            //     DataFile.LoadAllData(DiamStep, DiamCost, Length); // Загрузка данных из JSON в DataTable
            // }
            // catch (Exception ex)
            // {
            //     MessageBox.Show($"Ошибка при загрузке данных JSON: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            //     // Возможно, деактивировать кнопки расчета, если данные не загружены
            // }

            // === Загрузка последних использованных настроек UI (адаптировать из референса) ===
            // Вам нужно будет настроить Properties.Settings в проекте (Project -> Properties -> Settings)
            // try
            // {
            //     int numOfSolSetting = Properties.Settings.Settings.Default.MaxSol;
            //     MaxSolTextBox.Text = (numOfSolSetting > 0) ? numOfSolSetting.ToString() : "";

            //     string flrNameSetting = Properties.Settings.Settings.Default.FlrName;
            //     // Если загруженное имя уровня есть в ComboBox, выбираем его
            //     if (LevelComboBox.Items.Contains(flrNameSetting))
            //     {
            //         LevelComboBox.SelectedItem = flrNameSetting;
            //     }
            // }
            // catch (Exception ex)
            // {
            //      MessageBox.Show($"Ошибка при загрузке настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            //      // Продолжаем с настройками по умолчанию (пустые поля или первый уровень)
            // }

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
            // bestSolutions = null; // Очищаем предыдущие решения (если поле bestSolutions существует)
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

                // Если вы уверены, что UnitUtils.Convert с DUT_ префиксом работает в вашей версии API,
                // можете раскомментировать этот блок и закомментировать строку выше:
                // try
                // {
                //      // Получаем единицы длины документа Revit
                //      Units projectUnits = document.GetUnits();
                //      FormatOptions fo = projectUnits.GetFormatOptions(UnitType.UT_Length);
                //      DisplayUnitType displayUnits = fo.DisplayUnits;

                //      // Конвертируем из метров в футы (единицы Revit API)
                //      // Используем DUT_METERS и DUT_FEET, если они доступны в вашей версии API
                //      scaleFactor = UnitUtils.Convert(1.0, DisplayUnitType.DUT_METERS, DisplayUnitType.DUT_FEET);

                //      System.Diagnostics.Debug.WriteLine($"CSV Units Scale Factor (Meters to Feet): {scaleFactor}"); // Логируем коэффициент

                //  }
                //  catch (Exception ex)
                //  {
                //      System.Diagnostics.Debug.WriteLine($"Ошибка при определении или применении коэффициента конвертации единиц: {ex.Message}");
                //      MessageBox.Show($"Ошибка при определении единиц проекта. Убедитесь, что единицы заданы корректно. Использование масштаба по умолчанию (1:1).", "Предупреждение о единицах", MessageBoxButton.OK, MessageBoxImage.Warning);
                //      scaleFactor = 1.0; // Используем масштаб 1:1 как запасной вариант, если конвертация не удалась
                //  }


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
                    XYZ pointXYZ_ft = pointData.GetXYZ(scaleFactor);

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
                        pointsInsideFloorsGrouped[i].Add(pointData); // Добавляем Additional_Reinforcement_point сюда
                        foundSlab = true; // Устанавливаем флаг, что плита найдена

                        // Точка найдена в одной плите, переходим к следующей отфильтрованной точке
                        break; // Точка может принадлежать только одной плите на одном уровне
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


                // === 4. Преобразование Additional_Reinforcement_point в Node для каждой плиты ===
                // Этот шаг готовит данные для ReinforcementOptimizer
                // В этом цикле мы пройдемся по pointsInsideFloorsGrouped и создадим соответствующие объекты Node
                var nodesForOptimizer = new List<List<Node>>();

                // Определяем пороговые значения основного армирования для каждого направления (-1 если не выбрано)
                // Эти значения нужны для создания Node, так как Optimizer использует их для расчета requiredAs
                double as1xThreshold = useAs1X ? mainFitValue : -1;
                double as2xThreshold = useAs2X ? mainFitValue : -1;
                double as3yThreshold = useAs3Y ? mainFitValue : -1;
                double as4yThreshold = useAs4Y ? mainFitValue : -1;


                for (int i = 0; i < pointsInsideFloorsGrouped.Count; i++)
                {
                    List<Additional_Reinforcement_point> pointsOnThisSlab = pointsInsideFloorsGrouped[i]; // Теперь это List<Additional_Reinforcement_point>
                    List<Node> nodesForThisSlab = pointsOnThisSlab
                        .Select(p => new Node // Используем класс Node, определение которого добавлено ниже
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
                    // Это сообщение уже покрыто проверкой totalPointsInsideFloors == 0
                    // MessageBox.Show($"После привязки к плитам и подготовки данных не осталось узлов для расчета.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                    // this.IsEnabled = true;
                    // return;
                }


                // === 5. Получаем информацию об отверстиях для всех найденных плит ===
                // ReinforcementOptimizer.GetOpeningsFromRevit - этот метод, вероятно, должен быть доступен
                // Он должен принимать List<Floor> и возвращать List<List<XYZ>> или аналогичную структуру для отверстий
                // Этот метод также должен выполняться в контексте Revit API, если он работает с геометрией
                // Если он просто преобразует уже полученные CurveLoop, то может работать и в фоне.
                var openings = ReinforcementOptimizer.GetOpeningsFromRevit(floors); // Получаем отверстия


                // === 6. Запускаем расчетный алгоритм (ReinforcementOptimizer) ===
                // Этот шаг может быть длительным и, возможно, должен выполняться в фоновом потоке,
                // но сам ReinforcementOptimizer не должен напрямую работать с Revit API.
                // Если ReinforcementOptimizer.FindBestSolutions не вызывает API, его можно обернуть в Task.Run.
                // Вам потребуется адаптировать или реализовать класс ReinforcementOptimizer и его зависимости (Node, ReinforcementSolution, RebarConfig).

                // Создаем экземпляр ReinforcementOptimizer
                // Вам нужно будет передать ему необходимые данные (отверстия, параметры арматуры, настройки)
                var optimizer = new ReinforcementOptimizer // Используем класс ReinforcementOptimizer, определение которого добавлено ниже
                {
                    Openings = openings, // Передаем информацию об отверстиях (теперь свойство Openings существует)
                    NumOfSol = maxSolutions, // Максимальное число решений
                                             // Передаем ПОРОГОВЫЕ значения основного армирования по выбранным направлениям
                    BasicReinforcement = new[] { as1xThreshold, as2xThreshold, as3yThreshold, as4yThreshold },
                    // Стандартные длины стержней и доступные конфигурации арматуры (диаметр-шаг-цена)
                    // Эти данные должны быть загружены из JSON при открытии окна (с помощью DataFile)
                    // Вам нужно будет адаптировать загрузку JSON и преобразование в нужные структуры (List<double> и List<RebarConfig>)
                    // StandardLengths = DataFile.StandardLengths; // Пример использования DataFile
                    // AvailableRebars = DataFile.AvailableRebars; // Пример использования DataFile
                };

                // Вызываем основной метод оптимизатора для поиска лучших решений
                // Передаем списки узлов, сгруппированные по плитам, количество решений и сами плиты
                // bestSolutions = await Task.Run(() => optimizer.FindBestSolutions(nodesForOptimizer, maxSolutions, floors));
                // optimizer = null; // Очищаем оптимизатор после использования

                // === 7. Обработка и отображение результатов (будет реализована далее) ===
                // - Проверка, были ли найдены решения
                // - Заполнение ZonesTable данными из полученных решений bestSolutions
                // - Активация кнопок ApplyButton, PlanarButton, CancelButton

                // MessageBox.Show($"Геометрическая фильтрация и подготовка узлов завершена. Найдено {totalPointsInsideFloors} узлов внутри плит. Дальнейшая логика расчета и оптимизации будет реализована.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);


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
            // try
            // {
            //     // Сохраняем выбранное имя уровня
            //     Properties.Settings.Settings.Default.FlrName = LevelComboBox.SelectedItem?.ToString() ?? "";
            //     Properties.Settings.Settings.Default.MainFit = ArmTextBox.Text;
            //     // MaxSolTextBox сохраняется при успешном парсинге в CalculateButton_Click
            //     // Properties.Settings.Settings.Default.MaxSol = maxSolutions; // Не сохраняем здесь, чтобы избежать дублирования
            //     Properties.Settings.Settings.Default.Save();
            // }
            // catch (Exception ex)
            // {
            //      // MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            // }
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
            // TODO: Реализовать логику 2D визуализации (если требуется)
            // Возможно, потребуется отдельный ExternalEvent и обработчик для 2D
            MessageBox.Show("Логика 2D визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // Сохраняем выбранное имя уровня
                //Properties.Settings.Settings.Default.FlrName = LevelComboBox.SelectedItem?.ToString() ?? "";
                //Properties.Settings.Settings.Default.MainFit = ArmTextBox.Text;
                // MaxSolTextBox сохраняется при успешном парсинге в CalculateButton_Click
                // Properties.Settings.Settings.Default.MaxSol = maxSolutions; // Не сохраняем здесь, чтобы избежать дублирования
                //Properties.Settings.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                // MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // --- Вспомогательные классы и методы ---

        // Перемещено определение класса Additional_Reinforcement_point на уровень пространства имен
        /// <summary>
        /// Представляет точку из CSV файла с координатами и данными армирования.
        /// Координаты хранятся в исходных единицах CSV (например, метры).
        /// </summary>
        


        /// <summary>
        /// Проверяет, находится ли точка внутри заданного CurveLoop (полигона).
        /// Адаптировано из референсного проекта.
        /// </summary>
        /// <param name="loop">Контур (полигон) для проверки.</param>
        /// <param name="point">Проверяемая точка (в тех же единицах, что и контур - футы Revit API).</param>
        /// <returns>True, если точка находится внутри контура, иначе False.</returns>
        // Этот метод должен быть статическим, чтобы его можно было вызвать без создания экземпляра класса ReinforcementInputWindow
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


    } // Конец класса ReinforcementInputWindow

    // === Добавленные скелеты классов для компиляции ===
    // Эти классы нужно будет полностью адаптировать или реализовать на следующих шагах.
    // Они добавлены здесь временно, чтобы код в ReinforcementInputWindow компилировался.

    /// <summary>
    /// Представляет точку армирования в формате, используемом расчетным алгоритмом (ReinforcementOptimizer).
    /// Координаты хранятся в футах Revit API.
    /// Значения As* хранятся в исходных единицах CSV, или -1, если направление не выбрано.
    /// </summary>
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
        // Добавляем публичное свойство Openings
        public List<List<XYZ>> Openings { get; set; } // Отверстия в плитах (если нужны оптимизатору)
        public int NumOfSol { get; set; } // Количество решений для поиска
        public double[] BasicReinforcement { get; set; } // Массив порогов для As1X, As2X, As3Y, As4Y (-1 если не выбрано)
        public List<double> StandardLengths { get; set; } // Стандартные длины стержней
        public List<RebarConfig> AvailableRebars { get; set; } // Доступные конфигурации арматуры
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

    // TODO: Добавить скелет класса DataFile для работы с JSON данными об арматуре
    // public static class DataFile { ... }

    // TODO: Добавить скелет класса ProgressWindow для отображения прогресса
    // public partial class ProgressWindow : Window { ... }


    // === Скелет класса CsvFileReader ===
    // Этот класс должен содержать логику для чтения CSV файла.
    // Вам нужно будет адаптировать или реализовать его полностью.
    //public static class CsvFileReader
    //{
    //    /// <summary>
    //    /// Читает точки заданного типа из CSV файла.
    //    /// </summary>
    //    /// <param name="path">Путь к CSV файлу.</param>
    //    /// <param name="type">Тип объектов для чтения (например, "Floor").</param>
    //    /// <param name="error">Выходной параметр для сообщения об ошибках парсинга строк.</param>
    //    /// <returns>Список точек Additional_Reinforcement_point или null при критической ошибке файла.</returns>
    //    public static List<Additional_Reinforcement_point> ReadAllPointsOfType(string path, string type, out string error)
    //    {
    //        error = ""; // Инициализируем сообщение об ошибках
    //        List<Additional_Reinforcement_point> points = new List<Additional_Reinforcement_point>();
    //        StringBuilder errorBuilder = new StringBuilder();
    //        int lineNumber = 0;

    //        try
    //        {
    //            using (StreamReader reader = new StreamReader(path))
    //            {
    //                string headerLine = reader.ReadLine(); // Пропускаем заголовок
    //                lineNumber++;

    //                while (!reader.EndOfStream)
    //                {
    //                    string line = reader.ReadLine();
    //                    lineNumber++;
    //                    if (string.IsNullOrWhiteSpace(line)) continue;

    //                    string[] values = line.Split(';'); // Разделитель - точка с запятой

    //                    // Ожидаемый формат: Type;Number;X;Y;Z;ZMin;As1X;As2X;As3Y;As4Y
    //                    // Проверяем количество столбцов
    //                    if (values.Length < 10)
    //                    {
    //                        errorBuilder.AppendLine($"Строка {lineNumber}: Недостаточно столбцов ({values.Length}). Пропущена.");
    //                        continue;
    //                    }

    //                    try
    //                    {
    //                        string currentType = values[0].Trim();
    //                        int number = int.Parse(values[1].Trim());
    //                        double x = double.Parse(values[2].Trim(), CultureInfo.InvariantCulture);
    //                        double y = double.Parse(values[3].Trim(), CultureInfo.InvariantCulture);
    //                        double z = double.Parse(values[4].Trim(), CultureInfo.InvariantCulture); // Z центр
    //                        double zMin = double.Parse(values[5].Trim(), CultureInfo.InvariantCulture); // Z минимум
    //                        double as1x = double.Parse(values[6].Trim(), CultureInfo.InvariantCulture);
    //                        double as2x = double.Parse(values[7].Trim(), CultureInfo.InvariantCulture);
    //                        double as3y = double.Parse(values[8].Trim(), CultureInfo.InvariantCulture);
    //                        double as4y = double.Parse(values[9].Trim(), CultureInfo.InvariantCulture);

    //                        // Фильтруем по заданному типу
    //                        if (currentType.Equals(type, StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            points.Add(new Additional_Reinforcement_point(currentType, number, x, y, z, zMin, as1x, as2x, as3y, as4y));
    //                        }
    //                    }
    //                    catch (FormatException)
    //                    {
    //                        errorBuilder.AppendLine($"Строка {lineNumber}: Ошибка парсинга числовых данных. Пропущена.");
    //                    }
    //                    catch (IndexOutOfRangeException)
    //                    {
    //                        errorBuilder.AppendLine($"Строка {lineNumber}: Ошибка доступа к столбцу. Пропущена.");
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        errorBuilder.AppendLine($"Строка {lineNumber}: Неизвестная ошибка при парсинге: {ex.Message}. Пропущена.");
    //                    }
    //                }
    //            }
    //        }
    //        catch (FileNotFoundException)
    //        {
    //            MessageBox.Show($"Файл не найден: {path}", "Ошибка чтения файла", MessageBoxButton.OK, MessageBoxImage.Error);
    //            error = "Файл не найден.";
    //            return null; // Критическая ошибка
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"Произошла ошибка при чтении файла CSV: {ex.Message}", "Ошибка чтения файла", MessageBoxButton.OK, MessageBoxImage.Error);
    //            error = $"Ошибка при чтении файла: {ex.Message}";
    //            return null; // Критическая ошибка
    //        }

    //        error = errorBuilder.ToString(); // Сохраняем накопленные ошибки парсинга строк
    //        return points; // Возвращаем список прочитанных точек
    //    }
    //    // TODO: Добавить другие методы CsvFileReader, если они есть в референсе
    //}


    // === Скелеты классов External Event Handlers ===
    // Эти классы должны реализовывать логику работы с Revit API в потоке Revit.

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
                TaskDialog.Show("Визуализация", "Начало транзакции.");
                trans.Start();
                TaskDialog.Show("Визуализация", "Транзакция начата.");
                try
                {
                    // Получаем ElementId для категории "Обобщенные модели" (Generic Models)
                    // В этой категории удобно создавать DirectShape
                    ElementId modelCategoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                    TaskDialog.Show("Визуализация", $"Получен ID категории Обобщенные модели: {modelCategoryId.IntegerValue}");

                    // Попытка найти или создать материалы для визуализации
                    Material materialInside = FindOrCreateMaterial(doc, "Material_InsideFloors", new Autodesk.Revit.DB.Color(0, 0, 255)); // Синий
                    Material materialOutside = FindOrCreateMaterial(doc, "Material_OutsideFloors", new Autodesk.Revit.DB.Color(255, 0, 0)); // Красный
                    ElementId graphicsStyleId = ElementId.InvalidElementId;

                    ElementId materialInsideId = materialInside != null ? materialInside.Id : ElementId.InvalidElementId;
                    ElementId materialOutsideId = materialOutside != null ? materialOutside.Id : ElementId.InvalidElementId;
                    //SolidOptions solidOptions = new SolidOptions(GeometryPrecisionLevels.Medium);

                    TaskDialog.Show("Визуализация", "SolidOptions созданы.");

                    // Визуализация точек внутри плит (например, синим цветом)
                    if (PointsInsideFloors != null)
                    {
                        TaskDialog.Show("Визуализация", $"Найдено групп точек внутри плит: {PointsInsideFloors.Count}");
                        int pointCountInside = 0;
                        // Создаем SolidOptions для точек внутри плит, используя ID материала и InvalidElementId для графического стиля
                        SolidOptions solidOptionsInside = new SolidOptions(materialInsideId, graphicsStyleId);
                        foreach (var slabPointsList in PointsInsideFloors)
                        {
                            foreach (var pointData in slabPointsList)
                            {
                                pointCountInside++;
                                //// Получаем координаты точки в футах Revit API
                                //// Используем константу METERS_TO_FEET из ReinforcementInputWindow
                                //XYZ pointXYZ_ft = pointData.GetXYZ(ReinforcementInputWindow.METERS_TO_FEET);

                                //// Создаем сферу как геометрию для DirectShape
                                //// Радиус сферы можно настроить (например, 0.1 фута = ~3 см)
                                //double sphereRadius = 0.1;
                                //Sphere sphere = Sphere.Create(pointXYZ_ft, sphereRadius);
                                //List<GeometryObject> geometryObjects = new List<GeometryObject> { sphere };

                                //// Создаем DirectShape
                                //DirectShape ds = DirectShape.CreateElement(doc, modelCategoryId);
                                //ds.SetShape(geometryObjects);
                                //ds.Name = $"Point_{pointData.Number}_Inside"; // Присваиваем имя для идентификации

                                //// Назначаем материал, если он найден или создан
                                //if (materialInside != null)
                                //{
                                //    ds.SetMaterialIds(new List<ElementId> { materialInside.Id });
                                //}

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

                                // Назначаем материал, если он найден или создан
                                //if (materialInside != null)
                                //{
                                //    ds.SetMaterialIds(new List<ElementId> { materialInside.Id });
                                //}
                                //Parameter materialParam = ds.get_Parameter(BuiltInParameter.FAMILY_ELEM_SUBCATEGORY);
                                //if (materialParam != null && materialOutsideId != ElementId.InvalidElementId)
                                //{
                                //    try
                                //    {
                                //        materialParam.Set(materialOutsideId);
                                //    }
                                //    catch (Exception setParamEx)
                                //    {
                                //        System.Diagnostics.Debug.WriteLine($"Ошибка установки параметра материала для DirectShape: {setParamEx.Message}");
                                //        TaskDialog.Show("Ошибка параметра", $"Ошибка установки параметра материала для точки {pointData.Number}: {setParamEx.Message}");
                                //    }
                                //}
                                //else if (materialParam == null)
                                //{
                                //    System.Diagnostics.Debug.WriteLine($"Параметр FAMILY_ELEM_SUBCATEGORY не найден для DirectShape.");
                                //    TaskDialog.Show("Ошибка параметра", $"Параметр FAMILY_ELEM_SUBCATEGORY не найден для DirectShape.");
                                //}
                                //else if (materialInsideId == ElementId.InvalidElementId)
                                //{
                                //    System.Diagnostics.Debug.WriteLine($"ID материала Inside недействителен.");
                                //    TaskDialog.Show("Ошибка материала", $"ID материала Inside недействителен.");
                                //}
                                // Конец назначения материала
                            }
                        }
                        TaskDialog.Show("Визуализация", $"Обработано точек внутри плит: {pointCountInside}");
                    }

                    // Визуализация точек вне плит (например, красным цветом)
                    if (PointsOutsideFloors != null)
                    {
                        TaskDialog.Show("Визуализация", $"Найдено точек вне плит: {PointsOutsideFloors.Count}");
                        int pointCountOutside = 0;
                        //foreach (var pointData in PointsOutsideFloors)
                        //{
                        //    // Получаем координаты точки в футах Revit API
                        //    XYZ pointXYZ_ft = pointData.GetXYZ(ReinforcementInputWindow.METERS_TO_FEET);

                        //    // Создаем сферу как геометрию для DirectShape
                        //    double sphereRadius = 0.1;
                        //    Sphere sphere = Sphere.Create(pointXYZ_ft, sphereRadius);
                        //    List<GeometryObject> geometryObjects = new List<GeometryObject> { sphere };

                        //    // Создаем DirectShape
                        //    DirectShape ds = DirectShape.CreateElement(doc, modelCategoryId);
                        //    ds.SetShape(geometryObjects);
                        //    ds.Name = $"Point_{pointData.Number}_Outside"; // Присваиваем имя для идентификации

                        //    // Назначаем материал, если он найден или создан
                        //    if (materialOutside != null)
                        //    {
                        //        ds.SetMaterialIds(new List<ElementId> { materialOutside.Id });
                        //    }
                        //}
                        SolidOptions solidOptionsOutside = new SolidOptions(materialOutsideId, graphicsStyleId);

                        foreach (var pointData in PointsOutsideFloors)
                        {
                            pointCountOutside++;
                            // Получаем координаты точки в футах Revit API
                            XYZ pointXYZ_ft = pointData.GetXYZ();

                            // === Создание простой геометрии для визуализации (вместо Sphere) ===
                            List<GeometryObject> geometryObjects = new List<GeometryObject>();
                            double size = 1.0; // Размер элемента

                            try
                            {
                                // Создаем BoundingBoxXYZ вокруг точки
                                BoundingBoxXYZ bbox = new BoundingBoxXYZ();
                                bbox.Min = pointXYZ_ft - new XYZ(size / 2, size / 2, size / 2);
                                bbox.Max = pointXYZ_ft + new XYZ(size / 2, size / 2, size / 2);

                                // Создаем Solid из BoundingBoxXYZ
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

                                XYZ extrusionDir = new XYZ(0, 0, size);
                                double extrusionDist = size;
                                Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, extrusionDir, extrusionDist, solidOptionsOutside);

                                if (solid != null && solid.Volume > 0)
                                {
                                    geometryObjects.Add(solid);
                                }
                                else
                                {
                                    geometryObjects.Add(Autodesk.Revit.DB.Point.Create(pointXYZ_ft));
                                }
                            }
                            catch (Exception geomEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка создания геометрии для точки {pointData.Number}: {geomEx.Message}");
                                TaskDialog.Show("Ошибка геометрии", $"Ошибка создания геометрии для точки {pointData.Number}: {geomEx.Message}");
                                //continue; // Пропускаем текущую точку
                                geometryObjects = new List<GeometryObject>();
                            }

                            if (geometryObjects.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Не удалось создать геометрию для точки {pointData.Number}. Пропускаем.");
                                //continue;
                            }


                            // Создаем DirectShape
                            DirectShape ds = DirectShape.CreateElement(doc, modelCategoryId);
                            ds.SetShape(geometryObjects);
                            ds.Name = $"Point_{pointData.Number}_Outside"; // Присваиваем имя для идентификации

                            //Parameter materialParam = ds.get_Parameter(BuiltInParameter.FAMILY_ELEM_SUBCATEGORY);
                            //if (materialParam != null && materialInsideId != ElementId.InvalidElementId)
                            //{
                            //    //ds.SetMaterialIds(new List<ElementId> { materialOutside.Id });
                            //    try
                            //    {
                            //        materialParam.Set(materialInsideId);
                            //    }
                            //    catch (Exception setParamEx)
                            //    {
                            //        TaskDialog.Show("Ошибка параметра", $"Ошибка установки параметра материала для точки {pointData.Number}: {setParamEx.Message}");
                            //        System.Diagnostics.Debug.WriteLine($"Ошибка установки параметра материала для DirectShape: {setParamEx.Message}");
                            //    }
                            //}
                            //else if (materialParam == null)
                            //{
                            //    System.Diagnostics.Debug.WriteLine($"Параметр FAMILY_ELEM_SUBCATEGORY не найден для DirectShape.");
                            //    TaskDialog.Show("Ошибка параметра", $"Параметр FAMILY_ELEM_SUBCATEGORY не найден для DirectShape.");
                            //}
                            //else if (materialOutsideId == ElementId.InvalidElementId)
                            //{
                            //    System.Diagnostics.Debug.WriteLine($"ID материала Outside недействителен.");
                            //    TaskDialog.Show("Ошибка материала", $"ID материала Outside недействителен.");
                            //}
                        }
                        TaskDialog.Show("Визуализация", $"Обработано точек вне плит: {pointCountOutside}");
                    }

                    //trans.Commit(); // Завершаем транзакцию
                    TaskDialog.Show("Визуализация", "Попытка завершить транзакцию.");
                    trans.Commit(); // Завершаем транзакцию
                    TaskDialog.Show("Визуализация", "Транзакция завершена.");
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
