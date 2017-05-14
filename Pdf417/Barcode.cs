using System;
using System.Runtime.CompilerServices;

namespace Pdf417
{
    /// <summary>
    /// Создание шитрихкода в формате PDF417
    /// По описанию отсюда https://grandzebu.net/informatique/codbar-en/pdf417.htm
    /// https://github.com/harbour/core/blob/master/contrib/hbzebra/pdf417.c
    /// </summary>
    public struct Barcode
    {
        /// <summary>
        /// Внутреннее пркдставление изображения
        /// </summary>
        public readonly MonoCanvas Canvas;

        /// <summary>
        /// Количество строк штрих-кода
        /// </summary>
        public int RowsCount => _rows;

        /// <summary>
        /// Количество колонок в модулях
        /// </summary>
        public int ColumnsCount => (_dataColumns + 4) * 17 + 1;

        /// <summary>
        /// Количество строк штрих-кода
        /// </summary>
        private readonly int _rows;

        /// <summary>
        /// Количество столбцов с данными
        /// </summary>
        private readonly int _dataColumns;

        /// <summary>
        /// Внутреннее представление данных штрих-кода в виде массива битовых векторов
        /// </summary>
        private readonly BitVector[] _internalData;

        /// <summary>
        /// Настройки штрих-кода
        /// </summary>
        private readonly Settings _settings;

        /// <summary>
        /// Левый индикатор
        /// </summary>
        private readonly BitVector[] _leftIndicator;

        /// <summary>
        /// Правый индикатор
        /// </summary>
        private readonly BitVector[] _rightIndicator;

        /// <summary>
        /// Длина слова PDF417
        /// </summary>
        private const int WordLen = 17;

        /// <summary>
        /// Максимальное количество кодовых слов, умещаемое в штрих-коде
        /// </summary>
        private const int MaxCodeWords = 925;

        /// <summary>
        /// Битовое представление Start Pattern
        /// </summary>
        private static readonly BitVector StartPattern = new BitVector(0b11111111010101000UL, true);

        /// <summary>
        /// Битовое представление Stop Pattern
        /// </summary>
        private static readonly BitVector StopPattern = new BitVector(0b11111110100010100UL, true);

        /// <summary>
        /// Создает новый экземпляр <see cref="Barcode"/>, с данными из массива байт
        /// </summary>
        /// <param name="input">Данные для кодировки в штрих-коде</param>
        /// <param name="settings">Настройки  штрих-кода</param>
        public Barcode(byte[] input, Settings settings) : this()
        {
            _settings = settings;

            // Получаем массив кодовых слов из входных данных
            (var data, int rdl) = GetDataFromBytes(input);

            // Определяем уровень коррекции ошибок, если он не задан
            _settings.CorrectionLevel  = DetermineCorrectionLevel(settings.CorrectionLevel, rdl);

            // Сумммарное количество значимых кодовых слов
            // Длина + данные + коррекции
            int cl = 2 << (int) _settings.CorrectionLevel;
            int cwCount = 1 + rdl + cl;

            // Создаем хранилище кодовых слов
            (_rows, _dataColumns, _internalData) = CreateDataStorage(cwCount, settings.AspectRatio);

            // Заполняем индикаторы
            _leftIndicator = new BitVector[_rows];
            _rightIndicator = new BitVector[_rows];
            FillIndicators();

            // Длина блока данных
            data[0] = _internalData.Length - cl;

            // Заполняем пустоты
            for (int i = rdl; i < data[0]; i++)
                data[i] = 900;

            // Данные
            for (int i = 0; i < data[0]; i++)
                _internalData[i] = new BitVector(Tables.LowLevel[(i / _dataColumns) % 3][data[i]], true);

            // Коррекция ошибок
            var corrections = GetReedSolomonCorrections(data, data[0]);
            Array.Copy(corrections, 0, _internalData, data[0], corrections.Length);

            Canvas = FillCanvas();
        }

