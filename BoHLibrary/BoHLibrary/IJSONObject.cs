// Солод Алексей Александрович БПИ-248_2 Вариант-6 дополнительная задача библиотека классов
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace BoHLibrary
{
    /// <summary>
    /// Интерфейс IJSONObject:
    ///   - Возвращает имена полей объекта (все ключи JSON).
    ///   - Получает/устанавливает значения полей по имени (как строки).
    /// </summary>
    public interface IJSONObject
    {
        /// <summary>
        /// Возвращает названия всех полей (ключей) JSON-объекта.
        /// </summary>
        IEnumerable<string> GetAllFields();

        /// <summary>
        /// Возвращает значение поля с именем fieldName в виде строки.
        /// Если такого поля нет, вернуть null.
        /// </summary>
        string GetField(string fieldName);

        /// <summary>
        /// Устанавливает значение поля с именем fieldName.
        /// Если поля нет, кинуть исключение KeyNotFoundException.
        /// </summary>
        void SetField(string fieldName, string value);
    }
}