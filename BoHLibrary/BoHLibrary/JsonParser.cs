// Солод Алексей Александрович БПИ-248_2 Вариант-6 дополнительная задача библиотека классов
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BoHLibrary
{
    /// <summary>
    /// Статический класс, отвечающий за чтение/запись JSON
    /// через стандартные потоки (Console.In, Console.Out).
    ///
    /// Упрощённая реализация: предполагаем, что JSON имеет структуру:
    /// {
    ///   "elements": [ {...}, {...}, ...]
    /// }
    /// где внутри массива - объекты, описывающие items.
    /// </summary>
    public static class JsonParser
    {
        /// <summary>
        /// Считывает JSON из Console.In, парсит, возвращает список AspectedItem.
        /// Если некорректно, кидает исключение с описанием проблемы.
        /// </summary>
        public static List<AspectedItem> ReadJson()
        {
            // Считаем весь текст до конца. Можно построчно, но проще целиком.
            string json = Console.In.ReadToEnd();

            // Проверим, что это валидный JSON. Упрощённо, без полного RFC.
            // Предположим, что ищем массив объектов по ключу "elements".

            // Убедимся, что json содержит "elements"
            if (!json.Contains("\"elements\""))
                throw new Exception("Нет ключа \"elements\". Некорректный JSON!");

            // Вырежем всё, что внутри массив elements
            // Примерно: "elements": [ .... ]
            var matchElements = Regex.Match(json, @"""elements""\s*:\s*\[(.*)\]\s*,?", RegexOptions.Singleline);
            if (!matchElements.Success)
            {
                throw new Exception("Не удалось найти массив [ ... ] после \"elements\". Некорректный JSON!");
            }
            string arrayContent = matchElements.Groups[1].Value;
            // Здесь будет содержание между скобками [ ... ]

            // Теперь надо найти объекты в фигурных скобках { ... } внутри arrayContent.
            // Учтём, что внутри фигурных скобок тоже могут быть вложенные фигурные скобки (для aspects, xtriggers и т.д.).
            // Однако поскольку у нас JSON уже готовый, допустим что вложенные объекты тоже соответствуют правилам (до 1 уровня).
            // Для упрощения найдём все { ... }, которые парсим "как есть".
            MatchCollection objectMatches = Regex.Matches(arrayContent, @"\{(?>[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}");


            List<AspectedItem> items = new List<AspectedItem>();

            foreach (Match m in objectMatches)
            {
                string objJson = m.Value; // сам текст вида { ... }
                // Парсим его в AspectedItem
                AspectedItem item = ParseItem(objJson);
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Упрощённая запись JSON в Console.Out:
        /// Выводит конструкцию вида
        /// {
        ///   "elements": [
        ///     {...},
        ///     ...
        ///   ]
        /// }
        /// 
        /// </summary>
        public static void WriteJson(IEnumerable<AspectedItem> items)
        {
            Console.WriteLine("{");
            Console.WriteLine("  \"elements\": [");

            bool first = true;
            foreach (var it in items)
            {
                if (!first) Console.WriteLine(",");
                first = false;
                Console.WriteLine("    {");
                // Выведем поля:
                Console.WriteLine($"      \"ID\": \"{EscapeString(it.ID)}\",");
                Console.WriteLine($"      \"uniquenessgroup\": \"{EscapeString(it.UniquenessGroup)}\",");
                Console.WriteLine($"      \"Label\": \"{EscapeString(it.Label)}\",");
                Console.WriteLine($"      \"Desc\": \"{EscapeString(it.Desc)}\",");
                Console.WriteLine($"      \"inherits\": \"{EscapeString(it.Inherits)}\",");
                Console.WriteLine($"      \"audio\": \"{EscapeString(it.Audio)}\",");

                // Выведем aspects как JSON-объект:
                Console.WriteLine($"      \"aspects\": {{");
                bool firstA = true;
                foreach (var asp in it.Aspects)
                {
                    if (!firstA) Console.WriteLine(",");
                    firstA = false;
                    Console.Write($"        \"{EscapeString(asp.Key)}\": {asp.Value}");
                }
                Console.WriteLine();
                Console.WriteLine("      },");

                // Сырьё xtriggers, xexts можно вывести без глубокой обработки
                // но чтобы избежать кавычек в кавычках, лучше экранировать
                Console.WriteLine($"      \"xtriggers\": \"{EscapeString(it.XTriggersRaw)}\",");
                Console.WriteLine($"      \"xexts\": \"{EscapeString(it.XExtsRaw)}\"");

                Console.Write("    }");
            }

            Console.WriteLine();
            Console.WriteLine("  ]");
            Console.WriteLine("}");
        }

        // ======= Вспомогательные методы парсинга =======

        /// <summary>
        /// Парсит фрагмент вида { ... } в AspectedItem.
        /// </summary>
        private static AspectedItem ParseItem(string objJson)
        {
            AspectedItem item = new AspectedItem();

            // Упрощённо вычленим поля по регулярке "key":"value"
            // (или "key": value, если число)
            // Или "key": { ... } для aspects
            // Но aspects может быть вложенным объектом.

            // 1) Найдём "ID": "...", uniquenessgroup, Label, Desc, inherits, audio 
            // 2) Найдём "aspects": { ... }
            // 3) "xtriggers", "xexts" как строки 
            // Любое поле, которого нет, не устанавливаем => default.

            // Простейшее решение:
            string patternStringField = @"""(\w+(\.\w+)*)""\s*:\s*""([^""]*)""";
            // например "ID": "dog.hungry"
            // Но aspects могут быть внутри фигурных скобок. Тогда захватим другое:
            string patternObjectField = @"""(\w+(\.\w+)*)""\s*:\s*\{(.*?)\}";
            // "aspects": { ... }   -- будем ловить отдельно
            // А числа (double)? "key": 123
            // Примитивно: patternNumberField: "key": 123(\.\d+)?
            string patternNumberField = @"""(\w+(\.\w+)*)""\s*:\s*([\d\.\-]+)";

            // Сначала найдём объект aspects, если есть:
            var aspectsMatch = Regex.Match(objJson, @"""aspects""\s*:\s*\{(.*?)\}", RegexOptions.Singleline);
            if (aspectsMatch.Success)
            {
                string aspectsContent = aspectsMatch.Groups[1].Value; // содержимое фигурных скобок
                // Теперь внутри aspectsContent ищем пары "key": number
                var aspMatches = Regex.Matches(aspectsContent, patternNumberField);
                var dict = new Dictionary<string, double>();
                foreach (Match am in aspMatches)
                {
                    string aKey = am.Groups[1].Value; // например edge
                    string aVal = am.Groups[3].Value; // например 1
                    if (double.TryParse(aVal, out double dv))
                    {
                        dict[aKey] = dv;
                    }
                }
                item.SetAspects(dict);
            }

            // Строковые поля:
            var strMatches = Regex.Matches(objJson, patternStringField, RegexOptions.Singleline);
            foreach (Match m in strMatches)
            {
                string key = m.Groups[1].Value;
                string val = m.Groups[3].Value;
                // Устанавливаем:
                try
                {
                    item.SetField(key, val);
                }
                catch (KeyNotFoundException)
                {
                    // Игнор, если поля нет
                }
            }

            // Числовые поля (кроме aspects, мы уже разобрали)
            // Вдруг кто-то положил "something": 42 
            var numMatches = Regex.Matches(objJson, patternNumberField, RegexOptions.Singleline);
            foreach (Match nm in numMatches)
            {
                string key = nm.Groups[1].Value;
                string val = nm.Groups[3].Value;
                // Проверяем, не aspects ли это?
                if (!key.StartsWith("aspects."))
                {
                    // Пробуем применить SetField
                    // Может, его нет, тогда KeyNotFound => игнор
                    try
                    {
                        item.SetField(key, val);
                    }
                    catch { /* ignore */ }
                }
            }

            return item;
        }

        /// <summary>
        /// Экранирует спецсимволы \ и " для корректного вывода в JSON.
        /// </summary>
        private static string EscapeString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
