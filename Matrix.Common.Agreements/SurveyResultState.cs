namespace Matrix.Common.Agreements
{
    public enum SurveyResultState
    {
        /// <summary>
        /// Метод не реализован в драйвере
        /// </summary>
        NotImplemented,
        /// <summary>
        /// Неправильные входные параметры
        /// </summary>
        InvalidIncomingParameters,
        /// <summary>
        /// Устройство не отвечает
        /// </summary>
        NoResponse,
        /// <summary>
        /// Ответ от устройства есть, но не распознан
        /// </summary>
        NotRecognized,
        /// <summary>
        /// Чтение успешно
        /// </summary>
        Success,
        /// <summary>
        /// Не все записи архива считаны
        /// </summary>
        PartialyRead,
        /// <summary>
        /// Непонятная ситуация
        /// </summary>
        Unknown,
    }
}
