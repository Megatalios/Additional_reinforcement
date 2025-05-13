using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System.Windows.Forms; // Для OpenFileDialog
using System.Globalization; // Для парсинга чисел


namespace Diplom_Project
{
    //class MainApp: IExternalApplication
    //{
    //    const String Tab_name = "Расчет доп армирования";

    //    public Result OnStartup(UIControlledApplication application)
    //    {
    //        application.CreateRibbonTab(Tab_name);
    //        RibbonPanel our_main_panel = application.CreateRibbonPanel(Tab_name, "Расчет доп зон армирования");
    //        PushButton test_knopka = null;
    //        {
    //            PushButtonData data_of_button = new PushButtonData("test_button", "Это кнопка", System.Reflection.Assembly.GetExecutingAssembly().Location, "Diplom_Project.AboutShow");
    //            test_knopka = our_main_panel.AddItem(data_of_button) as PushButton;
    //        }



    //        return Result.Succeeded;
    //    }
    //    public Result OnShutdown(UIControlledApplication application)
    //    {
    //        return Result.Succeeded;
    //    }
    //}



    //class MainApp : IExternalApplication
    //{
    //    const String Tab_name = "Расчет доп армирования";

    //    public Result OnStartup(UIControlledApplication application)
    //    {
    //        application.CreateRibbonTab(Tab_name);
    //        RibbonPanel our_main_panel = application.CreateRibbonPanel(Tab_name, "Расчет доп зон армирования");
    //        PushButton test_knopka = null;
    //        {
    //            PushButtonData data_of_button = new PushButtonData("test_button", "Это кнопка", System.Reflection.Assembly.GetExecutingAssembly().Location, "Diplom_Project.AboutShow");
    //            test_knopka = our_main_panel.AddItem(data_of_button) as PushButton;
    //        }



    //        return Result.Succeeded;
    //    }
    //    public Result OnShutdown(UIControlledApplication application)
    //    {
    //        return Result.Succeeded;
    //    }
    //}

    class MainApp : IExternalApplication
    {
        const String Tab_name = "Расчет доп армирования";

        public Result OnStartup(UIControlledApplication application)
        {
            // Создание вкладки
            try
            {
                application.CreateRibbonTab(Tab_name);
            }
            catch (Exception)
            {
                // Возможно, вкладка уже существует
            }


            // Создание панели
            RibbonPanel our_main_panel = application.CreateRibbonPanel(Tab_name, "Расчет доп зон армирования");

            // Добавление кнопки
            PushButtonData data_of_button = new PushButtonData(
                "AboutShowCommand", // Внутреннее имя кнопки (уникальное)
                "Показать точки", // Текст на кнопке
                System.Reflection.Assembly.GetExecutingAssembly().Location, // Путь к текущей сборке
                "Diplom_Project.AboutShow" // Полное имя класса команды (включая Namespace)
            );

            // Добавление кнопки на панель
            PushButton test_knopka = our_main_panel.AddItem(data_of_button) as PushButton;

            // Можно добавить иконку к кнопке (необязательно)
            // Uri uriImage = new Uri("pack://application:,,,/YourPluginAssemblyName;component/Resources/YourIcon.png");
            // BitmapImage largeImage = new BitmapImage(uriImage);
            // test_knopka.LargeImage = largeImage;


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Здесь можно выполнить очистку, если необходимо
            return Result.Succeeded;
        }
    }


    public class Additional_Reinforcement_point
    {
        //public double x, y, z;
        //public double force; // Значение силы, связанное с точкой
        //public int clusterID; // ID кластера (пока не используется для визуализации)

        //public Additional_Reinforcement_point(double x, double y, double z, double force)
        //{
        //    this.x = x;
        //    this.y = y;
        //    this.z = z;
        //    this.force = force;
        //    this.clusterID = 0; // Инициализируем ID кластера
        //}
        // Координаты
        public double x, y, z;

        // Требуемое армирование по направлениям из CSV
        public double As1X { get; set; }
        public double As2X { get; set; }
        public double As3Y { get; set; }
        public double As4Y { get; set; }

