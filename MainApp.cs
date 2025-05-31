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
using System.Windows.Media.Imaging; // Для BitmapImage


namespace Diplom_Project
{

    class MainApp : IExternalApplication
    {
        const String Tab_name = "Дополнительное армирование";

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
            RibbonPanel our_main_panel = application.CreateRibbonPanel(Tab_name, "Расчет зон дополнительного армирования");

            // Добавление кнопки
            PushButtonData data_of_button = new PushButtonData(
                "AboutShowCommand", // Внутреннее имя кнопки (уникальное)
                "Открыть меню", // Текст на кнопке
                System.Reflection.Assembly.GetExecutingAssembly().Location, // Путь к текущей сборке
                "Diplom_Project.AboutShow" // Полное имя класса команды (включая Namespace)
            );

            // Добавление кнопки на панель
            PushButton test_knopka = our_main_panel.AddItem(data_of_button) as PushButton;

            try
            {
                // Иконка для большой кнопки (32x32)
                //Uri uriImageLarge = new Uri("pack://application:,,,/Diplom_Project;component/Resources/icon_reinforcement_32.png");
                // Если папки Resources нет, и иконка лежит в корне проекта:
                Uri uriImageLarge = new Uri("pack://application:,,,/Diplom_Project;component/icon_reinforcement_32.png");
                BitmapImage largeImage = new BitmapImage(uriImageLarge);
                test_knopka.LargeImage = largeImage;

                // (Опционально) Иконка для маленькой кнопки (16x16), если кнопка будет отображаться в маленьком размере
                // Uri uriImageSmall = new Uri("pack://application:,,,/Diplom_Project;component/Resources/icon_reinforcement_16.png");
                // BitmapImage smallImage = new BitmapImage(uriImageSmall);
                // test_knopka.Image = smallImage;

                // (Опционально) Подсказка для кнопки
                test_knopka.ToolTip = "Запуск плагина для расчета и визуализации дополнительного армирования плит.";

                // (Опционально) Изображение для подсказки (если нужно)
                // Uri uriToolTipImage = new Uri("pack://application:,,,/Diplom_Project;component/Resources/tooltip_image.png");
                // BitmapImage toolTipImage = new BitmapImage(uriToolTipImage);
                // test_knopka.ToolTipImage = toolTipImage;

            }
            catch (Exception ex)
            {
                // Ошибку загрузки иконки можно проигнорировать или вывести в отладочную консоль,
                // чтобы плагин все равно запустился, но без иконки.
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки иконки для кнопки: {ex.Message}");
                // TaskDialog.Show("Ошибка иконки", $"Не удалось загрузить иконку: {ex.Message}"); // Можно показать пользователю
            }
            // --- Конец добавления иконки ---


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

        
    }

}
