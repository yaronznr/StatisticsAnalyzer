
namespace ServicesLib
{
    public class ServiceContainer
    {
        private static ExcelService _excelService;
        public static ExcelService ExcelDocumentService()
        {
            if (_excelService == null)
            {
                _excelService = new ExcelService();
            }

            return _excelService;
        }

        private static AnalyzerService _analyzerService;
        public static AnalyzerService AnalyzerService()
        {
            if (_analyzerService == null)
            {
                _analyzerService = new AnalyzerService();
            }

            return _analyzerService;
        }

        private static ModelService _modelService;
        public static ModelService ModelService()
        {
            if (_modelService == null)
            {
                _modelService = new ModelService();
            }

            return _modelService;
        }

        private static StorageService _storageService;
        public static StorageService StorageService()
        {
            if (_storageService == null)
            {
                _storageService = new StorageService();
            }

            return _storageService;
        }

        private static IrService _irService;
        public static IrService RService()
        {
            if (_irService == null)
            {
                _irService = new IrService();
            }

            return _irService;
        }

        private static EnvironmentService _environmentService;
        public static EnvironmentService EnvironmentService()
        {
            if (_environmentService == null)
            {
                _environmentService = new EnvironmentService();
            }

            return _environmentService;
        }
    }
}