        // Другие данные из CSV
        public string Type { get; set; }
        public int Number { get; set; }
        public double ZCenter { get; set; } // Дублирует z из 4го столбца, но сохраняем
        public double ZMin { get; set; } // Из 5го столбца

        // Дополнительные поля для кластеризации/обработки
        public int ClusterID { get; set; } // Для кластеризации
        // public double Force { get; set; } // Поле 'force' можно убрать или пересчитывать по выбранным As*

        // Конструктор для парсинга из CSV
        public Additional_Reinforcement_point(string type, int number, double x, double y, double z, double zMin, double as1x, double as2x, double as3y, double as4y)
        {
            this.Type = type;
            this.Number = number;
            this.x = x;
            this.y = y;
            this.z = z; // Z из 4го столбца CSV
            this.ZCenter = z; // Дублирование Z центра
            this.ZMin = zMin; // Z минимум из 5го столбца

            this.As1X = as1x;
            this.As2X = as2x;
            this.As3Y = as3y;
            this.As4Y = as4y;

            this.ClusterID = 0; // Инициализация
            // this.Force = 0; // Можно установить позже или удалить, если не используется
        }

        // Если нужно XYZ из Revit API
        public XYZ GetXYZ()
        {
            // !!! Важно: здесь должна быть конвертация из единиц CSV в футы Revit, если они разные !!!
            // double scaleFactor = UnitUtils.Convert(1.0, DisplayUnitType.DUF_METERS, DisplayUnitType.DUF_FEET); // Пример для метров
            double scaleFactor = 1.0; // Предполагаем, что x,y,z уже в футах или конвертация будет позже
            return new XYZ(x * scaleFactor, y * scaleFactor, z * scaleFactor);
        }

        public XYZ GetXYZ(double scaleFactor)
        {
            // !!! Важно: здесь должна быть конвертация из единиц CSV в футы Revit, если они разные !!!
            // double scaleFactor = UnitUtils.Convert(1.0, DisplayUnitType.DUF_METERS, DisplayUnitType.DUF_FEET); // Пример для метров
            // Предполагаем, что x,y,z уже в футах или конвертация будет позже
            return new XYZ(x * scaleFactor, y * scaleFactor, z * scaleFactor);
        }
    }


    public static class CsvFileReader // Пример имени статического класса
    {
        // Ваш метод ReadFile
        //public static List<Additional_Reinforcement_point> ReadFile(string Filename, string Type_of_object, double threshold)
        //{
        //    List<Additional_Reinforcement_point> result_list = new List<Additional_Reinforcement_point>();
        //    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Регистрация провайдера кодировок
        //    System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(1251); // Указанная вами кодировка

        //    try
        //    {
        //        using (StreamReader sr = new StreamReader(Filename, encoding))
        //        {
        //            bool header = true;
        //            string currentLine;
        //            while ((currentLine = sr.ReadLine()) != null)
        //            {
        //                if (header)
        //                {
        //                    header = false;
        //                    continue; // Пропускаем строку заголовка
        //                }

        //                string[] splitted_line = currentLine.Split(';');
        //                // Убедимся, что в строке достаточно столбцов для парсинга
        //                if (splitted_line.Length < 10) // Минимум 10 столбцов согласно вашим индексам [0]..[9]
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"Пропущена строка из-за недостатка столбцов: {currentLine}");
        //                    continue;
        //                }

