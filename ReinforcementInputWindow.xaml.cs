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

namespace Diplom_Project
{
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

        // Поле для отображения результатов расчета в UI DataGrid
        private DataTable ZonesTable;


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

        /// <summary>
        /// Обработчик нажатия кнопки "Выполнить расчет".
        /// </summary>
        //private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Логика получения параметров из UI, фильтрации точек, привязки к плитам,
        //    // запуска ReinforcementOptimizer и отображения результатов будет здесь
        //    MessageBox.Show("Логика расчета будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
        //}
        /// <summary>
        /// Обработчик нажатия кнопки "Выполнить расчет".
        /// Этот метод теперь будет выполнять фильтрацию точек на основе параметров UI.
        /// </summary>
        private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что исходные точки из CSV загружены
            if (allCsvPoints == null || allCsvPoints.Count == 0)
            {
                MessageBox.Show("Сначала загрузите точки из CSV файла.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Деактивируем UI на время выполнения фильтрации/расчета
            this.IsEnabled = false;
            ApplyButton.IsEnabled = false;
            PlanarButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            ZonesTable.Clear(); // Очищаем предыдущие результаты в таблице
            // bestSolutions = null; // Очищаем предыдущие решения (если поле bestSolutions существует)

            // === 1. Получение параметров из UI ===
            double mainFitValue;
            // Пытаемся распарсить значение основного армирования из ArmTextBox
            if (!double.TryParse(ArmTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out mainFitValue))
            {
                MessageBox.Show("Некорректное значение основного армирования. Пожалуйста, введите число.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return; // Прерываем выполнение
            }

            int maxSolutions;
            // Пытаемся распарсить значение максимального числа решений из MaxSolTextBox
            if (!int.TryParse(MaxSolTextBox.Text, out maxSolutions) || maxSolutions <= 0)
            {
                MessageBox.Show("Некорректное значение максимального числа решений. Пожалуйста, введите целое положительное число.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return; // Прерываем выполнение
            }

            string levelName = FlrTextBox.Text;
            // Проверяем, указано ли имя уровня
            if (string.IsNullOrWhiteSpace(levelName))
            {
                MessageBox.Show("Не указано имя уровня Revit.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return; // Прерываем выполнение
            }

            // Определяем, какие направления армирования выбраны пользователем
            // Используем HasValue и Value для получения булевого значения из Nullable<bool>
            bool useAs1X = As1X_CheckBox.IsChecked.HasValue ? As1X_CheckBox.IsChecked.Value : false;
            bool useAs2X = As2X_CheckBox.IsChecked.HasValue ? As2X_CheckBox.IsChecked.Value : false;
            bool useAs3Y = As3Y_CheckBox.IsChecked.HasValue ? As3Y_CheckBox.IsChecked.Value : false;
            bool useAs4Y = As4Y_CheckBox.IsChecked.HasValue ? As4Y_CheckBox.IsChecked.Value : false;

            // Проверяем, выбрано ли хотя бы одно направление
            if (!useAs1X && !useAs2X && !useAs3Y && !useAs4Y)
            {
                MessageBox.Show("Не выбрано ни одно направление для расчета. Пожалуйста, выберите хотя бы одно.", "Ошибка выбора", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.IsEnabled = true; // Включаем UI обратно
                return; // Прерываем выполнение
            }
            // ==============================================


            // === 2. Фильтрация загруженных точек ===
            // Отбираем только те точки из allCsvPoints, которые нуждаются в ДОПОЛНИТЕЛЬНОМ армировании.
            // Точка нуждается в доп. армировании, если ее значение As* превышает mainFitValue
            // хотя бы в ОДНОМ ИЗ ВЫБРАННЫХ НАПРАВЛЕНИЙ.
            // Также можно добавить фильтрацию по типу точки ("Floor", "Wall", "Node") если необходимо.
            List<Additional_Reinforcement_point> filteredPoints = new List<Additional_Reinforcement_point>();

            // Фильтрация выполняется с помощью LINQ
            filteredPoints = allCsvPoints
                // .Where(p => p.Type.ToLower() != "wall") // Пример: исключаем точки типа "Wall", если это нужно
                .Where(p =>
                    // Условие: точка нуждается в доп. армировании, если для ВЫБРАННОГО направления (useAs*X/Y == true)
                    // значение As* в этой точке (p.As*X/Y) превышает пороговое значение mainFitValue.
                    (useAs1X && p.As1X > mainFitValue) ||
                    (useAs2X && p.As2X > mainFitValue) ||
                    (useAs3Y && p.As3Y > mainFitValue) ||
                    (useAs4Y && p.As4Y > mainFitValue)
                )
                // !!! Важно: Здесь также добавьте фильтрацию по Z-координате, если она все еще нужна
                // Например, если нужно фильтровать точки, находящиеся на определенной Z-отметке:
                // .Where(p => Math.Abs(p.Z - (-9.67847769028871)) < 0.001) // Пример вашей старой фильтрации по Z с допуском (координаты точки и значение в одной системе единиц)
                .ToList(); // Преобразуем результат LINQ в список

            // =======================================


            // Проверяем, остались ли узлы после фильтрации
            if (filteredPoints.Count == 0)
            {
                MessageBox.Show("После фильтрации по основному армированию и выбранным направлениям не осталось узлов, нуждающихся в дополнительном армировании.", "Расчет", MessageBoxButton.OK, MessageBoxImage.Information);
                this.IsEnabled = true; // Включаем UI обратно
                // Логируем (если доступен Tools.CreateLogMessage)
                // ConsoleLog.AppendText(Tools.CreateLogMessage(Tools.CalcErr + ". Нет узлов после фильтрации."));
                return; // Прерываем выполнение
            }

            MessageBox.Show($"Фильтрация завершена. Найдено {filteredPoints.Count} узлов, нуждающихся в доп. армировании.", "Фильтрация", MessageBoxButton.OK, MessageBoxImage.Information);


            // --- Здесь будет дальнейшая логика РАСЧЕТА ---
            // 3. Привязка отфильтрованных точек к плитам Revit (нужен доступ к document и геометрии)
            // 4. Создание объектов Node для Optimizer
            // 5. Получение отверстий из плит
            // 6. Запуск ReinforcementOptimizer
            // 7. Обработка результатов ReinforcementOptimizer и заполнение ZonesTable
            // 8. Отображение прогресса (нужен ProgressWindow)
            // 9. Обработка ошибок
            // --------------------------------------------


            this.IsEnabled = true; // Включаем UI обратно после завершения (или ошибки) фильтрации

            // Если дальнейший расчет успешен и получены решения:
            // ApplyButton.IsEnabled = true;
            // PlanarButton.IsEnabled = true;
            // CancelButton.IsEnabled = true;
        }


        /// <summary>
        /// Обработчик нажатия кнопки "Применить (3D)".
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика запуска ExternalEvent для 3D визуализации будет здесь
            MessageBox.Show("Логика 3D визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Логика отмены визуализации будет реализована здесь.", "Следующий шаг", MessageBoxButton.OK, MessageBoxImage.Information);
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
