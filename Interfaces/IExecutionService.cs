namespace ScreTranPlus;

public interface IExecutionService
{
    /// <summary>
    /// Starts service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops service.
    /// </summary>
    void Stop();

    /// <summary>
    /// Ручной запуск захвата и перевода (для Manual Mode)
    /// </summary>
    void TriggerManualTranslation();
}