        //                // Ваша логика фильтрации и парсинга из предоставленного кода
        //                if (splitted_line[0].Trim().Equals(Type_of_object, StringComparison.OrdinalIgnoreCase)) // Проверка типа объекта (без учета регистра)
        //                {
        //                    // Проверка условия value > threshold (используем splitted_line[6] как "value")
        //                    if (double.TryParse(splitted_line[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double value) && value > threshold)
        //                    {
        //                        // Парсинг координат и силы (используем splitted_line[6] как "force")
        //                        if (double.TryParse(splitted_line[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
        //                            double.TryParse(splitted_line[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&
        //                            double.TryParse(splitted_line[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double z) && // Z из 4го столбца
        //                            double.TryParse(splitted_line[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double forceValue)) // Сила из 6го столбца (As1X)
        //                        {
        //                            // Дополнительная фильтрация по Z-координате
        //                            // !!! Внимание: жестко заданное значение -9.67847769028871. Убедитесь, что это намеренно и корректно для ваших данных.
        //                            if (Math.Abs(z - (-9.67847769028871)) < 0.001) // Сравнение с допуском
        //                            {
        //                                // Создаем объект вашего класса
        //                                result_list.Add(new Additional_Reinforcement_point(x, y, z, forceValue));
        //                            }
        //                        }
        //                        else
        //                        {
        //                            System.Diagnostics.Debug.WriteLine($"Не удалось распарсить числовые данные в строке: {currentLine}");
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (FileNotFoundException)
        //    {
        //        System.Windows.Forms.MessageBox.Show("Файл не найден.", "Ошибка чтения CSV", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //        return null; // Возвращаем null в случае ошибки
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Windows.Forms.MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка чтения CSV", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //        System.Diagnostics.Debug.WriteLine($"Ошибка при чтении файла CSV: {ex.ToString()}");
        //        return null; // Возвращаем null в случае ошибки
        //    }

        //    return result_list;
        //}

        
            // Шаблон заголовков из вашего файла Tools.cs (предполагаем, что Tools доступен)
            // Если Tools недоступен, скопируйте HeadersTemplate и HeadersCount сюда.
            // private static readonly string[] HeadersTemplate = { "Тип", "Номер", "Координата X узлов", "Координата Y узлов", "Координата Z центр", "Координата Z минимум", "As1X", "As2X", "As3Y", "As4Y" };
            // private const int HeadersCount = 10;

