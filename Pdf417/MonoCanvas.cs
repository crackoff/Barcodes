using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Pdf417
{
    /// <summary>
    /// Полотно для рисования монохромного изображения
    /// </summary>
    public class MonoCanvas
    {
        /// <summary>
        /// Ширина полотна в пикселях
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Высота полотна в пикселях
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Ширина изображения с учетом выравнивания
        /// </summary>
        private readonly int _alignedWidth;

        /// <summary>
        /// Битовый вектор для хранения точек изображения
        /// Значение бита 0 - черный цвет, 1 - белый
        /// </summary>
        private BitVector _bitVector;

        /// <summary>
        /// Создание нового экземпляра полотна для рисования монохромного изображения
        /// </summary>
        /// <param name="width">Ширина полотна в пикелях</param>
        /// <param name="heigth">Высота полотна в пикселях</param>
        public MonoCanvas(int width, int heigth)
        {
            Width = width;
            _alignedWidth = (int) Math.Ceiling(width / 32d) * 32;
            Height = heigth;
            _bitVector = new BitVector(_alignedWidth * heigth, true);
        }

        /// <summary>
        /// Получение или установка цвета точки по координате полотна
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        public bool this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _bitVector[y * _alignedWidth + x]; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _bitVector[y * _alignedWidth + x] = value; }
        }

        /// <summary>
        /// Сохранить изображение в виде BMP файла
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <remarks>Описание взято здесь: https://en.wikipedia.org/wiki/BMP_file_format</remarks>
        public void SaveBmp(string path)
        {
            var bmp = GetBmpBytes();
            File.WriteAllBytes(path, bmp);
        }

        /// <summary>
        /// Получить массив байт, представляющие изображение в BMP формате
        /// </summary>
        public byte[] GetBmpBytes()
        {
            byte[] imageBytes = GetImageBytes();

            const int hsize = 0x3E, dibsize = 0x28;
            int size = hsize + imageBytes.Length;
            byte[] ret = new byte[size];

            // Магический заголовок
            ret[0] = 0x42;
            ret[1] = 0x4d;

            // Размер - со 2 байта
            Array.Copy(BitConverter.GetBytes(size), 0, ret, 2, sizeof(int));

            // Смещение начала изображения - с 10 байта
            Array.Copy(BitConverter.GetBytes(hsize), 0, ret, 10, sizeof(int));

            // Размер DIB заголовка
            Array.Copy(BitConverter.GetBytes(dibsize), 0, ret, 14, sizeof(int));

            // Ширина изображения
            Array.Copy(BitConverter.GetBytes(Width), 0, ret, 18, sizeof(int));

            // Высота изображения
            Array.Copy(BitConverter.GetBytes(Height), 0, ret, 22, sizeof(int));

            // the number of color planes (must be 1)
            ret[26] = 1;
            ret[27] = 0;

            // Количетво бит на пиксель (всегда 1)
            ret[28] = 1;
            ret[29] = 0;

            // Битовые маски палитры (?)
            ret[0x3A] = ret[0x3B] = ret[0x3C] = 0xFF;

            // Размер изображения
            Array.Copy(BitConverter.GetBytes(imageBytes.Length), 0, ret, 34, sizeof(int));

            // Изображение
            Array.Copy(imageBytes, 0, ret, hsize, imageBytes.Length);

            return ret;
        }

        /// <summary>
        /// Получить изображение в виде массива байт
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] GetImageBytes()
        {
            int isize = (int) Math.Ceiling(_alignedWidth * Height / 8d);
            var ret = new byte[isize];

            _bitVector.CopyTo(ret, true);

            // Переворачиваем изображение
            const int bitsPerByte = 8;
            int bytesPerRow = _alignedWidth / bitsPerByte;
            for (int y = 0; y < Height / 2; y++)
            {
                for (int x = 0; x < bytesPerRow; x++)
                {
                    int i1 = y * bytesPerRow + x;
                    int i2 = (Height - y - 1) * bytesPerRow + x;
                    byte tmp = ret[i1];
                    ret[i1] = ret[i2];
                    ret[i2] = tmp;
                }
            }

            return ret;
        }
    }
}