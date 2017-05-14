/**
(c) Nikolay Martyshchenko
*/

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Pdf417
{
    /// <summary>
    /// Массив битовых значений для компактного и эффективного хранения без необходимости вычислять маски вручную
    /// </summary>
    /// <remarks>
    /// Упрощенный (и оптимизированный под 64-бит) аналог стандартного класса <see cref="System.Collections.BitArray"/>
    ///
    /// В отличие от стандартной реализации не поддерживает динамическое изменение размера хранения
    ///
    /// Внутреннее представление для хранения значений: массив ulong, поэтому все значения выравниваются исходя из этого
    ///
    /// NOTE: Валидация попадания в заданный исходный диапазон значений (0 .. length) не производится для ускорения операций,
    /// поэтому возможно пограничное некорректное обращение к несуществующему индексу
    ///
    /// Например, при длине 2 бит, обращение к битам 2..63 так же будет считаться валидным, как и к 0..1
    /// Ошибка (выход за пределы диапазона) в данном случае будет выдана только при обращении к битам c номером больше 64
    ///
    /// Для текущего применения данная особенность некритична
    ///
    /// (c) Nikolay Martyshchenko
    /// </remarks>
    [DebuggerDisplay("{" + nameof(DebugDisplay) + ",nq}")]
    public struct BitVector
    {
        /// <summary>
        /// Количество бит в байте
        /// </summary>
        private const int BitsPerByte = 8;

        /// <summary>
        /// Количество бит на единицу хранения
        /// </summary>
        private const int BitsPerStorage = sizeof(ulong) * BitsPerByte;

        /// <summary>
        /// Массив для хранения битовых значений в упакованном виде
        /// </summary>
        private readonly ulong[] _array;

        /// <summary>
        /// Признак того, что все битовые значения в массиве установлены в 0
        /// </summary>
        public bool IsFalseForAll => Array.TrueForAll(_array, item => item == 0UL);

        /// <summary>
        /// Отображение в отладчике
        /// </summary>
        public string DebugDisplay => _array == null ? "null" : Convert.ToString((long)_array[0], 2);

        /// <summary>
        /// Инициализация нового экземпляра класса <see cref="BitVector" />
        /// </summary>
        /// <param name="length">Максимальное количество обрабатываемых битов</param>
        /// <param name="defaultValue">Устанавливаемое для всех битов значение по умолчанию</param>
        public BitVector(int length, bool defaultValue = false)
        {
            _array = new ulong[GetArrayLength(length, BitsPerStorage)];

            ulong fillValue = defaultValue ? unchecked ((ulong) -1) : 0UL;

            if (!defaultValue) return;

            for (int i = 0; i < _array.Length; i++)
            {
                _array[i] = fillValue;
            }
        }

        /// <summary>
        /// Инициализация нового экземпляра класса <see cref="BitVector" />.
        /// </summary>
        /// <param name="initValue">Значения для битов</param>
        /// <param name="rotateBits">Перевернуть биты</param>
        /// <remarks>(c) Oleg Krekov</remarks>
        /// <remarks>Переворачивание битов не совсем честное, т.к. при завершении не выполняется смещение вправо,
        /// таким образом, затрагиваются только то количетво бит, которое было заполнено, а с хвоста может оставаться "мусор"</remarks>
        public unsafe BitVector(ulong initValue, bool rotateBits)
        {
            _array = new ulong[1];

            if (!rotateBits)
                _array[0] = initValue;
            else
            {
                ulong r = initValue, v = initValue;
                for (v >>= 1; v != 0; v >>= 1)
                {
                    r <<= 1;
                    r |= v & 1;
                }

                fixed (void* dst = _array)
                    Buffer.MemoryCopy(&r, dst, sizeof(ulong), sizeof(ulong));
            }
        }

        /// <summary>
        /// Получение или установка значения бита по заданному индексу
        /// </summary>
        /// <param name="index">Индекс обрабатываемого бита</param>
        /// <returns>Значение бита в указанной позиции</returns>
        public bool this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [Pure]
            get
            {
                return (_array[index/BitsPerStorage] & (1UL << (index%BitsPerStorage))) != 0UL;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value) _array[index/BitsPerStorage] |= (1UL << (index%BitsPerStorage));
                else _array[index/BitsPerStorage] &= ~(1UL << (index%BitsPerStorage));
            }
        }

        /// <summary>
        /// Сброс всех разрядов в 0
        /// </summary>
        public void Clear()
        {
            Array.Clear(_array, 0, _array.Length);
        }

        /// <summary>
        /// Установка всех битов в заданное значение
        /// </summary>
        /// <param name="value">Значение, в которое требуется установить все биты</param>
        public void SetAll(bool value)
        {
            ulong fillValue = value ? unchecked((ulong)-1) : 0UL;

            for (int i = 0; i < _array.Length; i++)
            {
                _array[i] = fillValue;
            }
        }

        /// <summary>
        /// Используется для вычисления необходимого размера массива для хранения <paramref name="n"/> значений при размерности хранения одного элемента в <paramref name="div"/>
        /// </summary>
        /// <param name="n">Количество хранимых значений</param>
        /// <param name="div">Вместимость одной единицы хранения</param>
        /// <returns>Необходимый размер массива для хранения заданного количества элементов</returns>
        /// <remarks>
        /// Реально вычисляется (n+(div-1))/div, но формула изменена на ((n-1)/div) + 1 чтобы избежать арифметического переполнения при вычислении
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        private static int GetArrayLength(int n, int div)
        {
            return n > 0 ? (((n - 1) / div) + 1) : 0;
        }

        /// <summary>
        /// Инициализация состояния битовой маски из переданной маски
        /// </summary>
        /// <param name="mask">Маска используемая как источник состояния</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(BitVector mask)
        {
            Array.Copy(mask._array, _array, _array.Length);
        }

        /// <summary>
        /// Копировать данные в массив байт
        /// </summary>
        /// <param name="array">Принимаемый массив</param>
        /// <param name="useMsb">Использовать MSB (https://en.wikipedia.org/wiki/Bit_numbering)</param>
        /// <remarks>(c) Oleg Krekov</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyTo([NotNull] byte[] array, bool useMsb)
        {
            fixed (void* src = _array)
            fixed (void* dst = array)
                Buffer.MemoryCopy(src, dst, array.Length, array.Length);

            if (!useMsb) return;

            // Переворачиваем биты в MSB
            for (var i = 0; i < array.Length; i++)
                array[i] = (byte) ((array[i] * 0x0202020202UL & 0x010884422010UL) % 1023);
        }

        /// <summary>
        /// Реализация оператора & (побитовое AND)
        /// </summary>
        /// <param name="lhs">Левый аргумент</param>
        /// <param name="rhs">Правый аргумент</param>
        /// <returns>
        /// Результат операции
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitVector operator &(BitVector lhs, BitVector rhs)
        {
            int length;
            int min;

            if (lhs._array.Length <= rhs._array.Length)
            {
                length = rhs._array.Length;
                min = lhs._array.Length;
            }
            else
            {
                length = lhs._array.Length;
                min = rhs._array.Length;
            }

            var r = new BitVector(length*BitsPerStorage);

            for (int i = 0; i < min; i++)
            {
                r._array[i] = lhs._array[i] & rhs._array[i];
            }

            return r;
        }
    }
}