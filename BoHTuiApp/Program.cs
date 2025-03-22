// Солод Алексей Александрович БПИ-248_2 Вариант-6 дополнительная задача консольное приложение
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Terminal.Gui;
using BoHLibrary; // Подключите вашу библиотеку, в которой лежат AspectedItem и JsonParser

namespace BoHTuiApp
{
    /// <summary>
    /// Основной класс TUI-приложения, использует библиотеку Terminal.Gui 
    /// для отображения меню, окон диалога и статус-бара.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Список загруженных AspectedItem-объектов, хранимых в памяти.
        /// </summary>
        private static List<AspectedItem> _items = new List<AspectedItem>();

        /// <summary>
        /// Элемент статус-бара, в котором мы динамически меняем Title (текст).
        /// </summary>
        private static StatusItem _statusItem;

        /// <summary>
        /// Главное окно приложения (позволяет нам размещать прочие элементы UI).
        /// </summary>
        private static Window _mainWindow;

        /// <summary>
        /// Точка входа TUI-приложения. Инициализирует движок Terminal.Gui, 
        /// создаёт меню, статус-бар и запускает Application.Run().
        /// </summary>
        static void Main()
        {
            // Инициализируем движок TUI
            Application.Init();
            var top = Application.Top;

            // Создаём главное окно, занимающее всё пространство (кроме меню и статус-бара)
            _mainWindow = new Window("BoH TUI Application")
            {
                X = 0,
                Y = 1,            // Отступ сверху под MenuBar
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1 // Оставляем место снизу под StatusBar
            };
            top.Add(_mainWindow);

            // Создаём верхнее меню
            var menu = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("_Open", "", OnOpenFile),
                    new MenuItem("_Save", "", OnSaveFile),
                    new MenuItem("_Exit", "", OnExit)
                }),
                new MenuBarItem("_Actions", new MenuItem[]
                {
                    new MenuItem("_Filter", "", OnFilter),
                    new MenuItem("_Sort",   "", OnSort),
                    new MenuItem("_Combine", "", OnCombine)
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new MenuItem("_About", "", OnAbout)
                })
            });
            top.Add(menu);

            // Создаём статус-бар. 
            // Вместо Key.None используем (Key)0 или Key.Null, если None недоступен.
            _statusItem = new StatusItem((Key)0, "Welcome!", null);
            var statusBar = new StatusBar(new StatusItem[] { _statusItem });
            top.Add(statusBar);

            // Запускаем основной цикл TUI
            Application.Run();
        }

        /// <summary>
        /// Открывает диалог выбора файла (ShowFilePickerDialog), 
        /// читает JSON и загружает объекты в _items.
        /// </summary>
        private static void OnOpenFile()
        {
            ShowFilePickerDialog(true, (path) =>
            {
                try
                {
                    ShowProgress("Loading data...", () =>
                    {
                        // Открываем файл, перенаправляем Console.In,
                        // чтобы JsonParser читала из "консоли"
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                        using var sr = new StreamReader(fs);

                        var oldIn = Console.In;
                        Console.SetIn(sr);
                        _items = JsonParser.ReadJson();
                        Console.SetIn(oldIn);
                    });

                    _statusItem.Title = $"Loaded {_items.Count} items from {Path.GetFileName(path)}";
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", "Failed to load file:\n" + ex.Message, "OK");
                }
            });
        }

        /// <summary>
        /// Открывает диалог выбора файла (ShowFilePickerDialog),
        /// записывает текущие _items в JSON (вызовом JsonParser.WriteJson).
        /// </summary>
        private static void OnSaveFile()
        {
            if (_items.Count == 0)
            {
                MessageBox.Query("Info", "No items to save. Load data first.", "OK");
                return;
            }

            ShowFilePickerDialog(false, (path) =>
            {
                try
                {
                    ShowProgress("Saving data...", () =>
                    {
                        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                        using var sw = new StreamWriter(fs);

                        var oldOut = Console.Out;
                        Console.SetOut(sw);
                        JsonParser.WriteJson(_items);
                        Console.SetOut(oldOut);
                    });

                    _statusItem.Title = $"Saved {_items.Count} items to {Path.GetFileName(path)}";
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", "Failed to save file:\n" + ex.Message, "OK");
                }
            });
        }

        /// <summary>
        /// Запрашивает подтверждение выхода (MessageBox), 
        /// при согласии завершает Application.Run().
        /// </summary>
        private static void OnExit()
        {
            var n = MessageBox.Query("Exit", "Are you sure you want to quit?", "Yes", "No");
            if (n == 0) // "Yes"
            {
                Application.RequestStop();
            }
        }

        /// <summary>
        /// Показывает диалог для фильтрации данных (по строковым полям или аспектам).
        /// Результат фильтрации сохраняется в _items.
        /// </summary>
        private static void OnFilter()
        {
            if (_items.Count == 0)
            {
                MessageBox.Query("Info", "No data to filter. Load data first.", "OK");
                return;
            }

            var dialog = new Dialog("Filter Data", 60, 15);

            var labelField = new Label("Field/aspect:") { X = 2, Y = 2 };
            var textField = new TextField("") { X = 18, Y = 2, Width = 20 };

            var labelValue = new Label("Substring / min value:") { X = 2, Y = 4 };
            var textValue = new TextField("") { X = 18, Y = 4, Width = 20 };

            var btnOk = new Button("OK")
            {
                X = Pos.Center(),
                Y = 9,
                IsDefault = true
            };
            btnOk.Clicked += () =>
            {
                string field = textField.Text.ToString();
                string rawVal = textValue.Text.ToString();

                var knownStringFields = new[] { "ID", "Label", "Desc", "audio", "inherits", "uniquenessgroup" };
                bool isAspect = !knownStringFields.Contains(field);

                if (!isAspect)
                {
                    // Фильтр по строковому полю:
                    if (string.IsNullOrEmpty(rawVal))
                    {
                        MessageBox.Query("Info", "Empty substring => no filter applied.", "OK");
                        return;
                    }

                    _items = _items.Where(i =>
                    {
                        string val = i.GetField(field) ?? "";
                        return val.IndexOf(rawVal, StringComparison.OrdinalIgnoreCase) >= 0;
                    }).ToList();

                    _statusItem.Title = $"Filtered by substring in '{field}': {_items.Count} items remain.";
                }
                else
                {
                    // Фильтр по аспекту:
                    if (!double.TryParse(rawVal, out double dVal))
                    {
                        MessageBox.ErrorQuery("Error", "Value must be numeric for aspect filtering.", "OK");
                        return;
                    }

                    _items = _items.Where(i =>
                    {
                        if (i.Aspects.TryGetValue(field, out double aspectVal))
                            return aspectVal >= dVal;
                        return false;
                    }).ToList();

                    _statusItem.Title = $"Filtered by aspect '{field} >= {dVal}': {_items.Count} items remain.";
                }

                Application.RequestStop();
            };

            dialog.Add(labelField, textField, labelValue, textValue, btnOk);
            Application.Run(dialog);
        }

        /// <summary>
        /// Показывает диалог для сортировки (по строковому полю или аспекту).
        /// Результат сортировки возвращается в _items.
        /// </summary>
        private static void OnSort()
        {
            if (_items.Count == 0)
            {
                MessageBox.Query("Info", "No data to sort. Load data first.", "OK");
                return;
            }

            var dialog = new Dialog("Sort Data", 60, 15);

            var labelField = new Label("Field/aspect:") { X = 2, Y = 2 };
            var textField = new TextField("") { X = 18, Y = 2, Width = 20 };

            var labelDirection = new Label("Direction (1 asc / 2 desc):") { X = 2, Y = 4 };
            var textDir = new TextField("1") { X = 28, Y = 4, Width = 5 };

            var btnOk = new Button("OK")
            {
                X = Pos.Center(),
                Y = 9,
                IsDefault = true
            };
            btnOk.Clicked += () =>
            {
                string field = textField.Text.ToString();
                string direction = textDir.Text.ToString();
                bool asc = (direction == "1");

                var knownStringFields = new[] { "ID", "Label", "Desc", "audio", "inherits", "uniquenessgroup" };
                bool isAspect = !knownStringFields.Contains(field);

                if (isAspect)
                {
                    // Сортировка по аспекту:
                    if (asc)
                    {
                        _items = _items.OrderBy(i => i.Aspects.ContainsKey(field) ? i.Aspects[field] : double.MinValue).ToList();
                    }
                    else
                    {
                        _items = _items.OrderByDescending(i => i.Aspects.ContainsKey(field) ? i.Aspects[field] : double.MinValue).ToList();
                    }
                }
                else
                {
                    // Сортировка по строковому полю:
                    if (asc)
                    {
                        _items = _items.OrderBy(i => i.GetField(field) ?? "").ToList();
                    }
                    else
                    {
                        _items = _items.OrderByDescending(i => i.GetField(field) ?? "").ToList();
                    }
                }

                _statusItem.Title = $"Sorted by {field}, asc={asc}. Count: {_items.Count}";
                Application.RequestStop();
            };

            dialog.Add(labelField, textField, labelDirection, textDir, btnOk);
            Application.Run(dialog);
        }

        /// <summary>
        /// Показывает диалог, где пользователь вводит аспект (например, "edge=2"),
        /// а затем выполняется поиск комбинаций предметов, удовлетворя этому требованию.
        /// Результат показывается в MessageBox.
        /// </summary>
        private static void OnCombine()
        {
            if (_items.Count == 0)
            {
                MessageBox.Query("Info", "No data to combine. Load data first.", "OK");
                return;
            }

            var dialog = new Dialog("Combine", 60, 18);

            var labelAspect = new Label("Required aspect=val:") { X = 2, Y = 2 };
            var textAspect = new TextField("edge=2") { X = 22, Y = 2, Width = 15 };

            var btnOk = new Button("OK")
            {
                X = Pos.Center(),
                Y = 5,
                IsDefault = true
            };
            btnOk.Clicked += () =>
            {
                var input = textAspect.Text.ToString(); // Пример: "edge=2"
                var parts = input.Split('=');
                if (parts.Length != 2 || !double.TryParse(parts[1], out double val))
                {
                    MessageBox.ErrorQuery("Error", "Invalid format. Use aspect=value (e.g. 'edge=2').", "OK");
                    return;
                }
                var requiredAspects = new Dictionary<string, double>();
                requiredAspects[parts[0]] = val;

                List<List<AspectedItem>> foundCombos = null;

                ShowProgress("Combining...", () =>
                {
                    foundCombos = FindCombinations(requiredAspects);
                });

                if (foundCombos == null || foundCombos.Count == 0)
                {
                    MessageBox.Query("Result", "No suitable combos found.", "OK");
                }
                else
                {
                    // Показать первые 10
                    var resultText = $"Found {foundCombos.Count} combos (up to 10 shown):\n\n";
                    int showCount = Math.Min(foundCombos.Count, 10);
                    for (int i = 0; i < showCount; i++)
                    {
                        var combo = foundCombos[i];
                        resultText += $"#{i + 1} (size={combo.Count}): " +
                                      string.Join(", ", combo.Select(x => x.ID)) + "\n";
                    }
                    MessageBox.Query("Result", resultText, "OK");
                }

                Application.RequestStop();
            };

            dialog.Add(labelAspect, textAspect, btnOk);
            Application.Run(dialog);
        }

        /// <summary>
        /// Показывает окно «О программе» (автор, вариант, год и т.д.).
        /// </summary>
        private static void OnAbout()
        {
            MessageBox.Query("About", "BoH TUI Application\nAuthor: YourName\nVariant: #X\n© 2025", "OK");
        }

        /// <summary>
        /// Диалог выбора файла. При <paramref name="isOpenMode"/> = true 
        /// показывает "Open File", иначе "Save File". Позволяет ходить 
        /// по директориям (Up Dir) и выбирать файл.
        /// </summary>
        /// <param name="isOpenMode">Режим открытия (true) или сохранения (false)</param>
        /// <param name="onFileSelected">Вызывается при выборе файла, 
        /// передаётся полный путь</param>
        private static void ShowFilePickerDialog(bool isOpenMode, Action<string> onFileSelected)
        {
            var currentDir = Directory.GetCurrentDirectory();
            var dialog = new Dialog(isOpenMode ? "Open File" : "Save File", 60, 20);

            var dirLabel = new Label($"Directory: {currentDir}") { X = 1, Y = 1 };
            var fileListView = new ListView()
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill() - 2,
                Height = 10
            };

            // Обновляет список файлов и папок в fileListView
            void RefreshFileList(string dir)
            {
                try
                {
                    currentDir = dir;
                    dirLabel.Text = $"Directory: {currentDir}";

                    var dirs = Directory.GetDirectories(currentDir)
                        .Select(d => "[DIR] " + Path.GetFileName(d));
                    var files = Directory.GetFiles(currentDir)
                        .Select(f => Path.GetFileName(f));

                    var combined = dirs.Concat(files).ToList();
                    fileListView.SetSource(combined);
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", ex.Message, "OK");
                }
            }

            RefreshFileList(currentDir);

            var btnUp = new Button("Up Dir")
            {
                X = 1,
                Y = 13
            };
            btnUp.Clicked += () =>
            {
                var parent = Directory.GetParent(currentDir);
                if (parent != null)
                {
                    RefreshFileList(parent.FullName);
                }
            };

            var btnSelect = new Button("Select")
            {
                X = 10,
                Y = 13
            };
            btnSelect.Clicked += () =>
            {
                var sel = fileListView.SelectedItem;
                if (sel < 0) return;

                var selStr = fileListView.Source.ToList()[sel] as string;
                if (selStr == null) return;

                if (selStr.StartsWith("[DIR] "))
                {
                    var dirName = selStr.Substring(6);
                    var fullPath = Path.Combine(currentDir, dirName);
                    RefreshFileList(fullPath);
                }
                else
                {
                    // Файл
                    var fileName = selStr;
                    var fullPath = Path.Combine(currentDir, fileName);
                    onFileSelected?.Invoke(fullPath);
                    Application.RequestStop();
                }
            };

            var btnCancel = new Button("Cancel")
            {
                X = 20,
                Y = 13
            };
            btnCancel.Clicked += () => Application.RequestStop();

            dialog.Add(dirLabel, fileListView, btnUp, btnSelect, btnCancel);
            Application.Run(dialog);
        }

        /// <summary>
        /// Показывает диалог с ProgressBar и выполняет <paramref name="longAction"/>
        /// в фоновом потоке. По завершении скрывает прогресс-бар.
        /// </summary>
        /// <param name="title">Заголовок окна диалога</param>
        /// <param name="longAction">Действие, имитирующее долгую операцию</param>
        private static void ShowProgress(string title, Action longAction)
        {
            var pbDialog = new Dialog(title, 50, 7);

            var pb = new ProgressBar()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2
            };
            pbDialog.Add(pb);

            // Запускаем задачу в отдельном потоке
            System.Threading.Tasks.Task.Run(() =>
            {
                // Имитация плавного заполнения
                for (int i = 0; i <= 100; i += 10)
                {
                    System.Threading.Thread.Sleep(40);
                    Application.MainLoop.Invoke(() => { pb.Fraction = i / 100f; });
                }

                try
                {
                    longAction();
                }
                finally
                {
                    // Скрываем окно прогресса
                    Application.MainLoop.Invoke(() =>
                    {
                        Application.RequestStop(pbDialog);
                    });
                }
            });

            Application.Run(pbDialog);
        }

        /// <summary>
        /// Перебирает все комбинации предметов, пока не найдёт до 10 
        /// подходящих, удовлетворя словарю <paramref name="requiredAspects"/>.
        /// </summary>
        /// <param name="requiredAspects">Ключ=имя аспекта, значение=минимальное значение</param>
        /// <returns>Список списков (до 10) предметов AspectedItem</returns>
        private static List<List<AspectedItem>> FindCombinations(Dictionary<string, double> requiredAspects)
        {
            var found = new List<List<AspectedItem>>();
            int maxCount = _items.Count;

            for (int r = 1; r <= maxCount; r++)
            {
                var combos = GetCombinations(_items, r);
                foreach (var combo in combos)
                {
                    // Суммируем аспекты combo
                    var sumDict = new Dictionary<string, double>();
                    foreach (var item in combo)
                    {
                        foreach (var kv in item.Aspects)
                        {
                            if (!sumDict.ContainsKey(kv.Key))
                                sumDict[kv.Key] = 0;
                            sumDict[kv.Key] += kv.Value;
                        }
                    }

                    // Проверяем, подходят ли под requiredAspects
                    bool ok = true;
                    foreach (var req in requiredAspects)
                    {
                        sumDict.TryGetValue(req.Key, out double have);
                        if (have < req.Value)
                        {
                            ok = false;
                            break;
                        }
                    }
                    if (ok)
                    {
                        found.Add(combo);
                        if (found.Count >= 10)
                            return found;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Генерирует все сочетания размера <paramref name="r"/> 
        /// из списка <paramref name="items"/>.
        /// </summary>
        /// <param name="items">Исходные элементы</param>
        /// <param name="r">Размер сочетания</param>
        /// <returns>Последовательность списков AspectedItem</returns>
        private static IEnumerable<List<AspectedItem>> GetCombinations(List<AspectedItem> items, int r)
        {
            var result = new List<List<AspectedItem>>();
            RecurCombine(items, 0, r, new List<AspectedItem>(), result);
            return result;
        }

        /// <summary>
        /// Вспомогательный рекурсивный метод для формирования сочетаний размера r.
        /// </summary>
        /// <param name="items">Исходные элементы</param>
        /// <param name="start">Текущий индекс, с которого добавляем элементы</param>
        /// <param name="r">Сколько элементов ещё нужно добавить</param>
        /// <param name="current">Текущее формируемое сочетание</param>
        /// <param name="result">Список всех сгенерированных сочетаний</param>
        private static void RecurCombine(List<AspectedItem> items, int start, int r, List<AspectedItem> current, List<List<AspectedItem>> result)
        {
            if (r == 0)
            {
                result.Add(new List<AspectedItem>(current));
                return;
            }
            for (int i = start; i < items.Count; i++)
            {
                current.Add(items[i]);
                RecurCombine(items, i + 1, r - 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}
