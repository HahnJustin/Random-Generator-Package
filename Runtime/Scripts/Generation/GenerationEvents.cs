using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator
{
    public delegate void GenerationStartHandler(GenerationInfo info);
    public delegate void ConfigGeneratedHandler(AbstractGeneratorConfig config, float amount);
    public delegate void GenerationEndHandler(GenerationInfo info);
    public delegate void GenerationCancelHandler();
    public delegate void GenerationErrorHandler(string errorMessage);
    public delegate void UngeneratedChangeCheckHandler(bool ungeneratedChanges);

    public class GenerationEvents
    {
        public event GenerationStartHandler OnGenerationStart;
        public event ConfigGeneratedHandler OnConfigGenerated;
        public event GenerationEndHandler OnGenerationEnd;
        public event GenerationCancelHandler OnGenerationCancel;
        public event GenerationErrorHandler OnGenerationError;
        public event UngeneratedChangeCheckHandler OnUngeneratedCheck;

        public void RaiseGenerationEnd(GenerationInfo info) => OnGenerationEnd?.Invoke(info);
        public void RaiseConfigGenerated(AbstractGeneratorConfig config, float amount) => OnConfigGenerated?.Invoke(config, amount);
        public void RaiseGenerationStart(GenerationInfo info) => OnGenerationStart?.Invoke(info);
        public void RaiseGenerationCancel() => OnGenerationCancel?.Invoke();
        public void RaiseGenerationError(string errorMessage) => OnGenerationError?.Invoke(errorMessage);

        public void RaiseUngeneratedChangesCheck(bool ungeneratedChanges) => OnUngeneratedCheck?.Invoke(ungeneratedChanges);
    }
}