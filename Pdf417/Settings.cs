namespace Pdf417
{
    /// <summary>
    /// Натройки для формирования штрихкода PDF417
    /// </summary>
    public struct Settings
    {
        /// <summary>
        /// Отношение высоты модуля PDF417 к ширине (по умолчанию 3)
        /// </summary>
        public int YHeight;

        /// <summary>
        /// Ширина модуля PDF417 в пиикселях (по умолчанию 4)
        /// </summary>
        public int ModuleWidth;

        /// <summary>
        /// Уровень коррекции ошибок (по умолчанию <see cref="Pdf417.CorrectionLevel.Auto"/>)
        /// </summary>
        public CorrectionLevel CorrectionLevel;

        /// <summary>
        /// Отношение ширины штрих-кода к высоте (по умолчанию 2.2)
        /// </summary>
        public double AspectRatio;

        /// <summary>
        /// Ширина и высота "Тихой зоны" в пикселях (по умолчанию 8)
        /// </summary>
        public int QuietZone;

        /// <summary>
        /// Настройки по умолчанию
        /// </summary>
        public static Settings Default => new Settings
        {
            YHeight = 3,
            ModuleWidth = 4,
            CorrectionLevel = CorrectionLevel.Auto,
            AspectRatio = 2.2,
            QuietZone = 8
        };
    }
}