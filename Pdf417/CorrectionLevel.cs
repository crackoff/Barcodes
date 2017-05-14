namespace Pdf417
{
    /// <summary>
    /// Уроыень коррекции
    /// </summary>
    public enum CorrectionLevel : byte
    {
        /// <summary>
        /// Автоматический. Рекомендуется оставить его.
        /// </summary>
        Auto = 0xFF,

        /// <summary>
        /// 2 кодовых слова
        /// </summary>
        Level0 = 0,

        /// <summary>
        /// 4 кодовых слова
        /// </summary>
        Level1 = 1,

        /// <summary>
        /// 8 кодовых слов
        /// </summary>
        Level2 = 2,

        /// <summary>
        /// 16 кодовых слов
        /// </summary>
        Level3 = 3,

        /// <summary>
        /// 32 кодовых слова
        /// </summary>
        Level4 = 4,

        /// <summary>
        /// 64 кодовых слова
        /// </summary>
        Level5 = 5,

        /// <summary>
        /// 128 кодовых слова
        /// </summary>
        Level6 = 6,

        /// <summary>
        /// 256 кодовых слов
        /// </summary>
        Level7 = 7,

        /// <summary>
        /// 512 кодовых слов
        /// </summary>
        Level8 = 8
    }
}