        /// <summary>
        /// Создает новый экземпляр <see cref="Barcode"/>, с данными из строки
        /// </summary>
        /// <param name="input">Данные для кодировки в штрих-коде</param>
        /// <param name="settings">Настройки  штрих-кода</param>
        public Barcode(string input, Settings settings) : this()
        {
            _settings = settings;

            // Получаем массив кодовых слов из входных данных
            (var data, int rdl) = GetDataFromText(input);

            // Определяем уровень коррекции ошибок, если он не задан
            _settings.CorrectionLevel  = DetermineCorrectionLevel(settings.CorrectionLevel, rdl);

            // Сумммарное количество значимых кодовых слов
            // Длина + данные + коррекции
            int cl = 2 << (int) _settings.CorrectionLevel;
            int cwCount = 1 + rdl + cl;

            // Создаем хранилище кодовых слов
            (_rows, _dataColumns, _internalData) = CreateDataStorage(cwCount, settings.AspectRatio);

            // Заполняем индикаторы
            _leftIndicator = new BitVector[_rows];
            _rightIndicator = new BitVector[_rows];
            FillIndicators();

            // Длина блока данных
            data[0] = _internalData.Length - cl;

            // Заполняем пустоты
            for (int i = rdl; i < data[0]; i++)
                data[i] = 900;

            // Данные
            for (int i = 0; i < data[0]; i++)
                _internalData[i] = new BitVector(Tables.LowLevel[(i / _dataColumns) % 3][data[i]], true);

            // Коррекция ошибок
            var corrections = GetReedSolomonCorrections(data, data[0]);
            Array.Copy(corrections, 0, _internalData, data[0], corrections.Length);

            Canvas = FillCanvas();
        }

        /// <summary>
        /// Заполнить <see cref="_internalData"/> байтами
        /// </summary>
        /// <param name="input">Данные в виде массива байт</param>
        /// <returns>Данные в виде массива кодовых слов, и реальная длина данных</returns>
        private (int[], int) GetDataFromBytes(byte[] input)
        {
            int len = input.Length;

            int dl = (len / 6) * 5 + len % 6 + 1;
            int[] data = new int[dl + 8];

            var mode = len == 1
                ? ControlChar.ShiftToByte
                : (len % 6 == 0 ? ControlChar.SwitchToByteMod6 : ControlChar.SwitchToByte);
            data[1] = (int) mode;

            int ipos = 0, opos = 2;
            for (int i = 0; i < input.Length / 6; i++)
            {
                ulong s = 0;
                for (uint j = 0; j < 6; j++, ipos++)
                    s += input[ipos] * Pow(256, 5 - j);

                for (int j = 0, b = opos; j < 5; j++, opos++, s /= 900)
                    data[b + 4 - j] = (int)(s % 900);
            }

            for (; ipos < len; opos++, ipos++)
                data[opos] = input[ipos];

            return (data, opos);
        }

        /// <summary>
        /// Получить данные из текста
        /// </summary>
        /// <param name="input">Данные в виде ASCII строки</param>
        /// <returns>Данные в виде массива кодовых слов, и реальная длина данных</returns>
        private (int[], int) GetDataFromText(string input)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";
            const string lower = "abcdefghijklmnopqrstuvwxyz ";
            const string mixed = "0123456789&\r\t,:#-.$/+%*=^? ";
            const string punct = ";<>@[\\]_`~!\r\t,:\n-.$/\"|*()\0{}'";
            const int utll = 27, utml = 28, utps = 29;
            const int ltus = 27, ltml = 28, ltps = 29;
            const int /*mtpl = 25,*/ mtll = 27, mtul = 28, mtps = 29;
            //const int ptul = 29;

            char mode = 'u'; // Текущий режим { u | l | m | p }
            // Выделяем буфер для формирования предварительных данных размером 8+input.Length
            int[] pre = new int[input.Length + 8];
            int pos = 1, cur = 0, pp = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                switch (mode)
                {
                    case 'u': // upper
                    {
                        int k = upper.IndexOf(c);
                        if (k >= 0) Push(k);
                        else
                        {
                            k = lower.IndexOf(c);
                            if (k >= 0)
                            {
                                mode = 'l';
                                Push(utll);
                                Push(k);
                                continue;
                            }

                            k = mixed.IndexOf(c);
                            if (k >= 0)
                            {
                                mode = 'm';
                                Push(utml);
                                Push(k);
                                continue;
                            }

                            k = punct.IndexOf(c);
                            if (k >= 0)
                            {
                                Push(utps);
                                Push(k);
                                continue;
                            }

                            throw new IndexOutOfRangeException($"Index not found for [{c}]");
                        }

                        break; // upper
                    }

                    case 'l': // lower
                    {
                        int k = lower.IndexOf(c);
                        if (k >= 0) Push(k);
                        else
                        {
                            k = upper.IndexOf(c);
                            if (k >= 0)
                            {
                                if (input.Length > i + 1 && upper.IndexOf(input[i + 1]) >= 0)
                                {
                                    mode = 'u';
                                    Push(ltml);
                                    Push(mtul);
                                    Push(k);
                                    continue;
                                }

                                Push(ltus);
                                Push(k);
                                continue;
                            }

                            k = mixed.IndexOf(c);
                            if (k >= 0)
                            {
                                mode = 'm';
                                Push(ltml);
                                Push(k);
                                continue;
                            }

                            k = punct.IndexOf(c);
                            if (k >= 0)
                            {
                                Push(ltps);
                                Push(k);
                                continue;
                            }

                            throw new IndexOutOfRangeException($"Index not found for [{c}]");
                        }

                        break; // lower
                    }

                    case 'm': // mixed
                    {
                        int k = mixed.IndexOf(c);
                        if (k >= 0) Push(k);
                        else
                        {
                            k = upper.IndexOf(c);
                            if (k >= 0)
                            {
                                mode = 'u';
                                Push(mtul);
                                Push(k);
                                continue;
                            }

                            k = lower.IndexOf(c);
                            if (k >= 0)
                            {
                                mode = 'l';
                                Push(mtll);
                                Push(k);
                                continue;
                            }

                            k = punct.IndexOf(c);
                            if (k >= 0)
                            {
                                Push(mtps);
                                Push(k);
                                continue;
                            }

                            throw new IndexOutOfRangeException($"Index not found for [{c}]");
                        }

                        break; // mixed
                    }
                }