            // Этот метод читает все точки определенного типа из файла и возвращает список.
            // Фильтрация по значениям As или Z ЗДЕСЬ НЕ ВЫПОЛНЯЕТСЯ.
        public static List<Additional_Reinforcement_point> ReadAllPointsOfType(string filePath, string typeToRead, out string errorMessage)
        {
            List<Additional_Reinforcement_point> allPoints = new List<Additional_Reinforcement_point>();
            // Используйте кодировку 1251
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // Убедитесь, что пакет установлен
            var encoding = System.Text.Encoding.GetEncoding(1251);
            errorMessage = ""; // Инициализируем сообщение об ошибке
       
            try
            {
                using (StreamReader sr = new StreamReader(filePath, encoding))
                {
                    // Пропускаем строку заголовка
                    string headerLine = sr.ReadLine();
                    if (headerLine == null) { errorMessage = "Файл пуст."; return null; } // Возвращаем null при критической ошибке
       
                    // Опционально: проверка заголовков (можно скопировать из UserControl1.xaml.cs)
                    string[] headers = headerLine.Split(';');
                    if (headers.Length < Tools.HeadersCount) // Предполагаем доступ к Tools.HeadersCount
                    {
                        // Можно вывести предупреждение, но не прерывать чтение
                        System.Diagnostics.Debug.WriteLine($"Предупреждение: Недостаточно столбцов в CSV. Ожидалось минимум {Tools.HeadersCount}, найдено {headers.Length}.");
                    }
       
       
                    string line;
                    int lineNumber = 1; // Начинаем счет после заголовка
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNumber++;
                        string[] fields = line.Split(';');
       
                        // Проверка на минимальное количество столбцов
                        if (fields.Length < Tools.HeadersCount) // Предполагаем доступ к Tools.HeadersCount
                        {
                            System.Diagnostics.Debug.WriteLine($"Пропущена строка {lineNumber} из-за недостатка столбцов ({fields.Length}).");
                            continue;
                        }
       
                        try
                        {
                            // Проверяем тип объекта в строке (используем 0-й столбец)
                            if (fields[0].Trim().Equals(typeToRead, StringComparison.OrdinalIgnoreCase))
                            {
                                // Парсим все необходимые данные, используя CultureInfo.InvariantCulture для чисел
                                // Используйте индексы столбцов согласно Tools.HeadersTemplate из вашего Tools.cs
                                double x = double.Parse(fields[2], CultureInfo.InvariantCulture);
                                double y = double.Parse(fields[3], CultureInfo.InvariantCulture);
                                double z = double.Parse(fields[4], CultureInfo.InvariantCulture); // Z центр
                                double zMin = double.Parse(fields[5], CultureInfo.InvariantCulture); // Z минимум
       
                                double as1x = double.Parse(fields[6], CultureInfo.InvariantCulture);
                                double as2x = double.Parse(fields[7], CultureInfo.InvariantCulture);
                                double as3y = double.Parse(fields[8], CultureInfo.InvariantCulture);
                                double as4y = double.Parse(fields[9], CultureInfo.InvariantCulture);
       
                                int number = int.Parse(fields[1]);
                                string type = fields[0].Trim();
       
                                // Создаем объект точки с полными данными
                                allPoints.Add(new Additional_Reinforcement_point(type, number, x, y, z, zMin, as1x, as2x, as3y, as4y));
                            }
                        }
                        catch (FormatException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка формата данных в строке {lineNumber}: {ex.Message}. Строка: {line}");
                            errorMessage += $"Ошибка формата в строке {lineNumber}: {ex.Message}\n"; // Собираем ошибки
                            continue; // Пропускаем ошибочную строку
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка доступа к столбцу в строке {lineNumber}: {ex.Message}. Строка: {line}");
                            errorMessage += $"Ошибка столбца в строке {lineNumber}: {ex.Message}\n"; // Собираем ошибки
                            continue;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Неизвестная ошибка при обработке строки {lineNumber}: {ex.Message}. Строка: {line}");
                            errorMessage += $"Ошибка в строке {lineNumber}: {ex.Message}\n"; // Собираем ошибки
                            continue;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                errorMessage = "Файл не найден.";
                return null; // Возвращаем null при ошибке файла
            }
            catch (InvalidDataException ex)
            {
                errorMessage = $"Ошибка в структуре файла CSV: {ex.Message}";
                return null; // Возвращаем null при ошибке структуры (например, пустой файл)
            }
            catch (Exception ex)
            {
                errorMessage = $"Общая ошибка при чтении файла: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Общая ошибка при чтении CSV: {ex.ToString()}");
                return null; // Возвращаем null при общей ошибке
            }
       
            // Если были ошибки формата/парсинга в строках, сообщаем об этом (опционально)
            // If (!string.IsNullOrEmpty(errorMessage))
            // {
            //     MessageBox.Show($"При чтении файла были пропущены строки из-за ошибок формата:\n{errorMessage}", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            // }
       
            return allPoints; // Возвращаем список всех прочитанных точек заданного типа
        }
       
    }

