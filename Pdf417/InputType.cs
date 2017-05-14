namespace Pdf417
{
    /// <summary>
    /// Тип входных данных
    /// </summary>
    public enum InputType
    {
        /// <summary>
        /// Массив байт
        /// </summary>
        Bytes,

        /// <summary>
        /// Текст в ASCII (31-127, 9, 10, 13)
        /// </summary>
        Text,

        /// <summary>
        /// Целое положительное число (только цифры 0-9)
        /// </summary>
        Numeric
    }
}