                void Push(int val)
                {
                    if (pp % 2 == 0) cur = val * 30;
                    else pre[pos++] = cur + val;

                    pp++;
                }
            }

            if (pp % 2 == 1) pre[pos++] = cur + 29;

            return (pre, pos);
        }

//        /// <summary>
//        /// Заполнить <see cref="_internalData"/> цифрами
//        /// </summary>
//        /// <param name="input">Данные в виде строки с десятичным числом</param>
//        private void FillDataNumeric(byte[] input)
//        {
//            throw new NotImplementedException();
//        }

        /// <summary>
        /// Заполнение кодов Рида-Соломона (масив будет перевернут)
        /// </summary>
        /// <param name="data">Массив данных</param>
        /// <param name="length">Длина блока данных</param>
        private BitVector[] GetReedSolomonCorrections(int[] data, int length)
        {
            const int module = 929;
            int k = 2 << (int)_settings.CorrectionLevel;
            int[] c = new int[k];
            ushort[] a = GetFactors();
            BitVector[] ret = new BitVector[k];

            for (int i = 0; i < length; i++)
            {
                int t = (data[i] + c[k - 1]) % module;
                for (int j = k - 1; j >= 0; j--)
                    if (j == 0)
                        c[j] = (module - (t * a[j]) % module) % module;
                    else
                        c[j] = (c[j - 1] + module - (t * a[j]) % module) % module;
            }

            for (int j = 0; j < k; j++)
                if (c[j] != 0) c[j] = module - c[j];

            for (int i = 0; i < k; i++)
                ret[i] = new BitVector(Tables.LowLevel[((i + length) / _dataColumns) % 3][c[k - i - 1]], true);

            return ret;
        }

        /// <summary>
        /// Заполнить индикаторы
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillIndicators()
        {
            int x1 = (_rows - 1) / 3;
            int x2 = (int) _settings.CorrectionLevel * 3 + (_rows - 1) % 3;
            int x3 = _dataColumns - 1;
            for (int i = 0; i < _rows; i++)
            {
                int t = i % 3;
                int xleft = t == 0 ? x1 : (t == 1 ? x2 : x3);
                int xright = t == 0 ? x3 : (t == 1 ? x1 : x2);
                _leftIndicator[i] = new BitVector(Tables.LowLevel[t][(i / 3) * 30 + xleft], true);
                _rightIndicator[i] = new BitVector(Tables.LowLevel[t][(i / 3) * 30 + xright], true);
            }
        }

        /// <summary>
        /// Зполнить полотно для рисования изображением штрих-кода PDF417
        /// </summary>
        private MonoCanvas FillCanvas()
        {
            var s = _settings;
            var canvas = new MonoCanvas(
                (4 + _dataColumns) * WordLen * s.ModuleWidth + s.QuietZone * 2 + 1,
                _rows * s.YHeight * s.ModuleWidth + s.QuietZone * 2);

            for (int i = 0; i < _rows; i++)
            {
                int j = 0;

                // StartPattern
                for (int k = 0; k < WordLen; k++, j++) DrawModule(StartPattern[k]);

                // Left row indicator
                for (int k = 0; k < WordLen; k++, j++) DrawModule(_leftIndicator[i][k]);

                // Data
                for (int l = 0; l < _dataColumns; l++)
                for (int k = 0; k < WordLen; k++, j++)
                    DrawModule(_internalData[l + i * _dataColumns][k]);

                // Right row indicator
                for (int k = 0; k < WordLen; k++, j++) DrawModule(_rightIndicator[i][k]);

                // StopPattern
                for (int k = 0; k < WordLen; k++, j++) DrawModule(StopPattern[k]);

                DrawModule(true);

                void DrawModule(bool color)
                {
                    for (int x = 0; x < s.ModuleWidth; x++)
                    for (int y = 0; y < s.ModuleWidth * s.YHeight; y++)
                        canvas[s.QuietZone + j * s.ModuleWidth + x,
                            s.QuietZone + i * s.ModuleWidth * s.YHeight + y] = !color;
                }
            }

            return canvas;
        }

        /// <summary>
        /// Определяет уровень коррекции ошибок
        /// </summary>
        /// <param name="correctionLevel">Установленный уровень коррекции</param>
        /// <param name="cwDataCount">Количество кодовых слов с данными</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CorrectionLevel DetermineCorrectionLevel(CorrectionLevel correctionLevel, int cwDataCount)
        {
            var ret = correctionLevel;
            if (ret == CorrectionLevel.Auto)
            {
                switch (cwDataCount)
                {
                    case int x when x <= 40 || (x > 911 && x <= 919):
                        ret = CorrectionLevel.Level2;
                        break;

                    case int x when (x > 40 && x <= 160) || (x > 895 && x <= 911):
                        ret = CorrectionLevel.Level3;
                        break;

                    case int x when (x > 160 && x <= 320) || (x > 863 && x <= 895):
                        ret = CorrectionLevel.Level4;
                        break;

                    case int x when x > 320 && x <= 863:
                        ret = CorrectionLevel.Level5;
                        break;

                    case int x when x > 919 && x <= 923:
                        ret = CorrectionLevel.Level1;
                        break;

                    default:
                        ret = CorrectionLevel.Level0;
                        break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Создать двумерный массив с кодовыми словами на основании размера
        /// </summary>
        /// <param name="cwCount">Количество кодовых слов</param>
        /// <param name="aspectRatio">Отношение ширины к высоте</param>
        /// <returns>Пустой массив для хранения данных и его размеры</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (int rows, int dataColumns, BitVector[] data) CreateDataStorage(int cwCount, double aspectRatio)
        {
            if (cwCount > MaxCodeWords)
                throw new ArgumentException($"Codewords count {cwCount} more than maximum {MaxCodeWords}");

            // Расчет количества строк и столбцов
            // Ширина = 1 + 17 * (4 + dataColumns(x)) модулей, Высота = rows(y) * _yHeight(h) модулей
            // x * y = c, где cwCount <= c < cwCount+x  =>  y = (c/x) * aspectRatio(a)
            // 69 + 17x = ahc/x  =>  17x^2 + 69x - ahc = 0  =>
            // x = (sqrt(4761 + 68ahc)-69)/(2*17) - количество столбцов с данными

            int x = (int) Math.Ceiling((Math.Sqrt(4761d + 68 * aspectRatio * _settings.YHeight * cwCount) - 69) / 34);
            int y = cwCount / x + (cwCount % x == 0 ? 0 : 1);

            return (y, x, new BitVector[x * y]);
        }

        /// <summary>
        /// Быстрое целочисленное возведение в степень
        /// </summary>
        /// <param name="n">Число, которое необходимо возвести в степень</param>
        /// <param name="e">Степень</param>
        /// <remarks>https://ru.wikibooks.org/wiki/Реализации_алгоритмов/Быстрое_возведение_в_степень</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong Pow(ulong n, uint e)
        {
            ulong ret = 1UL;
            while (e != 0)
            {
                if (e % 2 == 1) ret *= n;
                n *= n;
                e >>= 1;
            }

            return ret;
        }

        /// <summary>
        /// Получить таблицу с решениями полинома для кодов Рида-Соломона для нашего случая
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort[] GetFactors()
        {
            switch (_settings.CorrectionLevel)
            {
                case CorrectionLevel.Level0:
                    return Tables.ReedSolomon00;

                case CorrectionLevel.Level1:
                    return Tables.ReedSolomon01;

                case CorrectionLevel.Level2:
                    return Tables.ReedSolomon02;

                case CorrectionLevel.Level3:
                    return Tables.ReedSolomon03;

                case CorrectionLevel.Level4:
                    return Tables.ReedSolomon04;

                case CorrectionLevel.Level5:
                    return Tables.ReedSolomon05;

                case CorrectionLevel.Level6:
                    return Tables.ReedSolomon06;

                case CorrectionLevel.Level7:
                    return Tables.ReedSolomon07;

                case CorrectionLevel.Level8:
                    return Tables.ReedSolomon08;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_settings.CorrectionLevel));
            }
        }
    }
}
