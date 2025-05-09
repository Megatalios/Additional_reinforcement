﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Diplom_Project
{
    public static class Tools
    {

        public const string DataFileName = "FitData.json";
        public const string CreateJSONFile = "Создан файл данных FitData.json";
        public const string SucFindJSONFile = "Файл данных FitData.json обнаружен";
        public const string ErrCreateJSONFile = "Не удалось создать файл данных FitData.json";
        public const string EmptyDiamStep = "Не удалось выполнить расчет, отсутствуют данные Диаметр – Шаг";
        public const string EmptyDiamCost = "Не удалось выполнить расчет, отсутствуют данные Диаметр – Цена";
        public const string EmptyLength = "Не удалось выполнить расчет, отсутствуют данные Длина";
        public const string ErrJSON = "Не удалось получить доступ к данным в FitData.json";
        public const string SucUpdateJSON = "Данные обновлены";
        public const string PluginName = "CalcFittingsPlugin"; // Или другое имя вашего плагина
        public const string ChangesSaved = "Изменения успешно сохранены!";
        public const string ChangesNotSaved = "Не удалось сохранить изменения!\n";
        public const string ForDiamStep = "Для таблицы Диаметр – Шаг:\n";
        public const string ForDiamCost = "Для таблицы Диаметр – Цена:\n";
        public const string ForLength = "Для таблицы Длина:\n";
        public const string InvalideData = "Невалидные данные:\n";
        public const string SucLoadFit = "Загрузка армирования: Успешно.";
        public const string ErrLoadFit = "Загрузка армирования: Ошибка.";
        public const string ParseStart = "Начата загрузка армирования из .csv файла";
        public const string ParceEnd = "Армирование из .csv файла загружено успешно";
        public const string ParceErr = "Не удалось прочитать данные об армировании из .csv файла";
        public const string CalcStart = "Расчет запущен";
        public const string CalcErr = "Не удалось выполнить расчет для уровня";
        public const string CalcSuc = "Выполнен расчет для уровня";
        public const int HeadersCount = 10; // Количество столбцов в CSV
        public static readonly string[] HeadersTemplate = { "Тип", "Номер", "Координата X узлов", "Координата Y узлов", "Координата Z центр", "Координата Z минимум", "As1X", "As2X", "As3Y", "As4Y" }; // Заголовки CSV

        // Преобразует текст в Лог-сообщение [время]: Текст
        public static string CreateLogMessage(string text)
        {
            DateTime now = DateTime.Now;
            return "[" + now.ToString("HH:mm:ss") + "]: " + text + "\n-----------------------------------------------------------\n";
        }
        //Функция для проверки регуляркой, является ли введенный символ числом (используется в UserControl2)
        public static bool IsInt(string text)
        {
            // Адаптируйте регулярное выражение, если нужно проверять не только целые, но и дробные числа
            Regex regex = new Regex("[^0-9.-]+"); // Только цифры, точка, минус
            return !regex.IsMatch(text);
        }
    }
}
