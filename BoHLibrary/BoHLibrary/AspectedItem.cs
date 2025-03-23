// Солод Алексей Александрович БПИ-248_2 Вариант-6 дополнительная задача библиотека классов
using System;
using System.Collections.Generic;
using System.Text;

namespace BoHLibrary
{
    /// <summary>
    /// Класс, представляющий вещь (AspectedItem) из BoH.
    /// Реализует IJSONObject, чтобы поддерживать доступ
    /// к JSON-полям "через интерфейс".
    ///
    /// В JSON примере мы видим поля типа:
    ///   {
    ///     "ID": "dog.hungry",
    ///     "uniquenessgroup": "uqdog",
    ///     "Label": "Голодный пёс",
    ///     "Desc": "...",
    ///     "inherits": "_beast.carnivore",
    ///     "audio": "Dog",
    ///     "aspects": { "edge":1, "boost.edge":1, "heart":1, ... },
    ///     "xtriggers": { ... },
    ///     "xexts": { ... }
    ///   }
    /// Мы не обязаны все ключи хранить в отдельных полях, 
    /// но для удобства сделаем так.
    /// </summary>
    public class AspectedItem : IJSONObject
    {
        // Основные поля, которые чаще всего понадобятся:
        private string _id;
        private string _uniquenessgroup;
        private string _label;
        private string _desc;
        private string _inherits;
        private string _audio;

        /// <summary>
        /// Аспекты храним как словарь: "edge" -> 1, "boost.edge" -> 1 и т.д.
        /// </summary>
        private Dictionary<string, double> _aspects;

        // Для примера: xtriggers, xexts 
        // В данном примере будем хранить как "сырые" JSON-фрагменты (строки) 
        private string _xtriggersRaw;
        private string _xextsRaw;

        /// <summary>
        /// Конструктор без параметров.
        /// </summary>
        public AspectedItem()
        {
            _aspects = new Dictionary<string, double>();
            // Начальное значение полей - пустые строки, чтобы избежать null-возвратов.
            _id = "";
            _uniquenessgroup = "";
            _label = "";
            _desc = "";
            _inherits = "";
            _audio = "";
            _xtriggersRaw = "";
            _xextsRaw = "";
        }

        // Свойства только на чтение, но при этом SetField() сможет их менять.
        public string ID => _id;
        public string UniquenessGroup => _uniquenessgroup;
        public string Label => _label;
        public string Desc => _desc;
        public string Inherits => _inherits;
        public string Audio => _audio;

        /// <summary>
        /// Возвращает словарь аспектов ("edge"->1, "boost.edge"->1, ...).
        /// </summary>
        public Dictionary<string, double> Aspects => _aspects;

        public string XTriggersRaw => _xtriggersRaw;
        public string XExtsRaw => _xextsRaw;

        // ==============================
        // Реализация IJSONObject
        // ==============================

        public IEnumerable<string> GetAllFields()
        {
            // Перечислим все названия полей, включая те, что мы не храним явно,
            List<string> fields = new List<string>()
            {
                "ID",
                "uniquenessgroup",
                "Label",
                "Desc",
                "inherits",
                "audio",
                "xtriggers",
                "xexts"
            };
            // Если хотим, можем ещё добавлять ключи из _aspects.
            // Но официально aspects - отдельный объект JSON.
            return fields;
        }

        public string GetField(string fieldName)
        {
            // Возвращаем значение в виде строки (или null).
            switch (fieldName)
            {
                case "ID": return _id;
                case "uniquenessgroup": return _uniquenessgroup;
                case "Label": return _label;
                case "Desc": return _desc;
                case "inherits": return _inherits;
                case "audio": return _audio;
                case "xtriggers": return _xtriggersRaw;
                case "xexts": return _xextsRaw;
                default:
                    // Если пользователь хочет прочитать aspects,
                    // можем вернуть JSON (строку) или null.
                    if (fieldName.StartsWith("aspects."))
                    {
                        // Допустим, "aspects.edge" -> ищем "edge" в словаре
                        string aspectKey = fieldName.Substring("aspects.".Length);
                        if (_aspects.ContainsKey(aspectKey))
                            return _aspects[aspectKey].ToString();
                        return null;
                    }
                    // Если просто "aspects", можем вернуть сериализованный словарь
                    if (fieldName == "aspects")
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("{");
                        bool first = true;
                        foreach (var kvp in _aspects)
                        {
                            if (!first) sb.Append(", ");
                            first = false;
                            sb.Append($"{kvp.Key}:{kvp.Value}");
                        }
                        sb.Append("}");
                        return sb.ToString();
                    }
                    return null;
            }
        }

        public void SetField(string fieldName, string value)
        {
            // Парсим строку value и записываем в нужное поле.
            // Если поле не известно, кидаем KeyNotFoundException.
            // В случае aspects.* - пытаемся преобразовать value в double.
            switch (fieldName)
            {
                case "ID":
                    _id = value ?? "";
                    return;
                case "uniquenessgroup":
                    _uniquenessgroup = value ?? "";
                    return;
                case "Label":
                    _label = value ?? "";
                    return;
                case "Desc":
                    _desc = value ?? "";
                    return;
                case "inherits":
                    _inherits = value ?? "";
                    return;
                case "audio":
                    _audio = value ?? "";
                    return;
                case "xtriggers":
                    _xtriggersRaw = value ?? "";
                    return;
                case "xexts":
                    _xextsRaw = value ?? "";
                    return;
                default:
                    if (fieldName.StartsWith("aspects."))
                    {
                        string aspectKey = fieldName.Substring("aspects.".Length);
                        if (!string.IsNullOrEmpty(aspectKey))
                        {
                            if (double.TryParse(value, out double dVal))
                            {
                                _aspects[aspectKey] = dVal;
                                return;
                            }
                            else
                            {
                                // Например, если формат не соответствует double
                                throw new FormatException($"Cannot parse aspect value '{value}' as double.");
                            }
                        }
                    }
                    if (fieldName == "aspects")
                    {
                        // Допустим, пользователь целиком задаёт JSON aspects
                        throw new NotSupportedException("Use 'aspects.*' for single aspect or specialized method for entire dictionary.");
                    }
                    throw new KeyNotFoundException($"Field '{fieldName}' not found in AspectedItem.");
            }
        }

        // Для удобства добавим метод, чтобы задавать словарь аспектов скопом (используется JsonParser):
        public void SetAspects(Dictionary<string, double> aspects)
        {
            _aspects = aspects;
        }
    }
}