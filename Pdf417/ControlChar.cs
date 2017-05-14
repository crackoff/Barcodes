namespace Pdf417
{
    /// <summary>
    /// Управляющие символы для PDF417
    /// </summary>
    public enum ControlChar
    {
        /// <summary>
        /// Переключиться в текстовый режим
        /// </summary>
        /// <remarks>Рекомендуется использовать данный символ для блоков выравнивания</remarks>
        SwitchToText = 900,

        /// <summary>
        /// Переключиться в режим байт
        /// </summary>
        SwitchToByte = 901,

        /// <summary>
        /// Переключиться в режим чисел
        /// </summary>
        SwitchToNumeric = 902,

        /// <summary>
        /// Переключить в режим байт только для следующего CW
        /// </summary>
        ShiftToByte = 913,

        Initialization = 921,
        MacroPdfTerminator = 922,
        MacroPdfOptionalFields = 923,
        SwitchToByteMod6 = 924,
        MacroPdfControlHeader = 928
    }
}