    [Transaction(TransactionMode.Manual)]
    class AboutShow : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref String message, ElementSet elements)
        {
            // Получаем объекты UIApplication и UIDocument для доступа к текущему сеансу Revit
            UIApplication uiApplication = commandData.Application;
            UIDocument uiDocument = uiApplication.ActiveUIDocument;

            try
            {
                // === Основное изменение: Вместо выполнения логики здесь, мы создаем и показываем главное окно UI ===

                // Создаем экземпляр вашего нового главного окна UI (ReinforcementInputWindow)
                // Убедитесь, что класс ReinforcementInputWindow определен и доступен в этом namespace или через using.
                // Передаем UIDocument в конструктор окна, чтобы оно имело доступ к текущему документу Revit
                ReinforcementInputWindow mainWindow = new ReinforcementInputWindow(uiDocument);

                // Показываем окно как модальное.
                // ShowDialog() блокирует основной поток Revit API до закрытия окна,
                // что позволяет выполнять операции с документом из кода окна.
                mainWindow.Show();

                // Результат команды Execute может зависеть от того, как завершилось взаимодействие с окном,
                // но для простоты вернем Succeeded, если окно показалось без ошибок.
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Обработка ошибок, которые могли произойти при создании или показе окна
                message = "Произошла ошибка при открытии окна плагина: " + ex.Message;
                // Используем TaskDialog для вывода сообщений пользователю в Revit
                TaskDialog.Show("Ошибка", message);
                // Возвращаем Failed, чтобы показать, что команда не выполнилась успешно
                return Result.Failed;
            }
        }

        //public Result Execute(ExternalCommandData commandData, ref String message, ElementSet elements)
        //{
        //    UIApplication uiApplication = commandData.Application;
        //    UIDocument uiDocument = uiApplication.ActiveUIDocument;
        //    Document document = uiDocument.Document;

        //    // 1. Выбираем CSV файл
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        //    openFileDialog.Title = "Выберите файл с точками армирования";

        //    if (openFileDialog.ShowDialog() != DialogResult.OK)
        //    {
        //        return Result.Cancelled; // Пользователь отменил выбор
        //    }

        //    string filePath = openFileDialog.FileName;

        //    // === Параметры для вашего метода ReadFile ===
        //    // Вам нужно определить, какие значения Type_of_object и threshold использовать
        //    string typeOfObject = "Floor"; // Пример: фильтруем по типу "Node"
        //    double threshold = 1.0; // Пример: фильтруем по значению > 1.0 в 6-м столбце (As1X)
        //    // ============================================

        //    // 2. Читаем точки из CSV файла, используя ВАШ метод
        //    List<Additional_Reinforcement_point> pointsToVisualize = CsvFileReader.ReadFile(filePath, typeOfObject, threshold);

        //    if (pointsToVisualize == null || pointsToVisualize.Count == 0)
        //    {
        //        System.Windows.Forms.MessageBox.Show("Не удалось прочитать точки из файла, файл пуст, или все точки отфильтрованы.", "Информация", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        //        return Result.Succeeded; // Завершаем успешно, но без точек
        //    }

        //    // --- Важное замечание по единицам измерения ---
        //    // Revit API работает в футах. Ваши x, y, z из CSV, вероятно, в метрах или мм.
        //    // ВАШ метод ReadFile сейчас просто парсит double без конвертации в футы.
        //    // ПЕРЕД созданием геометрии в Revit, вам нужно будет конвертировать эти double в футы.
        //    // double scaleFactor = 1.0; // Изначально считаем, что единицы совпадают
        //    // try
        //    // {
        //    //     // Пример конвертации из метров в футы (если ваши CSV в метрах)
        //    //     scaleFactor = UnitUtils.Convert(1.0, DisplayUnitType.DUF_METERS, DisplayUnitType.DUF_FEET);
        //    // }
        //    // catch (Exception ex)
        //    // {
        //    //     System.Diagnostics.Debug.WriteLine($"Не удалось определить единицы проекта для конвертации: {ex.Message}");
        //    //     // Возможно, стоит вывести предупреждение пользователю
        //    // }
        //    // --- Конец замечания по единицам ---


        //    // 3. Создаем визуальные элементы в Revit
        //    using (Transaction tx = new Transaction(document, "Visualize Reinforcement Points"))
        //    {
        //        tx.Start(); // Начинаем транзакцию

        //        Autodesk.Revit.DB.View activeView = document.ActiveView;
        //        if (activeView == null)
        //        {
        //            // Возможно, нужно найти или создать 3D вид, если активного нет
        //            TaskDialog.Show("Ошибка", "Отсутствует активный вид для визуализации.");
        //            tx.RollBack();
        //            return Result.Failed;
        //        }

        //        // Поиск или создание материала для визуализации
        //        //Material pointsMaterial = GetOrCreateMaterial(document, "ReinforcementPointsMaterial", new Color(0, 255, 0));
        //        Autodesk.Revit.DB.Color displayColor = new Autodesk.Revit.DB.Color(0, 0, 255); // Синий цвет для точек


        //        foreach (var pointData in pointsToVisualize)
        //        {
        //            // Используем координаты из вашей точки (после возможной конвертации)
        //            // XYZ point = new XYZ(pointData.x * scaleFactor, pointData.y * scaleFactor, pointData.z * scaleFactor); // С учетом конвертации
        //            XYZ point = new XYZ(pointData.x, pointData.y, pointData.z); // БЕЗ конвертации (предполагаем, что в CSV уже футы или пропускаем конвертацию)


        //            // Создаем простую геометрию (куб) в координатах точки
        //            double size = 0.2; // Размер куба в футах. Адаптируйте под ваши нужды.
        //            Solid pointGeometry = CreateCubeAtPoint(document, point, size);

        //            if (pointGeometry != null)
        //            {
        //                //// Создаем DirectShape
        //                //DirectShape pointShape = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
        //                //pointShape.SetShape(new GeometryObject[] { pointGeometry });
        //                //// Можно использовать данные из точки в имени или параметрах DirectShape
        //                //pointShape.Name = $"Point_{pointData.clusterID}_Force_{pointData.force:F2}"; // Пример имени с force и clusterID

        //                //// Применяем материал
        //                //if (pointsMaterial != null)
        //                //{
        //                //    pointShape.SetMaterialId(pointsMaterial.Id, false);
        //                //}


        //                // Создаем DirectShape
        //                DirectShape pointShape = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
        //                pointShape.SetShape(new GeometryObject[] { pointGeometry });
        //                pointShape.Name = $"Point_{pointData.ClusterID}_";

        //                // === Настраиваем графическое отображение только линиями ===
        //                OverrideGraphicSettings ogs = new OverrideGraphicSettings()
        //                    .SetProjectionLineColor(displayColor) // Цвет линий кубика
        //                    .SetProjectionLineWeight(2); // Толщина линии (можно настроить)

        //                activeView.SetElementOverrides(pointShape.Id, ogs);
        //                // =======================================================
        //                // Опционально: можно использовать pointData.force для изменения цвета или размера визуализации
        //                // Например, точки с большей силой сделать больше или другого цвета.
        //            }
        //        }

        //        tx.Commit(); // Завершаем транзакцию (сохраняем изменения)
        //    }

        //    System.Windows.Forms.MessageBox.Show($"Успешно загружено и отображено {pointsToVisualize.Count} точек.", "Загрузка завершена", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

        //    return Result.Succeeded; // Команда выполнена успешно
        //}

        // Вспомогательный метод для создания куба (оставляем как было)
        private Solid CreateCubeAtPoint(Document doc, XYZ center, double size)
        {
            // Создаем профиль квадрата
            List<Curve> profile = new List<Curve>();
            XYZ pt1 = new XYZ(center.X - size / 2, center.Y - size / 2, center.Z - size / 2);
            XYZ pt2 = new XYZ(center.X + size / 2, center.Y - size / 2, center.Z - size / 2);
            XYZ pt3 = new XYZ(center.X + size / 2, center.Y + size / 2, center.Z - size / 2);
            XYZ pt4 = new XYZ(center.X - size / 2, center.Y + size / 2, center.Z - size / 2);

            profile.Add(Line.CreateBound(pt1, pt2));
            profile.Add(Line.CreateBound(pt2, pt3));
            profile.Add(Line.CreateBound(pt3, pt4));
            profile.Add(Line.CreateBound(pt4, pt1));

            CurveLoop curveLoop = CurveLoop.Create(profile);
            List<CurveLoop> curveLoops = new List<CurveLoop>() { curveLoop };

            // Экструдируем профиль
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, XYZ.BasisZ, size);

            return solid;
        }

        // Вспомогательный метод для поиска или создания материала (оставляем как было)
        //private Material GetOrCreateMaterial(Document doc, string materialName, Color color)
        //{
        //    // Ищем материал по имени
        //    Material material = new FilteredElementCollector(doc)
        //        .OfClass(typeof(Material))
        //        .Cast<Material>()
        //        .FirstOrDefault(m => m.Name.Equals(materialName));

        //    if (material != null)
        //    {
        //        return material;
        //    }

        //    // Если не найден, создаем новый
        //    using (Transaction t = new Transaction(doc, "Create Material"))
        //    {
        //        t.Start();
        //        try
        //        {
        //            material = doc.Create.NewMaterial(materialName);
        //            material.Color = color;
        //            material.Transparency = 0;
        //            material.Shininess = 0;
        //            t.Commit();
        //            return material;
        //        }
        //        catch (Exception ex)
        //        {
        //            t.RollBack();
        //            System.Diagnostics.Debug.WriteLine($"Ошибка при создании материала: {ex.Message}");
        //            return null;
        //        }
        //    }
        //}
    }




    //[Transaction(TransactionMode.Manual)]
    //class AboutShow : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref String message, ElementSet elements)
    //    {
    //        // Получаем данные о документе из предыдущего окна
    //        UIApplication uiApplication = commandData.Application;
    //        UIDocument uiDocument = uiApplication.ActiveUIDocument;
    //        Document document = uiDocument.Document;

    //        TaskDialog mainDialog = new TaskDialog("Hello, Revit!");
    //        mainDialog.Show();
    //        //Autodesk.Revit.DB.Structure.PathReinforcement класс для зон
    //        using (Transaction tx = new Transaction(document))
    //        {
    //            // Мы должны наши действия оборачивать в транзакции, иначе Revit не позволит вносить изменения
    //            tx.Start();

    //            //Autodesk.Revit.DB.Structure.PathReinforcement newPR = Autodesk.Revit.DB.Structure.PathReinforcement.Create(document, /*Плита с зоной*/,
    //            // /* Curves  - отрезки зоны (кривые) */, true, document.GetDefaultElementTypeId(ElementTypeGroup.PathReinforcementType),
    //            // /*Тип арматуры (задать диаметр)*/, ElementId.InvalidElementId, ElementId.InvalidElementId);

    //            //Задание параметров
    //            // 1 - Грань зоны армирования (нижняя грань или верхняя)
    //            // 2 - Длина зоны армирования
    //            // 3 - Шаг армирования 

    //            //newPR.get_Parameter(BuiltInParameter.PATH_REIN_FACE_SLAB).Set(1 - нижняя, 0 - верхняя)
    //            //newPR.get_Parameter(BuiltInParameter.PATH_REIN_LENGTH_1).Set(double - в футах)
    //            //newPR.get_Parameter(BuiltInParameter.PATH_REIN_SPACING).Set(double - в футах)

    //            // Нам необходимо найти длины зон и шаги


    //            tx.Commit();
    //        }
    //        return Result.Succeeded;
    //    }
    //}









    //[Transaction(TransactionMode.Manual)]
    //class AboutShow: IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref String message, ElementSet elements)
    //    {
    //        // Получаем данные о документе из предыдущего окна
    //        UIApplication uiApplication = commandData.Application;
    //        UIDocument uiDocument = uiApplication.ActiveUIDocument;
    //        Document document = uiDocument.Document;

    //        TaskDialog mainDialog = new TaskDialog("Hello, Revit!");
    //        mainDialog.Show();
    //        //Autodesk.Revit.DB.Structure.PathReinforcement класс для зон
    //        using(Transaction tx = new Transaction(document))
    //        {
    //            // Мы должны наши действия оборачивать в транзакции, иначе Revit не позволит вносить изменения
    //            tx.Start();

    //            //Autodesk.Revit.DB.Structure.PathReinforcement newPR = Autodesk.Revit.DB.Structure.PathReinforcement.Create(document, /*Плита с зоной*/,
    //            // /* Curves  - отрезки зоны (кривые) */, true, document.GetDefaultElementTypeId(ElementTypeGroup.PathReinforcementType),
    //            // /*Тип арматуры (задать диаметр)*/, ElementId.InvalidElementId, ElementId.InvalidElementId);

    //            //Задание параметров
    //            // 1 - Грань зоны армирования (нижняя грань или верхняя)
    //            // 2 - Длина зоны армирования
    //            // 3 - Шаг армирования 

    //            //newPR.get_Parameter(BuiltInParameter.PATH_REIN_FACE_SLAB).Set(1 - нижняя, 0 - верхняя)
    //            //newPR.get_Parameter(BuiltInParameter.PATH_REIN_LENGTH_1).Set(double - в футах)
    //            //newPR.get_Parameter(BuiltInParameter.PATH_REIN_SPACING).Set(double - в футах)

    //            // Нам необходимо найти длины зон и шаги


    //            tx.Commit();
    //        }
    //        return Result.Succeeded;
    //    }
    //}

}
